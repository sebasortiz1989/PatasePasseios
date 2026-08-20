# DapperDemo — contrast table, both modes

21 Aug 2026. Every text pair in the refresh, measured WCAG 2.1 (sRGB relative
luminance, `(L1+0.05)/(L2+0.05)`). Floor is **4.5:1** for text below 24px and
**3:1** for text at or above 24px, for icons, and for non-text UI boundaries.

Five of the six type steps sit below 24px, including the default body (18px), so
the 3:1 large-text allowance almost never applies. It applies to `type.title`
(30px) and to the 24px tab glyphs, and nowhere else.

---

## 1. Composited values

The light muted inks are authored as alphas of `#201F1D`. Alphas are not
measurable, so every one is composited against the surface it actually sits on
before measuring. These are the composites:

| Alpha of `#201F1D` | over `#F3F2F2` | over `#EAE9E9` | over `#E0DFDF` |
|---|---|---|---|
| 45% | `#949392` | `#8F8E8D` | `#8B8A89` |
| 55% | `#7F7E7D` | `#7A7978` | `#777675` |
| 65% | `#6A6968` | `#666564` | `#636261` |
| 75% | `#555452` | `#525150` | `#50504E` |
| 16% (hairline) | `#D1D0D0` | `#CAC9C9` | `#C4C3C2` |

Dark mode is authored as explicit hex, not alpha — see the decisions file §2.

---

## 2. Light mode

Page `#F3F2F2` · raised `#E6E5E5` · control `#D9D8D8`

Surface steps: page → raised is **ΔL\* 4.54**, raised → control **ΔL\* 4.61**. The
token set's `#EAE9E9` was ΔL\* 3.14 and is not used — see decisions §2.1.

### 2.1 Ink on surfaces

| Foreground | Background | Ratio | 4.5:1 | Used for |
|---|---|---|---|---|
| `ink.primary` `#201F1D` | page `#F3F2F2` | **14.73:1** | pass | body, titles |
| `ink.primary` | raised `#E6E5E5` | **13.10:1** | pass | rows in a group |
| `ink.primary` | control `#D9D8D8` | **11.58:1** | pass | every label on a control |
| `ink.secondary` `#5F5E5D` (70%) | page | **5.79:1** | pass | captions, metadata |
| `ink.secondary` | raised | **5.15:1** | pass | row captions |
| `ink.secondary` | control | **4.55:1** | pass | passes — the old exception is gone |
| `ink.disabled` `#949392` (45%) | page | 2.74:1 | **FAIL** | non-text only |

### 2.2 Alphas that were rejected

Measured because they are in the app today, and this is why they are gone:

| Foreground | Background | Ratio | Verdict |
|---|---|---|---|
| 55% `#7F7E7D` | page | 3.62:1 | fails — was the caption colour |
| 65% `#6A6968` | page | 4.90:1 | passes the page… |
| 65% `#6A6968` | raised `#E6E5E5` | **4.36:1** | …and fails the group surface. This is why the step moved to 70% |
| 45% `#949392` | page | 2.74:1 | fails even 3:1 |
| 75% `#555452` | page | 6.77:1 | passes, but redundant beside 70% |

### 2.3 Accent

| Foreground | Background | Ratio | 4.5:1 | Verdict |
|---|---|---|---|---|
| `accent` `#B68235` | page | **3.02:1** | fail | large text, 24px glyphs and hairlines only |
| `accent` | raised | 2.68:1 | fail | **below 3:1 — not permitted for glyphs either** |
| `accent` | control | 2.37:1 | fail | **not permitted** |
| `accent.strong` `#7D5411` | page | **5.97:1** | pass | accent at body size |
| `accent.strong` | raised | **5.31:1** | pass | totals, figures |
| `accent.strong` | control | **4.69:1** | pass | the active tab cell |
| `accent.strong` | tint `#ECE5DB` | **5.33:1** | pass | selected chip label |
| `accent.deep` `#5A3B0A` | page | **9.10:1** | pass | the one heaviest figure |
| `ink.primary` | tint `#ECE5DB` | **13.17:1** | pass | selected chip, alternative |

The consequence is stated once and obeyed everywhere: **`accent` is a stroke and
a large-type colour on the page only. Anywhere it would sit on `surface.raised`
or `surface.control` it becomes `accent.strong`.** That is why the active tab
cell is `accent.strong` and not `accent`.

### 2.4 Non-text

| Pair | Ratio | Required | Verdict |
|---|---|---|---|
| `stroke.subtle` `#D1D0D0` on page | 1.38:1 | — | decorative hairline, exempt |
| `stroke.raised` `#C6C5C5` on raised | 1.27:1 | — | decorative hairline, exempt |
| `stroke.control` `#BBBABA` on control | 1.24:1 | — | the 3px page ring carries the edge, not this |
| raised `#E6E5E5` against page | 1.125:1 | — | Nib's own step is 1.120:1 |
| control `#D9D8D8` against page | 1.273:1 | 3:1 | **deliberately below** — see below |

The control's *fill* does not meet 3:1 against the page and is not required to:
the boundary of the control is carried by the 3px ring of page colour plus the
1px stroke, which is a hard geometric edge, and the control's contents are all
`ink.primary` at 12.38:1. WCAG 1.4.11 governs the boundary of a component where
that boundary is the only thing identifying it; here it is not.

---

## 3. Dark mode

Page `#1B1A18` · raised `#262523` · control `#302E2B`

### 3.1 Ink on surfaces

| Foreground | Background | Ratio | 4.5:1 | Used for |
|---|---|---|---|---|
| `ink.primary` `#EDEBE7` | page | **14.61:1** | pass | body, titles |
| `ink.primary` | raised | **12.86:1** | pass | rows in a group |
| `ink.primary` | control | **11.30:1** | pass | every label on a control |
| `ink.secondary` `#A5A09A` | page | **6.71:1** | pass | captions, metadata |
| `ink.secondary` | raised | **5.90:1** | pass | row captions |
| `ink.secondary` | control | **5.19:1** | pass | — still not used there, for parity |
| `ink.disabled` `#6F6B66` | page | 2.63:1 | **FAIL** | non-text only |

Both modes now pass on `surface.control` — light at 4.55:1 with the 70% step,
dark at 5.19:1. The rule *labels on a control are `ink.primary`* therefore stops
being a contrast requirement and becomes a consistency convention: it is kept
because a label that changes role with its surface is harder to port than one
that does not.

### 3.2 Accent

| Foreground | Background | Ratio | 4.5:1 | Verdict |
|---|---|---|---|---|
| `accent` `#D9A857` | page | **8.03:1** | pass | tint, glyphs, figures |
| `accent` | raised | **7.06:1** | pass | |
| `accent` | control | **6.21:1** | pass | the active tab cell |
| `accent.strong` `#EFCE93` | page | **11.53:1** | pass | totals |
| `accent.deep` `#F7E4BE` | page | **13.86:1** | pass | rare |
| `accent` | tint `#3C3529` | 6.05:1 | pass | selected chip label |

### 3.3 The light accent measured on the dark page

The brief predicted `#B68235` would fall short of 4.5:1 on a dark page. On a page
this dark it does not — but it fails where it is actually used:

| Pair | Ratio | Verdict |
|---|---|---|
| `#B68235` on page `#1B1A18` | 5.16:1 | passes |
| `#B68235` on raised `#262523` | 4.55:1 | passes by 0.05 |
| `#B68235` on control `#302E2B` | **3.99:1** | **fails** |

The tab bar is a `surface.control`, and the active tab is the single most
frequent accent in the app. So the accent needs a dark counterpart regardless —
the reasoning is in decisions §3, and it is not the reasoning the brief expected.

### 3.4 Non-text

| Pair | Ratio | Verdict |
|---|---|---|
| `stroke.subtle` `#34322F` on page | 1.36:1 | decorative, exempt |
| `stroke.raised` `#3C3A36` on raised | 1.29:1 | decorative, exempt |
| `stroke.control` `#4C4945` on control | 1.40:1 | edge carried by the ring |

---

## 4. Where this design sits below the floor on purpose

Four places, all of them argued rather than overlooked:

1. **`accent` on the light page at 3.02:1** — permitted for `type.title` (30px,
   over the 24px threshold), for 24px stroked glyphs, and for hairlines. Never
   for body, ui, caption or micro. Body-size accent is `accent.strong`.
2. **`ink.disabled` at 2.74:1 light / 2.63:1 dark** — disabled controls are
   exempt under 1.4.3, and dimming *is* the disabled affordance. It appears on
   exactly one control: the text-size slider while *Seguir o tamanho do sistema*
   is on. Darkening it would delete the only signal that the slider is inert.
3. **The control fill against the page at 1.273:1** — reasoned in §2.4.
4. **Hairlines everywhere** — decorative separators, not component boundaries.

No text pair anywhere in the design sits below its floor.
