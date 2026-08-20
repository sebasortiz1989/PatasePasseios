# Nib — the browsing surface: decisions

19 Aug 2026. Why these screens are shaped the way they are, and what was rejected. This
is the file to argue with.

---

## 1. What a list row shows — title, folder, time — and what it omits

**Decision.** Two lines. Title (`type.body`, up to two lines, then ellipsis) and one
caption line: `folder · time`. Nothing else.

The question a row answers is not "what is in this note" but "**is this the note I
mean**". Three facts settle that in under a second, and they settle it in a fixed order:
the title is right or it is not; if two titles are the same, the folder separates them;
if the folder is the same too, the time says which one you touched an hour ago. That is
the whole job.

**Rejected: a snippet line.** It is the obvious third line and it fails twice. It grows
every row from 54 to ~76pt, so a screen holds nine rows instead of thirteen — in a
551-note vault, a third less list for a line you do not read. And a snippet is the first
~80 characters of the **body**, which means reading past the first line of all 551 files
to draw a list the user scrolls through. The snippet earns its cost exactly once: when it
explains why a search result is in the list. So it exists on Screen 3 and nowhere else.

**Rejected: `n links` in the caption.** The link picker shows it, and the row pattern
says to reuse the picker's furniture, so this was the closest call in the session. It is
out because a link count means parsing every file for anchors. The picker can afford it
across a small recency window; a 551-row list cannot, and a list that is briefly wrong
about link counts is worse than a list that never mentions them. Recorded as a deviation
from the picker row: **the list caption is `folder · time` where the picker's is
`date · n links`.** The folder replaces the link count because on this screen
disambiguation matters more than connectedness.

**Rejected: file-type or image icons, tag chips, colour dots, thumbnails, a "modified by
another app" marker.** Each is a new visual language for a fact nobody is asking the list
for.

**Rejected: alphabetical order, and grouping by folder.** Recency is the brief and it is
also correct: the note you want is nearly always one you have touched. Folder grouping
would put an eight-month-old file above this morning's because of where it happens to
live.

---

## 2. Telling identically-named notes apart

Same-name collisions are normal here, not an edge case, so disambiguation is a **rule in
the row**, not a fallback that fires when the app notices trouble.

**Decision — the folder cell shows the shortest suffix of the folder path that is unique
in the vault.**

- `Reading/Learning` → `Learning`
- `Work/Clients/Acme Corp/2026/Q3` → `Acme Corp / Q3`, because a dozen folders are named
  `Q3` and "Q3" alone identifies nothing.

The rule is computed from the folder list alone — no file reads — and it is stable: a
folder's display form changes only when another folder is added or renamed, not per
query, not per screen. Two notes called "Retrieval practice" sitting one above the other
read as `Learning · 18:40` and `Northwind · 16:05` and are separable at a glance without
either row growing.

**Second level — same title *and* same folder.** The caption gains the filename:
`Q1 · kickoff-2.md · 28 Feb`. Ugly on purpose. Two files with the same first line in the
same folder is a mess the user made in the file system, and the honest thing is to show
the file system.

**Third level — no title at all.** An empty file, or one starting with an image or a
blank line, shows its **filename with extension** in `ink.secondary`:
`2026-08-18-0912.md`. Grey signals "these are not the writer's words". The app never
invents a title, never writes "Untitled", and never promotes the second line into the
first — the title is the first line, and if the first line is empty then so is the title.

**Rejected: always showing the full path.** `Work / Clients / Acme Corp / 2026 / Q3`
truncates to uselessness in a 353pt caption and buries the one component that
distinguishes anything.

**Rejected: disambiguating only on collision** (show nothing normally, show a path when
two rows clash). The row would change shape depending on what else is on screen, and the
collision is invisible when the twin is 400 rows further down.

**Rejected: a per-note ID, a hash suffix, or a "2" badge.** All invent an identity the
file system does not have.

---

## 3. How the tree survives 78 folders averaging 7.1 files

**Decision — one level per push, and every level lists its own notes underneath its
subfolders.**

The arithmetic is the design. 78 folders holding 7.1 files each means most drilling ends
in three files. A conventional tree makes you tap a folder to discover it holds two
notes, then tap back. Showing `FOLDERS · 4` and then `NOTES · 6` on the same screen means
a small folder resolves where you already are. Nothing is flattened and nothing is
hidden: the nesting is real, every level is a real screen, and you can walk to the
bottom.

**Where am I: the path strip** under the bar — `All notes / … / Acme Corp / 2026 / Q3`,
ancestors in `ink.secondary`, current folder in `ink.primary`. The middle elides; the
last two ancestors survive, because "Q3 inside 2026 inside Acme Corp" is the part that
disambiguates and "Work / Clients" is the part you can infer.

**How do I get back to everything: `All notes`** is the head of the strip at every depth,
always drawn, always tappable, and always in the same place. One tap from level five to
the unfiltered list. The system back control still walks up one level at a time; the
strip is the escape hatch, not a replacement for back.

**Recursive counts on folder rows** (`98 notes · 21 folders`) are what make the tree
worth walking — you can tell before you tap whether a folder is a room or a cupboard.
They cost one directory enumeration and no file reads.

**`Filter` includes descendants.** Filtering to `Acme Corp` and getting only the four
files sitting directly in it, while 37 sit in its quarters, would be a lie about what the
folder contains. The strip on the list says so: `Acme Corp / 2026 — 41 notes`.

**Rejected: an expand/collapse outline in one scroll view.** It is what "folder tree"
usually means and it is wrong here: 78 disclosure triangles, indentation eating the
measure at depth five, and scroll position jumping every time a branch opens. It also
fights the app's one navigation idea — a push is how you go deeper, everywhere else in
Nib.

**Rejected: a sidebar or a drawer.** No sidebar exists on iPhone in this app, and the
brief already refused panels for backlinks.

**Rejected: folders as the root of navigation.** The brief is explicit and it is right —
the list is home; folders are a filter over it.

**Rejected: "recent folders" or "favourite folders" at the top.** That is a suggestion
engine wearing a small hat.

**Rejected: showing notes above folders.** Folders first is scannable and stable; a
folder's contents never push its siblings around.

---

## 4. Title matches vs in-note matches in one list

**Decision — two labelled groups in one scrolling surface, `TITLES · n` above
`IN NOTES · n`, and the kind of match is shown by *where the match is drawn*, not by a
label on the row.**

A title match bolds the matched characters inside the title and shows **no second line**.
An in-note match keeps the title plain and shows the **matching line of the note**
beneath it, with the matched characters bolded there. So the row's own shape tells you
what happened: bold in the title = the note is called this; bold in a grey line = the
note says this. No badge, no icon, no "in title" tag.

**Why titles always rank first.** Typing a name to jump to it is the commonest motion in
this app, and the failure being fixed is exactly that competing apps bury a name match
under twenty body hits. It is also honest about the machinery: titles come from the index
and are instant; body matches arrive from a background pass a beat later. The ranking the
user sees matches the order the answers actually exist.

**Rejected: one interleaved ranking with a per-row kind badge.** It reads as "one list"
in a spec and as noise on screen — a badge column that is `TITLE` / `TEXT` / `TEXT` /
`TITLE` down the page, plus a relevance ordering the user cannot predict or verify.
Groups do the same work with type.micro labels the app already uses.

**Rejected: a segmented control or two tabs.** That is the split being fixed.

**Rejected: colouring the match in `accent`.** It is the strongest available marker and
it is forbidden for a good reason: accent means "this is a link". A search hit that looks
like an anchor teaches the wrong thing about the one colour in the app. Weight 600 is the
marker; the palette stays honest.

**Rejected: multiple rows for a note that matches several times, and a "3 matches"
count.** One row per note; the first match is shown. The user is choosing a note, not a
line — line-level navigation is a job for the note screen, not the finder.

**Rejected: fuzzy / subsequence matching (`acq` → "Acme Corp Q3").** Powerful in an IDE
where identifiers are unique and typed constantly; here it produces confident nonsense
across 551 informal notes and cannot be explained to the user in one sentence. Substring
matching, all terms must match, order-independent.

**Rejected: search history, saved searches, and a suggestion list on the empty field.**
The empty field shows `RECENT` — the same rows as the list, in the same order. Nothing is
suggested; the list is the suggestion.

---

## 5. Where the find field lives

**Decision — a real field at the top of the list, visible on arrival, above the first
group label.** Not a magnifier icon in the bar, not a pull-to-reveal, not a keyboard
shortcut, not a long-press. The brief forbids a hidden gesture and this is the least
clever thing that works.

It scrolls with the content (standard `.searchable` behaviour) and comes back with a
short pull, which is the one concession to platform convention — an iPhone user already
knows it is there because every list on the phone works that way.

The placeholder is `Find in vault`, not "Search", because the field always searches the
whole vault even when the list is filtered to a folder. That is a real behavioural claim
made in three words.

---

## 6. The image block

**Decision — the image is a paragraph.** Full text measure, `radius.none`, no border, no
shadow, no card, `space.4` above and below — one step larger than the paragraph gap, so
it separates as a block without a rule or a frame. Alt text becomes a `type.caption` line
`space.1` under it; no alt text, no line.

Full measure rather than a small inline thumbnail, because in a plain-text vault an image
is almost always evidence — a whiteboard, a receipt, a drawing — and a 120pt thumbnail of
a whiteboard is a decoration of a fact.

**Tall images fit a 480 cap and narrow within the measure rather than crop.** Cropping
edits the writer's file in the reader's eyes. A tall photo becomes a narrow photo, which
looks slightly odd and is completely honest.

**Loading and missing are the same furniture at two heights** — a hairline rectangle with
nothing in it, and a 64pt hairline rectangle with the filename. Because on iCloud they
are frequently the same situation: the file is fine, it is just not here yet. Neither may
ever become a spinner, a progress bar or a "Retry" — the app does not narrate the file
system.

**Rejected: a broken-image glyph or a `danger`-coloured warning.** A missing file is not
an error the user made and `danger` is reserved for destruction. The sentence is
`floor-levels.heic — not in this folder`.

**Rejected: rewriting the markdown when a file is missing**, or offering to relink it.
Another app may put the file back in an hour.

**Rejected: a rounded corner on images.** It would be the only radius in the reading
surface, and the note has no edges.

---

## 7. The instrument

**Decision — one screen, called `Opens`, entered from the trailing control on the folder
**root**.**

Placement was the whole question. Not in the list (it would be chrome on the busiest
screen), not in the note (forbidden, correctly), and there is no settings screen to hide
it in — so it sits at the end of the only other piece of chrome in the app. Two pushes
from writing, invisible unless you go looking, and it costs the browsing surface one word
of bar text.

**It shows both windows: last 14 days and all-time**, as raw counts, kinds ordered by
count. The 14-day column answers "is it working now" and is the number that will actually
be read; all-time answers "did it ever" and stops the 14-day figure being over-read on a
quiet fortnight. Two columns of eight numbers is small enough to take in at once, which
is the argument against a chart — a sparkline of four series is a graphic where four
numbers are a fact.

**Rejected: a row in a settings list.** There is no settings list, and inventing one to
hold a single row adds a screen to the app for the author's benefit.

**Rejected: putting it on the note list.** It would be the only self-referential thing on
the home screen.

**Rejected: badges, weekly summaries, a "your best week" line, any celebration.** The
brief forbids it and the tone forbids it twice.

Counting rules worth arguing about: a **pop** back to a note already on the stack is not
an open (otherwise `LINK` inflates every time someone goes back and forth), and `RECENT`
is a reopen within five minutes — deliberately a duration, not a "last opened" list,
because the metric is about churn, not about a feature.

Storage in the vault (`.nib/opens.json`) rather than in app storage, so the numbers
survive a reinstall and can be read with any text editor. Consistent with the app owning
nothing.

---

## 8. Things inherited without change, listed so nobody re-opens them

- Back is the **source note's title** — the list's back control reads
  `‹ Saturday, 16 August`, not "Back" and not "Notes".
- Tapping any row is a **push**. No preview popover, no peek, no context menu in v1.
- The link picker remains the only **sheet**. Nothing in these five screens is a sheet.
- No accent anywhere except a link anchor. There are no anchors on Screens 1, 2, 3 or the
  instrument, so those screens carry **no accent at all** apart from the caret — and that
  is correct, not an omission.
- The row pattern from the picker (title `type.body`, caption `type.caption`, hairline
  inset to the text margin, recency-ordered, unfiltered until typing) is reused. The one
  deviation is documented in §1.
- Empty-state voice: statements of fact, no illustration, no exclamation mark, no
  "get started".

---

## 9. Known contradictions in the brief, and how they were read

1. **"One merged find field… one ranked list"** vs **"make it obvious which kind each
   result is"**. A single strict ranking and an obvious kind distinction pull apart.
   Read as: one field, one scrolling surface, one keyboard — with two labelled groups
   inside it. That satisfies "not two screens", which is what the requirement is
   protecting.
2. **"Reuse the picker row"** vs **"show which folder a note is in"**. The picker row's
   caption is `date · n links`. Kept the shape, changed the payload to `folder · time`,
   and said so in §1.
3. **"The folder tree is reachable in one tap and is a filter, not the root of
   navigation"** vs **folders being first-class and never flattened**. Read as: getting
   *to* the tree is one tap; walking *within* it is as many taps as it is deep, and the
   result of walking is a filter on the list, not a new home.


---

## 10. Revision 2 — floating controls and Settings (19 Aug, after the Apple Notes reference)

**What was asked for:** floating, larger icon controls in the manner of Apple Notes, and
a settings screen holding text size, the iCloud account, and whatever else is needed.

**What it costs.** Two locked rules bend:

1. *"Chrome separates by hairline and space, never by fill."* A control floating over a
   scrolling list must be opaque. Resolved by filling with `surface.primary` — the page
   colour — so the only thing dividing the control from the page is still the hairline.
   Apple's version relies on a translucent grey material plus a shadow; both were
   refused. The result is quieter than the reference and reads as Nib rather than as
   Notes, which is the point of keeping the palette.
2. *"Settings screens beyond whatever the instrument needs"* was out of scope in the
   brief. A per-app text size cannot live anywhere else, so the screen now exists — and
   the instrument moved into it, which is a better home than the trailing slot of the
   folder root.

**What was gained, not just conceded.** Moving find to a floating pill at the bottom puts
the app's most frequent action in the thumb and stops it scrolling away — the top-of-list
field was the weakest part of revision 1 on a 6.1" phone. Icons at 22px inside 44–52pt
targets are legible in a way 13px bar glyphs are not, which is the same accessibility
argument as the text size control.

**Rejected: an icon-only back control.** The floating pill widens to hold the source
note's title. Losing that label would break the return-to-source rule, which is the one
piece of navigation the app has.

**Rejected: a compose / new-note circle.** Apple's bottom-right button creates a note.
Nib has no create action — the daily page is where writing happens — so the same position
holds a pencil that returns to today. Same gesture, honest label.

**Rejected: controls that hide on scroll.** Nothing may move before the person does.

**Text size: one number, six steps.** The control sets body only; title, ui, caption,
micro and every line-height are derived from the locked ratios. That keeps the same-note
rule true at every size — the page is the same design bigger, not a bigger font poured
into a fixed layout. Margins and hit targets deliberately do not scale, so the measure
narrows as text grows, which is correct: fewer characters per line is what large type is
*for*.

**"Follow the system size" defaults on.** Most people have already told iOS what they
want. The in-app slider exists for the case where Nib specifically should be bigger than
everything else on the phone.

**Rejected: a text-size control inside the note** (pinch to zoom, or an Aa button in the
chrome). It would put a persistent control on the writing surface, and per-note sizes
would break the "one body step" rule the first time two notes disagreed.

**Still open:** whether Settings also carries Appearance (system / light / dark). Left
out rather than guessed — say the word and it is one more section.
