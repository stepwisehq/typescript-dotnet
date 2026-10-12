// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Go LanguageService permits a zero-value owner. Original ls/format_test.go calls
// formatting-only workers on that owner; their genuine bodies do not query Program.
namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    internal LanguageService()
    {
        projectID = null!;
        host = null!;
        activeConfig = default;
        program = null!;
        converters = null!;
        documentPositionMappers = null!;
    }
}
