// Authored graph observation over actual original loader/parser producers.
// Not a replacement graph search. Only explicit contract-external stages omitted.
package compiler

import (
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"github.com/microsoft/TypeScript/tsc/internal/diagnostics"
	"github.com/microsoft/TypeScript/tsc/internal/module"
	"github.com/microsoft/TypeScript/tsc/internal/tsoptions"
	"github.com/microsoft/TypeScript/tsc/internal/tspath"
	"github.com/microsoft/TypeScript/tsc/internal/vfs"
)

type OracleResolutionEdge struct {
	File          *ast.SourceFile
	Specifier     *ast.Node
	Name          string
	Mode          core.ResolutionMode
	ImportIndex   int
	SyntheticKind string
	Resolution    *module.ResolvedModule
	Traces        []module.DiagAndArgs
	Error         error
	Scheduling    string
	Target        *ast.SourceFile
	task          *parseTask
}
type OracleTypeEdge struct {
	File       *ast.SourceFile
	Reference  *ast.FileReference
	Mode       core.ResolutionMode
	Resolution *module.ResolvedTypeReferenceDirective
	Traces     []module.DiagAndArgs
	Target     *ast.SourceFile
	Scheduling string
}
type OraclePathEdge struct {
	File         *ast.SourceFile
	Reference    *ast.FileReference
	ResolvedFile tspath.RootedFilePath
	ResolvedPath tspath.PathKey
	Diagnostic   *ast.Diagnostic
	Target       *ast.SourceFile
	Scheduling   string
	diagnostic   *processingDiagnostic
}
type OracleResolutionGraph struct {
	Files                []*ast.SourceFile
	Roots                []tspath.RootedFilePath
	Metadata             map[tspath.PathKey]ast.SourceFileMetaData
	Missing              []tspath.PathKey
	Edges                []*OracleResolutionEdge
	Types                []*OracleTypeEdge
	Paths                []*OraclePathEdge
	InclusionDiagnostics []*ast.Diagnostic
	ResolutionData       *module.ResolutionData
}

func (p *fileLoader) oracleCaptureModule(t *parseTask, entry *ast.Node, index int, name string, mode core.ResolutionMode, result *module.ResolvedModule, trace []module.DiagAndArgs, err error) *OracleResolutionEdge {
	if p.oracleGraph == nil {
		return nil
	}
	synthetic := ""
	if entry == t.importHelpersImportSpecifier {
		synthetic = "tslib"
	}
	if t.jsxRuntimeImportSpecifier != nil && entry == t.jsxRuntimeImportSpecifier.specifier {
		synthetic = "jsx-runtime"
	}
	edge := &OracleResolutionEdge{File: t.file, Specifier: entry, Name: name, Mode: mode, ImportIndex: index, SyntheticKind: synthetic, Resolution: result, Traces: trace, Error: err, Scheduling: "Unresolved", task: t}
	p.oracleGraph.Edges = append(p.oracleGraph.Edges, edge)
	return edge
}
func (p *fileLoader) oracleCaptureModuleDecision(edge *OracleResolutionEdge, allowed bool, js bool, diagnostic *diagnostics.Message) {
	if edge == nil {
		return
	}
	if allowed {
		edge.Scheduling = "Scheduled"
		return
	}
	options := p.opts.Config.CompilerOptions()
	switch {
	case diagnostic != nil:
		edge.Scheduling = "ResolutionDiagnostic"
	case options.NoResolve.IsTrue():
		edge.Scheduling = "NoResolve"
	case js && !options.GetAllowJS():
		edge.Scheduling = "JavaScriptDisabled"
	case edge.ImportIndex >= len(edge.File.Imports()):
		edge.Scheduling = "ModuleAugmentation"
	default:
		edge.Scheduling = "JSDocInTypeScript"
	}
}
func (p *fileLoader) oracleCapturePath(t *parseTask, ref *ast.FileReference, resolved *resolvedRef, diagnostic *processingDiagnostic) {
	if p.oracleGraph == nil {
		return
	}
	edge := &OraclePathEdge{File: t.file, Reference: ref, diagnostic: diagnostic, Scheduling: "Scheduled"}
	if resolved != nil {
		edge.ResolvedFile = resolved.fileName
		edge.ResolvedPath = resolved.path
	}
	if diagnostic != nil {
		edge.Scheduling = "ResolutionDiagnostic"
	}
	p.oracleGraph.Paths = append(p.oracleGraph.Paths, edge)
}
func (p *fileLoader) oracleCaptureType(t *parseTask, ref *ast.FileReference, mode core.ResolutionMode, resolved *module.ResolvedTypeReferenceDirective, trace []module.DiagAndArgs) {
	if p.oracleGraph == nil {
		return
	}
	edge := &OracleTypeEdge{File: t.file, Reference: ref, Mode: mode, Resolution: resolved, Traces: trace, Scheduling: "Unresolved"}
	if resolved.IsResolved() {
		edge.Scheduling = "Scheduled"
	}
	p.oracleGraph.Types = append(p.oracleGraph.Types, edge)
}

func ObserveResolutionGraph(config *tsoptions.ParsedCommandLine, fs vfs.FS) *OracleResolutionGraph {
	// These are exact original processAllProgramFiles construction inputs; omitted
	// stages: project-reference tasks, root defaultlibs and automatic type directives.
	host := NewCompilerHost(fs, config.BaseDirectory(), nil, nil, nil)
	options := config.CompilerOptions()
	supported := tsoptions.GetSupportedExtensions(options, config.ContentMapperExtensions())
	maxDepth := 0
	if options.MaxNodeModuleJsDepth != nil {
		maxDepth = *options.MaxNodeModuleJsDepth
	}
	graph := &OracleResolutionGraph{Roots: config.FileNames(), Edges: []*OracleResolutionEdge{}, Types: []*OracleTypeEdge{}, Paths: []*OraclePathEdge{}, Missing: []tspath.PathKey{}}
	loader := fileLoader{opts: ProgramConfig{Config: config}, host: host, defaultLibraryPath: host.DefaultLibraryPath(), caseSensitivity: fs.CaseSensitivity(), filesParser: &filesParser{wg: core.NewWorkGroup(true), maxDepth: maxDepth}, rootTasks: make([]*parseTask, 0, len(config.FileNames())), supportedExtensions: supported, supportedExtensionsWithJsonIfResolveJsonModule: tsoptions.GetSupportedExtensionsWithJsonIfResolveJsonModule(options, supported), contentMapperExtensions: config.ContentMapperExtensions(), oracleGraph: graph}
	// Exact allocation arm from addProjectReferenceTasks before its external stage.
	loader.projectReferences = &projectReferenceFileMapperBuilder{projectReferenceFileMapper: &projectReferenceFileMapper{config: config, useSourceOfProjectReference: loader.opts.canUseProjectReferenceSource()}, host: &compilerResolutionHost{host: host, baseDirectory: config.BaseDirectory()}}
	loader.resolver = module.NewResolver(module.ResolverOptions{Host: loader.projectReferences.host, CompilerOptions: options, ExtraExtensions: config.ContentMapperExtensions(), ProjectName: config.ConfigName().AsString()})
	for index, file := range config.FileNames() {
		loader.addRootFileTask(file, config.RootFileNameForDiagnostic(index), nil, &FileIncludeReason{kind: fileIncludeKindRootFile, index: index})
	}
	loader.filesParser.parse(&loader, loader.rootTasks)
	processed := loader.filesParser.getProcessedFiles(&loader)
	graph.Files = processed.files
	graph.Metadata = processed.sourceFileMetaDatas
	for key := range processed.missingFiles.Keys() {
		graph.Missing = append(graph.Missing, key)
	}
	graph.ResolutionData = loader.resolver.GetResolutionData()
	// Capture domain is retained actual processed owners, matching C# CollectFiles.
	// Parsed package/casing losers are not published graph owners and are omitted,
	// not coalesced into another source identity.
	retained := map[*ast.SourceFile]bool{}
	for _, file := range graph.Files {
		retained[file] = true
	}
	edges := graph.Edges[:0]
	for _, edge := range graph.Edges {
		if retained[edge.File] {
			edges = append(edges, edge)
		}
	}
	graph.Edges = edges
	paths := graph.Paths[:0]
	for _, edge := range graph.Paths {
		if retained[edge.File] {
			paths = append(paths, edge)
		}
	}
	graph.Paths = paths
	types := graph.Types[:0]
	for _, edge := range graph.Types {
		if retained[edge.File] {
			types = append(types, edge)
		}
	}
	graph.Types = types
	// NoResolve observations describe actual retained syntax without resolving it.
	if options.NoResolve.IsTrue() {
		for _, file := range graph.Files {
			for _, ref := range file.ReferencedFiles {
				graph.Paths = append(graph.Paths, &OraclePathEdge{File: file, Reference: ref, Scheduling: "NoResolve"})
			}
			for _, ref := range file.TypeReferenceDirectives {
				graph.Types = append(graph.Types, &OracleTypeEdge{File: file, Reference: ref, Mode: core.ResolutionModeNone, Scheduling: "NoResolve"})
			}
		}
	}
	// Zero-checker metadata view for the real original inclusion-diagnostic producer.
	// No NewProgram, semantic/option validation, checker pool or global environment.
	program := &Program{opts: loader.opts, hosts: ProgramHosts{Host: host}, caseSensitivity: loader.caseSensitivity, processedFiles: processed, resolutionData: graph.ResolutionData}
	diagnostics := program.includeProcessor.getDiagnostics(program)
	graph.InclusionDiagnostics = append(graph.InclusionDiagnostics, diagnostics.GetGlobalDiagnostics()...)
	for _, file := range graph.Files {
		graph.InclusionDiagnostics = append(graph.InclusionDiagnostics, diagnostics.GetDiagnosticsForFile(file)...)
	}
	for _, edge := range graph.Edges {
		edge.Target = processed.filesByPath[edge.Resolution.ResolvedPath]
		if edge.Scheduling != "Scheduled" {
			continue
		}
		if edge.Target == nil {
			for _, task := range edge.task.subTasks {
				if task.path == edge.Resolution.ResolvedPath {
					if !task.loaded && task.elideOnDepth {
						edge.Scheduling = "ElidedJavaScriptDepth"
					} else {
						edge.Scheduling = "MissingFile"
					}
					break
				}
			}
		}
	}
	for _, edge := range graph.Paths {
		if edge.diagnostic != nil {
			edge.Diagnostic = edge.diagnostic.toDiagnostic(program)
		}
		if edge.Scheduling == "Scheduled" {
			edge.Target = processed.filesByPath[edge.ResolvedPath]
			if edge.Target == nil {
				edge.Scheduling = "MissingFile"
			}
		}
	}
	for _, edge := range graph.Types {
		if edge.Scheduling == "Scheduled" {
			edge.Target = processed.filesByPath[edge.Resolution.ResolvedPath]
			if edge.Target == nil {
				edge.Scheduling = "MissingFile"
			}
		}
	}
	return graph
}
