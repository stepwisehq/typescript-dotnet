// Authored read-only configuration observation. No compiler algorithms are changed.
package tsoptions

import (
    "github.com/microsoft/TypeScript/tsc/internal/ast"
    "github.com/microsoft/TypeScript/tsc/internal/core"
    "github.com/microsoft/TypeScript/tsc/internal/tspath"
)

type OracleConfigurationField struct { Name string; Value any }

func oracleConfigurationSpecs(s *configFileSpecs) []OracleConfigurationField {
    if s == nil { return nil }
    return []OracleConfigurationField{
        {"filesSpecs",s.filesSpecs}, {"includeSpecs",s.includeSpecs}, {"excludeSpecs",s.excludeSpecs},
        {"validatedFilesSpec",s.validatedFilesSpec}, {"validatedFileNames",s.validatedFileNames},
        {"fileSpecByPath",s.fileSpecByPath}, {"validatedIncludeSpecs",s.validatedIncludeSpecs},
        {"validatedExcludeSpecs",s.validatedExcludeSpecs},
        {"validatedFilesSpecBeforeSubstitution",s.validatedFilesSpecBeforeSubstitution},
        {"validatedIncludeSpecsBeforeSubstitution",s.validatedIncludeSpecsBeforeSubstitution},
        {"isDefaultIncludeSpec",s.isDefaultIncludeSpec},
    }
}

func OracleConfigurationSpecs(p *ParsedCommandLine) []OracleConfigurationField {
    // Mirror the stored selection, without invoking lazy parsed-project getters.
    if p.configFileSpecs != nil {return oracleConfigurationSpecs(p.configFileSpecs)}
    if p.ConfigFile != nil {return oracleConfigurationSpecs(p.ConfigFile.configFileSpecs)}
    return nil
}

type OracleConfigurationExtendedEntry struct {
    Source *ast.SourceFile
    Specs []OracleConfigurationField
    ExtendedSourceFiles []tspath.RootedFilePath
    HasParsedConfig bool
    Raw any
    Options *core.CompilerOptions
    UnresolvedPaths map[string]any
    TypeAcquisition *core.TypeAcquisition
    ExtendedConfigPaths []tspath.RootedFilePath
    Errors []*ast.Diagnostic
}

func OracleConfigurationExtendedConfig(e *ExtendedConfigCacheEntry) OracleConfigurationExtendedEntry {
    if e == nil {panic("nil cache entry")}
    result := OracleConfigurationExtendedEntry{Errors:e.errors}
    if e.extendedResult != nil {
        result.Source=e.extendedResult.SourceFile
        result.Specs=oracleConfigurationSpecs(e.extendedResult.configFileSpecs)
        result.ExtendedSourceFiles=e.extendedResult.ExtendedSourceFiles
    }
    if e.extendedConfig != nil {
        result.HasParsedConfig=true
        result.Raw=e.extendedConfig.raw
        result.TypeAcquisition=e.extendedConfig.typeAcquisition
        result.ExtendedConfigPaths=e.extendedConfig.extendedConfigPaths
        if e.extendedConfig.options != nil {
            result.Options=e.extendedConfig.options.CompilerOptions
            result.UnresolvedPaths=e.extendedConfig.options.unresolvedPaths
        }
    }
    return result
}
