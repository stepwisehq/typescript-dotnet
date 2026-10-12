# Independent review: selected NFD dependency

Source review is clear for the selected canonical `NFD.String` closure in x/text v0.42.0, Unicode17.0.0, using the pinned Go1.27.1 dependency. This does not claim the composing, compatibility, Transform, Append or iterator APIs. Managed test execution and Native AOT acceptance remain pending.

The review compared quick-span scanning, malformed UTF8 advancement, Hangul decomposition, combining-class insertion, stream-safe segmentation and continuation, embedded reorder-buffer storage, borrowed decomposition headers and the required final byte-array-to-string ownership copy against the pinned source. All six generated arrays match the pinned table element for element: ccc56, decomps19650, nfcValues3072, nfcIndex1408, nfcSparseOffset171 and nfcSparseValues787. The eight selected constants also match. The source table SHA256 is `dade3c78c0b36426a6966f46926f2ab61c484ae425feb175a07fbe1ee8f855fa`.

[NormalizationChecks](../../validation/LanguageServices.Checks/NormalizationChecks.cs) retains all20 original TestDecomposeSegment input/position/buffer assertions as one original test function. These exercise the segment worker, not the complete public string worker. Eight separately counted authored public String observations cover ASCII, canonical decomposition, Hangul, malformed bytes, stream-safe continuation, musical Mc marks, quick prefixes spanning multiple segments and combining-class reordering.

Review found one authored expectation defect: the whole-string grave31 case had copied the segment-only expectation and dropped the final grave. The actual pinned public oracle, run once centrally in0.364s, confirms `a + grave30 + CGJ + grave1`. The corrected shared assertion retains that mark. Two additional public-path rows use exactly the captured outputs. All eight shared public expectations agree byte for byte with [the captured oracle](../../artifacts/language-services/integration-1/nfd-public-go.json); no production behavior was changed to fit an expectation. The [durable oracle](../../tools/language-services-oracle/nfd_public.go) preserves malformed input/output through hexadecimal fields.

Pins and selected source boundaries are recorded in [the standard-library mapping](./lsp-service-standard-library-mapping.json). This is independent source/assertion review plus pinned public Go witness evidence, not completed managed/native verification or general normalization completeness.

## Bound snapshot

- `src/TypeScript.Scanner/UnicodeNormalization/Properties.cs`: `8590779775d3e18a5277a0154ae6e3513b041e1210bae562452427e6995b722f`
- `src/TypeScript.Scanner/UnicodeNormalization/Tables.Generated.cs`: `d0b2e0f5766bf9bcabe2094fd95e214023df5b1a7bc0f3e7263449ecec1c6b5b`
- `src/TypeScript.Scanner/UnicodeNormalization/Composition.cs`: `26b212c2fd7204c52885ed1b8838ea8e2d9ff34948af10ad4c4d7e1fcf67d4bd`
- `src/TypeScript.Scanner/UnicodeNormalization/Normalize.cs`: `96e140c70898b82268005d70cf6bcb9bc838d487bd54f39fc1a4f4a3e1fe6bb0`
- `src/TypeScript.Scanner/UnicodeNormalization/Trie.cs`: `b85c7408adbfb0bf7149e6be41e030a544e564b402e3346888fd65086ae699f2`
- `tools/generate-nfd-tables.py`: `d8940392facdcaecaad88ec321f21e6a2a2c94e75495de4d296dbe877b657ce5`
- `validation/LanguageServices.Checks/NormalizationChecks.cs`: `4a41758c03eeb3996e4d5c8875e276987c7db10b392215141df92eb3f606933e`
- `tools/language-services-oracle/nfd_public.go`: `8698c99ca42321824f0343471995f0c918ef4286d835e1250c0bbab8cb756b5f`
- `artifacts/language-services/integration-1/nfd-public-go.json`: `ecc0e398a1431120d1926b248687a74a2135865705d883b6ea61652f8e4b3751`
