// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
package lsproto
import (
 "testing"
 "github.com/microsoft/TypeScript/tsc/internal/json"
)
func TestAuthoredProtocolZeroValueBoundaries(t *testing.T) {
 mustPanic:=func(value any,want string) {
  t.Helper()
  defer func(){if got:=recover();got!=want {t.Fatalf("panic %v; want %s",got,want)}}()
  _,_ = json.Marshal(value)
 }
 mustPanic(BooleanOrHoverOptions{},"exactly one element of BooleanOrHoverOptions should be set")
 mustPanic(Registration{},"RegisterOptions must be set")
 for _,test:=range []struct{value any;want string}{
  {HoverOrNull{},"null"},
  {CompletionItem{},`{"label":""}`},
  {(*CompletionItem)(nil),"null"},
 } {
  data,err:=json.Marshal(test.value)
  if err!=nil || string(data)!=test.want {t.Fatalf("output %s / %v; want %s",data,err,test.want)}
 }
}
