"""Guard the independent static port of eleven original metadata/path tests.

This reads original declarations/tests and assertion-body labels only. It does not
generate expectations from production C# Equals, descriptors or runtime results.
"""
from pathlib import Path
import hashlib
import json
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "validation/Configuration.Checks/metadata-test-provenance.json"
PREFIX = "upstream/TypeScript/tsc/internal/"


def read(path):
    return (ROOT / path).read_text()


def fields(path, name):
    body = read(path).split("type " + name + " struct {", 1)[1].split("\n}", 1)[0]
    return re.findall(r"^[ \t]*([A-Z]\w*)[ \t]+([^`\s]+)", body, re.M)


def digest(path):
    return hashlib.sha256((ROOT / path).read_bytes()).hexdigest()


groups = [
    ("ParsedOptionChecks", PREFIX + "tsoptions/parsedoptions_test.go", 3),
    ("OutputPathChecks", PREFIX + "outputpaths/outputpaths_test.go", 5),
    ("WildcardDirectoryChecks", PREFIX + "tsoptions/wildcarddirectories_test.go", 3),
]
inventory = []
for counterpart, path, count in groups:
    functions = re.findall(r"^func (Test\w+)\(t \*testing.T\)", read(path), re.M)
    if len(functions) != count:
        raise ValueError("Original function inventory drift: " + path)
    cs = "validation/Configuration.Checks/" + counterpart + ".cs"
    actual = re.findall(r'"(Test\w+)"', read(cs))
    if set(actual) != set(functions) or any(actual.count(name) != 2 for name in functions):
        raise ValueError("Cases/switch registry differs from original functions: " + cs)
    inventory.append({"source": path, "sourceSha256": digest(path), "counterpart": cs, "functions": functions})

parsed_path = PREFIX + "tsoptions/parsedoptions.go"
mapper_path = PREFIX + "contentmapper/contentmapper.go"
parsed_fields = fields(parsed_path, "ParsedOptions")
definition_fields = fields(mapper_path, "Definition")
manifest_fields = fields(mapper_path, "Manifest")
mapper_fields = fields(mapper_path, "Mapper")
visible = ["ParsedOptions." + name for name, _ in parsed_fields]
visible += ["Mapper." + name for name, _ in definition_fields + manifest_fields + mapper_fields]
cs = read("validation/Configuration.Checks/ParsedOptionChecks.cs")
actual_fields = re.findall(r'new\("((?:ParsedOptions|Mapper)\.\w+)"', cs)
if len(visible) != 15 or actual_fields != visible:
    raise ValueError("Full static visible-field coverage differs from original reflection domain")
slices = [owner + "." + name for owner, rows in [("ParsedOptions", parsed_fields), ("Definition", definition_fields), ("Manifest", manifest_fields)] for name, kind in rows if kind.startswith("[]") or kind == "json.Value"]
actual_slices = re.findall(r'\("((?:ParsedOptions|Definition|Manifest)\.\w+)"', cs.split("[] Slices =", 1)[1])
if len(slices) != 7 or actual_slices != slices:
    raise ValueError("Nil/empty slice domain differs from original reflection domain")
source_test = read(PREFIX + "tsoptions/parsedoptions_test.go")
mutation_body = source_test.split("change func(*ParsedOptions)", 1)[1].split("} {", 1)[0]
mutations = re.findall(r'\{"([^"]+)", func', mutation_body)
actual_mutations = re.findall(r'\("([^"]+)", p =>', cs.split("[] Changes =", 1)[1].split("[] Slices =", 1)[0])
if len(mutations) != 25 or actual_mutations != mutations:
    raise ValueError("Explicit mutation inventory differs from original assertions")

result = {
    "upstreamRevision": "fed0bf24149fb1ed36039212648bafdafc1ea10e",
    "functions": 11,
    "groups": inventory,
    "fieldSources": [{"source": p, "sourceSha256": digest(p)} for p in [parsed_path, mapper_path]],
    "parsedEquality": {
        "visibleFields": visible,
        "mutations": mutations,
        "nilEmptySlices": slices,
        "expectedEqualityChecks": 124,
        "emptyTypeAcquisitionComparisons": 16,
        "pathsAllocationComparisons": 2,
        "adaptation": "Original reflection becomes a source-guarded static field/mutation table. Source-populated fixture and each zero/change establish the finite DeepEqual result independently; no production Equals result determines expected values. Nullable receivers dispatch explicitly. JsonValue null versus allocated empty Bytes represents nil versus empty json.Value."
    },
    "outputPaths": {"buildInfoRows": 10, "stemRows": 6, "totalPathEqualityAssertions": 26},
    "wildcardDirectories": {"exactMapRows": 2, "unicodeNonnilRows": 4},
    "runtimeStatus": "Independent execution and review evidence are recorded separately; this file guards source assertion inventory only."
}
encoded = json.dumps(result, indent=2, ensure_ascii=False) + "\n"
if "--check" in sys.argv:
    if not OUT.exists() or OUT.read_text() != encoded:
        raise SystemExit("metadata test source provenance drift")
else:
    OUT.write_text(encoded)
