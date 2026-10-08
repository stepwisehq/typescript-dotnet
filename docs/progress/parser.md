# Parser port checkpoint

- Status: executable and generator sources frozen; final verification gate running under verification owner. Do not modify those files until the gate finishes unless a concrete blocker is found.
- Scope: full bounded syntax parser, AST syntax trees and syntax diagnostics; TS/TSX/JS/JSX/JSON, malformed recovery, pragmas/references, top-level-await reparse, lazy/eager JSDoc and JSDoc transformation reparses. Binder, checker, module resolution, emitter and SourceFile position maps excluded.
- Pin: maintained TypeScript `fed0bf24149fb1ed36039212648bafdafc1ea10e`, sources `upstream/TypeScript/tsc/internal`, Apache-2.0.
- Owned source files: Parser/{Parser,Types,Utilities,References,Jsdoc,Reparser}.cs; Runtime/{ParserCompat,ParserStrings}.cs; Scanner/ParserBridge.cs plus scanner syntax helpers; Core/{ScriptKind,Tristate,CompilerOptions,NodeModules}.cs, enum additions OptionsGenerated.cs; TsPath/Path.cs. AST owner merged Core combinators/line starts into Core.cs and range helpers into Text.cs.
- Function inventory: parser.go434 =431 emitted plus getParser/isMissingNodeList adapters; putParser omitted because each call obtains a fresh isolated managed parser. jsdoc.go54 =53 emitted plus init registration mapped to static constructor. reparser.go21, utilities.go6, references.go2 all emitted. types.go ParseFlags fully included.
- Declaration mapping: Parser.cs owns parser.go enums/fields/ParserState/adapters; Jsdoc.cs owns jsdoc.go enums/init; Types.cs contains ParseFlags only. Hand-adapted declarations are preserved in tools/parser-port/declarations/*.cs.inc and inserted by patch.py.
- AST contract: Node/NodeData payload union, exact AsX accessors, flattened embedded syntax-base interfaces; Kind/Flags/Loc/Parent and function boundaries retained. List<T> preserves null, allocated empty, and shared missing sentinel identity; explicit IsMissing marker supports canonical serialization.
- Byte contract: public ParseSourceFile string/byte[] converts source exactly once; parser/scanner internal source and token values remain Latin1 byte strings representing Go UTF8 bytes. SourceFile.Text and AST Text scalars retain that raw convention; TextBytes/DecodedText and scanner node-text byte/decoded wrappers expose clear external boundaries. Raw scanner callbacks avoid double decoding diagnostics.
- Public ScriptKind contract: JS1/JSX2/TS3/TSX4/JSON6. Unknown retains upstream failure; reserved5/7 and arbitrary numeric casts keep Go fallback behavior outside tested contract. No grammar modes stubbed.
- Allocation adaptations: fresh parser replaces Go pool; arenas allocate managed lists; Append/Slice copy storage when extending/ranging to preserve speculative slice lengths. Zero-additions Append returns original source, preserving nil/identity. Node references remain shared; reviewed actual index mutations are local/cloned arrays or parser-owned scratch. Costs unmeasured; optimization deferred by user.
- Fidelity fixes retained: range bodies/receiver closures/fallthrough/partially-new short declarations have small scaffold tests; make(slice,len,cap) preserves length; arena Clone(empty)/NewSlice(0) return nil; GoBuilder.String retained; Tristate values Unknown0/False1/True2; raw diagnostics forwarded correctly.
- AST cross-review fixes: NodeList zero span vs factory undefined span, JSX module-indicator method promotion/pruning, kind-based IsJSDoc, parent validation/null-source ancestor resolution, lazy-cache locking, and nullable JSDoc callback result cached without treating nil as missing registration.
- Final gate passed:464parser xUnit; Native AOT460matching cases/19268nodes/318diagnostics; scanner regressions1287xUnit plus1276native cases/178helper outputs. Authoritative hashes/results:../../validation/results/parser-verified.json.
- Scanner helpers implemented: scanner.go GetTokenPosOfNode now in Scanner.cs. utilities.go tokenIsIdentifierOrKeyword, GetSourceTextOfNodeFromSourceFile, isJSDocTypeExpressionOrChild, normalizeJSDocTypeSourceText, stripLeadingJSDocComment, GetTextOfNodeFromSourceText, GetTextOfNode now in Utilities.cs.
- Remaining scanner/utilities.go exclusions: IdentifierToKeywordKind, GetTextOfJSDocComment, DeclarationNameToString, IsIdentifierText, IsIntrinsicJsxName. Other scanner AST error-range/SourceFile scanner-acquisition helpers remain excluded.
- Independent AST/harness review: review-parser-support.md; no remaining blocking findings. AST owner reviews parser/runtime/emitter; verifier owns oracle/protocol/tests/artifacts/final gates; root owns source-map/assessment/pin/overall state.
- Reproducibility proved: emit all5algorithm files into scratch, apply patch.py plus declarations, LF-normalize both scratch/reviewed files via tools/CSharpFormat; all5 files byte-identical. Handwritten runtime/scanner/Core files are not generated. Scaffold is specific fail-fast Go syntax translation, not a general Go compiler.

Regeneration from repository root must target scratch output first:

```sh
GO111MODULE=off tools/.cache/go/bin/go test ./tools/parser-port
tools/.cache/go/bin/go build -o /tmp/parser-port tools/parser-port/main.go
/tmp/parser-port upstream/TypeScript/tsc/internal/parser/parser.go /scratch/Parser.cs
# Repeat jsdoc->Jsdoc, reparser->Reparser, utilities->Utilities, references->References.
python3 tools/parser-port/patch.py /scratch
dotnet run --project tools/CSharpFormat/CSharpFormat.csproj -- /scratch/*.cs
```

- Complete and stopped. Final evidence and mapping are recorded; no unresolved implementation action. See ../STATE.md for acceptance limits and any later mandate.
