// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/project/dirty/box.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.ProjectSystem.Dirty;
internal sealed class Box<T>(T original) : IValue<T> where T : ICloneable<T>
{
    private readonly T original = original;
    private T value = original;
    private bool dirty, deleted;
    // box.go:14 Value.
    public T Value() => deleted ? default! : value;
    // box.go:22 Original.
    public T Original() => original;
    // box.go:26 Dirty.
    public bool Dirty() => dirty;
    // box.go:30 Set.
    public void Set(T replacement)
    {
        value = replacement;
        deleted = false;
        dirty = true;
    }

    // box.go:36 Change.
    public void Change(Action<T> apply)
    {
        if (!dirty)
        {
            value = value.Clone();
            dirty = true;
        }

        apply(value);
    }

    // box.go:44 ChangeIf.
    public bool ChangeIf(Func<T, bool> cond, Action<T> apply)
    {
        if (!cond(value))
            return false;
        Change(apply);
        return true;
    }

    // box.go:52 Delete.
    public void Delete() => deleted = true;
    // box.go:56 Locked.
    public void Locked(Action<IValue<T>> fn) => fn(this);
    // box.go:60 FinalizeValue.
    public (T Value, bool Changed) FinalizeValue() => (Value(), dirty || deleted);
}
