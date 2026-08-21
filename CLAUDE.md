# CLAUDE.md

Repository-specific guidance for **DapperDemo**. The personal global `CLAUDE.md`
holds the rules that apply everywhere — layering, naming, async, DI, data access,
testing, git. This file only adds what is true *here*: paths, commands, the
wiring this app actually uses, and where it departs from the general rules.

Where the two disagree, this file wins.

The developer role for this repo is the **`avalonia-developer`** skill
(`.claude/skills/avalonia-developer/SKILL.md`) — load it for any work here. It
carries the language policy (English code, Brazilian Portuguese UI text), the
layering/async/DI rules, and the AvaloniaFramework submodule workflow. Deep
per-topic material lives in its `references/` folder (navigation, schema,
money, backup, styling, the docs connector, the framework) — read the one that
matches the task rather than duplicating it here. The former six standalone
skills were consolidated into those references on 2026-08-19.

Cursor was retired from this repo on 2026-08-19; `.cursor/` is deleted and
there is no rules mirror to keep in step. The skill above is the single copy.

## What this is

A learning project for Dapper: a cross-platform Avalonia app for a pet-sitting
business ("Patas & Passeios"). Identifiers, comments and docs are English; the
UI text is Brazilian Portuguese.

**One deliberate exception, added 2026-08-21:** the four service-kind labels read
**Passeio · Pet sitting · Hotel · Day Care**. `Hospedagem` and `Creche` were the
pt-BR words and were replaced on the owner's instruction, twice confirmed. They live
in `AppSession.TypeLabel` plus the chip lists in `AgendaView`/`ServicesView` and the
two income lists in `UsersViewModel` — change all of them together or the screens
disagree. Do not "correct" these back to Portuguese; everything else user-facing
stays pt-BR.

`README.md` (repo root) describes the product as built — screens, schema names
(`Tutors` / `PetSitterTutors`, not Client), and how to run it. For task-specific
rules (billing, navigation, schema), prefer the skills below over inventing
behaviour from memory.

Everything below assumes you are in `DapperDemo/` (the solution directory, one
level under the repo root).

## Commands

```bash
dotnet build DapperDemo.sln
dotnet test tests/Tests.Dapper/Tests.Dapper.csproj
dotnet run --project app/DapperDemo.Desktop/DapperDemo.Desktop.csproj
```

`DapperDemo.Desktop` (net10.0, Windows/Linux), `DapperDemo.MacOS` (net10.0-macOS,
AppKit), `DapperDemo.iOS`, `DapperDemo.Android` (net10.0-android) are entry
points over the same `DapperDemo.View`. Mobile heads need the matching workload.
iOS pins a personal `CodesignKey` in its csproj.

**Building the solution fails while a head is running** — the running process
locks `bin/` output and MSBuild reports `MSB3027`/`MSB3021`. That is a lock, not
a code error. Build the individual project you changed, or stop the app.

## Layout

```
<repo>/
  CLAUDE.md  README.md
  .claude/skills/<skill-name>/SKILL.md   ← per-topic deep-dive skills
  external/AvaloniaFramework/     ← git submodule, ProjectReference (not NuGet)
  DapperDemo/
    DapperDemo.sln
    Directory.Packages.props      ← NoWarn list only; see Deviations
    Directory.Build.targets       ← imports the analyzer targets from the submodule
    Default.Analyzers.ruleset
    src/Repository.Dapper/          ← the whole data layer
    app/                          ← Viewmodel, View, Infrastructure, platform heads
    tests/Tests.Dapper/
```

The data layer lives at **`src/Repository.Dapper/`**. `src/` also still holds
`1. Contrato`, `2. Aplicacao`, `3. Modelo` and `4. Infraestrutura` from the
original template — they are **empty husks plus stale `obj/` output** from where
the project used to sit, untracked by git and compiled by nothing. Do not put
anything in them, and do not be misled by the `obj/` folders under
`1. Contrato/Repository.Dapper/`: that is last year's build output, not source.
The Portuguese folder and test-project names are legacy; leave them, and write
English inside them.

### Project graph

```
Repository.Dapper (src/Repository.Dapper)  ← DTOs, repositories, SQLite/Dapper, backup
        ↑
DapperDemo.Viewmodel                  ← presentation models, commands, session
        ↑
DapperDemo.View                       ← .axaml + code-behind, theme, components
        ↑
DapperDemo.Infrastructure             ← composition root
        ↑
Desktop / MacOS / iOS / Android
```

Assembly and root namespaces are `DapperDemo.<Project>`; the data layer is
`DapperDemo.Repository.Dapper` (project file `Repository.Dapper.csproj`).

## AvaloniaFramework submodule

The MVP, navigation, DI and control infrastructure comes from
**AvaloniaFramework**, vendored at `external/AvaloniaFramework` and consumed by
`ProjectReference`. It is also listed in `DapperDemo.sln`; restore needs it.

```bash
git clone --recursive git@github.com:sebasortiz1989/DapperDemo.git
```

A non-recursive clone fails restore with `NU1105`. Fix with
`git submodule update --init --recursive`.

Editing framework source takes effect on the next `dotnet build` — no pack, no
restore. The trade-off is that this repo pins a framework commit: after changing
framework source, commit and push there, then stage the moved pointer here
(`git add external/AvaloniaFramework`), or other machines build the old code.

`DapperDemo.Viewmodel` and `DapperDemo.View` declare `AvaloniaFramework` and
`AvaloniaFramework.DependencyInjection` as global usings (`<Using Include=... />`),
which is why `Unit`, `Factory<T>` and `Container` resolve with no per-file using.

The data layer deliberately does **not** reference the framework. Keep it that
way — it uses `ConfigureAwait` rather than the framework's `WithSync()`/`NoSync()`
helpers, which is the one place in the repo where that is correct.

## Deviations from the global guidance

Three, and they will bite if assumed away:

- **Central package management is not in use.** `Directory.Packages.props` exists
  but declares no `PackageVersion` items and does not set
  `ManagePackageVersionsCentrally`. It carries a long `NoWarn` list and nothing
  else. **Package versions live in each `.csproj`** — including
  `Avalonia 12.1.1` in `DapperDemo.View`. Adding a package means adding the
  version where the reference is.
- **There is no `stylecop.json` at the solution root.** It ships inside
  `AvaloniaFramework.Development/build/` in the submodule and is attached as an
  `AdditionalFiles` by the imported targets. Do not add a local copy — two files
  of that name is exactly the failure that package exists to avoid.
- **StyleCop is enforced at build time.** `Directory.Build.targets` imports
  `Analyzer.CodeQuality.targets` from the submodule, which brings in
  `StyleCop.Analyzers 1.2.0-beta.556` and sets `EnforceCodeStyleInBuild`.
  `Default.Analyzers.ruleset` tunes severities. If the submodule is missing, the
  build emits a loud warning and analysis silently stops.

## Dependency injection

The container is AvaloniaFramework's own, not `Microsoft.Extensions.DI`. Each
layer owns a `ContainerBuilder` under `DependencyInversion/` that yields the
builder below it plus its own registrations.

- Adding a view or view model means registering it in **both**
  `DapperDemoViewContainerBuilder` and `DapperDemoViewmodelContainerBuilder`
  (with `.WithAbstractions()`). A miss fails at runtime, not compile time.
- Navigating is `CurrentView.Show(view)` for a detail screen, `ShowRoot(view, label)`
  for a tab. `ViewShown` has no public setter, so nothing can bypass them.
  **Detail screens replace each other rather than stacking**: the history is never
  deeper than one screen above a tab, and Back from any detail returns to its tab.
  Only tabs carry a label, because only a tab is ever a back target. Back controls
  bind `Tag="{Binding Navigation.BackLabel}"` and never a literal — a detail screen
  opens from several tabs and a constant is wrong in all but one of them.
  The flattening is load-bearing, not tidiness: dog → tutor → dog → tutor grew the
  stack without bound, and because the presenters are reused singletons with the
  selected record on `AppSession`, a stacked entry does not remember which record it
  was showing. Walking back through one re-renders it with whatever is selected now.
- Data-layer singletons (`DapperDatabaseService`, the repositories,
  `BackupArchive`) are registered in `DapperDemoInfrastructureContainerBuilder`.
- `AvaloniaViewContainerBuilder` (framework) supplies the
  `SynchronizationContext` and `NavigationController`.

### Platform capability abstractions

Anything needing a `TopLevel` cannot live in a view model. The established
pattern is an interface in `DapperDemo.Viewmodel/Services/` and an Avalonia
implementation in `DapperDemo.View/Services/`, registered
`CreateSingleton<Impl>().WithAbstractions()` in the View builder:

| Abstraction | Implementation | Used for |
|---|---|---|
| `ImagePicker` | `StorageProviderImagePicker` | choosing a dog photo |
| `UriLauncher` | `AvaloniaUriLauncher` | opening the Google Calendar link |
| `FileExportDialog` | `StorageProviderFileExportDialog` | report and backup save/open dialogs |
| `DisplaySettings` | `AvaloniaDisplaySettings` | applying the theme and the text-size ramp |
| `ShareSheet` | `UnsupportedShareSheet` / `AndroidShareSheet` | sending a rendered report to another app |

Follow it for the next one. The interfaces are not `I`-prefixed, matching the
framework's convention.

`ShareSheet` is the pattern for a capability **one head has and the others do not**,
which is different from the rows above. The View layer registers the do-nothing
implementation so every head resolves something, and `DroidContainerBuilder` yields
its own builder *after* `DapperDemoInfrastructureContainerBuilder` to replace it —
the framework's container takes the later registration for a service type. Screens
ask `CanShare` and hide the button rather than offering one that fails. Put the next
phone-only capability in `DapperDemo.Android` and register it the same way.

## Current state and known gaps

Say what is real — several things here are not. Verified 2026-08-21.

- **The visual refresh is complete.** `DesignDocs/2026-08-20_dapperdemo-visual-refresh/`
  holds the brief and the delivered design; `delivery/corrections.md` lists where the
  design disagrees with the app and the app is right. Every screen is on the `T*` type
  classes and the `Border.Group` treatment, and the old token aliases (`ColorBg`,
  `ColorText`…) and the pre-refresh style classes (`Heading1`, `BtnPrimary`, `Card`,
  `Tag`, `TagSign`, `FormInput`, `ClassicCheckBox`…) were deleted from
  `ClassicalTheme` along with them. There is one token vocabulary now — the roles.
  Do not add `FontSize` literals to anything.
- **`Seguir o tamanho do sistema` does nothing yet.** Avalonia exposes no portable
  read of the OS text scale, so `AvaloniaDisplaySettings.SystemTextSizeStep` returns
  the default step. The switch, the inert-slider readout and the persistence are
  real; the platform value is the seam, and it needs per-head code for iOS Dynamic
  Type and Android's font scale.
- **Automatic backup is set up from one row in Perfil.** It was unreachable before:
  `CloudBackupStore.LinkAsync` existed and nothing ever called it, so no destination
  was ever stored, `IsLinkedAsync` was always false, and "Enviar backup agora" could
  only fail. `SetUpCloudBackupCommand` now picks the folder, and sends the first copy
  immediately — which proves the folder is writable while the sitter is still looking
  at it. The weekly prompt (`CloudBackupSchedule.UploadInterval`, 7 days; `RetryInterval`,
  1 day after a "Não") fires from `MainViewModel.OfferBackupAsync` at login and returns
  early when no folder is set. The row's caption names the actual folder, resolved from
  the stored bookmark through `DestinationNameAsync` — the old `DisplayName` was the
  constant string "pasta escolhida", which named nothing. The manual "Salvar backup em
  outro lugar" is a deliberately separate one-off and says so; it does not touch the
  automatic destination.
- **A screen-covering overlay must tell the navigation bar to hide.** The bar is a
  child of `MainView`, added *after* the control hosting the tabs, so it paints over
  everything a tab draws — a tab's own dialogs and full-screen images included.
  Nothing inside a tab can get above it: `ZIndex` orders siblings within one panel
  and the bar is in a different parent. Avalonia's `OverlayLayer` would, but it sits
  on the TopLevel and therefore outside DesignCanvas' scale, which is the same reason
  this app has no popups. So `ConfirmDialog`, `VPhotoViewer` and `VReportPreview` each
  report their open state to the framework's `Hosting/ScreenOverlay`, and `MainView`
  hides the bar while anything is covering. A new overlay component has to do the same
  or it will render with the tab bar sitting on top of it.
- **A report is shown before it is saved.** Exporting no longer opens a save dialog:
  `ReportExporter.RenderAsync` writes the PNG to the temporary folder, the
  `VReportPreview` control puts it on screen, and Compartilhar / Salvar sit beneath
  it — the order a phone uses for a screenshot. Sharing needs nothing saved, because
  the file already exists in the cache by the time the sheet opens. The preview
  deletes its file on close. Render and save are the whole contract — the older
  one-shot `ExportAsync` is gone. **Untested on Android**: the FileProvider authority, the manifest
  `<provider>` and `Resources/xml/file_paths.xml` all have to agree, and none of it
  has run — see the note in `AndroidShareSheet`.
- **Photos are reduced on save, in the picker.** `PhotoDownscaler.Reduce` caps the
  longest edge at 1280 and re-encodes JPEG at quality 85, and
  `StorageProviderImagePicker` runs it before handing the stream on — so dog photos
  and profile photos both shrink, and no view model or repository changed.
  `DogImageStore` still writes whatever stream it is given: it is in the data layer,
  which has no codecs and stays that way. Two things to know if you touch it. The EXIF
  rotation is **baked into the pixels** and the tag dropped, because a re-encode loses
  the tag and a photo that kept it would be turned twice. And a photo already within
  1280 and already upright is passed through byte-for-byte, so picking a small image
  costs it no generation loss.
- **Photos are decoded off the UI thread, through `AvaloniaFramework.Imaging`.** Bind
  `imaging:ImageLoader.Path` (plus `DecodeWidth` where the display size is small),
  never `Image.Source` through a converter: a converter has to return the bitmap
  inside the layout pass, which put a file read and a JPEG decode on the UI thread
  every time a row scrolled into view. `PhotoCache` holds 64 decodes, LRU, keyed by
  path *and* width, and hands back a cached one in the same frame so a row scrolled
  back does not blank and refill. `ImagePathConverter` was deleted; `ExifOrientation`
  survives and is used by `PhotoCache`.
- **The shared components live in the framework, not here** — moved 2026-08-21, because
  they are not about pet-sitting. `AvaloniaFramework.Imaging` has the four above;
  `Hosting/ScreenOverlay`; `Controls/Overlays/VPhotoViewer` and `VReportPreview`;
  `Controls/Pickers/VPeriodPicker` with its `Presentation.PeriodPicker` /
  `PeriodScope` / `PeriodCell` / `MonthOption`. Two consequences that bite.
  **They cannot see this app's tokens.** A framework control never writes
  `{DynamicResource InkPrimary}` — it takes `V*` properties, and the three style
  blocks at the end of `ClassicalTheme.axaml` are where the roles are mapped onto
  them. Restyling one of these controls means editing that block, not the control.
  **Their Portuguese lives at the usage site**, not in the control: `VHint`,
  `VShareText` and `VSaveText` are set on the tags in `DogDetailView`, `UsersView`
  and `TutorDetailView`, and a control whose caption is left unset renders an empty
  button rather than an English word. `ServicePeriod` stays here — it depends on the
  DTOs — and supplies the pt-BR month abbreviations to each `new PeriodPicker(this,
  ServicePeriod.ShortMonthName)`; the framework's own default is the culture's, which
  is not the same three lower-case letters. `ConfirmDialog` and `DesignCanvas` are
  still local and are the obvious next candidates.
- **Virtualization needs a bounded viewport, not just the panel.** **Cachorros,
  Tutores and Agenda** virtualize: the list is the direct child of a `ScrollViewer`
  sitting in a star-sized grid row, with `VirtualizingStackPanel` as its `ItemsPanel`
  and the tab-bar clearance moved to the ScrollViewer's `Padding`. Setting the panel
  on a list nested in a page-level `StackPanel` does nothing — the height is
  unbounded, so every row is realized regardless. The remaining lists are sections of
  a scrolling page (the two detail screens, Usuários) or hand-entered form rows
  (Serviços' DIAS/HORÁRIOS); making one of those the scroll host would mean a
  scrollable list inside a scrolling page. They are bounded by data instead — one
  dog's services, one tutor's dogs — and their rows are text now that photos load
  asynchronously.
- **`RepositoryBase` is a full CRUD contract with unimplemented overrides.**
  `RepositoryDogs`, `RepositoryTutors` and `RepositoryPetSitter` throw
  `NotImplementedException` from ten overrides between them. Check before calling;
  implement the stub you need rather than routing around it. `RepositoryServices`
  does not derive from it at all — it spans four tables.
- **Tests cover the data layer only.** `tests/Tests.Dapper` has real coverage of the
  repositories, the migrations, the money rules, backup compatibility and the display
  preference — 176 tests; run them before touching any of those. The view models have
  none, so anything in `DapperDemo.Viewmodel` is verified only by running the app.
- **Walk and pet-sitting bookings have no stored duration.** The Google Calendar
  export assumes one hour (`ServiceDetailViewModel`, `Date.AddHours(1)`).
- The dog screen lists **upcoming unpaid services only**, so past unpaid work is
  invisible there. The tutor screen lists every unpaid service regardless of date,
  because that list is the tutor's bill.

### Fixed since this list was written

Kept briefly so the entries are not re-added from memory:

- **The Android head starts.** `App.axaml.cs` handles `IActivityApplicationLifetime`
  and so does `AvaloniaNavigationController` in the submodule; `DroidApplication`
  is in place. Deploy with `dotnet build … -t:Install`, never a bare `adb install` —
  Debug builds use Fast Deployment and the APK deliberately ships without managed
  assemblies.
- **Hotel income is not understated.** `GetMonthlyIncomeAsync` sums `ServiceItem.Total`,
  which multiplies nights, adds the extra and applies the discount — the same figure
  the per-dog breakdown uses.
