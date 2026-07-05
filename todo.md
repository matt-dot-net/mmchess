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
Treat "can this side actually win?" as a first-class eval concept, assuming
every opponent plays like a computer (no swindling — e.g. KN vs KP is at
best a draw for the knight side and should never evaluate as better).
Extend the existing `EvaluateWinners` canWin logic to cover insufficient
material (KNN vs K), cap the score at a draw for a side that cannot win
instead of hard-returning 0, scale down known-drawish configurations
(wrong-rook-pawn, opposite-colored bishops), and add drive-to-corner logic
for basic mates (KQ/KR/KBB/KBN) so won endings actually convert without
tablebases.

## 7. MultiPV + analysis mode
Report the top N lines and support infinite analysis, making the engine
usable as an analysis tool rather than only a game player.

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
