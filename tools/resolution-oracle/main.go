// Authored independent observations of actual pinned Module/TsOptions APIs.
// This first vertical has no Program or Checker and no production instrumentation.
package main

import (
	"bufio"
	stdbytes "bytes"
	"encoding/base64"
	"encoding/json"
	"fmt"
	"os"
	"reflect"
	"sort"

	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/binder"
	"github.com/microsoft/TypeScript/tsc/internal/collections"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"github.com/microsoft/TypeScript/tsc/internal/module"
	"github.com/microsoft/TypeScript/tsc/internal/packagejson"
	"github.com/microsoft/TypeScript/tsc/internal/parser"
	"github.com/microsoft/TypeScript/tsc/internal/tsoptions"
	"github.com/microsoft/TypeScript/tsc/internal/tspath"
	"github.com/microsoft/TypeScript/tsc/internal/vfs"
	"github.com/microsoft/TypeScript/tsc/internal/vfs/vfstest"
)

type fileInput struct {
	Path, Text             string
	PathBase64, TextBase64 *string
	Symlink                *string
}
type query struct {
	Kind, Name, Containing       string
	NameBase64, ContainingBase64 *string
	Mode                         core.ResolutionMode
	Other                        string
	Extension                    string
	ExtraExtension               bool
	EnableDirectorySearch        bool
	RedirectId                   string
}
type request struct {
	Name, Cwd, ConfigPath, ConfigText string
	CaseInsensitive                   bool
	Graph                             bool
	ExtraExtensions                   []string
	TypingsLocation                   string
	RawCompilerOptions                *string
	Redirects                         []redirectInput
	Static                            *staticInput
	Files                             []fileInput
	Queries                           []query
}

func bytes(s string, raw *string) string {
	if raw == nil {
		return s
	}
	result, err := base64.StdEncoding.Strict().DecodeString(*raw)
	if err != nil {
		panic(err)
	}
	return string(result)
}
func b64[S ~string](s S) string { return base64.StdEncoding.EncodeToString([]byte(s)) }

// Static data projection enumerates actual stored exported fields. Raw byte strings
// have a role-specific base64 encoding; no substring/path relocation is applied.
func value(input any) any {
	if input != nil {
		t := reflect.TypeOf(input)
		if t.PkgPath() == "github.com/microsoft/TypeScript/tsc/internal/module" && t.Name() == "resolutionCandidate" {
			return value(input.(interface{ AsString() string }).AsString())
		}
	}
	if directory, ok := input.(packagejson.PackageDirectory); ok {
		return value(directory.AsDirectoryPath())
	}
	if reference, ok := input.(*ast.FileReference); ok && reference != nil {
		return map[string]any{"FileName": value(reference.FileName), "ResolutionMode": reference.ResolutionMode, "Preserve": reference.Preserve, "pos": reference.Pos(), "end": reference.End()}
	}
	if set, ok := input.(*collections.Set[string]); ok {
		if set == nil {
			return nil
		}
		keys := []string{}
		for key := range set.Keys() {
			keys = append(keys, key)
		}
		sort.Strings(keys)
		return value(keys)
	}
	if input == nil {
		return nil
	}
	if paths, ok := input.(*collections.OrderedMap[string, []string]); ok {
		if paths == nil {
			return nil
		}
		entries := []any{}
		for key, values := range paths.Entries() {
			entries = append(entries, map[string]any{"keyBase64": b64(key), "value": value(values)})
		}
		return entries
	}
	if object, ok := input.(*collections.OrderedMap[string, packagejson.JSONValue]); ok {
		if object == nil {
			return nil
		}
		entries := []any{}
		for key, item := range object.Entries() {
			entries = append(entries, map[string]any{"keyBase64": b64(key), "value": value(item)})
		}
		return entries
	}
	if object, ok := input.(*collections.OrderedMap[string, packagejson.ExportsOrImports]); ok {
		if object == nil {
			return nil
		}
		entries := []any{}
		for key, item := range object.Entries() {
			entries = append(entries, map[string]any{"keyBase64": b64(key), "value": value(item)})
		}
		return entries
	}
	v := reflect.ValueOf(input)
	switch v.Kind() {
	case reflect.Interface, reflect.Pointer:
		if v.IsNil() {
			return nil
		}
		return value(v.Elem().Interface())
	case reflect.String:
		return map[string]any{"bytesBase64": b64(v.String())}
	case reflect.Bool:
		return v.Bool()
	case reflect.Int, reflect.Int8, reflect.Int16, reflect.Int32, reflect.Int64:
		return v.Int()
	case reflect.Uint, reflect.Uint8, reflect.Uint16, reflect.Uint32, reflect.Uint64:
		return v.Uint()
	case reflect.Float64:
		return v.Float()
	case reflect.Map:
		if v.IsNil() {
			return nil
		}
		if v.Type().Key().Kind() != reflect.String {
			panic("Unobserved map key family: " + v.Type().String())
		}
		keys := v.MapKeys()
		sort.Slice(keys, func(i, j int) bool { return keys[i].String() < keys[j].String() })
		entries := []any{}
		for _, key := range keys {
			entries = append(entries, map[string]any{"keyBase64": b64(key.String()), "value": value(v.MapIndex(key).Interface())})
		}
		return entries
	case reflect.Slice, reflect.Array:
		if v.Kind() == reflect.Slice && v.IsNil() {
			return nil
		}
		result := []any{}
		for i := 0; i < v.Len(); i++ {
			result = append(result, value(v.Index(i).Interface()))
		}
		return result
	case reflect.Struct:
		result := map[string]any{}
		for i := 0; i < v.NumField(); i++ {
			field := v.Type().Field(i)
			if field.IsExported() {
				result[field.Name] = value(v.Field(i).Interface())
			}
		}
		return result
	default:
		panic("Unobserved stored value family: " + v.Type().String())
	}
}

type observer struct {
	files  map[*ast.SourceFile]int
	owners []any
}

func (o *observer) file(f *ast.SourceFile) *int {
	if f == nil {
		return nil
	}
	id, ok := o.files[f]
	if !ok {
		id = len(o.owners)
		o.files[f] = id
		o.owners = append(o.owners, map[string]any{"id": id, "nameBase64": b64(f.FileName()), "pathBase64": b64(f.PathKey()), "textBase64": b64(f.Text())})
	}
	return &id
}
func (o *observer) diagnostics(ds []*ast.Diagnostic) any {
	if ds == nil {
		return nil
	}
	result := []any{}
	for _, d := range ds {
		result = append(result, map[string]any{"code": d.Code(), "category": d.Category(), "key": string(d.MessageKey()), "textBase64": b64(d.String()), "args": value(d.MessageArgs()), "pos": d.Pos(), "end": d.End(), "file": o.file(d.File()), "sourceBase64": b64(d.Source()), "reportsUnnecessary": d.ReportsUnnecessary(), "reportsDeprecated": d.ReportsDeprecated(), "skippedOnNoEmit": d.SkippedOnNoEmit(), "chain": o.diagnostics(d.MessageChain()), "related": o.diagnostics(d.RelatedInformation()), "repopulateInfo": value(d.RepopulateInfo())})
	}
	return result
}
func (o *observer) module(r *module.ResolvedModule) any {
	if r == nil {
		return nil
	}
	return map[string]any{"ResolutionDiagnostics": o.diagnostics(r.ResolutionDiagnostics), "ResolvedFileName": value(r.ResolvedFileName), "ResolvedPath": value(r.ResolvedPath), "OriginalPath": value(r.OriginalPath), "Extension": value(r.Extension), "ResolvedUsingTsExtension": r.ResolvedUsingTsExtension, "ResolvedUsingExtraExtensions": r.ResolvedUsingExtraExtensions, "IsExternalLibraryImport": r.IsExternalLibraryImport, "IsCustomResolution": r.IsCustomResolution, "PackageId": value(r.PackageId), "AlternateResult": value(r.AlternateResult)}
}
func (o *observer) directive(r *module.ResolvedTypeReferenceDirective) any {
	if r == nil {
		return nil
	}
	return map[string]any{"ResolutionDiagnostics": o.diagnostics(r.ResolutionDiagnostics), "Primary": r.Primary, "ResolvedFileName": value(r.ResolvedFileName), "ResolvedPath": value(r.ResolvedPath), "OriginalPath": value(r.OriginalPath), "PackageId": value(r.PackageId), "IsExternalLibraryImport": r.IsExternalLibraryImport}
}
func traces(rows []module.DiagAndArgs) any {
	if rows == nil {
		return nil
	}
	result := []any{}
	for _, row := range rows {
		result = append(result, map[string]any{"code": row.Message.Code(), "category": row.Message.Category(), "key": string(row.Message.Key()), "templateBase64": b64(row.Message.String()), "args": value(row.Args)})
	}
	return result
}

type operation struct {
	Kind   string `json:"kind"`
	Path   string `json:"pathBase64"`
	Result any    `json:"result"`
}
type host struct {
	vfs.FS
	cwd        tspath.RootedDirectoryPath
	operations []operation
}

func (h *host) GetCurrentDirectory() tspath.RootedDirectoryPath { return h.cwd }
func (h *host) FSHost() vfs.FS                                  { return h }

// Go cannot have both an embedded FS field and FS method, so resolver uses wrapper.
type resolverHost struct{ h *host }

func (h resolverHost) FS() vfs.FS                                      { return h.h }
func (h resolverHost) GetCurrentDirectory() tspath.RootedDirectoryPath { return h.h.cwd }
func (h *host) FileExists(p tspath.RootedFilePath) bool {
	r := h.FS.FileExists(p)
	h.operations = append(h.operations, operation{"fileExists", b64(p), r})
	return r
}
func (h *host) DirectoryExists(p tspath.RootedDirectoryPath) bool {
	r := h.FS.DirectoryExists(p)
	h.operations = append(h.operations, operation{"directoryExists", b64(p), r})
	return r
}
func (h *host) ReadFile(p tspath.RootedFilePath) (string, bool) {
	r, ok := h.FS.ReadFile(p)
	h.operations = append(h.operations, operation{"readFile", b64(p), map[string]any{"ok": ok, "textBase64": b64(r)}})
	return r, ok
}
func (h *host) Realpath(p tspath.RootedPath) tspath.RootedPath {
	r := h.FS.Realpath(p)
	h.operations = append(h.operations, operation{"realpath", b64(p), value(r)})
	return r
}
func (h *host) Stat(p tspath.RootedPath) vfs.FileInfo {
	r := h.FS.Stat(p)
	var result any
	if r != nil {
		result = map[string]any{"name": value(r.Name()), "size": r.Size(), "mode": uint32(r.Mode()), "isDir": r.IsDir()}
	}
	h.operations = append(h.operations, operation{"stat", b64(p), result})
	return r
}
func (h *host) GetAccessibleEntries(p tspath.RootedDirectoryPath) vfs.Entries {
	r := h.FS.GetAccessibleEntries(p)
	links := []string(nil)
	if r.Symlinks != nil {
		links = []string{}
		for name := range r.Symlinks {
			links = append(links, name)
		}
		sort.Strings(links)
	}
	h.operations = append(h.operations, operation{"accessibleEntries", b64(p), map[string]any{"files": value(r.Files), "directories": value(r.Directories), "symlinks": value(links)}})
	return r
}

func observe(req request) any {
	cs := tspath.CaseSensitive
	if req.CaseInsensitive {
		cs = tspath.CaseInsensitive
	}
	entries := map[string]any{}
	for _, file := range req.Files {
		path := bytes(file.Path, file.PathBase64)
		if _, exists := entries[path]; exists {
			panic("Duplicate fixture path")
		}
		if file.Symlink != nil {
			entries[path] = vfstest.Symlink(*file.Symlink)
		} else {
			entries[path] = []byte(bytes(file.Text, file.TextBase64))
		}
	}
	h := &host{FS: vfstest.FromMap(entries, cs), cwd: tspath.RootedDirectoryPath(req.Cwd)}
	o := &observer{files: map[*ast.SourceFile]int{}, owners: []any{}}
	configPath := tspath.RootedFilePath(req.ConfigPath)
	var parseErrors []*ast.Diagnostic
	var configErrors []*ast.Diagnostic
	var options *core.CompilerOptions
	var rawError error
	var parsed *tsoptions.ParsedCommandLine
	if req.Graph {
		parsed, parseErrors = tsoptions.GetParsedCommandLineOfConfigFile(configPath, nil, nil, h, nil)
		if parsed == nil {
			panic("Graph configuration did not parse")
		}
		if req.RawCompilerOptions != nil {
			options, configErrors, rawError = rawOptions(*req.RawCompilerOptions, h.cwd)
			if rawError != nil {
				panic(rawError)
			}
			parsed.SetCompilerOptions(options)
		} else {
			options = parsed.CompilerOptions()
			configErrors = parsed.Errors
		}
		result := o.graph(parsed, h)
		return map[string]any{"name": req.Name, "options": value(options), "parseDiagnostics": o.diagnostics(parseErrors), "configDiagnostics": o.diagnostics(configErrors), "graph": result, "hostOperations": h.operations, "diagnosticOwners": o.owners}
	}
	if req.RawCompilerOptions != nil {
		options, configErrors, rawError = rawOptions(*req.RawCompilerOptions, h.cwd)
	} else {
		raw, errors := tsoptions.ParseConfigFileTextToJson(configPath, cs.PathKey(configPath.AsPath()), req.ConfigText)
		parseErrors = errors
		parsed = tsoptions.ParseJsonConfigFileContent(raw, h, configPath.Directory(), nil, configPath, nil, nil)
		options = parsed.CompilerOptions()
		configErrors = parsed.Errors
	}
	resolver := module.NewResolver(module.ResolverOptions{Host: resolverHost{h}, CompilerOptions: options, ExtraExtensions: req.ExtraExtensions, TypingsLocation: tspath.RootedDirectoryPath(req.TypingsLocation)})
	var active module.Resolver = resolver
	var staticError error
	if req.Static != nil {
		entries := []module.StaticResolutionEntry{}
		for _, entry := range req.Static.Entries {
			entries = append(entries, module.StaticResolutionEntry{ModuleName: entry.ModuleName, ContainingDirectory: tspath.RootedDirectoryPath(entry.ContainingDirectory), ResolutionMode: entry.Mode, Result: entry.Result})
		}
		overrides, err := module.NewStaticResolutions(entries, req.Static.FallbackToResolver, h.cwd, cs)
		staticError = err
		if err == nil {
			active = module.NewStaticResolver(resolver, overrides)
		} else if len(req.Queries) != 0 {
			panic("Queries supplied for invalid static factory")
		}
	}
	redirects := map[string]*redirectReference{}
	redirectRows := []any{}
	for _, input := range req.Redirects {
		if _, exists := redirects[input.Id]; exists {
			panic("Duplicate redirect ID")
		}
		path := tspath.RootedFilePath(input.ConfigPath)
		raw, errors := tsoptions.ParseConfigFileTextToJson(path, cs.PathKey(path.AsPath()), input.ConfigText)
		target := tsoptions.ParseJsonConfigFileContent(raw, h, path.Directory(), nil, path, nil, nil)
		redirects[input.Id] = &redirectReference{name: target.ConfigName(), options: target.CompilerOptions()}
		redirectRows = append(redirectRows, map[string]any{"id": input.Id, "configName": value(target.ConfigName()), "options": value(target.CompilerOptions()), "parseDiagnostics": o.diagnostics(errors), "configDiagnostics": o.diagnostics(target.Errors)})
	}
	results := []any{}
	identities := map[any]int{}
	for _, q := range req.Queries {
		name := bytes(q.Name, q.NameBase64)
		containing := bytes(q.Containing, q.ContainingBase64)
		var redirect module.ResolvedProjectReference
		if q.RedirectId != "" {
			r, ok := redirects[q.RedirectId]
			if !ok {
				panic("Unknown redirect ID")
			}
			redirect = r
		}
		var result any
		var identity any
		var trace []module.DiagAndArgs
		var err error
		switch q.Kind {
		case "module":
			var r *module.ResolvedModule
			r, trace, err = active.ResolveModuleName(name, tspath.RootedFilePath(containing), q.Mode, redirect)
			result = o.module(r)
			if r != nil {
				identity = r
			}
		case "directory":
			var r *module.ResolvedModule
			r, trace, err = active.ResolveModuleNameFromDirectory(name, tspath.RootedDirectoryPath(containing), q.Mode)
			result = o.module(r)
			if r != nil {
				identity = r
			}
		case "type":
			var r *module.ResolvedTypeReferenceDirective
			r, trace = active.ResolveTypeReferenceDirective(name, tspath.RootedFilePath(containing), q.Mode, redirect)
			result = o.directive(r)
			if r != nil {
				identity = r
			}
		case "config":
			r := module.ResolveConfig(name, tspath.RootedFilePath(containing), h)
			result = o.module(r)
			if r != nil {
				identity = r
			}
		case "package-directory":
			r := resolver.ResolvePackageDirectory(name, tspath.RootedFilePath(containing), q.Mode, redirect)
			result = o.module(r)
			if r != nil {
				identity = r
			}
		case "entrypoints":
			var entry *packagejson.InfoCacheEntry
			resolver.PackageJsonCacheEntries(func(key tspath.PathKey, item *packagejson.InfoCacheEntry) bool {
				if item.PackageDirectory.AsDirectoryPath() == tspath.RootedDirectoryPath(containing) {
					if entry != nil {
						panic("Ambiguous actual package entry")
					}
					entry = item
				}
				return true
			})
			if entry == nil {
				panic("Unobserved actual package entry")
			}
			result = value(resolver.GetEntrypointsFromPackageJsonInfo(entry, name, q.EnableDirectorySearch))
		case "utility":
			switch name {
			case "js-extension":
				result = value(module.TryGetJSExtensionForFile(containing, options))
			case "js-extension-typed":
				result = value(module.TryGetJSExtensionForFileName(tspath.RootedFilePath(containing), options))
			case "versioned-types-key":
				result = module.IsApplicableVersionedTypesKey(containing)
			case "parse-package-name":
				a, b := module.ParsePackageName(containing)
				result = map[string]any{"packageName": value(a), "remaining": value(b)}
			case "mangle-package-name":
				result = value(module.MangleScopedPackageName(containing))
			case "unmangle-package-name":
				result = value(module.UnmangleScopedPackageName(containing))
			case "types-package-name":
				result = value(module.GetTypesPackageName(containing))
			case "package-from-types":
				result = value(module.GetPackageNameFromTypesPackageName(containing))
			case "compare-patterns":
				result = module.ComparePatternKeys(containing, q.Other)
			case "automatic-type-names":
				result = value(module.GetAutomaticTypeDirectiveNames(options, h.cwd, h))
			case "conditions":
				result = value(module.GetConditions(options, q.Mode))
			case "resolution-diagnostic":
				fileName := tspath.RootedFilePath(containing)
				file := parser.ParseSourceFile(ast.SourceFileParseOptions{FileName: fileName, PathKey: cs.PathKey(fileName.AsPath())}, "", core.ScriptKindTS)
				message := module.GetResolutionDiagnostic(options, &module.ResolvedModule{Extension: q.Extension, ResolvedUsingExtraExtensions: q.ExtraExtension}, file)
				if message != nil {
					result = map[string]any{"code": message.Code(), "category": message.Category(), "key": string(message.Key()), "templateBase64": b64(message.String())}
				}
			case "binder-source-file":
				fileName := tspath.RootedFilePath(containing)
				file := parser.ParseSourceFile(ast.SourceFileParseOptions{FileName: fileName, PathKey: cs.PathKey(fileName.AsPath())}, q.Other, core.ScriptKindTS)
				binder.BindSourceFile(file)
				var symbolName any
				if file.Symbol != nil {
					symbolName = value(file.Symbol.Name)
				}
				result = map[string]any{"fileName": value(file.FileName()), "path": value(file.PathKey()), "moduleSymbolName": symbolName, "parseDiagnostics": o.diagnostics(file.Diagnostics())}
			default:
				panic("Unknown actual Module utility API: " + name)
			}
		default:
			panic("Unknown resolver request operation: " + q.Kind)
		}
		var id *int
		if identity != nil {
			n, ok := identities[identity]
			if !ok {
				n = len(identities)
				identities[identity] = n
			}
			id = &n
		}
		var errorText any
		if err != nil {
			errorText = b64(err.Error())
		}
		results = append(results, map[string]any{"kind": q.Kind, "name": value(name), "containing": value(containing), "mode": q.Mode, "identity": id, "result": result, "trace": traces(trace), "errorBase64": errorText})
	}
	// Cache Range is unordered upstream. Sort actual keys before assigning rooted
	// entry/content IDs; preserve entry aliases and never sort producer trace arrays.
	cache := map[tspath.PathKey]*packagejson.InfoCacheEntry{}
	resolver.PackageJsonCacheEntries(func(k tspath.PathKey, e *packagejson.InfoCacheEntry) bool { cache[k] = e; return true })
	keys := []tspath.PathKey{}
	for key := range cache {
		keys = append(keys, key)
	}
	sort.Slice(keys, func(i, j int) bool { return keys[i] < keys[j] })
	cacheRows := []any{}
	entryIDs := map[*packagejson.InfoCacheEntry]int{}
	contentIDs := map[*packagejson.PackageJson]int{}
	for _, key := range keys {
		entry := cache[key]
		id, ok := entryIDs[entry]
		if !ok {
			id = len(entryIDs)
			entryIDs[entry] = id
		}
		var contentID *int
		var fields any
		if entry.Contents != nil {
			n, ok := contentIDs[entry.Contents]
			if !ok {
				n = len(contentIDs)
				contentIDs[entry.Contents] = n
			}
			contentID = &n
			fields = map[string]any{"parseable": entry.Contents.Parseable, "fields": value(entry.Contents.Fields)}
		}
		cacheRows = append(cacheRows, map[string]any{"key": value(key), "identity": id, "packageDirectory": value(entry.PackageDirectory.AsDirectoryPath()), "directoryExists": entry.DirectoryExists, "contentsIdentity": contentID, "contents": fields})
	}
	response := map[string]any{"name": req.Name, "options": value(options), "parseDiagnostics": o.diagnostics(parseErrors), "configDiagnostics": o.diagnostics(configErrors), "queries": results, "hostOperations": h.operations, "packageCache": cacheRows, "diagnosticOwners": o.owners}
	if req.Static != nil {
		var text any
		if staticError != nil {
			text = b64(staticError.Error())
		}
		response["staticErrorBase64"] = text
	}
	if req.RawCompilerOptions != nil {
		var text any
		if rawError != nil {
			text = b64(rawError.Error())
		}
		response["rawOptionsErrorBase64"] = text
	}
	if req.Redirects != nil {
		response["redirects"] = redirectRows
	}
	return response
}

func main() {
	scanner := bufio.NewScanner(os.Stdin)
	scanner.Buffer(make([]byte, 4096), 64*1024*1024)
	encoder := json.NewEncoder(os.Stdout)
	for scanner.Scan() {
		var req request
		if err := exactRequestFields(scanner.Bytes(), reflect.TypeOf(req)); err != nil {
			panic(err)
		}
		decoder := json.NewDecoder(stdbytes.NewReader(scanner.Bytes()))
		decoder.DisallowUnknownFields()
		if err := decoder.Decode(&req); err != nil {
			panic(err)
		}
		for _, q := range req.Queries {
			if q.Mode != 0 && q.Mode != 1 && q.Mode != 99 {
				panic("Unknown resolution mode")
			}
		}
		if req.Static != nil {
			for _, entry := range req.Static.Entries {
				if entry.Result != nil && entry.Result.ResolutionDiagnostics != nil {
					panic("Unobserved static diagnostic input constructor")
				}
				if entry.Mode != nil && *entry.Mode != 0 && *entry.Mode != 1 && *entry.Mode != 99 {
					panic("Unknown static resolution mode")
				}
			}
		}
		if req.Graph && (len(req.Queries) > 0 || req.Static != nil || req.Redirects != nil || req.ExtraExtensions != nil || req.TypingsLocation != "") {
			panic("Resolver-only inputs supplied for graph observation")
		}
		if err := encoder.Encode(observe(req)); err != nil {
			panic(err)
		}
	}
	if err := scanner.Err(); err != nil {
		fmt.Fprintln(os.Stderr, err)
		os.Exit(1)
	}
}

// Decode's case-insensitive Go field matching is not the declared NDJSON
// schema. Validate exact DTO/exported-model names before invoking any API.
func exactRequestFields(data json.RawMessage, typ reflect.Type) error {
	for typ.Kind() == reflect.Pointer {
		typ = typ.Elem()
	}
	if stdbytes.Equal(stdbytes.TrimSpace(data), []byte("null")) {
		return nil
	}
	switch typ.Kind() {
	case reflect.Struct:
		var fields map[string]json.RawMessage
		if err := json.Unmarshal(data, &fields); err != nil {
			return err
		}
		known := map[string]reflect.Type{}
		for index := 0; index < typ.NumField(); index++ {
			field := typ.Field(index)
			if field.IsExported() {
				known[field.Name] = field.Type
			}
		}
		for name, value := range fields {
			field, ok := known[name]
			if !ok {
				return fmt.Errorf("Unknown resolution request field: %s", name)
			}
			if err := exactRequestFields(value, field); err != nil {
				return err
			}
		}
	case reflect.Slice:
		var values []json.RawMessage
		if err := json.Unmarshal(data, &values); err != nil {
			return err
		}
		for _, value := range values {
			if err := exactRequestFields(value, typ.Elem()); err != nil {
				return err
			}
		}
	}
	return nil
}
