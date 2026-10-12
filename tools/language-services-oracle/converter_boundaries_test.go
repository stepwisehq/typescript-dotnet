// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.
// Authored boundary witness against the pinned, unchanged converter owner.
package lsconv

import (
 "testing"
 "github.com/microsoft/TypeScript/tsc/internal/lsp/lsproto"
 "github.com/microsoft/TypeScript/tsc/internal/spanmap"
 "github.com/microsoft/TypeScript/tsc/internal/tspath"
)
type coordinateBoundaryScript string
func (s coordinateBoundaryScript) FileName() tspath.RootedFilePath { return "/test.ts" }
func (s coordinateBoundaryScript) OriginalFileName() tspath.RootedFilePath { return s.FileName() }
func (s coordinateBoundaryScript) Text() string { return string(s) }
func (s coordinateBoundaryScript) OriginalText() string { return string(s) }
func (s coordinateBoundaryScript) SpanMap() *spanmap.SpanMap { return nil }
func TestAuthoredCoordinateBoundaries(t *testing.T) {
 cases := []struct { text string; encoding lsproto.PositionEncodingKind; line, character uint32; expected int32 }{
  {"a\nbc", lsproto.PositionEncodingKindUTF16, 1, ^uint32(0), 2},
  {"a\nbc", lsproto.PositionEncodingKindUTF16, 1, 2147483647, 2},
  {"éx", lsproto.PositionEncodingKindUTF16, 0, ^uint32(0), 0},
  {"éx", lsproto.PositionEncodingKindUTF8, 0, ^uint32(0), 0},
 }
 for _, c := range cases {
  script := coordinateBoundaryScript(c.text)
  lines := ComputeLSPLineStarts(c.text)
  converter := NewConverters(c.encoding, func(tspath.RootedFilePath) *LSPLineMap { return lines })
  got := converter.lineAndCharacterToPosition(script, lsproto.Position{Line:c.line, Character:c.character})
  if int32(got) != c.expected { t.Fatalf("%q %s (%d,%d): got %d, want %d", c.text, c.encoding, c.line, c.character, got, c.expected) }
 }
}
