// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.DiagnosticPrinting;
[Flags]
internal enum GeneratedIdentifierFlags
{
    None = 0, Auto = 1, Loop = 2, Unique = 3, Node = 4, KindMask = 7,
    ReservedInNestedScopes = 1 << 3, Optimistic = 1 << 4, FileLevel = 1 << 5, AllowNameSubstitution = 1 << 6
}
internal static class GeneratedIdentifierFlagExtensions
{
    internal static GeneratedIdentifierFlags Kind(this GeneratedIdentifierFlags f) => f & GeneratedIdentifierFlags.KindMask;
    internal static bool IsAuto(this GeneratedIdentifierFlags f) => f.Kind() == GeneratedIdentifierFlags.Auto;
    internal static bool IsLoop(this GeneratedIdentifierFlags f) => f.Kind() == GeneratedIdentifierFlags.Loop;
    internal static bool IsUnique(this GeneratedIdentifierFlags f) => f.Kind() == GeneratedIdentifierFlags.Unique;
    internal static bool IsNode(this GeneratedIdentifierFlags f) => f.Kind() == GeneratedIdentifierFlags.Node;
    internal static bool IsReservedInNestedScopes(this GeneratedIdentifierFlags f) => (f & GeneratedIdentifierFlags.ReservedInNestedScopes) != 0;
    internal static bool IsOptimistic(this GeneratedIdentifierFlags f) => (f & GeneratedIdentifierFlags.Optimistic) != 0;
    internal static bool IsFileLevel(this GeneratedIdentifierFlags f) => (f & GeneratedIdentifierFlags.FileLevel) != 0;
    internal static bool HasAllowNameSubstitution(this GeneratedIdentifierFlags f) => (f & GeneratedIdentifierFlags.AllowNameSubstitution) != 0;
}
