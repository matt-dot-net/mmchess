# mmchess Move-Ordering Bench Log

Fixed-depth benchmark via the `bench <epdfile> [depth]` command (see
`CommandProcessor.cs`). Runs every position in the file at a fixed search
depth (no wall clock) and reports aggregate search-efficiency metrics -
`FirstMoveFH%` above all, since that's the fraction of fail-highs found on
the very first move tried at a node (higher = less wasted search from better
move ordering).

Compare entries at the **same depth** only - node counts aren't linearly
comparable across depths, and Knps varies by machine. FirstMoveFH% and node
counts should be reproducible across machines since search is deterministic
and single-threaded.

## 2026-07-04 - baseline (post null-move mate-threat fix, pre history heuristic)

```
bench wac.epd 6
Bench: 300 positions at depth 6
Nodes=7325890, QNodes=4573751, Knps=570
FirstMoveFH%=93.73, KillerFH%=17.96, TTFH%=26.53, FailHigh=634950
NullMoveTries=110083, NullMoveFH%=51.19, MateThreats=126, LMRResearch=1949, FPrune=1093353, EFPrune=173065

bench wac.epd 7
Bench: 300 positions at depth 7
Nodes=13997687, QNodes=8734824, Knps=1393
FirstMoveFH%=94.17, KillerFH%=17.44, TTFH%=25.18, FailHigh=1285537
NullMoveTries=268086, NullMoveFH%=60.47, MateThreats=291, LMRResearch=2830, FPrune=2382910, EFPrune=464487
```

## 2026-07-04 - history heuristic ([piece][to] table, `[2,6,64]`, in AlphaBeta.OrderMove/UpdateKillers)

**Caveat on the first pass below:** `bench wac.epd 6` and `bench wac.epd 7`
were run back-to-back in the same engine process. `TranspositionTable` is a
process-lifetime singleton (`TranspositionTable.cs`) with no clear/reset -
`NextSearchId()` only biases replacement on a hash collision, it doesn't
invalidate anything, and `Read()` has no age check. Since `wac.epd` is the
same 300 positions every time, the depth-7 run inherited a fully warm cache
from the depth-6 run moments earlier - that's why its node count came in
*below* depth-6's, not any property of the search itself. The depth-6 number
below is still clean (first command in a fresh process each session).

```
bench wac.epd 6
Bench: 300 positions at depth 6
Nodes=6977284, QNodes=4368503, Knps=1196
FirstMoveFH%=94.39, KillerFH%=17.41, TTFH%=27.27, FailHigh=611361
NullMoveTries=107956, NullMoveFH%=51.13, MateThreats=123, LMRResearch=1535, FPrune=1077244, EFPrune=179685

bench wac.epd 7   <- contaminated by depth-6's warm TT, see caveat above
Bench: 300 positions at depth 7
Nodes=6682855, QNodes=4231533, Knps=1095
FirstMoveFH%=94.97, KillerFH%=17.20, TTFH%=22.31, FailHigh=638245
NullMoveTries=158766, NullMoveFH%=66.54, MateThreats=164, LMRResearch=939, FPrune=1275490, EFPrune=277544
```

**Clean depth-7 rerun (fresh process, first command run):**

```
bench wac.epd 7
Bench: 300 positions at depth 7
Nodes=13287674, QNodes=8322396, Knps=1199
FirstMoveFH%=94.74, KillerFH%=17.15, TTFH%=24.90, FailHigh=1244808
NullMoveTries=269143, NullMoveFH%=60.23, MateThreats=279, LMRResearch=2405, FPrune=2365510, EFPrune=456327
```

With the TT-carryover confound removed, both depths agree: FirstMoveFH% up
(93.73%→94.39% at depth 6, 94.17%→94.74% at depth 7) and node count down
(7.3M→7.0M at depth 6, 14.0M→13.3M at depth 7). History heuristic is a real,
if modest, move-ordering improvement at both depths - not an artifact of a
warm hash table.

**Process note going forward:** always run `bench` as the first command in a
fresh engine process (or restart between invocations) to get an
uncontaminated number - chaining multiple `bench` calls in one session only
warms the shared TT for whichever call goes second.
