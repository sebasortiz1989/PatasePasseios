# DapperDemo — build sheet

21 Aug 2026 · "Patas & Passeios" · Avalonia, single-view on mobile.
Authored at **412 phone px**. The port to the app multiplies by **1.7476**
(412 × 1.7476 = 720, the app's design canvas width) and text and text-sized
controls take a further **× 1.1**. Nothing here is pre-scaled.

**Every size in this document is px. There is no pt value anywhere.** Where a
size is shown to the user — the text-size screen — the unit is written out.

---

## 1. Colour roles

Given by the brief and not redesigned, renamed to role names, plus four roles
this pass adds. Light values are authoritative; dark values are new.

| Role | Light | Dark | Use |
|---|---|---|---|
| `surface.primary` | `#F3F2F2` | `#1B1A18` | the page |
| `surface.raised` | `#E6E5E5` | `#262523` | a group of rows — **changed**, see below |
| `surface.control` | `#D9D8D8` | `#302E2B` | a floating control — **new** |
| `stroke.subtle` | `#D1D0D0` | `#34322F` | hairline on the page |
| `stroke.raised` | `#C6C5C5` | `#3C3A36` | separator inside a group — **new** |
| `stroke.control` | `#BBBABA` | `#4C4945` | 1px edge on a control — **new** |
| `ink.primary` | `#201F1D` | `#EDEBE7` | body, titles, every label on a control |
| `ink.secondary` | `#5F5E5D` (70%) | `#A5A09A` | captions, metadata |
| `ink.disabled` | `#949392` | `#6F6B66` | **non-text only** — the inert slider |
| `accent` | `#B68235` | `#D9A857` | app tint: glyphs, hairlines, 30px titles |
| `accent.strong` | `#7D5411` | `#EFCE93` | accent at body size: totals, active tab |
| `accent.deep` | `#5A3B0A` | `#F7E4BE` | the single heaviest figure on a screen |
| `accent.tint` | `#ECE5DB` | `#3C3529` | selected chip fill |
| `scrim` | `#201F1D` @ 60% | `#000000` @ 65% | behind a modal |

`surface.raised` is **not** the `#EAE9E9` in the token set. `#EAE9E9` is ΔL* 3.14
from the page — at the threshold of noticeability for a large flat area, and gone
under any warm-display filter. `#E6E5E5` is ΔL* 4.54, matching Nib's own
page→raised step of 4.48. `surface.control` follows one further step down.
Nothing depended on `#EAE9E9` (2 uses, 1 file), so this costs nothing. Reasoning
in decisions §2.1.

Light `ink.secondary` is the **70%** step — the app's most-used muted step (33
uses) and the only one that clears 4.5:1 on all three light surfaces. 65% fails
on the new group surface (4.36:1) and 55% fails on the page (3.62:1).
`ink.disabled` is the 45% step, demoted to non-text. Full mapping of all six
steps in the ink-mapping document.

**The one accent rule.** `accent` on `surface.raised` is 2.68:1 and on
`surface.control` is 2.37:1 — below even 3:1. So: *anywhere accent would sit on
a raised or control surface, it is `accent.strong`.* That is why the active tab
cell is `accent.strong`.

No shadow, no gradient, no blur, no elevation anywhere. Separation is value,
hairline, space, and the 3px ring.

---

## 2. Type

Headings **Cormorant Garamond**, body **Lora**. Neither is bundled in the app
today; both fall back to the platform serif stack (`Georgia, 'Times New Roman',
serif`). Design against the real faces; the fallback runs ~6% larger on the body
and must not be corrected by changing the ramp.

Seven roles, derived from the app's 26 hardcoded sizes — see the type-mapping
document for the full assignment and for why `display` and `section` exist.

**Two weights: 400 and 600.** Cormorant is *only ever 400* — display sizes take
the normal cut. 600 is Lora only and appears in exactly five places:

1. `type.micro` section labels
2. the active tab cell's label
3. the word *Total* and its figure in a money block
4. the confirm button label (`Sim`)
5. the selected cell of the service-kind selector

Nowhere else. There is no italic in the interface; italic is body-copy emphasis
only and does not appear on any screen here.

### 2.1 The ramp at the default step

| Token | Face | Size | Weight | Line-height | Tracking | Case |
|---|---|---|---|---|---|---|
| `type.display` | Cormorant | 42 | 400 | 46 | −0.01em | — |
| `type.title` | Cormorant | 30 | 400 | 34 | −0.005em | — |
| `type.section` | Cormorant | 22 | 400 | 28 | −0.002em | — |
| `type.body` | Lora | 18 | 400 | 28 | 0 | — |
| `type.ui` | Lora | 15 | 400 | 22 | 0 | — |
| `type.caption` | Lora | 13 | 400 | 20 | 0 | — |
| `type.micro` | Lora | 11 | 600 | 14 | +0.14em | uppercase |

Cormorant sets small on the body: 30px of Cormorant has roughly the presence of
24px of a sans. That is why `type.title` is 30 where a sans system would use 26,
and why the title/body ratio is 1.65 rather than the 1.4 the sizes suggest.

### 2.2 Derivation

The control sets **body**. Everything else follows fixed ratios:

- `display` = body × 2.35
- `title` = body × 1.65
- `section` = body × 1.22
- `ui` = body × 0.85
- `caption` = body × 0.74
- `micro` = body × 0.62
- line-height = size × 1.55, except `title` at × 1.13 (display type sets tighter)
- row height, one-line = round(body × 1.55) + 24

### 2.3 The six steps

| Step | Label | display | title | section | body | ui | caption | micro | Row h |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Pequeno | 38 | 26 | 20 | 16 | 14 | 12 | 10 | 49 |
| 2 | **Padrão** | **42** | **30** | **22** | **18** | **15** | **13** | **11** | **52** |
| 3 | Grande | 47 | 33 | 24 | 20 | 17 | 15 | 12 | 55 |
| 4 | Maior | 54 | 38 | 28 | 23 | 20 | 17 | 14 | 60 |
| 5 | Muito maior | 61 | 43 | 32 | 26 | 22 | 19 | 16 | 64 |
| 6 | Máximo | 68 | 48 | 35 | 29 | 25 | 21 | 18 | 69 |

**Margins, hit targets and control sizes do not scale.** Text margin stays 20,
minimum hit target stays 44, the tab bar stays 56, group radius stays 12, row
padding stays 12/12/16/16. The measure narrows as the text grows, which is what
large type is for.

Implementation: one base value fed into every text style at the root — not per-
view font literals, and not a multiplier applied at the call site.

---

## 3. Space, radius, hairline

`space.1` 4 · `space.2` 8 · `space.3` 12 · `space.4` 16 · `space.5` 20 ·
`space.6` 24 · `space.8` 32.

Radius: `radius.none` 0 · `radius.group` 12 · `radius.control` 12 ·
`radius.chip` 8. Nothing is a pill; nothing is a circle except a photo.

Hairline 1px logical. Text margin `space.5` = 20. Minimum hit target 44 × 44.
Screen width 412; measure 372; row text measure 340.

---

## 4. The group — the change that carries the refresh

Today the app is full-bleed rows on hairlines. Every list of rows becomes a
group:

| Property | Value |
|---|---|
| Fill | `surface.raised` |
| Radius | 12, all four corners |
| Inset from screen edge | 20 each side → 372 wide |
| Row padding | 12 top / 12 bottom / 16 leading / 16 trailing |
| Separator | 1px `stroke.raised`, inset to 16, **between rows only** — never above the first or below the last |
| Gap between groups | 20 |
| Section label | `type.micro`, `ink.secondary`, **outside** the group on `surface.primary`, at margin 20, 8 above the group |
| Single-row group | identical treatment, no special case |
| Bottom clearance | last group ends 96 above the safe area on any screen with the tab bar |

Applies to: agenda dog groups, dog list, tutor list, form field groups, detail
fact blocks, money blocks, profile blocks, settings sections, payment history,
report table body.

**Not applied to:** the page title, the section labels, the empty-state
sentence, and the report's own header block. Those sit on `surface.primary`
with no fill, edge or inset.

---

## 5. The floating control, and the tab bar

| Property | Value |
|---|---|
| Fill | `surface.control` |
| Edge | 1px `stroke.control` |
| Ring | **3px of `surface.primary` outside the edge** — a hard keyline gap |
| Radius | 12 |
| Behaviour | never hides, fades, shrinks or moves on scroll; content passes under it |
| Where it appears | **every screen inside the tabbed shell**, including pushed detail screens, pushed forms, Ajustes, and behind a dialog's scrim. Absent only on Login, Cadastro and the exported report |
| Every label and glyph inside | `ink.primary` — never `ink.secondary` |

The ring is implemented as a zero-blur, 3px-spread ring, not a shadow. There is
no shadow anywhere in the design.

### 5.1 The tab bar

Five tabs, unchanged in count and order: **Agenda · Cachorros · Serviços ·
Tutores · Perfil**. It stops being a full-bleed 100-unit row with a hairline top
edge and becomes a floating control group:

| Property | Value |
|---|---|
| Geometry | 372 × 56, inset 20 each side, 20 above the safe area, radius 12 |
| Fill / edge / ring | `surface.control` + 1px `stroke.control` + 3px `surface.primary` ring |
| Cells | five, 74.4 wide, 1px `stroke.control` dividers inset 10 top and bottom |
| Glyph | 24 × 24, 1.5 stroke, round cap and join, **stroked never filled** |
| Label | `type.micro`, 4 under the glyph |
| Inactive cell | glyph and label `ink.primary`, weight 400 |
| Active cell | glyph and label `accent.strong`, label weight **600** |
| Hit target | 74 × 56 — above 44 × 44 |

No fill behind the active cell, no indicator bar, no badge. The active state is
colour plus weight, which is the same signal the rest of the design uses.

### 5.2 The icon set, redrawn to one standard

24 × 24, 1.5 stroke, round cap and join, no fill, no two-tone. One geometry per
meaning; nothing else in the app may use a glyph that belongs to a tab.

| Tab | Glyph |
|---|---|
| Agenda | calendar — square, two hangers, one cross-rule |
| Cachorros | dog head — muzzle, two drop ears |
| Serviços | leash — a hook and a loop, with a plus at the top right |
| Tutores | two figures, the rear one offset and clipped |
| Perfil | one figure inside a ring |

*Tutores* and *Perfil* are the collision the old set got wrong — two people
versus one person at 24px is a coin-flip. The ring on *Perfil* is what separates
"me" from "them", and it is the only ring in the set.

---

## 6. Rows

Every row answers one question, and everything that does not serve it is cut.

| Row | The question | Line 1 | Line 2 | Trailing |
|---|---|---|---|---|
| Dog | is this the dog I mean | name, `type.body` `ink.primary` | `breed · tutor`, `type.caption` `ink.secondary` | 40 photo, radius 20 (leading, not trailing) |
| Tutor | is this the tutor I mean | name, `type.body` | `neighbourhood · n cachorros`, `type.caption` | `›` `ink.secondary` |
| Service (in a dog group) | which booking is this, and is it settled | `kind · date, time`, `type.body` | `R$ · execução · pagamento`, `type.caption` | — |
| Settings | — | label, `type.body` | value or state, `type.caption` | `›` or the control |

Cut from the dog row: the description, the service count, the date of the next
service. Cut from the tutor row: the address, the balance. Cut from the service
row: the tutor name (the group is the dog and the dog names the tutor).

Trailing `›` is `type.body` `ink.secondary`, and only on rows that push.

### 6.1 State words

Never a coloured pill, never a dot. `type.caption`, inline in line 2, separated
by ` · `:

| State | Rendering |
|---|---|
| `A executar` | `ink.secondary` |
| `Feito` | `ink.secondary` |
| `Sem pagar` | `accent.strong` — the only state that is coloured |
| `Pago` | `ink.secondary` |

Money owed is the one fact the sitter opens the app for, so it is the one state
that takes colour. Everything else is a word in grey.

---

## 7. Money

Always `R$ 1.234,56` — dot thousands, comma decimal, non-breaking space after
`R$`. Figures are tabular (`font-variant-numeric: tabular-nums`) everywhere
they stand in a column or as a figure; running prose keeps text figures.

The money block, in this order, always, on `surface.raised`:

| Line | Type | Colour |
|---|---|---|
| `Diária R$ 60,00 × 3 noites` | `type.caption` | `ink.secondary` |
| `Preço` / `Extra` | `type.body` | `ink.primary` |
| `Subtotal` | `type.body` | `ink.primary` |
| `Desconto 10%` | `type.body`, figure prefixed `−` | `ink.secondary` |
| `Total` | `type.body` **600** | `accent.strong` |
| `Pago` / `Em aberto` | `type.caption` | `ink.secondary` / `accent.strong` |

A flat-fee service omits the `Diária` line and the `Extra` row; it never omits
`Subtotal` or `Total`, so the block has the same shape for all four kinds.

---

## 8. Screens

Text margin 20 throughout. Page title `type.title`, first line of the screen, at
the margin, 8 below the bar. Bar 44 high, no fill, no hairline.

### 8.1 Login / Cadastro

Single group of fields, 20 inset. Field row: label `type.caption`
`ink.secondary` above, value `type.body` `ink.primary`, 12/12 padding, separator
between. Submit is a full-measure control: 52 high, radius 12,
`surface.control` + edge + ring, label `type.body` `ink.primary`. The secondary
route (`Criar uma conta`) is a `type.ui` line at the margin, `accent.strong`,
underlined 1px at 3px offset — not a second button.

No tab bar on either screen.

### 8.2 Agenda

Month/year filter: a 44-high control row inset 20, radius 12, `surface.control`
+ edge + ring, `‹ Agosto 2026 ›` — `type.body` `ink.primary`, chevrons 20px.
Not a dropdown: the app's popups do not scale with the design canvas, so the
month is stepped inline.

One group per dog. The group's first row is the **dog header**: name
`type.body`, `n serviços · R$ total` `type.caption`, trailing chevron rotated
(down = open, right = collapsed). Collapsed shows the header row only; the count
never hides, so a collapsed group still states what it holds.

Empty: `Nenhum serviço neste filtro.` — one sentence, `type.ui`
`ink.secondary`, at the margin, 20 below the filter. No illustration, no button.

### 8.3 Cachorros / Tutores

One group, one row per record, alphabetical. `+ Novo` is a 44-high control at
the top right of the title line — `type.ui` `ink.primary`, 16 horizontal
padding, radius 12, control fill + edge + ring.

Empty: `Nenhum cachorro cadastrado.` / `Nenhum tutor cadastrado.`

### 8.4 Serviços — the booking form

Kind selector: four cells in one 44-high group, radius 12, `surface.raised`, 1px
`stroke.raised` dividers. Selected cell = `accent.tint` fill, label
`accent.strong` **600**. Unselected = no fill, `ink.primary` 400.

Then, in one group: dog picker (row, pushes to an inline list — never a popup),
date mode (`Único` / `Período` / `Vários dias`, same four-cell pattern with
three cells), the date rows for that mode, and `Horários` — a sub-group of time
rows with `+ Adicionar horário` as its last row.

Then a second group: `Preço` or `Diária`, `Extra` (Hospedagem only),
`Desconto (%)`.

Summary line, `type.ui` `ink.secondary`, at the margin under the last group:
`3 serviços serão criados.` The number is literal.

### 8.5 Perfil

Photo 72 radius 36, name `type.title`, then `Nascimento` and `Pix` as two
`type.caption` lines under the name — not rows in a group. They are identity,
not settings, and they belong to the name the way a byline belongs to a title.

Income panel: `type.micro` label `AGOSTO DE 2026`, then the figure at
`type.title` in `accent.deep` — the one heaviest figure in the app — with a
24px eye glyph, `ink.primary`, as a 44 hit target to its right. Hidden state
replaces the figure with `R$ ••••••` at the same size, and the per-dog rows with
a single `type.ui` `ink.secondary` line: `Valores ocultos.`

Per-dog billing: one group, row = dog name + figure, tabular.

Counts: one `type.caption` line, `ink.secondary`, at the margin:
`5 cachorros · 4 tutores · 9 serviços em agosto`.

Backup group: `Exportar dados`, `Importar dados`, `Backup automático` with its
state as the caption. `Ajustes` is the **last row of the last group**, above
`Sair da conta`. `Sair da conta` is a `type.ui` line at the margin,
`accent.strong`, underlined — not a button, and not `danger`; signing out
destroys nothing. Version line `type.caption` `ink.disabled`… **no** —
`ink.secondary`; `ink.disabled` is non-text only. `versão 1.4.2`.

### 8.6 Detail screens

Back is a 44-high labelled control, top left, `‹ Cachorros` — `type.ui`
`ink.primary`, control fill + edge + ring, max width 200, tail truncation. Never
icon-only, never the word `Voltar` alone.

**Ficha do cachorro** — photo 96 radius 48 at the margin, name `type.title`,
breed `type.caption`. Groups: `TUTOR` (one row, pushes), `DESCRIÇÃO` (one row,
`type.body`, wrapping, no truncation), `SERVIÇOS` (the service row).
`Editar` and `Excluir` are the last two rows of a final group — `Excluir` in
`accent.strong`, because the palette has no `danger` and inventing one for this
pass is out of scope.

**Ficha do tutor** — contact group, dogs group, `CONTA` group with the bill by
month (row = month + figure), `Crédito` row, `Registrar pagamento` row,
`HISTÓRICO` group, and `Exportar resumo (PNG)` as a full-measure control.

**Ficha do serviço** — a single `QUANDO` row carrying the whole period
(`28/08, 09:00 → 31/08, 18:00`) rather than two rows, because the pair is one
fact; the money block (§7); then `SITUAÇÃO` — `Execução` and `Pagamento` as
rows whose trailing edge is the state word plus a chevron, not an embedded
control. `Adicionar ao calendário`, `Editar` and `Excluir` are the final
group. On a stay the `Extra` folds into the `Diária` caption
(`Diária R$ 60,00 × 3 noites · extra R$ 45,00`) so the priced line is the
subtotal.

All three detail screens are taller than one viewport. The artboards are drawn
at scroll-top, so the final group sits below the fold; the 96 bottom clearance
in §4 governs the **end of the scrollable content**, and no content is ever
drawn behind the bar.

### 8.7 New dog / New tutor

Field groups as §8.1. Photo picker is a row whose trailing edge is a 40 square,
radius 8, 1px `stroke.raised`, empty when unset — no glyph, no camera icon, no
`+`. Tutor picker is a row that expands **inline** to a list of tutor rows; it
is never a popup.

### 8.8 ConfirmDialog

Not a window. `scrim` over the current screen, dialog centred, 340 wide, radius
12, `surface.raised` fill, 1px `stroke.control`, **no ring** — over a scrim
there is no page colour to ring with, and the scrim is doing the separating.

Title `type.body` `ink.primary`. Body `type.ui` `ink.secondary`, wrapping.
Two 48-high cells side by side, divided 1px `stroke.raised`: `Não`
`ink.primary` 400, `Sim` `accent.strong` **600**. Alert form is one full-width
cell, `Entendi`.

Verbatim: `Excluir Toby?` · `As 6 fichas de serviço deste cachorro também serão
excluídas. Não é possível desfazer.` · `Não` / `Sim`.

### 8.9 Ajustes — the new screen

Pushed from the last row of Perfil's last group. Two sections.

**`APARÊNCIA`** — three cells in one 44-high group, same pattern as the kind
selector: `Claro` / `Escuro` / `Seguir o sistema`. A segmented group, not a
switch plus a dependent control: the three states are peers and one of them is
always true, which is what a segmented control means. A switch would make
"follow the system" the axis and light/dark the consequence, which is a lie —
the app has three settings, not a boolean with a rider.

Live preview under it: a group holding a title row, a body row and a caption
row, drawn in the chosen mode's own tokens, so the choice is visible without
leaving the screen. Label `type.micro`: `PRÉVIA`.

**`TAMANHO DO TEXTO`** — six-step slider, 44 tall, track 2px `stroke.raised`,
fill 2px `accent`, handle 24 radius 12 `surface.control` + `stroke.control`.
Six `A` specimens above the track at the six sizes, the current one
`ink.primary`, the rest `ink.secondary`.

Value line, `type.caption`: `Grande — corpo 20px` on the leading edge,
`3 de 6` on the trailing edge. **px, written out.**

`Seguir o tamanho do sistema` — a switch row, default **on**. While on:
the slider is **disabled** — track, fill, specimens and handle all
`ink.disabled` / `stroke.raised`, no accent — but it still shows where the
system has put the ramp, and the value line reads
`Seguindo o sistema — corpo 20px`. Tapping it does nothing. Turning the switch
off makes it live at that same step.

Live preview paragraph at the current size, `type.body`, in a group, with a
`type.caption` line: `Prévia no tamanho atual`.

---

## 9. The exported report (PNG)

A document, not a screen: no bar, no tab bar, no controls. 412 wide, height
follows content.

| Block | Spec |
|---|---|
| Header | `type.micro` `RESUMO DE SERVIÇOS`; `type.title` tutor name; `type.caption` `Agosto de 2026`; 1px `stroke.subtle` rule at full measure |
| Contact | two `type.caption` lines, `ink.secondary` |
| Table head | `type.micro`, `ink.secondary`, 1px rule under |
| Columns | `Cachorro / Tipo / Data / Valor / Execução / Pagamento` |
| Row | `type.caption`; `Valor` and the two state columns right-aligned; 1px `stroke.subtle` between rows |
| Small print | a `type.caption` `ink.secondary` second line under a row for a stay's nights and any discount |
| Month total | `type.body` **600** `accent.strong`, above a 1px rule |
| Payment block | `type.micro` `PAGAMENTO`; Pix key `type.body`, tabular; `type.caption` note |
| Footer | `type.caption` `ink.secondary`, generation date |

The table is the one place in the design with no group fill: a document is not a
screen, and a bordered card inside an exported image reads as a screenshot of an
app rather than as a statement of account.

---

## 10. Motion

Push and pop are the platform's, unmodified. Expanding a dog group, stepping the
month, toggling the money eye, and switching kind all happen at **0 ms** —
nothing animates, nothing fades, nothing crossfades. The scrim under a dialog
appears at 0 ms. Nothing on any screen animates on load or before the person
acts.

---

## 11. Copy, verbatim

`Tutor` · `Passeio` · `Pet sitting` · `Creche` · `Hospedagem` ·
`A executar` · `Feito` · `Sem pagar` · `Pago` · `Sim` · `Não` ·
`+ Novo` · `Editar` · `Excluir` · `Ajustes` · `Sair da conta` ·
`Exportar resumo (PNG)` · `Registrar pagamento` · `Adicionar ao calendário` ·
`Backup automático` · `Seguir o sistema` · `Seguir o tamanho do sistema`

Empty states, one sentence each, at the text margin:

- `Nenhum serviço neste filtro.`
- `Nenhum cachorro cadastrado.`
- `Nenhum tutor cadastrado.`
- `Nenhum pagamento registrado.`
- `Valores ocultos.`

Summary and confirmation:

- `3 serviços serão criados.`
- `Excluir Toby?` / `As 6 fichas de serviço deste cachorro também serão excluídas. Não é possível desfazer.`
- `Substituir o arquivo existente?`
- `O backup automático está desligado. Ligar agora?`

All sentence case, statements of fact, no exclamation mark, no second-person
imperative.
