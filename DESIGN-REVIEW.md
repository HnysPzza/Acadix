# Acadix — UI/UX Review

**Date:** 2026-07-29
**Method:** measured audit (contrast ratios computed, token usage counted), not visual inspection.

> ⚠️ **No build, no screenshots.** There is no .NET SDK or emulator in this environment, and no
> network access to install one. Every change below is token-level or additive precisely because
> structural changes could not be visually verified. Please run the app before trusting the result.

---

## What was measured

| Finding | Evidence | Status |
|---|---|---|
| Type scale sprawl | **27 distinct font sizes** (10…96), including likely typos `23` and `31` | ⚠️ Scale added; applied to setup screen only |
| Contrast failures on light backgrounds | Amber **1.77:1**, Coral **2.42:1**, Success **2.89:1** (AA needs 4.5) | ✅ Fixed |
| Radius sprawl | 12 `CornerRadius` values + 10 `StrokeShape` radii | ⚠️ Tokens added, not force-applied |
| Sub-minimum touch targets | 4 buttons at 40px (HIG 44 / Material 48) | ✅ Fixed |
| Hardcoded hex bypassing tokens | `#B0B0CC` ×35, `#22FFFFFF` ×49, `#33FFFFFF` ×29 | 📋 Documented |
| Deprecated `Frame` | 39 uses across 10 files | 📋 Deferred — see below |
| Online-only categories dead-end offline | Geography/Computers/Mythology have no local questions | ✅ Fixed |

---

## The most important finding

**Contrast failures were background-dependent, and blanket-replacing the colours would have been
wrong.**

The same three brand colours are used as text on *both* light and dark screens:

| Colour | On light canvas `#EAF2EE` | On dark game bg `#1A1A2E` |
|---|---|---|
| `MobileSuccess #16A34A` | **2.89:1** ❌ | **5.18:1** ✅ |
| `MobileCoral #F9735B` | **2.42:1** ❌ | **6.20:1** ✅ |
| `MobileAmber #F6A623` | **1.77:1** ❌ | **8.45:1** ✅ |

So the game screens were already fine. Only **3 instances** on light backgrounds needed changing.
A global find-and-replace would have wrecked the dark screens' deliberate palette.

**Fix:** added three *new* `*OnLight` tokens rather than editing the originals:

```xml
<Color x:Key="MobileAmberOnLight">#8A5A00</Color>    <!-- 5.20:1 -->
<Color x:Key="MobileCoralOnLight">#B23A22</Color>    <!-- 5.23:1 -->
<Color x:Key="MobileSuccessOnLight">#0F7434</Color>  <!-- 5.17:1 -->
```

Originals remain correct for fills, strokes and dark-background text.

---

## Design tokens (`Resources/Styles/Typography.xaml`)

**Type scale — 7 steps replacing 27 sizes:**

| Token | Size | Use |
|---|---|---|
| `TextDisplay` | 40 | Hero numbers |
| `TextTitle` | 28 | Screen titles |
| `TextHeadline` | 20 | Section headers |
| `TextBody` | 16 | Body copy |
| `TextCallout` | 14 | Secondary |
| `TextLabel` | 12 | Eyebrows, field labels |
| `TextCaption` | 11 | Metadata |

Migration map is in the file's header comment. Semantic styles (`TitleLabel`, `BodyLabel`,
`CaptionLabel`…) are **opt-in** — nothing changes until a label references one.

**Radius:** `RadiusSmall 8` · `RadiusMedium 16` · `RadiusLarge 28`
**Spacing:** 8pt grid, `Space1`–`Space8`
**Touch:** `TouchTargetMin 44`

---

## On fonts — deliberately not changed

A display/body font pairing is the single highest-impact fix for the "framework default" look.
**It is not implemented**, because only OpenSans Regular/Semibold are bundled and there is no
network access here to add more. Referencing a `FontFamily` whose file doesn't exist would break
the build.

The hierarchy improvements above work with the fonts you already have. To adopt a pairing later:

1. Add the `.ttf` to `Resources/Fonts/`
2. Register in `MauiProgram.cs` → `ConfigureFonts(...)`
3. Change `FontHeading` in `Typography.xaml`

Keep OpenSans for body text — it's a genuinely good UI face. Only the display/heading font needs
to change. Suggested (all open-licence): **Geist**, **Bricolage Grotesque**, or **Outfit**.

---

## Anti-slop rules applied

The trivia loading state was a centred `ActivityIndicator` — the definition of a framework
default. It is now a **skeleton screen** that mirrors the incoming question card's layout
(question lines + 2×2 answer grid), so content doesn't jump when it lands.

Rules followed throughout:
- ❌ No gradient buttons where the gradient carries no meaning
- ❌ No centred-everything — the loading state is left-aligned with real hierarchy
- ❌ No emoji as the *only* signifier — the offline banner pairs 📶 with a text label
- ❌ No drop shadow as the sole depth cue
- ✅ ALL-CAPS eyebrow carries `CharacterSpacing="2.4"`
- ✅ Every colour verified against its actual background, not assumed

---

## Deferred (with reasons)

**`Frame` → `Border` migration (39 uses, 10 files).** `Frame` is legacy in MAUI. But the two
render shadows and clipping differently, so this changes appearance in ways that **must** be
checked visually. Doing it blind risks silently breaking ten screens. Do this with an emulator
open, one file at a time.

**Global type-scale application.** Rewriting all 27 font sizes across 30 XAML files without
being able to see the result would cause clipping inside fixed-height `Grid` rows. The scale is
defined and applied to the trivia setup screen as a reference; migrate the rest incrementally.

**Consolidating the three parallel palettes** (`Mobile*` light, `Gamified*` dark, raw hex). Worth
doing, but it's a refactor that needs visual verification.

---

## New trivia categories

Added **Geography** (OpenTDB 22), **Computers** (18) and **Mythology** (20), wired through the
setup screen, theme service, and no-repeat history.

These have **no local questions**, so they can't be played offline. Rather than let players hit
a dead end after tapping Start, the setup screen now shows an amber "Needs internet" banner when
one is selected, and the in-game empty state names specific offline-capable alternatives.
