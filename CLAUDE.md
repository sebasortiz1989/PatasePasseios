# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A learning project for Dapper: a cross-platform Avalonia app for a pet-sitting business ("Patas & Passeios"). UI text is Brazilian Portuguese; code/identifiers are English. `README.md` holds the intended entity model, relationships, and screen-by-screen spec — check it before adding features.

Everything lives under the `DapperDemo/` subdirectory (solution root). All commands below assume you are in `DapperDemo/`.

## Avalonia docs connector

An Avalonia MCP connector is configured for this repo. Before writing or editing any `.axaml`, custom control, style selector, or binding, call `get_avalonia_expert_rules` once per session, then `search_avalonia_docs` for the specific topic. Prefer it over recalling Avalonia from memory — this project is on Avalonia **12.1.1**, so verify anything version-sensitive rather than assuming 11.x behaviour.

Limits worth knowing:

- `lookup_avalonia_api` has gaps (e.g. no entry for `InputPane`, which `PresenterUserControl` relies on). `search_avalonia_docs` is the more reliable of the two.
- It covers stock Avalonia only. `AvaloniaFramework` types (`VTextBoxWithLabel`, `PresenterBase`, `NavigationController`, `SynchronizedCommand`) are absent — read `../../AvaloniaFramework` or that repo's `README.md` for those.
- The migration tools (`analyze_wpf_project`, `migrate_to_avalonia`, `migrate_to_xpf`, `lookup_wpf_to_avalonia_mapping`) are for WPF ports and are not relevant here.

## Commands

```bash
dotnet build DapperDemo.sln
```

```bash
dotnet test tests/Tests.CasosDeUso.Dapper/Tests.CasosDeUso.Dapper.csproj
```

Single test: `dotnet test tests/Tests.CasosDeUso.Dapper/Tests.CasosDeUso.Dapper.csproj --filter "FullyQualifiedName~Test1"`

Run the app — pick the head project for the platform:

```bash
dotnet run --project app/DapperDemo.MacOS/DapperDemo.MacOS.csproj
```

`DapperDemo.Desktop` (net10.0, Windows/Linux/generic), `DapperDemo.MacOS` (net10.0-macOS, uses AppKit), `DapperDemo.iOS`, `DapperDemo.Android` are all entry points over the same `View` project. Requires the `android`/`ios`/`macos` workloads. iOS builds pin a personal `CodesignKey` in the csproj.

## AvaloniaFramework dependency

The MVP, navigation, DI, and custom-control infrastructure comes from **AvaloniaFramework**, which is vendored as a **git submodule** at `external/AvaloniaFramework` (repo root, one level above the solution) and consumed by `ProjectReference` — not as a NuGet package. It is also listed in `DapperDemo.sln`, which restore requires.

Cloning therefore needs the submodule:

```bash
git clone --recursive git@github.com:sebasortiz1989/DapperDemo.git
```

If a clone was made without `--recursive`, the build fails with `NU1105`. Fix it with:

```bash
git submodule update --init --recursive
```

Because it is a project reference, **editing framework source takes effect on the next `dotnet build` — there is no pack or restore step**. The trade-off is that the submodule pins a specific framework commit: after committing framework changes in `external/AvaloniaFramework`, push them, then stage the moved pointer from the DapperDemo repo root (`git add external/AvaloniaFramework`) or other machines will still get the old commit.

`DapperDemo.Viewmodel` and `DapperDemo.View` declare `AvaloniaFramework` and `AvaloniaFramework.DependencyInjection` as global usings (`<Using Include=... />`), which is why `Unit`, `Factory<T>`, and `Container` resolve without a per-file using. The data layer (`Mensagens.Dapper`) deliberately does **not** reference the framework — keep it free of UI dependencies, and use `ConfigureAwait` there rather than the framework's `WithSync()`/`NoSync()` helpers.

## Architecture

### Project graph

```
Mensagens.Dapper (src/1. Contrato)  ← data layer: DTOs, repositories, SQLite/Dapper
        ↑
DapperDemo.Viewmodel  ← presentation models, commands, MockAppData
        ↑
DapperDemo.View       ← Avalonia .axaml + code-behind, theme
        ↑
DapperDemo.Infrastructure  ← composition root binding repositories into the container
        ↑
Desktop / MacOS / iOS / Android  ← platform heads
```

`src/2. Aplicacao`, `src/3. Modelo`, `src/4. Infraestrutura` are empty placeholders from the original layered template; all real data code currently sits in `1. Contrato`.

### Dependency injection

The container is `AvaloniaFramework`'s own, not Microsoft.Extensions.DI. Each layer owns a `ContainerBuilder` in its `DependencyInversion/` folder that yields the builder of the layer below it plus its own registrations. Adding a View or ViewModel means registering it in **both** `DapperDemoViewContainerBuilder` and `DapperDemoViewmodelContainerBuilder` (with `.WithAbstractions()`), otherwise resolution fails at runtime, not compile time.

`AvaloniaViewContainerBuilder` (from the framework) supplies the `SynchronizationContext` and the `NavigationController`.

### MVVM wiring

- ViewModels derive from `PresentationModelBase<TInput, TResult>` and are decorated `[AddINotifyPropertyChangedInterface]` (PropertyChanged.Fody weaves INPC — write plain auto-properties, never hand-rolled `OnPropertyChanged`).
- Views derive from `PresenterUserControl<TViewModel, TInput, TResult>` (from the framework), which resolves its ViewModel from the ambient container and sets it as `DataContext`. A view's constructor only calls `InitializeComponent()`. The resolved view model is exposed as `PresentationModel`.
- `Unit` is the framework's no-input/no-result type — the equivalent of `void` in a generic position.
- Navigation is `NavigationController.PushAsync(factory.Create())`. ViewModels take `Factory<PresenterBase<TargetViewModel, Unit, Unit>>` in their constructor rather than the target ViewModel directly. `PushAsync` does not complete until that screen is popped.
- `Factory.Create()` takes no runtime arguments, so "which record am I opening" is passed via `MockAppData.SelectedDogId` / `SelectedTutorId` / `SelectedServiceId` set immediately before pushing.
- Commands are `SynchronizedCommand(..., SynchronizationBehavior.Discard, true)`.
- Startup: `App.OnFrameworkInitializationCompleted` installs `ShellWindow`/`ShellView` as the shell and pushes `LoginViewModel` as the initial view. `MainViewModel` hosts the five tabs by swapping `CurrentView` between eagerly created presenters.

### Data layer (Dapper + SQLite)

`DapperDatabaseService` is a DI singleton that, in its constructor, calls `SQLitePCL.Batteries.Init()`, resolves a per-OS app-data folder, creates `DapperDemo.db`, runs `CREATE TABLE IF NOT EXISTS` for the whole schema, and inserts a mock `test@test.com` / `8998` pet sitter. It exposes `Connection` as a **new** `SqliteConnection` per access — callers `using` it and `Open()` it themselves.

Repositories derive from `RepositoryBase<TEntity>` and live in `Aggregates/`. The base class is a full CRUD contract but most overrides currently `throw new NotImplementedException()` — only `RepositoryPetSitter.Add`/`GetAll`/`VerifyLogin` and `RepositoryDogs.GetAll` are real. Note `GetAll` is callback-based (`Action<TEntity[]> onComplete, Action<Exception>? onError`) fired from a `Task.Run`, so callers must marshal back to the UI thread — use `synchronizationContext.Run(() => ...)` around anything that touches bound state.

Operations return the `Response` enum (`Successful`, `EmailExists`, `WrongPassword`, …) rather than throwing; `EnumExtensions.GetDescription()` turns it into user-facing text. Passwords are BCrypt-hashed in the repository, never stored raw.

The canonical schema is the SQL string in `DapperDatabaseService.CreatePetSitterTableIfNotExists`. DTOs in `Dtos/` mirror it and carry the matching `CREATE TABLE` in a trailing comment. Note the schema uses `Tutors`/`PetSitterTutors` where README says Client/PetSitterClient.

### Mock data

`MockAppData` (DI singleton, in `Viewmodel/Viewmodels/Mock/`) is the in-memory stand-in for everything except PetSitter auth — tutors, dogs, and services are all mock. Its `DataChanged` and `LogoutRequested` events are how screens stay in sync. Replacing a mock area with real Dapper repositories is the ongoing direction of the project.

### Styling

`View/Components/ClassicalTheme.axaml` defines the design tokens (`ColorBg`, `ColorAccent`, `Heading1`, `Kicker`, `ClassicInput`, …). Views must bind to these `StaticResource` keys and classes — no raw hex colors or font names. Layouts are authored against a fixed 720×1560 canvas wrapped in a `Viewbox`; pixel values are the source design's px scaled by ~1.7476. Inputs and buttons come from `AvaloniaFramework` (`inputs:VTextBoxWithLabel`, `buttons:VButton`, `buttons:GroupButton`), not stock Avalonia controls — their per-state appearance is set through `V*` properties on a style class, see `ClassicalTheme.axaml`. `App.axaml` must include `<framework:LayoutStyles />` or those controls render untemplated. Compiled bindings are on by default, so every `.axaml` needs `x:DataType`.

## Conventions

- `.editorconfig` + `stylecop.json` are enforced; `Directory.Packages.props` suppresses a long `NoWarn` list. Analyzer warnings (CA1305, CA2000) are currently tolerated in the Viewmodel project.
- Assembly/root namespaces are `DapperDemo.<Project>`; the data layer is `DapperDemo.Mensagens.Dapper`. These are set per-csproj, not derived in `Directory.Packages.props`.
- `NoSync()` / `WithSync()` (from `AvaloniaFramework.Threading`) are used instead of `ConfigureAwait` in the view and view-model layers.
