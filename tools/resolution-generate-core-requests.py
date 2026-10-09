#!/usr/bin/env python3
"""Authored complete-API resolver witnesses; expectations come only from pinned Go.

This writes inputs, never compiler-derived expected values. Individual requests
share a real resolver; separate requests create independent environments.
"""
from pathlib import Path
import argparse
import base64
import hashlib
import json

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / "validation/resolution-corpus/requests-core.jsonl"
PROVENANCE = DEST.with_name("requests-core-provenance.json")


def j(value):
    return json.dumps(value, ensure_ascii=True, separators=(",", ":"))


def query(name, mode=99, kind="module", containing="/repo/src/main.ts", **extra):
    return dict(Kind=kind, Name=name, Containing=containing, Mode=mode, **extra)


def generate():
    rows = []
    coverage = []

    def add(name, files=(), queries=(), options=None, family="search", **extra):
        opts = {"module": "esnext", "moduleResolution": "bundler"}
        opts.update(options or {})
        mapping = {"/repo/src/main.ts": ""}
        mapping.update(dict(files))
        row = dict(Name="core-" + name, Cwd="/repo", ConfigPath="/repo/tsconfig.json",
                   ConfigText=j({"compilerOptions": opts, "files": ["src/main.ts"]}),
                   Files=[dict(Path=p, Text=t) for p, t in mapping.items()], Queries=list(queries))
        row.update(extra)
        rows.append(row)
        coverage.append(dict(name=row["Name"], family=family, queries=len(row["Queries"])))

    declaration = "export declare const value: number;"
    implementation = "export const value = 1;"
    # Every supported resolver kind and actual request mode; condition-object
    # insertion order is meaningful source input, unlike ordinary map traversal.
    conditional = {"name": "pkg", "version": "1.2.3", "exports": {
        ".": {"import": {"types": "./import.d.mts", "default": "./import.mjs"},
              "require": {"types": "./require.d.cts", "default": "./require.cjs"}},
        "./private": None, "./array": ["../invalid", "./array.d.ts"],
        "./features/*": "./features/*.d.ts", "./features/special": "./special.d.ts"}}
    package_files = [("/repo/node_modules/pkg/package.json", j(conditional))]
    package_files += [("/repo/node_modules/pkg/" + f, declaration) for f in
                      ["import.d.mts", "require.d.cts", "array.d.ts", "features/a.d.ts", "special.d.ts"]]
    for resolution in ["node16", "nodenext", "bundler"]:
        for mode in [0, 1, 99]:
            add(f"conditions-{resolution}-{mode}", package_files,
                [query("pkg", mode), query("pkg/features/a", mode), query("pkg/features/special", mode),
                 query("pkg/private", mode), query("pkg/array", mode), query("pkg/missing", mode), query("pkg", mode)],
                {"moduleResolution": resolution, "module": "esnext" if resolution == "bundler" else resolution,
                 "traceResolution": True}, "modes-and-exports")

    for trace in [False, True]:
        add("cache-trace-" + str(trace).lower(), [("/repo/src/local.ts", implementation)],
            [query("./local"), query("./absent"), query("./local"), query("./absent"),
             query("./local", containing="/repo/src/other.ts"), query("./local", kind="directory", containing="/repo/src"),
             query("./local")], {"traceResolution": trace}, "positive-negative-cache-identity")

    extensions = [".ts", ".tsx", ".d.ts", ".mts", ".d.mts", ".cts", ".d.cts", ".js", ".jsx", ".mjs", ".cjs", ".json"]
    files = [("/repo/src/x" + ext, declaration if ".d." in ext else "{}" if ext == ".json" else implementation) for ext in extensions]
    for resolution in ["bundler", "nodenext"]:
        for mode in [1, 99]:
            add(f"extension-precedence-{resolution}-{mode}", files,
                [query("./x" + ext, mode) for ext in ["", ".js", ".jsx", ".mjs", ".cjs", ".ts", ".mts", ".cts", ".json"]],
                {"moduleResolution": resolution, "module": "esnext" if resolution == "bundler" else "nodenext",
                 "allowJs": True, "resolveJsonModule": True, "traceResolution": True}, "extension-substitution")
    for suffixes in [[".native", ""], [""], [".native"]]:
        add("suffixes-" + ("native-and-default" if len(suffixes) == 2 else "native-only" if suffixes[0] else "default-only"),
            [("/repo/src/view.native.ts", implementation), ("/repo/src/view.ts", implementation),
             ("/repo/src/only.ts", implementation), ("/repo/src/..native.ts", implementation)],
            [query("./view.js"), query("./only"), query("./..js"), query("./view.js")],
            {"moduleSuffixes": suffixes, "traceResolution": True}, "module-suffixes")
    add("directory-intent", [("/repo/src/dir.ts", implementation), ("/repo/src/dir/index.ts", implementation),
                              ("/repo/src/dir.js/index.ts", implementation)],
        [query(n, 1) for n in ["./dir", "./dir/", "./dir.js/", ".", "..", "./missing/"]],
        {"traceResolution": True}, "directory-intent")

    mapped = [("/repo/maps/exact.ts", implementation), ("/repo/maps/deep/item.ts", implementation),
              ("/repo/fallback/item.ts", implementation), ("/repo/lib/plain.ts", implementation)]
    paths = {"alias/exact": ["maps/exact"], "alias/deep/*": ["maps/deep/*"],
             "alias/*": ["missing/*", "fallback/*"], "empty/*": []}
    add("paths-exact-pattern-fallback", mapped,
        [query(n) for n in ["alias/exact", "alias/deep/item", "alias/item", "empty/item", "plain", "alias/exact"]],
        {"baseUrl": ".", "paths": paths, "traceResolution": True}, "baseurl-paths")
    add("paths-without-baseurl", mapped, [query("alias/exact"), query("alias/item")],
        {"paths": paths, "traceResolution": True}, "paths-config-directory")
    add("baseurl-precedes-package", [("/repo/pkg.ts", implementation)] + package_files,
        [query("pkg"), query("pkg/features/a")], {"baseUrl": ".", "traceResolution": True}, "baseurl-precedence")
    add("rootdirs-longest-and-fallback", [("/repo/generated/sub/dep.ts", implementation),
                                         ("/repo/generated/index.ts", implementation), ("/repo/src/sub/main.ts", "")],
        [query("./dep", containing="/repo/src/sub/main.ts"), query("../", 1, containing="/repo/src/sub/main.ts"),
         query("./absent", containing="/repo/src/sub/main.ts")],
        {"rootDirs": ["src", "generated", "src/sub"], "traceResolution": True}, "rootdirs")

    # Package parsing, ordered condition matching, self and imports branches.
    for condition_order in [{"custom": "./custom.d.ts", "types": "./types.d.ts", "default": "./main.js"},
                            {"types": "./types.d.ts", "custom": "./custom.d.ts", "default": "./main.js"}]:
        label = "custom-first" if next(iter(condition_order)) == "custom" else "types-first"
        add("custom-condition-" + label,
            [("/repo/node_modules/pkg/package.json", j({"name": "pkg", "exports": condition_order})),
             ("/repo/node_modules/pkg/custom.d.ts", declaration), ("/repo/node_modules/pkg/types.d.ts", declaration),
             ("/repo/node_modules/pkg/main.js", implementation)], [query("pkg", 99), query("pkg", 1)],
            {"customConditions": ["custom"], "traceResolution": True}, "custom-condition-order")
    for exports in [True, False]:
        add("exports-enabled-" + str(exports).lower(),
            [("/repo/node_modules/pkg/package.json", j({"name": "pkg", "types": "fallback.d.ts", "exports": {".": None}})),
             ("/repo/node_modules/pkg/fallback.d.ts", declaration)], [query("pkg")],
            {"resolvePackageJsonExports": exports, "traceResolution": True}, "exports-fallback-control")
    for metadata, label in [("{", "malformed"), (j({"name": "pkg", "types": 42, "main": "index.js"}), "wrong-type"),
                            (j({"name": "pkg", "types": "missing.d.ts", "main": "index.js"}), "missing-types")]:
        add("package-metadata-" + label,
            [("/repo/node_modules/pkg/package.json", metadata), ("/repo/node_modules/pkg/index.js", implementation),
             ("/repo/node_modules/pkg/index.d.ts", declaration)], [query("pkg"), query("pkg/")],
            {"traceResolution": True}, "package-fields-errors")
    add("self-and-imports", [("/repo/package.json", j({"name": "self", "version": "2.0.0", "exports": {
        ".": "./src/index.ts", "./*": "./src/*.ts"}, "imports": {"#exact": "./src/index.ts",
        "#pattern/*": "./src/*.ts", "#external": "pkg", "#blocked": None}})),
        ("/repo/src/index.ts", implementation), ("/repo/src/feature.ts", implementation)] + package_files,
        [query(n) for n in ["self", "self/feature", "#exact", "#pattern/feature", "#external", "#blocked", "#missing"]],
        {"traceResolution": True}, "self-package-imports")
    add("versioned-types-conditions", [("/repo/node_modules/pkg/package.json", j({"name": "pkg", "exports": {
        ".": {"types@<1": "./old.d.ts", "types@>=7": "./current.d.ts", "types": "./fallback.d.ts"}}})),
        ("/repo/node_modules/pkg/current.d.ts", declaration), ("/repo/node_modules/pkg/fallback.d.ts", declaration)],
        [query("pkg")], {"traceResolution": True}, "versioned-types")
    add("typesversions-package-and-subpath", [("/repo/node_modules/pkg/package.json", j({"name": "pkg", "version": "1.0.0",
        "types": "index.d.ts", "typesVersions": {"<1": {"*": ["old/*"]}, "*": {"*": ["missing/*", "v7/*"]}}})),
        ("/repo/node_modules/pkg/index.d.ts", declaration), ("/repo/node_modules/pkg/v7/index.d.ts", declaration),
        ("/repo/node_modules/pkg/v7/sub.d.ts", declaration)], [query("pkg"), query("pkg/sub"), query("pkg/absent")],
        {"traceResolution": True}, "typesversions")
    add("peer-package-identity", [("/repo/node_modules/pkg/package.json", j({"name": "pkg", "version": "1.0.0", "types": "index.d.ts",
        "peerDependencies": {"peer": "*", "absent": "*"}})), ("/repo/node_modules/pkg/index.d.ts", declaration),
        ("/repo/node_modules/peer/package.json", j({"name": "peer", "version": "3.2.1"}))],
        [query("pkg"), query("pkg")], {"traceResolution": True}, "peer-package-id")
    add("scoped-package-types-fallback", [("/repo/node_modules/@scope/pkg/package.json", j({"name": "@scope/pkg", "main": "index.js"})),
        ("/repo/node_modules/@scope/pkg/index.js", implementation),
        ("/repo/node_modules/@types/scope__pkg/package.json", j({"name": "@types/scope__pkg", "version": "1.0.0", "types": "index.d.ts"})),
        ("/repo/node_modules/@types/scope__pkg/index.d.ts", declaration)], [query("@scope/pkg"), query("@scope/pkg/sub")],
        {"traceResolution": True}, "types-fallback-scoped")
    add("alternate-result", [("/repo/node_modules/pkg/package.json", j({"name": "pkg", "main": "index.js", "types": "index.d.ts",
        "exports": {".": "./index.js"}})), ("/repo/node_modules/pkg/index.js", implementation),
        ("/repo/node_modules/pkg/index.d.ts", declaration)], [query("pkg")],
        {"moduleResolution": "nodenext", "module": "nodenext", "traceResolution": True}, "alternate-resolution")

    # Actual type-directive requests and automatic-name discovery are separate APIs.
    type_files = [("/repo/node_modules/@types/a/package.json", j({"name": "@types/a", "version": "1.0.0", "types": "index.d.ts"})),
                  ("/repo/node_modules/@types/a/index.d.ts", declaration), ("/custom/b/index.d.ts", declaration),
                  ("/repo/node_modules/pkg/package.json", j({"name": "pkg", "types": "index.d.ts"})),
                  ("/repo/node_modules/pkg/index.d.ts", declaration),
                  ("/repo/node_modules/@types/hidden/package.json", j({"typings": None}))]
    for roots, label in [(None, "default"), (["/custom", "/repo/node_modules/@types"], "explicit"), ([], "empty")]:
        opts = {"traceResolution": True}
        if roots is not None:
            opts["typeRoots"] = roots
        add("type-roots-" + label, type_files,
            [query(n, m, "type") for n in ["a", "b", "pkg", "absent", "./local"] for m in [0, 99]] +
            [query("a", 0, "type"), query("automatic-type-names", kind="utility", containing="")], opts, "type-directives-roots")
    add("typing-location", type_files + [("/cache/node_modules/@types/c/index.d.ts", declaration)],
        [query("c"), query("c", kind="type"), query("a", kind="type"), query("c")],
        {"traceResolution": True}, "typings-location", TypingsLocation="/cache")
    add("automatic-explicit-names", type_files, [query("automatic-type-names", kind="utility", containing="")],
        {"types": ["a", "a", "unknown"]}, "automatic-names")

    # Public config and package-directory APIs use their actual source boundaries.
    add("config-package-resolution", [("/repo/base.json", "{}"), ("/repo/node_modules/config-pkg/package.json", j({"name": "config-pkg", "tsconfig": "custom.json"})),
        ("/repo/node_modules/config-pkg/custom.json", "{}"), ("/repo/node_modules/config-pkg/nested.json", "{}")],
        [query(n, kind="config", containing="/repo/tsconfig.json") for n in ["./base", "./base.json", "config-pkg", "config-pkg/nested", "missing"]],
        family="config-api")
    add("package-directory-api", package_files,
        [query(n, m, "package-directory") for n in ["pkg", "pkg/features/a", "missing", "@scope/missing"] for m in [1, 99]],
        {"traceResolution": True}, "package-directory-api")

    for preserve in [False, True]:
        add("symlink-preserve-" + str(preserve).lower(),
            [("/store/pkg/package.json", j({"name": "pkg", "version": "1.0.0", "types": "index.d.ts"})),
             ("/store/pkg/index.d.ts", declaration)], [query("pkg"), query("pkg")],
            {"preserveSymlinks": preserve, "traceResolution": True}, "symlinks")
        rows[-1]["Files"].append(dict(Path="/repo/node_modules/pkg", Symlink="/store/pkg"))
    add("case-insensitive-canonical-path", [("/Repo/node_modules/Pkg/package.json", j({"name": "pkg", "types": "Index.d.ts"})),
        ("/Repo/node_modules/Pkg/Index.d.ts", declaration)], [query("Pkg", containing="/Repo/src/main.ts"), query("pkg", containing="/repo/src/main.ts")],
        family="case-keys", CaseInsensitive=True)
    add("unicode-and-raw-virtual-paths", [("/repo/src/é.ts", implementation)],
        [query("./é"), query("./raw", NameBase64=base64.b64encode(b"./\xff").decode()), query("./é")], family="raw-byte-identity")
    rows[-1]["Files"].append(dict(PathBase64=base64.b64encode(b"/repo/src/\xff.ts").decode(), Text=implementation))
    add("extra-file-extensions", [("/repo/src/component.vue", "<template/>"), ("/repo/src/component.d.vue.ts", declaration)],
        [query("./component.vue"), query("./component")], {"allowArbitraryExtensions": True, "traceResolution": True},
        "extra-extensions", ExtraExtensions=[".vue"])
    add("redirect-options-cache-keys", [("/repo/target/dep.ts", implementation), ("/repo/src/dep.ts", implementation)],
        [query("./dep"), query("mapped", RedirectId="redirect"), query("mapped"), query("mapped", RedirectId="redirect")],
        {"traceResolution": False}, "redirect-reference", Redirects=[dict(Id="redirect", ConfigPath="/repo/redirect.json",
            ConfigText=j({"compilerOptions": {"module": "esnext", "moduleResolution": "bundler", "baseUrl": ".",
                         "paths": {"mapped": ["target/dep"]}}, "files": ["src/main.ts"]}))])
    add("redirect-resolution-kind-and-type-roots", [("/repo/src/dep.ts", implementation),
        ("/custom/a/index.d.ts", declaration), ("/repo/node_modules/@types/a/index.d.ts", declaration)],
        [query("./dep"), query("./dep", RedirectId="node"), query("./dep", 1, RedirectId="node"),
         query("a", kind="type"), query("a", kind="type", RedirectId="node"),
         query("./dep", RedirectId="node")], {"traceResolution": True}, "redirect-mode-type-roots",
        Redirects=[dict(Id="node", ConfigPath="/repo/node.json", ConfigText=j({"compilerOptions": {
            "module": "nodenext", "moduleResolution": "nodenext", "typeRoots": ["/custom"]}, "files": ["src/main.ts"]}))])
    for fallback in [True, False]:
        add("static-fallback-" + str(fallback).lower(), [("/repo/src/fallback.ts", implementation)],
            [query("provided"), query("provided", 1), query("provided", containing="/repo/other/main.ts"),
             query("./fallback"), query("blocked"), query("provided", kind="directory", containing="/repo/src"), query("provided")],
            family="static-precedence-identity", Static=dict(FallbackToResolver=fallback, Entries=[
                dict(ModuleName="provided", Result=dict(ResolvedFileName="/global.d.ts")),
                dict(ModuleName="provided", ContainingDirectory="/repo/src", Result=dict(ResolvedFileName="/directory.d.ts")),
                dict(ModuleName="provided", Mode=99, Result=dict(ResolvedFileName="/esm.d.ts")),
                dict(ModuleName="provided", ContainingDirectory="/repo/src", Mode=99, Result=dict(ResolvedFileName="/both.d.ts")),
                dict(ModuleName="blocked", Result=None)]))
    for directory_search in [False, True]:
        add("entrypoint-directory-search-" + str(directory_search).lower(), package_files,
            [query("pkg"), query("pkg", kind="entrypoints", containing="/repo/node_modules/pkg", EnableDirectorySearch=directory_search)],
            {"traceResolution": True}, "entrypoints")
    for root_dir in ["src", None]:
        opts = {"outDir": "dist", "declarationDir": "types", "traceResolution": True}
        if root_dir is not None:
            opts["rootDir"] = root_dir
        add("self-output-to-input-" + ("rootdir" if root_dir else "config-directory"),
            [("/repo/package.json", j({"name": "self", "exports": {".": "./dist/index.js", "./type": "./types/index.d.ts"},
                                      "imports": {"#output": "./dist/index.js"}})),
             ("/repo/src/index.ts", implementation), ("/repo/index.ts", implementation),
             ("/repo/dist/index.js", implementation), ("/repo/types/index.d.ts", declaration)],
            [query("self"), query("self/type"), query("#output"), query("self")], opts, "output-to-input-remapping")
    # RawCompilerOptions is a distinct original public input mode. In particular,
    # it does not seed ConfigFilePath: the ambiguous-root diagnostics are real
    # reached resolver output rather than hand-built diagnostic expectations.
    add("raw-options-ambiguous-output-root", [("/repo/package.json", j({"name": "self", "exports": {".": "./dist/index.js"},
        "imports": {"#output": "./dist/index.js"}})), ("/repo/dist/index.js", implementation)],
        [query("self"), query("#output")], family="resolution-diagnostics",
        RawCompilerOptions=j({"module": 99, "moduleResolution": 100, "outDir": "dist", "traceResolution": True}))
    for no_dts in [False, True]:
        add("raw-options-no-dts-" + str(no_dts).lower(),
            [("/repo/node_modules/pkg/package.json", j({"name": "pkg", "types": "index.d.ts", "main": "index.js"})),
             ("/repo/node_modules/pkg/index.js", implementation), ("/repo/node_modules/pkg/index.d.ts", declaration)],
            [query("pkg"), query("pkg", 1)], family="raw-factory-options",
            RawCompilerOptions=j({"module": 99, "moduleResolution": 100, "noDtsResolution": no_dts, "traceResolution": True}))
    for label, entries, insensitive in [
        ("empty-name", [dict(ModuleName="", Result=None)], False),
        ("duplicate-global", [dict(ModuleName="dup", Result=None), dict(ModuleName="dup", Result=None)], False),
        ("duplicate-case-directory", [dict(ModuleName="dup", ContainingDirectory="/repo/src", Result=None),
                                      dict(ModuleName="dup", ContainingDirectory="/Repo/Src", Result=None)], True),
    ]:
        add("static-constructor-error-" + label, family="static-constructor-errors", CaseInsensitive=insensitive,
            Static=dict(FallbackToResolver=True, Entries=entries))

    # Independent matrix review: module-condition witnesses do not exercise the
    # type-directive producer, and static module fallback is not type fallback.
    conditional_types = [("/repo/node_modules/@types/dual/package.json", j({
        "name": "@types/dual", "version": "1.0.0", "exports": {".": {
            "import": {"types": "./import.d.mts"},
            "require": {"types": "./require.d.cts"}}}})),
        ("/repo/node_modules/@types/dual/import.d.mts", declaration),
        ("/repo/node_modules/@types/dual/require.d.cts", declaration)]
    for resolution in ["nodenext", "bundler"]:
        add("type-conditional-" + resolution, conditional_types,
            [query("dual", mode, "type") for mode in [0, 1, 99, 1, 99]],
            {"moduleResolution": resolution, "module": "nodenext" if resolution == "nodenext" else "esnext",
             "traceResolution": True}, "type-directive-conditional-modes")
    root_import_files = [("/repo/package.json", j({"name": "self", "imports": {
        "#/*": "./src/*.ts", "#exact": "./src/item.ts"}})),
        ("/repo/src/item.ts", implementation)]
    for resolution in ["node16", "nodenext", "bundler"]:
        add("imports-root-feature-" + resolution, root_import_files,
            [query(name) for name in ["#/item", "#exact", "#", "#/absent"]],
            {"moduleResolution": resolution, "module": resolution if resolution != "bundler" else "esnext",
             "traceResolution": True}, "imports-root-feature-defaults")
    add("imports-disabled", root_import_files,
        [query(name) for name in ["#/item", "#exact", "#", "#/absent"]],
        {"resolvePackageJsonImports": False, "traceResolution": True}, "imports-disabled-control")
    add("static-type-delegation-without-module-fallback", conditional_types,
        [query("dual"), query("dual", 1, "type"), query("dual", 99, "type"),
         query("absent", 99, "type"), query("dual", 1, "type")],
        {"traceResolution": True}, "static-type-directive-delegation",
        Static=dict(FallbackToResolver=False, Entries=[dict(ModuleName="dual", Result=None)]))

    assert len(rows) == len({r["Name"] for r in rows})
    for row in rows:
        paths = [f.get("PathBase64") or f["Path"] for f in row["Files"]]
        assert len(paths) == len(set(paths)), row["Name"]
    return rows, coverage


def main():
    args = argparse.ArgumentParser()
    args.add_argument("--check", action="store_true")
    parsed = args.parse_args()
    rows, coverage = generate()
    requests = "".join(j(row) + "\n" for row in rows).encode()
    sources = [ROOT / "upstream/TypeScript/tsc/internal/module" / n for n in ["resolver.go", "types.go", "cache.go", "staticresolver.go", "util.go"]]
    provenance = dict(kind="authored-default-resolver-api-witnesses", upstream="fed0bf24149fb1ed36039212648bafdafc1ea10e",
                      expectedPolicy="Only independently executed pinned Go produces expectations; this generator writes requests only.",
                      cases=len(rows), queries=sum(len(r["Queries"]) for r in rows), requestsSha256=hashlib.sha256(requests).hexdigest(),
                      sourceFiles={str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest() for p in sources},
                      coverage=coverage, limits=["Finite API/input witnesses, not exhaustive branches or compiler fixture successes.",
                      "Default repeated-result identities supplement complete original clone/cache/race assertions; no private runtime topology census.",
                      "All file names are virtual host inputs, including one raw non-scalar byte path; no OS arbitrary-byte support claim.",
                      "RawCompilerOptions is the source stored-option domain: numeric ModuleKind.ESNext=99 and ModuleResolutionKind.Bundler=100, unlike configuration-text enum names. Initial string-enum raw factory failure is preserved as investigation evidence, not a compiler discrepancy."])
    outputs = {DEST: requests, PROVENANCE: (json.dumps(provenance, indent=2) + "\n").encode()}
    for path, content in outputs.items():
        if parsed.check:
            if not path.exists() or path.read_bytes() != content:
                raise SystemExit("Generated input/provenance drift: " + str(path.relative_to(ROOT)))
        else:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(content)
    print(f"{len(rows)} authored resolver requests, {provenance['queries']} actual API queries")


if __name__ == "__main__":
    main()
