// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/exports.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Scalar IsTypeAssignableTo export only; general checker exports excluded.
namespace TypeScript.Checking;
public sealed partial class Checker
{
    public bool IsTypeAssignableTo(Type source, Type target)
    {
        if (source.checker != this || target.checker != this)
            throw new ArgumentException("Types belong to a different checker");
        return isTypeAssignableTo(source, target);
    }
}
