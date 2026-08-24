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

`CurrentView` keeps a **back stack**, and detail screens genuinely stack: Davis →
Jony → Davis walks back Jony → Davis → Tutores, one press per hop.

- `Show(x, label)` pushes the current screen and shows `x` — for drilling into a
  detail screen. The label names the **record**, not the screen type: "Jony", never
  "Cachorro", because it is what the back control of anything opened from `x` reads.
  Back controls bind `Tag="{Binding Navigation.BackLabel}"`, never a literal.
- `ShowRoot(x, label)` clears the stack and shows `x` — used by `MainViewModel` for
  the five tabs, so switching tabs discards detail screens opened from the old one.
- `GoBack()` pops, restoring the popped entry's `Selection` before its screen
  reappears.

**Why the `Selection` matters.** The presenters are reused singletons and the record
each shows lives on `AppSession`, so an entry remembering only "the dog screen" does
not remember *which dog* — walking back into it re-renders it with whatever is
selected now, and the history becomes a loop rather than a path. This is why the app
spent a while with the stack flattened instead. Each entry now carries the
`Selection` (`Viewmodels/Session/Selection.cs`) current when its screen was shown,
captured **at `Show` time, not at push time**: by push time the caller has already
selected the record for the screen it is opening. A new navigation path that skips
this reintroduces the loop.

**A record deleted while it sits on the stack** is handled by the three detail view
models: finding it gone, their reload calls `GoBack()` rather than showing a phantom,
which unwinds a cascade (a tutor, then its dogs, then their bookings) one entry at a
time down to the tab. That is only safe because detail screens reload from `OnLoaded`
alone — the **tabs are the only `DataChanged` subscribers**. Subscribe a detail screen
to `DataChanged` and this turns into a background reload that can navigate underneath
the user.

**The gotcha:** a screen shown through `CurrentView` is never `RunAsync`'d, so
`OnRunStarting` never fires and `OnRunFinishing` may never fire. Those view
models expose a public `ReloadAsync()` and the view calls it from `OnLoaded` in
code-behind. This is the one sanctioned exception to "code-behind is
initialization only". Follow it for any new screen reached this way.

## Passing "which record am I opening"

`Factory.Create()` takes no runtime arguments, so the selection is put on
`AppSession` immediately before the assignment: `SelectedDogId`,
`SelectedTutorId`, `SelectedServiceId`, `SelectedServiceKind`. Keep the
set-then-show pair adjacent so it stays greppable — `CurrentView.Show` reads all four
back as one `AppSession.Selection` the moment it is called, and an intervening line
that changes one of them stores the wrong record on the stack.

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
