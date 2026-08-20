# Nib — the browsing surface (build sheet) · v3

19 Aug 2026 · iPhone only · extends the approved shared visual system (16 Aug 2026).
Colour, type, space, radius, hairline, motion and the anchor are **given input** and are
reproduced here only so nothing has to be looked up. Nothing in §2 was designed in this
session.

Vault this is designed against: **551 notes, 78 folders, 7.1 files per folder**, nested
several levels, names long and often duplicated across folders, written to by other apps.

---

## 0. What changed from v2

Three corrections, nothing else. No screen was redesigned, no control moved, no colour
value was added or altered.

| # | Correction | Where |
|---|---|---|
| 1 | Labels and glyphs on a `surface.control` fill move `ink.secondary` → `ink.primary` | back pill (chevron + title), find pill placeholder + magnifier, `n of m` counter in the find-in-note bar. §13.2, §14.4, §15.1, §17 |
| 2 | The AI placeholder row is deleted from the Attachments panel | §14.2 |
| 3 | `body 24pt` → `body 24px` in Settings → TEXT SIZE, and every other user-facing size audited for the same error | §12.4 |

### 0.1 Why correction 1

| Pair | Light | Dark | 4.5:1 |
|---|---|---|---|
| `ink.secondary` on `surface.control` | 4.205:1 | 3.982:1 | fails both |
| `ink.primary` on `surface.control` | 13.852:1 | 9.541:1 | passes both |

Five of the six type steps sit below the 24px large-text threshold, including the default,
so the 3:1 bar never applied. Moving the label closes the failure without a token change.
The alternative — darkening `surface.control` — would drag `accent` on `surface.control`,
already at 4.857:1 in dark against a 4.5:1 floor, and would cost a token release plus a
regeneration of the generated colour code.

### 0.2 What is deliberately still `ink.secondary`

- **The disabled previous/next glyph** in the find-in-note bar. A disabled control is
  exempt from the contrast requirement and dimming *is* the disabled affordance. Darkening
  it would destroy the only signal that the control is unavailable.
- **Anything on `surface.raised`** (`#F1EEE8` / `#2A2724`) — 4.644:1 / 4.934:1, passes.
  Row metadata, group labels and captions are unchanged.
- **Body-text metadata on `surface.primary`** — unchanged.

### 0.3 Correction 3 in full

Every user-facing size in this document and in the artifact is **px**. The design has no
pt value anywhere. One instance was wrong ("body 24pt"); it is now `body 24px`. 24pt would
be 32px — a step above anything the six-step table describes. The unit is written out
everywhere a number is shown to the user, because the same silent ambiguity nearly caused a
contrast check to apply a 3:1 threshold to 18px body text on the grounds that 18pt is the
large-text boundary.

---

## 1. Screens produced

| # | Screen | Reached from | Container |
|---|---|---|---|
| 1 | Note list (home for browsing) | `Notes` control on the daily page | pushed onto the navigation stack |
| 2 | Folder level (a filter) | `Folders`, trailing control on Screen 1 | pushed; one push per level |
| 3 | Find | the field at the top of Screen 1 | `.searchable` state of Screen 1, not a new screen |
| 4 | Note with an image | any row, any link | the existing note screen; only the image block is new |
| 5 | Opens (the instrument) | `Opens`, trailing control on the folder **root** | pushed |

No tab bar, no FAB, no toolbar, no second sheet. The link picker remains the only sheet
in the app.

---

## 2. Tokens used (given — reference only)

| Role | Light | Dark |
|---|---|---|
| `surface.primary` | `#FCFBF9` | `#1A1917` |
| `ink.primary` | `#1A1815` | `#E8E5E0` |
| `ink.secondary` | `#6E6A63` | `#9A948A` |
| `stroke.subtle` | `#E5E3DE` | `#2C2A27` |
| `accent` (= link, = app tint) | `#1B3A6B` | `#8AA7CF` |
| `danger` | `#A32D2D` | `#E24B4A` — **not rendered anywhere in these screens** |

| Token | Phone px | Desktop px | Weight | Line-height ph / dt | Tracking |
|---|---|---|---|---|---|
| `type.title` | 28 | 30 | 600 | 34 / 36 | −0.4 |
| `type.section` | 20 | 22 | 600 | 26 / 28 | −0.2 |
| `type.body` | 17 | 18 | 400 | 26 / 28 | 0 |
| `type.ui` | 15 | 16 | 400 | 20 / 22 | 0 |
| `type.caption` | 13 | 13 | 400 | 18 / 18 | 0 |
| `type.micro` | 11 | 11 | 600 | 14 / 14 | +0.6, uppercase |

Space: `space.1` 4 · `space.2` 8 · `space.3` 12 · `space.4` 16 · `space.5` 20 ·
`space.6` 24 · `space.8` 32 · `space.12` 48 (desktop only).
Radius: `radius.none` 0 · `radius.control` 6 · `radius.sheet` 14.
Hairline: 1px logical = `1 / displayScale`, colour `stroke.subtle`.
Bar height 44. Minimum hit target 44 × 44. Text margin `space.5` = 20.

Rows in this document use **two weights only**. Weight 600 appears in exactly three
places: `type.micro` labels, the `Filter` bar control, and the matched characters in a
find result. Nowhere else.

---

## 3. Screen 1 — the note list

### 3.1 Geometry

| Element | Value |
|---|---|
| Bar | 44 high, no fill, no hairline until content scrolls under it |
| Leading control | source note's title, `‹ Saturday, 16 August`, `type.ui`, `ink.secondary`, max width 230, tail truncation |
| Trailing control | `Folders`, `type.ui`, `ink.secondary` |
| Find field | inset `space.5` each side, 36 high, `radius.control` 6, 1px `stroke.subtle` border, no fill |
| Field glyph | 13 × 13 magnifier, 1.4 stroke, `ink.secondary`, `space.2` before the placeholder |
| Field placeholder | `Find in vault`, `type.ui`, `ink.primary` (v3 — was `ink.secondary`; see §0.1) |
| Field top gap | `space.1` below the bar; `space.3` below the field to the first group label |
| Group label | `type.micro`, `ink.secondary`, leading margin 20, `space.2` above the first row |
| Row padding | 10 top / 10 bottom, 20 leading, 20 trailing |
| Row line 1 | title, `type.body` 400, `ink.primary`, max **2 lines**, then tail ellipsis |
| Row line 2 | `folder · time`, `type.caption`, `ink.secondary`, `space.1`-ish 2px under line 1 |
| Row height | 54 minimum (one-line title), 76 with a two-line title — above the 44 target either way |
| Separator | 1px `stroke.subtle`, **inset to the text margin** (starts at x = 20), above every row and after the last |

Order is **modification date, newest first**. Never alphabetical, never grouped by folder.

### 3.2 Group labels

`TODAY · 3` · `YESTERDAY · 3` · `EARLIER · 545`. `type.micro`, count in the label, never
a badge or pill. Labels are the only chrome added to the list. Padding: 14 above (after
the preceding separator), 6 below.

### 3.3 What a row shows, and what it does not

Shown: **title, folder, time**. Omitted: snippet, link count, tags, file size, icons,
thumbnails, and any indicator of type. Rationale in the decisions file; cost in §9.

### 3.4 The folder name in a row

The folder shown is the **shortest suffix of the note's folder path that is unique across
the whole vault**:

- `Reading/Learning` → `Learning` (no other folder ends in "Learning")
- `Work/Clients/Acme Corp/2026/Q3` → `Acme Corp / Q3` (many folders are named "Q3")
- Vault root → the folder cell is empty; the caption is just the time.

Separator between components is ` / ` (space slash space). Computed from the folder list
alone — no file reads.

### 3.5 Telling identical titles apart

1. Two notes with the same title in **different** folders: the folder cell already
   differs. Nothing else is added.
2. Two notes with the same title in the **same** folder: the caption gains the filename —
   `Q1 · kickoff-2.md · 28 Feb`. Filename in `ink.secondary`, no extension hiding.
3. A note with **no first line** (empty file, or a file that starts with an image or a
   blank line): the title line shows the **filename with extension** in `ink.secondary`
   — `2026-08-18-0912.md`. The app never invents a title, never says "Untitled", and
   never promotes the second line.

### 3.6 Time format

| Age | Rendered |
|---|---|
| < 60 s | `just now` |
| today | `14:02` (24h or 12h per the OS locale) |
| yesterday | shown under the `YESTERDAY` group; time only |
| 2–6 days | `Mon` |
| this year | `12 Jun` |
| earlier | `4 Mar 2025` |

### 3.7 States

| State | Rendering |
|---|---|
| Many (551) | as drawn; lazy rows |
| One | one group label, one row, no special case |
| Empty vault | `type.ui` `ink.secondary` at the text margin, `space.3` under the field: **“No notes in this folder yet — anything you write, or drop in from another app, appears here.”** No illustration, no create button (the daily page is the way to write) |
| Loading the index on first launch | the list renders as rows resolve; no spinner, no skeleton, no shimmer. The find field is present and works on what is already indexed |
| Filtered | see 3.8 |

### 3.8 The filter strip

Present only when a folder filter is on. Sits between the find field and the first group
label, at the text margin, `space.3` below the field, `space.2` above the first label.

- Leading: `Acme Corp / 2026 — 41 notes`, `type.ui`, `ink.primary`, single line, tail
  truncation. The count **includes subfolders**.
- Trailing: `All notes`, `type.ui`, `ink.secondary` — clears the filter and stays in the
  same place every time.
- No chip, no pill, no fill, no close glyph, no accent.
- Filtering does not change the order, the row, or the group labels.

---

## 4. Screen 2 — the folder level

One screen class, pushed once per level. Root level and any nested level are the same
screen with different content.

### 4.1 Chrome

| Element | Value |
|---|---|
| Leading control | parent folder name, `‹ 2026`; at the root, `‹ Notes` |
| Trailing control | `Filter`, `type.ui`, **weight 600**, `ink.primary`. Absent at the root |
| Trailing control at root | `Opens`, `type.ui`, `ink.secondary` — the instrument |
| Path strip | `type.caption`, directly under the bar, 2 top / 12 bottom, at the text margin, single line, no wrap |
| Path strip content | `All notes / … / Acme Corp / 2026 / ` in `ink.secondary` + current folder in `ink.primary` |
| Path strip at root | `All notes · 551`, `ink.primary` |

The head crumb `All notes` is always drawn and always tappable: it pops to the unfiltered
list from any depth. The middle elides to `…` when the path does not fit; the last two
ancestors and the current folder always survive.

### 4.2 Content

Two sections, in this order, using `type.micro` labels:

1. `FOLDERS · n` — child folders, alphabetical (folders do not have a meaningful
   recency). Row: name `type.body` `ink.primary`; caption `type.caption` `ink.secondary`
   = **recursive** counts — `98 notes · 21 folders`, or `11 notes` when there are no
   subfolders, or `Empty` when the subtree holds nothing. Trailing `›` in `ink.secondary`,
   `type.body`.
2. `NOTES · n` — notes **directly** in this folder, recency-ordered, using the Screen 1
   row minus the folder cell (the folder is the screen). At the vault root this section
   is labelled `NOTES HERE · n`.

Separators, padding and hit targets are identical to Screen 1.

### 4.3 Folder states

| State | Rendering |
|---|---|
| Folders **and** notes | both sections, folders first |
| Only folders | `FOLDERS` section only. In place of the notes section, one line, `type.ui` `ink.secondary`, `space.4` below the last separator: **“No notes directly in 2026 — the four quarters hold them.”** (folder name and child count are substituted) |
| Only notes | `NOTES` section only. No empty folders section, no label |
| Completely empty | no sections, no labels. One line under the path strip: **“No notes in this folder.”** Nothing else — no create action. The folder exists and another app may fill it |
| Folder disappeared while open | the level pops to its parent without animation and the parent's counts refresh. No alert |

### 4.4 Filter semantics

- `Filter` pops the stack back to Screen 1 and applies the current folder **including all
  descendants**. Screen 1 shows the strip in 3.8.
- Tapping a **folder row** drills one level (push). Tapping a **note row** opens the note
  (push). Two targets, two outcomes, no long-press.
- The filter survives navigation into and out of notes. It is cleared only by `All notes`.
- The find field always searches the **whole vault**, never the filter. Stated on screen
  by the placeholder — `Find in vault`.

---

## 5. Screen 3 — find

Not a screen: the searchable state of Screen 1. Activating the field replaces the bar
row with the field plus `Cancel`; the list below becomes the result surface; the keyboard
comes up.

### 5.1 Field

| Element | Value |
|---|---|
| Position | in the 44 bar row, inset `space.5`, `space.3` gap to `Cancel` |
| Field | 36 high, `radius.control` 6, 1px `stroke.subtle`, no fill |
| Caret | `accent`, drawn by the OS from the app tint |
| Query text | `type.ui`, `ink.primary` |
| `Cancel` | `type.ui`, `ink.secondary` |
| Return key | `search`; opens the first result |

### 5.2 Results — one surface, two groups

| Group | Label | Ordering |
|---|---|---|
| Title matches | `TITLES · n` | recency within the group |
| Body matches | `IN NOTES · n` | recency within the group |

Titles always rank above body matches. Both groups are in one scrolling list under one
field — no segmented control, no tabs, no second screen.

**Title row** — the Screen 1 row exactly, with the matched characters in **weight 600**
inside the title. No snippet.

**In-note row** —
- Line 1: title `type.body` `ink.primary`, flexible, tail truncation; trailing on the
  same line, the disambiguating folder in `type.caption` `ink.secondary`.
- Line 2: the matching line of the note, `type.caption` `ink.secondary`, single line,
  ellipsis-led and tail-truncated, with the matched characters in **weight 600** and
  `ink.primary`.
- One row per note, not per occurrence. A note matching four times appears once, showing
  its first match.

Match marking is weight, never colour. `accent` means "this is a link" and a search hit
is not one.

### 5.3 Query states

| State | Rendering |
|---|---|
| Field focused, query empty | `RECENT` label, then the unfiltered recency list — same rows, same order as Screen 1. No "suggested", no history, no saved searches |
| 1–2 characters | live results, both groups, exactly as at any other length. No minimum query length |
| Multi-word query (`acme q3`) | all terms must match, in any order, within the same note; each term is marked independently |
| One result | one group, one row. No special layout |
| Zero results | one line, `type.ui` `ink.secondary`, at the text margin, `space.2` under the bar: **“No title or note text contains “aciform”.”** The query is quoted verbatim. No suggestion, no create, no apology |
| Body pass still running | titles are shown immediately; the `IN NOTES` label and rows appear when the pass completes. The count in the label is final when it is drawn — no counting up |

Results update per keystroke (titles from the index, synchronously). Nothing animates in
or out; rows replace rows at 0 ms.

---

## 6. Screen 4 — an image inside a note

The note screen is unchanged: title → body → hairline → `LINKED FROM`. An embedded image
is a **paragraph**, not an object.

| Property | Value |
|---|---|
| Width | the full text measure — 353 at the 393 phone width (`space.5` margins). Never inset, never full-bleed |
| Radius | `radius.none` = 0. No border, no shadow, no card, no rounded corner |
| Gap above | `space.4` = 16 |
| Gap below | `space.4` = 16 (to the next paragraph), or `space.1` = 4 when a caption follows |
| Caption | markdown alt text, `type.caption`, `ink.secondary`, `space.1` under the image, then `space.4` to the next paragraph. No alt text → no caption line, no filename shown |
| Tall image | scaled to fit a **480** height cap; it narrows within the measure rather than cropping. Never clipped |
| Wide image | full measure at natural height. No minimum height — a 3000 × 200 banner renders 353 × 24 |
| Tap | opens the system quick-look/zoom view. No custom viewer, no gallery, no carousel |
| Alignment | left at the text margin, like every other block |

### 6.1 Image states

| State | Rendering |
|---|---|
| Loaded | the image |
| Loading / not yet downloaded from iCloud | a 1px `stroke.subtle` rectangle at the image's own aspect ratio (from the file header, not a decode), `radius.none`, empty. **No spinner, no shimmer, no progress, no glyph** |
| Aspect unknown | the same rectangle at 4:3 within the measure (353 × 265) |
| File missing | a 1px `stroke.subtle` rectangle, **64** high, 12 leading padding, one line of `type.caption` `ink.secondary`: **“floor-levels.heic — not in this folder”**. The markdown is not altered. If the file reappears the note simply renders it |
| Unsupported format | the missing-file box with the same sentence — from the reader's point of view the difference is nothing |
| Broken / zero-byte file | the missing-file box |

Loading and missing are deliberately the same furniture at two heights: neither is an
error, and iCloud makes them the same situation half the time.

---

## 7. The instrument — `Opens`

One screen. Entry: trailing control `Opens` on the **folder root** — two pushes away from
the writing surface and invisible from it.

| Element | Value |
|---|---|
| Title | `Opens`, `type.title`, first line of the screen at the text margin |
| Section labels | `LAST 14 DAYS · 297` and `ALL TIME · SINCE 3 JUNE · 4,137`, `type.micro` |
| Rows | kind left (`type.body`), count right (`type.body`, tabular numerals) |
| Kinds | `Link`, `Find`, `Browse`, `Recent` — ordered by count, not alphabetical |
| Separator | hairline, full text measure, above and below each block |
| Block gap | `space.8` = 32 between the two windows |
| Footer | `type.caption` `ink.secondary`, `space.5` under the last rule: “Counted when a note opens. Written to `.nib/opens.json` in the vault. Nothing leaves the phone.” |

It shows **both** windows: 14 days answers "is it working now", all-time answers "did it
ever". Raw counts only — no chart, no percentage, no trend arrow, no sparkline.

Counting rules:
- One event per note **open**, classified at the moment of opening.
- `BROWSE` — opened from the list or a folder level. `FIND` — opened from the find field.
  `LINK` — opened by following an anchor or a backlink row. `RECENT` — opened again
  within 5 minutes of having been closed.
- A **pop** back to a note already on the stack is not an open and is not counted.
- Never badged, never notified, never summarised. It is true only when you go and look.

---

## 8. Motion in these screens

Everything inherits §5 of the shared system. Specifically:

| Event | Motion |
|---|---|
| Push a level, a note, or `Opens` | system push, unmodified |
| `Filter` | system pop back to the list; the strip is simply present on arrival |
| `All notes` | filter clears at 0 ms; the list re-renders without animation |
| Typing in the field | results replace results at 0 ms; no fade, no reorder animation |
| Index updating behind the list | rows change in place at 0 ms |
| An image finishing loading | it replaces its box at 0 ms; no fade-in |
| Reduce Motion | 100 ms cross-fade replaces the push, per the shared system |

Nothing on these screens animates on load, on idle, or before the person acts.

---

## 9. Cost model — what each element makes the app read

| Element | Cost | Mitigation |
|---|---|---|
| Row title (= first line of the file) | head read of every file | read the first 512 bytes only; cache `title + mtime + size` in a local index; refresh from file-presenter events |
| Row folder | directory enumeration only | free after the first walk |
| Row time | `mtime` from the enumeration | free |
| Recursive folder counts | one full enumeration, no file reads | incremental after first launch |
| Title search | index only | synchronous per keystroke |
| In-note search | reads file bodies | background pass, results appended under `IN NOTES`; debounce 150 ms |
| Link count (`n links`) | parse of every file | **not shown on the list row** for exactly this reason |
| Snippet on a list row | read of every file body | **not shown**; snippets appear only where a match justifies the read |

---

## 10. Desktop numbers (for the same screens later)

Not drawn this session; recorded so the ramp does not get re-derived.

| Element | Phone | Desktop |
|---|---|---|
| `type.title` | 28 / 34 | 30 / 36 |
| `type.body` | 17 / 26 | 18 / 28 |
| `type.ui` | 15 / 20 | 16 / 22 |
| `type.caption` | 13 / 18 | 13 / 18 |
| `type.micro` | 11 / 14 | 11 / 14 |
| Text margin | `space.5` 20 | `space.8` 32 |
| Row vertical padding | 10 / 10 | 8 / 8 (28 minimum hit target) |
| Bar height | 44 | 38 |
| Paragraph gap | `space.3` 12 | 14 |
| Image gap above/below | `space.4` 16 | `space.6` 24 |
| Image width | full measure (353) | full measure at the 68-character cap |

---

## 11. Copy, verbatim

- `Find in vault`
- `Folders` · `All notes` · `Filter` · `Opens` · `Cancel`
- `TODAY` · `YESTERDAY` · `EARLIER` · `FOLDERS` · `NOTES` · `NOTES HERE` · `TITLES` ·
  `IN NOTES` · `RECENT` · `LAST 14 DAYS` · `ALL TIME`
- “No notes in this folder.”
- “No notes directly in 2026 — the four quarters hold them.”
- “No notes in this folder yet — anything you write, or drop in from another app, appears here.”
- “No title or note text contains “aciform”.”
- “floor-levels.heic — not in this folder”
- “Counted when a note opens. Written to .nib/opens.json in the vault. Nothing leaves the phone.”

All sentence case, all statements of fact, no exclamation marks, no second person
imperative, no “Oops”, no “Try”, no “Get started”.


---

## 12. Revision 2 (19 Aug) — floating controls and Settings

Supersedes the bar chrome in §3.1, §4.1 and §5.1 where they conflict. Row anatomy,
ordering, disambiguation, find grouping and image rules are unchanged.

### 12.1 Floating control geometry

| Element | Value |
|---|---|
| Circle control | 44 × 44, radius 22, fill `surface.primary`, 1px `stroke.subtle`, no shadow, no blur, no material |
| Grouped pill (2 controls) | 105 × 44, radius 22, same fill and stroke, 1px `stroke.subtle` divider inset 10 top and bottom |
| Labelled back pill (on a note) | 44 high, radius 22, padding 12 leading / 16 trailing, chevron + source note title (`type.ui`, `ink.primary` — v3), max width 250, tail truncation |
| Icon | 22 × 22 artwork, 1.6–1.8 stroke, `ink.primary`. Was 13px inside a bar |
| Top inset | 8 below the status bar; content padded 64 from the top so rows clear the controls |
| Side inset | `space.5` = 20 |
| Find pill (bottom) | full width minus the compose circle, 52 high, radius 26, magnifier 19 + `Find in vault` (`type.ui`, `ink.primary` — v3), glyph also `ink.primary` |
| Today circle (bottom right) | 52 × 52, radius 26, pencil glyph |
| Bottom inset | 28 above the home indicator |
| Behaviour on scroll | none. Controls do not hide, fade, shrink or reposition. Content scrolls under them |

Placement: back top-left; folders + settings in the top-right pill; find and today's page
at the bottom. On a note: labelled back top-left, settings top-right, no bottom cluster.

**Rule broken, knowingly:** "chrome separates by hairline and space, never by fill." A
floating control needs an opaque fill or content runs through it. The fill is
`surface.primary` — the same colour as the page — so the separation is still carried by
the hairline. No grey, no translucency, no elevation.

**Note:** the bottom-right circle is *today's page*, not a compose action. Nib has no
"new note" button; writing happens on the daily note.

### 12.2 Settings

Reached from the gear in the top-right pill. Pushed, not a sheet.

| Section | Rows |
|---|---|
| `TEXT SIZE` | six-step slider; "Follow the system size" switch (default **on**); a live preview paragraph at the current size |
| `TEXT SIZE` — switch on | the slider is **disabled**: track, fill and specimens in `stroke.subtle` / `ink.secondary`, no accent. It still shows where iOS has put the ramp, and the value line reads "Following the system — body 24px" (px, not pt — see §0.3). Tapping the slider does nothing; turning the switch off makes it live at that same step |
| `VAULT` | `iCloud Drive` — account email + state; `Folder` — path + note count; `Opens` — the instrument |

- Slider fill and the switch's on-state use `accent` as the **app tint**. This is the only
  non-link use of accent in the app and it is what the locked palette permits.
- Signed-out state reads: "Not signed in — notes are on this phone only." No cloud badge,
  no sync progress, no reassurance copy.
- `Opens` moves here from the folder root; the folder root loses its trailing control.

### 12.3 Text size steps

The control sets **body**; every other step is derived from the locked ratios
(title = body × 1.65, line-height = size × 1.53, paragraph gap = 0.46 × line-height).

| Step | body | title | ui | caption | micro | Row height (1-line title) |
|---|---|---|---|---|---|---|
| 1 Small | 15 | 25 | 13 | 12 | 10 | 48 |
| 2 Default | 17 | 28 | 15 | 13 | 11 | 54 |
| 3 Large | 20 | 33 | 18 | 15 | 13 | 62 |
| 4 Larger | 22 | 36 | 20 | 16 | 14 | 68 |
| 5 Largest | 24 | 40 | 22 | 17 | 14 | 74 |
| 6 Maximum | 28 | 46 | 25 | 20 | 16 | 86 |

Margins (`space.5` = 20), hit targets (44 minimum) and control sizes do **not** scale.
The two-line title clamp holds at every step.

Implementation: one `@ScaledMetric`-style base fed into every text style, or a custom
`DynamicTypeSize` override on the root view — **not** per-view font literals. When
"Follow the system size" is on, the app's control is disabled and iOS drives the ramp.


---

## 13. Revision 3 (19 Aug) — grouped sections and filled controls · **v2 artifact**

Drawn in `2026-08-19_nib-browsing-surface-v2.dc.html`. Supersedes §3.1, §4.1, §5.1 and
§12.1 wherever they conflict. Row anatomy, ordering, disambiguation, find grouping, image
rules and the text-size ramp are unchanged.

### 13.1 Four new colour roles

| Role | Light | Dark | Use |
|---|---|---|---|
| `surface.raised` | `#F1EEE8` | `#2A2724` | A group of rows. Never the note |
| `stroke.raised` | `#E2DFD8` | `#3A3733` | Separator between rows **inside** a group |
| `surface.control` | `#EAE6DE` | `#34302C` | Floating controls — one step above a card, because a control usually sits over one |
| `stroke.control` | `#D8D3C9` | `#4A4641` | 1px edge on every floating control, **both** modes |

The locked six are unchanged and `danger` is still unused. `accent` gains its
app-tint uses: `Filter`, `Cancel`, `All notes`, the size slider fill, the switch.

Amended rule: *the note* separates by hairline and space; *chrome* may use two raised
surfaces. No shadow, no gradient, no blur, no elevation anywhere.

### 13.2 Group geometry

| Property | Value |
|---|---|
| Fill | `surface.raised` |
| Radius | 12, all four corners |
| Inset from screen edge | `space.5` = 20 each side |
| Row padding | 11 top / 11 bottom, 16 leading, 16 trailing |
| Separator | 1px `stroke.raised`, inset to 16 (the row's text edge), between rows only — never above the first or below the last |
| Gap between groups | `space.5` = 20 |
| Section label | `type.micro`, `ink.secondary`, **outside** the group on `surface.primary`, at margin 20, 8 above the group |
| Single-row group | same treatment; no special case |
| Bottom clearance | the last group ends 120 above the safe area on screens with a bottom cluster |

Applies to: list groups, filter strip, path strip, folder/notes sections, find result
groups, backlinks, Settings sections, Opens sections.

**Not applied to:** the note. Title, body, images and anchors stay on
`surface.primary` with no card, edge, inset or radius — same-note rule 7 is intact.
The image loading/missing boxes keep the plain `stroke.subtle` hairline, because they
live inside the note.

### 13.3 Floating control geometry

| Property | Value |
|---|---|
| Fill / edge | `surface.control` + 1px `stroke.control` |
| Circle | 44 × 44, radius 22 (52 × 52, radius 26 for the bottom cluster) |
| Labelled pill | 44 high, radius 22, padding 12 leading / 16 trailing, chevron 22 + label `type.ui` `ink.primary` (v3) |
| Grouped pill | two 52-wide cells, 1px `stroke.control` divider inset 10 top and bottom |
| Text pill (`Filter`) | 44 high, radius 22, padding 18, `type.ui` weight 600, `accent` |
| Icon | 22 × 22 artwork, 1.6–1.8 stroke, `ink.primary` |
| Insets | 8 below the status bar, 20 from the sides, 28 above the home indicator |
| Behaviour | never hides, fades, shrinks or moves on scroll |

Placement per screen:

| Screen | Top left | Top right | Bottom |
|---|---|---|---|
| Note list | back (circle) | folders + settings (grouped pill) | find pill + today circle |
| Folder root | back "Notes" (labelled pill) | settings (circle) | find pill + today circle |
| Folder level | back = parent name (labelled pill) | `Filter` (text pill) | find pill + today circle |
| Find active | — | — | find pill + `Cancel`, lifted to sit **on** the keyboard |
| Note | back = source note title (labelled pill) | find-in-note + settings (grouped pill) | tools pill — see §14 |
| Settings / Opens | back (labelled pill) | — | none |

Find active: the pill rises to 12 above the keyboard rather than jumping to the top of
the screen; results occupy everything above it.


---

## 14. Revision 4 (19 Aug) — the note screen's tools

Supersedes the Note row of §13.3. Everything in §6 (image geometry and states) is unchanged.

### 14.1 Tools pill

Bottom left, 20 from the edge, 28 above the home indicator. 52 high, radius 26,
`surface.control` + `stroke.control`, three 56-wide cells with 1px `stroke.control`
dividers inset 12 top and bottom.

| Cell | Glyph | Opens |
|---|---|---|
| 1 | `Aa` (`type.body` 600) | Format panel — everything that changes the text |
| 2 | paperclip, 22px | Attachments panel — everything that comes in from outside |
| 3 | chain link, 22px | Links panel — everything about connections |

Only three, and the grouping is the contract: a feature added later joins an existing
panel rather than adding a fourth button.

### 14.2 Panels

| Property | Value |
|---|---|
| Anchor | left edge aligned with the pill, 12 above it (bottom: 92) |
| Fill / edge | `surface.raised` + 1px `stroke.control` — it floats over content, so it takes a control edge |
| Radius | 12 |
| Width | fits its content: 256 (format), 280 (attachments), full measure (links) |
| Row | 12 top / 12 bottom, 16 leading, 20px glyph + 14 gap + `type.body` label |
| Separator | 1px `stroke.raised`, inset 16 |
| Open state | the pill cell's glyph turns `accent`. One panel at a time |
| Dismiss | tap anywhere else, tap the same cell again, or start typing. No scrim, no drag handle, no Done |
| Unbuilt rows | label and glyph in `ink.secondary` with a `type.caption` second line: "Not in this version". Present so the placement is decided; never tappable |

Contents as drawn:

- **Format** — Text style · Checklist · Table · Equation. None built in v1.
- **Attachments** — Choose photo · Take photo · Attach file *(not in this version)* ·
  Scan document *(not in this version)*.
- **Links** — `LINKS IN THIS NOTE · n` (rows: title + `folder · date`),
  `LINKED FROM · n` (rows: source title 600 + context caption), then
  **New link…** in `accent` with the caption "Select words first, or link the whole note"
  (opens the existing picker sheet — still the only sheet in the app), then the switch
  **Show Linked from in the note**.

### 14.3 Keyboard rule

When the caret is placed:
- the tools pill is **removed**, and any open panel is **dismissed**;
- one control remains — a 52 circle, bottom right, 12 above the keyboard, keyboard-with-
  chevron glyph — which dismisses the keyboard;
- nothing else sits above the keyboard.

On dismiss the pill returns in place. **The panel does not** — a dismissed panel stays
dismissed.

Exception: find-in-note keeps a bar above the keyboard, because the typing goes into it.

### 14.4 Find in this note

Entry: magnifier in the top-right grouped pill, beside settings — a reading action, so it
is not in the tools pill.

Bar: 52 high, radius 26, `surface.control` + `stroke.control`, 12 above the keyboard,
containing magnifier · query (`type.ui`) · `n of m` (`type.caption` 13px, `ink.primary` — v3; was the worst instance in the document) ·
previous · next, with **Done** in `accent` outside the bar to the right. The **disabled** previous/next glyph stays `ink.secondary` — see §0.2. A previous/next
control is `ink.secondary` when there is nothing in that direction.

Matches are drawn with the OS selection tint (accent at 18/26%) — no new colour, no
scroll-to-match animation; the view jumps.

### 14.5 Collapsing LINKED FROM

Two controls, one setting: a chevron on the `LINKED FROM · n` label (down = open, right =
collapsed) and the switch in the links panel. Collapsed hides the rows; the label and the
count always remain, so the fact that two notes point here is never hidden. Per note,
persisted in `.nib/`, not in the markdown — the app does not write to the user's file to
store a display preference.


---

## 15. Revision 5 (19 Aug) — selection linking, two searches, open notes

Supersedes §14.1 (three cells → four), §14.2 (links panel contents), §14.4 (which
magnifier is which) and §14.5 (LINKED FROM no longer lives in the note).

### 15.1 Contrast of a floating control

Content **does** scroll under controls. Three things separate them, in this order:

1. `surface.control` — now `#E8E3D9` light / `#3A3631` dark, two steps off the page.
2. `stroke.control` — `#CFC9BD` / `#56514A`, 1px.
3. A **3px ring of `surface.primary`** outside the stroke. A hard keyline gap, not a
   shadow and not a blur: text passing behind always has a clean page-coloured margin
   between it and the control.

The same three apply to panels.

### 15.2 Making a link

There is no "New link" button. Selecting text raises the system callout —
**Cut · Copy · Paste · Delete · Link** — with Link at weight 600. It opens the existing
picker sheet. A link needs words to attach to, so it can only start from a selection.

### 15.3 LINKED FROM

Removed from the note body. No block at the foot, no chevron, no per-note setting. It is
in the links panel, visible only while that panel is open.

**This breaks same-note rule 6** ("same order down the page: title → body → hairline →
LINKED FROM. Backlinks always END the note"). Rule 6 is now: the note ends where the
writing ends; backlinks are chrome, reachable from the links panel on every platform.

### 15.4 The tools pill — four cells

`Aa` · paperclip · chain · **open notes**. Geometry as §14.1, now 4 × 56 wide.

### 15.5 Two searches

| | Find in this note | Find in the vault |
|---|---|---|
| Control | circle, bottom **right**, on its own | magnifier in the top-right grouped pill |
| Opens as | the circle expands in place into a 52-high bar above the keyboard | full-screen takeover — results replace the note |
| Bar contents | magnifier · query · `n of m` · previous · next, **Cancel** outside to the right | field + Cancel, results grouped TITLES / IN NOTES |
| Scope | this note | 551 notes |
| Leaving | Cancel collapses the bar back to a circle; if the keyboard is still up, the dismiss-keyboard circle appears beside it | Cancel returns to where you were; picking a result opens that note |

Rationale: scope is signalled by size and position. Local search stays small and at the
bottom where the thumb is; vault search needs the whole screen, so it comes from the top.

### 15.6 Open notes (the tab system)

A **list in a panel**, not a tab strip.

- Rows: title (`type.body`) + `folder · when you last had it` (`type.caption`), an ✕ per
  row, recency-ordered.
- The note you are in carries a 3px `accent` bar at its leading edge and has no ✕.
- Footer action: **"Close the other three"** — the count is literal.
- No tab strip across the top: at four open notes, tabs truncate to nothing and steal
  width from the measure. A list shows every title in full.
- Open notes is what is *parked*; the navigation stack is history. They do not interact —
  closing another note never moves you.

### 15.7 Keyboard rule, amended

While the keyboard is up: no tools pill, no panel. Bottom right shows the dismiss-keyboard
circle, plus the find-in-note circle when a search was just cancelled. The find-in-note
bar is the only thing that may occupy the full width above the keyboard.


---

## 16. Revision 6 (19 Aug) — consistency pass, vault search, the switcher

### 16.1 Chrome that is identical on every screen

| Slot | Contents | Rule |
|---|---|---|
| Top left | back, **always a labelled pill** naming where it returns to | never icon-only, never "Back". Truncates at 210 |
| Top right | `[screen-specific] · find in vault · settings` in one pill | find and settings are the last two cells **everywhere**; a screen that needs its own control adds a cell to the **left** of them |

Applied: note list `[folders][find][settings]` · folder root `[find][settings]` ·
folder level `[Filter][find][settings]` · note `[find][settings]` · Settings and Opens
`[]` (back only). Settings is therefore reachable from anywhere, not only from a note.

### 16.2 Find in vault — the same behaviour app-wide

Triggered from the top-right magnifier on any screen.

1. The field expands **in place at the top** and stays there.
2. Every other floating control is hidden — top pill, tools pill, bottom circles.
3. Results fill the space between the field and the keyboard.
4. A dismiss-keyboard circle sits bottom right, above the keyboard. The find-in-note
   circle is **not** offered here — you are already searching.
5. **Cancel** (right of the field) returns to the exact screen you left, scroll position
   intact. Picking a result opens that note.

Contrast with find in this note (§15.5): local search is a small bar at the **bottom**
that never leaves the note; vault search is a **top-anchored takeover**.

### 16.3 Keyboard, final rule

Whenever the keyboard is up in a note, exactly **two** circles sit bottom right, 12 apart:
find in this note, then dismiss keyboard. No tools pill, no panel. The only exceptions are
the two search bars, which replace the circle they came from.

### 16.4 Open notes — full-screen switcher

Fourth cell of the tools pill. Takes the whole screen (a tab strip truncates to nothing by
the fourth note and steals width from the measure).

| Property | Value |
|---|---|
| Grid | 2 columns, 12 gap, 20 margins, cards 196 high |
| Card | `surface.raised`, radius 12, 1px `stroke.raised`; header = title (`type.ui` 600, 2-line clamp) + ✕; body = first two lines (`type.caption`); footer = `folder · when you last had it` |
| Current note | 1px `accent` border, footer reads "Reading now" in `accent`, **no ✕** |
| Bottom bar | "Today's page" (opens the daily note — Nib has no blank-new-note action) · "Close 3" (counts only what would actually close) · **Done** in `accent` |
| Relationship to the stack | open notes is what is *parked*; the navigation stack is history. Closing another note never moves you |

### 16.5 Panel contents, current

- **Format** — Text style · Checklist · Table · Equation *(not in this version)*.
- **Attachments** — Choose photo · Take photo · Attach file *(nitv)* · Scan document
  *(nitv)* · Record audio *(nitv)*. **No AI row** — v1 ships zero AI
  by decision, not by schedule, so there is nothing to reserve a place for (v3).
- **Links** — links out of this note · LINKED FROM. No actions: linking is a selection.

Unbuilt rows are `ink.secondary` with a `type.caption` second line and are never tappable.

### 16.6 Removed

The two note frames in the older "Screen 4" section are deleted rather than left
contradicting the current chrome. Image geometry and the missing/loading boxes are
unchanged and remain specified in §6.


---

## 17. Revision 7 (19 Aug) — final control placement

### 17.1 The two fixed slots

- **Top left**: labelled back pill, `max-width 150`, tail ellipsis. Never icon-only.
- **Top right**: **find in vault, then settings. Nothing else, ever.** No screen may add a
  cell here; a screen's own action goes to the bottom.

### 17.2 Bottom, per screen

| Screen | Bottom left | Bottom right |
|---|---|---|
| Note list | — | new folder · today's page |
| Folder root | — | new folder · today's page |
| Folder level | **Filter** (text pill, `accent`) | new folder · today's page |
| Note | tools pill (Aa · attach · links · open notes) | find in this note |
| Note, keyboard up | — | find in this note · dismiss keyboard |

The "Find in vault" bar is gone from the bottom of the browse screens — the magnifier at
the top does that job on every screen.

### 17.3 Two searches, two glyphs

| | Find in this note | Find in vault |
|---|---|---|
| Glyph | a **page with a magnifier on it** | a **plain magnifier** |
| Position | bottom right | top right |
| Behaviour | expands in place, bar sits on the keyboard | expands at the top, hides every other control, takes the screen |
| Close | ✕ button | ✕ button |

Neither uses the word Cancel. The ✕ is a 52 circle in `surface.control` with the same
edge and ring as any other control.

### 17.4 Content behind the vault field

The vault field is absolutely positioned at the top (8 below the status bar, 20 each
side). Results start at the top of the screen and scroll **behind** it — same rule as the
bottom controls, and the reason every control carries fill + edge + 3px ring.

### 17.5 Selection does not hide the chrome

The bottom controls disappear for exactly one reason: **the keyboard**. Selecting text
raises the callout and leaves the tools pill and the find circle in place.

### 17.6 Switcher

Footer is "Today's page · Close all · Done". **Close all** empties the switcher and leaves
you on today's page; single cards close with their own ✕.


---

## 18. Revision 8 (19 Aug) — making a link

### 18.1 The selection callout

Icons only, no words. 40 high, `radius.control` 6, `surface.control` + `stroke.control` +
3px ring, 48-wide cells, 1px dividers, 20px glyphs in `ink.primary`.

`cut · copy · paste · delete · link+`

The **link+** glyph is the tools-pill chain **with a plus**. Same object, two jobs: the
plain chain shows the links a note already has; the chain-with-a-plus makes one. Nothing
else in the app may use either glyph.

### 18.2 The picker sheet

The only sheet in Nib. Presented from the bottom, system radius and scrim.

| Element | Value |
|---|---|
| Grabber | 36 × 4, `stroke.raised`, 8 from the top |
| Title | `Link “extractor run”` — `type.body` 600, the selection quoted **verbatim**: never re-cased, trimmed or rewritten |
| Close | ✕ in a 40 circle, trailing. No "Cancel" |
| Search | 44 pill, `surface.raised`, placeholder `Search 551 notes` |
| List | `RECENT` label + one group; recency-ordered, **unfiltered until you type**; rows are title + `folder · date` |
| Create | pinned at the bottom in its own group, `accent`: `New note “extractor run”` with the caption "Creates the note and links the selection." Same position whether or not anything matched |
| Dismiss | returns the caret and the selection exactly as they were |

No ranking, no "suggested", no relevance score, no AI.

### 18.3 After linking

The selected words become the anchor: `accent`, 1px underline at 3px offset, text
unchanged. The caret lands immediately after the anchor. The bottom controls return at
once. Nothing animates, nothing confirms, no toast.
