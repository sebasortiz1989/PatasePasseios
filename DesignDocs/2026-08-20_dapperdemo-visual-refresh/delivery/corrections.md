# Corrections to the delivery

20 Aug 2026, from reviewing the delivery against the app. **The app is right in every
case below; the design documents are wrong.** Nothing here is a change to make in
DapperDemo — these are the places where the build sheet must not be followed literally.

Not sent back to Claude Design. The corrections are small and the port carries them.

---

## 1. The tab bar — order and two labels

`build-sheet.md` §5.1 and §5.2, and the canvas, draw:

> Agenda · Cachorros · Serviços · Tutores · Perfil

The app is (`MainView.axaml`, `Tag=` on each `RadioButton`, columns 0–4):

> **Cães · Tutores · Agenda · Novo · Perfil**

Two errors:

- **Agenda is the centre tab, not the first.** Column 2 of 5. It is also the default —
  `MainViewModel.OnRunStarting` calls `HomeViewCommand`. Moving it to the first position
  takes the app's home off the thumb position and breaks the brief's own "do not re-order
  navigation".
- **`Cachorros` and `Serviços` are screen headings, not tab labels.** The tabs read
  `Cães` and `Novo`. The delivery uses the screen names in the bar; the strings `Cães`
  and `Novo` appear nowhere on the canvas.

Cause: the brief gave the order twice, in two different orders, and both were wrong.
`decisions.md` §10.1 flags the contradiction and asks to be told which is live — this is
the answer. Nothing was misread.

**When porting:** keep the app's order and `Tag` values. Take the geometry, the fill, the
ring, the 24px stroked glyphs and the active-cell treatment from §5.1; take nothing from
it about which cell is where. The icon table in §5.2 needs its rows re-paired to the
right tabs.

## 2. The booking screen's heading is `Agendar`

`build-sheet.md` §8.4 is titled "Serviços — the booking form". That is a document section
name, but `Agendar` appears nowhere on the canvas, so the screen heading was likely drawn
from the section title. `ServicesView.axaml` reads **`Agendar`**.

Same class of error as §1: tab label `Novo`, screen heading `Agendar`, and the word
`Serviços` is neither.

For completeness, the real headings: Agenda / Cachorros / Tutores / **Agendar** /
**Conta**.

## 3. Copy that is not verbatim

`build-sheet.md` §11 is headed *Copy, verbatim*. Three strings are not:

| Build sheet | The app |
|---|---|
| `Nenhum cachorro cadastrado.` | `Nenhum cachorro cadastrado ainda.` |
| `Nenhum tutor cadastrado.` | `Nenhum tutor cadastrado ainda.` |
| `Nenhum pagamento registrado.` | `Nenhum pagamento neste período.` |

The canvas only draws `Nenhum serviço neste filtro.`, which is correct. The app also has
`Nenhum serviço neste período.` on the detail screens, which §11 does not list.

**When porting:** take the existing strings out of the `.axaml`, not out of §11.

## 4. One wrong number in the contrast table

`contrast-table.md` §3.1:

| Pair | Claimed | Actual |
|---|---:|---:|
| dark `ink.disabled` `#6F6B66` on page `#1B1A18` | 2.63:1 | **3.29:1** |

Recomputed from the hexes (WCAG 2.1 relative luminance). Every other pair in the document
was checked and matches to two decimals — 21 of 22, including the 4.55:1 that the whole
65% → 70% argument depends on. This one is an isolated slip.

**No design consequence.** Still below 4.5:1, still disabled-exempt under 1.4.3, still
used on one inert control. It does mean the value clears the 3:1 non-text floor, which the
document's framing does not expect.

## 5. Not a correction — a decision to make

`decisions.md` §9 removes information from list rows:

- **Dog row** loses the description snippet and the next service.
- **Tutor row** loses the phone number and the address.
- **Service row** inside a dog group loses the dog and the tutor.

The reasoning is sound and it is presentation rather than function, so it is arguably
inside the brief. But it is information the sitter sees today, and the brief did say not
to change what the app does. **Decide this deliberately before porting the list screens**
rather than discovering it in the diff.
