# The AvaloniaFramework submodule

The MVP, navigation, DI and control infrastructure is **AvaloniaFramework** —
a separate repo, vendored at `external/AvaloniaFramework` as a **git submodule**
and consumed by `ProjectReference`, not NuGet. It is listed in `PatasePasseios.sln`;
restore needs it.

```bash
git clone --recursive git@github.com:sebasortiz1989/PatasePasseios.git
# a non-recursive clone fails restore with NU1105; fix with:
git submodule update --init --recursive
```

## What it provides

```
AvaloniaFramework/
  Core/                 Unit, await helpers (WithSync/NoSync/Forget),
                        SynchronizationContext.SwitchTo/Run
  DependencyInjection/  Container, ContainerBuilder, ImmutableContainerBuilder,
                        ContainerRegistration, Factory<T>, Lifestyle
  Presentation/         NavigationController, SynchronizedCommand,
                        PresentationExecutionContext
    UseCase/            PresentationModelBase<,>, PresenterBase<,,>, LifecycleStep<,>
  Controls/             PresenterUserControl<,,>, Buttons/ (VButton, GroupButton),
                        Inputs/ (VTextBoxWithLabel, VSearchableComboBox, …)
  Hosting/              ApplicationPreview, ShellWindow, ShellView, Navigation/,
                        DependencyInjection/
  LayoutStyles.axaml    merges every control theme — App.axaml must include
                        <framework:LayoutStyles /> or V* controls render untemplated
```

**Before inventing a UI primitive, read `Controls/` first.** The Avalonia docs
MCP knows nothing about these types — the source and the framework's `README.md`
are the documentation. Reinventing an input that already exists as a `V*`
control is the most common wasted work on this codebase.

`PatasePasseios.Viewmodel` and `PatasePasseios.View` declare `AvaloniaFramework` and
`AvaloniaFramework.DependencyInjection` as **global usings** (`<Using Include=… />`)
— that is why `Unit`, `Factory<T>` and `Container` resolve with no per-file using.

## The two packages, and the analyzer wiring

- `AvaloniaFramework` — the runtime library.
- `AvaloniaFramework.Development` — **build-only**: MSBuild props/targets, the
  shared `stylecop.json`, and the analyzer ruleset. No assembly.

`Directory.Build.targets` imports `Analyzer.CodeQuality.targets` from the
submodule, which brings in `StyleCop.Analyzers` and sets
`EnforceCodeStyleInBuild`; `Default.Analyzers.ruleset` tunes severities.

**Consequences:**

- **There is no `stylecop.json` at the solution root, and you must not add
  one.** It ships inside `AvaloniaFramework.Development/build/` and is attached
  via `AdditionalFiles` by the imported targets. Two files of that name is
  exactly the failure this package exists to prevent.
- If the submodule is missing, the build emits a loud warning and analysis
  silently stops — a clean build with no StyleCop output is a symptom, not luck.

## Editing framework source

Edits take effect on the next `dotnet build` — no pack, no restore. The
trade-off is that **this repo pins a framework commit**:

1. Change framework source under `external/AvaloniaFramework`.
2. Commit and push **in the submodule** (it is its own repo, on `master`).
3. Stage the moved pointer here: `git add external/AvaloniaFramework`, commit.

Skip step 2 or 3 and other machines build the old framework. `dotnet pack` is
only for publishing the NuGet package — never needed for PatasePasseios to pick up
a change.

The framework repo has its own `CLAUDE.md` and its own skills
(`development-analyzer-package`, `framework-conventions`) at
`external/AvaloniaFramework/.claude/skills/` — read them before changing
analyzer wiring or framework conventions. Framework API conventions (`Unit` vs
`void`, un-prefixed interface names, control theming, the reflection container)
are **deliberate**; do not normalize them to stock .NET defaults.

## Known framework gap that bites this app

Avalonia 12 replaced `ISingleViewApplicationLifetime` with
`IActivityApplicationLifetime` on Android. The framework's
`AvaloniaNavigationController.ShowCurrentPresenter` only handles the former —
this is half of why the Android head does not start (the app's `App.axaml.cs`
is the other half). Fixing it is a framework change and follows the submodule
commit-and-pin workflow above.

## The one place the framework rules do NOT apply

The data layer (`src/1. Contrato/Repository.Dapper`) deliberately does **not**
reference the framework. It uses `ConfigureAwait(false)` rather than
`WithSync()`/`NoSync()` — the only place in the repo where `ConfigureAwait` is
correct. Keep it framework-free.
