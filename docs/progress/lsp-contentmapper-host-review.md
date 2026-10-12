# Content-mapper process host review

Independent source review is clear for all58 pinned host implementation functions, the closed DTO codec family, and all28 original host test functions. [Mapping](./lsp-contentmapper-host-mapping.json) preserves direct source/function anchors; [bound review](../../artifacts/language-services/source-review/contentmapper-host.json) records reviewed hashes.

The review reconciled process close ordering, initialization error precedence, once-only leases, locale resets, project identity/required copies, timing snapshots, UTF8/UTF16 coordinates, directives and raw JSON ownership. Two findings were fixed: overlap sorting now uses the existing Go sort owner, and array/named-string JSON errors retain Go type names. Original assertions are unchanged.

The reviewer ran no builds/tests. Managed and actual Native AOT host execution remain pending root integration; prior IPC source review is reused.
