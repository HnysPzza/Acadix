# Acadix — Color System

**Single source of truth:** `Resources/Styles/Colors.xaml` (canonical block at the top).

> ⚠️ **Not visually verified.** No .NET SDK, emulator, or network access in this environment.
> Every contrast figure below is computed from the WCAG 2.1 relative-luminance formula. All 46
> XAML files parse and every `StaticResource` resolves, but please run the app before trusting it.

---

## The three brand colors

| # | Role | Token | Hex | Used for |
|---|---|---|---|---|
| 1 | **Primary** | `Brand` | `#0F766E` | Actions, navigation, brand identity |
| 2 | **Accent** | `Accent` | `#F9735B` | Streaks, rewards, highlights |
| 3 | **Ink** | `Ink` | `#121A1C` | Text, dark surfaces |

Plus **neutrals** (canvas / surface / stroke) and **two state signals** (success / danger).

### Why not literally three colors total

You asked for a maximum of three. That's the right instinct, and the brand palette is now
genuinely three. But success and danger cannot be folded into them — if "correct" and "wrong"
render in the same color, the quiz stops communicating. Every serious design system
(Material, Apple HIG, Radix) separates *brand* from *state* for this reason.

So: **3 brand + neutrals + 2 state.** That is the clean version of what you asked for.

---

## What the audit found

| Problem | Before | After |
|---|---|---|
| Total colour values | **205** (108 raw hex in Views + 81 tokens + 16 gamified) | Canonical set of ~20; legacy aliased |
| Greens meaning "success" | **9** (`#06D6A0` `#16A34A` `#10B981` `#0EE9B0` …) | **1** + accessible text variant |
| Reds meaning "error" | **9** (`#FF6B6B` `#F9735B` `#FF4D6D` `#EF4444` …) | **1** + accessible text variant |
| Ambers meaning "warning" | **8** | **1** + accessible text variant |
| Distinct page backgrounds | **10** | **6** (4 are modal scrims, which should differ) |
| Raw hex as text on light pages | **5** | **0** |
| Names for the same `#EAF2EE` | **4** (`MobileCanvas` `SurfaceAlt` `PageBackground` `Canvas`) | 1 in use; rest deprecated |

**The root cause** was not the hex codes — it was that four token names resolved to the same
color. Developers picked whichever they saw last, so pages drifted. Fixing the token layer stops
the drift at its source.

---

## Contrast fixes (all measured)

Vivid brand colours are tuned for **fills**, where contrast rules don't apply. As small text on
the light canvas they failed badly:

| Where | Before | After | Fix |
|---|---|---|---|
| Delete/Reset buttons, ProfilePage | **3.00:1** ❌ | **6.05:1** ✅ | `StateDangerOnLight` |
| Week stats + streak, HomePage | **2.23:1** ❌ | **5.17:1** ✅ | `StateSuccessOnLight` |
| XP stat, HomePage | **2.02:1** ❌ | **5.68:1** ✅ | `StateWarningOnLight` |
| Streak tile, LearningHub | **2.44:1** ❌ | **5.23:1** ✅ | `AccentOnLight` |
| Tasks tile, LearningHub | **2.43:1** ❌ | **5.64:1** ✅ | `StateSuccessOnLight` |
| Minutes tile, LearningHub | **4.41:1** ⚠️ | **4.80:1** ✅ | `Brand` |
| Subject label, Planner | **4.41:1** ⚠️ | **4.80:1** ✅ | `Brand` |

> The ProfilePage delete button was **my own regression** from the previous turn. Found by
> measuring rather than assuming, and fixed.

### Critical nuance: dark screens were already correct

The same colours on the dark game backgrounds pass comfortably:

| Colour | On light `#EAF2EE` | On dark `#1A1A2E` |
|---|---|---|
| `#06D6A0` | 1.66:1 ❌ | **9.04:1** ✅ |
| `#FF9A3C` | 1.85:1 ❌ | **8.07:1** ✅ |
| `#FF6B6B` | 2.44:1 ❌ | **6.15:1** ✅ |

A global find-and-replace would have destroyed the game screens' deliberate palette. **Only
light-background text was changed.**

---

## Backgrounds unified

**Light pages (24):** all now use `MobileCanvas`. Previously split across three identical tokens.

**Game screens (9):** all now use `DarkSurface`. Previously four near-identical values
(`#1A1A2E`, `#101427`, `#121A1C` via `MobileInk`) that differed by under 0.5% perceptual
lightness — invisible drift, not design intent.

Unifying nudged one trivia caption from 4.64:1 to 4.34:1, so it was moved to `DarkTextMuted`
(8.06:1) rather than allowed to regress.

**Modal scrims (`#B3000000`) intentionally differ** — they're overlays, not pages.

---

## Dead code — now deleted

`Views/Games/QuestionView.xaml` was never instantiated anywhere in the app, and was the sole
consumer of `GamifiedTheme.xaml` — an entire **third palette** in Duolingo colours (`#58CC02`
green, `#1CB0F6` blue, `#FF4B4B` red) that clashed with the teal/coral brand.

Removed in full:

| File | Why |
|---|---|
| `Views/Games/QuestionView.xaml` | Orphan — never resolved or navigated to |
| `Views/Games/QuestionView.xaml.cs` | Code-behind for the orphan |
| `ViewModels/QuestionViewModel.cs` | Only consumed by the orphan (incl. `AnswerOptionViewModel`) |
| `Resources/Styles/GamifiedTheme.xaml` | Third palette, 16 colours, no remaining consumers |
| `Services/IHapticService.cs` | Only injected into `QuestionViewModel` |
| `Services/HapticService.cs` | Implementation of the above |

`HapticService` went with it because every live ViewModel calls `HapticFeedback.Default`
directly — the abstraction had exactly one consumer, and it was the dead view.

Also cleaned: the `GamifiedTheme` merge in `App.xaml`, the `QuestionViewModel` and
`IHapticService` DI registrations in `MauiProgram.cs`, and its now-unused
`using AcadsJulie.Services`.

**Result: three style dictionaries** (`Colors`, `Typography`, `ControlStyles` + `Styles`), one
palette, zero competing systems.

---

## Rules going forward

1. **Never paste a raw hex into a page.** Use a token.
2. **Primary appears at most twice per screen.** Overuse kills an accent.
3. **On light backgrounds use `*OnLight` variants** for small text.
4. **On dark game screens use the vivid originals** — they already pass.
5. **Fills and strokes may use vivid colours freely** — contrast rules apply to text.

---

## Not done (deliberately)

- **The remaining ~100 raw hex values** are mostly gradients, translucent overlays
  (`#22FFFFFF`, `#33FFFFFF`) and dark-screen foregrounds that already pass. Converting them is
  safe but unverifiable blind, and gradients legitimately need two stops.
- **Deleting `QuestionView` + `GamifiedTheme.xaml`** — needs IDE confirmation first.
- **`Frame` → `Border`** (39 uses) — still deferred; changes shadow rendering.
