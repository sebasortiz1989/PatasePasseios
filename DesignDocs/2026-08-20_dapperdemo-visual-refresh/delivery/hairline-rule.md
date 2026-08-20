# DapperDemo — the hairline rule

21 Aug 2026. `ColorDivider` is the most-used token in the app — **87 uses across
14 screens**, more than the text colour — and under the grouped-surface system its
one meaning splits into three. This is the rule that decides each one without
going back to the artboards.

---

## The rule, in order

Apply these in sequence; the first one that matches wins.

### 1. Is it at the first or last edge of a group? → **delete it**

A group has no separator above its first row or below its last row. The fill and
the 12 radius are the edge. This is the single biggest bucket and it costs
nothing to apply: a hairline immediately inside a group boundary is removed, not
retinted.

Also deleted: any hairline that was drawing a **border around** something — a
card outline, a field box, a list frame. The group fill replaces all of it.

### 2. Is it between two rows of the same group? → `stroke.raised`

Same fill, both rows inside one rounded surface. **Inset to 16** (the row's text
edge), running to the trailing edge. Light `#C6C5C5`, dark `#3C3A36`.

This is the second biggest bucket, and it is where most of the app's list
separators end up.

### 3. Is it inside a floating control? → `stroke.control`

The tab bar's four cell dividers, the segmented selectors' dividers, the divider
in the confirm dialog's button row. Light `#BBBABA`, dark `#4C4945`. Inset 10
top and bottom in the tab bar, 24 in a 44-high selector, 28 in the dialog.

A control's divider is never `stroke.raised`: it sits on a different surface and
would disappear.

### 4. Everything else → `stroke.subtle`

On the page, at the text margin, full measure. Light `#D1D0D0`, dark `#34322F`.
In practice this is: rules under a page header block, the report's rules, and
nothing else — because the grouped system moves almost every other hairline into
bucket 2 or deletes it.

---

## The one-line version

> **A hairline inside a group is `stroke.raised` and inset 16. A hairline inside a
> control is `stroke.control`. A hairline at a group's outer edge is deleted. Any
> hairline still standing on the page is `stroke.subtle`.**

---

## Exceptions, named

1. **The exported report has no groups, so every rule in it is `stroke.subtle`** —
   including the ones between table rows, which look like in-group separators and
   are not. The report is a document; it has no raised surface for
   `stroke.raised` to sit on. Full measure, not inset.

2. **The confirm dialog's button divider is `stroke.raised`, not `stroke.control`,
   even though the dialog floats.** The dialog is filled `surface.raised`, and a
   divider must match the surface it is drawn on, not the component's role. The
   dialog's 1px *outer* edge is `stroke.control`. This is the only place where one
   component uses both.

3. **A separator between two rows of *different* groups does not exist.** If the
   port finds one, the two groups were one group and the 20 gap is missing.

4. **The tab bar's old top edge is deleted outright.** It was `ColorDivider`
   across the full screen width; the bar is now a detached control and its
   boundary is the 1px edge plus the 3px page-colour ring.

---

## Expected triage of the 87

Not a promise, but what the rule implies, so a wildly different split is a signal
that something was misread:

| Bucket | Rough share | Why |
|---|---|---|
| `stroke.raised` | the majority | most of the 87 are list-row separators |
| deleted | a large minority | group edges, card outlines, field boxes, the bar's top edge |
| `stroke.control` | ~10 | tab bar 4, selectors, dialog |
| `stroke.subtle` | a handful on screens, plus the report | header rules and the document |

If `stroke.subtle` ends up carrying most of the 87, rule 2 was not applied and
the port will look like the old app with rounder corners.
