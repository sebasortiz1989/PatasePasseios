# Follow-up — inventories from the existing app

Send this as a second message in the same Claude Design session, after the main brief.

It **adds evidence, and changes no requirement.** Everything already asked for still stands.
I counted what the app actually uses today, and three of the numbers should change how you
decide things I left open.

> If the brief you already have contains a section called "The type inventory you are
> replacing", skip Part A — you have it. Part B is new either way.

---

## Part A — the 200 font sizes

The app has **200 hardcoded `FontSize` declarations across 26 distinct values**, over 13
`.axaml` files. A user-settable size ramp means none of them can stay a literal, so the 26
have to collapse into a handful of named roles.

**The clustering is accidental, not deliberate.** Five values between 28 and 34 account for
**119 of the 200 uses** — one body role that drifted apart a pixel at a time. At the other
end, 97, 87, 78, 74, 66 and 62 are almost all one-offs on a single screen each.

`FontHeading` is the serif display face, `FontBody` the serif text face.

| px | uses | element | font | colour | style | seen in |
|---:|---:|---|---|---|---|---|
| 97 | 1 | TextBlock | FontHeading | ColorText | — | UsersView |
| 87 | 1 | TextBlock | — | — | Normal | LoginView |
| 78 | 3 | TextBlock | FontHeading | ColorAccentDark700, ColorTextMuted70 | — | UsersView, DogDetailView, TutorDetailView |
| 74 | 3 | TextBlock | — | — | — | DogDetailView, TutorDetailView, SignUpView |
| 68 | 8 | TextBlock | — | — | — | ServicesView, UsersView, TutorsView |
| 66 | 1 | TextBlock | FontHeading | ColorAccentDark700 | — | SignUpView |
| 62 | 1 | TextBlock | FontHeading | ColorAccentDark700 | — | ServiceDetailView |
| 53 | 1 | TextBlock | FontHeading | ColorAccentDark700 | — | ServiceDetailView |
| 51 | 5 | TextBlock | FontHeading | ColorText, ColorAccentDark700 | — | UsersView, AgendaView |
| 45 | 4 | TextBlock | FontHeading | ColorAccentDark700, ColorText | — | ServiceDetailView, TutorDetailView |
| 44 | 3 | TextBlock | FontHeading | ColorText | Medium | TutorsView, DogsView, AgendaView |
| 41 | 3 | TextBlock | FontHeading | ColorText | Medium | UsersView, AgendaView |
| 40 | 4 | TextBlock | FontHeading | ColorText | Medium | UsersView |
| 38 | 2 | TextBlock | FontHeading | ColorTextMuted70, ColorAccent | — | TutorDetailView |
| 37 | 6 | TextBlock | FontHeading | ColorAccentDark800, ColorAccent, ColorTextMuted70 | — | UsersView, TutorsView, DogsView |
| 36 | 3 | TextBlock | FontHeading | ColorText, ColorAccentDark700 | Medium | TutorDetailView, DogDetailView |
| 34 | 21 | TextBlock, TextBox | FontBody | ColorText, ColorAccent | — | ServiceDetailView, ServicesView, NewDogView |
| 32 | 31 | TextBlock, CalendarDatePicker | FontBody | ColorText, ColorTextMuted45, ColorTextMuted70 | Italic | UsersView, ServicesView, ServiceDetailView |
| 31 | 13 | TextBlock, CheckBox | FontBody | ColorTextMuted65, ColorAccentDark800, ColorTextMuted55 | Italic | UsersView, ServiceDetailView, ServicesView |
| 30 | 15 | TextBlock, CheckBox | FontBody | ColorTextMuted65, ColorText, ColorTextMuted45 | Italic | DogDetailView, TutorDetailView, UsersView |
| 29 | 10 | TextBlock | FontBody | ColorTextMuted65, ColorTextMuted55, ColorTextMuted70 | Italic | UsersView, TutorDetailView, ServiceDetailView |
| 28 | 39 | TextBlock | FontBody | ColorTextMuted70, ColorTextMuted55, ColorTextMuted50 | — | ServiceDetailView, ServicesView, NewDogView |
| 26 | 6 | TextBlock | FontBody | ColorTextMuted55, ColorTextMuted70, ColorAccentDark700 | Italic | TutorDetailView, UsersView, DogDetailView |
| 25 | 14 | TextBlock | FontBody | ColorTextMuted45, ColorAccent | Italic | UsersView, ServiceDetailView, TutorDetailView |
| 22 | 1 | TextBlock | FontBody | ColorTextMuted50 | — | AgendaView |
| 19 | 1 | TextBlock | FontBody | — | — | ClassicalTheme |

These are on the app's 720-wide canvas: divide by **1.7476** for your px, and by a further
**1.1** for anything text-sized. A `34` here is about `18px` in your units, a `28` about `15px`.

### What I need back

An explicit mapping, **every one of the 26 → its new role**, at the default step:

| Current px | Uses | New role | New px |
|---:|---:|---|---:|
| 34 | 21 | `type.body` | 17 |
| 28 | 39 | `type.ui` | 15 |
| … | | | |

Every row accounted for — no "and the rest map sensibly". Where two values collapse into one
role, say so; where one value has to split because it is doing two jobs, name which screens
take which.

---

## Part B — what the palette is actually used for

Every `StaticResource` colour reference in the app, counted:

| token | value | uses | screens |
|---|---|---:|---:|
| `ColorDivider` | ink 16% | 87 | 14 |
| `ColorText` | #201F1D | 68 | 12 |
| `ColorAccent` | #B68235 | 58 | 13 |
| `ColorTextMuted70` | ink 70% | 33 | 10 |
| `ColorBg` | #F3F2F2 | 32 | 15 |
| `ColorTextMuted55` | ink 55% | 27 | 8 |
| `ColorTextMuted45` | ink 45% | 23 | 10 |
| `ColorAccentDark800` | #5A3B0A | 20 | 4 |
| `ColorAccentTint12` | accent 12% | 19 | 1 |
| `ColorAccentDark700` | #7D5411 | 15 | 7 |
| `ColorTextMuted65` | ink 65% | 13 | 5 |
| `ColorTextMuted50` | ink 50% | 6 | 3 |
| `ColorSurface` | #EAE9E9 | 2 | 1 |
| `ColorTextMuted75` | ink 75% | 1 | 1 |
| `ColorScrim` | ink 60% | 1 | 1 |

Three of these should change your answers.

### 1. `ColorDivider` is the most-used token in the app, and its meaning is about to split

87 uses across 14 screens — more than the text colour. Today it is one thing: the hairline
that separates everything.

Under the grouped-surface system it becomes **two** things: `stroke.raised` for a separator
*inside* a group, and `stroke.subtle` for a hairline on the page. Which means 87 existing
hairlines have to be triaged, one at a time, into one bucket or the other.

Give me the **rule that decides it**, not a per-instance list. Something I can apply while
porting without going back to the artboards — e.g. "any separator between two rows of the
same list is `stroke.raised`; anything separating a section from the page is
`stroke.subtle`; a hairline directly on the note-equivalent surface keeps `stroke.subtle`."
If the rule has exceptions, name them.

### 2. `ColorSurface` — the raised surface — is used twice, in one file

`#EAE9E9` exists in the token set and is almost entirely unused. It is the value I pointed
at for `surface.raised`.

Good news: adopting the grouped-card treatment breaks nothing, because nothing currently
depends on it. But it also means **you are choosing this value, not inheriting it.** If
`#EAE9E9` is too close to the `#F3F2F2` page for a group to read as raised — and at 4 points
of luminance apart, it may well be — say so and propose a better light value in the same
warm-grey family. That is a change I will take.

### 3. The six muted-ink steps are not six

I asked which survive. Here is the evidence:

| Step | Uses | Screens |
|---|---:|---:|
| 70% | 33 | 10 |
| 55% | 27 | 8 |
| 45% | 23 | 10 |
| 65% | 13 | 5 |
| 50% | 6 | 3 |
| 75% | 1 | 1 |

75% and 50% together account for **7 of 103 uses**. 65% and 70% are five points apart and
almost certainly the same intent. Nib gets by on two ink steps plus the primary.

Propose the smallest set that survives contrast in **both** modes, and map all six onto it.
I would rather be told "three, and here is where each goes" than be handed six with two of
them justified after the fact.

---

## Add to the deliverables

Alongside the canvas, build sheet, decisions file and contrast table:

5. **The type mapping table** — all 26 sizes, each assigned a role.
6. **The hairline rule** — how to tell a `stroke.raised` from a `stroke.subtle` while porting.
7. **The ink mapping** — all six muted steps onto the reduced set.

These three are what make the port mechanical instead of 300 separate judgement calls, so
they are worth more to me than any single extra artboard.
