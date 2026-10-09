// Authored source-API observation of original module specifier generation.
package main

import (
	"github.com/microsoft/TypeScript/tsc/internal/checker"
	"github.com/microsoft/TypeScript/tsc/internal/compiler"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"github.com/microsoft/TypeScript/tsc/internal/modulespecifiers"
	"github.com/microsoft/TypeScript/tsc/internal/tspath"
	"strings"
)

func moduleSpecifierProjection(p *compiler.Program, c *checker.Checker, request ProjectRequest) any {
	targetName, contextName := request.SpecifierTarget, request.SpecifierContext
	if targetName == "" {
		targetName = "a.ts"
	}
	if contextName == "" {
		contextName = "main.ts"
	}
	for _, name := range []string{targetName, contextName} {
		if strings.HasPrefix(name, "/") || strings.Contains(name, "..") {
			panic("Noncanonical helper owner")
		}
	}
	target := p.GetSourceFile(tspath.RootedFilePath("/project/" + targetName))
	contextFile := p.GetSourceFile(tspath.RootedFilePath("/project/" + contextName))
	if target == nil || contextFile == nil {
		panic("Missing fixed helper source owners")
	}
	result := modulespecifiers.GetModuleSpecifiers(target.AsNode().Symbol(), c, p.Options(), contextFile, p,
		modulespecifiers.UserPreferences{ImportModuleSpecifierPreference: modulespecifiers.ImportModuleSpecifierPreferenceProjectRelative, ImportModuleSpecifierEnding: modulespecifiers.ImportModuleSpecifierEndingPreferenceNone},
		modulespecifiers.ModuleSpecifierOptions{OverrideImportMode: core.ResolutionModeNone}, false)
	return map[string]any{"kind": int(result.Kind), "specifiers": result.Specifiers, "ambient": result.AmbientModuleSymbol != nil}
}
