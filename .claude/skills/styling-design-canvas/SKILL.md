---
name: styling-design-canvas
description: The 720-wide design canvas, its scale factors, DesignCanvas.cs, popup-scaling mismatch, and ConfirmDialog. Use when building or adjusting any .axaml screen, control, font size, or dimension.
---

# Styling & the design canvas

`View/Components/ClassicalTheme.axaml` defines every design token (`ColorBg`,
`ColorAccent`, `ColorScrim`, `Heading1`, `Kicker`, `Chip`, `TagSign`,
`ClassicInput`, the stroked 24×24 `Icon*` geometries…). Bind to these — no raw
hex, font names or ad-hoc sizes in views.

- Layouts are authored against a **720**-wide design canvas, nominally **720×1560**.
  Pixel values are the source design's px scaled by **~1.7476**. Follow that
  factor rather than eyeballing new numbers.
- **Type carries a further ×1.1 on top of that factor**, and so do the controls
  sized around it — `VButton` heights, input `MinHeight`, the `FormInput` combo
  and date-picker heights. The canvas scales by device width, so on a phone a
  720-unit canvas lands near 0.57× and the original sizes read small in the hand.
  A new font size derived straight from the source design will look a step
  smaller than everything beside it; multiply it too. Geometry that holds no text
  — icons, dividers, avatar circles, the bottom bar's 100-unit row — is unscaled
  and stays on the plain factor.
- The canvas is scaled to the device by `Components/DesignCanvas.cs`, **not** by a
  `Viewbox`. A Viewbox fits the canvas whole and letterboxes any device that is not
  720:1560 — against `ShellView`'s black background, visibly. `DesignCanvas` takes
  its scale from the width and gives the leftover height to the screen as extra
  canvas, so the width is always exact and only the height varies. It falls back to
  Viewbox-style height-capped scaling, centred, when the display is too wide for
  that (desktop, tablet).
- **Consequence for new screens: never pin a root `Height`.** Set `Width="720"`,
  leave the height to stretch, and put the content in a `ScrollViewer` so it can
  absorb a taller device. Only the three screens pushed by `NavigationController`
  (`LoginView`, `SignUpView`, `MainView`) carry a `DesignCanvas`; everything shown
  through `CurrentView` sits inside `MainView`'s and must not add its own.
- **Popup content is not scaled by the canvas.** `DesignCanvas` scales with a
  `RenderTransform`, which does not affect layout, and a popup lays out in its own
  visual root — so a drop-down, flyout or tooltip measures in canvas units and
  draws at scale 1. The field shrinks with the page; its drop-down does not. At a
  phone-like 0.44 scale that is a list roughly **2.8× wider than the control it
  belongs to**, with text to match. Tuning font sizes does not fix it: the width,
  row height and padding are all wrong by the same factor.
  - For dog/tutor pickers use **`inputs:VSearchableComboBox`** (AvaloniaFramework),
    which lists its matches *inline* in ordinary layout for exactly this reason, so
    everything stays in canvas units. Style it with `Classes="FormInput"`.
  - Stock `ComboBox` and `CalendarDatePicker` still open real popups and still have
    this mismatch. It is tolerable there because their content is short and uses
    theme-default sizes rather than canvas-derived ones — but do not put a
    canvas-sized `FontSize` in one of their item templates.
- Inputs and buttons come from AvaloniaFramework (`inputs:VTextBoxWithLabel`,
  `buttons:VButton`, `buttons:GroupButton`), themed through `V*` properties on a
  style class in `ClassicalTheme.axaml`, not inline.
- `App.axaml` must include `<framework:LayoutStyles />` or those controls render
  untemplated. If a control looks unstyled, check that first.
- Compiled bindings are on: every `.axaml` needs `x:DataType`.
- Icons are stroked, never filled, so one geometry serves both the muted and the
  accent state by following `Foreground`.

## ConfirmDialog

`View/Components/ConfirmDialog.axaml` is the app's modal, used for every
destructive confirm and for alerts. It is a scrim over the hosting screen rather
than a window, because the app runs single-view on mobile. Add it as the **last
child of a screen's root `Grid`**.

- Confirm form: `Sim` / `Não`.
- Alert form: `ShowCancel="False"` plus `ConfirmText`, giving one full-width
  dismiss button.

Its internal bindings use `{Binding #Root.X}`. A `UserControl` inherits its
parent's `DataContext`, so a plain `{Binding X}` would resolve against the
screen's view model instead of the control.

---

Related: `avalonia-docs-connector` (verify Avalonia 12.1.1 XAML before editing)
and `navigation-presentation` (`CurrentView` vs the three canvas-bearing
screens). Mirrored for Cursor in `.cursor/rules/styling-design-canvas.mdc` — keep
the two in step.
