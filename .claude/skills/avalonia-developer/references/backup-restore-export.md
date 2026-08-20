# Backup, restore & dog photos

## Dog photos are not in the database

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

## Saving a file: never trust the save dialog on Android

Every export — both report PNGs and the backup zip — goes through
`FileExportDialog` (`StorageProviderFileExportDialog` in the View layer), which
takes **two different routes** by platform. Do not collapse them.

Avalonia's Android backend builds the `ACTION_CREATE_DOCUMENT` intent with a
hard-coded `*/*` MIME type — `FileTypeChoices` reaches it only as a filter — and
Android works out where the extension ends by comparing that MIME type against
the name it was handed. With `*/*` nothing matches, so a colliding `maria.png`
is treated as one long extensionless name and written as **`maria.png (2)`**,
which no viewer will open. `ShowOverwritePrompt` is no help: the Android backend
ignores it, and `ACTION_CREATE_DOCUMENT` renames rather than asking, by design.
Neither is reachable from `FilePickerSaveOptions`.

So on Android the user picks a **folder** and the app does the naming.
`IStorageFolder.GetFileAsync` returns null when nothing is there, which is what
makes the "Substituir?" question possible, and `CreateFileAsync` deliberately
returns the *existing* file truncated rather than inventing `file (1)` — so the
write overwrites in place and the name stays exact. Everywhere else the system
save dialog already handles naming and overwriting properly, and is what users
expect, so it is left alone.

The replace question is asked through a `Func<string, Task<bool>>` passed in by
the view model, because the answer comes from a `ConfirmDialog` bound to view
model state that the View layer cannot reach. `ConfirmRequest`
(`Viewmodels/Utils/`) is the awaitable form of that dialog — the app's other
confirmations are a bool plus two commands, which suits a question the *user*
starts, not one raised mid-operation.

## The automatic weekly backup

Separate from the manual export above, and one-way by design. `BackupArchive` is a
whole-database snapshot, so two devices uploading to one destination would be
last-writer-wins with no possible merge — this pushes copies out, and restore stays
the deliberate manual act it always was. Never label it "sincronizar" in the UI.

- `CloudBackupSchedule` (data layer) is the pure rule: due when the last upload is
  over `UploadInterval` (7 days) old **and** the last prompt is over `RetryInterval`
  (1 day) old, so declining defers by a day rather than a week. A stamp in the future
  counts as elapsed — otherwise a clock that moved back suspends backups until real
  time catches up.
- `CloudBackupState` persists it as JSON beside the database, not as a column on
  PetSitter: the question is "when did *this device* last upload", which a restore
  carrying another device's answer would get wrong. Reads are forgiving — missing or
  corrupt reads as `Empty`, which schedules a backup rather than suppressing one.
  Parsed with `JsonDocument`, because this assembly is AOT-compiled for the iOS head.
- `CloudBackupService` (Viewmodel) builds the archive into a temp file and uploads
  from it — a cloud upload wants a length up front and may have to retry, neither of
  which a stream being zipped as it goes can offer. The upload stamp only moves on
  success; the prompt stamp moves either way.
- `CloudBackupStore` is the destination abstraction. The only implementation today is
  `LocalFolderBackupStore`, a **stand-in** until there is a Google Drive OAuth client
  id. It sits in `Infrastructure/Services/` rather than `View/Services/` with the
  other implementations of Viewmodel abstractions, because it needs `AppStorage` and
  the View layer deliberately does not reference the data layer. The Drive store will
  need a TopLevel for the sign-in browser, so that one does belong in View.
- The prompt lives on **`MainView`**, not Perfil, so it can appear over whichever tab
  the sitter actually opened; its `ConfirmDialog` carries `Grid.RowSpan="2"` to cover
  the navigation bar. `MainViewModel.OnRunStarting` fires the check and does not await
  it. The prompted run reports nothing on success — Perfil is where backup state is on
  show, and where `SendCloudBackupCommand` runs one by hand and reports properly.

Everything is stored under one name (`patas-backup.zip`) and replaced each run: one
file that is always the newest, rather than a folder growing by a database and every
photo every week. The cost is that a corrupted database uploaded over the only copy leaves
nothing to fall back on — a second "anterior" slot is the cheap mitigation if that
ever matters.

---

Related: `references/styling-design-canvas.md` (`ConfirmDialog` placement) and
`references/data-layer-schema.md` (`DapperDatabaseService`/`DatabasePath`).
