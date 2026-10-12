// Copyright 2011 The Go Authors. All rights reserved.
// Go-derived worker licensed under the BSD license in licenses/Go-LICENSE.txt.
// Selected strings/replace.go genericReplacer closure, pinned Go 1.27.1.
// The LS quote replacer's mixed one/two-byte keys select this actual Go engine.
using System.Runtime.CompilerServices;

namespace TypeScript.Scanner;
internal sealed class GoGenericReplacer
{
    [InlineArray(256)]
    private struct Mapping
    {
        private byte element;
    }

    private Mapping mapping;
    private readonly TrieNode root = new();
    private int tableSize;
    private sealed class TrieNode
    {
        internal ByteStringValue value;
        internal int priority;
        internal ByteStringValue prefix;
        internal TrieNode? next;
        internal TrieNode? []? table;
        // replace.go:159. Prefix splitting preserves key order priorities.
        internal void Add(ByteStringValue key, ByteStringValue replacement, int newPriority, GoGenericReplacer owner)
        {
            if (key.Length == 0)
            {
                if (priority == 0)
                {
                    value = replacement;
                    priority = newPriority;
                }

                return;
            }

            if (prefix.Length != 0)
            {
                int length = 0;
                for (; length < prefix.Length && length < key.Length; length++)
                    if (prefix[length] != key[length])
                        break;
                if (length == prefix.Length)
                    next!.Add(key.Slice(length), replacement, newPriority, owner);
                else if (length == 0)
                {
                    var prefixNode = prefix.Length == 1 ? next : new TrieNode
                    {
                        prefix = prefix.Slice(1),
                        next = next
                    };
                    var keyNode = new TrieNode();
                    table = new TrieNode? [owner.tableSize];
                    table[owner.mapping[prefix[0]]] = prefixNode;
                    table[owner.mapping[key[0]]] = keyNode;
                    prefix = default;
                    next = null;
                    keyNode.Add(key.Slice(1), replacement, newPriority, owner);
                }
                else
                {
                    var child = new TrieNode
                    {
                        prefix = prefix.Slice(length),
                        next = next
                    };
                    prefix = prefix.Slice(0, length);
                    next = child;
                    child.Add(key.Slice(length), replacement, newPriority, owner);
                }
            }
            else if (table != null)
            {
                int index = owner.mapping[key[0]];
                table[index] ??= new TrieNode();
                table[index]!.Add(key.Slice(1), replacement, newPriority, owner);
            }
            else
            {
                prefix = key;
                next = new TrieNode();
                next.Add(default, replacement, newPriority, owner);
            }
        }
    }

    // replace.go:218. Longest path is searched, but argument priority wins.
    private (ByteStringValue Value, int Length, bool Found) Lookup(ByteStringValue text, bool ignoreRoot)
    {
        int bestPriority = 0, consumed = 0, keyLength = 0;
        bool found = false;
        ByteStringValue value = default;
        TrieNode? node = root;
        while (node != null)
        {
            if (node.priority > bestPriority && !(ignoreRoot && node == root))
            {
                bestPriority = node.priority;
                value = node.value;
                keyLength = consumed;
                found = true;
            }

            if (text.Length == 0)
                break;
            if (node.table != null)
            {
                int index = mapping[text[0]];
                if (index == tableSize)
                    break;
                node = node.table[index];
                text = text.Slice(1);
                consumed++;
            }
            else if (node.prefix.Length != 0 && text.StartsWith(node.prefix))
            {
                consumed += node.prefix.Length;
                text = text.Slice(node.prefix.Length);
                node = node.next;
            }
            else
                break;
        }

        return (value, keyLength, found);
    }

    // replace.go:267. Retain the actual dense mapping and root table owners.
    internal static GoGenericReplacer Make(Slice<ByteStringValue> oldNew)
    {
        var owner = new GoGenericReplacer();
        for (int i = 0; i < oldNew.Length; i += 2)
            for (int j = 0; j < oldNew[i].Length; j++)
                owner.mapping[oldNew[i][j]] = 1;
        for (int i = 0; i < 256; i++)
            owner.tableSize += owner.mapping[i];
        byte index = 0;
        for (int i = 0; i < 256; i++)
        {
            if (owner.mapping[i] == 0)
                owner.mapping[i] = unchecked((byte)owner.tableSize);
            else
            {
                owner.mapping[i] = index;
                index = unchecked((byte)(index + 1));
            }
        }

        owner.root.table = new TrieNode? [owner.tableSize];
        for (int i = 0; i < oldNew.Length; i += 2)
            owner.root.Add(oldNew[i], oldNew[i + 1], oldNew.Length - i, owner);
        return owner;
    }

    // replace.go:331/337. The original Replace writer cannot return I/O errors:
    // its real sink is the append-slice owner retained directly here.
    internal ByteStringValue Replace(ByteStringValue text)
    {
        var buffer = Slice<byte>.Make(0, text.Length);
        int last = 0;
        bool previousMatchEmpty = false;
        for (int index = 0; index <= text.Length;)
        {
            if (index != text.Length && root.priority == 0)
            {
                int mapped = mapping[text[index]];
                if (mapped == tableSize || root.table![mapped] == null)
                {
                    index++;
                    continue;
                }
            }

            var(value, keyLength, match) = Lookup(text.Slice(index), previousMatchEmpty);
            previousMatchEmpty = match && keyLength == 0;
            if (match)
            {
                buffer = AppendRaw(buffer, text.Slice(last, index));
                buffer = AppendRaw(buffer, value);
                index += keyLength;
                last = index;
                continue;
            }

            index++;
        }

        if (last != text.Length)
            buffer = AppendRaw(buffer, text.Slice(last));
        // Go string(buf) requires an immutable result copy; retain that boundary.
        return ByteStringValue.FromView(ByteStringView.FromOwnedBytes(buffer.AsSpan().ToArray()));
    }

    private static Slice<byte> AppendRaw(Slice<byte> buffer, ByteStringValue text)
    {
        if (text.IsBytes)
            return buffer.Append(text.ByteSpan);
        int offset = buffer.Length;
        buffer = buffer.Grow(text.Length).Slice2(0, offset + text.Length);
        for (int i = 0; i < text.Length; i++)
            buffer[offset + i] = (byte)text[i];
        return buffer;
    }
}
