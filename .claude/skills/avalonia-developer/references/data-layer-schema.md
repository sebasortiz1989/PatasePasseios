# Data layer & schema changes

Dapper over SQLite. `DapperDatabaseService` is a DI singleton that, in its
constructor, calls `SQLitePCL.Batteries.Init()`, resolves the app-data folder via
`AppStorage`, creates `DapperDemo.db`, runs the schema, and inserts a mock
`test@test.com` / `8998` pet sitter. It exposes `Connection` as a **new**
`SqliteConnection` per access — callers `using` and open it themselves — plus
`DatabasePath` for backup.

The namespace is `PatasePasseios.Repository.Dapper`, but the folder and the file are still
called `DapperDemo` — deliberately, since renaming either hides a sitter's live database
and every dog photo. See the deviations table in the root `CLAUDE.md`.

**The canonical schema is the DDL in `DapperDatabaseService`.** DTOs mirror it
and carry the matching `CREATE TABLE` in a trailing comment; keep both in step.

## Schema versioning has two paths, and picking the wrong one destroys data

- A **new table** needs nothing. Every statement is `CREATE TABLE IF NOT EXISTS`,
  so it appears on the next launch with existing data untouched.
- A **new column** on an existing table needs `AddColumnIfMissing`. Bumping
  `SchemaVersion` drops every table and is only for a genuinely incompatible
  layout.

## Services span four tables

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

`UpdateAsync` writes inside a transaction and **commits only when exactly one row changed** —
zero means the booking is gone and is reported as `Response.Failed` rather than as a save that
kept nothing; anything above one is rolled back. Every statement is keyed on its table's primary
key, so more than one is impossible as written; the check is there because a silent multi-row
write is the failure nobody sees until the records are.

**Deleting a dog or tutor cascades by hand** — `RepositoryDogs.Delete` and
`RepositoryTutors.Delete` each `DELETE FROM` all four service tables in a
transaction, plus the payment-ledger rows hanging off them. Add a fifth service
table and you must add it to both, or orphaned rows silently accumulate.

---

Related: `references/money-payments-credit.md` (the payment-ledger rows these deletes must also
clear).
