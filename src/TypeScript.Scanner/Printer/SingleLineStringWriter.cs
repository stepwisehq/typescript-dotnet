// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/printer/singlelinestringwriter.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
namespace TypeScript.DiagnosticPrinting;
internal sealed class singleLineStringWriter : EmitTextWriter
{
    private static readonly System.Collections.Concurrent.ConcurrentBag<singleLineStringWriter> pool = new();
    private readonly GoBuilder builder = new();
    private ByteStringValue lastWritten;
    internal static (EmitTextWriter Writer, Action Release) GetSingleLineStringWriter()
    {
        if (!pool.TryTake(out var writer)) writer = new();
        writer.Clear();
        return (writer, () => pool.Add(writer));
    }
    public void Clear() { lastWritten = default; builder.Reset(); }
    public void IncreaseIndent() { }
    public void DecreaseIndent() { }
    public int GetColumn() => 0;
    public int GetIndent() => 0;
    public int GetLine() => 0;
    public void WriteComment(ByteStringValue text) => Write(text);
    public void WriteKeyword(ByteStringValue text) => Write(text);
    public void WriteOperator(ByteStringValue text) => Write(text);
    public void WritePunctuation(ByteStringValue text) => Write(text);
    public void WriteSpace(ByteStringValue text) => Write(text);
    public void WriteStringLiteral(ByteStringValue text) => Write(text);
    public void WriteParameter(ByteStringValue text) => Write(text);
    public void WriteProperty(ByteStringValue text) => Write(text);
    public void WriteSymbol(ByteStringValue text, Symbol symbol) => Write(text);
    public string String() => builder.String();
    public ByteStringValue TextValue() => builder.Value();
    public int GetTextPos() => builder.Len();
    public bool HasTrailingComment() => false;
    public bool HasTrailingWhitespace()
    {
        if (builder.Len() == 0) return false;
        var (ch, _) = lastWritten.IsBytes ? Utf8.DecodeLastRuneInString(lastWritten.ByteSpan) : Utf8.DecodeLastRuneInString(lastWritten.CharSpan);
        return ch != Utf8.RuneError && StringUtil.IsWhiteSpaceLike(ch);
    }
    public bool IsAtStartOfLine() => false;
    public void Write(ByteStringValue text) { lastWritten = text.Snapshot(); builder.WriteString(text); }
    public void RawWrite(ByteStringValue text) => Write(text);
    public void WriteLiteral(ByteStringValue text) => Write(text);
    public void Write(string text) => Write(ByteStringValue.FromString(text));
    public void WriteTrailingSemicolon(string text) => Write(ByteStringValue.FromString(text));
    public void WriteComment(string text) => Write(ByteStringValue.FromString(text));
    public void WriteKeyword(string text) => Write(ByteStringValue.FromString(text));
    public void WriteOperator(string text) => Write(ByteStringValue.FromString(text));
    public void WritePunctuation(string text) => Write(ByteStringValue.FromString(text));
    public void WriteSpace(string text) => Write(ByteStringValue.FromString(text));
    public void WriteStringLiteral(string text) => Write(ByteStringValue.FromString(text));
    public void WriteParameter(string text) => Write(ByteStringValue.FromString(text));
    public void WriteProperty(string text) => Write(ByteStringValue.FromString(text));
    public void WriteSymbol(string text, Symbol symbol) => Write(ByteStringValue.FromString(text));
    public void RawWrite(string text) => Write(ByteStringValue.FromString(text));
    public void WriteLiteral(string text) => Write(ByteStringValue.FromString(text));
    public void WriteLine() => Write(" ");
    public void WriteLineForce(bool force) => Write(" ");
}
