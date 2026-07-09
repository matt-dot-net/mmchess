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
Status (2026-07-09): core UCI play works. The important distinction:
`option name ...` lines are only for GUI-configurable engine features, not for
every UCI command we parse. Advertise only features that actually work.

Advertised UCI identity/options:
- `uci`: emits `id name mmchess <version>`, `id author Matt McKnight`,
  then the supported option list, then `uciok`. xboard equivalent: `protover`
  advertises `myname`, but CECP has no standard author field.
- `option name Hash type spin default 512 min 1 max 4096`: resizes the
  transposition table via `setoption name Hash value N`. xboard equivalent:
  `memory N` via `feature memory=1`.
- `option name Clear Hash type button`: clears the transposition table and pawn
  hash via `setoption name Clear Hash`. No direct xboard equivalent except
  resizing memory or starting a fresh engine.
- `option name Ponder type check default false`: toggles pondering via
  `setoption name Ponder value true|false`. xboard equivalent: `hard`/`easy`.

Implemented UCI protocol:
- `isready`: emits `readyok`. xboard equivalent: none needed; `ping` is the
  closest CECP readiness command and is not implemented yet.
- `ucinewgame`: resets board/search game state. xboard equivalent: `new`.
- `position startpos|fen ... moves ...`: sets a UCI position and applies
  coordinate moves. xboard equivalent: `setboard` plus coordinate moves.
- `go depth`, `go movetime`, `go wtime/btime/winc/binc/movestogo`,
  `go infinite`: starts search with the requested limit. xboard equivalents:
  `sd`, `st`, `level`, and force/go mode, but not a one-to-one mapping.
- `go ponder` + `ponderhit`: Cute Chess will only send these after the engine
  advertises `Ponder`; UCI now returns `bestmove <move> ponder <reply>` when a
  ponder move is available, runs ponder searches without spending clock, and
  starts the clock after `ponderhit`. xboard equivalent: `hard`/`easy` plus
  current move-input ponder-hit detection.
- `stop`: stops the active search. xboard equivalent: `?` move-now.
- `quit`: exits.
- Search output: emits `info depth ... score cp ... time ... nodes ... nps
  ... pv ...` and final `bestmove <move>`. xboard equivalent: `post` thinking
  output and `move <san>`.

Remaining UCI protocol commands / args:
- `debug on|off`: parse and store a debug flag. No xboard equivalent.
- `register ...`: parse and ignore with a harmless success/no-op path, since
  mmchess is not a registering engine. No xboard equivalent.
- `go searchmoves <moves...>`: restrict root search to a move subset. No direct
  xboard equivalent.
- `go nodes N`: stop after a node budget. No standard xboard equivalent.
- `go mate N`: search for mate within N moves. No direct xboard equivalent.
- `go binc/winc` currently truncates millisecond increments to whole seconds;
  preserve sub-second increments for fast UCI time controls.
- UCI mate scores: emit `score mate N` for mate values instead of always
  `score cp`. xboard equivalent: engine-specific thinking text only.
- Optional richer `info`: `currmove`, `currmovenumber`, `hashfull`, `seldepth`,
  `multipv`, `tbhits`, and `string` diagnostics where useful. xboard
  equivalent: mostly engine-specific `post` text.
- `copyprotection` / `registration`: explicitly omit unless the engine ever
  needs them; they are not appropriate for this project today.

Future UCI options to advertise only when the related feature exists:
- `option name Threads type spin ...`: add with Lazy SMP (#2). No xboard
  equivalent currently.
- `option name MultiPV type spin ...` and `UCI_AnalyseMode type check`: add
  with MultiPV / analysis mode (#7). No xboard equivalent currently.
- `option name SyzygyPath type string ...`: add with tablebases (#1). No
  xboard equivalent currently.
- `UCI_Chess960`: do not advertise unless Chess960 castling support is added.

## 4. Pondering
DONE (2026-07-08): xboard `hard`/`easy` now toggle pondering. After our move,
the normal PV reply is tracked as `PonderMove`; while waiting for the opponent,
the main thread makes that predicted reply on the real board and searches our
answer from the resulting position, warming the shared transposition table
without cloning board state or running a background search thread. Coordinate
input records ponder-hit/miss counters: on hit the speculative move stays made,
on miss it is unmade before the actual move is applied. With `post` enabled,
ponder thinking output is visible and the assumed reply is emitted as `Hint:`.
If the normal PV did not leave a reply to ponder, the wait loop first performs
a shallow search from the opponent-to-move position to choose one before making
it speculatively.

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
DONE (2026-07-06): `Move` is now a `struct` with a `Move.Null` sentinel
(`Value == 0`, never a real move) + `==`/`!=`/`Equals` operators replacing
reference-null checks. `HistoryMove` is no longer a subclass (structs can't be a
base type) - it's a struct composing a `Move Move` field plus its undo state,
stored inline in the History list (no per-make heap alloc). `Board.UpdateCapture`
now takes `ref HistoryMove` since the struct is mutated during make. All null
sites converted (search killers/PV/bestMove, ParseMove returns `Move.Null`,
command MoveInput). Every generated move (`new Move`) is now a stack value;
move-list and killer/PV allocations are gone too.
Correctness: full suite green (110), incl. HashKeyTests/PawnHashKeyTests
make/unmake round-trips.
Strength confirmed: +69.0 +/- 94.6 (28-18-5, LOS 93.0%, 51 games, 10s+0.1s),
an incremental win stacked on top of the TT struct change (#9). No correctness
regressions in play (symmetric mate-based results, no forfeits).
Note the per-node `new List<Move>()` (#4) still remains.

## 2. Per-node LINQ OrderByDescending + capturing lambda (quick, independent)
DONE (2026-07-06): `Search` and `Quiesce` no longer call
`.OrderByDescending(...)` at every node/qnode. Both now score moves once into
small parallel `Span<int>` buffers (stackalloc up to 256 moves, heap fallback
above that) and use in-place stable insertion sort, preserving the old
descending order and generator-order tie behavior without the LINQ closure,
OrderedEnumerable, sort buffer, and enumerator allocations. Root move ordering
still uses LINQ, but it is outside the per-node hot path.
Correctness: full suite green (110).
Bench baseline reset in BENCH.md after this change: `bench wac.epd 7`
`Nodes=12889420, QNodes=8090156, Elapsed=9.512s, Knps=1355`;
`bench wac.epd 10` `Nodes=72223971, QNodes=45134583, Elapsed=41.614s,
Knps=1736`.

## 3. `new HistoryMove(...)` per MakeMove
DONE (2026-07-06): `HistoryMove` was already converted to a struct as part of
#1, so `new HistoryMove(...)` is no longer a per-make heap allocation. Finished
the remaining hot-path piece by replacing `GameHistory`'s `List<HistoryMove>`
and pawn/capture `List<int>` with preallocated stack-like arrays that grow only
if an unusually long game/search exceeds the initial capacity. Make/unmake now
pushes and pops inline history entries without `List<T>` capacity churn.

## 4. `new List<Move>()` per movegen call
DONE (2026-07-06): `AlphaBeta` now owns reusable `List<Move>` buffers indexed
by `Ply`, so recursive search and quiescence each write into their own cleared
per-ply buffer instead of allocating a fresh move list per node/qnode.
`MoveGenerator` keeps the old allocating APIs for non-search callers, but now
also exposes append-into-existing-list overloads used by the hot search path.
Removed two dead helper-list allocations inside movegen while here.
Correctness: full suite green (110).

## 5. `PawnScore` is a class holding a ulong[2,8] array
DONE (2026-07-06): `PawnScore` is now a struct with inline per-file storage
instead of a heap object wrapping a `ulong[2,8]` array. Pawn hash probing now
uses `TryProbe(out PawnScore)`, so cache hits return the stored value without
allocating a fresh result object, and `EvaluatePawns` updates the returned
struct copy's side-to-move-relative eval without mutating the cached entry.

## 6. Replace rectangular arrays in hot paths
Audit remaining `[,]`/`[,,]` arrays and replace performance-sensitive ones
with flat arrays plus explicit index helpers. .NET rectangular array indexing
has extra bounds/rank overhead compared with single-dimensional arrays, and the
search still uses these shapes for data like principal variation, killers, and
history. Do this one structure at a time and bench each change independently,
since some arrays are tiny or cold enough that readability may be worth keeping.

## 7. Flatten per-side board bitboards
Replace `Board`'s separate two-element arrays (`Pawns`, `Knights`, `Bishops`,
`Rooks`, `Queens`, `King`, `Pieces`) with inline board storage so each `Board`
does not own seven tiny heap arrays and hot accesses avoid array-object
indirection. For maximum performance, prefer explicit fields (`WhiteRooks`,
`BlackRooks`, etc.) or source-generated side-specific helpers over a polite
`ulong[14]` middle ground if benchmarks show the field version wins. This will
touch `Board`, `MoveGenerator`, `Evaluator`, hashing, SEE, notation, and tests,
so do it as its own isolated branch with a full correctness run and a
before/after bench.

## 8. Move buffers should live in per-thread search context
Current move generation uses stack-backed `MoveList` buffers, which removes the
old heap `List<Move>` churn but still requires each search/movegen call to
declare its own local buffer. When implementing Lazy SMP (#2), introduce a
`SearchState`/`Tree` context object per search thread and move these buffers
there: per-ply full-width move buffers, quiescence buffers, and ordering score
scratch space. Do not make this process-global mutable state - SMP needs one
context per worker thread so move lists, killers/history, ply state, and metrics
cannot race or bleed between searches.

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
