# DapperDemo visual refresh

20–21 Aug 2026. Restyling DapperDemo to the structural discipline of the Nib browsing
surface — grouped surfaces, floating controls, one type ramp, a real dark mode — while
keeping DapperDemo's own warm-grey palette, gold accent and serif type.

Two halves: what was asked for, and what came back.

```
brief/       the request, and the Nib material it was based on
delivery/    Claude Design's output, 21 Aug
```

## brief/

| File | What |
|---|---|
| `prompt.md` | The full brief. §7 is the type inventory counted out of the app. |
| `prompt-as-sent.md` | What was **actually** sent first — this predates §7. |
| `prompt-2-inventories.md` | Follow-up: the type inventory and the colour inventory, with the mapping tables they ask for. Sent as a second message. |
| `attach/` | The Nib reference given alongside: v3 spec, decisions, canvas PNG, Settings crop. |

`prompt-as-sent.md` is kept because it explains the sequence — the type inventory reached
Claude Design through the follow-up, not the first message. `prompt.md` is the version to
reuse if this is ever run again from scratch.

## delivery/

| File | What |
|---|---|
| `build-sheet.md` | Geometry tables — the file to implement from. |
| `decisions.md` | What was rejected and why. The file to argue with. |
| `contrast-table.md` | Every text pair, both modes, against 4.5:1. |
| `type-mapping.md` | All 26 font sizes → roles. The ramp grew from five roles to seven; the reasoning is in §1. |
| `hairline-rule.md` | How to tell a group separator from a page hairline while porting — the rule for the 87 `ColorDivider` uses. |
| `ink-mapping.md` | Six muted steps → **three** roles, only two of which carry text. |
| `canvas.png` | The whole canvas rendered, 2172 × 11996. |
| `canvas/` | The live canvas and everything it loads. |

### canvas/

`DapperDemo Refresh.dc.html` is the design; `Todas as Telas.dc.html` is the walkable
prototype at Android resolution. Both load `./support.js` and
`_ds/classical-<id>/{styles.css,_ds_bundle.js}` **by relative path**, so those four files
have to stay siblings — moving a `.dc.html` on its own gives you an unstyled page.

`_ds/classical-<id>/styles.css` is the token source of truth: the palette, both tonal
ramps, the type scale and the spacing scale as CSS custom properties. It is the most
useful single file here when porting to `ClassicalTheme.axaml`.

`android-frame.jsx` is loaded by neither page. Kept anyway — it is part of the canvas
bundle and the editor may want it if the canvas is ever reopened.

`canvas.png` was rendered from the HTML with headless Chrome at 1× on 20 Aug. If the
canvas changes, re-render rather than editing the PNG.

## Removed from the delivered folder

- **`uploads/`** — the prompts, already in `brief/`, plus eight screenshots from 1 Aug of
  a placeholder-era build ("Home View", emoji tab icons). Neither is the current app.
- **`.thumbnail`** — a small WebP preview, superseded by `canvas.png`.
