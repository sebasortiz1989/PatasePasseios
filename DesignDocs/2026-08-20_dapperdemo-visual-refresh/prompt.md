# Prompt for Claude Design — DapperDemo visual refresh

> Paste everything below into Claude Design and attach the four files in `attach/`,
> beside this file. Nothing else — `README.md` in this folder explains what was left out
> of the Nib folder and why, including why no `.dc.html` is attached.

---

## What I want

Redraw every screen of an existing app, **DapperDemo** ("Patas & Passeios"), so that it
carries the structural discipline of the attached **Nib browsing surface** design — and
add one screen the app does not have yet, Settings.

The app keeps its palette, its serif type, its information architecture and its
behaviour. What changes is how the surfaces, controls, groups and type steps are
organised. Think of it as porting Nib's *rules*, not Nib's *skin*.

**Do not adopt Nib's colours.** Nib is warm cream with a navy accent. DapperDemo is warm
grey with a gold accent and it is staying that way. Section 2 lists the real values.

---

## 1. The app, unchanged

A cross-platform Avalonia app for a one-person pet-sitting business in Brazil. One
sitter, their tutors (owners), those tutors' dogs, and the services booked for them.

**All user-facing text is Brazilian Portuguese.** Use the app's existing vocabulary
verbatim — do not translate or re-word it:

| Concept | The word the app uses |
|---|---|
| Owner / client | **Tutor** (never "Cliente") |
| Dog walk | **Passeio** |
| Boarding / hotel stay | **Hospedagem** |
| Day care | **Creche** |
| Pet sitting visit | **Pet sitting** |
| Booked but not yet carried out | **A executar** / **A fazer** |
| Carried out | **Feito** |
| Unpaid | **Sem pagar** |
| Paid | **Pago** |
| Confirm / cancel buttons | **Sim** / **Não** |
| Money | `R$ 1.234,56` — comma decimal |
| Dates | `dd/MM/yyyy`, `21/08/2026, 09:00` |

Four service kinds exist. A hotel stay is priced as a **daily rate × nights**, plus an
optional one-off extra; the other three are a flat fee. Any service can carry a
**percentage discount**. Money is always shown as: price → (subtotal) → discount →
total.

---

## 2. The palette — keep these values

These are the app's real tokens. They came from an earlier Claude Design project and
they are not up for redesign. Reuse them exactly; give them Nib-style role names.

### Light (existing, authoritative)

| Role | Value | Currently called | Used for |
|---|---|---|---|
| `surface.primary` | `#F3F2F2` | `ColorBg` | the page |
| `surface.raised` | `#EAE9E9` | `ColorSurface` | a group of rows |
| `ink.primary` | `#201F1D` | `ColorText` | body and titles |
| `ink.secondary` | `#201F1D` at 55% | `ColorTextMuted55` | captions, metadata |
| `ink.tertiary` | `#201F1D` at 45% | `ColorTextMuted45` | hints, small print |
| `stroke.subtle` | `#201F1D` at 16% | `ColorDivider` | hairlines |
| `accent` | `#B68235` | `ColorAccent` | app tint, warm gold |
| `accent.strong` | `#7D5411` | `ColorAccentDark700` | emphasised figures, totals |
| `accent.deep` | `#5A3B0A` | `ColorAccentDark800` | rare, heaviest emphasis |
| `accent.tint` | `#B68235` at 12% | `ColorAccentTint12` | selected chip fill |
| `scrim` | `#201F1D` at 60% | `ColorScrim` | behind a modal |

There are further muted steps at 50%, 65%, 70% and 75% — consolidate them if you can
justify it, but say so rather than silently dropping them.

**Type is serif and stays serif.** Headings *Cormorant Garamond*, body *Lora*; both
currently fall back to a system serif stack because the font files are not bundled.
Design against the real faces and note the fallback. This is the single biggest
visual difference from Nib, which is system sans — do not "fix" it.

### Dark — you need to design this

**DapperDemo has no dark mode.** Deriving one is part of the job, and it is not a
mechanical inversion. Two problems to solve rather than paper over:

1. **Every muted ink is currently an alpha of `#201F1D` over a light page.** Flip the
   background and those alphas composite to the wrong thing. The dark ramp needs
   explicit values, or alphas re-based on a light ink over a dark ground. Say which you
   chose.
2. **The gold accent `#B68235` is a mid-tone.** It reads well on `#F3F2F2` but will be
   short of 4.5:1 against a dark page. It needs a lighter dark-mode counterpart in the
   same hue family — the way Nib moves `#1B3A6B` → `#8AA7CF` — not the same hex in both
   modes.

Give a contrast table for both modes, as the Nib spec does. Every text pair against
4.5:1, and state where you deliberately sit below it and why.

You will also need dark counterparts for `surface.raised`, and for the two new control
roles in section 3.

---

## 3. What to take from Nib

Structure, not colour. In rough order of how much it will change the look:

**Grouped rows on a raised surface.** Rows live in a group: `surface.raised`, radius 12,
inset 20 from each screen edge, rows padded 16, inner separators inset to the row's text
edge and never above the first row or below the last, 20 between groups. The `type.micro`
section label sits **outside** the group, on the page. This is the change that will make
DapperDemo look like Nib, because today it is mostly full-bleed rows on hairlines.

**Two new surface roles.** Nib added `surface.control` / `stroke.control` for floating
controls, one step above a card, because a control usually sits over one. DapperDemo will
need the same pair, derived from *its* palette. A floating control gets fill + 1px edge +
**a 3px ring of page colour outside the edge** — a hard keyline gap, not a shadow.

**No shadow, no gradient, no blur, no elevation, anywhere.** Separation is carried by
value, hairline and space only.

**Two type weights.** Regular and 600, and 600 appears in a countable number of places.
Name them.

**A six-step text-size ramp** driven from one base, with everything else derived by fixed
ratio. Margins, hit targets and control sizes do **not** scale with it.

**Stroked icons, never filled**, one geometry serving both the muted and the accent state
by following the foreground. DapperDemo already works this way at 24×24 — keep the
convention, improve the drawings. The existing dog, home-with-a-paw, tutors and profile
glyphs are serviceable but uneven in weight and detail; redraw the set to one standard.

**Rows answer one question.** Nib's rule is that a row exists to settle "is this the one I
mean", and everything not serving that is cut. Apply the same test to DapperDemo's dog,
tutor and service rows.

**Empty states are one sentence** at the text margin, in `ink.secondary`. No
illustration, no centred call-to-action button.

### Where Nib's rules do NOT transfer

Nib is a stack-based app with no tab bar and floating controls in fixed corner slots.
**DapperDemo is tab-based and keeps its bottom bar** — five tabs, currently a 100-unit
row with a hairline top edge: Agenda · Cachorros · Serviços · Tutores · Perfil.

So: apply Nib's surface and edge discipline *to* the tab bar; do not delete it and do not
replace it with floating corner controls. If you think some of Nib's floating-control
pattern earns its place alongside a tab bar, argue for it in the decisions file rather
than assuming it.

---

## 4. Screens to draw

Every one of these exists today. Redraw all of them.

**Entry**
1. **Login** — email, password, sign-in, link to sign-up
2. **Sign-up** — name, email, password, birth date

**Shell**
3. **Main shell** — the five-tab bottom bar, showing which tab is active

**Tabs**
4. **Agenda** (home) — services grouped by dog, expand/collapse per group, month/year
   filter, each row showing kind, date/time, price, and its *Execução* / *Pagamento*
   state. Empty: "Nenhum serviço neste filtro."
5. **Cachorros** — list of dogs with photo, name, tutor; "+ Novo"; empty state
6. **Tutores** — list of tutors with name, phone, dogs; "+ Novo"; empty state
7. **Serviços** — the new-booking form: kind selector (four), dog picker, date (single /
   range / multiple dates), a list of **times of day** you can add rows to, price (or
   price-per-day for a stay), **discount (%)**, and a summary line "N serviços serão
   criados."
8. **Perfil** — the sitter's own photo, name, Pix key, birth date; a monthly income
   panel with a show/hide-money eye; per-dog billing breakdown; counts; the backup
   block (export / import / automatic backup status); "Sair da conta"; version line

**Detail screens (pushed)**
9. **Dog detail** — photo, name, breed, description, tutor link, that dog's services,
   edit and delete
10. **Tutor detail** — contact, dogs, the tutor's bill by month, credit balance, payment
    entry and history, "Exportar resumo (PNG)"
11. **Service detail** — dog, tutor, kind, date, price, **subtotal / desconto / total**,
    settled-vs-outstanding split, credit note, *Feito* / *Pago* chips, add-to-calendar,
    edit form, delete
12. **New dog** — name, breed, description, tutor picker, photo picker
13. **New tutor** — name, phone, address

**Modal**
14. **ConfirmDialog** — a scrim over the current screen, not a window (the app runs
    single-view on mobile). Two forms: confirm (`Sim` / `Não`) and alert (one dismiss
    button). Used for deletes, for "replace this file?", and for the automatic-backup
    prompt.

**Also draw** the exported **PNG report** for a tutor — a document, not a screen, but it
is a user-facing surface with its own type hierarchy. Title, period, contact summary,
a services table with `Cachorro / Tipo / Data / Valor / Execução / Pagamento`, per-row
small print for a stay's nights and any discount, month totals, and a payment block with
the sitter's Pix key.

---

## 5. The new screen — Settings

New, modelled on Nib's Settings (v3 spec §12.2–12.3). Two sections.

**`APARÊNCIA`**
- Theme: **Claro / Escuro / Seguir o sistema**. Pick a control and defend it — Nib uses a
  switch plus a disabled dependent control, which is one option.
- A live preview so the choice is visible without leaving the screen.

**`TAMANHO DO TEXTO`**
- Six steps, from small to maximum, driven by one base with every other step derived.
- "Seguir o tamanho do sistema", default **on**. While on, the slider is **disabled but
  still shows where the system has put the ramp**, and the value line reads what the
  system has chosen. Turning it off makes the slider live at that same step.
- A live preview paragraph at the current size.
- State the ratios and give the full six-row table: body, heading, ui, caption, micro,
  and the resulting row height. Say explicitly that margins, hit targets and control
  sizes do not scale.

Nib expresses sizes in **px** everywhere and never pt — it caught a real bug that way.
Do the same.

**Where it lives.** DapperDemo has no top-right chrome slot, so Nib's "settings is
always the last cell in the top-right pill" does not transfer. Put it somewhere that
works for a tab-based app — a pushed screen from Perfil is the obvious candidate — and
say why in the decisions file.

---

## 6. Constraints

- **Do not change what the app does.** No new features, no removed features, no
  re-ordered navigation, no renamed concepts. This is a visual and structural pass.
- **Do not redesign the information architecture.** Five tabs stay five tabs.
- **pt-BR throughout**, using the vocabulary in section 1.
- **Design at phone scale in plain px**, the way the Nib artboards do. The port to the
  app multiplies by **1.7476** (the app authors on a 720-wide canvas), and text and
  text-sized controls take a further **×1.1**. Do not pre-scale — author at phone px and
  let the port do it.
- **Popups do not scale with the app's design canvas**, so anything in a drop-down or
  flyout must not depend on a canvas-derived font size. Prefer inline expansion to a
  popup wherever a picker is needed.
- Every screen is authored to stretch vertically — **never a fixed root height**.

---

## 7. What to hand back

Match the shape of the Nib output, which worked well:

1. **A design canvas** with an artboard per screen, light and dark side by side for at
   least the shell, one list screen, one detail screen, the report and Settings.
2. **A build sheet** — geometry tables precise enough to implement from without
   guessing: every size, inset, radius, weight, line-height and colour role per element.
3. **A decisions file** — what you rejected and why. This is the one I will argue with,
   so be specific rather than diplomatic. In particular I want your reasoning on: the
   dark palette derivation, the accent's dark counterpart, what the tab bar becomes,
   where Settings lives, and which of DapperDemo's six muted-ink steps survive.
4. **A contrast table** for both modes.

Where a Nib rule genuinely does not fit a tab-based, serif, gold-accented app, break it —
and record that you did, the way the Nib spec records its own broken rules.
