// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/host.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.LanguageServices.AutoImport;
using TypeScript.LanguageServices.Lsconv;
using TypeScript.LanguageServices.Utilities;
using TypeScript.SourceMap;

namespace TypeScript.LanguageServices;
internal interface ILanguageServiceHost
{
    CaseSensitivity CaseSensitivity();
    (ByteStringValue Contents, bool Ok) ReadFile(RootedFilePath path);
    Converters Converters();
    UserPreferences GetPreferences(ByteStringValue activeFile);
    ECMALineInfo? GetECMALineInfo(RootedFilePath fileName);
    Registry? AutoImportRegistry();
    // Used only for module-specifier completions: these queries intentionally cross
    // the upstream snapshot-in-time boundary. Do not use them for other features.
    Slice<RootedFilePath> ReadDirectory(RootedDirectoryPath path, Slice<ByteStringValue> extensions, Slice<ByteStringValue> excludes, Slice<ByteStringValue> includes, int depth);
    Slice<ByteStringValue> GetDirectories(RootedDirectoryPath path);
    bool DirectoryExists(RootedDirectoryPath path);
    bool FileExists(RootedFilePath path);
}
