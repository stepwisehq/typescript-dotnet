// Authored scratch bridge; not part of the live gate. Original source preparation copied with explicit no-Emit observation boundary.
package harnessutil

import (
	"context"
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/bundled"
	"github.com/microsoft/TypeScript/tsc/internal/compiler"
	"github.com/microsoft/TypeScript/tsc/internal/contentmapper"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"github.com/microsoft/TypeScript/tsc/internal/locale"
	"github.com/microsoft/TypeScript/tsc/internal/testutil/contentmappertest"
	"github.com/microsoft/TypeScript/tsc/internal/tsoptions"
	"github.com/microsoft/TypeScript/tsc/internal/tspath"
	"github.com/microsoft/TypeScript/tsc/internal/vfs/vfstest"
	"maps"
	"strings"
	"testing"
	"testing/fstest"
)

func AuthoredCompileFiles(
	t *testing.T,
	inputFiles []*TestFile,
	otherFiles []*TestFile,
	testConfig TestConfiguration,
	tsconfig *tsoptions.ParsedCommandLine,
	currentDirectory tspath.RootedDirectoryPath,
	symlinks map[string]string,
) *AuthoredNoEmitCompilation {
	var compilerOptions *core.CompilerOptions
	if tsconfig != nil {
		compilerOptions = tsconfig.ParsedConfig.CompilerOptions.Clone()
	}
	if compilerOptions == nil {
		compilerOptions = &core.CompilerOptions{}
	}
	// Set default options for tests
	if compilerOptions.NewLine == core.NewLineKindNone {
		compilerOptions.NewLine = core.NewLineKindCRLF
	}
	if compilerOptions.SkipDefaultLibCheck == core.TSUnknown {
		compilerOptions.SkipDefaultLibCheck = core.TSTrue
	}
	compilerOptions.NoErrorTruncation = core.TSTrue
	harnessOptions := HarnessOptions{CaseSensitivity: tspath.CaseSensitive, CurrentDirectory: currentDirectory}

	// Parse harness and compiler options from the test configuration
	if testConfig != nil {
		SetOptionsFromTestConfig(t, testConfig, compilerOptions, &harnessOptions, currentDirectory, false /*allowUnknownOptions*/)
	}

	return AuthoredCompileFilesEx(t, inputFiles, otherFiles, &harnessOptions, compilerOptions, currentDirectory, symlinks, tsconfig)
}

func AuthoredCompileFilesEx(
	t *testing.T,
	inputFiles []*TestFile,
	otherFiles []*TestFile,
	harnessOptions *HarnessOptions,
	compilerOptions *core.CompilerOptions,
	currentDirectory tspath.RootedDirectoryPath,
	symlinks map[string]string,
	tsconfig *tsoptions.ParsedCommandLine,
) *AuthoredNoEmitCompilation {
	var programFileNames []tspath.RootedFilePath
	for _, file := range inputFiles {
		fileName := tspath.ToRootedFilePath(file.UnitName, currentDirectory)

		if !fileName.ExtensionIs(tspath.ExtensionJson) &&
			!fileName.ExtensionIs(tspath.ExtensionTsBuildInfo) {
			programFileNames = append(programFileNames, fileName)
		}
	}

	// !!! Note: lib files are not going to be in `built/local`.
	// In addition, not all files that used to be in `built/local` are going to exist.
	// Files from built\local that are requested by test "@includeBuiltFiles" to be in the context.
	// Treat them as library files, so include them in build, but not in baselines.
	// if harnessOptions.includeBuiltFile != "" {
	// 	programFileNames = append(programFileNames, tspath.CombinePaths(builtFolder, harnessOptions.includeBuiltFile))
	// }

	// Performance optimization; avoid copying in the /.lib folder if the test doesn't need it.
	includeLibDir := core.Some(inputFiles, func(file *TestFile) bool { return strings.Contains(file.Content, testLibFolder+"/") })

	// Files from testdata\lib that are requested by "@libFiles"
	if len(harnessOptions.LibFiles) > 0 {
		for _, libFile := range harnessOptions.LibFiles {
			if libFile == "lib.d.ts" && compilerOptions.NoLib != core.TSTrue {
				// We used to override lib with a custom lib.d.ts for some reason. Skip this unless it becomes necessary.
				continue
			}
			programFileNames = append(programFileNames, currentDirectory.ResolveFile(tspath.CombinePaths(testLibFolder, libFile)))
			includeLibDir = true
		}
	}

	if includeLibDir {
	}

	var contentMappers []*contentmapper.Mapper
	if tsconfig != nil && tsconfig.ParsedConfig != nil {
		contentMappers = tsconfig.ParsedConfig.ContentMappers
	}

	// Create fake FS for testing
	testfs := map[string]any{}
	for _, file := range inputFiles {
		fileName := tspath.GetNormalizedAbsolutePath(file.UnitName, currentDirectory)
		testfs[fileName] = &fstest.MapFile{
			Data: []byte(file.Content),
		}
	}
	for _, file := range otherFiles {
		fileName := tspath.GetNormalizedAbsolutePath(file.UnitName, currentDirectory)
		testfs[fileName] = &fstest.MapFile{
			Data: []byte(file.Content),
		}
	}
	for src, target := range symlinks {
		srcFileName := tspath.GetNormalizedAbsolutePath(src, currentDirectory)
		targetFileName := tspath.GetNormalizedAbsolutePath(target, currentDirectory)
		testfs[srcFileName] = vfstest.Symlink(targetFileName)
	}

	if includeLibDir {
		maps.Copy(testfs, testLibFolderMap())
	}

	// Authentic locked public API declarations, activated only by original explicit roles.
	apiFiles := make(map[string]string)
	for name, value := range testfs {
		if file, ok := value.(*fstest.MapFile); ok {
			apiFiles[name] = string(file.Data)
		}
	}
	if err := authoredAddAPIEnvironment(apiFiles); err != nil {
		t.Fatal(err)
	}
	for name, text := range apiFiles {
		if _, exists := testfs[name]; !exists {
			testfs[name] = &fstest.MapFile{Data: []byte(text)}
		}
	}
	fs := vfstest.FromMap(testfs, harnessOptions.CaseSensitivity)
	fs = bundled.WrapFS(fs)
	fs = NewOutputRecorderFS(fs)

	// Content mappers, when trusted, are served in-process by the test mapper (see contentmappertest).
	// The host is shared by the pre- and post-emit programs and torn down when this compilation finishes.
	var contentMapperHost contentmapper.Host
	if compilerOptions.RunExternalCode.IsTrue() && len(contentMappers) > 0 {
		contentMapperHost = contentmapper.NewHost(context.Background(), contentmappertest.NewSpawner(), locale.Default)
		defer contentMapperHost.Close()
	}

	var configFile *tsoptions.TsConfigSourceFile
	var errors []*ast.Diagnostic
	if tsconfig != nil {
		configFile = tsconfig.ConfigFile
		errors = tsconfig.Errors
	}
	baseDirectory := currentDirectory
	if tsconfig != nil && tsconfig.BaseDirectory() != "" {
		baseDirectory = tsconfig.BaseDirectory()
	}
	config := tsoptions.NewParsedCommandLine(compilerOptions, programFileNames, nil, baseDirectory, harnessOptions.CaseSensitivity)
	config.ParsedConfig.ContentMappers = contentMappers
	config.ConfigFile = configFile
	config.Errors = errors
	var contentMapperProject contentmapper.Project
	if contentMapperHost != nil {
		contentMapperProject = contentMapperHost.Project(contentmapper.ProjectSpec{
			ConfigFileName:  config.ConfigName(),
			Mappers:         config.ContentMappers(),
			CompilerOptions: config.CompilerOptions(),
		})
		defer contentMapperProject.Close()
	}
	host := createCompilerHost(fs, bundled.LibPath(), currentDirectory, contentMapperProject)
	result := authoredCompileNoEmitWithHost(host, config, harnessOptions)
	result.Result.Symlinks = symlinks
	result.Result.Trace = host.tracer.String()
	result.Repeat = func(testConfig TestConfiguration) *AuthoredNoEmitCompilation {
		newHarnessOptions := *harnessOptions
		newCompilerOptions := compilerOptions.Clone()
		SetOptionsFromTestConfig(t, testConfig, newCompilerOptions, &newHarnessOptions, currentDirectory, false /*allowUnknownOptions*/)
		return AuthoredCompileFilesEx(t, inputFiles, otherFiles, &newHarnessOptions, newCompilerOptions, currentDirectory, symlinks, tsconfig)
	}
	result.Result.Repeat = func(testConfig TestConfiguration) *CompilationResult {
		return result.Repeat(testConfig).Result
	}
	return result
}

type AuthoredNoEmitChannels struct {
	Config                []*ast.Diagnostic
	Program               []*ast.Diagnostic
	Syntactic             []*ast.Diagnostic
	Semantic              []*ast.Diagnostic
	Global                []*ast.Diagnostic
	Suggestions           []*ast.Diagnostic
	SortedAndDeduplicated []*ast.Diagnostic
}
type AuthoredNoEmitCompilation struct {
	Result      *CompilationResult
	PreChecking AuthoredNoEmitChannels
	Checking    AuthoredNoEmitChannels
	Repeat      func(TestConfiguration) *AuthoredNoEmitCompilation
}

func authoredCollectChecking(program compiler.ProgramLike, harness *HarnessOptions) AuthoredNoEmitChannels {
	ctx := context.Background()
	result := AuthoredNoEmitChannels{}
	result.Config = program.GetConfigFileParsingDiagnostics()
	result.Program = program.GetProgramDiagnostics()
	result.Syntactic = program.GetSyntacticDiagnostics(ctx, nil)
	result.Semantic = program.GetSemanticDiagnostics(ctx, nil)
	result.Global = program.GetGlobalDiagnostics(ctx)
	if harness.CaptureSuggestions {
		result.Suggestions = program.GetSuggestionDiagnostics(ctx, nil)
	}
	var all []*ast.Diagnostic
	all = append(all, result.Config...)
	all = append(all, result.Program...)
	all = append(all, result.Syntactic...)
	all = append(all, result.Semantic...)
	all = append(all, result.Global...)
	all = append(all, result.Suggestions...)
	result.SortedAndDeduplicated = compiler.SortAndDeduplicateDiagnostics(all)
	return result
}
func authoredCompileNoEmitWithHost(host compiler.CompilerHost, config *tsoptions.ParsedCommandLine, harness *HarnessOptions) *AuthoredNoEmitCompilation {
	// Source pre-check clone is retained exactly, including only TraceResolution=false.
	preOptions := config.CompilerOptions().Clone()
	preOptions.TraceResolution = core.TSFalse
	preConfig := tsoptions.NewParsedCommandLine(preOptions, config.FileNames(), config.ProjectReferences(), config.BaseDirectory(), config.CaseSensitivity())
	preConfig.ParsedConfig.ContentMappers = config.ContentMappers()
	preConfig.ConfigFile = config.ConfigFile
	preConfig.Errors = config.Errors
	preProgram := createProgram(host, preConfig)
	pre := authoredCollectChecking(preProgram, harness)
	// Original actual-option host/Program helper, including its Incremental wrapper.
	// Emit, declaration-output diagnostics and post-Emit invariant are explicit external operations.
	program := createProgram(host, config)
	checking := authoredCollectChecking(program, harness)
	result := &CompilationResult{Diagnostics: checking.SortedAndDeduplicated, Program: program, Options: program.Options(), HarnessOptions: harness, Host: host, currentDirectory: harness.CurrentDirectory}
	return &AuthoredNoEmitCompilation{Result: result, PreChecking: pre, Checking: checking}
}
