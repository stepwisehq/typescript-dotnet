# Closed protocol codec independent review

Pin: `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
Reviewer: `emit_foundation`; source owner: `emit_print_maps`.

Status: complete independent source review clear; dependency execution passed.
This is not full server acceptance. The complete source packet is
[codec-source-packet](../../artifacts/language-services/codec-source-packet.json).
Scope includes the closed codec generator/templates, handwritten transport and
ownership adapters, generated field/root/method shapes, and all27 original protocol
assertion functions. Previously verified framing bodies need only their actual
codec joins reviewed. Separate authored JSON/depth controls remain distinct.

The first read compares the original struct/discriminated decoder against the
closed field dispatch: raw quoted field-name dispatch, contiguous required IDs,
non-null rejection before field decoding, deferred raw-value Clone ownership,
discriminator-first decoding, original deferred order, and transactional arm
assignment after complete success. The explicit ref decoder replaces the Go
retained decoder pointer without an escaping CLR ref-struct owner.

Known owner corrections during central build20 are tracked separately: two
value-union declaration joins and invalid-UTF8 map-key duplicate admission.
Final hash binding waits for the coherent corrected source. Full generated shape
and original assertion provenance review, managed behavior and actual Native depth
execution remain pending. No workload is run by this reviewer.

Concrete finding sent to owner: custom marshalUnion writers must initialize the
zero owner for a reference-mapped Go **value** root, as default struct writers do.
The initial null CLR representation of that zero value dereferenced fields rather
than taking the pinned zero-arm nullable/error branch. This must not alter the
separate pointer-root null case. The owner corrected the complete family; final verification is recorded below.

Reviewed generator paths now include plain/default readers, pointer/slice/map/fixed
array shapes, all custom kind/raw-key/trial/discriminator/registration patterns,
union/default struct writers, root descriptors, method-info extraction and
fail-closed assembly coverage. Empty slice replacement and partial element/map
writes reconcile with Go1.27.1 json/v2's actual workers; required deferred clones
and pointer/value root distinctions remain explicit. Map namespace checks are
required under the wrapper's AllowInvalidUTF8 marshal option; replacing malformed
key bytes can make different retained keys emit the same name. The final correction
keeps valid headers borrowed and owns only the replacement-name projection.

Remaining review includes generated/schema provenance, the complete27 original
assertions and structural equality helpers, RPC/semantic error joins and actual
Native recursion boundary evidence when available. No completion claim yet.

## Completed review (production23 dependency checkpoint)

The final closed packet is independently clear. The remaining CompletionItem,
URI, table and generated decode assertions were compared with both pinned test
files: full expected CompletionItem fields, all five malformed dynamic paths,
null/incomplete/wrong-type admission tables, discriminated field-order cases and
original error substrings are retained. The recursive equality-helper closure
compares fields, pointer presence and nil slice/map headers without replacing
structural assertions with JSON equality. Generator/schema provenance and all
tracked semantic modules are included in the packet; its 76 current hashes were
checked against disk with no differences.

The custom value-root finding is resolved across all 97 union writers and the
Registration writer. Five additive Go observations distinguish value-zero errors,
nullable zero arms, default struct output and nil pointer output. Pointer roots
still encode null before entering value writers. The malformed map-key namespace
correction preserves valid borrowed headers and rejects replacement collisions.
The optional discriminator test adapter now correctly marks required string
Title as not independently reject-null; Kind remains nullable-pointer metadata.
This final flag-only correction occurred after Native23 publication. Its unchanged
nonnull original assertion passed in that historical snapshot; the corrected
metadata itself has source review, not a claimed fresh execution.

Managed142 ordinary assertions and Native23 shared assertions passed centrally.
All eight isolated Native decode probes returned zero, including known recursive
model depth4998 and dynamic-any depth10000. These observations justify no stack
adaptation. They do not establish encoder depth coverage, transport/session
lifecycle behavior or whole language-service completion. This reviewer ran no
builds or workloads. Final source binding is recorded in
[protocol-codecs review](../../artifacts/language-services/source-review/protocol-codecs.json).
