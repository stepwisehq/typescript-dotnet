// Authored behavioral observer over unmodified pinned compiler.NewProgram.
// All actual library/type/reference loading stages run. No old graph overlay.
package main

import (
	"bufio"
	"bytes"
	"context"
	"crypto/sha256"
	"encoding/base64"
	"encoding/json"
	"fmt"
	"os"
	"reflect"
	"sort"

	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/bundled"
	"github.com/microsoft/TypeScript/tsc/internal/collections"
	"github.com/microsoft/TypeScript/tsc/internal/compiler"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"github.com/microsoft/TypeScript/tsc/internal/diagnostics"
	"github.com/microsoft/TypeScript/tsc/internal/locale"
	"github.com/microsoft/TypeScript/tsc/internal/module"
	"github.com/microsoft/TypeScript/tsc/internal/packagejson"
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
type changeInput struct {
	Kind, File string
	Files      []fileInput
}
type request struct {
	Name, ConfigPath                                                   string
	Files                                                              []fileInput
	CaseInsensitive, UseSourceOfProjectReference, SkipModuleResolution bool
	SingleThreaded                                                     *bool
	TypingsLocation                                                    string
	Bind                                                               bool
	Changes                                                            []changeInput
	Dependency                                                         *dependencyInput
	ResolverDecorator                                                  *resolverDecoratorInput
	MapperProject                                                      *mapperProjectInput
	Semantic                                                           *semanticInput
}

func inputBytes(text string, raw *string) string {
	if raw == nil {
		return text
	}
	data, err := base64.StdEncoding.Strict().DecodeString(*raw)
	if err != nil {
		panic(err)
	}
	return string(data)
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
		o.owners = append(o.owners, map[string]any{"id": id, "nameBase64": b64(f.FileName()), "pathBase64": b64(f.PathKey()), "textSha256": fmt.Sprintf("%x", sha256.Sum256([]byte(f.Text()))), "textBytes": len(f.Text())})
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

func fixtureFiles(files []fileInput) (map[string]string, map[string]string) {
	contents, links := map[string]string{}, map[string]string{}
	for _, file := range files {
		path := inputBytes(file.Path, file.PathBase64)
		if _, ok := contents[path]; ok {
			panic("Duplicate fixture file")
		}
		if _, ok := links[path]; ok {
			panic("Duplicate fixture link")
		}
		if file.Symlink != nil {
			links[path] = *file.Symlink
		} else {
			contents[path] = inputBytes(file.Text, file.TextBase64)
		}
	}
	return contents, links
}

func makeFileSystem(contents, links map[string]string, sensitivity tspath.CaseSensitivity) vfs.FS {
	entries := map[string]any{}
	for path, text := range contents {
		entries[path] = []byte(text)
	}
	for path, target := range links {
		if _, ok := entries[path]; ok {
			panic("Fixture file/link collision")
		}
		entries[path] = vfstest.Symlink(target)
	}
	return bundled.WrapFS(vfstest.FromMap(entries, sensitivity))
}

func snapshot(o *observer, p *compiler.Program, bind bool, semantic *semanticInput) any {
	files := []any{}
	for _, file := range p.SourceFiles() {
		id := o.file(file)
		row := map[string]any{
			"id": id, "nameBase64": b64(file.FileName()), "pathBase64": b64(file.PathKey()),
			"textSha256": fmt.Sprintf("%x", sha256.Sum256([]byte(file.Text()))), "textBytes": len(file.Text()),
			"scriptKind": file.ScriptKind, "declaration": file.IsDeclarationFile,
			"externalModule": file.ExternalModuleIndicator != nil,
			"metadata":       value(p.GetSourceFileMetaData(file.PathKey())),
			"defaultLibrary": p.IsSourceFileDefaultLibrary(file.PathKey()), "library": p.IsLibFile(file),
			"externalLibrary": p.IsSourceFileFromExternalLibrary(file), "globalTypings": p.IsGlobalTypingsFile(file.FileName()),
			"parseDiagnostics":   o.diagnostics(file.Diagnostics()),
			"includeDiagnostics": o.diagnostics(p.GetIncludeProcessorDiagnostics(file)),
			"defaultMode":        p.GetDefaultResolutionModeForFile(file),
			"emitFormat":         p.GetEmitModuleFormatOfFile(file),
			"references":         value(file.ReferencedFiles), "typeReferences": value(file.TypeReferenceDirectives), "libReferences": value(file.LibReferenceDirectives),
		}
		row["mappedSource"] = o.mappedSource(file)
		files = append(files, row)
	}
	fileKeys := []string{}
	for path := range p.FilesByPath() {
		fileKeys = append(fileKeys, path.AsString())
	}
	sort.Strings(fileKeys)
	aliases := []any{}
	for _, path := range fileKeys {
		aliases = append(aliases, map[string]any{"pathBase64": b64(path), "file": o.file(p.GetSourceFileByPath(tspath.PathKey(path)))})
	}
	modules := []any{}
	p.ForEachResolvedModule(func(resolution *module.ResolvedModule, name string, mode core.ResolutionMode, path tspath.PathKey) {
		var target *int
		if resolution.IsResolved() {
			target = o.file(p.GetSourceFileForResolvedModule(resolution))
		}
		modules = append(modules, map[string]any{"pathBase64": b64(path), "nameBase64": b64(name), "mode": mode, "result": o.module(resolution), "target": target})
	}, nil)
	directives := []any{}
	p.ForEachResolvedTypeReferenceDirective(func(resolution *module.ResolvedTypeReferenceDirective, name string, mode core.ResolutionMode, path tspath.PathKey) {
		var target *int
		if resolution != nil && resolution.IsResolved() {
			target = o.file(p.GetSourceFileByPath(resolution.ResolvedPath))
		}
		directives = append(directives, map[string]any{"pathBase64": b64(path), "nameBase64": b64(name), "mode": mode, "result": o.directive(resolution), "target": target})
	}, nil)
	// These are authored association tables from unordered source maps, not search/trace sequences.
	sortAssociations := func(rows []any) {
		sort.SliceStable(rows, func(i, j int) bool {
			a, b := rows[i].(map[string]any), rows[j].(map[string]any)
			ap, _ := base64.StdEncoding.DecodeString(a["pathBase64"].(string))
			bp, _ := base64.StdEncoding.DecodeString(b["pathBase64"].(string))
			if string(ap) != string(bp) {
				return string(ap) < string(bp)
			}
			an, _ := base64.StdEncoding.DecodeString(a["nameBase64"].(string))
			bn, _ := base64.StdEncoding.DecodeString(b["nameBase64"].(string))
			if string(an) != string(bn) {
				return string(an) < string(bn)
			}
			return a["mode"].(core.ResolutionMode) < b["mode"].(core.ResolutionMode)
		})
	}
	sortAssociations(modules)
	sortAssociations(directives)
	references := []any{}
	for _, ref := range p.GetResolvedProjectReferences() {
		if ref == nil {
			references = append(references, nil)
		} else {
			references = append(references, map[string]any{"configBase64": b64(ref.ConfigFileName()), "roots": value(ref.FileNames()), "options": value(ref.CompilerOptions()), "references": value(ref.ProjectReferences())})
		}
	}
	referenceTraversal := []any{}
	p.RangeResolvedProjectReference(func(path tspath.PathKey, config *tsoptions.ParsedCommandLine, parent *tsoptions.ParsedCommandLine, index int) bool {
		var configName any
		if config != nil {
			configName = b64(config.ConfigFileName())
		}
		referenceTraversal = append(referenceTraversal, map[string]any{"pathBase64": b64(path), "configBase64": configName, "parentBase64": b64(parent.ConfigFileName()), "index": index})
		return true
	})
	var explain bytes.Buffer
	p.ExplainFiles(&explain, locale.Default, tspath.RootedDirectoryPath("/"))
	result := map[string]any{
		"files": files, "filesByPath": aliases, "duplicateSources": value(p.DuplicateSourceFiles()), "options": value(p.Options()), "roots": value(p.CommandLine().FileNames()),
		"configDiagnostics": o.diagnostics(p.GetConfigFileParsingDiagnostics()), "programDiagnostics": o.diagnostics(p.GetProgramDiagnostics()),
		"syntacticDiagnostics": o.diagnostics(p.GetSyntacticDiagnostics(context.Background(), nil)),
		"modules":              modules, "typeDirectives": directives, "references": references, "referenceTraversal": referenceTraversal, "commonSourceDirectory": value(p.CommonSourceDirectory()),
		"unresolvedImports": value(p.GetUnresolvedImports()), "resolvedPackages": value(p.ResolvedPackageNames()), "unresolvedPackages": value(p.UnresolvedPackageNames()), "deepImports": value(p.DeepImportPackageNames()),
		"explainFilesBase64": b64(explain.String()), "moduleResolutionErrorBase64": nil,
	}
	if err := p.ModuleResolutionError(); err != nil {
		result["moduleResolutionErrorBase64"] = b64(err.Error())
	}
	if bind {
		result["bindDiagnostics"] = o.diagnostics(p.GetBindDiagnostics(context.Background(), nil))
	} else {
		result["bindDiagnostics"] = nil
	}
	if semantic != nil {
		result["semantic"] = observeSemantics(o, p, semantic)
	}
	return result
}

func observe(input request) any {
	if input.ResolverDecorator != nil && (input.SingleThreaded == nil || !*input.SingleThreaded || len(input.Changes) != 0) {
		panic("Authored resolver callback host requires single-threaded fresh construction")
	}
	if input.MapperProject != nil && len(input.Changes) != 0 {
		panic("Authored mapper callback fixture requires fresh construction")
	}
	if input.Dependency != nil {
		return observeDependency(input.Name, input.Dependency)
	}
	sensitivity := tspath.CaseSensitive
	if input.CaseInsensitive {
		sensitivity = tspath.CaseInsensitive
	}
	contents, links := fixtureFiles(input.Files)
	fs := makeFileSystem(contents, links, sensitivity)
	parsed, readErrors := tsoptions.GetParsedCommandLineOfConfigFile(tspath.RootedFilePath(input.ConfigPath), nil, nil, fs, nil)
	project := configureMapperProject(parsed, input.MapperProject)
	o := &observer{files: map[*ast.SourceFile]int{}}
	traces := []any{}
	var host compiler.CompilerHost = compiler.NewCompilerHost(fs, bundled.LibPath(), nil, func(message *diagnostics.Message, args ...any) {
		traces = append(traces, map[string]any{"code": message.Code(), "key": string(message.Key()), "templateBase64": b64(message.String()), "args": value(args)})
	}, project)
	var factory compiler.ProgramFactories
	if input.ResolverDecorator != nil {
		captured := &captureSourceHost{CompilerHost: host, files: map[tspath.RootedFilePath]*ast.SourceFile{}}
		host = captured
		factory.CreateModuleResolver = func(options module.ResolverOptions) module.Resolver {
			return &decoratedResolver{Resolver: module.NewResolver(options), host: captured, input: input.ResolverDecorator}
		}
	}
	result := map[string]any{"name": input.Name, "readDiagnostics": o.diagnostics(readErrors), "snapshots": []any{}, "changes": []any{}}
	if parsed != nil {
		single := core.TSUnknown
		if input.SingleThreaded != nil {
			if *input.SingleThreaded {
				single = core.TSTrue
			} else {
				single = core.TSFalse
			}
		}
		p := compiler.NewProgram(compiler.ProgramOptions{ProgramConfig: compiler.ProgramConfig{Config: parsed, UseSourceOfProjectReference: input.UseSourceOfProjectReference, SingleThreaded: single, SkipModuleResolution: input.SkipModuleResolution, TypingsLocation: tspath.RootedDirectoryPath(input.TypingsLocation)}, ProgramHosts: compiler.ProgramHosts{Host: host}, ProgramFactories: factory})
		result["snapshots"] = append(result["snapshots"].([]any), snapshot(o, p, input.Bind, input.Semantic))
		for _, change := range input.Changes {
			more, moreLinks := fixtureFiles(change.Files)
			for key, text := range more {
				contents[key] = text
			}
			for key, target := range moreLinks {
				links[key] = target
			}
			newFS := makeFileSystem(contents, links, sensitivity)
			newHost := compiler.NewCompilerHost(newFS, bundled.LibPath(), nil, nil, nil)
			old := p
			var file *ast.SourceFile
			var reused bool
			path := sensitivity.PathKey(tspath.RootedPath(change.File))
			switch change.Kind {
			case "reuse":
				p, file, reused = p.ReuseProgram(path, newHost, nil, nil)
			case "update":
				p, file, reused = p.UpdateProgram(path, newHost, nil, nil)
			default:
				panic("Unknown Program lifecycle operation")
			}
			result["changes"] = append(result["changes"].([]any), map[string]any{"kind": change.Kind, "reused": reused, "changedFile": o.file(file), "programReturned": p != nil, "sameFileNames": p != nil && p.HasSameFileNames(old)})
			if p == nil {
				p = old
			} else {
				result["snapshots"] = append(result["snapshots"].([]any), snapshot(o, p, input.Bind, input.Semantic))
			}
		}
	}
	result["owners"] = o.owners
	result["traces"] = traces
	return result
}

func main() {
	if len(os.Args) == 2 && os.Args[1] == "--json-error-controls" {
		if err := json.NewEncoder(os.Stdout).Encode(jsonErrorProjectionControls()); err != nil {
			panic(err)
		}
		return
	}
	if len(os.Args) != 1 {
		panic("Unknown Program oracle arguments")
	}
	input := bufio.NewScanner(os.Stdin)
	input.Buffer(make([]byte, 4096), 128*1024*1024)
	output := json.NewEncoder(os.Stdout)
	for input.Scan() {
		var request request
		decoder := json.NewDecoder(bytes.NewReader(input.Bytes()))
		decoder.DisallowUnknownFields()
		if err := decoder.Decode(&request); err != nil {
			panic(err)
		}
		if err := output.Encode(observe(request)); err != nil {
			panic(err)
		}
	}
	if err := input.Err(); err != nil {
		panic(err)
	}
}
