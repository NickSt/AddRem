using Xunit.Sdk;
using Xunit.v3;

// These tests read the real machine, and some of them assert on a process-wide count
// of open registry keys. Running classes in parallel would let one class's open
// handles be observed by another's assertions, so the whole assembly runs
// sequentially. It is I/O bound against a single machine anyway.
[assembly: Parallelization(Mode = ParallelMode.None)]
