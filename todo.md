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

## 6. Endgame knowledge / scaling
Recognize drawn material configurations (KB vs K, wrong-rook-pawn, opposite
bishops), insufficient-material draws, and add drive-to-corner logic for
basic mates so won endings actually convert without tablebases.

## 7. MultiPV + analysis mode
Report the top N lines and support infinite analysis, making the engine
usable as an analysis tool rather than only a game player.

# Search / performance bugs costing Elo

## 1. LMR reduction value leaks between moves
`lmr` is declared outside the move loop in `Search`, so once a late quiet move
sets a reduction, later moves that should not be reduced can inherit it. Reset
`lmr` to 0 inside each loop iteration before applying LMR conditions.

## 2. Castling generation misses/permits bad queenside castles
`GenerateCastleMoves` uses `else if` for queenside castling, so queenside is
skipped whenever kingside rights also exist. The black queenside attack check
also tests white-side squares 59/58 instead of black-side squares 3/2.
