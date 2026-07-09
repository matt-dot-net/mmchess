# mmchess Move-Ordering Bench Log

Fixed-depth benchmark via the `bench <epdfile> [depth]` command (see
`CommandProcessor.cs`). Runs every position in the file at a fixed search
depth and reports aggregate search-efficiency metrics - `FirstMoveFH%` above
all for move ordering, since that's the fraction of fail-highs found on the
very first move tried at a node (higher = less wasted search from better move
ordering).

Compare entries at the **same depth** only - node counts aren't linearly
comparable across depths, and Knps varies by machine. FirstMoveFH% and node
counts should be reproducible across machines since search is deterministic
and single-threaded. When a change deliberately removes bad branches (for
example legality-filtering qsearch captures), total nodes can go down while
each node gets more expensive, so compare `Elapsed` first, then Knps and node
count together.

## 2026-07-06 - current baselines

```
bench wac.epd 7
Bench: 300 positions at depth 7
Nodes=12889420, QNodes=8090156, Elapsed=9.512s, Knps=1355
FirstMoveFH%=94.73, KillerFH%=17.20, TTFH%=24.66, FailHigh=1206716
NullMoveTries=265000, NullMoveFH%=60.55, MateThreats=272, LMRResearch=2545, FPrune=2325123, EFPrune=459652

bench wac.epd 10
Bench: 300 positions at depth 10
Nodes=72223971, QNodes=45134583, Elapsed=41.614s, Knps=1736
FirstMoveFH%=95.45, KillerFH%=17.11, TTFH%=27.09, FailHigh=6754469
NullMoveTries=1670820, NullMoveFH%=65.35, MateThreats=2560, LMRResearch=10148, FPrune=14043421, EFPrune=3447234
```

## 2026-07-09 - pre-king-safety checkpoint

Latest depth-10 checkpoint before adding `EvaluateKingSafety`. Search shape is
identical to the 2026-07-06 depth-10 baseline (same nodes/fail-high metrics),
but elapsed time improved from 41.614s to 28.042s and Knps from 1736 to 2576.

```
bench wac.epd 10
Bench: 300 positions at depth 10
Nodes=72223971, QNodes=45134583, Elapsed=28.042s, Knps=2576
FirstMoveFH%=95.45, KillerFH%=17.11, TTFH%=27.09, FailHigh=6754469
NullMoveTries=1670820, NullMoveFH%=65.35, MateThreats=2560, LMRResearch=10148, FPrune=14043421, EFPrune=3447234
```
