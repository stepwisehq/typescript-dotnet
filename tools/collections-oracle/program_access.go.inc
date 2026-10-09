// Authored same-checker entry to actual pinned Program semantic diagnostics.
// The public Program entry normally acquires a pool checker; this wrapper keeps
// the explicitly created checker and its observable caches/identities unchanged.
package compiler

import (
	"context"
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/checker"
)

func OracleCollectionsSemanticDiagnostics(p *Program, c *checker.Checker, file *ast.SourceFile) []*ast.Diagnostic {
	if p.SkipTypeChecking(file, false) {
		return nil
	}
	return filterAndSortDiagnostics(p.getSemanticDiagnosticsWithChecker(context.Background(), c, file))
}
