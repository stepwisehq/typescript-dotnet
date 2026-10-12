// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/lsp/lsproto/lsp.go scalar owners at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;

namespace TypeScript.Lsp.Protocol;

internal readonly partial struct DocumentUri(ByteStringValue value) : IEquatable<DocumentUri>
{
    internal readonly ByteStringValue Value = value;
    public bool Equals(DocumentUri other) => Value.EqualsValue(other.Value);
    public override bool Equals(object? other) => other is DocumentUri uri && Equals(uri);
    public override int GetHashCode() => ByteStringKey.FromValue(Value).GetHashCode();
    public static bool operator ==(DocumentUri left, DocumentUri right) => left.Equals(right);
    public static bool operator !=(DocumentUri left, DocumentUri right) => !left.Equals(right);
}

internal readonly partial struct URI(ByteStringValue value) : IEquatable<URI>
{
    internal readonly ByteStringValue Value = value;
    public bool Equals(URI other) => Value.EqualsValue(other.Value);
    public override bool Equals(object? other) => other is URI uri && Equals(uri);
    public override int GetHashCode() => ByteStringKey.FromValue(Value).GetHashCode();
    public static bool operator ==(URI left, URI right) => left.Equals(right);
    public static bool operator !=(URI left, URI right) => !left.Equals(right);
}

internal readonly partial struct Method(ByteStringValue value) : IEquatable<Method>
{
    internal readonly ByteStringValue Value = value;
    public bool Equals(Method other) => Value.EqualsValue(other.Value);
    public override bool Equals(object? other) => other is Method method && Equals(method);
    public override int GetHashCode() => ByteStringKey.FromValue(Value).GetHashCode();
    public static bool operator ==(Method left, Method right) => left.Equals(right);
    public static bool operator !=(Method left, Method right) => !left.Equals(right);
}

internal struct Null { }
internal struct NoParams { }
