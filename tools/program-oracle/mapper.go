// Explicit in-process Project response fixtures over genuine TransformAndParse.
// Mapper configuration is supplied through the same public ParsedConfig source
// API used by original compiler mapper tests; disk configuration is separate.
package main

import (
	"errors"
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/contentmapper"
	"github.com/microsoft/TypeScript/tsc/internal/spanmap"
	"github.com/microsoft/TypeScript/tsc/internal/tsoptions"
	"github.com/microsoft/TypeScript/tsc/internal/tspath"
)

type mappedResultInput struct {
	Text, VirtualExtension string
	MappingJSONBase64      *string
	NilMappings            bool
}
type mapperProjectInput struct {
	mappedResultInput
	Supplemental     []mappedResultInput
	ErrorKind        *uint8
	ProjectErrorKind *uint8
	ErrorText        string
	OptionDiagnostic bool
}
type fixtureMapperProject struct {
	input  *mapperProjectInput
	mapper *contentmapper.Mapper
}

func configureMapperProject(parsed *tsoptions.ParsedCommandLine, input *mapperProjectInput) contentmapper.Project {
	if input == nil || parsed == nil {
		return nil
	}
	mapper := &contentmapper.Mapper{Package: "vue", Extensions: []string{".vue"}, Name: "authored-mapper", Version: "1.0.0"}
	parsed.ParsedConfig.ContentMappers = []*contentmapper.Mapper{mapper}
	return &fixtureMapperProject{input: input, mapper: mapper}
}
func (p *fixtureMapperProject) Refresh() error                { return nil }
func (p *fixtureMapperProject) Identities() ([]string, error) { return nil, nil }
func (p *fixtureMapperProject) Identity(*contentmapper.Mapper) (string, error) {
	return "fixture-identity", nil
}
func (p *fixtureMapperProject) WatchedFiles() ([]tspath.RootedFilePath, error) { return nil, nil }
func (p *fixtureMapperProject) Close() error                                   { return nil }
func (p *fixtureMapperProject) Diagnostics() []contentmapper.OptionDiagnostic {
	if !p.input.OptionDiagnostic {
		return nil
	}
	return []contentmapper.OptionDiagnostic{{Mapper: p.mapper, Source: "fixture", Code: 9001, MessageText: "Authored mapper option diagnostic."}}
}
func mappedInput(input mappedResultInput) contentmapper.MappedResult {
	var mapping *spanmap.SpanMap
	if !input.NilMappings {
		mapping = spanmap.New(nil)
		if input.MappingJSONBase64 != nil {
			var err error
			mapping, err = spanmap.Unmarshal([]byte(inputBytes("", input.MappingJSONBase64)))
			if err != nil {
				panic(err)
			}
		}
	}
	return contentmapper.MappedResult{Text: input.Text, VirtualExtension: input.VirtualExtension, Mappings: mapping}
}
func (p *fixtureMapperProject) Transform(_ *contentmapper.Mapper, _ contentmapper.Request) (contentmapper.Result, error) {
	if p.input.ProjectErrorKind != nil {
		return contentmapper.Result{}, &contentmapper.ProjectError{Kind: contentmapper.ProjectErrorKind(*p.input.ProjectErrorKind)}
	}
	if p.input.ErrorKind != nil {
		return contentmapper.Result{}, contentmapper.NewTransformError(contentmapper.TransformErrorKind(*p.input.ErrorKind), errors.New(p.input.ErrorText))
	}
	main := mappedInput(p.input.mappedResultInput)
	var supplemental []contentmapper.MappedResult
	if p.input.Supplemental != nil {
		supplemental = []contentmapper.MappedResult{}
		for _, input := range p.input.Supplemental {
			supplemental = append(supplemental, mappedInput(input))
		}
	}
	return contentmapper.Result{Text: main.Text, VirtualExtension: main.VirtualExtension, Mappings: main.Mappings, Supplemental: supplemental}, nil
}
func (o *observer) mappedSource(file *ast.SourceFile) any {
	if file.ContentMapper() == "" {
		return nil
	}
	var mapBytes any
	if file.SpanMap() != nil {
		data, err := file.SpanMap().Marshal()
		if err != nil {
			panic(err)
		}
		mapBytes = b64(string(data))
	}
	var supplemental any
	if file.SupplementalSourceFiles() != nil {
		ids := []any{}
		for _, child := range file.SupplementalSourceFiles() {
			ids = append(ids, o.file(child))
		}
		supplemental = ids
	}
	return map[string]any{"mapperBase64": b64(file.ContentMapper()), "originalTextBase64": b64(file.OriginalText()), "originalFileNameBase64": b64(file.OriginalFileName()), "identityBase64": b64(file.ContentMapperTransformIdentity()), "failureStub": file.IsContentMapperFailureStub(), "mapBase64": mapBytes, "canonical": o.file(file.CanonicalSourceFile()), "supplemental": supplemental}
}
