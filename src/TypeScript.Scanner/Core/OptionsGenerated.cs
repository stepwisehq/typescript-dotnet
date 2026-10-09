// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/core/options_generated.go.
namespace TypeScript.Scanner;
// ScriptTarget plus parser-required ModuleKind/NewLineKind enum slices.
public enum ScriptTarget
{
    None = 0,
    ES5 = 1,
    ES2015 = 2,
    ES2016 = 3,
    ES2017 = 4,
    ES2018 = 5,
    ES2019 = 6,
    ES2020 = 7,
    ES2021 = 8,
    ES2022 = 9,
    ES2023 = 10,
    ES2024 = 11,
    ES2025 = 12,
    ES2026 = 13,
    ESNext = 99,
    JSON = 100,
    Latest = ESNext,
    LatestStandard = ES2026,
}

public enum ModuleKind
{
    None = 0,
    CommonJS = 1,
    AMD = 2,
    UMD = 3,
    System = 4,
    ES2015 = 5,
    ES2020 = 6,
    ES2022 = 7,
    ESNext = 99,
    Node16 = 100,
    Node18 = 101,
    Node20 = 102,
    NodeNext = 199,
    Preserve = 200
}

internal enum NewLineKind
{
    None = 0,
    CRLF = 1,
    LF = 2
}

// Selected CompilerOptions fields from options_generated.go; semantic slice only.
// No broad compiler option serialization/configuration/merge implementation.
public sealed partial class CompilerOptions
{
    public ScriptTarget Target;
    public ModuleKind Module;
    public Tristate NoResolve, SkipLibCheck, DownlevelIteration, NoUncheckedIndexedAccess;
    public Tristate NoLib, Strict, StrictNullChecks, NoImplicitAny, NoImplicitThis, NoImplicitReturns;
    public Tristate CheckJs;
    public Tristate StrictFunctionTypes, StrictBindCallApply, StrictPropertyInitialization;
    public Tristate StrictBuiltinIteratorReturn, UseUnknownInCatchVariables;
    public Tristate AllowUnreachableCode;
    public Tristate ExactOptionalPropertyTypes, ExperimentalDecorators;
    public Tristate VerbatimModuleSyntax, IsolatedModules, UseDefineForClassFields;
    public List<string>? Types;
}
