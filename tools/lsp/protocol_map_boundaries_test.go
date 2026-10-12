// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
package lsproto
import (
 "strings"
 "testing"
 "github.com/microsoft/TypeScript/tsc/internal/json"
)
func TestAuthoredProtocolMapNameCollision(t *testing.T) {
 changes:=map[DocumentUri][]*TextEdit{DocumentUri(string([]byte{0xff})): {}, DocumentUri(string([]byte{0xfe})): {}}
 for _, value := range []any{
  map[string]any{string([]byte{0xff}):1, string([]byte{0xfe}):2},
  &WorkspaceEdit{Changes:&changes},
 } {
  _,err:=json.Marshal(value)
  if err==nil || !strings.Contains(err.Error(),`duplicate object member name "�"`) {t.Fatalf("collision: %v",err)}
  t.Log(err)
 }
 if _,err:=json.Marshal(map[string]any{"é":1,"é":2});err!=nil {t.Fatal(err)}
}
