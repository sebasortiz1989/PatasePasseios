# CLAUDE.md

Repository-specific guidance for **DapperDemo**. The personal global `CLAUDE.md`
holds the rules that apply everywhere — layering, naming, async, DI, data access,
testing, git. This file only adds what is true *here*: paths, commands, the
wiring this app actually uses, and where it departs from the general rules.

Where the two disagree, this file wins.

For deep, task-specific background, see the skills in `.claude/skills/`:
`navigation-presentation`, `data-layer-schema`, `money-payments-credit`,
`backup-restore-export`, `styling-design-canvas`, `avalonia-docs-connector`.
Read the relevant one before working in that area rather than duplicating it
here.

Cursor users get the same guidance from `.cursor/rules/`: an always-on
`project-overview` rule mirroring this file, plus one auto-attached rule per
skill (same names) that fires on the relevant files and points back to the
matching `SKILL.md`. The rule and its skill are kept in step — when you change
one, change the other.

## What this is

A learning project for Dapper: a cross-platform Avalonia app for a pet-sitting
business ("Patas & Passeios"). Identifiers, comments and docs are English; the
UI text is Brazilian Portuguese.

`README.md` (repo root) holds the intended entity model, relationships and a
screen-by-screen spec. **Read it before adding a feature** rather than inventing
one — but note it is a design document and has drifted from the code in places
(see *Known drift* below).

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
    src/1. Contrato/Repository.Dapper/   ← the whole data layer
    app/                          ← Viewmodel, View, Infrastructure, platform heads
    tests/Tests.Dapper/
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
- **Tests cover the data layer only.** `tests/Tests.Dapper` has real coverage of
  the repositories, the migrations and the money rules (`ServiceItem`,
  `PaymentAllocation`) — run it before touching any of them. The view models have
  none, so anything in `DapperDemo.Viewmodel` is still verified only by running
  the app.
- **Walk and pet-sitting bookings have no stored duration.** The Google Calendar
  export assumes one hour.
- The dog screen lists **upcoming unpaid services only** (`s.Date >= now &&
  !s.ServicePaid`), so past unpaid work is invisible there. The tutor screen lists
  every unpaid service regardless of date, because that list is the tutor's bill.

### Known drift

`README.md` says Client / PetSitterClient; the schema says `Tutors` /
`PetSitterTutors`, and the code follows the schema. The schema wins — say so
rather than silently picking one.
