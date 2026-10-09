// Authored standalone observations of the pinned configuration subsystem.
// No Program, Checker, compiler execution or metadata instrumentation is used.
package main

import (
	"bufio"
	"encoding/base64"
	"encoding/json"
	"fmt"
	iofs "io/fs"
	"math"
	"os"
	"reflect"
	"sort"
	"strings"
	"testing/fstest"

	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/collections"
	"github.com/microsoft/TypeScript/tsc/internal/contentmapper"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"github.com/microsoft/TypeScript/tsc/internal/diagnosticwriter"
	tsjson "github.com/microsoft/TypeScript/tsc/internal/json"
	"github.com/microsoft/TypeScript/tsc/internal/locale"
	"github.com/microsoft/TypeScript/tsc/internal/packagejson"
	"github.com/microsoft/TypeScript/tsc/internal/semver"
	"github.com/microsoft/TypeScript/tsc/internal/tsoptions"
	"github.com/microsoft/TypeScript/tsc/internal/tspath"
	"github.com/microsoft/TypeScript/tsc/internal/vfs"
	"github.com/microsoft/TypeScript/tsc/internal/vfs/vfstest"
)

// Tagged raw values preserve ordered maps, nil/empty arrays and arbitrary string
// bytes. Ordinary request names and public UTF8 paths use JSON strings only once.
type RawValue struct {
	Kind    string      `json:"kind"`
	Bytes   string      `json:"bytesBase64,omitempty"`
	Boolean bool        `json:"boolean,omitempty"`
	Number  string      `json:"number,omitempty"`
	Null    bool        `json:"null,omitempty"`
	Items   []*RawValue `json:"items,omitempty"`
	Entries []RawEntry  `json:"entries,omitempty"`
}
type RawEntry struct {
	Key   string    `json:"keyBase64"`
	Value *RawValue `json:"value"`
}
type FileInput struct {
	Path          string  `json:"path"`
	PathBase64    *string `json:"pathBase64,omitempty"`
	Text          string  `json:"text"`
	TextBase64    *string `json:"textBase64,omitempty"`
	Symlink       *string `json:"symlink,omitempty"`
	SymlinkBase64 *string `json:"symlinkBase64,omitempty"`
	Directory     bool    `json:"directory,omitempty"`
	ReadFailure   bool    `json:"readFailure,omitempty"`
}
type Request struct {
	CliAction             string       `json:"cliAction,omitempty"`
	Constructor           string       `json:"constructor,omitempty"`
	Depth                 int          `json:"depth,omitempty"`
	Name                  string       `json:"name"`
	Mode                  string       `json:"mode"`
	ConfigPath            string       `json:"configPath"`
	ConfigPathBase64      *string      `json:"configPathBase64,omitempty"`
	BaseDirectory         string       `json:"baseDirectory"`
	BaseDirectoryBase64   *string      `json:"baseDirectoryBase64,omitempty"`
	CaseSensitivity       *int         `json:"caseSensitivity,omitempty"`
	Text                  *string      `json:"text,omitempty"`
	TextBase64            *string      `json:"textBase64,omitempty"`
	Value                 *RawValue    `json:"value,omitempty"`
	Files                 []FileInput  `json:"files"`
	Arguments             []string     `json:"arguments"`
	ArgumentsBase64       []string     `json:"argumentsBase64,omitempty"`
	ExistingOptions       *RawValue    `json:"existingOptions,omitempty"`
	ExistingOptionsRaw    *RawValue    `json:"existingOptionsRaw,omitempty"`
	FileQueries           []string     `json:"fileQueries"`
	DirectoryQueries      []string     `json:"directoryQueries"`
	ReloadFiles           *[]FileInput `json:"reloadFiles,omitempty"`
	ConfigurationSequence []string     `json:"configurationSequence,omitempty"`
	FormatNewLine         *string      `json:"formatNewLine,omitempty"`
}

func b64[S ~string](s S) string { return base64.StdEncoding.EncodeToString([]byte(s)) }
func unb64(s string) string {
	b, err := base64.StdEncoding.Strict().DecodeString(s)
	if err != nil {
		panic(err)
	}
	return string(b)
}
func input(s string, raw *string) string {
	if raw != nil {
		return unb64(*raw)
	}
	return s
}
func decode(v *RawValue) any {
	if v == nil || v.Kind == "null" {
		return nil
	}
	switch v.Kind {
	case "bytes":
		return unb64(v.Bytes)
	case "json-value":
		if v.Null {
			return tsjson.Value(nil)
		}
		return tsjson.Value([]byte(unb64(v.Bytes)))
	case "boolean":
		return v.Boolean
	case "number":
		var number float64
		if err := json.Unmarshal([]byte(v.Number), &number); err != nil {
			panic(err)
		}
		return number
	case "list":
		if v.Null {
			return []any(nil)
		}
		out := make([]any, len(v.Items))
		for i, item := range v.Items {
			out[i] = decode(item)
		}
		return out
	case "ordered-map":
		if v.Null {
			panic("Nil ordered-map pointer is an invalid original API input.")
		}
		out := &collections.OrderedMap[string, any]{}
		for _, e := range v.Entries {
			out.Set(unb64(e.Key), decode(e.Value))
		}
		return out
	case "map":
		if v.Null {
			return map[string]any(nil)
		}
		out := map[string]any{}
		for _, e := range v.Entries {
			out[unb64(e.Key)] = decode(e.Value)
		}
		return out
	default:
		panic("Unknown tagged input kind: " + v.Kind)
	}
}

// Reflection here enumerates original stored exported data only. It invokes no
// defaults, resolver, mapper or semantic getter; managed AOT uses static fields.
func value(x any) any {
	if x == nil {
		return map[string]any{"kind": "null"}
	}
	switch v := x.(type) {
	case struct{}:
		// Original convertPropertyValueToJson publishes this distinct empty
		// sentinel for absent root syntax; it is not an allocated ordered map.
		return map[string]any{"kind": "empty-config-object"}
	case *collections.OrderedMap[string, any]:
		var entries []any
		if v != nil {
			entries = []any{}
			for k, item := range v.Entries() {
				entries = append(entries, map[string]any{"keyBase64": b64(k), "value": value(item)})
			}
		}
		return map[string]any{"kind": "ordered-map", "entries": entries}
	case *collections.OrderedMap[string, []string]:
		var entries []any
		if v != nil {
			entries = []any{}
			for k, item := range v.Entries() {
				entries = append(entries, map[string]any{"keyBase64": b64(k), "value": value(item)})
			}
		}
		return map[string]any{"kind": "ordered-map", "entries": entries}
	}
	v := reflect.ValueOf(x)
	switch v.Kind() {
	case reflect.Interface:
		if v.IsNil() {
			return map[string]any{"kind": "null"}
		}
		return value(v.Elem().Interface())
	case reflect.Pointer:
		if v.IsNil() {
			return map[string]any{"kind": "null"}
		}
		return value(v.Elem().Interface())
	case reflect.String:
		return map[string]any{"kind": "bytes", "bytesBase64": b64(v.String())}
	case reflect.Bool:
		return map[string]any{"kind": "boolean", "boolean": v.Bool()}
	case reflect.Int, reflect.Int8, reflect.Int16, reflect.Int32, reflect.Int64:
		return map[string]any{"kind": "integer", "number": fmt.Sprint(v.Int())}
	case reflect.Uint, reflect.Uint8, reflect.Uint16, reflect.Uint32, reflect.Uint64:
		return map[string]any{"kind": "integer", "number": fmt.Sprint(v.Uint())}
	case reflect.Float64:
		return map[string]any{"kind": "float64", "bits": fmt.Sprintf("%016x", math.Float64bits(v.Float()))}
	case reflect.Slice:
		var items []any
		if !v.IsNil() {
			items = []any{}
			for i := 0; i < v.Len(); i++ {
				items = append(items, value(v.Index(i).Interface()))
			}
		}
		return map[string]any{"kind": "list", "items": items}
	case reflect.Map:
		if v.Type().Key().Kind() != reflect.String {
			panic("Unknown stored map key: " + v.Type().String())
		}
		var entries []any
		if !v.IsNil() {
			entries = []any{}
			keys := v.MapKeys()
			sort.Slice(keys, func(i, j int) bool { return keys[i].String() < keys[j].String() })
			for _, k := range keys {
				entries = append(entries, map[string]any{"keyBase64": b64(k.String()), "value": value(v.MapIndex(k).Interface())})
			}
		}
		return map[string]any{"kind": "map", "entries": entries}
	case reflect.Struct:
		typ := v.Type()
		name := typ.PkgPath() + "." + typ.Name()
		switch name {
		case "github.com/microsoft/TypeScript/tsc/internal/core.CompilerOptions",
			"github.com/microsoft/TypeScript/tsc/internal/core.TypeAcquisition",
			"github.com/microsoft/TypeScript/tsc/internal/core.BuildOptions",
			"github.com/microsoft/TypeScript/tsc/internal/core.ProjectReference",
			"github.com/microsoft/TypeScript/tsc/internal/core.PluginImport",
			"github.com/microsoft/TypeScript/tsc/internal/tsoptions.TSConfig",
			"github.com/microsoft/TypeScript/tsc/internal/contentmapper.Mapper",
			"github.com/microsoft/TypeScript/tsc/internal/contentmapper.Definition",
			"github.com/microsoft/TypeScript/tsc/internal/contentmapper.Manifest":
		default:
			panic("Unknown stored record family: " + name)
		}
		fields := []any{}
		for i := 0; i < v.NumField(); i++ {
			field := typ.Field(i)
			if !field.IsExported() {
				continue
			}
			jsonName, _, _ := strings.Cut(field.Tag.Get("json"), ",")
			fields = append(fields, map[string]any{"name": field.Name, "jsonName": jsonName, "declaredType": field.Type.String(), "value": value(v.Field(i).Interface())})
		}
		return map[string]any{"kind": "record", "type": typ.Name(), "fields": fields}
	default:
		panic("Unknown stored value: " + v.Type().String())
	}
}

// vfstest's POSIX MapFS cannot construct UNC roots. This finite host adapter
// relocates only actual FS paths; TsOptions receives original UNC config paths.
const uncBacking = "/__configuration_oracle_unc__"

func uncInput(path string) string {
	if path == uncBacking || strings.HasPrefix(path, uncBacking+"/") {
		panic("UNC backing-prefix collision")
	}
	if strings.HasPrefix(path, "//") {
		return uncBacking + path[1:]
	}
	return path
}
func uncOutput(path string) string {
	if strings.HasPrefix(path, uncBacking+"/") {
		return "/" + path[len(uncBacking):]
	}
	return path
}

type uncFS struct{ vfs.FS }

func (f *uncFS) FileExists(path tspath.RootedFilePath) bool {
	return f.FS.FileExists(tspath.RootedFilePath(uncInput(path.AsString())))
}
func (f *uncFS) DirectoryExists(path tspath.RootedDirectoryPath) bool {
	return f.FS.DirectoryExists(tspath.RootedDirectoryPath(uncInput(path.AsString())))
}
func (f *uncFS) ReadFile(path tspath.RootedFilePath) (string, bool) {
	return f.FS.ReadFile(tspath.RootedFilePath(uncInput(path.AsString())))
}
func (f *uncFS) GetAccessibleEntries(path tspath.RootedDirectoryPath) vfs.Entries {
	return f.FS.GetAccessibleEntries(tspath.RootedDirectoryPath(uncInput(path.AsString())))
}
func (f *uncFS) Stat(path tspath.RootedPath) vfs.FileInfo {
	return f.FS.Stat(tspath.RootedPath(uncInput(path.AsString())))
}
func (f *uncFS) Realpath(path tspath.RootedPath) tspath.RootedPath {
	return tspath.RootedPath(uncOutput(f.FS.Realpath(tspath.RootedPath(uncInput(path.AsString()))).AsString()))
}
func filesystem(files []FileInput, cs tspath.CaseSensitivity) vfs.FS {
	unc := false
	for _, f := range files {
		if strings.HasPrefix(input(f.Path, f.PathBase64), "//") {
			unc = true
		}
	}
	if unc {
		for _, f := range files {
			if !strings.HasPrefix(input(f.Path, f.PathBase64), "//") {
				panic("Mixed UNC/POSIX fixture paths")
			}
		}
	}
	entries := map[string]any{}
	for _, f := range files {
		path := input(f.Path, f.PathBase64)
		if unc {
			path = uncInput(path)
		}
		if _, ok := entries[path]; ok {
			panic("Duplicate virtual file path")
		}
		if f.Directory {
			entries[path] = &fstest.MapFile{Mode: iofs.ModeDir}
		} else if f.Symlink != nil || f.SymlinkBase64 != nil {
			target := ""
			if f.Symlink != nil {
				target = *f.Symlink
			}
			target = input(target, f.SymlinkBase64)
			if unc {
				if !strings.HasPrefix(target, "//") {
					panic("Mixed UNC symlink target")
				}
				target = uncInput(target)
			}
			entries[path] = vfstest.Symlink(target)
		} else {
			entries[path] = []byte(input(f.Text, f.TextBase64))
		}
	}
	base := vfstest.FromMap(entries, cs)
	failures := map[tspath.PathKey]bool{}
	for _, f := range files {
		if f.ReadFailure {
			path := tspath.RootedPath(input(f.Path, f.PathBase64))
			failures[cs.PathKey(path)] = true
		}
	}
	var result vfs.FS = base
	if unc {
		result = &uncFS{base}
	}
	return &readFailureFS{result, failures}

}

type readFailureFS struct {
	vfs.FS
	failures map[tspath.PathKey]bool
}

func (f *readFailureFS) ReadFile(path tspath.RootedFilePath) (string, bool) {
	if f.failures[f.CaseSensitivity().PathKey(path.AsPath())] {
		return "", false
	}
	return f.FS.ReadFile(path)
}

type owner struct {
	ID   int    `json:"id"`
	Name string `json:"nameBase64"`
	Path string `json:"pathBase64"`
	Text string `json:"textBase64"`
}
type diagnostic struct {
	Code        int32         `json:"code"`
	Category    int32         `json:"category"`
	Key         string        `json:"key"`
	Text        string        `json:"textBase64"`
	Arguments   []string      `json:"argsBase64"`
	Pos         int           `json:"pos"`
	End         int           `json:"end"`
	File        *int          `json:"file"`
	Source      string        `json:"sourceBase64"`
	Unnecessary bool          `json:"reportsUnnecessary"`
	Deprecated  bool          `json:"reportsDeprecated"`
	Skip        bool          `json:"skippedOnNoEmit"`
	Chain       []*diagnostic `json:"chain"`
	Related     []*diagnostic `json:"related"`
}
type observer struct {
	ids    map[*ast.SourceFile]int
	owners []owner
}

func (o *observer) file(f *ast.SourceFile) *int {
	if f == nil {
		return nil
	}
	id, ok := o.ids[f]
	if !ok {
		id = len(o.owners)
		o.ids[f] = id
		o.owners = append(o.owners, owner{id, b64(f.FileName()), b64(f.PathKey()), b64(f.Text())})
	}
	return &id
}
func (o *observer) diagnostics(ds []*ast.Diagnostic) []*diagnostic {
	if ds == nil {
		return nil
	}
	out := []*diagnostic{}
	for _, d := range ds {
		var args []string
		if d.MessageArgs() != nil {
			args = []string{}
			for _, arg := range d.MessageArgs() {
				args = append(args, b64(arg))
			}
		}
		out = append(out, &diagnostic{d.Code(), int32(d.Category()), string(d.MessageKey()), b64(d.String()), args, d.Pos(), d.End(), o.file(d.File()), b64(d.Source()), d.ReportsUnnecessary(), d.ReportsDeprecated(), d.SkippedOnNoEmit(), o.diagnostics(d.MessageChain()), o.diagnostics(d.RelatedInformation())})
		if d.RepopulateInfo() != nil {
			panic("Unexpected incremental config diagnostic")
		}
	}
	return out
}

type cache struct {
	entries map[tspath.PathKey]*tsoptions.ExtendedConfigCacheEntry
}

func (c *cache) GetExtendedConfig(file tspath.RootedFilePath, path tspath.PathKey, stack []tspath.PathKey, fs vfs.FS) *tsoptions.ExtendedConfigCacheEntry {
	if entry, ok := c.entries[path]; ok {
		return entry
	}
	entry := tsoptions.ParseExtendedConfig(file, path, stack, fs, c)
	c.entries[path] = entry
	return entry
}
func options(raw *RawValue) *core.CompilerOptions {
	if raw == nil {
		return nil
	}
	out := &core.CompilerOptions{}
	mapValue, ok := decode(raw).(*collections.OrderedMap[string, any])
	if !ok {
		panic("Existing options require ordered-map")
	}
	for name, item := range mapValue.Entries() {
		tsoptions.ParseCompilerOptions(name, item, out)
	}
	return out
}

// Public metadata queries are requested behavior, not serializer side effects.
func rawOptions(v *RawValue) *collections.OrderedMap[string, any] {
	if v == nil {
		return nil
	}
	return decode(v).(*collections.OrderedMap[string, any])
}

func (o *observer) parsed(p *tsoptions.ParsedCommandLine, r Request, config tspath.RootedFilePath) any {
	if p == nil {
		return nil
	}
	var sourceID *int
	if p.ConfigFile != nil {
		sourceID = o.file(p.ConfigFile.SourceFile)
	}
	result := map[string]any{"configFile": sourceID, "options": value(p.CompilerOptions()), "typeAcquisition": value(p.ParsedConfig.TypeAcquisition), "fileNames": value(p.FileNames()), "literalFileNames": value(p.LiteralFileNames()), "references": value(p.ProjectReferences()), "contentMappers": value(p.ContentMappers()), "raw": value(p.Raw), "compileOnSave": value(p.CompileOnSave), "errors": o.diagnostics(p.Errors), "baseDirectoryBase64": b64(p.BaseDirectory()), "caseSensitivity": int(p.CaseSensitivity()), "extendedSourceFiles": value(p.ExtendedSourceFiles()), "configParsingDiagnostics": o.diagnostics(p.GetConfigFileParsingDiagnostics()), "commonSourceDirectoryBase64": b64(p.CommonSourceDirectory()), "resolvedReferencePaths": value(p.ResolvedProjectReferencePaths())}
	rootNames := []string{}
	for i := range p.FileNames() {
		rootNames = append(rootNames, b64(p.RootFileNameForDiagnostic(i)))
	}
	result["rootFileNamesForDiagnosticsBase64"] = rootNames
	outputNames := []tspath.RootedFilePath{}
	for f := range p.GetOutputFileNames() {
		outputNames = append(outputNames, f)
	}
	result["outputFileNames"] = value(outputNames)
	result["buildInfoFileNameBase64"] = b64(p.GetBuildInfoFileName())
	result["showConfig"] = value(tsoptions.ConvertToTSConfig(p, config))
	queries := []any{}
	for _, f := range r.FileQueries {
		path := tspath.ToRootedFilePath(f, p.BaseDirectory())
		q := map[string]any{"pathBase64": b64(path), "possiblyMatches": p.PossiblyMatchesFileName(path)}
		if r.Mode != "command" {
			spec, defaultInclude := p.GetMatchedIncludeSpec(path)
			q["fileSpecBase64"] = b64(p.GetMatchedFileSpec(path))
			q["includeSpecBase64"] = b64(spec)
			q["defaultInclude"] = defaultInclude
		}
		queries = append(queries, q)
	}
	result["fileQueries"] = queries
	directories := []any{}
	for _, d := range r.DirectoryQueries {
		path := tspath.ToRootedPath(d, p.BaseDirectory())
		directories = append(directories, map[string]any{"pathBase64": b64(path), "possiblyMatches": p.PossiblyMatchesDirectoryName(p.CaseSensitivity().PathKey(path))})
	}
	result["directoryQueries"] = directories
	result["storedSpecs"] = configurationFields(tsoptions.OracleConfigurationSpecs(p))
	if r.Mode != "command" {
		result["wildcardDirectories"] = value(p.WildcardDirectories())
	}
	return result
}

func configurationFields(fields []tsoptions.OracleConfigurationField) any {
	if fields == nil {
		return nil
	}
	rows := []any{}
	for _, f := range fields {
		rows = append(rows, map[string]any{"name": f.Name, "value": value(f.Value)})
	}
	return rows
}
func (o *observer) configurationCache(c *cache) any {
	keys := []tspath.PathKey{}
	for k := range c.entries {
		keys = append(keys, k)
	}
	sort.Slice(keys, func(i, j int) bool { return keys[i] < keys[j] })
	entries := []any{}
	for _, key := range keys {
		e := tsoptions.OracleConfigurationExtendedConfig(c.entries[key])
		entries = append(entries, map[string]any{"pathBase64": b64(key), "source": o.file(e.Source), "storedSpecs": configurationFields(e.Specs), "extendedSourceFiles": value(e.ExtendedSourceFiles), "hasParsedConfig": e.HasParsedConfig, "raw": value(e.Raw), "options": value(e.Options), "unresolvedPaths": value(e.UnresolvedPaths), "typeAcquisition": value(e.TypeAcquisition), "extendedConfigPaths": value(e.ExtendedConfigPaths), "errors": o.diagnostics(e.Errors)})
	}
	return entries
}
func run(r Request) any {
	if r.Mode == "command" && (len(r.FileQueries) > 0 || len(r.DirectoryQueries) > 0 || r.ReloadFiles != nil) {
		panic("Config-spec queries require config parse mode")
	}
	cs := tspath.CaseSensitive
	if r.CaseSensitivity != nil {
		if *r.CaseSensitivity != 0 && *r.CaseSensitivity != 1 {
			panic("Unknown case policy")
		}
		cs = tspath.CaseSensitivity(*r.CaseSensitivity)
	}
	config := input(r.ConfigPath, r.ConfigPathBase64)
	if config == "" {
		config = "/project/tsconfig.json"
	}
	base := input(r.BaseDirectory, r.BaseDirectoryBase64)
	if base == "" {
		base = tspath.GetDirectoryPath(config)
	}
	configPath := tspath.RootedFilePath(config)
	basePath := tspath.RootedDirectoryPath(base)
	text := ""
	if r.Text != nil {
		text = *r.Text
	}
	text = input(text, r.TextBase64)
	files := append([]FileInput(nil), r.Files...)
	if r.Text != nil || r.TextBase64 != nil {
		found := false
		for i := range files {
			if input(files[i].Path, files[i].PathBase64) == config {
				files[i].Text = text
				encoded := b64(text)
				files[i].TextBase64 = &encoded
				found = true
			}
		}
		if !found {
			encoded := b64(text)
			files = append(files, FileInput{Path: config, TextBase64: &encoded})
		}
	}
	fs := filesystem(files, cs)
	o := &observer{ids: map[*ast.SourceFile]int{}, owners: []owner{}}
	c := &cache{map[tspath.PathKey]*tsoptions.ExtendedConfigCacheEntry{}}
	result := map[string]any{"name": r.Name, "mode": r.Mode}
	var parsed *tsoptions.ParsedCommandLine
	var readErrors []*ast.Diagnostic
	switch r.Mode {
	case "name-map":
		var declarations []*tsoptions.CommandLineOption
		if r.Constructor == "empty" {
			declarations = []*tsoptions.CommandLineOption{}
		} else if r.Constructor != "nil" {
			panic("Unknown empty-name-map constructor")
		}
		names := tsoptions.GetNameMapFromList(declarations)
		records := []any{}
		for _, arg := range r.Arguments {
			records = append(records, map[string]any{"nameBase64": b64(arg), "getIsNil": names.Get(arg) == nil, "shortIsNil": names.GetFromShort(arg) == nil, "longIsNil": names.GetOptionDeclarationFromName(arg, false) == nil, "withShortIsNil": names.GetOptionDeclarationFromName(arg, true) == nil})
		}
		result["records"] = records
	case "mapper-equality":
		terms, ok := decode(r.Value).([]any)
		if !ok || len(terms) != 2 {
			panic("Mapper equality requires two raw terms")
		}
		option := func(value any) tsjson.Value {
			if value == nil {
				return nil
			}
			raw, ok := value.(tsjson.Value)
			if !ok {
				panic("Mapper options require raw JSON")
			}
			return raw
		}
		left, right := &contentmapper.Mapper{Definition: contentmapper.Definition{Options: option(terms[0])}}, &contentmapper.Mapper{Definition: contentmapper.Definition{Options: option(terms[1])}}
		result["equal"] = left.Equals(right)
		result["leftOptionsNil"] = left.Options == nil
		result["rightOptionsNil"] = right.Options == nil
		result["leftLength"] = len(left.Options)
		result["rightLength"] = len(right.Options)
	case "raw-json":
		raw := ""
		if r.Text != nil {
			raw = *r.Text
		}
		raw = input(raw, r.TextBase64)
		var v any
		err := tsjson.Unmarshal([]byte(raw), &v)
		var message *string
		if err != nil {
			text := b64(observedErrorText(err))
			message = &text
		}
		encoded, marshalErr := tsjson.Marshal(v)
		var marshalMessage *string
		if marshalErr != nil {
			text := b64(observedErrorText(marshalErr))
			marshalMessage = &text
		}
		result["errorBase64"] = message
		result["marshaledBase64"] = b64(string(encoded))
		result["marshalErrorBase64"] = marshalMessage
	case "marshal":
		v := decode(r.Value)
		switch r.Constructor {
		case "":
		case "static-tsconfig-nil", "static-tsconfig-empty", "static-tsconfig-false":
			config := &tsoptions.TSConfig{CompilerOptions: &collections.OrderedMap[string, any]{}}
			if r.Constructor == "static-tsconfig-empty" {
				config.References = []any{}
				config.Files = []string{}
				config.Include = []string{}
				config.Exclude = []string{}
			}
			if r.Constructor == "static-tsconfig-false" {
				no := false
				config.CompileOnSave = &no
			}
			v = config
		case "static-acquisition-nil", "static-acquisition-empty":
			options := &core.TypeAcquisition{}
			if r.Constructor == "static-acquisition-empty" {
				options.Include = []string{}
				options.Exclude = []string{}
			}
			v = options
		case "static-build-nil", "static-build-zero":
			options := &core.BuildOptions{}
			if r.Constructor == "static-build-zero" {
				zero := 0
				options.Builders = &zero
			}
			v = options
		case "static-compiler-nil", "static-compiler-empty", "static-compiler-zero":
			options := &core.CompilerOptions{}
			if r.Constructor == "static-compiler-empty" {
				options.Types = []string{}
				options.Plugins = []core.PluginImport{}
				options.Paths = &collections.OrderedMap[string, []string]{}
			}
			if r.Constructor == "static-compiler-zero" {
				a, b := 0, 0
				options.MaxNodeModuleJsDepth = &a
				options.Checkers = &b
			}
			v = options
		case "static-reference-false", "static-reference-original":
			reference := &core.ProjectReference{Path: tspath.RootedPathFromAbsolute("/project/lib"), Circular: false}
			if r.Constructor == "static-reference-original" {
				reference.OriginalPath = "./lib"
			}
			v = reference
		case "definition-raw":
			raw, ok := v.(tsjson.Value)
			if !ok {
				panic("Definition raw options require json-value")
			}
			v = &contentmapper.Definition{Package: "pkg", Extensions: []string{".x"}, Options: raw}
		case "json-value-depth", "list-json-value-depth":
			if r.Depth < 0 || r.Depth > 10001 {
				panic("Invalid raw JSON depth")
			}
			v = tsjson.Value(strings.Repeat("[", r.Depth) + "0" + strings.Repeat("]", r.Depth))
			if r.Constructor == "list-json-value-depth" {
				v = []any{float64(1), v}
			}
		case "list-depth", "map-depth":
			if r.Depth < 0 || r.Depth > 10001 {
				panic("Invalid bounded marshal depth")
			}
			v = float64(0)
			for range r.Depth {
				if r.Constructor == "list-depth" {
					v = []any{v}
				} else {
					v = map[string]any{"x": v}
				}
			}
		case "nan", "positive-infinity", "negative-infinity", "list-nan", "list-positive-infinity", "list-negative-infinity", "map-nan", "map-positive-infinity", "map-negative-infinity":
			term := r.Constructor
			container := ""
			if strings.HasPrefix(term, "list-") {
				container = "list"
				term = strings.TrimPrefix(term, "list-")
			}
			if strings.HasPrefix(term, "map-") {
				container = "map"
				term = strings.TrimPrefix(term, "map-")
			}
			number := math.NaN()
			if term == "positive-infinity" {
				number = math.Inf(1)
			}
			if term == "negative-infinity" {
				number = math.Inf(-1)
			}
			v = number
			if container == "list" {
				v = []any{number}
			}
			if container == "map" {
				v = map[string]any{"x": number}
			}
		case "list-cycle":
			list := make([]any, 1)
			list[0] = list
			v = list
		case "map-cycle":
			dict := map[string]any{}
			dict["x"] = dict
			v = dict
		default:
			panic("Unknown bounded marshal constructor")
		}
		encoded, err := tsjson.Marshal(v)
		var message *string
		if err != nil {
			text := b64(observedErrorText(err))
			message = &text
		}
		result["marshaledBase64"] = b64(string(encoded))
		result["errorBase64"] = message
	case "package-json":
		args := r.Arguments
		if r.ArgumentsBase64 != nil {
			args = []string{}
			for _, arg := range r.ArgumentsBase64 {
				args = append(args, unb64(arg))
			}
		}
		records := []any{}
		for _, arg := range args {
			fields, err := packagejson.Parse([]byte(arg))
			var message *string
			if err != nil {
				text := b64(observedErrorText(err))
				message = &text
			}
			records = append(records, map[string]any{"inputBase64": b64(arg), "errorBase64": message, "fields": packageFields(fields)})
		}
		result["records"] = records
	case "raw-options":
		raw := &tsoptions.RawCompilerOptions{}
		args := r.Arguments
		if r.ArgumentsBase64 != nil {
			args = []string{}
			for _, arg := range r.ArgumentsBase64 {
				args = append(args, unb64(arg))
			}
		}
		records := []any{}
		for _, arg := range args {
			err := tsjson.Unmarshal([]byte(arg), raw)
			var message *string
			if err != nil {
				text := b64(observedErrorText(err))
				message = &text
			}
			serialized, marshalError := tsjson.Marshal(raw)
			var marshalMessage *string
			if marshalError != nil {
				text := b64(observedErrorText(marshalError))
				marshalMessage = &text
			}
			finalized, diagnostics := raw.Finalize(basePath)
			records = append(records, map[string]any{"inputBase64": b64(arg), "errorBase64": message, "marshaledBase64": b64(string(serialized)), "marshalErrorBase64": marshalMessage, "options": value(finalized), "diagnostics": o.diagnostics(diagnostics)})
		}
		result["records"] = records
	case "versions", "ranges", "locales":
		args := r.Arguments
		if r.ArgumentsBase64 != nil {
			args = []string{}
			for _, a := range r.ArgumentsBase64 {
				args = append(args, unb64(a))
			}
		}
		records := []any{}
		for _, arg := range args {
			if r.Mode == "locales" {
				parsed, ok := locale.Parse(arg)
				records = append(records, map[string]any{"inputBase64": b64(arg), "parsed": ok, "stringBase64": b64(parsed.String())})
			} else if r.Mode == "versions" {
				v, err := semver.TryParseVersion(arg)
				var message *string
				if err != nil {
					encoded := b64(observedErrorText(err))
					message = &encoded
				}
				records = append(records, map[string]any{"inputBase64": b64(arg), "stringBase64": b64(v.String()), "errorBase64": message})
			} else {
				rangeValue, ok := semver.TryParseVersionRange(arg)
				records = append(records, map[string]any{"inputBase64": b64(arg), "parsed": ok, "stringBase64": b64(rangeValue.String())})
			}
		}
		result["records"] = records

	case "text":
		raw, errors := tsoptions.ParseConfigFileTextToJson(configPath, cs.PathKey(configPath.AsPath()), text)
		result["raw"] = value(raw)
		result["errors"] = o.diagnostics(errors)
		if r.FormatNewLine != nil {
			options := &diagnosticwriter.FormattingOptions{NewLine: *r.FormatNewLine, CurrentDirectory: basePath, CaseSensitivity: cs}
			diagnostics := diagnosticwriter.FromASTDiagnostics(errors)
			var plain, pretty, summary strings.Builder
			diagnosticwriter.WriteFormatDiagnostics(&plain, diagnostics, options)
			diagnosticwriter.FormatDiagnosticsWithColorAndContext(&pretty, diagnostics, options)
			diagnosticwriter.WriteErrorSummaryText(&summary, diagnostics, options)
			result["formatted"] = map[string]any{"plainBase64": b64(plain.String()), "prettyBase64": b64(pretty.String()), "summaryBase64": b64(summary.String())}
		}
	case "source":
		source := tsoptions.NewTsconfigSourceFileFromFilePath(configPath, cs.PathKey(configPath.AsPath()), text)
		o.file(source.SourceFile)
		var existingRaw *collections.OrderedMap[string, any]
		if r.ExistingOptionsRaw != nil {
			existingRaw = decode(r.ExistingOptionsRaw).(*collections.OrderedMap[string, any])
		}
		parsed = tsoptions.ParseJsonSourceFileConfigFileContent(source, fs, basePath, options(r.ExistingOptions), existingRaw, nil, c)
	case "json":
		raw := decode(r.Value)
		parsed = tsoptions.ParseJsonConfigFileContent(raw, fs, basePath, options(r.ExistingOptions), configPath, nil, c)
		result["inputValueAfterParsing"] = value(raw)
	case "file":
		parsed, readErrors = tsoptions.GetParsedCommandLineOfConfigFile(configPath, options(r.ExistingOptions), rawOptions(r.ExistingOptionsRaw), fs, c)
	case "command", "build":
		args := r.Arguments
		if r.ArgumentsBase64 != nil {
			args = []string{}
			for _, a := range r.ArgumentsBase64 {
				args = append(args, unb64(a))
			}
		}
		if r.Mode == "command" {
			parsed = tsoptions.ParseCommandLine(args, fs, basePath)
		} else {
			p := tsoptions.ParseBuildCommandLine(args, fs, basePath)
			result["buildOptions"] = value(p.BuildOptions)
			result["compilerOptions"] = value(p.CompilerOptions)
			result["projects"] = value(p.Projects)
			result["resolvedProjectPaths"] = value(p.ResolvedProjectPaths())
			result["raw"] = value(p.Raw)
			result["errors"] = o.diagnostics(p.Errors)
		}
	default:
		panic("Unknown configuration operation: " + r.Mode)
	}
	if r.Mode == "source" || r.Mode == "json" || r.Mode == "file" || r.Mode == "command" {
		result["parsed"] = o.parsed(parsed, r, configPath)
		result["readErrors"] = o.diagnostics(readErrors)
	}
	if parsed != nil && r.ReloadFiles != nil {
		reloaded := parsed.ReloadFileNamesOfParsedCommandLine(filesystem(*r.ReloadFiles, cs))
		result["reloaded"] = o.parsed(reloaded, r, configPath)
		result["reloadIdentity"] = map[string]any{"distinctParsed": parsed != reloaded, "sameCompilerOptions": parsed.CompilerOptions() == reloaded.CompilerOptions(), "sameConfigSource": parsed.ConfigFile == reloaded.ConfigFile}
	}
	if r.ConfigurationSequence != nil {
		states := []any{}
		for _, f := range r.ConfigurationSequence {
			p, errors := tsoptions.GetParsedCommandLineOfConfigFile(tspath.RootedFilePath(f), options(r.ExistingOptions), rawOptions(r.ExistingOptionsRaw), fs, c)
			states = append(states, map[string]any{"parsed": o.parsed(p, r, tspath.RootedFilePath(f)), "readErrors": o.diagnostics(errors)})
		}
		result["sequence"] = states
	}
	result["extendedConfigCache"] = o.configurationCache(c)
	result["diagnosticFiles"] = o.owners
	return result
}

func main() {
	if len(os.Args) == 2 && os.Args[1] == "--json-error-controls" {
		if err := json.NewEncoder(os.Stdout).Encode(jsonErrorProjectionControls()); err != nil {
			panic(err)
		}
		return
	}
	cliProjection := len(os.Args) == 2 && os.Args[1] == "--cli-projections"
	if len(os.Args) == 2 && os.Args[1] == "--os-projections" {
		if err := json.NewEncoder(os.Stdout).Encode(osReadProjection()); err != nil {
			panic(err)
		}
		return
	}
	if len(os.Args) != 1 && !cliProjection {
		panic("Unknown oracle flag")
	}

	reader := bufio.NewScanner(os.Stdin)
	reader.Buffer(make([]byte, 65536), 64*1024*1024)
	writer := json.NewEncoder(os.Stdout)
	for reader.Scan() {
		var request Request
		decoder := json.NewDecoder(strings.NewReader(reader.Text()))
		decoder.DisallowUnknownFields()
		if err := decoder.Decode(&request); err != nil {
			panic(err)
		}
		var output any
		if cliProjection {
			output = runCLI(request)
		} else {
			output = run(request)
		}
		if err := writer.Encode(output); err != nil {
			panic(err)
		}
	}
	if err := reader.Err(); err != nil {
		panic(err)
	}
}
