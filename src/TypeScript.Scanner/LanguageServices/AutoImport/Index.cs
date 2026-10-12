// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete counterpart of internal/ls/autoimport/index.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;

namespace TypeScript.LanguageServices.AutoImport;
internal interface INamed
{
    ByteStringValue Name();
}

// Every retained Go Index pointer maps to this stable owner; slice headers retain backing owners.
internal sealed class Index<T>
    where T : INamed
{
    private Slice<T> entries;
    internal Slice<T> Entries => entries;

    private Dictionary<int, Slice<int>>? index;
    // Same-package original index_test.go observes this map cardinality.
    internal int IndexCount => index?.Count ?? 0;

    // internal/ls/autoimport/index.go:24
    internal Slice<T> Find(ByteStringValue name, bool caseSensitive)
    {
        if (entries.Count == 0 || name.Length == 0)
            return default;
        var firstRune = Decode(name).Rune;
        if (firstRune == 0xfffd)
            return default;
        var firstRuneUpper = UnicodeCase.ToUpper(firstRune);
        if (index == null || !index.TryGetValue(firstRuneUpper, out var candidates))
            return default;
        Slice<T> results = default;
        foreach (var entryIndex in candidates)
        {
            var entry = entries[entryIndex];
            var entryName = entry.Name();
            if ((caseSensitive && entryName.EqualsValue(name)) || (!caseSensitive && UnicodeCase.EqualFold(entryName, name)))
                results = results.Append(entry);
        }

        return results;
    }

    // internal/ls/autoimport/index.go:54
    internal Slice<T> SearchWordPrefix(ByteStringValue prefix)
    {
        if (entries.Count == 0)
            return default;
        if (prefix.Length == 0)
            return entries;
        prefix = GoStrings.ToLower(prefix);
        var firstRune = Decode(prefix).Rune;
        if (firstRune == 0xfffd)
            return default;
        var firstRuneUpper = UnicodeCase.ToUpper(firstRune);
        var firstRuneLower = UnicodeCase.ToLower(firstRune);
        Slice<int> nameStarts = default, wordStarts = default;
        index?.TryGetValue(firstRuneUpper, out nameStarts);
        if (firstRuneUpper != firstRuneLower)
            index?.TryGetValue(firstRuneLower, out wordStarts);
        var count = nameStarts.Count + wordStarts.Count;
        if (count == 0)
            return default;
        var results = Slice<T>.Make(0, count);
        // The original two-header temporary needs no retained backing owner.
        for (var pass = 0; pass < 2; pass++)
        {
            var starts = pass == 0 ? nameStarts : wordStarts;
            foreach (var i in starts)
            {
                var entry = entries[i];
                if (ContainsCharsInOrder(entry.Name(), prefix))
                    results = results.Append(entry);
            }
        }

        return results;
    }

    // internal/ls/autoimport/index.go:99
    internal static bool ContainsCharsInOrder(ByteStringValue str, ByteStringValue pattern)
    {
        str = GoStrings.ToLower(str);
        pattern = GoStrings.ToLower(pattern);
        var patternIndex = 0;
        for (var offset = 0; offset < str.Length;)
        {
            var(ch, width) = Decode(str.Slice(offset));
            offset += width;
            if (patternIndex < pattern.Length)
            {
                var(patternRune, size) = Decode(pattern.Slice(patternIndex));
                if (ch == patternRune)
                    patternIndex += size;
            }
        }

        return patternIndex == pattern.Length;
    }

    // internal/ls/autoimport/index.go:114
    internal void InsertAsWords(T value)
    {
        index ??= new();
        var name = value.Name();
        if (name.Length == 0)
            throw new InvalidOperationException("Cannot index entry with empty name");
        var entryIndex = entries.Count;
        entries = entries.Append(value);
        var indices = AutoImport.WordIndices(name);
        var seenRunes = new HashSet<int>();
        for (var i = 0; i < indices.Count; i++)
        {
            var firstRune = Decode(name.Slice(indices[i])).Rune;
            if (firstRune == 0xfffd)
                continue;
            if (i == 0)
            {
                firstRune = UnicodeCase.ToUpper(firstRune);
                index.TryGetValue(firstRune, out var values);
                index[firstRune] = values.Append(entryIndex);
                seenRunes.Add(firstRune);
            }
            else
            {
                firstRune = UnicodeCase.ToLower(firstRune);
                if (!seenRunes.Contains(firstRune))
                {
                    index.TryGetValue(firstRune, out var values);
                    index[firstRune] = values.Append(entryIndex);
                    seenRunes.Add(firstRune);
                }
            }
        }
    }

    // internal/ls/autoimport/index.go:152. The nullable receiver branch is retained by CloneNullable.
    internal static Index<T>? CloneNullable(Index<T>? owner, Func<T, bool> filter) => owner?.Clone(filter);
    internal Index<T> Clone(Func<T, bool> filter)
    {
        var newIndex = new Index<T>
        {
            entries = Slice<T>.Make(0, entries.Count),
            index = new Dictionary<int, Slice<int>>(index?.Count ?? 0),
        };
        var oldToNew = new Dictionary<int, int>(entries.Count);
        for (var oldIndex = 0; oldIndex < entries.Count; oldIndex++)
        {
            var entry = entries[oldIndex];
            if (filter(entry))
            {
                var newEntryIndex = newIndex.entries.Count;
                newIndex.entries = newIndex.entries.Append(entry);
                oldToNew[oldIndex] = newEntryIndex;
            }
        }

        if (index != null)
        {
            foreach (var(rune, oldIndices)in index)
            {
                var newIndices = Slice<int>.Make(0, oldIndices.Count);
                foreach (var oldIndex in oldIndices)
                    if (oldToNew.TryGetValue(oldIndex, out var mapped))
                        newIndices = newIndices.Append(mapped);
                if (newIndices.Count > 0)
                    newIndex.index[rune] = newIndices;
            }
        }

        return newIndex;
    }

    private static (int Rune, int Width) Decode(ByteStringValue value) => value.IsBytes ? Utf8.DecodeRuneInString(value.ByteSpan) : Utf8.DecodeRuneInString(value.CharSpan);
}
