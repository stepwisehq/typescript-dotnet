// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
package lsproto

import (
    "strings"
    "testing"
    "github.com/microsoft/TypeScript/tsc/internal/json"
)

// Additive verification of Go's admitted recursive ownership path. No lowered
// decoder depth and no replacement model parser are used by either runtime.
func TestAuthoredProtocolDepthBoundaries(t *testing.T) {
    const symbol = `{"name":"s","kind":1,"range":{"start":{"line":0,"character":0},"end":{"line":0,"character":0}},"selectionRange":{"start":{"line":0,"character":0},"end":{"line":0,"character":0}}`
    for _, depth := range []int{16, 256, 2048, 4998} {
        input := strings.Repeat(symbol+`,"children":[`, depth-1)+symbol+`}`+strings.Repeat(`]}`,depth-1)
        var result DocumentSymbol
        if err := json.Unmarshal([]byte(input), &result); err != nil { t.Fatalf("model depth %d: %v",depth,err) }
        count := 1
        current := &result
        for current.Children!=nil && len(*current.Children)>0 { current=(*current.Children)[0]; count++ }
        if count != depth { t.Fatalf("model depth %d decoded %d",depth,count) }
    }
    for _, depth := range []int{16,256,2048,10000} {
        input := strings.Repeat(`[`,depth)+`0`+strings.Repeat(`]`,depth)
        var result any
        if err := json.Unmarshal([]byte(input), &result); err != nil { t.Fatalf("any depth %d: %v",depth,err) }
        count := 0
        for { array,ok:=result.([]any); if !ok { break }; if len(array)!=1 {t.Fatalf("any depth %d has length %d",depth,len(array))};result=array[0];count++ }
        if count!=depth {t.Fatalf("any depth %d decoded %d",depth,count)}
    }
}
