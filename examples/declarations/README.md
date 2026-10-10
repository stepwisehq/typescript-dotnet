# Declaration campaign examples

These authored examples use genuine strict ES2022 configuration and the unchanged
pinned ES2022 library closure. No emit or runtime execution is required. Current
acceptance status and qualified evidence are in the
[assessment](../../docs/DECLARATIONS-ASSESSMENT.md) and durable verification proof.

[warehouse](warehouse/tsconfig.json) combines an abstract base and derived class,
constructor parameter properties, private/protected members, accessors, an enum,
class/namespace merging and a cross-file ambient interface merge. The consumer uses
the model through both base and derived types.

[warehouse-invalid](warehouse-invalid/tsconfig.json) keeps the domain declarations
unchanged and exercises invalid constructor arguments, visibility, accessor writes,
incomplete merged-interface assignments and abstract construction. Expected complete
diagnostics are generated only by the pinned genuine Go Program oracle.

After the [verification gate](../../docs/DECLARATIONS-VERIFY.md) publishes the native
CLI, run from the repository root:

```sh
artifacts/declarations-validation/cli-native/tscheck --config examples/declarations/warehouse/tsconfig.json --json
artifacts/declarations-validation/cli-native/tscheck --config examples/declarations/warehouse-invalid/tsconfig.json --json
```

The valid warehouse exits 0. The invalid warehouse exits 1 with six diagnostics:
2345, 2341, 2445, 2322, 2741 and 2511. Both commands use the actual parsed options
and bundled libraries; `skipLibCheck` retains its false default. No fixed library
profile or hidden roots are injected.
