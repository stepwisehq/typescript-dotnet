# Independent preference/configuration source review

Status: source review clear for the listed preference/configuration owner and original assertions; integration and execution are pending. This does not review the remaining LSUtil, normalization, regexp, configured-hover or project/session families.

Compared pinned userpreferences.go14 functions, userpreferences_generated.go19 functions, formatcodeoptions.go3 functions and all ten original preference test functions. Defaults, embedded value copies, raw/unstable/stable precedence, first-present null path behavior, enum parsing/defaults, Unicode whitespace trimming, editor settings precedence, ATA precedence, wire format conversions and serialization omissions match the original. The source-derived dispatch has83 raw fields,74 stable configuration blocks and83 serialization writes. All83 flattened preference fields participate in explicit AOT fill and equality; slice equality preserves nil independently from contents. Original parallel subtests run synchronously because they assert values, not scheduling.

Findings resolved: Go int preference owners and numeric enums now retain Linux64 values as long through storage, parsing and serialization; only actual uint32 LSP wire conversion narrows. The separate authored public control retains a value above Int32 through configuration and JSON. Two initializer field-prefix collisions and an unresolved lowercase namespace prefix were corrected without changing original inputs or expectations.

The generated closed dispatch and generator were read together. Source-derived field/tag enumeration is not runtime reflection. Regexp matching remains an external real dependency under implementation, not a permissive result admitted by this review. No build, original-Go test or managed/Native execution was performed by this reviewer.

Bound files:

- [UserPreferences.cs](../../src/TypeScript.Scanner/LanguageServices/Utilities/UserPreferences.cs): `0ae5186d645102e7b809bb86681eb2b18b4e172162e9301205b7902395118bb3`
- [UserPreferences.Generated.cs](../../src/TypeScript.Scanner/LanguageServices/Utilities/UserPreferences.Generated.cs): `e878372a8d529813022a4da7f4eeb53f127d755b0f62458d6f2e90633e3cfe62`
- [UserPreferences.Models.cs](../../src/TypeScript.Scanner/LanguageServices/Utilities/UserPreferences.Models.cs): `d3bebcf3f2736ea326c4d73edc4b27f6244f6747e335712cd2bfb51f21c4c712`
- [FormatCodeOptions.cs](../../src/TypeScript.Scanner/LanguageServices/Utilities/FormatCodeOptions.cs): `c6a135666294c67ebc9f6532eccd3236c4d3b96374d09eb75ecf576895bb40cf`
- [generate-ls-preferences.py](../../tools/generate-ls-preferences.py): `e533806c74252882d816eebb6a64475bc4fe660ad6025b34ba1e4b459b5a5769`
- [UserPreferenceChecks.Original.cs](../../validation/LanguageServices.Checks/UserPreferenceChecks.Original.cs): `483b787e40d83dd1e741c31941c2c499e6f3d25efb16c214ea2145f5103946de`
- [UserPreferenceChecks.Values.cs](../../validation/LanguageServices.Checks/UserPreferenceChecks.Values.cs): `adae504e5e1ae55d8e22df1ee666cc9410d4312cc5e71430e5f044976a5f51da`
- [UserPreferenceTests.cs](../../tests/LanguageServices.Tests/UserPreferenceTests.cs): `0d464513036b6134bb225a67c3158a44c51e74de3c843aff1ad48f8b6ce782f1`

Narrow build4 rebind: generated module-specifier constants now use the actual imported type names, and ToConfiguration is readonly on its value owner. The generator carries both changes. These are declaration/receiver adaptations; configuration branches, defaults, original inputs and expected JSON remain unchanged.

Runtime-exposed correction: the two suppressWhen* serializer fields now invert the corresponding Include* Tristate exactly as the original leading-not expressions require; emission is restricted to actual True or False values. This repairs an independent source-review miss, confirmed by unchanged original round-trip/serialization assertions. The generator and generated owner were narrowly rechecked together; execution of the corrected snapshot remains pending.
