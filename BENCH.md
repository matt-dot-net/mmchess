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

## 2026-07-04 - pawn hash table (`PawnHashTable`, caches doubled/passed pawn score by `Board.PawnHashKey`)

```
bench wac.epd 7
Bench: 300 positions at depth 7
Nodes=13287674, QNodes=8322396, Knps=1256
FirstMoveFH%=94.74, KillerFH%=17.15, TTFH%=24.90, FailHigh=1244808
NullMoveTries=269143, NullMoveFH%=60.23, MateThreats=279, LMRResearch=2405, FPrune=2365510, EFPrune=456327
```

Every search-behavior number here is identical to the history-heuristic
clean depth-7 run above (`Nodes`, `FirstMoveFH%`, `FailHigh`, `NullMoveTries`,
`MateThreats`, ...) - expected, since the pawn hash table doesn't change any
evaluation value or search decision, just skips recomputing the same
structural pawn score twice. Only `Knps` moved (1199→1256, ~+4.7%), which is
exactly the kind of change this feature should produce: pure throughput, zero
effect on determinism. Identical node counts across a 300-position, ~13M-node
run is itself a good correctness sanity check - if the pawn hash had a
caching bug that leaked stale values into eval, it would almost certainly
have perturbed some fail-high/pruning decision somewhere and changed the
node count.

## 2026-07-04 - aspiration widening: fail-soft jump (`Iterate.WidenBeta`/`WidenAlpha`)

Refactored the root aspiration re-search widening out of `DoIterate` into
testable helpers and made it jump straight past the returned fail-soft score
(`max(beta + 33*relax, score + 33)`, mirrored for alpha) instead of walking
the fixed `33 * 4^n` ladder out one full re-search at a time when the true
score lands far outside the window.

**New baseline note:** current HEAD (post blocked-central-pawn/unmoved-rook
eval penalties) benches at **12,959,969** nodes, not the 13,287,674 of the
pawn-hash entry above - the eval commit did shift search behavior, so future
comparisons at depth 7 should use the number below.

```
bench wac.epd 7   (before, HEAD)
Bench: 300 positions at depth 7
Nodes=12959969, QNodes=8146215, Knps=1233
FirstMoveFH%=94.64, KillerFH%=17.30, TTFH%=24.62, FailHigh=1209280
NullMoveTries=265664, NullMoveFH%=60.51, MateThreats=274, LMRResearch=2537, FPrune=2322901, EFPrune=458391

bench wac.epd 7   (after, fail-soft jump)
Bench: 300 positions at depth 7
Nodes=12959969, QNodes=8146215, Knps=1227
FirstMoveFH%=94.64, KillerFH%=17.30, TTFH%=24.62, FailHigh=1209280
NullMoveTries=265664, NullMoveFH%=60.51, MateThreats=274, LMRResearch=2537, FPrune=2322901, EFPrune=458391
```

**Byte-identical - the jump path never fires on wac.epd at depth 7, and the
reason is structural:** `AlphaBeta.Search` is fail-soft on beta cutoffs
(`return score`) but fail-hard on the low side (the ALL-node path returns
the original `alpha`, and TT CUT/ALL hits clamp to `beta`/`alpha` too). A
root fail high means the root's child failed low, and a fail-hard fail-low
returns exactly `-beta` - so the root sees `score == beta` on essentially
every fail high, and `max(beta + step, beta + 33)` is just the old fixed
step. The only ways a true score can leak past a root bound today are a
root child that is immediately mated/stalemated or an immediate repetition
draw scored outside the window - rare, and absent from this bench run.

Keeping the change anyway: it is strictly never worse, it covers those rare
real-game cases (e.g. finding a saving repetition while losing no longer
walks the window up in 3-4 re-searches), and it makes the widening correct
in advance if `Search` ever becomes properly fail-soft on the low side -
which is the actual follow-up that would give this teeth (it changes what
ALL nodes store in the TT, so it needs its own careful pass + self-play).

**Two benching pitfalls found while measuring this (both burned this
session):** (1) `dotnet build src/mmchess -c Release` writes to
`bin/Release/net10.0/`, **not** `bin/Release/net10.0/win-x64/` - the csproj
only lists `RuntimeIdentifiers` (plural allowed-list); the `win-x64` folder
is only refreshed by `-r win-x64` builds and had a stale binary from before
the eval commit, which produced a convincing-looking but bogus identical
A/B pair on the first attempt. (2) Check the freshness of the exact
`mmchess.dll` you're running (`ls -l` its timestamp vs the source edit)
before trusting any bench number.
