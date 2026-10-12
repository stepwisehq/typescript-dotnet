// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.ContentMapper;
using TypeScript.Scanner;

namespace TypeScript.ProjectSystem;
// Authored boundary adaptation: project/session Go []*Mapper headers coexist with
// the accepted parsed-command-line List owner. Retaining the header requires no
// copy; only publishing it through that existing List API creates a detached owner.
internal readonly struct MapperHeaders
{
    private readonly List<Mapper>? legacy;
    private readonly Slice<Mapper> values;
    private MapperHeaders(List<Mapper>? legacy, Slice<Mapper> values)
    {
        this.legacy = legacy;
        this.values = values;
    }

    public static implicit operator MapperHeaders(List<Mapper>? values) => new(values, default);
    public static implicit operator MapperHeaders(Slice<Mapper> values) => new(null, values);
    internal List<Mapper>? AsCommandLineList()
    {
        if (legacy is not null)
            return legacy;
        if (values.IsNil)
            return null;
        var result = new List<Mapper>(values.Length);
        foreach (var mapper in values)
            result.Add(mapper);
        return result;
    }
}
