// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// server.go and ipc/conn_async.go pending maps copy *jsonrpc.ID into a value key.
// Authored CLR map-key adaptation; draft outside compilation.
using TypeScript.JsonRpc;
using TypeScript.Scanner;

namespace TypeScript.JsonRpc;
internal readonly struct IDKey : IEquatable<IDKey>
{
    private readonly int number;
    private readonly ByteStringValue text;
    private IDKey(ByteStringValue text, int number)
    {
        this.number = number;
        this.text = text.Snapshot();
    }

    internal static IDKey FromID(ID id)
    {
        var(text, number) = id.MapKeyParts();
        return new(text, number);
    }

    public bool Equals(IDKey other) => number == other.number && (text.Length == 0 ? other.text.Length == 0 : text.EqualsValue(other.text));
    public override bool Equals(object? value) => value is IDKey key && Equals(key);
    public override int GetHashCode() => HashCode.Combine(number, text.Length == 0 ? 0 : ByteStringKey.FromValue(text).GetHashCode());
}
