// Copyright 2011 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Authored public observations of pinned golang.org/x/text v0.42.0 NFD.String.
// Run from the pinned TypeScript Go module with its bundled Go1.27.1 toolchain.
// Hex preserves malformed UTF8; no private normalization state is observed.
package main

import (
	"encoding/hex"
	"encoding/json"
	"os"
	"strings"

	"golang.org/x/text/unicode/norm"
)

func main() {
	grave := func(n int) string { return strings.Repeat("\u0300", n) }
	rows := []struct{ Name, Input string }{
		{"ascii", "plain ASCII"},
		{"canonical", "\u00C0"},
		{"hangul", "\uAC01"},
		{"malformed", "\x80\xC2"},
		{"stream-safe-whole-string", "a" + grave(31)},
		{"musical-Mc-boundary", strings.Repeat("\U0001D16D", 30) + "\U0001D165"},
		{"quick-prefix-multiple-segments", "prefix\u00C0middle\u00E9"},
		{"combining-class-reorder", "a\u0315\u0300"},
	}
	encoder := json.NewEncoder(os.Stdout)
	for _, row := range rows {
		if err := encoder.Encode(struct{ Name, InputHex, OutputHex string }{
			row.Name, hex.EncodeToString([]byte(row.Input)), hex.EncodeToString([]byte(norm.NFD.String(row.Input))),
		}); err != nil {
			panic(err)
		}
	}
}
