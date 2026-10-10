// Authored observation adapter for the pinned JSON SemanticError modal variation.
// Only an actual *json.SemanticError node is canonicalized. Other errors and
// user-provided cause text retain their exact bytes. Source error objects are
// never mutated; raw renderings are retained in the captured stderr stream.
package main

import (
	"encoding/json"
	jsonv2 "encoding/json/v2"
	"errors"
	"fmt"
	"math"
	"os"
	"reflect"
	"strings"

	tsjson "github.com/microsoft/TypeScript/tsc/internal/json"
)

type observedJSONCause string

func (e observedJSONCause) Error() string { return string(e) }

func canonicalJSONSemanticError(err error) string {
	semantic, ok := err.(*jsonv2.SemanticError)
	if !ok {
		return err.Error()
	}
	copy := *semantic
	if _, ok := semantic.Err.(*jsonv2.SemanticError); ok {
		copy.Err = observedJSONCause(canonicalJSONSemanticError(semantic.Err))
	}
	text := copy.Error()
	if strings.HasPrefix(text, "json: unable to ") {
		return "json: cannot " + strings.TrimPrefix(text, "json: unable to ")
	}
	if !strings.HasPrefix(text, "json: cannot ") {
		panic("Unknown pinned JSON SemanticError modal rendering")
	}
	return text
}

func observedErrorText(err error) string {
	canonical := canonicalJSONSemanticError(err)
	if semantic, ok := err.(*jsonv2.SemanticError); ok {
		typ := ""
		if semantic.GoType != nil {
			typ = semantic.GoType.String()
		}
		trace, marshalErr := json.Marshal(map[string]any{
			"type":      "encoding/json/v2.SemanticError",
			"rawBase64": b64(err.Error()), "canonicalBase64": b64(canonical),
			"jsonKind": uint8(semantic.JSONKind), "goType": typ,
			"byteOffset": semantic.ByteOffset, "jsonPointerBase64": b64(string(semantic.JSONPointer)),
		})
		if marshalErr != nil {
			panic(marshalErr)
		}
		fmt.Fprintln(os.Stderr, string(trace))
	}
	return canonical
}

func jsonErrorProjectionControls() map[string]any {
	count := 0
	check := func(actual, expected string) {
		if actual != expected {
			panic(fmt.Sprintf("JSON error projection mismatch: %q != %q", actual, expected))
		}
		count++
	}
	_, actualError := tsjson.Marshal(math.NaN())
	if _, ok := actualError.(*jsonv2.SemanticError); !ok {
		panic("Marshal did not return actual SemanticError")
	}
	check(observedErrorText(actualError), "json: cannot marshal from Go float64: unsupported value: NaN")
	for _, text := range []string{
		"json: unable to marshal from Go float64: user-provided text",
		"json: cannot unmarshal JSON number into Go float64: user-provided text",
		"wrapper: json: unable to marshal from Go float64: user-provided text",
	} {
		check(observedErrorText(errors.New(text)), text)
	}
	user := "json: unable to marshal from Go float64: user-provided cause"
	semantic := &jsonv2.SemanticError{GoType: reflect.TypeFor[float64](), Err: errors.New(user)}
	check(observedErrorText(semantic), "json: cannot handle Go float64: "+user)
	nested := &jsonv2.SemanticError{GoType: reflect.TypeFor[float64](), Err: actualError}
	check(observedErrorText(nested), "json: cannot handle Go float64: json: cannot marshal from Go float64: unsupported value: NaN")
	return map[string]any{"controls": count, "passed": count}
}
