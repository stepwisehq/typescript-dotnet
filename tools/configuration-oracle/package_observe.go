// Authored public packageJSON wrapper/state observations. No parser work is
// repeated and no private fields or cache topology are inspected.
package main

import "github.com/microsoft/TypeScript/tsc/internal/packagejson"

func expectedValue[T any](e packagejson.Expected[T], encoded any) any {
	return map[string]any{"present": e.IsPresent(), "valid": e.Valid, "null": e.Null,
		"expectedType": e.ExpectedJSONType(), "actualType": e.ActualJSONType(), "value": encoded}
}

func packageValue(v packagejson.JSONValue) any {
	var encoded any
	switch v.Type {
	case packagejson.JSONValueTypeArray:
		var items []any
		if v.AsArray() != nil {
			items = []any{}
			for _, item := range v.AsArray() {
				items = append(items, packageValue(item))
			}
		}
		encoded = map[string]any{"kind": "list", "items": items}
	case packagejson.JSONValueTypeObject:
		entries := []any{}
		for key, item := range v.AsObject().Entries() {
			entries = append(entries, map[string]any{"keyBase64": b64(key), "value": packageValue(item)})
		}
		encoded = map[string]any{"kind": "ordered-map", "entries": entries}
	default:
		encoded = value(v.Value)
	}
	return map[string]any{"type": int(v.Type), "present": v.IsPresent(), "falsy": v.IsFalsy(), "value": encoded}
}

func packageExports(v packagejson.ExportsOrImports) any {
	var encoded any
	switch v.Type {
	case packagejson.JSONValueTypeArray:
		var items []any
		if v.AsArray() != nil {
			items = []any{}
			for _, item := range v.AsArray() {
				items = append(items, packageExports(item))
			}
		}
		encoded = map[string]any{"kind": "list", "items": items}
	case packagejson.JSONValueTypeObject:
		entries := []any{}
		for key, item := range v.AsObject().Entries() {
			entries = append(entries, map[string]any{"keyBase64": b64(key), "value": packageExports(item)})
		}
		encoded = map[string]any{"kind": "ordered-map", "entries": entries}
	default:
		encoded = value(v.Value)
	}
	return map[string]any{"type": int(v.Type), "present": v.IsPresent(), "falsy": v.IsFalsy(), "subpaths": v.IsSubpaths(), "imports": v.IsImports(), "conditions": v.IsConditions(), "value": encoded}
}

func packageFields(f packagejson.Fields) any {
	mapper := f.ContentMapper.Value
	return map[string]any{
		"name": expectedValue(f.Name, value(f.Name.Value)), "version": expectedValue(f.Version, value(f.Version.Value)), "type": expectedValue(f.Type, value(f.Type.Value)),
		"tsconfig": expectedValue(f.TSConfig, value(f.TSConfig.Value)), "main": expectedValue(f.Main, value(f.Main.Value)), "types": expectedValue(f.Types, value(f.Types.Value)), "typings": expectedValue(f.Typings, value(f.Typings.Value)),
		"typesVersions": packageValue(f.TypesVersions), "imports": packageExports(f.Imports), "exports": packageExports(f.Exports),
		"dependencies": expectedValue(f.Dependencies, value(f.Dependencies.Value)), "devDependencies": expectedValue(f.DevDependencies, value(f.DevDependencies.Value)), "peerDependencies": expectedValue(f.PeerDependencies, value(f.PeerDependencies.Value)), "optionalDependencies": expectedValue(f.OptionalDependencies, value(f.OptionalDependencies.Value)),
		"contentMapper": expectedValue(f.ContentMapper, map[string]any{"exec": expectedValue(mapper.Exec, value(mapper.Exec.Value)), "compilerOptions": expectedValue(mapper.CompilerOptions, value(mapper.CompilerOptions.Value)), "dynamicConfig": expectedValue(mapper.DynamicConfig, value(mapper.DynamicConfig.Value))}),
	}
}
