using Xunit;

// Every test class here opens SQLite, and both TestDatabase.Dispose and BackupArchive's restore
// call SqliteConnection.ClearAllPools() — which is process-wide, not per-connection. Under xUnit's
// default parallelism that means one class finishing a test can close pooled connections another
// class is in the middle of using, which surfaced as RepositoryPaymentsTests failing intermittently
// with a different test each run while passing in isolation.
//
// Serialised rather than grouped into one collection because there is no subset that is safe to run
// concurrently: the pool is shared by the whole process, so any two classes that touch a database
// can interfere. The suite is slower for it, and deterministic.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
