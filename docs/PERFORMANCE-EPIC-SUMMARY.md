# From checker acceptance to the VS Code performance target

The campaign from checker acceptance `17e8c36f` to delivery `e3131a5b` made the Native AOT compiler substantially faster while preserving pinned-Go behavior. The final delivered configuration checked the VS Code project in **6.95 seconds**, against **6.06 seconds** for pinned Go: **1.147×**, meeting the user's best-of-five target of at most 1.2×. This result uses explicit AVX2 publication, eight checkers in both compilers and Native's opt-in `--no-gc-region`. Portable defaults remain separate.

The workload was one fixed VS Code checkout's main project, with `noEmit` and `skipLibCheck`, unchanged source and libraries. Pinned Go reports ten diagnostics; success means reproducing those complete results, not making them disappear. The [detailed evidence](./VSCODE-CHECK-STATUS.md) retains failures, measurements and review boundaries.

## The timeline

| Stage | What changed | Measurement and interpretation |
|---|---|---|
| First real VS Code check | Repaired nil-distribution handling, diagnostic aggregation and option agreement | Corrected Native **62.37 s**, pinned Go **9.61 s**: individual historical observations. |
| Shared text | Borrowed JSDoc/regex source instead of repeated whole-source copies | Balanced comparison means **65.72 → 54.76 s**, about **17% faster**, with lower CPU and RSS; two runs each. |
| Server GC | Selected and verified the standard server collector under actual Native AOT | Balanced means **60.57 → 28.49 s**, about **53% faster**, with approximately **11% higher RSS**; two runs each. |
| Comprehensive allocation rollout | Carried byte text, slices, callbacks and record storage through connected owners and consumers | Integrated workload observations moved into roughly **18–22 s**; these were not one controlled timing experiment. |
| Later runtime/source work | Throughput publication settings, dispatch and storage refinements, followed by explicit AVX2 experiments | Portable default observations reached roughly **16–18 s**. These combined source changes do not have isolated speedup claims. |
| Delivered target | AVX2, matched eight checkers and a fully qualified NoGCRegion | Fresh five-each cohort: **6.95 / 6.06 s best**, meeting the target. |

These rows describe progress, not additive savings or a controlled ninefold speedup. See the [initial workload](./VSCODE-CHECK-STATUS.md#workload-and-compiler-identity), [text comparison](./VSCODE-CHECK-STATUS.md#shared-text-views-verified-implementation-and-measured-improvement), [Server GC comparison](./VSCODE-CHECK-STATUS.md#server-gc-enabled-and-verified-under-native-aot) and [final delivery](./VSCODE-CHECK-STATUS.md#integration31-accepted-delivered-correctness-and-observed-target).

## What worked

The central finding was that a faithful compiler algorithm can still incur substantial costs when its storage model is translated literally into managed objects.

- **Borrow text through its consumers.** Immutable UTF8 views replaced copied substrings and repeated byte/string conversions across source, JSDoc, tokens, AST text, filenames and names. Strings still materialize at actual consumer boundaries. Byte offsets, malformed UTF8/WTF8 advancement and diagnostic behavior remain intact.
- **Use slice headers with real ownership rules.** Owners, producers, caches and consumers share backing storage where Go does. Required clipping, cloning and detachment remain. A missing upstream `slices.Clip` initially overwrote argument suffixes and introduced six diagnostics; restoring that operation fixed the defect.
- **Avoid transient callback and enumeration objects.** Cached static callbacks with explicit state and source-shaped loops replaced unnecessary closures, delegates and LINQ objects while retaining upstream lifetime boundaries.
- **Reduce separately allocated records.** Embedded AST/Type/Mapper payloads, link slots, ownership tokens and internal arena pages reduced wrapper overhead without discarding identity. Wider packed locations were measured rather than assumed beneficial.
- **Use standard runtime facilities deliberately.** Server GC, throughput-oriented Native AOT settings, explicit AVX2 and complete parser `ArrayPool` spill ownership addressed costs outside the compiler's algorithms. Required final arena clones remain.

The [technique index](./VSCODE-CHECK-STATUS.md#optimization-reference-index) maps these changes to evidence. Matched Server GC CoreCLR profiles went from approximately **35.17 to 10.08 GiB of sampled allocation weight**, about **71% lower**. That is useful evidence of the allocation trend, not exact Native allocation bytes. Later public cumulative counters measured **10.595 GB Native versus 8.992 GB Go**, about **18% more** for Native. Those counters belong to integration20 and its saved Go reference, not a final-delivery census; neither profiles nor counters measure retained heap or RSS.

## Why the last mile mattered

Allocation reduction alone did not close the wall-time gap. CPU evidence still showed significant GC/allocation-helper work. Explicit AVX2 enabled vector paths unavailable in the portable artifact. Matching eight checkers improved concurrency without changing the Go-shaped scheduler or sharing private checker caches.

NoGCRegion then traded memory headroom for avoiding collections during the qualified checking region. The initial **12 GiB total / 2 GiB LOH** budget failed qualification with eight checkers. Exhaustion of its smaller-object allowance was plausible, but the exact cause was not observed. Increasing the total to **14 GiB**, retaining **2 GiB LOH**, subsequently qualified; production requires a **20 GiB Linux MemAvailable** preflight and reports fallback/end outcomes honestly. No isolated parser-pooling, hashing or comparer speedup is claimed.

Final medians were **9.25 s Native / 6.78 s Go**, with ranges **6.95–10.42 / 6.06–8.33 s**. Median RSS was approximately **11.46 / 8.41 GiB**. Best-to-best was the agreed primary gate; median parity and portable-default target performance were not established. See [reproduction and qualifications](../README.md).

## Experiments and lessons

Broader JSDoc views reduced strings but increased total sampled allocation slightly because descriptor arrays cost memory. Public Symbol packing increased cumulative allocation **8.15%** and worsened observed time/RSS. POH placement, SustainedLowLatency, ReadyToRun and memory-policy combinations were rejected after separate comparisons. Heap-capture failures supplied no trustworthy retained-heap composition; that investigation was closed.

Complete ownership-path changes and coordinated integration avoided repeatedly testing unfinished bridges. Independent review and frozen source/binary bindings caught semantic and stale-harness problems. Final acceptance retained **1,730 selected managed cases**, **15,304 exact original Native records**, foundation assertions and shared controls, utilities and Program checks. Historical broad gates remain attributed to their own checkpoints. Five observations of one workload establish the delivered target under its explicit configuration, not a universal runtime guarantee.
