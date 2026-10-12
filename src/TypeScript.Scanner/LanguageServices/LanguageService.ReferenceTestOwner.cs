// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original findallreferences_test.go constructs a partial Go LanguageService value
// with Program and Converters fields only. Retain that exact test owner admission.
using TypeScript.LanguageServices.Lsconv;
using CompilerProgram = TypeScript.Compiler.Program;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    internal LanguageService(CompilerProgram program, Converters converters) : this()
    {
        this.program = program;
        this.converters = converters;
    }
}
