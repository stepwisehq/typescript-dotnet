// Authored direct-source probes for dependencies selected by Program construction.
// This does not call NewProgram or imply complete JSON/mapper process support.
package main

import (
	stdjson "encoding/json"
	jsontext "encoding/json/jsontext"
	jsonv2 "encoding/json/v2"
	"fmt"
	"os"
	"reflect"
	"strconv"

	"github.com/microsoft/TypeScript/tsc/internal/contentmapper"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	tsjson "github.com/microsoft/TypeScript/tsc/internal/json"
	"github.com/microsoft/TypeScript/tsc/internal/spanmap"
)

type dependencyInput struct {
	Kind                                      string
	JSONBase64                                string
	MapperName, MapperVersion, ContributionID string
	MapperOptionsBase64                       *string
	DeclaredOptions                           []string
	CompilerOptionsJSONBase64                 *string
}

func dependencyErrorDetails(err error) any {
	if err == nil {
		return nil
	}
	result := map[string]any{"textBase64": b64(observedErrorText(err)), "type": reflect.TypeOf(err).String()}
	switch e := err.(type) {
	case *jsonv2.SemanticError:
		result["byteOffset"] = e.ByteOffset
		result["jsonPointerBase64"] = b64(string(e.JSONPointer))
		result["jsonKind"] = uint8(e.JSONKind)
		result["jsonValueBase64"] = nil
		if e.JSONValue != nil {
			result["jsonValueBase64"] = b64(string(e.JSONValue))
		}
		result["goType"] = nil
		if e.GoType != nil {
			result["goType"] = e.GoType.String()
		}
		result["cause"] = nil
		if e.Err != nil {
			result["cause"] = dependencyErrorDetails(e.Err)
		}
	case *jsontext.SyntacticError:
		result["byteOffset"] = e.ByteOffset
		result["jsonPointerBase64"] = b64(string(e.JSONPointer))
		result["cause"] = dependencyErrorDetails(e.Err)
	case *strconv.NumError:
		result["function"] = e.Func
		result["numberBase64"] = b64(e.Num)
		result["cause"] = dependencyErrorDetails(e.Err)
	}
	return result
}

// Go-only error layout is diagnostic metadata. The portable API exposes exact
// error rendering and partial destination value, matching the managed API shape.
func dependencyError(err error) any {
	if err == nil {
		return nil
	}
	details := dependencyErrorDetails(err)
	encoded, encodeError := stdjson.Marshal(map[string]any{"kind": "program-dependency-error-details", "details": details})
	if encodeError != nil {
		panic(encodeError)
	}
	fmt.Fprintln(os.Stderr, string(encoded))
	return map[string]any{"textBase64": b64(observedErrorText(err))}
}

func observeDependency(name string, input *dependencyInput) any {
	result := map[string]any{"name": name, "dependencyKind": input.Kind}
	switch input.Kind {
	case "int32-matrix":
		var matrix [][]int32
		data := []byte(inputBytes("", &input.JSONBase64))
		err := tsjson.Unmarshal(data, &matrix)
		result["value"] = value(matrix)
		result["error"] = dependencyError(err)
	case "spanmap-unmarshal":
		parsed, err := spanmap.Unmarshal([]byte(inputBytes("", &input.JSONBase64)))
		result["error"] = dependencyError(err)
		result["mapPresent"] = parsed != nil
		result["segments"] = value(parsed.Segments())
		result["marshalBase64"] = nil
		result["marshalError"] = nil
		if parsed != nil {
			data, marshalError := parsed.Marshal()
			result["marshalBase64"] = b64(string(data))
			result["marshalError"] = dependencyError(marshalError)
		}
	case "mapper-identity":
		var options *core.CompilerOptions
		if input.CompilerOptionsJSONBase64 != nil {
			options = new(core.CompilerOptions)
			err := tsjson.Unmarshal([]byte(inputBytes("", input.CompilerOptionsJSONBase64)), options)
			result["optionsError"] = dependencyError(err)
			if err != nil {
				result["partialOptions"] = value(options)
				return result
			}
		} else {
			result["optionsError"] = nil
		}
		mapper := &contentmapper.Mapper{
			Manifest:       contentmapper.Manifest{Name: input.MapperName, Version: input.MapperVersion, CompilerOptions: input.DeclaredOptions},
			ContributionID: input.ContributionID,
		}
		if input.MapperOptionsBase64 != nil {
			mapper.Options = tsjson.Value(inputBytes("", input.MapperOptionsBase64))
		}
		declared, err := mapper.MarshalDeclaredOptions(options)
		result["declaredError"] = dependencyError(err)
		result["declared"] = nil
		if declared != nil {
			entries := []any{}
			for key, raw := range declared.Entries() {
				entries = append(entries, map[string]any{"nameBase64": b64(key), "rawBase64": b64(string(raw))})
			}
			result["declared"] = entries
		}
		encoded, marshalError := tsjson.Marshal(declared)
		result["declaredJSONBase64"] = b64(string(encoded))
		result["marshalError"] = dependencyError(marshalError)
		identity := mapper.TransformIdentity(options)
		// Exact decimal strings avoid an observer's float64 narrowing of uint64.
		result["hashHi"] = strconv.FormatUint(identity.Hi, 10)
		result["hashLo"] = strconv.FormatUint(identity.Lo, 10)
		result["identityBase64"] = b64(mapper.Identity())
	default:
		panic("Unknown Program dependency probe")
	}
	return result
}
