# Synchronous order fulfillment

The valid and invalid projects exercise actual strict ES2022/Bundler checking with
unchanged ES2022 libraries. They combine discriminated unions, runtime binding/rest,
spread, optional/nullish expressions, synchronous iteration, switch/labels, and
try/catch/finally across files. Default `skipLibCheck` remains false.

After the expression verification gate publishes the native checker:

```sh
artifacts/expressions-validation/cli-native/tscheck --config examples/expressions/order-fulfillment/tsconfig.json --json
artifacts/expressions-validation/cli-native/tscheck --config examples/expressions/order-fulfillment-invalid/tsconfig.json --json
```

The valid project must exit 0. The invalid project must exit 1 with complete diagnostics
matching pinned Go. This example is behavioral evidence for selected responsibilities,
not whole-checker completion. Authoritative acceptance status belongs to the
[expression assessment](../../docs/EXPRESSIONS-ASSESSMENT.md) and eventual proof.
