// Authored source API inputs. No option masking or replacement algorithms.
package main

import (
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	tsjson "github.com/microsoft/TypeScript/tsc/internal/json"
	"github.com/microsoft/TypeScript/tsc/internal/module"
	"github.com/microsoft/TypeScript/tsc/internal/tsoptions"
	"github.com/microsoft/TypeScript/tsc/internal/tspath"
)

type redirectInput struct{ Id, ConfigPath, ConfigText string }
type redirectReference struct {
	name    tspath.RootedFilePath
	options *core.CompilerOptions
}

func (r *redirectReference) ConfigName() tspath.RootedFilePath      { return r.name }
func (r *redirectReference) CompilerOptions() *core.CompilerOptions { return r.options }

type staticEntryInput struct {
	ModuleName, ContainingDirectory string
	Mode                            *core.ResolutionMode
	Result                          *module.ResolvedModule
}
type staticInput struct {
	FallbackToResolver bool
	Entries            []staticEntryInput
}

func rawOptions(input string, cwd tspath.RootedDirectoryPath) (*core.CompilerOptions, []*ast.Diagnostic, error) {
	raw := &tsoptions.RawCompilerOptions{}
	err := tsjson.Unmarshal([]byte(input), raw)
	options, diagnostics := raw.Finalize(cwd)
	return options, diagnostics, err
}
