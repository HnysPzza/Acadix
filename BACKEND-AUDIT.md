# Acadix — Backend Audit

**Date:** 2026-07-29
**Scope:** `Services/`, `Configuration/`, `firebase/`, `Models/`, auth flow pages, `App.xaml.cs`, Android manifest
**Commit audited:** `54c5971` (`Juls First Project Commit`)

---

## 0. What "backend" means in this app

There is **no custom server**. The backend surface is:

| Component | Implementation |
|---|---|
| Identity | Firebase Auth via raw REST (`identitytoolkit.googleapis.com`) — email/password + Google IdP |
| Token storage | `SecureStorage` (Keystore-backed on Android) |
| Remote data | **One** Firestore collection: `leaderboards/global/users/{uid}`, via raw REST |
| Everything else | `Preferences` (Android SharedPreferences) — plaintext JSON, device-local |
| Scheduling | `Plugin.LocalNotification`, device-local |
| Authorization | `firebase/firestore.rules.example` (not deployed — example only) |

So ~95% of the app's "backend" is local device storage. The only true server round-trips are **auth** and **leaderboard sync**.

> **Note:** `dotnet` is not installed in this sandbox, so this is a static review — I could not compile or run the app. Nothing here is based on runtime observation.

---

## Summary

| Severity | Count |
|---|---|
| 🔴 High | 5 |
| 🟠 Medium | 8 |
| 🟡 Low | 7 |

**Good news first:** no secrets are committed. `.gitignore` correctly excludes `.env`, `Configuration/FirebaseSettings.local.cs`, `google-services*.json`, and `GoogleService-Info.plist`. I grepped the full working tree and git history for `AIza…` keys, `*.apps.googleusercontent.com`, and PEM blocks — the only hit is the placeholder in the `.example` file. Token handling correctly uses `SecureStorage` rather than `Preferences`, PKCE is implemented properly with S256 and a CSPRNG verifier, and `HttpClient` is a static singleton (no socket exhaustion). That's a better starting point than most student MAUI projects.

The problems are concentrated in **multi-account handling, trust of the client, and write amplification**.

---

## 🔴 High severity

### H1 — Local data is not scoped to an account (cross-user data bleed)

`LogoutAsync()` clears only `SecureStorage`:

```csharp
// Services/FirebaseAuthService.cs:105
public async Task LogoutAsync()
{
    _currentSession = null;
    SecureStorage.Default.Remove(UidKey);
    // … 4 more SecureStorage removals. Preferences is untouched.
}
```

But every gameplay artifact lives in **unnamespaced** `Preferences` keys: `user_profile`, `game_sessions`, `academic_tasks`, `user_goals`, `AcadsJulie_DailyQuests`, `course_progress`, `daily_challenge`, `content_bookmarks`.

**Consequence:** User A logs out → User B logs in on the same device → B inherits A's name, XP, level, streak, brain scores, 200 game sessions, **and A's academic tasks and notes**. Worse, `EnsureInitialSyncAsync()` then pushes that inherited profile up to **B's** leaderboard document under B's UID.

For a school app where devices get shared, this is the most serious issue in the codebase — it is both a correctness bug and a privacy leak (tasks contain free-text `Notes`).

**Fix:** Namespace every Preferences key with the UID (`$"{uid}:user_profile"`), or wipe non-auth Preferences on logout and on login-as-a-different-UID. The UID-namespacing approach is better because it preserves offline data for returning users. `App.ResetServices()` already exists to rebuild the service caches after such a switch — call it on login, not just on reset.

---

### H2 — The leaderboard is entirely client-authoritative

`RankingService.SyncCurrentUserAsync()` PATCHes whatever the client says:

```csharp
["brainScore"] = IntegerField(profile.BrainScore),
["level"]      = IntegerField(profile.Level),
["xp"]         = IntegerField(profile.XP),
["streakDays"] = IntegerField(profile.StreakDays),
["updatedAt"]  = TimestampField(DateTimeOffset.UtcNow)   // client clock
```

And the rules only check identity, not values:

```
allow create, update: if request.auth != null && request.auth.uid == userId;
```

Since `profile` is deserialized from plaintext SharedPreferences, **a rooted device or any repackaged APK can post `brainScore = 999999`**. `updatedAt` is client-supplied, so even "recently active" ordering is spoofable. There is no server-side scoring, no Cloud Function validation, no App Check.

**Fix (in order of effort):**
1. Add field validation to the rules — type checks, plausible ranges (`brainScore is int && brainScore >= 0 && brainScore <= 1000`), reject unknown keys via `request.resource.data.keys().hasOnly([...])`, and force `updatedAt == request.time`.
2. Enable **Firebase App Check** (Play Integrity on Android) — this alone stops repackaged-APK abuse.
3. If the leaderboard ever carries real stakes, move score computation into a Cloud Function that accepts raw session events and derives `brainScore` server-side.

Given this is a student project, (1) and (2) are the realistic scope.

---

### H3 — Every authenticated user can read every user's email address

```
match /leaderboards/global/users/{userId} {
  allow read: if request.auth != null;
}
```

The document contains `["email"] = StringField(session.Email)`, and `GetTopGlobalAsync` reads it into `RankingEntry.Email`. Any signed-in user can `GET` the whole collection and harvest the email of every registered student.

Notably, `Views/RankingPage.xaml` **never displays** `Email` — it only binds `DisplayName`. So the field is written, transmitted, and exposed for no functional reason.

**Fix:** Drop `email` from the leaderboard document entirely (it is unused), and add `.keys().hasOnly([...])` to the rules so it can't be reintroduced by a modified client. If you ever need email server-side, keep it in a separate `users/{uid}` doc readable only by its owner.

---

### H4 — Token refresh has a race condition and unsynchronized shared state

`_currentSession` is mutable instance state mutated from background threads with **no lock anywhere in the codebase** (I grepped: zero `lock`, `SemaphoreSlim`, or `Interlocked` usages).

```csharp
_currentSession.IdToken     = payload.IdToken;
_currentSession.RefreshToken = payload.RefreshToken;
_currentSession.ExpiresAt    = …;
await SaveSessionAsync(_currentSession);
```

`QueueLeaderboardSync()` fires `Task.Run(...)` per call, and several of these can be in flight simultaneously (see H5). Each calls `GetValidIdTokenAsync()`. If the token is near expiry, **multiple concurrent `RefreshIdTokenAsync()` calls** hit `securetoken.googleapis.com` at once.

Firebase refresh tokens are long-lived and generally reusable, so this is unlikely to hard-fail, but the consequences are real: interleaved writes produce a torn `FirebaseAuthSession`, concurrent `SecureStorage.SetAsync` writes to the same keys can race, and one failed refresh calls `LogoutAsync()` — **silently signing the user out mid-game** while other threads still hold the old session object.

**Fix:** Guard refresh with a `SemaphoreSlim(1,1)` and re-check `IsExpiredSoon` after acquiring it (double-checked locking), so concurrent callers await one refresh instead of racing.

---

### H5 — Leaderboard write amplification: one game produces 5–8 Firestore writes

`QueueLeaderboardSync()` is called from `ProfileService.SaveProfile()` and `ProgressService.AddSession()` — both extremely hot paths. Trace a single completed game through `DatabaseService.SaveGameResultAsync()`:

| Step | Triggers |
|---|---|
| `ProgressService.AddSession(...)` | sync #1 |
| `UpdateCategoryScore(...)` → `SaveProfile` | sync #2 |
| `AddXP(...)` → `SaveProfile` | sync #3 |
| `ChallengeService.ProcessGameResult` → `AddXP` → `SaveProfile` | sync #4 |
| `QuestService.ProcessGameResult` → … → `AddXP`/`AddBadge` → `SaveProfile` | sync #5+ |
| Trivia badges: up to 3 × `AddBadge` → `SaveProfile` | sync #6–8 |
| Explicit `App.QueueLeaderboardSync()` at the end | sync #9 |

That's up to **nine full-document PATCH requests, plus nine `SecureStorage` token reads, for one finished game** — all racing each other to write the same document. Firestore's free tier is 20k writes/day; a modest user base will burn through it, and the concurrent PATCHes to one document risk contention.

`ProfileService.CheckInToday()` has the same shape: `SaveProfile()` then `AddXP()` (which calls `SaveProfile()` again) = two syncs for one check-in.

**Fix:** Debounce. Replace the fire-and-forget `Task.Run` with a coalescing scheduler — set a dirty flag and have a single background loop flush at most once every N seconds (and on `OnSleep`). This also naturally fixes most of H4's concurrency pressure.

---

## 🟠 Medium severity

### M1 — No password reset, email verification, or account deletion

`FirebaseAuthService` implements `signUp`, `signInWithPassword`, `signInWithIdp`, and `token` refresh. It does **not** implement `accounts:sendOobCode` (password reset / email verification) or `accounts:delete`. I grepped the whole repo — there is no "Forgot password" affordance anywhere in the XAML.

Two consequences:
- **UX dead end:** a student who forgets their password is permanently locked out with no in-app recovery.
- **Play Store policy:** Google Play requires apps that support account creation to also offer in-app **account deletion** (and a web-accessible deletion route). This will block publication.

Email verification is also absent, so `email` is unverified — worth noting given H3 exposes it.

### M2 — Notification ID collisions

```csharp
public int NotificationId { get; set; } = Random.Shared.Next(10000, 99999);
```

Each task then uses `NotificationId`, `+1`, `+2`, `+3`. Problems:
- **Adjacent-range overlap:** two tasks assigned IDs `12345` and `12346` will silently cancel/overwrite each other's reminders — no collision check exists.
- **Birthday problem:** with ~89k slots and 4 IDs consumed per task, collisions become likely well before a user has many tasks.
- **Fixed-ID clash:** `ScheduleDailyOverdueCheck()` hardcodes `99999`, which is inside the random range. `App.xaml.cs` hardcodes `100`.

**Fix:** Use a persisted monotonically-increasing counter stride-4 (`id = ++counter * 4`), stored in Preferences, and move the fixed IDs to a reserved band (e.g. 1–99).

### M3 — Raw backend error strings are surfaced to end users

`MapFirebaseError` translates 7 known codes, then falls through to `_ => message` — the raw Firebase string. `AuthUiMessageMapper.ToUserMessage` does the same, returning `exception.Message` for anything unrecognized. `RankingService.ReadFirestoreErrorAsync` returns the raw Firestore error, and `RankingPage` renders it directly into `StatusLabel`.

So users can see things like `TOO_MANY_ATTEMPTS_TRY_LATER : Access to this account has been temporarily disabled…`, or Firestore's `Missing or insufficient permissions.` with project internals. It's low-grade information disclosure and poor UX.

**Fix:** Default to a generic friendly message; log the raw text instead of displaying it.

### M4 — No HTTP timeout, no retry, no offline handling

Both `HttpClient` instances use defaults — a **100-second** timeout. There is no `Connectivity.NetworkAccess` check anywhere. On a flaky campus network:
- `AuthGatePage.OnAppearing()` can hang on `RestoreSessionAsync()` for up to 100s on the splash screen, then fall into `catch { LogoutAsync(); NavigateToLogin(); }` — **logging out a valid user because the network blipped.** This is the most user-visible symptom of the group.
- `QueueLeaderboardSync` swallows the failure with a bare `catch {}` and there is no retry queue, so the score simply never syncs until some future event happens to trigger another sync.

**Fix:** Set `Timeout = TimeSpan.FromSeconds(15)`; distinguish network errors from auth errors in `AuthGatePage` so a transient failure doesn't force a logout; add a "pending sync" flag flushed on next launch/resume.

### M5 — Firestore rules are an unvalidated example with no deployment config

`firebase/firestore.rules.example` (11 lines) validates identity only — no field allowlist, no type checks, no size limits, no `updatedAt == request.time`. Because it's `.example`, there's also no `firebase.json`, no `.firebaserc`, and no `firestore.indexes.json`, so the rules aren't reproducibly deployable. The live rules could be anything — including the 30-day open-test default, which would make H2/H3 trivially exploitable by *unauthenticated* clients.

**Fix:** Promote to a real `firestore.rules` + `firebase.json`, add field validation, and verify what's actually deployed in the console.

### M6 — `DateTime.Now` vs `DateTime.UtcNow` are mixed

`GameSession.PlayedAt` defaults to `DateTime.UtcNow`; `GameResult.PlayedAt` defaults to `DateTime.Now`. `DatabaseService` copies the latter into the former, so stored values are *local* time despite the field's UTC-flavoured default. All the analytics then compare against local `DateTime.Today`:

```csharp
// ProgressService — weekly/monthly aggregation
var day = DateTime.Today.AddDays(-i);
sessions.Where(s => s.PlayedAt.Date == day)
```

Any `GameSession` constructed directly (rather than through `DatabaseService`) gets a UTC timestamp, which in PH (UTC+8) lands on the **previous day** for anything played before 08:00 local. Streaks (`LastCheckInDate`), quests (`GeneratedDate`), and the daily challenge all key off local `DateTime.Today`, while the leaderboard writes UTC. The result is off-by-one-day bugs in stats and potentially skipped streaks.

**Fix:** Standardize on `DateTimeOffset` UTC for storage and convert to local only for display and day-bucketing.

### M7 — `SaveGameResultAsync` is synchronous work behind an async signature

It returns `Task.CompletedTask` but does all its work inline: multiple JSON serializations of up to 200 sessions plus several synchronous `Preferences.Set` writes. Called from game-over handlers on the UI thread, this will jank the results screen as the session list grows.

**Fix:** Make it genuinely async (`Task.Run` for the serialize + write), or keep an in-memory model and flush on a timer / `OnSleep`.

### M8 — `GetSessions()` hands out its mutable internal cache

```csharp
public List<GameSession> GetSessions()
{
    if (_cachedSessions != null) return _cachedSessions;  // internal list, by reference
```

Callers (`CareerRecommendationService`, `DailyChallengeService`, `DatabaseService`, several pages) receive the live list. Any `.Add()`/`.Remove()` by a caller mutates cached state without persisting — and can throw `InvalidOperationException` if the list is modified while another thread enumerates it during a background sync. Same pattern in `TaskService.GetTasks()` and `GoalService.GetGoals()`.

Note also that `AddSession` trims to 200 sessions by **reassigning a new list** — so any caller holding a reference to the old list is now silently stale.

**Fix:** Return `IReadOnlyList<T>` or `.ToList()` copies.

---

## 🟡 Low severity

- **L1 — `.env.example` is fiction.** No code reads a `.env` file. `FirebaseSettings.GetSetting` only checks the compiled-in `ConfigureLocal` dictionary and then `Environment.GetEnvironmentVariable`, and on Android there are no process env vars at runtime — so `.env` is *never* consulted on the only shipping target. The real mechanism is `FirebaseSettings.local.cs`. Either delete `.env.example` or document it as build-host-only to avoid sending contributors down a dead end.
- **L2 — `ConfigureLocal` allocates per property read.** `GetLocalSetting` builds a new `Dictionary` and repopulates it on *every* access to `FirebaseProjectId`, `FirebaseWebApiKey`, etc. `IsFirebaseConfigured` reads two properties, and it's called on every auth/Firestore operation. Cache it in a `static readonly Lazy<>`.
- **L3 — Google OAuth client type may be misconfigured.** `GoogleOAuthService` uses a custom-scheme redirect (`com.companyname.acadsjulie://auth`) with the token exchange sent without a client secret. That is the correct *installed-app PKCE* shape — but it only works if `ACADIX_GOOGLE_OAUTH_CLIENT_ID` is an **Android/iOS-type** OAuth client. If a **Web**-type client ID was pasted in (the common mistake, and what the `.env` naming suggests), Google will reject the custom-scheme `redirect_uri` with `redirect_uri_mismatch`. Additionally, Firebase's `signInWithIdp` requires the ID token's audience to be registered in the Firebase project. Worth verifying against the actual console values — this is a config-side concern, not a code defect.
- **L4 — No CI, no tests, no `Directory.Build.props`.** No `.github/` directory, zero test projects. Scoring, streak, XP-levelling, and quest-matching logic are all pure functions with no UI dependency — they're the easiest possible unit-test targets and currently have zero coverage.
- **L5 — `Preferences` is plaintext.** Not exploitable without device access (app-private storage), but `android:allowBackup="true"` in the manifest means task notes and profile data can be pulled via ADB backup on some configurations. Consider `allowBackup="false"` or a backup rules file.
- **L6 — Dead code.** `FirebaseSettings.IsConfigured` is a never-referenced alias for `IsFirebaseConfigured`. `MissingConfigurationDiagnostics` is also unused.
- **L7 — Seven bare `catch {}` blocks** (`QuestService:27`, plus six in the game pages) silently swallow exceptions including deserialization failures. Corrupt quest JSON degrades to "regenerate quests" with no signal that anything went wrong. Log at minimum.

---

## Suggested order of work

**Before anything ships to more than one device:**
1. **H1** — UID-namespace the Preferences keys. Highest user-visible impact, ~30 lines.
2. **H3** — Delete the `email` field from the leaderboard doc. It's unused; a one-line removal plus a rules tightening.
3. **M5 + H2** — Promote the rules file to real `firestore.rules`, add field validation, deploy, and confirm what's currently live.

**Next:**
4. **H5** — Debounce `QueueLeaderboardSync` (this substantially relieves H4 too).
5. **H4** — `SemaphoreSlim` around token refresh.
6. **M4** — HTTP timeouts + don't log users out on transient network failure.

**Before Play Store submission:**
7. **M1** — Account deletion (policy blocker) and password reset (UX blocker).

**Cleanup:**
8. M2, M6, M7, M8, then the L-items.

---

## Architectural note

`App.xaml.cs` exposes 14 mutable `public static` service properties, while `MauiProgram.cs` registers a *different, smaller* set into the DI container. Services reach back through `App.ProfileService` / `App.ProgressService` statics rather than taking constructor dependencies (`DatabaseService`, `DailyChallengeService`, `CourseService`, and `TaskService` all do this), and `App.ResetServices()` swaps the singletons out from under any page currently holding a reference to the old instance.

This is the root cause behind several issues above — H1 (no lifecycle hook for "user changed"), H4 (no ownership of shared mutable state), and M8 (caches shared by reference). It's also why the code is hard to unit-test.

None of this is urgent for a student project, and I would **not** recommend a rewrite. But if the codebase keeps growing, migrating the statics into the existing `builder.Services` container — and making `ResetServices` a scope swap rather than a field reassignment — would prevent this class of bug from recurring.

---

## Appendix — verified clean

For the record, these were explicitly checked and found sound:

- No credentials in the working tree or in git history (`AIza…`, `*.apps.googleusercontent.com`, PEM blocks — placeholders only).
- `.gitignore` correctly covers `.env*`, `FirebaseSettings.local.cs`, `google-services*.json`, `GoogleService-Info.plist`.
- Auth tokens are in `SecureStorage`, not `Preferences` — correct choice.
- PKCE is textbook: 32 CSPRNG bytes via `RandomNumberGenerator.Fill`, S256 challenge, correct base64url encoding.
- `HttpClient` is `static readonly` in both services — no socket exhaustion.
- Android manifest requests only the four permissions actually used (`INTERNET`, `ACCESS_NETWORK_STATE`, `POST_NOTIFICATIONS`, `VIBRATE`) — no over-requesting.
- `WebAuthenticationCallbackActivity` intent filter correctly matches the `GoogleOAuthService` scheme/host constants.
- The partial-method `ConfigureLocal` pattern degrades gracefully: with no `FirebaseSettings.local.cs` present the call is elided by the compiler, the app still builds, and sign-in disables itself cleanly rather than crashing. Genuinely nice touch.
- `ProfileService.AddXP`'s level-up `while` loop terminates correctly (`XPToNextLevel = Level * 500` is strictly increasing).
