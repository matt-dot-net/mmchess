# mmchess

A chess engine written in C# (.NET 10).

## Why this project exists

mmchess is a **research and validation harness for alpha-beta chess-engine
algorithms and heuristics**. Its primary driver is to prototype, test, and
measure search and evaluation techniques in a fast-to-iterate, managed-code
environment before porting the ones that prove out into
**Dorky**, my main C/C++ engine.

Working in C# here buys quick iteration, an xUnit test suite around every
tricky rule and heuristic, and reproducible benchmarks — so a change can be
proven correct and measured for strength *before* the effort of a C/C++
implementation. When a technique earns its place here (via the move-ordering
bench and self-play), it graduates to Dorky.

## Features

### Board representation
- **Bitboards** (`ulong` per piece type per side) with **rotated bitboards**
  (R90 / R45 / L45) for sliding-piece attack generation without magics.
- Zobrist hashing for the main board and a separate **pawn hash key**.
- FEN parsing (`setboard` / UCI `position`), full make/unmake with incremental
  hash updates, en passant, castling, and promotion.

### Search (`AlphaBeta.cs`, `Iterate.cs`, `Quiesce.cs`)
- Iterative deepening with **aspiration windows** (fail-soft widening).
- **Principal Variation Search** (zero-window scout + re-search).
- **Transposition table** (PV / CUT / ALL entries, mate-score adjustment by ply).
- **Null-move pruning** with adaptive R and mate-threat detection.
- **Late Move Reductions** and **futility / extended futility pruning**.
- **Check extensions**.
- **Quiescence search** with SEE-based capture pruning.
- Move ordering: hash move → winning/even captures (MVV/LVA + **SEE**) →
  **killers** → **history heuristic** → losing captures.

### Evaluation (`Evaluator.cs`, `Evaluation.cs`, `PawnScore.cs`)
- Material and piece-square tables, game-phase aware.
- Pawn structure: passed, doubled, blocked (central-pawn) — cached in a
  **pawn hash table**.
- King safety (open files near a castled king, attack pressure), bishop pair,
  knight outposts, rooks on open files / the seventh, rook-development penalties.
- **Endgame knowledge**: winnability caps (a side that cannot mate is capped at
  a draw, incl. KNN; wrong-rook-pawn and opposite-colored-bishop draw scaling),
  `IsInsufficientMaterial` dead-position detection, and drive-to-corner
  **mate-conversion** scoring.

### Protocols & tooling
- **UCI** and **xboard / WinBoard** (CECP) compatible, including position
  setup, engine search commands, time controls, and configurable hash size.
- Time controls: per-game, moves-in-N, fixed time per move, fixed depth.
- `perft` / `perft <depth> parallel` for move-generation verification.
- `bench <epd> [depth]` — fixed-depth move-ordering benchmark reporting
  `FirstMoveFH%`, node counts, and pruning stats (see `BENCH.md`).
- `epdtest <file>` — timed EPD best-move test suite.

## Building & running

Requires the **.NET 10 SDK**.

```sh
# Build
dotnet build src/mmchess -c Release

# Run the engine (reads UCI or xboard/CECP commands on stdin)
dotnet run --project src/mmchess -c Release

# Run the test suite
dotnet test
```

The engine speaks both UCI and CECP/xboard, so it plugs into UCI-compatible
GUIs as well as WinBoard, XBoard, or cutechess-cli (which can also supply
opening books — there is no built-in book).

### Handy interactive commands
```
new                 # reset to the start position
setboard <fen>      # set an arbitrary position
go                  # let the engine move
eval                # print the static evaluation
perft 6             # or: perft 6 parallel
bench wac.epd 7     # move-ordering benchmark at depth 7
epdtest wac.epd     # 5s/position best-move test
```

## Project layout

| Path | Contents |
|------|----------|
| `src/mmchess/` | Engine: board, move generation, search, evaluation, protocol |
| `test/` | xUnit test suite (26 test files) covering rules and heuristics |
| `BENCH.md` | Move-ordering benchmark log and methodology |
| `todo.md` | Roadmap and known Elo-costing search bugs |

## Roadmap

See `todo.md` for the full list. Highlights: Lazy-SMP parallel search (needs
per-thread search context), Syzygy tablebases, texel tuning then NNUE, and
MultiPV / analysis mode.

## Author

Matt McKnight
