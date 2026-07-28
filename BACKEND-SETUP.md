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
- [ ] Set `android:allowBackup="false"` in `AndroidManifest.xml` if you do not want task notes
      pulled off the device via ADB backup
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
