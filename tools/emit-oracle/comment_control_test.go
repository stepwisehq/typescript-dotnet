package printer_test

import (
	"encoding/hex"
	"fmt"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"github.com/microsoft/TypeScript/tsc/internal/printer"
	"github.com/microsoft/TypeScript/tsc/internal/testutil/parsetestutil"
	"testing"
)

func TestAuthoredCommentOutput(t *testing.T) {
	f := parsetestutil.ParseTypeScript("/*\n\u200bretain\ufeff\n*/\nlet a = 1;", false)
	p := printer.NewPrinter(printer.PrinterOptions{NewLine: core.NewLineKindLF}, printer.PrintHandlers{}, nil)
	got := p.EmitSourceFile(f)
	expected := "/*\n \u200bretain\ufeff\n*/\nlet a = 1;\n"
	if got != expected {
		t.Fatalf("comment output=%q expected=%q", got, expected)
	}
	fmt.Printf("OUTPUT=%q\nHEX=%s\n", got, hex.EncodeToString([]byte(got)))
}
