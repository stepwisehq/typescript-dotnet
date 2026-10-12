// Authored observations for the consumed LSP/project fmt boundary, not upstream tests.
// Apply additively to pinned internal/lsp at fed0bf24149fb1ed36039212648bafdafc1ea10e.
package lsp

import (
    "encoding/base64"
    "fmt"
    "math"
    "testing"
    "time"
)

func TestAuthoredConsumedLogFormats(t *testing.T) {
    cases := []struct { name, format string; args []any }{
        {"nil", "%T\n%+v", []any{nil, nil}},
        {"bool", "%T\n%+v", []any{true, true}},
        {"string", "%T\n%+v", []any{"café", "café"}},
        {"number", "%T\n%+v", []any{1.25, 1.25}},
        {"object", "%T\n%+v", []any{map[string]any{"z": 2.0, "a": []any{true, nil, "café"}}, map[string]any{"z": 2.0, "a": []any{true, nil, "café"}}}},
        {"array", "%T\n%+v", []any{[]any{1.0, false, "x"}, []any{1.0, false, "x"}}},
        {"emptyObject", "%T\n%+v", []any{map[string]any{}, map[string]any{}}},
        {"emptyArray", "%T\n%+v", []any{[]any{}, []any{}}},
        {"numbers", "%v", []any{[]any{math.Copysign(0,-1), 0.0001, 0.00001, 999999.0, 1000000.0, 1e23, math.SmallestNonzeroFloat64, math.MaxFloat64}}},
        {"exceptionString", "%s\n%v\n%q", []any{fmt.Errorf("café failure"), fmt.Errorf("café failure"), fmt.Errorf("café failure")}},
        {"rawQuoted", "%q", []any{"café\n\xff"}},
        {"countWidth", "%6d/%d", []any{7, int64(-12)}},
        {"duration", "%v", []any{time.Hour+time.Minute+123456789*time.Nanosecond}},
    }
    for _, c := range cases {
        t.Run(c.name, func(t *testing.T) {
            out := fmt.Sprintf(c.format, c.args...)
            t.Logf("LOG_FORMAT %s %s", c.name, base64.StdEncoding.EncodeToString([]byte(out)))
        })
    }
}
