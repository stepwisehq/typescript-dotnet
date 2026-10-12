// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/project/dirty/interfaces.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.ProjectSystem.Dirty;
internal interface ICloneable<T>
{
    T Clone();
}

internal interface IValue<T>
{
    T Value();
    T Original();
    bool Dirty();
    void Change(Action<T> apply);
    bool ChangeIf(Func<T, bool> cond, Action<T> apply);
    void Delete();
    void Locked(Action<IValue<T>> fn);
}
