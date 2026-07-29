# Acadix — Backend Setup

What you need to do outside the code for the backend fixes to actually take effect.

---

## 1. Deploy the Firestore security rules  ← **most important**

The rules in `firebase/firestore.rules` are the only thing stopping someone from posting a fake
Brain Score or reading other students' data. **They do nothing until deployed.**

```bash
npm install -g firebase-tools     # once
firebase login                    # once
firebase use your-firebase-project-id
firebase deploy --only firestore:rules
```

Then confirm in the Firebase console → Firestore → Rules that what you see matches the file.

> **Check this first.** New Firebase projects often start in test mode:
> `allow read, write: if request.time < timestamp.date(2026, 1, 1);`
> That lets *anyone on the internet* read and write your whole database. If that is what is
> currently live, deploying the new rules is urgent.

### What the rules now enforce
- Only signed-in users can read the leaderboard.
- You can only write **your own** row (`request.auth.uid == userId`).
- Every field is type- and range-checked, so `brainScore: 999999` is rejected.
- No extra fields can be added.
- `updatedAt` must be near server time, so entries cannot be backdated.
- Everything outside the leaderboard is denied by default.

---

## 2. Turn on App Check (recommended)

Rules verify *what* is written but not *what app* is writing it. App Check rejects requests
that do not come from your real, unmodified APK.

Firebase console → **App Check** → register the Android app with **Play Integrity** → set the
Firestore API to *Enforced* once you see legitimate traffic being verified.

---

## 3. Verify the Google OAuth client type

Google sign-in uses a custom-scheme redirect (`com.companyname.acadsjulie://auth`), which only
works with an **Android**-type OAuth client. If `ACADIX_GOOGLE_OAUTH_CLIENT_ID` holds a **Web**
client ID, sign-in fails with `redirect_uri_mismatch`.

Google Cloud console → *APIs & Services* → *Credentials*:
- The client must be type **Android**.
- Package name: `com.companyname.acadsjulie`
- SHA-1: from your signing keystore —
  `keytool -list -v -keystore %USERPROFILE%\.android\debug.keystore -alias androiddebugkey -storepass android`

Add the release keystore's SHA-1 too before publishing.

---

## 4. Local configuration

Runtime config comes from `Configuration/FirebaseSettings.local.cs` (git-ignored):

```bash
cp Configuration/FirebaseSettings.local.example.cs Configuration/FirebaseSettings.local.cs
```

Fill in the real values. `.env.example` documents the same settings but **is not read on
Android** — there are no process environment variables on-device.

---

## 5. One-time data migration (automatic)

Local data is now namespaced per account (`u_{uid}_user_profile` etc.). On first launch after
this update, existing data is migrated into the first account that signs in, so current users
keep their progress. This runs once per install — later accounts start clean, which is the
point of the fix.

Nothing to do; just be aware that on a device where two people previously shared the app, the
existing data now belongs to whoever logs in first. Anyone else gets a fresh profile.

---

## 6. Before publishing to Google Play

- [x] In-app account deletion (Profile → Delete My Account) — **required by Play policy**
- [ ] A **web page** describing how to request deletion — also required; link it in the listing
- [ ] Privacy policy covering what you collect (email, display name, scores)
- [ ] Complete the Play Console *Data safety* form
- [x] `android:allowBackup="false"` set in `AndroidManifest.xml`, with `backup_rules.xml` and
      `data_extraction_rules.xml` excluding SharedPreferences as a safety net if backup is ever
      re-enabled
- [ ] Release keystore SHA-1 added to the Google OAuth client (step 3)

---

## Still outstanding (not fixed in code)

These are deliberate scope calls, listed so they are not forgotten:

- **The client still computes its own scores.** Rules bound them to plausible values, but a
  determined cheater inside those bounds is not detectable. Truly authoritative scoring needs a
  Cloud Function, which is a bigger change than this pass.
- **No automated tests / CI.** The scoring, streak and XP logic are pure functions and would be
  easy to unit-test.
- **No retry queue for failed syncs.** A failed upload is retried on the next save or on app
  suspend, but nothing persists across a force-quit while offline.

---

## 7. Trivia questions now come from the Open Trivia DB

The knowledge quiz previously served ~130 hardcoded questions from
`Data/TriviaQuestionBank.cs`. It now fetches from [OpenTDB](https://opentdb.com) and keeps the
local bank as an offline fallback.

**No configuration needed** — OpenTDB requires no API key.

### How it works
- `OpenTriviaService` calls the API using a **session token**, which guarantees the API never
  returns the same question twice for that player until the pool is exhausted.
- `TriviaQuestionProvider` merges remote + local questions and keeps its own per-account
  "already seen" history, so no-repeat also works offline and across token resets.
- Requested difficulty (Easy/Medium/Hard) is passed straight to the API.
- If the network is unavailable, the round silently falls back to the local bank and shows
  "Offline — using Acadix's own questions."

### Category mapping
| Acadix | OpenTDB |
|---|---|
| History | 23 (History) |
| Math | 19 (Science: Mathematics) |
| Science / Space / Biology | 17 (Science & Nature) |
| Animals | 27 (Animals) |
| General | 9 (General Knowledge) |
| **Philippine History** | **local only — deliberately** |

Philippine History stays on the curated local bank: OpenTDB's History category is overwhelmingly
Western and would replace curriculum-relevant content with unrelated questions.

### Rate limits
OpenTDB allows **one request per IP every 5 seconds**. All calls go through a shared throttle,
and Endless/Zen prefetches the next batch of 50 before the queue runs dry so the player never
waits mid-round. Do not lower `MinRequestInterval` in `OpenTriviaService`.

### Attribution
OpenTDB content is licensed **CC BY-SA 4.0**. If you publish the app, credit the Open Trivia
Database in your about/credits screen.
