---
name: backup-restore-export
description: How backup/restore (BackupArchive, dog photos) and file export/save dialogs work, including the Android save-dialog naming quirk. Use when touching export/import, "the database" as a whole, or any FileExportDialog/save-dialog code.
---

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
