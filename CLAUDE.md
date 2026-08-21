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

Follow it for the next one. The interfaces are not `I`-prefixed, matching the
framework's convention.

## Current state and known gaps

Say what is real — several things here are not. Verified 2026-08-20.

- **A visual refresh is half-applied.** `DesignDocs/2026-08-20_dapperdemo-visual-refresh/`
  holds the brief and the delivered design; `delivery/corrections.md` lists where the
  design disagrees with the app and the app is right. Ported so far: **Agenda,
  Cachorros, Tutores**, plus the new **Ajustes**. The other ten screens still use
  their old layout and hardcoded font sizes — they render, and they pick up both
  palettes, because `ClassicalTheme` keeps the old token names (`ColorBg`,
  `ColorText`…) alive as aliases onto the new roles. Port a screen by moving it to
  the `T*` type classes and the `Border.Group` treatment; do not add new `FontSize`
  literals to anything.
- **`Seguir o tamanho do sistema` does nothing yet.** Avalonia exposes no portable
  read of the OS text scale, so `AvaloniaDisplaySettings.SystemTextSizeStep` returns
  the default step. The switch, the inert-slider readout and the persistence are
  real; the platform value is the seam, and it needs per-head code for iOS Dynamic
  Type and Android's font scale.
- **Dog photos are stored at the camera's own size.** `DogImageStore.SaveAsync`
  copies the stream untouched. `ImagePathConverter` now decodes to the size the
  binding asks for — 192px in the dogs list, 512 elsewhere — which is what fixed the
  Android lag, but the *files* are still full-size, so the backup zip carries them at
  full resolution and photos go into it uncompressed. Downscaling on save is the
  other half and has not been done.
- **The lists are not virtualized.** `ItemsControl` inside a `StackPanel` inside a
  `ScrollViewer` realizes every row, so every dog photo decodes whether or not it is
  on screen. Fine at tens of dogs; the fix is structural (make the list the scrolling
  element) rather than a setting.
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
