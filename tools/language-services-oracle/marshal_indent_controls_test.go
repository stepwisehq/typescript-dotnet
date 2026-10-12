// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.
// Pinned TypeScript revision fed0bf24149fb1ed36039212648bafdafc1ea10e.
package lsproto

import (
 "fmt"
 "encoding/json/jsontext"
 "math"
 "testing"
 tsjson "github.com/microsoft/TypeScript/tsc/internal/json"
)

// Authored reference observations for actual MarshalIndent owner boundaries.
func TestAuthoredMarshalIndentBoundaries(t *testing.T) {
 tests := []struct{Name string; Value any; Prefix,Indent string}{
  {"CompactScalar", "café\n", "", ""},
  {"NestedArray", []any{[]any{1.0,"café"},[]any{},nil}, " \t", "\t"},
  {"ProtocolValue", Position{Line:1,Character:2}, "", "\t"},
  {"RawValue", jsontext.Value(`{"a":[1,{},["café"]]}`), " ", "  "},
  {"PartialError", []any{1.0, math.NaN()}, "", "\t"},
  {"NestedPartialError", []any{[]float64{1,math.NaN()}}, "", "\t"},
  {"ObjectPartialError", struct{Good int; Bad float64}{1,math.NaN()}, "", "\t"},
  {"PrefixOnly", []int{1,2}, " \t", ""},
  {"InvalidIndent", nil, "", "x"},
  {"InvalidPrefix", nil, "\xff", " "},
 }
 for _, test := range tests { t.Run(test.Name,func(t *testing.T){
  defer func(){if p:=recover();p!=nil {fmt.Printf("INDENT %s panic=%q\n",test.Name,fmt.Sprint(p))}}()
  out,err:=tsjson.MarshalIndent(test.Value,test.Prefix,test.Indent)
  var errorText string; if err!=nil {errorText=err.Error()}
  fmt.Printf("INDENT %s output=%q error=%q\n",test.Name,string(out),errorText)
 }) }
}
