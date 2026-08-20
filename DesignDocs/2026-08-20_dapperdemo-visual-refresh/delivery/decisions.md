# DapperDemo — decisions

21 Aug 2026. Why the refresh is shaped the way it is, and what was rejected.
This is the file to argue with.

---

## 1. What the port actually is

Nib's rules that transferred without argument: grouped rows on a raised surface,
the section label outside the group, separators inset to the text edge and never
at a group's ends, no shadow or gradient or blur anywhere, two type weights, one
base size driving a six-step ramp, stroked icons following the foreground, rows
that answer one question, one-sentence empty states.

Nib's rules that did **not** transfer, and what replaced them, are §5–§7.

The single change that makes the app look different is the group. DapperDemo is
full-bleed rows on hairlines today; the same rows inset 20 on `surface.raised`
at radius 12 read as objects rather than as a ruled page. Nothing else in this
pass is worth as much.

---

## 2. Deriving the dark palette: explicit values, not re-based alphas

**Decision — every dark ink is an explicit hex.**

The alternative was to keep the alpha authoring and re-base it: `ink.secondary`
becomes *light ink at 65% over the dark page* instead of *dark ink at 65% over
the light page*. It is one line of code and it is wrong twice.

First, it does not hold across surfaces. An alpha composites against whatever is
behind it, so `ink.secondary` on the page and `ink.secondary` inside a group are
different colours — in light mode they measure 4.90:1 and 4.52:1, which is
survivable because both pass. Re-based over the three dark surfaces the same
alpha lands on three values whose ratios are 6.71, 5.90 and 5.19. Also all
passing, also all different, and now there are six numbers to hold instead of
two. Explicit hex means `ink.secondary` is one colour and I check it three
times, rather than three colours I have to derive before I can check them.

Second, alpha loses the warmth. `#201F1D` is warm-neutral — red is 3 above blue.
Composite it 65% over a light grey and that cast survives. Composite a *light*
ink over `#1B1A18` and the result trends neutral, because the ground is doing
most of the work and the ground's own cast is only 3 points wide. The greys go
cold, which is the one thing a warm-grey-and-gold app cannot afford. Explicit
values keep 2–5 points of red-over-blue at every step.

**What the dark ground is.** `#1B1A18`, one step below `ink.primary`'s
`#201F1D` and in the same hue. Not black: a gold accent on true black reads as a
warning light. Not `#201F1D` itself: the page and the light mode's body text
should not be the same value, or a screenshot of one mode looks like a bug in
the other.

### 2.1 The light raised surface is not the one in the token set

**Decision — `surface.raised` is `#E6E5E5`, not the `#EAE9E9` the token set
carries.**

`#EAE9E9` against `#F3F2F2` is 1.084:1, which is a useless number for a
decorative pair. The useful one is perceptual lightness: **ΔL\* 3.14**. Nib's own
page→raised step — the thing being ported — is **ΔL\* 4.48**, on a lighter ground
where a step of any size reads more easily. So DapperDemo's given pair is about
70% of the separation, in worse conditions.

At ΔL\* 3 a large flat area is at the threshold of noticeability: visible on a
calibrated screen in a dim room, marginal on a phone outdoors, and gone entirely
under a warm-display or night filter. Since the group *is* the change this pass
makes, a group that only sometimes reads as a group is not worth making.

`#E6E5E5` is ΔL\* 4.54 from the page — Nib's step, in DapperDemo's hue, keeping
the family's 1 point of red over blue (243/242/242 → 230/229/229).
`surface.control` follows one more step down at `#D9D8D8` (ΔL\* 4.61), and the two
hairlines re-composite over their new surfaces: `stroke.raised` `#C6C5C5`,
`stroke.control` `#BBBABA`.

**This is free.** `ColorSurface` has 2 uses in 1 file, so nothing in the app
depends on the old value. It is a choice, not an inheritance, and it was worth
spending.

**It also forced the ink.** On the darker group surface the 65% step measures
4.36:1 and fails; the 70% step — the app's most-used muted ink, 33 uses — clears
all three surfaces. The full chain is in the ink-mapping document §4.

**Rejected: darkening the page instead.** Moving `#F3F2F2` down would give the
same ΔL\* for free, and `ColorBg` has 32 uses across 15 screens. The page is the
one value in the palette that is genuinely load-bearing; the surface with two
uses is the one to move.

**Rejected: inverting the ramp mechanically.** `surface.raised` is 9 points
below the page in light (`#F3F2F2` → `#EAE9E9`). Inverted that is 9 points
*above* `#1B1A18` — `#242321`, which is invisible against the page on an OLED
screen at low brightness. Dark mode needs a wider step, so raised is 11 points
up at `#262523` and control is 21 up at `#302E2B`. Perceptual steps, not
arithmetic ones.

---

## 3. The accent's dark counterpart — and why the brief's reason is not the reason

**Decision — `accent` becomes `#D9A857` in dark mode.**

The brief predicted `#B68235` would fall short of 4.5:1 against a dark page. It
does not: on `#1B1A18` it measures **5.16:1** and passes. On `surface.raised` it
measures 4.55:1 and passes by five hundredths. On `surface.control` it measures
**3.99:1** and fails.

That last row is the one that matters, because `surface.control` is the tab bar,
and the active tab is the most frequent accent in the app. An accent that passes
on the page and fails on the one surface it is always on is not usable, so the
counterpart is needed — for a different reason than the brief gave, and the
reason should be recorded rather than quietly accepted.

There is a second argument the numbers do not show. `#B68235` is a mid-tone. On
`#F3F2F2` it reads as gold because the ground is lighter than it is. On
`#1B1A18` it is the lightest thing in its own region of the screen and it reads
as **brown** — the hue is identical and the impression is not. `#D9A857` is the
same hue family two steps lighter and reads as the same *material* in both
modes, which is what "the app's tint" has to mean.

`#D9A857` measures 8.03 / 7.06 / 6.21 against page, raised and control. It has
room, which the dark accent needs: dark mode is where the accent ends up on
whichever surface a control happens to sit over.

**Rejected: keeping `#B68235` in both modes and moving the active tab off the
control surface.** It would mean the tab bar is not a control in dark mode, or
that the active tab is indicated some other way in dark mode only. Two modes of
one component is worse than two hexes of one token.

**Rejected: desaturating the accent for dark.** A duller gold at the same
lightness would pass the same checks and would stop being the brand.

**Direction reverses for the emphasis steps.** In light mode `accent.strong` and
`accent.deep` go *darker* than `accent` (`#7D5411`, `#5A3B0A`) because the
ground is light. In dark mode they go *lighter* (`#EFCE93`, `#F7E4BE`). "Strong"
means further from the ground, not lower in lightness — worth stating because
the token name implies the wrong thing in one of the two modes.

---

## 4. Which of the six muted inks survive: two, plus one that is not text

*Superseded in part: the base step moved from 65% to 70% once `surface.raised`
changed. The reasoning below stands; the number is in the ink-mapping document,
which is authoritative for all six steps and their use counts.*

**Decision — `ink.secondary` is the 70% step. `ink.disabled` is the 45% step and
may not carry text. The 50%, 55%, 70% and 75% steps are deleted.**

The app has six muted steps and uses 55% for captions and 45% for hints. Both
fail:

| Step | On the page | Verdict |
|---|---|---|
| 45% `#949392` | 2.74:1 | fails 4.5 **and** 3 |
| 55% `#7F7E7D` | 3.62:1 | fails 4.5 |
| 65% `#6A6968` | 4.90:1 | passes |
| 75% `#555452` | 6.77:1 | passes |

55% is the caption colour and captions are the second line of every row in the
app — the single most-read secondary text there is, at 13px, at 3.62:1. That is
not a rounding problem. 65% is the lightest step that passes, and it passes on
`surface.raised` too (4.52:1), which it has to, because that is where the rows
live.

45% cannot be text at any size: 2.74:1 fails even the 3:1 large-text floor. Its
job in the app — hints and small print — is gone, and those strings move to
`ink.secondary`. What is left is the one thing a failing contrast is *correct*
for: **the disabled state.** Dimming is the disabled affordance, disabled
controls are exempt, and darkening it would delete the only signal the control
is inert. So 45% is renamed `ink.disabled`, appears on exactly one control — the
text-size slider while the system switch is on — and is barred from text.

75% passes and is deleted anyway. It sits between `ink.primary` at 14.73 and
`ink.secondary` at 4.90 and there is nothing for it to say. Three greys is one
more than the design has meanings for: *this is the answer* and *this is about
the answer*. A third step invites "slightly less important than the caption",
which is not a real category and is where muted-ink ramps go to die.

**Rejected: keeping 55% for large text only.** `type.title` is the only step
over 24px and titles are never muted. The exemption would have no members.

---

## 5. What the tab bar becomes

**Decision — the tab bar becomes a floating control group: 372 × 56, inset 20
each side, 20 above the safe area, radius 12, `surface.control` + 1px
`stroke.control` + the 3px ring of page colour. Content scrolls under it.**

The brief said apply Nib's surface and edge discipline *to* the tab bar, do not
delete it, and argue for any floating-control pattern rather than assuming it.
This is the argument.

A full-bleed bar with a hairline top edge is the one piece of chrome that
contradicts everything else the refresh does. After this pass, every list on
every screen is an inset object on a raised surface with a 12 radius; a bar
welded to the bottom two edges of the screen is the only element left that
touches the screen edge. It would read as a leftover, and it would be the first
thing anyone noticed.

Detaching it also fixes something real. A welded bar has to be opaque and it has
to own its band of the screen, so content stops 100 units above the bottom and
that dead band is visible on every screen. A floating bar lets the last group
scroll under it, so the page is 412 × the full height and the bar is an object on
top of it — which is what the grouped-row rule already claims about everything
else.

**What it costs.** The bar becomes the app's only `surface.control`, so the role
exists to serve one component on five screens. That is a fair objection and the
answer is that it does not: the same fill, edge and ring are on the month
stepper, the `+ Novo` control, the back control, the submit controls and the
slider handle. The tab bar is the largest user of a role that seven components
share.

**Rejected: floating corner controls instead of the bar.** Nib can do that
because it is stack-based with one home. Five peer destinations cannot be corner
controls — there are no corners for the fourth and fifth, and the whole point of
a tab bar is that all five are visible and one is marked.

**Rejected: keeping the bar welded and giving it the raised fill.** Half a port.
It would still be the only element touching the screen edge, and `surface.raised`
against `surface.primary` is 1.09:1 — with no ring and no stroke, an invisible
change.

**Rejected: a fill or an indicator bar behind the active cell.** The design's
active state everywhere else is colour plus weight. `accent` at 2.53:1 on
`surface.control` cannot be a fill under `ink.primary` anyway, and
`accent.tint` on a control surface would be a third fill value inside one
component.

**Rejected: hiding the bar on scroll.** Nothing in the design moves before the
person does.

**Rejected: dropping the bar on pushed screens.** One rule, stated once: the bar
is on every screen inside the tabbed shell — pushed detail screens, pushed forms,
Ajustes, and behind a dialog's scrim. A pushed screen in a tab-based app has not
left the tab, and hiding the bar would turn every entry into an exit from
navigation. It is absent in exactly three places, none of which is inside the
shell: Login, Cadastro (there is nowhere to go yet) and the exported report
(not a screen). The alternative — bar on details, no bar on forms, on the theory
that a half-filled form should not offer five ways to abandon itself — is a real
argument and it loses to the confirm dialog, which is what actually protects
unsaved work.

---

## 6. Where Ajustes lives

**Decision — the last row of the last group on Perfil, above `Sair da conta`,
pushed.**

Nib puts settings in the top-right pill on every screen. DapperDemo has no
top-right chrome slot and inventing one is the wrong trade: a permanent floating
control on all five tabs, forever, for a screen a person opens twice — once to
set the text size and once when they change their mind about dark mode.

A sixth tab is out: the brief locks the count at five, and it would be the least
used of the six by two orders of magnitude.

Perfil already *is* this screen's neighbourhood. It holds the sitter's own
identity, the Pix key, the backup block and the version line — the app talking
about itself rather than about dogs. Theme and text size are the same kind of
fact. Putting them anywhere else would mean Perfil holds four app-level settings
and Ajustes holds two.

Position within Perfil matters: **last row of the last group, above
`Sair da conta`**. Not first — a settings row at the top of Perfil is the first
thing the eye lands on and it is not what the tab is for. Two taps from
anywhere, which is what Nib pays to reach Opens.

**Rejected: merging Ajustes into Perfil as two more groups.** Theme has a live
preview and text size has a slider with a dependent disabled state; both are
tall, and both would push the income panel — the thing the sitter actually opens
Perfil for — below the fold.

---

## 7. Appearance: a segmented control, not a switch

**Decision — `Claro` / `Escuro` / `Seguir o sistema` as three cells of one
segmented group.**

Nib uses a switch plus a disabled dependent control for text size, and the brief
offers that as one option for theme. It is the wrong shape here. A switch means
one boolean is the truth and everything else follows from it. Theme has three
peer states, exactly one of which is true. A switch labelled "follow the system"
plus a light/dark control that greys out when it is on encodes the same three
states in two controls, and it makes the user reason about which control is in
charge before they can answer a one-tap question.

Text size keeps the switch-plus-disabled-slider pattern, and the difference is
the point: there, the system value is a *number on a continuum* the app can show
you inside its own control. The disabled slider is not a dependent control, it is
a **readout** — it shows where the system has put the ramp. Theme has nothing to
read out; "the system says dark" is a word, and the word fits in a cell.

So the two controls differ because the two settings differ, and each one is
recorded here rather than being made consistent for its own sake.

---

## 8. The state words, and the one that takes colour

**Decision — `Sem pagar` is `accent.strong`. `A executar`, `Feito` and `Pago`
are `ink.secondary`. None of them is a pill, a dot or a badge.**

Four states across two axes on every service row. Coloured chips for all four
would put two coloured objects on every row of the busiest screen, and the app
would be answering a question nobody asked — *what are all the states this thing
could be in* — instead of the one it exists for.

Money owed is why the sitter opens the app. So one state takes colour, and it is
the one where something is required of somebody. `accent.strong` at 5.50:1 on
the group surface, at caption size, inline in the row's second line.

**Rejected: green for `Pago`.** The palette is one accent and a neutral ramp.
Adding a semantic green would be a second hue for the least urgent fact on the
row.

**Rejected: `danger` for `Sem pagar`.** There is no `danger` in this palette
and an unpaid booking is not an error.

**Rejected: chips.** A pill at caption size is 24 tall and the row is 52; two of
them per row and the group becomes a table of badges.

---

## 9. What each row stopped showing

**Dog row** — was name, photo, tutor, breed, description snippet, next service.
Now name, breed, tutor, photo. The question is *is this the dog I mean*; the
description is why you tap through, not how you choose, and the next service is
answered better by Agenda, which is a tab away and sorted for exactly that.

**Tutor row** — was name, phone, neighbourhood, dog count, address. Now name,
neighbourhood, dog count. The phone number is a nine-digit string that reads as
noise in a list you are scanning by name, and it is the first thing on the detail
screen.

**Service row inside a dog group** — was kind, dog, tutor, date, time, price,
two states. Now kind, date, time, price, two states. The dog is the group header
and the tutor is a property of the dog: printing both on every row repeats the
group label as many times as the group has rows.

**Rejected: cutting the price from the service row.** It was the closest call.
Price is arguably detail, and the row would drop to one line without it. It
stays because `Sem pagar` without an amount is half a fact — the sitter is
scanning for what she is owed, and *how much* is the other half.

---

## 10. Known contradictions in the brief, and how they were read

1. **Tab order.** §3 gives the bar as `Agenda · Cachorros · Serviços · Tutores ·
   Perfil`. §4 lists the tab screens in the order Agenda, Cachorros, Tutores,
   Serviços, Perfil. Read as: §3 describes the bar and §4 describes a document,
   so the bar is §3's order, and §6's "do not re-order navigation" binds to it.
   Flag it — if §4 is the live order, it is one array.

2. **"Keep the palette exactly" vs "give a contrast table against 4.5:1".** The
   light values are authoritative and two of them fail. Read as: the *hexes* are
   locked, the *alpha steps* are not — §2 explicitly invites consolidating them.
   No hex in the brief's table was altered. `ink.secondary` changed which alpha
   step it names, which is the one degree of freedom offered.

3. **"Do not change what the app does" vs adding Settings.** Read as: no change
   to the pet-sitting features. Theme and text size are not features of a
   pet-sitting business, and the brief commissions the screen itself.

4. **"Never a fixed root height" vs the artboards.** The canvas draws each screen
   at 412 × 892 because an artboard has to end somewhere. Every screen is
   authored to stretch; the frame is the drawing's edge, not the layout's.

---

## 11. Rules broken knowingly

1. **"No fill on chrome — separation is hairline and space."** A floating control
   over a scrolling list must be opaque or content runs through it. Broken the
   way Nib broke it, with two mitigations: the fill is `surface.control`, one
   value off the page, and the boundary is carried by the 3px ring of page colour
   plus the 1px stroke rather than by the fill's own contrast. No grey band, no
   translucency, no elevation.

2. **"Every group gets the raised fill."** The exported report has no group
   fills. A bordered card inside a PNG the tutor receives by message reads as a
   screenshot of an app; a statement of account should read as a document. Rules,
   space and tabular figures carry it instead.

3. **"Labels on a control are `ink.primary`."** Inherited from Nib v3 §0.1
   without argument — light `ink.secondary` on `surface.control` is 4.12:1 and
   fails. Applied in dark mode too, where it measures 5.19:1 and passes, so the
   rule does not have to be remembered per mode.

4. **`Excluir` in `accent.strong`.** Destructive actions want `danger` and this
   palette has none. Adding a red is a token release and a colour decision the
   brief did not ask for. The confirm dialog is doing the real work of preventing
   the accident; the row colour is not the safeguard.

---

## 12. Still open

- **The fourth service kind's price shape.** `Creche` is drawn as a flat fee.
  If day care is ever billed per period it needs the `Hospedagem` money block,
  and the form's kind selector would drive two shapes rather than one flag.
- **`Vários dias` in the booking form.** Drawn as a row that expands inline to a
  list of chosen dates, because popups do not scale with the app's canvas. A
  calendar grid would be better and is a bigger component than this pass should
  invent.
- **The report's paper size.** Drawn at 412 wide, the phone measure, on the
  assumption it is shared as an image in a message. If it is ever printed it
  wants a real page size and a different measure.
- **Dark mode for the report.** Drawn in both modes here, but an exported PNG
  probably should not follow the app's theme — the tutor receiving it did not
  choose it. Argued in neither direction yet.
