// Authored no-emit adapter reuses the accepted Program diagnostic value/owner projection.
// These are complete diagnostic fields and correlated file owners, not a private type census.
package testrunner

import (
	"crypto/sha256"
	"encoding/base64"
	"fmt"
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/collections"
	"github.com/microsoft/TypeScript/tsc/internal/packagejson"
	"reflect"
	"sort"
)

func authoredNoEmitB64[S ~string](s S) string { return base64.StdEncoding.EncodeToString([]byte(s)) }

// Static data projection enumerates actual stored exported fields. Raw byte strings
// have a role-specific base64 encoding; no substring/path relocation is applied.
func authoredNoEmitValue(input any) any {
	if input != nil {
		t := reflect.TypeOf(input)
		if t.PkgPath() == "github.com/microsoft/TypeScript/tsc/internal/module" && t.Name() == "resolutionCandidate" {
			return authoredNoEmitValue(input.(interface{ AsString() string }).AsString())
		}
	}
	if directory, ok := input.(packagejson.PackageDirectory); ok {
		return authoredNoEmitValue(directory.AsDirectoryPath())
	}
	if reference, ok := input.(*ast.FileReference); ok && reference != nil {
		return map[string]any{"FileName": authoredNoEmitValue(reference.FileName), "ResolutionMode": reference.ResolutionMode, "Preserve": reference.Preserve, "pos": reference.Pos(), "end": reference.End()}
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
		return authoredNoEmitValue(keys)
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
			entries = append(entries, map[string]any{"keyBase64": authoredNoEmitB64(key), "value": authoredNoEmitValue(values)})
		}
		return entries
	}
	if object, ok := input.(*collections.OrderedMap[string, packagejson.JSONValue]); ok {
		if object == nil {
			return nil
		}
		entries := []any{}
		for key, item := range object.Entries() {
			entries = append(entries, map[string]any{"keyBase64": authoredNoEmitB64(key), "value": authoredNoEmitValue(item)})
		}
		return entries
	}
	if object, ok := input.(*collections.OrderedMap[string, packagejson.ExportsOrImports]); ok {
		if object == nil {
			return nil
		}
		entries := []any{}
		for key, item := range object.Entries() {
			entries = append(entries, map[string]any{"keyBase64": authoredNoEmitB64(key), "value": authoredNoEmitValue(item)})
		}
		return entries
	}
	v := reflect.ValueOf(input)
	switch v.Kind() {
	case reflect.Interface, reflect.Pointer:
		if v.IsNil() {
			return nil
		}
		return authoredNoEmitValue(v.Elem().Interface())
	case reflect.String:
		return map[string]any{"bytesBase64": authoredNoEmitB64(v.String())}
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
			entries = append(entries, map[string]any{"keyBase64": authoredNoEmitB64(key.String()), "value": authoredNoEmitValue(v.MapIndex(key).Interface())})
		}
		return entries
	case reflect.Slice, reflect.Array:
		if v.Kind() == reflect.Slice && v.IsNil() {
			return nil
		}
		result := []any{}
		for i := 0; i < v.Len(); i++ {
			result = append(result, authoredNoEmitValue(v.Index(i).Interface()))
		}
		return result
	case reflect.Struct:
		result := map[string]any{}
		for i := 0; i < v.NumField(); i++ {
			field := v.Type().Field(i)
			if field.IsExported() {
				result[field.Name] = authoredNoEmitValue(v.Field(i).Interface())
			}
		}
		return result
	default:
		panic("Unobserved stored authoredNoEmitValue family: " + v.Type().String())
	}
}

type authoredNoEmitObserver struct {
	files  map[*ast.SourceFile]int
	owners []any
}

func (o *authoredNoEmitObserver) file(f *ast.SourceFile) *int {
	if f == nil {
		return nil
	}
	id, ok := o.files[f]
	if !ok {
		id = len(o.owners)
		o.files[f] = id
		o.owners = append(o.owners, map[string]any{"id": id, "nameBase64": authoredNoEmitB64(f.FileName()), "pathBase64": authoredNoEmitB64(f.PathKey()), "textSha256": fmt.Sprintf("%x", sha256.Sum256([]byte(f.Text()))), "textBytes": len(f.Text())})
	}
	return &id
}
func (o *authoredNoEmitObserver) diagnostics(ds []*ast.Diagnostic) any {
	if ds == nil {
		return nil
	}
	result := []any{}
	for _, d := range ds {
		result = append(result, map[string]any{"code": d.Code(), "category": d.Category(), "key": string(d.MessageKey()), "textBase64": authoredNoEmitB64(d.String()), "args": authoredNoEmitValue(d.MessageArgs()), "pos": d.Pos(), "end": d.End(), "file": o.file(d.File()), "sourceBase64": authoredNoEmitB64(d.Source()), "reportsUnnecessary": d.ReportsUnnecessary(), "reportsDeprecated": d.ReportsDeprecated(), "skippedOnNoEmit": d.SkippedOnNoEmit(), "chain": o.diagnostics(d.MessageChain()), "related": o.diagnostics(d.RelatedInformation()), "repopulateInfo": authoredNoEmitValue(d.RepopulateInfo())})
	}
	return result
}
