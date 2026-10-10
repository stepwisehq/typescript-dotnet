# Typed event client

The [event client](event-client/) combines mapped handler names, intrinsic casing and template keys, conditional payload extraction, variadic arguments and assertion-based validation across three TypeScript files. Its [invalid variant](event-client-invalid/) retains the same libraries/options and demonstrates complete source diagnostics for incorrect event names, payloads, handlers and tuple use.

Use the actual Native AOT checker after the gate builds it:

```sh
artifacts/typesystem-validation/cli-native/tscheck --config examples/typesystem/event-client/tsconfig.json --json
artifacts/typesystem-validation/cli-native/tscheck --config examples/typesystem/event-client-invalid/tsconfig.json --json
```

The valid form expects exit 0; the invalid form expects exit 1 with six complete pinned-Go diagnostics recorded by verification. Both use genuine unchanged strict ES2022 libraries/options; `skipLibCheck` remains its default false. This demonstrates checking, not emitting or running JavaScript. Current acceptance status belongs in the [assessment](../../docs/TYPESYSTEM-ASSESSMENT.md) and [proof](../../validation/results/typesystem-verified.json), once recorded.
