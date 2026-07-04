using Xunit;

// TranspositionTable.Instance is a process-wide singleton with unsynchronized
// mutable state (SearchId, the table itself). xUnit parallelizes different
// test classes across threads by default, which caused intermittent failures
// in tests that assert on the singleton's exact state (e.g. NextSearchId
// cycling) whenever another test touching it - directly, or indirectly via
// Iterate.DoIterate - happened to run concurrently. Disabling parallelization
// makes the whole suite deterministic.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
