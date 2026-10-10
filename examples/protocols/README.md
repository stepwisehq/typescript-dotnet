# Job stream protocol checking

From the repository root, use the native checker produced by the
[protocol gate](../../docs/PROTOCOLS-VERIFY.md):

```sh
artifacts/protocols-validation/cli-native/tscheck --config examples/protocols/job-stream/tsconfig.json --json
artifacts/protocols-validation/cli-native/tscheck --config examples/protocols/job-stream-invalid/tsconfig.json --json
```

The first command exits 0 with no diagnostics. The second exits 1 with six complete
errors covering incorrect Promise/generator results and disposal protocols. Genuine
ES2022 and ESNext.Disposable libraries and strict options are unchanged; skipLibCheck
remains false. These are checking examples, not emitted runtime execution examples.
See [the assessment](../../docs/PROTOCOLS-ASSESSMENT.md) for current acceptance status.
