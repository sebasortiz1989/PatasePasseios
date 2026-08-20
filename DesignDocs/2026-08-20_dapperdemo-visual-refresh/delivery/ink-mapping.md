# DapperDemo — the ink mapping

21 Aug 2026. Six muted steps, 103 uses. The answer is **three roles — two of
which carry text** — and the reduction is forced by contrast, not by taste.

---

## 1. The surviving set

| Role | Light | Dark | Carries text? |
|---|---|---|---|
| `ink.primary` | `#201F1D` | `#EDEBE7` | yes |
| `ink.secondary` | `#5F5E5D` (the **70%** step) | `#A5A09A` | yes |
| `ink.disabled` | `#949392` (the 45% step) | `#6F6B66` | **no** |

`ink.secondary` is authored as an explicit hex, not an alpha, for the reason in
decisions §2 — and now for a second reason: it has to clear 4.5:1 on three
different surfaces, and an alpha is a different colour on each of them.

---

## 2. All six steps mapped

| Step | Uses | Screens | → | Why |
|---|---:|---:|---|---|
| **70%** | 33 | 10 | `ink.secondary` | The new base. Most-used step in the app, and the only one that clears 4.5:1 on all three light surfaces |
| **65%** | 13 | 5 | `ink.secondary` | Five points from 70% and the same intent, as you suspected. Fails on `surface.control` (4.12:1), so it could not have been the base |
| **55%** | 27 | 8 | `ink.secondary` | 3.62:1 on the page. It was the caption colour, at 13 px, on the second line of every row in the app |
| **50%** | 6 | 3 | `ink.secondary` | 3.14:1. Six uses, three screens, no distinct job |
| **45%** | 23 | 10 | `ink.secondary` (text) / `ink.disabled` (controls) | 2.74:1 — fails even the 3:1 large-text floor. Every one of the 23 is text, so all 23 become `ink.secondary`; the *role* survives for disabled controls only |
| **75%** | 1 | 1 | `ink.primary` | 6.77:1 — it passes, and it is one use reaching for emphasis. Emphasis is `ink.primary` at 14.73:1 |

**103 uses → 102 to `ink.secondary`, 1 to `ink.primary`, 0 to `ink.disabled`.**

`ink.disabled` has no existing users and exactly one new one: the text-size
slider while *Seguir o tamanho do sistema* is on. That is not an oversight — it
is the only place in the app where failing contrast is the correct rendering,
because dimming *is* the disabled affordance and disabled controls are exempt.

---

## 3. Why three and not four

Two text greys is the number of meanings the design has: **this is the answer**,
and **this is about the answer**. A third passing step — 75% at 6.77:1 sits
between primary's 14.73 and secondary's 5.79 — invites "slightly less important
than the caption", which is not a category anyone can apply consistently. That
is how a two-step ramp becomes a six-step ramp, which is the thing being undone.

The evidence agrees: 75% and 50% together are **7 of 103 uses**. They are not
roles, they are a value someone typed once.

---

## 4. The step moved from 65% to 70%, and the surface change is why

Worth recording because it happened after the palette was already settled.

The first pass put `ink.secondary` at **65%** (`#6A6968`): 4.90:1 on the page and
4.52:1 on the old `surface.raised` `#EAE9E9` — both passing, and it carried a
standing exception that `ink.secondary` may never sit on `surface.control`
(4.12:1).

Then `surface.raised` moved from `#EAE9E9` to `#E6E5E5` (build sheet §1, and the
reasoning is in decisions §2.1). On the darker group surface, 65% measures
**4.36:1** and fails. The next step in the app's own set is 70%, and it clears
everything:

| `ink.secondary` | on page | on raised | on control |
|---|---:|---:|---:|
| 65% `#6A6968` | 4.90:1 | **4.36:1** ✗ | **4.12:1** ✗ |
| **70% `#5F5E5D`** | **5.79:1** | **5.15:1** | **4.55:1** |

So the fix was already in the inventory: the app's most-used muted step is also
the correct one, and adopting it **deletes the exception** — labels on a control
no longer need a special case in light mode. Two problems closed by one number
that was already there.
