// Authored managed compatibility support; not a new upstream algorithm.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
#nullable enable
namespace TypeScript.Scanner;

// Go JSDoc text keeps []string descriptors pointing into immutable source text.
// These four AST payloads retain views until the existing mutable List<string>
// API is requested. Clones share this object, including its canonical projection.
// A detached JSDoc node can intentionally retain the whole source, as in Go.
internal sealed class JSDocTextFragments
{
    private sealed record BorrowedFragments(Slice<ByteStringView> Values);
    private BorrowedFragments? views;
    private List<string>? strings;

    private JSDocTextFragments(BorrowedFragments? views, List<string>? strings)
        => (this.views, this.strings) = (views, strings);

    internal static JSDocTextFragments? FromViews(List<ByteStringView>? views)
        => views is null ? null : FromViews(Slice<ByteStringView>.FromArray(views.ToArray()));
    // The parser passes a detached arena snapshot. Header copying and pooled
    // scratch reset cannot change this persistent fragment set.
    internal static JSDocTextFragments? FromViews(Slice<ByteStringView> views)
        => views.IsNil ? null : new(new BorrowedFragments(views), null);
    internal static JSDocTextFragments? FromStrings(List<string>? strings)
        => strings is null ? null : new(null, strings);

    internal int Count
    {
        get
        {
            var borrowed = Volatile.Read(ref views);
            var saved = Volatile.Read(ref strings);
            return saved is not null ? saved.Count : borrowed!.Values.Length;
        }
    }

    internal ByteStringView this[int index]
    {
        get
        {
            var borrowed = Volatile.Read(ref views);
            var saved = Volatile.Read(ref strings);
            return saved is not null ? (ByteStringView)saved[index] : borrowed!.Values[index];
        }
    }

    internal List<string> AsList()
    {
        var borrowed = Volatile.Read(ref views);
        var saved = Volatile.Read(ref strings);
        if (saved is not null) return saved;
        var projected = new List<string>(borrowed!.Values.Length);
        foreach (var view in borrowed!.Values) projected.Add(view.Materialize());
        // Concurrent first callers must receive one canonical mutable list.
        var canonical = Interlocked.CompareExchange(ref strings, projected, null) ?? projected;
        // After projection, mutations must read only the canonical public list;
        // the obsolete view fragments need not keep their source alive.
        Volatile.Write(ref views, null);
        return canonical;
    }

    internal string Join()
    {
        var borrowed = Volatile.Read(ref views);
        var saved = Volatile.Read(ref strings);
        return saved is not null ? string.Concat(saved) : GoStrings.Join(borrowed!.Values, "");
    }

    internal void WriteTo(GoBuilder builder)
    {
        var borrowed = Volatile.Read(ref views);
        var saved = Volatile.Read(ref strings);
        if (saved is not null)
        {
            foreach (var value in saved) builder.WriteString(value);
        }
        else
        {
            foreach (var view in borrowed!.Values) builder.WriteString(view.AsSpan());
        }
    }
}
