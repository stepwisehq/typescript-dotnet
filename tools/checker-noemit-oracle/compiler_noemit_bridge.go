// Authored additive scratch bridge. The original configuration/root preparation is copied;
// only the no-Emit compile destination and returned observation wrapper differ.
package testrunner

import (
	"fmt"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"github.com/microsoft/TypeScript/tsc/internal/repo"
	"github.com/microsoft/TypeScript/tsc/internal/testutil/harnessutil"
	"github.com/microsoft/TypeScript/tsc/internal/testutil/tsbaseline"
	"github.com/microsoft/TypeScript/tsc/internal/tsoptions"
	"github.com/microsoft/TypeScript/tsc/internal/tspath"
	"slices"
	"strings"
	"testing"
)

type authoredNoEmitCompilerTest struct {
	*compilerTest
	Channels *harnessutil.AuthoredNoEmitCompilation
}

// Uses exactly the original loaded-file selection/header and type-before-symbol walker.
// The caller records any original NoTypesAndSymbols decision rather than claiming a pass.
func (c *authoredNoEmitCompilerTest) authoredFullBaselines() *tsbaseline.AuthoredFullBaselines {
	if c.harnessOptions.NoTypesAndSymbols {
		return nil
	}
	program := c.result.Program
	allFiles := core.Filter(core.Concatenate(c.toBeCompiled, c.otherFiles), func(f *harnessutil.TestFile) bool {
		return program.GetSourceFile(tspath.ToRootedFilePath(f.UnitName, c.currentDirectory)) != nil
	})
	headerComponents := tspath.GetPathComponentsRelativeTo(repo.TestDataPath(), c.filename, tspath.CaseInsensitive)
	header := tspath.GetPathFromPathComponents(headerComponents)
	result := tsbaseline.AuthoredObserveFullBaselines(program, allFiles, header, len(c.result.Diagnostics) > 0)
	return &result
}

func authoredNewNoEmitCompilerTest(
	t *testing.T,
	testName string,
	filename string,
	testContent *testCaseContent,
	namedConfiguration *harnessutil.NamedTestConfiguration,
) *authoredNoEmitCompilerTest {
	basename := tspath.GetBaseFileName(filename)
	configuredName := basename
	if namedConfiguration != nil && namedConfiguration.Name != "" {
		extname := tspath.GetAnyExtensionFromPath(basename, nil, tspath.CaseSensitive)
		extensionlessBasename := basename[:len(basename)-len(extname)]
		configuredName = fmt.Sprintf("%s(%s)%s", extensionlessBasename, namedConfiguration.Name, extname)
	}

	var configuration harnessutil.TestConfiguration
	if namedConfiguration != nil {
		configuration = namedConfiguration.Config
	}
	testCaseContentWithConfig := testCaseContentWithConfig{
		testCaseContent: *testContent,
		configuration:   configuration,
	}

	harnessConfig := testCaseContentWithConfig.configuration
	currentDirectory := srcFolder
	if rawCurrentDirectory := harnessConfig["currentdirectory"]; rawCurrentDirectory != "" {
		currentDirectory = tspath.ToRootedDirectoryPath(rawCurrentDirectory, srcFolder)
	}

	units := testCaseContentWithConfig.testUnitData
	var toBeCompiled []*harnessutil.TestFile
	var otherFiles []*harnessutil.TestFile
	var tsConfig *tsoptions.ParsedCommandLine
	hasNonDtsFiles := core.Some(
		units,
		func(unit *testUnit) bool { return !tspath.FileExtensionIs(unit.name, tspath.ExtensionDts) },
	)
	var tsConfigFiles []*harnessutil.TestFile
	if testCaseContentWithConfig.tsConfig != nil {
		tsConfig = testCaseContentWithConfig.tsConfig
		tsConfigFiles = []*harnessutil.TestFile{
			createHarnessTestFile(testCaseContentWithConfig.tsConfigFileUnitData, currentDirectory),
		}
		for _, unit := range units {
			if slices.Contains(
				tsConfig.ParsedConfig.FileNames,
				tspath.ToRootedFilePath(unit.name, currentDirectory),
			) {
				toBeCompiled = append(toBeCompiled, createHarnessTestFile(unit, currentDirectory))
			} else {
				otherFiles = append(otherFiles, createHarnessTestFile(unit, currentDirectory))
			}
		}
	} else {
		baseUrl, ok := harnessConfig["baseurl"]
		if ok && !tspath.IsRootedDiskPath(baseUrl) {
			harnessConfig["baseurl"] = tspath.ToRootedDirectoryPath(baseUrl, currentDirectory).AsString()
		}

		lastUnit := units[len(units)-1]
		// We need to assemble the list of input files for the compiler and other related files on the 'filesystem' (ie in a multi-file test)
		// If the last file in a test uses require or a triple slash reference we'll assume all other files will be brought in via references,
		// otherwise, assume all files are just meant to be in the same compilation session without explicit references to one another.

		if testCaseContentWithConfig.configuration["noimplicitreferences"] != "" ||
			strings.Contains(lastUnit.content, requireStr) ||
			referencesRegex.MatchString(lastUnit.content) {
			toBeCompiled = append(toBeCompiled, createHarnessTestFile(lastUnit, currentDirectory))
			for _, unit := range units[:len(units)-1] {
				otherFiles = append(otherFiles, createHarnessTestFile(unit, currentDirectory))
			}
		} else {
			toBeCompiled = core.Map(units, func(unit *testUnit) *harnessutil.TestFile { return createHarnessTestFile(unit, currentDirectory) })
		}
	}

	observed := harnessutil.AuthoredCompileFiles(
		t,
		toBeCompiled,
		otherFiles,
		harnessConfig,
		tsConfig,
		currentDirectory,
		testCaseContentWithConfig.symlinks,
	)

	result := observed.Result

	// Content-mapped files are transformed during program construction; the transformed text is what the
	// compiler actually parses and reports positions against. Baseline that text (rather than the original
	// foreign source) so the type, symbol, and error baselines line up with the compiler's positions.
	for _, file := range core.Concatenate(toBeCompiled, otherFiles) {
		if sf := result.Program.GetSourceFile(tspath.ToRootedFilePath(file.UnitName, currentDirectory)); sf != nil && sf.ContentMapper() != "" {
			file.Content = sf.Text()
		}
	}

	metadata := &compilerTest{
		testName:         testName,
		filename:         filename,
		basename:         basename,
		configuredName:   configuredName,
		currentDirectory: currentDirectory,
		options:          result.Options,
		harnessOptions:   result.HarnessOptions,
		result:           result,
		tsConfigFiles:    tsConfigFiles,
		toBeCompiled:     toBeCompiled,
		otherFiles:       otherFiles,
		hasNonDtsFiles:   hasNonDtsFiles,
	}
	return &authoredNoEmitCompilerTest{compilerTest: metadata, Channels: observed}
}
