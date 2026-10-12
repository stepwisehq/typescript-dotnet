// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/source_map.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.LanguageServices.Lsconv;
using TypeScript.Lsp.Protocol;
using TypeScript.SourceMap;
using TypeScript.SpanMap;
using LspRange = TypeScript.Lsp.Protocol.Range;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    // source_map.go:18. Compose content-mapper projection before declaration maps.
    private (Location Location, Fidelity Fidelity) sourceFileRangeToLSPLocation(SourceFile file, TextRange fileRange)
    {
        if (file.ContentMapper() != "")
            return converters.ToLSPLocation(file, fileRange);
        return getMappedLocation(RootedFilePath.FromValue(file.FileNameGoValue()), fileRange);
    }

    // source_map.go:28. Feature filtering applies only to visible service results.
    private (Location Location, Fidelity Fidelity) sourceFileRangeToLSPLocationForFeature(SourceFile file, TextRange fileRange, Feature feature)
    {
        if (file.ContentMapper() != "")
            return converters.ToLSPLocationForFeature(file, fileRange, feature);
        return getMappedLocation(RootedFilePath.FromValue(file.FileNameGoValue()), fileRange);
    }

    // source_map.go:38.
    private (Location Location, Fidelity Fidelity) getMappedLocation(RootedFilePath fileName, TextRange fileRange)
    {
        var startPos = tryGetSourcePosition(fileName, fileRange.Pos());
        if (startPos == null)
        {
            var (range, fidelity) = createLspRangeFromRange(fileRange, getScript(fileName));
            return (new Location { Uri = TypeScript.LanguageServices.Lsconv.Lsconv.FileNameToDocumentURI(fileName), Range = range }, fidelity);
        }

        var endPos = tryGetSourcePosition(fileName, fileRange.End());
        if (endPos == null || endPos.FileName != startPos.FileName || endPos.Pos < startPos.Pos)
            endPos = new DocumentPosition(startPos.FileName, startPos.Pos + fileRange.Len());
        var (mappedRange, mappedFidelity) = createLspRangeFromRange(new TextRange(startPos.Pos, endPos.Pos), getScript(startPos.FileName));
        return (new Location { Uri = TypeScript.LanguageServices.Lsconv.Lsconv.FileNameToDocumentURI(startPos.FileName), Range = mappedRange }, mappedFidelity);
    }

    // source_map.go:65. The text header borrows the actual host read owner.
    private sealed class Script : IScript
    {
        internal RootedFilePath fileName;
        internal ByteStringValue text;
        public RootedFilePath FileName() => fileName;
        public RootedFilePath OriginalFileName() => fileName;
        public ByteStringValue TextValue() => text;
        public ByteStringValue OriginalTextValue() => text;
        public TypeScript.SpanMap.SpanMap? SpanMap() => null;
    }

    private Script? getScript(RootedFilePath fileName)
    {
        var (text, ok) = host.ReadFile(fileName);
        return ok ? new Script
        {
            fileName = fileName,
            text = text
        }

        : null;
    }

    // source_map.go:93.
    private DocumentPosition? tryGetSourcePosition(RootedFilePath fileName, int position)
    {
        var newPos = tryGetSourcePositionWorker(fileName, position);
        if (newPos != null && !ReadFile(newPos.FileName).Ok)
            return null;
        return newPos;
    }

    private DocumentPosition? tryGetSourcePositionWorker(RootedFilePath fileName, int position)
    {
        if (!fileName.IsDeclarationFile())
            return null;
        var positionMapper = GetDocumentPositionMapper(fileName);
        var documentPos = positionMapper?.GetSourcePosition(new DocumentPosition(fileName, position));
        if (documentPos == null)
            return null;
        var newPos = tryGetSourcePositionWorker(documentPos.FileName, documentPos.Pos);
        return newPos ?? documentPos;
    }

    // source_map.go:129.
    private DocumentPosition? tryGetGeneratedPosition(RootedFilePath fileName, int position)
    {
        var newPos = tryGetGeneratedPositionWorker(fileName, position);
        if (newPos != null && !ReadFile(newPos.FileName).Ok)
            return null;
        return newPos;
    }

    private DocumentPosition? tryGetGeneratedPositionWorker(RootedFilePath fileName, int position)
    {
        if (fileName.IsDeclarationFile())
            return null;
        var currentProgram = GetProgram();
        if (currentProgram == null || currentProgram.GetSourceFile(fileName) == null)
            return null;
        var path = currentProgram.PathKeyForFileName(fileName);
        if (currentProgram.IsSourceFromProjectReference(path))
            return null;
        var declarationFileName = TypeScript.OutputPaths.OutputPaths.GetOutputDeclarationFileNameWorker(fileName, currentProgram.Options(), currentProgram);
        var positionMapper = GetDocumentPositionMapper(declarationFileName);
        var documentPos = positionMapper?.GetGeneratedPosition(new DocumentPosition(fileName, position));
        if (documentPos == null)
            return null;
        var newPos = tryGetGeneratedPositionWorker(documentPos.FileName, documentPos.Pos);
        return newPos ?? documentPos;
    }
}
