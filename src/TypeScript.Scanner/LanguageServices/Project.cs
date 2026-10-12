// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.Compiler;
using TypeScript.Scanner;

namespace TypeScript.LanguageServices;
// internal/ls/crossproject.go:17. Complete project contract; IDs retain borrowed Go bytes.
internal interface IProject
{
    ByteStringValue Id();
    Program? GetProgram();
    bool HasFile(RootedFilePath fileName);
}
