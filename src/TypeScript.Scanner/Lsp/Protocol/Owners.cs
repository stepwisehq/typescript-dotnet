// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Managed headers for Go map values and pointer-to-interface protocol fields.
namespace TypeScript.Lsp.Protocol;

internal sealed class ProtocolPointer<T>(T value)
{
    internal T Value = value;
}

// A copied map header shares its dictionary, as a Go map value does. Nullable
// ProtocolMap means a Go *map: null pointer and pointer-to-nil map remain distinct.
internal readonly partial struct ProtocolMap<TKey, TValue>(Dictionary<TKey, TValue>? entries) where TKey : notnull
{
    private readonly Dictionary<TKey, TValue>? entries = entries;
    internal bool IsNil => entries is null;
    internal int Count => entries?.Count ?? 0;
    internal static ProtocolMap<TKey, TValue> Make() => new(new Dictionary<TKey, TValue>());

    internal bool TryGetValue(TKey key, out TValue value)
    {
        if (entries is not null)
            return entries.TryGetValue(key, out value!);
        value = default!;
        return false;
    }

    internal TValue this[TKey key]
    {
        get => TryGetValue(key, out var value) ? value : default!;
        set
        {
            if (entries is null)
                throw new InvalidOperationException("assignment to entry in nil map");
            entries[key] = value;
        }
    }

    public Enumerator GetEnumerator() => new(entries);
    internal struct Enumerator
    {
        private Dictionary<TKey, TValue>.Enumerator enumerator;
        private readonly bool hasEntries;
        internal Enumerator(Dictionary<TKey, TValue>? entries)
        {
            hasEntries = entries is not null;
            enumerator = entries is null ? default : entries.GetEnumerator();
        }
        public readonly KeyValuePair<TKey, TValue> Current => enumerator.Current;
        public bool MoveNext() => hasEntries && enumerator.MoveNext();
        public void Dispose() => enumerator.Dispose();
    }
}
