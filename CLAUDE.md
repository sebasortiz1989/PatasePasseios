# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A learning project for Dapper: a cross-platform Avalonia app for a pet-sitting business ("Patas & Passeios"). UI text is Brazilian Portuguese; code/identifiers are English. `README.md` holds the intended entity model, relationships, and screen-by-screen spec — check it before adding features.

Everything lives under the `DapperDemo/` subdirectory (solution root). All commands below assume you are in `DapperDemo/`.

## Avalonia docs connector

An Avalonia MCP connector is configured for this repo. Before writing or editing any `.axaml`, custom control, style selector, or binding, call `get_avalonia_expert_rules` once per session, then `search_avalonia_docs` for the specific topic. Prefer it over recalling Avalonia from memory — this project is on Avalonia **12.1.1**, so verify anything version-sensitive rather than assuming 11.x behaviour.

Limits worth knowing:

- `lookup_avalonia_api` has gaps (e.g. no entry for `InputPane`, which `UserControlMobile` relies on). `search_avalonia_docs` is the more reliable of the two.
- It covers stock Avalonia only. `Verion.Apresentacao.Avalonia` types (`VTextBoxWithLabel`, `PresenterBase`, `NavigationController`, `SynchronizedCommand`) are absent — read existing code or the package itself for those.
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

`NuGet.Config` points at a private feed (`nuget.tratorimetro.com`) for the `Verion.*` packages — the app does not build without access to it.

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

`src/2. Aplicacao`, `src/3. Modelo`, `src/4. Infraestrutura` are empty placeholders from the Verion layered template; all real data code currently sits in `1. Contrato`.

### Dependency injection

The framework is Verion's own container (`Verion.Infraestrutura.Dependency`), not Microsoft.Extensions.DI. Each layer owns a `ContainerBuilder` in its `DependencyInversion/` folder that yields the builder of the layer below it plus its own registrations. Adding a View or ViewModel means registering it in **both** `DapperDemoViewContainerBuilder` and `DapperDemoViewmodelContainerBuilder` (with `.WithAbstractions()`), otherwise resolution fails at runtime, not compile time.

### MVVM wiring

- ViewModels derive from `PresentationModelBase<TInput, TResult>` and are decorated `[AddINotifyPropertyChangedInterface]` (PropertyChanged.Fody weaves INPC — write plain auto-properties, never hand-rolled `OnPropertyChanged`).
- Views derive from `UserControlMobile<TViewModel, TInput, TResult>` (in `View/Components/`), which resolves its ViewModel from the static `PreviewMobile.Container` and sets it as `DataContext`. A view's constructor only calls `InitializeComponent()`.
- Navigation is `NavigationController.PushAsync(factory.Create())`. ViewModels take `Factory<PresenterBase<TargetViewModel, Void, Void>>` in their constructor rather than the target ViewModel directly.
- `Factory.Create()` takes no runtime arguments, so "which record am I opening" is passed via `MockAppData.SelectedDogId` / `SelectedTutorId` / `SelectedServiceId` set immediately before pushing.
- Commands are `SynchronizedCommand(..., SynchronizationBehavior.Discard, true)`.
- Startup: `App.OnFrameworkInitializationCompleted` pushes `LoginViewModel` as the initial view. `MainViewModel` hosts the five tabs by swapping `CurrentView` between eagerly created presenters.

### Data layer (Dapper + SQLite)

`DapperDatabaseService` is a DI singleton that, in its constructor, calls `SQLitePCL.Batteries.Init()`, resolves a per-OS app-data folder, creates `DapperDemo.db`, runs `CREATE TABLE IF NOT EXISTS` for the whole schema, and inserts a mock `test@test.com` / `8998` pet sitter. It exposes `Connection` as a **new** `SqliteConnection` per access — callers `using` it and `Open()` it themselves.

Repositories derive from `RepositoryBase<TEntity>` and live in `Aggregates/`. The base class is a full CRUD contract but most overrides currently `throw new NotImplementedException()` — only `RepositoryPetSitter.Add`/`GetAll`/`VerifyLogin` and `RepositoryDogs.GetAll` are real. Note `GetAll` is callback-based (`Action<TEntity[]> onComplete, Action<Exception>? onError`) fired from a `Task.Run`, so callers must `synchronizationContext.SwitchTo()` before touching UI state.

Operations return the `Response` enum (`Successful`, `EmailExists`, `WrongPassword`, …) rather than throwing; `EnumExtensions.GetDescription()` turns it into user-facing text. Passwords are BCrypt-hashed in the repository, never stored raw.

The canonical schema is the SQL string in `DapperDatabaseService.CreatePetSitterTableIfNotExists`. DTOs in `Dtos/` mirror it and carry the matching `CREATE TABLE` in a trailing comment. Note the schema uses `Tutors`/`PetSitterTutors` where README says Client/PetSitterClient.

### Mock data

`MockAppData` (DI singleton, in `Viewmodel/Viewmodels/Mock/`) is the in-memory stand-in for everything except PetSitter auth — tutors, dogs, and services are all mock. Its `DataChanged` and `LogoutRequested` events are how screens stay in sync. Replacing a mock area with real Dapper repositories is the ongoing direction of the project.

### Styling

`View/Components/ClassicalTheme.axaml` defines the design tokens (`ColorBg`, `ColorAccent`, `Heading1`, `Kicker`, `ClassicInput`, …). Views must bind to these `StaticResource` keys and classes — no raw hex colors or font names. Layouts are authored against a fixed 720×1560 canvas wrapped in a `Viewbox`; pixel values are the source design's px scaled by ~1.7476. Inputs and buttons come from `Verion.Apresentacao.Avalonia` (`inputs:VTextBoxWithLabel`, `buttons:`), not stock Avalonia controls. Compiled bindings are on by default, so every `.axaml` needs `x:DataType`.

## Conventions

- `.editorconfig` + `stylecop.json` are enforced; `Directory.Packages.props` suppresses a long `NoWarn` list. Analyzer warnings (CA1305, CA2000) are currently tolerated in the Viewmodel project.
- Assembly/root namespaces are `Verion.Treinamento.<Project>`; the data layer is `Verion.Treinamento.Mensagens.Dapper`.
- `NoSync()` / `WithSync()` (Verion.Threading) are used instead of `ConfigureAwait` in most async call sites.
