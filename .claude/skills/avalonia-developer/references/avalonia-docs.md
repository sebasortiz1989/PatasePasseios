# Using the Avalonia docs connector

An Avalonia MCP connector is configured. Before writing or editing any `.axaml`,
custom control, style selector or binding, call `get_avalonia_expert_rules` once
per session, then `search_avalonia_docs` for the topic.

This project is on **Avalonia 12.1.1**. Verify anything version-sensitive rather
than assuming 11.x — the Android lifetime change (see root `CLAUDE.md` known
gaps) is exactly that mistake, and `AvaloniaMainActivity` went from generic to
non-generic in 12.

- `search_avalonia_docs` is more reliable than `lookup_avalonia_api`, which has
  gaps (no entry for `InputPane`, which `PresenterUserControl` relies on).
- It covers stock Avalonia only. `AvaloniaFramework` types
  (`VTextBoxWithLabel`, `PresenterBase`, `NavigationController`,
  `SynchronizedCommand`) are absent — read `external/AvaloniaFramework` or that
  repo's `README.md`.
- Some API details are faster to confirm against the reference assemblies in
  `~/.nuget/packages/avalonia/12.1.1/ref/net10.0/` than through the docs.
- The migration tools (`analyze_wpf_project`, `migrate_to_avalonia`,
  `migrate_to_xpf`, `lookup_wpf_to_avalonia_mapping`) are for WPF ports and are
  not relevant here.

---

Related: `references/styling-design-canvas.md` (the theme tokens, canvas and controls you author
against).
