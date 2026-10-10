// Authored public resolver-factory callback. The default resolver still performs
// every lookup; supplied AST diagnostics test Program's returned-slot consumers,
// not a claim that ordinary type lookup naturally generates these diagnostics.
package main

import (
	"errors"
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/compiler"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"github.com/microsoft/TypeScript/tsc/internal/diagnostics"
	"github.com/microsoft/TypeScript/tsc/internal/module"
	"github.com/microsoft/TypeScript/tsc/internal/tspath"
)

type resolverDecoratorInput struct{ ModuleDiagnostics, TypeDiagnostics, Error bool }
type captureSourceHost struct {
	compiler.CompilerHost
	files map[tspath.RootedFilePath]*ast.SourceFile
}

func (h *captureSourceHost) GetSourceFile(options ast.SourceFileParseOptions) *ast.SourceFile {
	file := h.CompilerHost.GetSourceFile(options)
	if file != nil {
		h.files[options.FileName] = file
	}
	return file
}

type decoratedResolver struct {
	module.Resolver
	host  *captureSourceHost
	input *resolverDecoratorInput
}

func (r *decoratedResolver) diagnostic(fileName tspath.RootedFilePath, kind string) *ast.Diagnostic {
	file := r.host.files[fileName]
	if file == nil {
		return ast.NewCompilerDiagnostic(diagnostics.File_0_not_found, "authored-factory-"+kind)
	}
	return ast.NewDiagnostic(file, core.NewTextRange(0, 1), diagnostics.File_0_not_found, "authored-factory-"+kind)
}
func (r *decoratedResolver) ResolveModuleName(name string, file tspath.RootedFilePath, mode core.ResolutionMode, reference module.ResolvedProjectReference) (*module.ResolvedModule, []module.DiagAndArgs, error) {
	result, traces, err := r.Resolver.ResolveModuleName(name, file, mode, reference)
	if r.input.Error {
		return result, traces, errors.New("authored resolver factory failure")
	}
	if result != nil && r.input.ModuleDiagnostics {
		result.ResolutionDiagnostics = []*ast.Diagnostic{r.diagnostic(file, "module")}
	}
	return result, traces, err
}
func (r *decoratedResolver) ResolveTypeReferenceDirective(name string, file tspath.RootedFilePath, mode core.ResolutionMode, reference module.ResolvedProjectReference) (*module.ResolvedTypeReferenceDirective, []module.DiagAndArgs) {
	result, traces := r.Resolver.ResolveTypeReferenceDirective(name, file, mode, reference)
	if result != nil && r.input.TypeDiagnostics {
		result.ResolutionDiagnostics = []*ast.Diagnostic{r.diagnostic(file, "type")}
	}
	return result, traces
}
