# CLAUDE.md

Repository-specific guidance for **DapperDemo**. The personal global `CLAUDE.md`
holds the rules that apply everywhere — layering, naming, async, DI, data access,
testing, git. This file only adds what is true *here*: paths, commands, the
wiring this app actually uses, and where it departs from the general rules.

Where the two disagree, this file wins.

## What this is

A learning project for Dapper: a cross-platform Avalonia app for a pet-sitting
business ("Patas & Passeios"). Identifiers, comments and docs are English; the
UI text is Brazilian Portuguese.

`README.md` (repo root) holds the intended entity model, relationships and a
screen-by-screen spec. **Read it before adding a feature** rather than inventing
one — but note it is a design document and has drifted from the code in places
(see *Known drift*).

Everything below assumes you are in `DapperDemo/` (the solution directory, one
level under the repo root).

## Commands

```bash
dotnet build DapperDemo.sln
```

```bash
dotnet test tests/Tests.CasosDeUso.Dapper/Tests.CasosDeUso.Dapper.csproj
```

Run a head — pick the platform:

```bash
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
  external/AvaloniaFramework/     ← git submodule, ProjectReference (not NuGet)
  DapperDemo/
    DapperDemo.sln
    Directory.Packages.props      ← NoWarn list only; see Deviations
    Directory.Build.targets       ← imports the analyzer targets from the submodule
    Default.Analyzers.ruleset
    src/1. Contrato/Repository.Dapper/   ← the whole data layer
    app/                          ← Viewmodel, View, Infrastructure, platform heads
    tests/Tests.CasosDeUso.Dapper/
```

`src/` contains only `1. Contrato`. The numbered sibling folders from the
original template are gone — do not recreate them. The Portuguese folder and
test-project names are legacy; leave them, and write English inside them.

### Project graph

```
Repository.Dapper (src/1. Contrato)   ← DTOs, repositories, SQLite/Dapper, backup
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
| `BackupFileDialog` | `StorageProviderBackupFileDialog` | export/import dialogs |

Follow it for the next one. The interfaces are not `I`-prefixed, matching the
framework's convention.

## Presentation wiring

View models derive from `PresentationModelBase<TInput, TResult>` and are
`[AddINotifyPropertyChangedInterface]`. Views derive from
`PresenterUserControl<TViewModel, TInput, TResult>`; the constructor calls
`InitializeComponent()` and nothing else.

### Two navigation mechanisms — know which you are in

1. **`NavigationController.PushAsync`** (framework) — only the outer flow:
   `App.OnFrameworkInitializationCompleted` installs `ShellWindow`/`ShellView`
   and pushes `LoginViewModel`; login pushes `MainViewModel`.
2. **`CurrentView`** (`Viewmodels/Session/CurrentView.cs`) — everything inside
   the shell. `MainView` binds a `TransitioningContentControl` to
   `CurrentView.ViewShown`; tabs and detail screens are swapped by assigning it.

`CurrentView` keeps a **back stack**:

- `ViewShown = x` pushes the current screen and shows `x` — for drilling into a
  detail screen.
- `ShowRoot(x)` clears the stack and shows `x` — used by `MainViewModel` for the
  five tabs, so switching tabs discards detail screens opened from the old one.
- `GoBack()` pops. This is what lets a service opened from a tutor return to that
  tutor while the tutor's own Back still reaches the tutors list.

**The gotcha:** a screen shown through `CurrentView` is never `RunAsync`'d, so
`OnRunStarting` never fires and `OnRunFinishing` may never fire. Those view
models expose a public `ReloadAsync()` and the view calls it from `OnLoaded` in
code-behind. This is the one sanctioned exception to "code-behind is
initialization only". Follow it for any new screen reached this way.

### Passing "which record am I opening"

`Factory.Create()` takes no runtime arguments, so the selection is put on
`AppSession` immediately before the assignment: `SelectedDogId`,
`SelectedTutorId`, `SelectedServiceId`, `SelectedServiceKind`. Keep the
set-then-show pair adjacent so it stays greppable.

`AppSession` also carries the signed-in pet sitter, the `DataChanged` and
`LogoutRequested` events screens use to stay in sync, and the shared formatters
`TypeLabel`, `Money`, `DateTimeLabel`, `TimeLabel`, `Initials`.

Commands are `SynchronizedCommand(..., SynchronizationBehavior.Discard, true)`.
Rows that own commands (`ServiceRow`, `FutureServiceRow`,
`TutorFutureServiceRow`) are `IDisposable` and the owning list disposes them when
it rebuilds — follow that rather than leaking a command per row per refresh.

## Data layer

Dapper over SQLite. `DapperDatabaseService` is a DI singleton that, in its
constructor, calls `SQLitePCL.Batteries.Init()`, resolves the app-data folder via
`AppStorage`, creates `DapperDemo.db`, runs the schema, and inserts a mock
`test@test.com` / `8998` pet sitter. It exposes `Connection` as a **new**
`SqliteConnection` per access — callers `using` and open it themselves — plus
`DatabasePath` for backup.

**The canonical schema is the DDL in `DapperDatabaseService`.** DTOs mirror it
and carry the matching `CREATE TABLE` in a trailing comment; keep both in step.

Schema versioning has two paths, and picking the wrong one destroys data:

- A **new table** needs nothing. Every statement is `CREATE TABLE IF NOT EXISTS`,
  so it appears on the next launch with existing data untouched.
- A **new column** on an existing table needs `AddColumnIfMissing`. Bumping
  `SchemaVersion` drops every table and is only for a genuinely incompatible
  layout.

### Services span four tables

Walks, pet sitting, hotel stays and day-care live in `WalkingService`,
`PetSittingService`, `PetHotelService` and `DayCareService`, and are read as one
agenda through `RepositoryServices`. Each is a separate `SELECT` — the comment on
`WalkSelect` explains why a `UNION ALL` breaks `DateTime` mapping. Reads come
back as `ServiceItem` with the tables' differences flattened.

`ServiceKind` values are baked into those queries as literals (`0 AS Kind`), not
stored — **append new kinds, never insert**.

Day-care is the odd one: a single `Date` stored at midnight, no `EndDate`, and a
flat `Price` for the day rather than the hotel's daily rate. `AppSession`'s
kind-aware `DateTimeLabel`/`TimeLabel` overloads exist so it never renders
`00:00`.

Operations return the `Response` enum rather than throwing;
`EnumExtensions.GetDescription()` turns it into user-facing text at the
presentation boundary. Passwords are BCrypt-hashed in `RepositoryPetSitter`.

**Deleting a dog or tutor cascades by hand** — `RepositoryDogs.Delete` and
`RepositoryTutors.Delete` each `DELETE FROM` all four service tables in a
transaction. Add a fifth service table and you must add it to both, or orphaned
rows silently accumulate.

### Dog photos are not in the database

`Dogs.Image` holds a bare **file name**; the image itself lives in
`AppStorage/DogImages/` via `DogImageStore`. Anything that copies, backs up or
migrates "the database" must carry that folder too, or every photo is lost while
every record survives.

`BackupArchive` is the one place this is handled. It exports a `.zip` containing
`DapperDemo.db` (snapshotted with `VACUUM INTO`, not a file copy), the
`DogImages/` folder, and a `backup.json` manifest. Restore extracts to a temp
file and checks for the expected tables **before** touching anything, so an
invalid archive leaves the device untouched. Open validation connections with
`Pooling = false` — a pooled connection holds the file handle past `Dispose` and
leaks a full copy of the database per import.

## Styling

`View/Components/ClassicalTheme.axaml` defines every design token (`ColorBg`,
`ColorAccent`, `ColorScrim`, `Heading1`, `Kicker`, `Chip`, `TagSign`,
`ClassicInput`, the stroked 24×24 `Icon*` geometries…). Bind to these — no raw
hex, font names or ad-hoc sizes in views.

- Layouts are authored against a **720**-wide design canvas, nominally **720×1560**.
  Pixel values are the source design's px scaled by **~1.7476**. Follow that
  factor rather than eyeballing new numbers.
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
- Inputs and buttons come from AvaloniaFramework (`inputs:VTextBoxWithLabel`,
  `buttons:VButton`, `buttons:GroupButton`), themed through `V*` properties on a
  style class in `ClassicalTheme.axaml`, not inline.
- `App.axaml` must include `<framework:LayoutStyles />` or those controls render
  untemplated. If a control looks unstyled, check that first.
- Compiled bindings are on: every `.axaml` needs `x:DataType`.
- Icons are stroked, never filled, so one geometry serves both the muted and the
  accent state by following `Foreground`.

### ConfirmDialog

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

## Current state and known gaps

Say what is real — several things here are not:

- **The Android head does not start.** It boots the .NET runtime and Avalonia,
  then dies in `MainActivity.OnCreate`. Avalonia 12 replaced
  `ISingleViewApplicationLifetime` with `IActivityApplicationLifetime` on
  Android; `App.axaml.cs` only handles the former, and
  `AvaloniaNavigationController.ShowCurrentPresenter` in the submodule has the
  same gap. `DroidApplication : AvaloniaAndroidApplication<AppAndroid>` (the
  Avalonia 12 entry point) is already in place. Deploy with
  `dotnet build … -t:Install`, never a bare `adb install` — Debug builds use
  Fast Deployment and the APK deliberately ships without managed assemblies.
- **Hotel income is understated.** `RepositoryServices.GetMonthlyIncomeAsync`
  sums `s.Price` for hotel stays, which is the *daily rate*, not rate × nights.
  `DogSummaryBuilder.AmountOf` and `ServiceDetailViewModel` do multiply, so the
  Perfil headline disagrees with the per-dog breakdown beneath it for months
  containing stays. Known, not yet fixed.
- **`RepositoryBase` is a full CRUD contract with unimplemented overrides.**
  `RepositoryDogs`, `RepositoryTutors` and `RepositoryPetSitter` each still throw
  `NotImplementedException` from several. Check before calling; implement the
  stub you need rather than routing around it. `RepositoryServices` does not
  derive from it at all — it spans four tables.
- **`tests/Tests.CasosDeUso.Dapper` tests Dapper, not this app.** It is a
  scratchpad against an in-memory `Products` table. There is no coverage of the
  repositories or view models. New behaviour needs a real test, and that means
  writing the first one.
- **Walk and pet-sitting bookings have no stored duration.** The Google Calendar
  export assumes one hour.
- Detail screens list **future services only** (`s.Date >= now`), so past unpaid
  work is invisible on the dog and tutor screens.

### Known drift

`README.md` says Client / PetSitterClient; the schema says `Tutors` /
`PetSitterTutors`, and the code follows the schema. The schema wins — say so
rather than silently picking one.

## Avalonia docs connector

An Avalonia MCP connector is configured. Before writing or editing any `.axaml`,
custom control, style selector or binding, call `get_avalonia_expert_rules` once
per session, then `search_avalonia_docs` for the topic.

This project is on **Avalonia 12.1.1**. Verify anything version-sensitive rather
than assuming 11.x — the Android lifetime change above is exactly that mistake,
and `AvaloniaMainActivity` went from generic to non-generic in 12.

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
