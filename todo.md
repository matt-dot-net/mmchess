# mmchess TODO — Major Missing Features

## 1. Endgame tablebases (Syzygy)
Probe WDL tables at search nodes and DTZ tables at the root for perfect play
with ≤7 (practically ≤5/6) pieces. Requires a probing library or a C# port
(e.g. wrapping Fathom) and a `SyzygyPath`-style option.

## 2. Parallel search (Lazy SMP)
Run multiple search threads sharing the transposition table. Prerequisite:
extract per-search state (killers, history, ply stack, metrics) off the
`AlphaBeta` instance fields into a per-thread search context — see the
existing TPL search context plan.

## 3. Full UCI support
Only the `uci` handshake exists today; the engine is effectively xboard-only.
Implement `position`, `go` (wtime/btime/movestogo/movetime/infinite),
`stop`, `setoption`, `info` output (depth/score/pv/nps), and `isready`.
Opens the door to standard GUIs and testing tools (cutechess-cli, fastchess).

## 4. Pondering
Think on the opponent's time (`hard`/`easy` are currently no-ops). Predict
the reply, search it while waiting, and handle ponder-hit vs ponder-miss.

## 5. Stronger evaluation: texel tuning, then NNUE
Near term: automated tuning of the existing hand-crafted eval terms against
a labeled position set (texel tuning). Long term: an NNUE network with
incremental updates in Make/UnmakeMove, which is where all top engines get
their strength today.

## 6. Endgame knowledge: winnability detection and draw scaling
Mostly DONE (2026-07-05): winnability caps in `Evaluate` (a side that can't
mate is capped at draw, incl. KNN; a side whose opponent can't mate is
floored at draw — fixed the bug where KR vs K scored 0 with the bare-king
side to move), wrong-rook-pawn fortress and opposite-colored-bishop scaling,
`Board.IsInsufficientMaterial` wired into the search draw checks, and
drive-to-corner mate-conversion eval (edge-driving + king proximity +
KBN corner color). Covered by WinnabilityTests, EndgameScalingTests,
InsufficientMaterialTests, MateConversionTests, EndgameConversionTests.

Strength confirmed (2026-07-06): +41.9 Elo (90-66-44, LOS 97.3%, 200
games) vs the parser-fixed baseline, after the pawn-ending mate-conversion
hijack fix (commit 0dda47a).

Remaining: the conversion term weights (20/10/20) and the OCB 50% scale
are untuned guesses — fold into the texel-tuning task (#5).

## 7. MultiPV + analysis mode
Report the top N lines and support infinite analysis, making the engine
usable as an analysis tool rather than only a game player.

## 8. One-legal move no search at root
DONE (2026-07-06): `Iterate.DoIterate` short-circuits when
`MoveGenerator.GenerateLegalMoves` returns exactly one move - it plays that
move immediately without searching or spending the clock. (Zero legal moves
falls through to the normal path, which returns no move.) Covered by
SingleLegalMoveTests.

## 9. Tune hashtable size / make configurable
DONE (2026-07-06): configurable via xboard `memory N` (feature memory=1) and
UCI `setoption name Hash value N` (advertised as `option name Hash type spin
default 512 min 1 max 4096`), routed through `TranspositionTable.SetSize`/
`Resize`. Also converted `TranspositionTableEntry` from a class to a struct so
the table is a contiguous inline array (no per-entry heap object, no GC churn),
which made `memory N` an honest N megabytes instead of the old nominal "512 MB"
that grew to multiple GB. Default 512 MB. Covered by TranspositionTableTests +
CommandProcessorTests.

Strength confirmed: the struct conversion was a large A/B win on its own -
+134.5 Elo +/- 60.8 (74-29-19, LOS 100.0%, 122 games, 10s+0.1s) vs the
class-entry baseline. This kicked off the runtime/allocation-perf audit below.

# Runtime / allocation performance

The class-to-struct win on the TT entry (#9) revealed that the same anti-pattern
- small value-like data wrapped in heap `class` objects, allocated in the hot
search/movegen path - is present elsewhere and often hit far more often. Do
these ONE AT A TIME so each shows up cleanly in an A/B tournament.

## 1. `Move` is a class wrapping a 4-byte uint (headline)
`Move` (src/mmchess/Move.cs) is `[StructLayout(Size=4)] class` whose whole
payload is one `uint Value`, yet it is a heap object. Every generated move is a
`new Move()` (~20 sites in MoveGenerator.cs) and `GenerateMoves`/
`GenerateQuiescenceMoves` also `new List<Move>()` per node - hundreds of
millions of allocations per search. Likely a bigger win than the TT change since
movegen frequency exceeds TT stores.
Blockers/effort (larger, riskier refactor): `HistoryMove : Move` inheritance
must be reworked (structs can't be a base class - embed a `Move` value + its own
fields); pervasive `null` semantics need a sentinel (`Value == 0` = "no move" is
safe - a1a1 is never legal) or `Move?` at absence sites (`ParseMove`, `bestMove
== null`, `Killers[..] != null`). Cascades: killers, move lists, and HistoryMove
all stop allocating once Move is a struct.

## 2. Per-node LINQ OrderByDescending + capturing lambda (quick, independent)
AlphaBeta.cs:297 (Search) and Quiesce.cs:80 (Quiesce) call
`.OrderByDescending(m => OrderMove(m, ...))` at every node/qnode - each allocates
a closure (captures hasEntry/entry/inCheck), an OrderedEnumerable, a sort buffer,
and an enumerator. Replace with: score each move into a small parallel array once
and do an in-place insertion/selection sort. Self-contained, low-risk, and
independently A/B-measurable. Recommended starting point.

## 3. `new HistoryMove(...)` per MakeMove
Board.cs:309 allocates a HistoryMove on every make. `History[]` is already
ply-indexed, so making HistoryMove a struct in a preallocated array removes it.
Folds naturally into #1.

## 4. `new List<Move>()` per movegen call
Tie to #1/#2: a reusable move buffer indexed by Ply removes the per-node list
allocation. Needs care - each ply/recursion level needs its own buffer.

## 5. `PawnScore` is a class holding a ulong[2,8] array
PawnScore.cs. Lower frequency (cached in the pawn hash) but each store allocates
the object AND the 8x2 array. Smaller win; convert to a struct with inline
storage.

# Search / performance bugs costing Elo

## 1. LMR reduction value leaks between moves
`lmr` is declared outside the move loop in `Search`, so once a late quiet move
sets a reduction, later moves that should not be reduced can inherit it. Reset
`lmr` to 0 inside each loop iteration before applying LMR conditions.

Experiment status: patch lives on branch `lmr-leak`, but do not commit it to
main at this time. The patch is semantically cleaner, but it materially slows
the search and did not show a clear strength gain in a small match.

Bench result for `lmr-leak`:

```
bench wac.epd 7
Bench: 300 positions at depth 7
Nodes=15238486, QNodes=9500837, Elapsed=12.511s, Knps=1218
FirstMoveFH%=95.49, KillerFH%=17.41, TTFH%=24.84, FailHigh=1493262
NullMoveTries=337988, NullMoveFH%=59.57, MateThreats=267, LMRResearch=2075, FPrune=2728805, EFPrune=879387
```

Compared with the qsearch-legality baseline
(`Nodes=12912911, QNodes=8100831, Elapsed=10.843s`), this is about +18%
nodes and +15% elapsed time. 50-game self-play against `mmchess 0.1.226`
finished `17 - 19 - 14` for `lmr-leak` (`0.480`, Elo `-13.9 +/- 83.2`,
LOS `36.9%`). That sample is too small to prove weakness, but it is enough
to avoid treating the cleanup as an obvious Elo-positive bug fix.

Possible follow-up: revisit LMR as a tuning task instead of a pure bug fix.
Keep per-move reduction semantics, but tune which checks, captures, and
passed-pawn pushes are fully exempt versus merely reduced less.
