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
    // Immutable header: projection may publish a shared mutable public list,
    // but never rewrites this multiword header during concurrent reads.
    private readonly Slice<ByteStringView> views;
    private List<string>? strings;

    private JSDocTextFragments(Slice<ByteStringView> views, List<string>? strings)
        => (this.views, this.strings) = (views, strings);

    internal static JSDocTextFragments? FromViews(List<ByteStringView>? views)
        => views is null ? null : FromViews(Slice<ByteStringView>.FromArray(views.ToArray()));
    // The parser passes a detached arena snapshot. Header copying and pooled
    // scratch reset cannot change this persistent fragment set.
    internal static JSDocTextFragments? FromViews(Slice<ByteStringView> views)
        => views.IsNil ? null : new(views, null);
    internal static JSDocTextFragments? FromStrings(List<string>? strings)
        => strings is null ? null : new(default, strings);

    internal int Count
    {
        get
        {
            var borrowed = views;
            var saved = Volatile.Read(ref strings);
            return saved is not null ? saved.Count : borrowed.Length;
        }
    }

    internal ByteStringView this[int index]
    {
        get
        {
            var borrowed = views;
            var saved = Volatile.Read(ref strings);
            return saved is not null ? (ByteStringView)saved[index] : borrowed[index];
        }
    }

    internal List<string> AsList()
    {
        var borrowed = views;
        var saved = Volatile.Read(ref strings);
        if (saved is not null) return saved;
        var projected = new List<string>(borrowed.Length);
        foreach (var view in borrowed) projected.Add(view.Materialize());
        // Concurrent first callers must receive one canonical mutable list.
        var canonical = Interlocked.CompareExchange(ref strings, projected, null) ?? projected;
        // Public mutations read only the canonical list. Retain the immutable
        // source header as Go does; it must not race with concurrent readers.
        return canonical;
    }

    internal ByteStringValue JoinValue()
    {
        var borrowed = views;
        var saved = Volatile.Read(ref strings);
        if (saved is not null)
        {
            if (saved.Count == 0) return default;
            if (saved.Count == 1) return ByteStringValue.FromString(saved[0] ?? "");
        }
        else
        {
            if (borrowed.Length == 0) return default;
            if (borrowed.Length == 1) return ByteStringValue.FromView(borrowed[0]);
        }
        var builder = new GoBuilder();
        WriteTo(builder);
        return builder.Value();
    }

    internal string Join()
    {
        var borrowed = views;
        var saved = Volatile.Read(ref strings);
        return saved is not null ? string.Concat(saved) : GoStrings.Join(borrowed, "");
    }

    internal void WriteTo(GoBuilder builder)
    {
        var borrowed = views;
        var saved = Volatile.Read(ref strings);
        if (saved is not null)
        {
            foreach (var value in saved) builder.WriteString(value);
        }
        else
        {
            foreach (var view in borrowed) builder.WriteString(view.AsSpan());
        }
    }
}
