# Parser stack continuation source review

The three promoted paths in [the manifest](./lsp-parser-stack-promotion.json)
are byte-identical to the reviewed drafts. The source review covers thirteen entry
guards and the synchronous explicit-state continuation worker, preserving original
parser bodies, scanner/factory ownership, argument values, exception propagation,
and child completion before parent resumption. Ordinary calls only perform the runtime stack
check; no test-only stack setting or reduced fixture depth is introduced.

The helper retains one exclusive parser owner through child completion and joins
before parser reuse. It preserves ExecutionContext and rethrows captured exceptions.
Fixed 4 MiB segments retain blocked parent threads, so this adaptation does not claim
Go's resource topology or arbitrary-depth admission. The runtime margin alone is not
behavioral proof: the unchanged 12,000-depth original selection assertion must pass
managed and actual Linux x64 Native AOT before acceptance. Later recursive consumers
remain subject to concrete observed failures, not inferred completion.

The hash-bound detailed review is retained in
[the source-review packet](../../artifacts/language-services/source-review/parser-stack-continuation.json).
No candidate runtime result is claimed here.
