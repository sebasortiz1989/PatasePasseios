# DapperDemo — type mapping

21 Aug 2026. All **26 hardcoded `FontSize` values / 200 uses** assigned to a role.
Every row accounted for; no residue.

App values are on the 720-wide canvas. Design px = app px ÷ 1.7476 ÷ 1.1 (the
text-sized factor) = **÷ 1.92236**. The "design px" column is what the app value
*already was* in my units — the "role px" column is what it becomes.

---

## 1. The ramp grew by two roles, and the inventory is why

The brief's ramp had five roles: body, title, ui, caption, micro. The inventory
does not fit into five.

- **Six values from 97 down to 66 convert to 34–50 design px.** `type.title` at
  the default step is 30. Collapsing 50 px into 30 px is not a drift correction,
  it is a redesign of the Login brand line and the Perfil name. Those two are
  display type and get `type.display`.
- **Six values from 53 down to 40 convert to 21–28 design px** — 20 uses across
  six screens, all `FontHeading`, none of them a screen title and none of them
  body. That is a section heading, and there was no role for it. `type.section`.

So: **seven roles, still two weights.** Cormorant carries display, title and
section, always at 400. Lora carries body, ui, caption and micro; 600 is Lora
only and still appears in exactly five places.

| Role | Face | Default px | Ratio to body |
|---|---|---:|---|
| `type.display` | Cormorant 400 | 42 | × 2.35 |
| `type.title` | Cormorant 400 | 30 | × 1.65 |
| `type.section` | Cormorant 400 | 22 | × 1.22 |
| `type.body` | Lora 400 | 18 | — |
| `type.ui` | Lora 400 | 15 | × 0.85 |
| `type.caption` | Lora 400 | 13 | × 0.74 |
| `type.micro` | Lora 600 | 11 | × 0.62 |

---

## 2. The mapping — all 26

| App px | Design px | Uses | New role | Role px | Note |
|---:|---:|---:|---|---:|---|
| 97 | 50.5 | 1 | `type.display` | 42 | UsersView — the profile name |
| 87 | 45.3 | 1 | `type.display` | 42 | LoginView — the brand line |
| 78 | 40.6 | 3 | `type.display` | 42 | rounds **up**; nearest role |
| 74 | 38.5 | 3 | `type.display` | 42 | see §3.1 — the closest call |
| 68 | 35.4 | 8 | `type.title` | 30 | biggest title cluster |
| 66 | 34.3 | 1 | `type.title` | 30 | |
| 62 | 32.3 | 1 | `type.title` | 30 | |
| 53 | 27.6 | 1 | `type.section` | 22 | |
| 51 | 26.5 | 5 | `type.section` | 22 | |
| 45 | 23.4 | 4 | `type.section` | 22 | |
| 44 | 22.9 | 3 | `type.section` | 22 | was `Medium` — see §3.3 |
| 41 | 21.3 | 3 | `type.section` | 22 | was `Medium` |
| 40 | 20.8 | 4 | `type.section` | 22 | was `Medium` |
| 38 | 19.8 | 2 | `type.section` | 22 | rounds up; `FontHeading` |
| 37 | 19.2 | 6 | `type.section` | 22 | rounds up; `FontHeading` |
| 36 | 18.7 | 3 | `type.section` | 22 | rounds up; `FontHeading`, `Medium` |
| 34 | 17.7 | 21 | `type.body` | 18 | `ColorText` — the real body |
| 32 | 16.6 | 31 | `type.body` | 18 | `ColorText` + muted; see §3.2 |
| 31 | 16.1 | 13 | `type.ui` | 15 | all-muted, incl. `CheckBox` |
| 30 | 15.6 | 15 | `type.ui` | 15 | exact match |
| 29 | 15.1 | 10 | `type.caption` | 13 | all-muted, italic |
| 28 | 14.6 | 39 | `type.caption` | 13 | all-muted; see §3.2 |
| 26 | 13.5 | 6 | `type.caption` | 13 | |
| 25 | 13.0 | 14 | `type.caption` | 13 | exact match |
| 22 | 11.4 | 1 | `type.micro` | 11 | |
| 19 | 9.9 | 1 | `type.micro` | 11 | `ClassicalTheme` default |

**Totals:** display 8 · title 10 · section 31 · body 52 · ui 28 · caption 69 ·
micro 2 = **200**.

---

## 3. The four judgement calls, named

### 3.1 `74` → display, not title

38.5 design px sits almost exactly between `title` 30 and `display` 42, so the
number does not decide it. The screens do: DogDetailView, TutorDetailView and
SignUpView, one use each — the dog's name, the tutor's name, the sign-up
heading. Those are the first line of the screen with nothing above them, which
is what display means here. **If you disagree, this is the one row to move**;
moving it changes three strings and nothing else.

### 3.2 The 28–34 cluster splits three ways, and it splits by colour

The brief calls 28, 29, 30, 31, 32 and 34 "one body role that drifted". 119 of
200 uses. But 34 → 17.7 and 28 → 14.6 are two roles apart in my ramp, so the
cluster cannot land in one place. The number will not decide it either — the
**colour token** already recorded the intent:

| App px | Colours in the inventory | Reading |
|---:|---|---|
| 34 | `ColorText`, `ColorAccent` | primary text and inputs → **body** |
| 32 | `ColorText`, muted45, muted70 | mostly primary → **body** |
| 31 | muted65, muted55, `ColorAccentDark800` | all secondary → **ui** |
| 30 | muted65, `ColorText`, muted45 | mostly secondary → **ui** |
| 29 | muted65, muted55, muted70 | all secondary, italic → **caption** |
| 28 | muted70, muted55, muted50 | all secondary → **caption** |

A value drawn in a muted ink was never body, whatever its size said. That rule
resolves all six rows without a per-instance list.

### 3.3 `Medium` weight disappears

40, 41, 44 and 36 are `FontHeading` at `FontWeight="Medium"` — 13 uses. The
design has two weights and Cormorant is always 400. All four become
`type.section` at 400. The size step (from ~21 to 22) is doing the work the
weight was doing, and Cormorant at 22/400 on a quiet page has more presence than
Cormorant at 21/500 — this is the "the bigger the text, the lighter it sets"
rule paying for itself.

### 3.4 Italic disappears too

32, 31, 30, 29, 26 and 25 carry `FontStyle="Italic"` in places — roughly 60 of
the 200. **No interface text in this design is italic.** Italic in Lora at 13–15
px on a warm grey is a legibility cost paid for emphasis that the muted ink
already provides. Italic survives only as body-copy emphasis, and no screen here
has body copy long enough to need it.

---

## 4. What does not scale

Every number above is a **default-step** value. The six steps are in the build
sheet §2.3. Margins (20), hit targets (44), the tab bar (56), group radius (12)
and row padding (12/12/16/16) do **not** scale with the ramp — they are not in
the table and must not be derived from it.

**Every value in this document is px. There is no pt anywhere.**
