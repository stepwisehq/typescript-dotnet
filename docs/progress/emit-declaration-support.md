# Declaration support — active

From accepted script checkpoint `e8b11bcf`, this lane owns all pinned declaration
[diagnostics.go](../../upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go)
32 functions, [tracker.go](../../upstream/TypeScript/tsc/internal/transformers/declarations/tracker.go)
21 and [supplementalreferences.go](../../upstream/TypeScript/tsc/internal/transformers/declarations/supplementalreferences.go)
three, plus exact declaration host/output and diagnostic-result contracts. All bodies
are written; API/ownership reconciliation and formatting are complete. Source is
frozen/build-ready and foundation’s independent full source review is pending.
No build or execution acceptance claim. Root coordinates integration builds.

The host inherits the actual module-specifier/output host, retaining upstream
HasFileName roles there and exact SourceFile/FileReference parameters in declaration
methods. Diagnostic payloads use a nullable readonly value snapshot: upstream copies
the returned struct before inspection and never mutates that payload. The tracker
and shared state retain pointer identity and original slice headers; synchronous
ancestor/find/filter callbacks use existing explicit-state adapters. Text inspection
stays borrowed until owned diagnostic formatting. Supplemental references append to
the genuine source-file owner, preserve relative-path bytes and original nil diagnostics.
The demanded checker diagnostic helper has only an internal-access modifier join.

Foundation owns full declaration transform/util and independently reviews this lane.
This lane reviews those complete125 functions and inventory’s genuine VFS output
owner/wrappers once ready. Actual Program output/corpus observations and whole-emit
acceptance remain separate integrated work; no new private-state census is planned.

The focused scanner declaration-name value helper preserves exact missing/zero-width
behavior and existing string wrappers. The actual diagnostic owner explicitly accepts
ByteStringValue and materializes only its required owned argument storage; no implicit
ToString path. Mapping packet: `/tmp/emit-declaration-support-source-map-entries.json`.

Independent foundation source review is clear in artifacts/emit/source-review/declaration-support.json, including the genuine common IDeclarationTransformer contract. Reciprocal Transform110/Util15 and focused dependency review is clear in artifacts/emit/source-review/declaration-transform.json; demanded VFS write owner/wrapper review is clear in artifacts/emit/source-review/vfs-write.json. Integration compile and behavioral/output/Native acceptance are still pending. The actual complete original Go reference capture has finished:15330 configurations,154 bounded processes, zero original assertion failures, original policy/channel skips preserved separately; artifacts/emit/whole-reference/result.json binds lossless compressed observations. This reference capture is not managed whole-emit acceptance.
