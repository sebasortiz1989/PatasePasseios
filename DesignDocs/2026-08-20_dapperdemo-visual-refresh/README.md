# DapperDemo visual refresh — Claude Design bundle

20 Aug 2026. Everything needed to brief Claude Design on restyling DapperDemo to the
structural discipline of the Nib browsing surface, keeping DapperDemo's own palette.

## How to use it

1. Open `prompt.md`, copy the whole thing, paste it into Claude Design.
2. Attach the four files in `attach/`. Nothing else.

## What is in `attach/`, and why

| File | What it is |
|---|---|
| `1-nib-v3-spec.md` | The Nib v3 build sheet — token values, every geometry table, the six-step text ramp, the contrast maths. The authoritative reference. |
| `2-nib-decisions.md` | Why Nib is shaped the way it is, and what was rejected. This is where the *tone* lives; the spec only carries the numbers. |
| `3-nib-canvas.png` | The whole v3 canvas rendered, 3270 × 8700 — every screen, light and dark. |
| `4-nib-settings-detail.png` | Just the Settings artboards, both modes. Pulled out because DapperDemo is gaining a Settings screen and this is the pattern to follow. |

Both PNGs were rendered from `2026-08-19_nib-browsing-surface-v3.dc.html` with headless
Chrome at 1× on 20 Aug 2026. If the Nib canvas changes, re-render rather than editing them.

## What was deliberately left out

From `NibHub/DesignDocs/ClaudeDesign/2026-08-19_nib-browsing-surface/`:

- **`support.js`, `image-slot.js`, `ios-frame.jsx`** — Claude Design's canvas runtime and
  the phone bezel component. Over 3,000 lines carrying no design intent.
- **`…-v2.dc.html` and `…dc.html` (v1)** — superseded, and not harmlessly: v3 exists
  because v1 and v2 failed a contrast check (`ink.secondary` on `surface.control`, 3.98:1).
  Supplying all three invites averaging across versions, including the broken ones.
- **`spec.md`** — the v1 spec, superseded by the v3 one.
- **`…-state-paste.md`, `…-update-body.md`** — session process notes describing how the
  artifact was updated. Not design content.
- **`uploads/`** — reference photos that were inputs to the Nib session. Unrelated to
  DapperDemo.

## Why no `.dc.html` is attached

A `.dc.html` is a **canvas file, not a reference document**. Handing one to Claude Design
risks it treating the file as a canvas to re-seed or edit — so it would work on Nib rather
than building something new for DapperDemo. The rendered PNGs say the same thing with no
ambiguity.
