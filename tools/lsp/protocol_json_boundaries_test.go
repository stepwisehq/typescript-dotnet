// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
package lsproto
import (
    "bytes"
    "strings"
    "testing"
    "github.com/microsoft/TypeScript/tsc/internal/json"
)
func TestAuthoredProtocolJSONBoundaries(t *testing.T) {
    t.Run("strict invalid UTF8 decode",func(t *testing.T){
        input:=append([]byte(`{"label":"`),0xff)
        input=append(input,[]byte(`"}`)...)
        var value CompletionItem
        if json.Unmarshal(input,&value)==nil {t.Fatal("invalid UTF8 admitted")}
    })
    t.Run("replacement invalid UTF8 encode",func(t *testing.T){
        data,err:=json.Marshal(&CompletionItem{Label:string([]byte{0xff})})
        if err!=nil {t.Fatal(err)}
        if string(data)!=`{"label":"�"}` {t.Fatalf("output %q",data)}
    })
    for _,input:=range []string{`{"label":"a","label":"b"}`,`{"label":"a","l\u0061bel":"b"}`} {
        var value CompletionItem
        if json.Unmarshal([]byte(input),&value)==nil {t.Fatal("duplicate label admitted")}
    }
    t.Run("deferred result lifetime",func(t *testing.T){
        input:=[]byte(`{"jsonrpc":"2.0","id":1,"result":{"text":"café"}}`)
        var message Message
        if err:=message.UnmarshalJSON(input);err!=nil {t.Fatal(err)}
        for index:=range input {input[index]='x'}
        result,ok:=message.AsResponse().Result.(json.Value)
        if !ok || string(result)!=`{"text":"café"}` {t.Fatalf("retained result %v",message.AsResponse().Result)}
    })
    t.Run("reused request absent params",func(t *testing.T){
        var request RequestMessage
        if err:=request.UnmarshalJSON([]byte(`{"jsonrpc":"2.0","id":1,"method":"first","params":{"x":1}}`));err!=nil {t.Fatal(err)}
        if err:=request.UnmarshalJSON([]byte(`{"jsonrpc":"2.0","id":2,"method":"second"}`));err!=nil {t.Fatal(err)}
        raw,ok:=request.Params.(json.Value)
        if !ok || string(raw)!=`{"x":1}` || request.Method!="second" || request.ID.MustInt()!=2 {t.Fatalf("reused request %#v",request)}
    })
    t.Run("response nil raw result",func(t *testing.T){
        var message Message
        if err:=message.UnmarshalJSON([]byte(`{"jsonrpc":"2.0","id":1}`));err!=nil {t.Fatal(err)}
        raw,ok:=message.AsResponse().Result.(json.Value)
        if !ok || raw!=nil {t.Fatal("nil raw result type lost")}
        data,err:=message.MarshalJSON()
        if err!=nil {t.Fatal(err)}
        if !bytes.Equal(data,[]byte(`{"jsonrpc":"2.0","id":1,"result":null}`)) {t.Fatalf("output %s",data)}
        if !strings.Contains(string(data),`"result":null`) {t.Fatal("nil raw result omitted")}
    })
}
