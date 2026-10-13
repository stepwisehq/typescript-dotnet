# Reaching the VS Code emit performance target

This is the historical optimization/delivery summary. The fresh post-LSP comparison
on 2026-10-13 is in [current VS Code measurements](./VSCODE-CHECK-STATUS.md):
The latest matched pre-LSP/current/Go cohort gives current check + emit 9.61 s /
Go 8.52 s (+12.8%) and check-only 6.96 s / 6.15 s (+13.2%), both observed targets met.
Pre-LSP emit best is 9.68 s, while its median is faster than current; variability
remains unexplained. Earlier current emit misses of +42.3% and +34.3% remain preserved.
All emitted files and comparable diagnostics still match. The historical results
and work described below are unchanged; no stable LSP regression or cause is established.

The delivered Native AOT compiler completed the VS Code check-and-emit workload in **10.18 seconds**, compared with **9.37 seconds for pinned TypeScript Go**: **8.6% slower**, within the requested 20% limit. These are the best results from five fresh processes for each compiler, run in alternating order with warm inputs. Every measured run produced the same **10,031 files, 150,466,543 bytes and ten complete comparable diagnostic messages**.

The work ran from baseline `edc53e02` to delivery `9b8fa74a`. It preserved the Go compiler algorithms and ownership rules while restoring inexpensive storage and reuse patterns in the .NET representation. The [accepted proof](../validation/results/vscode-emit-performance.json), [independent review](../validation/results/vscode-emit-performance-review.json) and [detailed measurements](./VSCODE-CHECK-STATUS.md) retain the successful results and the approaches that did not meet the target. The earlier [checker performance summary](./PERFORMANCE-EPIC-SUMMARY.md) describes a separate delivery.

## Allocation was the first problem, but not the last

The initial tuned emit run allocated about **19.916 GB**. Its optional no-GC region ended early, and Native took 30.72 seconds against Go's 10.19 seconds. Emit added substantial work beyond checking: transforming trees, reading comments, constructing names and writing output.

We completed the allocation changes across producers and consumers:

- **Borrowed UTF-8 and value iteration:** comment and printer paths reused source storage, and comment traversal avoided allocating iterator objects.
- **Callback reuse:** recursive visitors retained callbacks at the correct owner lifetime; static callbacks received explicit state instead of creating closures.
- **Embedded values and upstream pooling:** factory and visitor hooks became value records with independent copy semantics, and the parent-setting visitor regained the pooling present upstream.
- **Standard span operations:** ASCII runs were scanned efficiently and indentation went directly into the output buffer, preserving the original non-ASCII rune behavior.

This was not a blanket attempt to remove copies. Required Go slice clones, independent tree owners, nil-versus-empty distinctions and callback lifetimes remained intact. The packet was integrated and independently reviewed before behavioral and performance acceptance.

Allocation fell to **14.755 GB**, approximately **26% less**, but the old no-GC policy still failed and that first run remained slow. Its 14 GiB total reservation included 2 GiB for large objects, leaving 12 GiB for ordinary objects. Allocation traces supported increasing the optional full-emit reservation to **16 GiB total, including 2 GiB for large objects**. The check-only policy remained 14/2. The region is optional, reports failure and fallback, and requires at least **20 GiB available memory**; it is not a process memory cap.

The first two rows below are single qualification runs; the remaining rows compare each compiler's best of five. Times from different cohorts do not isolate each edit's benefit.

| Stage | Native / Go wall time | Outcome |
| --- | --- | --- |
| Initial emit qualification | 30.72 / 10.19 s | No-GC region failed |
| Allocation packet qualification | 32.21 / 8.59 s | Allocation lower; old region still failed |
| Full-emit 16/2 region, five-run cohort | 12.96 / 8.41 s | Region succeeded; 54.1% slower |
| Minimum 128 workers with CLR mutex | 13.55 / 8.69 s | Still 55.9% slower |
| POSIX mutex with default workers | 11.61 / 8.66 s | Improved; still 34.1% slower |
| POSIX mutex plus minimum 128 workers | **10.18 / 9.37 s** | **8.6% slower; target met** |

## The remaining gap involved waiting

CPU profiling showed that Native could consume less CPU time than Go while taking longer on the wall clock. Late emit work showed checker-lock waiting. A separate diagnostic trace and runtime-source inspection supported investigating the CLR's shared wait coordination, rather than assuming more allocation changes would solve the gap. The evidence did not identify every wait object conclusively or establish an exact causal saving.

A thread-affine lock would have changed the Go contract: checker leases permit another thread to release them. Instead, the Linux x64/glibc implementation uses a **standard POSIX binary semaphore**, with the existing managed semaphore as the fallback elsewhere. The checker, pool and resolver still share the same mutex and the same critical sections. Non-reentrancy, transferable release, native error handling and lifetime safety remain explicit. There is no custom wait queue or compiler scheduling rewrite.

Raising the standard thread-pool minimum to 128 had failed with the CLR wait backend. Combining it with the POSIX representation met the target. That interaction matters: the measurements do not justify assigning the final improvement to either change alone.

The final Native median was **10.39 seconds**, with a 10.18–14.76 second range; Go's median was **10.06 seconds**, with a 9.37–12.00 second range. Native peak memory was roughly **14.3 GiB**, against Go's **10.8–11.4 GiB**. The result uses additional memory to keep the optional no-GC region active.

## Correctness and scope stayed part of acceptance

The final gates passed **2,226 managed tests** and actual Linux x64 Native AOT execution of **152 original functions plus 254 authored controls**, alongside the retained foundation and program checks. The full emit corpus contained **15,285 exact public-output matches**, **39 original preparation skips** and **six explicitly retained external incremental-buildinfo metadata differences**. All **15,304 retained no-emit comparisons** matched. Original formatted baseline assertions, disk output controls and real JavaScript execution were also checked.

No-emit performance protection measured **8.55 seconds Native versus 7.13 seconds Go**, nominally 19.9% slower. Its margin is narrower than the timer's 0.01-second precision supports robustly; this is not a promise to reproduce the older 6.95-second checker result. An outdated verification-harness status expectation was corrected against pinned Go; compiler behavior and diagnostic expectations stayed unchanged.

The accepted tuning is explicit: **AVX2, eight checkers, optional 16/2 GiB full-emit no-GC reservation and minimum 128 workers**. It applies to VS Code's main source project with its existing `skipLibCheck=true`, source maps and declarations disabled, on Linux x64/glibc. Ordinary runtime defaults were not silently changed.

The practical lesson was to complete ownership paths first, preserve failed measurements, and use profiles to choose the next experiment. Allocation reduction was necessary but insufficient. Runtime representation and worker availability had to work together. The retained effort records include builds, profiles, repeated measurements and full correctness gates; they do not provide token, monetary or human-time totals. The [reproduction guide](./VSCODE-CHECK-STATUS.md) records the exact policy and evidence needed to repeat the delivered result.
