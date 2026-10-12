// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.
// Authored public document-symbol response witness for symbols.go shared *slice headers.
// Pinned TypeScript fed0bf24149fb1ed36039212648bafdafc1ea10e; additive overlay only.
package project_test

import (
 "context"
 "fmt"
 "testing"
 tsjson "github.com/microsoft/TypeScript/tsc/internal/json"
 "github.com/microsoft/TypeScript/tsc/internal/lsp/lsproto"
 "github.com/microsoft/TypeScript/tsc/internal/testutil/projecttestutil"
)

func TestAuthoredDocumentSymbolChildrenAliases(t *testing.T) {
 const text = "function f() {}\nfunction f() {}\nf.a = 1;\nf.b = 2;\n"
 session, _ := projecttestutil.Setup(map[string]any{
  "/fixture/tsconfig.json": `{"compilerOptions":{"skipLibCheck":true,"allowJs":true,"checkJs":true},"files":["input.js"]}`,
  "/fixture/input.js": text,
 })
 defer session.Close()
 uri := lsproto.DocumentUri("file:///fixture/input.js")
 session.DidOpenFile(context.Background(), uri, 1, text, lsproto.LanguageKindJavaScript)
 service, err := session.GetLanguageService(context.Background(),uri); if err != nil {t.Fatal(err)}
 for _, hierarchical := range []bool{true,false} {
  caps := &lsproto.ResolvedClientCapabilities{}
  caps.TextDocument.DocumentSymbol.HierarchicalDocumentSymbolSupport = hierarchical
  ctx := lsproto.WithClientCapabilities(context.Background(),caps)
  response, err := service.ProvideDocumentSymbols(ctx,uri); if err != nil {t.Fatal(err)}
  encoded, err := tsjson.Marshal(response); if err != nil {t.Fatal(err)}
  fmt.Printf("DOCUMENT_SYMBOL_ALIAS_OBSERVATION hierarchical=%v %s\n", hierarchical,encoded)
 }
}
