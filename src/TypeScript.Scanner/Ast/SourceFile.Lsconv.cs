// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// SourceFile's genuine lsconv.Script implementation; Go strings retain byte headers.
using TypeScript.LanguageServices.Lsconv;
namespace TypeScript.Scanner;

public sealed partial class SourceFile : IScript
{
    RootedFilePath IScript.FileName() => RootedFilePath.FromValue(FileNameGoValue());
    ByteStringValue IScript.TextValue() => ByteStringValue.FromView(sourceText);
    ByteStringValue IScript.OriginalTextValue() => ContentMapper() != ""
        ? ByteStringValue.FromString(contentMapperInfo!.Value.OriginalText)
        : ByteStringValue.FromView(sourceText);
}
