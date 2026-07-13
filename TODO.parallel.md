# Parallel Search Plan

## Goal

Add parallel alpha-beta search without treating recursive search as a collection
of independent TPL tasks. The search needs explicit split and join operations,
bounded long-lived workers, and re-search rules for speculative results whose
search window is stale by the time they return.

The intended starting model is Young Brothers Wait Concept (YBWC): search the
first legal move synchronously to establish a useful alpha, then allow remaining
siblings to be searched speculatively with zero-width windows.

## Correctness Model

The current search is negamax with principal variation search (PVS). After the
first move, a sibling is normally searched with a scout window. Parallel work
must preserve that behavior.

A worker result is not simply a score. It is a bound established using the
alpha/window snapshot supplied to that worker. The join operation must interpret
the result using that snapshot and the parent's current alpha.

For a parent scout window:

```text
[alphaSnapshot, alphaSnapshot + 1]
```

the child negamax search uses:

```text
[-alphaSnapshot - 1, -alphaSnapshot]
```

Worker results should be classified explicitly:

- `Upper`: the move failed low and cannot beat `alphaSnapshot`.
- `Lower`: the move failed high and may beat `alphaSnapshot`.
- `Exact`: the result came from an authoritative full-window search.

Workers should initially perform scout searches only. The split-point owner
performs authoritative re-searches because it knows the current parent alpha.

## Scheduler and Split Points

Use a fixed collection of long-lived workers and a bounded work queue. Do not
create a `Task` for every node or use nested `Parallel.ForEach` calls.

The initial shape should be similar to:

```csharp
sealed class SearchScheduler
{
    public bool HasIdleWorker { get; }
    public bool TrySchedule(SplitWork work);
    public void Cancel(SplitPoint splitPoint);
}

sealed class SplitPoint
{
    public int Alpha;
    public readonly int Beta;
    public bool Cutoff;
    public int PendingWorkers;
    public readonly List<SplitResult> Results;
}

readonly struct SplitWork
{
    public Board Board;
    public AlphaBetaContext Context;
    public Move Move;
    public int Depth;
    public int AlphaSnapshot;
    public int Beta;
    public int Reduction;
}

readonly struct SplitResult
{
    public Move Move;
    public int Score;
    public SearchBound Bound;
    public int AlphaSnapshot;
    public int Reduction;
    public Move[] Pv;
    public AlphaBetaMetrics Metrics;
}
```

The exact types can change during implementation, but the following ownership
rules should remain:

- A worker never mutates the parent's board.
- A worker has private PV, killer, history-heuristic, and metrics storage.
- Shared tables and the global stop signal must be thread-safe.
- Only the split-point owner updates the authoritative alpha, PV, heuristics,
  and node result.
- Only the main search owner writes thinking/UCI output and the root result.

## Context and Board Cloning

Add deliberate search-cloning APIs rather than relying on shallow struct copies:

```csharp
Board Board.CloneForSearch();
AlphaBetaContext AlphaBetaContext.Split(Board clonedBoard);
void AlphaBetaContext.Join(in SplitResult result);
```

`Board(Board)` currently creates an empty `GameHistory`. That is insufficient
for worker searches because repetition and fifty-move detection depend on the
complete relevant history. Add copy support to `GameHistory` and ensure a split
board carries the same history as its parent at the split position.

`AlphaBetaContext.Split()` should:

- Clone the board and its history.
- Preserve `Ply`.
- Allocate private PV arrays and metrics.
- Initially copy killers and history heuristics.
- Use the shared, thread-safe cancellation state.

For the first implementation, do not merge worker killer/history-heuristic
changes. Losing those updates may affect performance, but cannot change search
correctness. Metrics should be accumulated during join, and the PV should be
imported only when the result becomes authoritative.

## When to Split

Never split before the first legal child has completed its full search. The
first result establishes alpha and provides the move-ordering benefit on which
PVS depends.

A node is eligible to split only when:

- At least one legal move has completed.
- At least two unsearched moves remain.
- A worker is idle.
- The remaining depth meets `MinSplitDepth`.
- The node is not in quiescence search.
- Search cancellation/time-up has not been requested.
- The configured split nesting limit has not been reached.
- The estimated remaining work is greater than cloning and scheduling cost.

Use a conservative initial policy:

```text
minimum depth:       6
minimum moves left:  3
maximum split depth: one active split level
```

Tune these values from benchmarks after correctness is established.

Move-specific extension, LMR, and futility decisions must be made consistently
with the sequential loop. A work item must carry enough information to reproduce
the depth and reduction that the move would have received sequentially.

## Join and Re-search Rules

Assume a worker searched a move against `alphaSnapshot`, while the owner now has
`currentAlpha`, where `currentAlpha >= alphaSnapshot`.

### Failed-low result

If the worker score did not exceed `alphaSnapshot`, the result is still safe to
discard. A move that could not beat the older alpha cannot beat the newer alpha.
No re-search is required.

### Failed-high result

If the worker score exceeded `alphaSnapshot`, it is only a candidate. A scout
fail-high is a lower bound, not an exact score.

The owner should:

1. Re-scout the move against `currentAlpha` if alpha has changed.
2. Discard the move if it now fails low.
3. Search it with the parent's full window if it still exceeds current alpha.
4. If the original search was reduced and the move still improves alpha,
   re-search at unreduced depth, preserving the current LMR behavior.
5. Update alpha, PV, TT, and heuristics only from the authoritative search.
6. If the score reaches beta, close the split point and cancel or abandon all
   outstanding sibling work.

Conceptually:

```csharp
if (result.Score <= result.AlphaSnapshot)
{
    AcceptFailLow(result);
}
else
{
    var score = SearchScoutAtCurrentAlpha(result.Move);

    if (score > alpha)
    {
        score = SearchFullWindow(result.Move);

        if (score > alpha && result.Reduction > 0)
            score = SearchUnreduced(result.Move);
    }

    ApplyAuthoritativeResult(result.Move, score);
}
```

Do not treat a large fail-soft worker score as exact.

Completed work does not have to be joined in original move order for
correctness. For efficiency, process results in this order:

1. Apparent beta-cutoff candidates.
2. Other fail-high candidates, ordered by descending worker score.
3. Proven fail-low results.

The owner should continue useful local search while workers are active and wait
only when no local sibling work remains.

## Shared-State Prerequisites

Parallel search exposes races in currently shared caches and counters.

### Transposition table

`TranspositionTableEntry` is 16 bytes. Reads and writes of the complete entry are
not guaranteed to be atomic, so a probe can observe a torn entry during a
concurrent store.

Begin with a correctness-first implementation such as lock striping around
bucket access. A later optimization may use a safely published version/generation
scheme, but it must prove that a reader cannot accept fields from different
writes as one valid entry.

TT `Hits`, `Probes`, and `Stores` must use thread-local accounting or atomic
updates.

### Pawn hash table

Pawn hash entries are immutable objects published through references, which is
safer than the inline TT entry. Its counters still race and need thread-local or
atomic accounting. Publication behavior should also be made explicit.

### Cancellation and time control

Replace worker mutation/polling of ordinary `GameState.TimeUp` state with a
thread-safe shared search-stop object. Workers should check it through the
existing scheduled interrupt mechanism and before accepting new work.

On a beta cutoff, a split point is marked closed. Queued work for it should be
skipped, while running workers should abandon it at their next scheduled stop
check. Search-wide cancellation must also stop all split points before the
search returns.

## Implementation Phases

### Phase 1: Correct cloning (complete)

- [x] Add `GameHistory` copy support.
- [x] Add `Board.CloneForSearch()`.
- [x] Add `AlphaBetaContext.Split()` with private mutable search state.
- [x] Test that cloned state has identical hashes, history, draw decisions, and
  legal moves.

### Phase 2: Thread-safe shared state (complete)

- [x] Make TT probes/stores safe under concurrent access using Hyatt/Mann
  XOR-key validation and atomic 64-bit word access.
- [x] Make pawn-hash publication explicit and counters safe.
- [x] Introduce a shared search-stop object.
- [x] Separate TT statistics into per-context metrics; keep pawn-table global
  counters atomic because pawn probes do not currently receive a search context.

### Phase 3: Extract sibling-search operations

- Refactor the current PVS/LMR sequence into reusable scout, full-window, and
  unreduced re-search operations.
- Preserve the existing sequential behavior before adding concurrency.
- Add focused tests for the extracted re-search state machine.

### Phase 4: Root-only split/join

- [x] Build the bounded worker scheduler.
  - Use long-lived background workers and a bounded, non-blocking queue.
  - Treat the configured thread count as total search threads: the search
    owner is one thread and the scheduler owns `ThreadCount - 1` workers.
  - UCI advertises `Threads` and accepts
    `setoption name Threads value N`.
  - XBoard advertises `feature smp=1` and accepts `cores N`.
  - `Threads=1` or `cores 1` disables SMP and creates no workers.
- [ ] Search the first root move synchronously.
- [ ] Scout remaining root moves on workers.
- [ ] Perform all authoritative re-search and PV updates on the owner.
- [ ] Implement cutoff cancellation and clean worker shutdown.

Root splitting is the first milestone because it validates scheduling, cloning,
stale-window handling, PV joining, metrics, and cancellation with simple
ownership.

### Phase 5: Internal YBWC split points

- Add the conservative eligibility policy to internal nodes.
- Let the owner continue searching siblings while workers run.
- Enforce the initial split-nesting limit.
- Reuse exactly the same result classification and owner re-search rules as the
  root implementation.

### Phase 6: Tune and optimize

- Benchmark fixed positions at fixed depth with 1, 2, 4, and 8 workers.
- Tune minimum depth, moves remaining, nesting, and queue size.
- Consider work stealing or faster TT synchronization only after profiling.
- Evaluate whether and how to merge worker heuristic updates.

## Metrics to Add

Track enough data to distinguish useful parallelism from node inflation:

- Split points created and declined.
- Work items scheduled, started, completed, skipped, and cancelled.
- Worker fail-lows.
- Worker fail-high candidates.
- Candidates invalidated by a newer alpha.
- Full-window re-searches.
- Unreduced LMR re-searches.
- Parallel beta cutoffs.
- Board/context clone time.
- Owner wait time.
- Total nodes, elapsed time, and NPS by worker count.

## Required Tests

- One-worker parallel mode returns the same move, score, and PV as sequential
  mode.
- Multi-worker mode returns the same move and score across repeated runs.
- A fail-low against an older alpha is accepted without re-search.
- A fail-high against an older alpha is re-scouted at current alpha.
- A reduced fail-high is eventually searched at unreduced depth.
- A joined beta cutoff closes the split point and cancels outstanding siblings.
- Split boards preserve repetition and fifty-move behavior.
- Parent board, hash, history, and ply are unchanged after split/join.
- Worker PV and heuristic updates cannot corrupt the parent context.
- TT probes never accept torn entries under concurrent writers.
- Search time cancellation does not leave workers running against a completed
  search.
- Single-thread mode preserves existing node counts wherever the refactoring is
  intended to be behavior-neutral.

## Initial Definition of Done

The first usable version is complete when root-only splitting:

- Uses bounded long-lived workers.
- Produces the same best move and score as sequential search.
- Correctly re-searches stale fail-high results.
- Preserves repetition and fifty-move detection in worker boards.
- Has a thread-safe TT and stop mechanism.
- Shuts down or reuses all workers cleanly after every search.
- Demonstrates a repeatable speedup on the fixed-depth benchmark suite without
  unacceptable node inflation.

Internal split points should be built only after this milestone is stable.
