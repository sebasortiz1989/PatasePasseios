# Navigation & presentation wiring

View models derive from `PresentationModelBase<TInput, TResult>` and are
`[AddINotifyPropertyChangedInterface]`. Views derive from
`PresenterUserControl<TViewModel, TInput, TResult>`; the constructor calls
`InitializeComponent()` and nothing else.

## Two navigation mechanisms — know which you are in

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

## Passing "which record am I opening"

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

---

Related: `references/styling-design-canvas.md` (screens shown via `CurrentView` must not add
their own `DesignCanvas`).
