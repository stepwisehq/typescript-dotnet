// Authored additive bridge; actual original ordered full walker/renderers are called directly.
package tsbaseline

import (
	"github.com/microsoft/TypeScript/tsc/internal/compiler"
	"github.com/microsoft/TypeScript/tsc/internal/testutil/harnessutil"
)

type AuthoredFullBaselines struct {
	Types   string
	Symbols string
}

func AuthoredObserveFullBaselines(program compiler.ProgramLike, files []*harnessutil.TestFile, header string, hasCheckingErrors bool) AuthoredFullBaselines {
	walker := newTypeWriterWalker(program, hasCheckingErrors)
	// Exact original type-then-symbol operation order and shared walker state.
	types := generateBaseline(files, walker, header, false)
	symbols := generateBaseline(files, walker, header, true)
	return AuthoredFullBaselines{Types: types, Symbols: symbols}
}
