# From checker acceptance to the VS Code performance target

We went from **a correct-but-expensive port taking roughly a minute on VSCode to a verified Native AOT configuration finishing in 6.95 seconds—within 14.7% of pinned Go’s 6.06 seconds.**

The main lesson was that preserving compiler algorithms wasn’t enough: we also needed to preserve Go’s inexpensive storage, borrowing, reuse, and callback patterns, then choose appropriate .NET runtime settings.

The work spans checker acceptance at `17e8c36f` through performance checkpoint `e3131a5b`, recorded October 10–11.

**The timeline**

| Stage | What we learned and changed | Recorded result |
|---|---|---|
| Establish the real-project baseline | Fixed a nil-handling crash and diagnostic aggregation; aligned options and compared with our exact pinned Go revision. | Native **62.37s**, Go **9.61s** in initial individual runs. |
| Profile before guessing further | Native profiling showed substantial GC/runtime work. Allocation profiles exposed copied substrings, repeated encoding, collection copies, and managed wrappers. | About **52% of sampled native user CPU** was in workstation-GC runtime functions. |
| Start with JSDoc and regex scanning | Borrowed existing source storage instead of copying substrings or converting entire files between representations. | Controlled comparison: mean **65.72 → 54.76s**. |
| Enable Server GC | Used the collector better suited to this parallel, allocation-heavy workload. | Separate controlled comparison: mean **60.57 → 28.49s**, with higher memory use. |
| Roll allocation techniques through complete subsystems | Extended UTF8 borrowing, slice reuse, explicit callback state, compact records, and upstream pooling across producers and consumers. | Integrated runs reached roughly **18–22s**. |
| Refine storage and generated code | Compacted internal link locations, restored upstream fast paths and capacities, reduced dispatch overhead, and selected throughput-oriented Native AOT settings. | Later default-configuration observations were around **16–18s**. |
| Close the remaining runtime gap | Enabled AVX2, qualified an explicit no-GC region, and used eight checkers in **both** implementations. | Final best-of-five: **6.95s Native / 6.06s Go**. |

These are milestones, **not one controlled before/after experiment**: hardware conditions, runtime settings, checker counts, and measurement methods changed. Individual improvements cannot be added together. The [detailed performance record](./VSCODE-CHECK-STATUS.md) preserves those distinctions.

**The most important findings and techniques**

- **Borrow text rather than repeatedly creating strings.** Go string slices share backing storage. We carried immutable UTF8 views through scanning, JSDoc, tokens, AST text, names, paths, and lookups. Span-based comparisons and direct rune advancement avoid intermediate decoding; real output boundaries still materialize text.

- **Preserve slice ownership end to end.** Our `Slice<T>` representation carries offset, length, and capacity, allowing views and reuse instead of repeatedly copying lists. Standard .NET copying handles actual transfers. Required upstream copies and capacity restrictions remain essential: an omitted Go `Clip` operation caused incorrect diagnostics during development.

- **Avoid allocating closures for immediate callbacks.** Explicit state plus cached static callbacks removed many transient closures and delegates. Callbacks whose state genuinely escapes retain their necessary lifetime.

- **Make embedded records inexpensive in C#.** We removed redundant wrapper/payload objects, packed suitable internal records into stable storage, and reduced retained location sizes. Public identity and live references stayed intact.

- **Reuse temporary storage where ownership permits.** Parser/binder/task reuse, spelling scratch buffers, inline parser list storage, and now pooled parser overflow arrays reduce temporary garbage. Retained AST or token storage is not returned to a pool.

- **Use .NET’s optimized facilities.** Span operations, standard hashing and collections, Server GC, and Native AOT speed settings replaced unnecessary compatibility overhead without introducing a different checker algorithm.

The earliest retained allocation profile did show **over 50 GB of allocation traffic**: **52,015,179,472 sampled bytes**, or **52.02 GB / 48.44 GiB**. That was the corrected baseline running under Release **CoreCLR**, used because Native AOT tracing lacked named allocation types and stacks. It estimates allocation traffic across the run—not simultaneous live memory, RSS, or an exact Native AOT allocation census. [Original profile and limitations](./VSCODE-CHECK-STATUS.md#measured-profiling-follow-up-2026-10-10) · [Retained sampled total](../artifacts/vscode-profiling/managed-baseline/folded-summary.json)

The later matched Server-GC allocation-profile series fell from approximately **35.17 GiB to 10.08 GiB of sampled allocation traffic—a 71% reduction**. This is its own comparable series, not a direct percentage comparison with that first profile. Later public runtime counters measured approximately **10.59 GB Native versus 8.99 GB Go**, much closer than the early profiles suggested. Those counters predate the final checkpoint and measure cumulative heap allocation, not peak memory. [Allocation evidence](./VSCODE-CHECK-STATUS.md#integration20-callbacksetup-completion-and-native-throughput-policy-accepted)

**Why the last stretch improved so quickly**

Much of the groundwork was already done. The remaining obstacle was increasingly the cost of collecting and scheduling that workload.

1. **AVX2 enabled wider generated operations**, including relevant byte-comparison and hashing paths.
2. **The no-GC region allowed checking to proceed without recurring collections while the region remained active**, trading additional memory for throughput.
3. **Eight checkers exposed more parallelism.** Go received the same setting.
4. The first eight-checker attempt **fell out of its no-GC region**. Exhausting its ordinary-object allowance was a plausible explanation. Raising the total allowance from 12 to 14 GiB, while retaining 2 GiB for large objects, allowed all five final regions to finish successfully.

So the final jump was primarily a **combined runtime and parallelism result built on the allocation rollout**, not a sudden breakthrough from parser pooling alone.

**What we tried and did not adopt**

SustainedLowLatency, ReadyToRun, pinned-object-heap placement, public-symbol arena packing, and several GC-policy combinations did not establish a worthwhile overall improvement. Some increased memory or allocation costs. We retained the negative results rather than treating every optimization idea as a success.

The process improved too: we moved away from isolated edits followed by repeated tests and benchmarks, toward complete ownership-path changes, coordinated builds, VSCode-first qualification, and broader acceptance at integration boundaries. Astra coordinated; Sol agents implemented and independently reviewed. No reliable token or monetary totals were available.

**What the final result establishes**

| Final five-run cohort | Native AOT | Pinned Go |
|---|---:|---:|
| Best wall time | **6.95s** | **6.06s** |
| Median wall time | 9.25s | 6.78s |
| Median peak memory | ~11.46 GiB | ~8.41 GiB |

We met the requested **best-to-best 20% target**, not median parity. The result requires Linux x64, AVX2, eight checkers, and the explicit no-GC option with sufficient available memory; portable defaults are not covered by that performance claim.

Correctness remained intact: **1,730 selected xUnit cases, all 15,304 complete Native reference comparisons, and retained foundation gates passed**, followed by independent review. VSCode retains the same ten diagnostics as pinned Go—it was not made “clean” by suppressing errors.

[Final evidence](./VSCODE-CHECK-STATUS.md#integration31-accepted-delivered-correctness-and-observed-target) · [Reproduction instructions](../README.md)
