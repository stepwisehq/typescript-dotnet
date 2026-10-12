// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/printer/textwriter.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
namespace TypeScript.DiagnosticPrinting;
internal class TextWriter : EmitTextWriter, GrowableEmitTextWriter
{
    protected string newLine;
    protected int indentSize;
    protected readonly GoBuilder builder = new();
    protected ByteStringValue lastWritten;
    protected int indent;
    protected bool lineStart;
    protected int lineCount, linePos;
    internal bool hasTrailingCommentState;
    internal static TextWriter NewTextWriter(string newLine, int indentSize)
    {
        var writer = new TextWriter { newLine = newLine, indentSize = indentSize <= 0 ? 4 : indentSize };
        writer.Clear();
        return writer;
    }
    public virtual void Clear()
    {
        builder.Reset(); lastWritten = default; indent = 0; lineStart = true;
        lineCount = 0; linePos = 0; hasTrailingCommentState = false;
    }
    public void Grow(int count) => builder.Grow(count);
    public virtual void DecreaseIndent() => indent--;
    public virtual int GetColumn()
    {
        if (lineStart) return indent * indentSize;
        var tail = builder.Value().Slice(linePos);
        return tail.IsBytes ? Core.UTF16Len(tail.ByteView) : Core.UTF16Len(tail.Materialize());
    }
    public virtual int GetIndent() => indent;
    public virtual int GetLine() => lineCount;
    public virtual string String() => builder.String();
    public virtual int GetTextPos() => builder.Len();
    public virtual bool HasTrailingComment() => hasTrailingCommentState;
    public virtual bool HasTrailingWhitespace()
    {
        if (builder.Len() == 0) return false;
        var (ch, _) = lastWritten.IsBytes ? Utf8.DecodeLastRuneInString(lastWritten.ByteSpan) : Utf8.DecodeLastRuneInString(lastWritten.CharSpan);
        return ch != Utf8.RuneError && StringUtil.IsWhiteSpaceLike(ch);
    }
    public virtual void IncreaseIndent() => indent++;
    public virtual bool IsAtStartOfLine() => lineStart;
    public virtual void RawWrite(string text) => RawWrite(ByteStringValue.FromString(text));
    public virtual void RawWrite(ByteStringValue text)
    {
        if (text.Length != 0) { builder.WriteString(text); lastWritten = text.Snapshot(); hasTrailingCommentState = false; }
        updateLineCountAndPosFor(text);
    }
    private void updateLineCountAndPosFor(ByteStringValue text)
    {
        int count = 0, lastLineStart = 0;
        var starts = Core.ComputeECMALineStartsValue(text);
        foreach (int lineStart in starts) { count++; lastLineStart = lineStart; }
        if (count > 1)
        {
            lineCount += count - 1;
            int currentLength = builder.Len();
            linePos = currentLength - text.Length + lastLineStart;
            this.lineStart = linePos == currentLength;
            return;
        }
        this.lineStart = false;
    }
    internal const int defaultIndentSize = 4;
    internal static int GetDefaultIndentSize() => defaultIndentSize;
    internal static string getIndentString(int indent, int indentSize) => indent == 0 ? "" : new string(' ', checked(indent * indentSize));
    protected void writeText(ByteStringValue text)
    {
        if (text.Length == 0) return;
        if (lineStart) { builder.WriteString(getIndentString(indent, indentSize)); lineStart = false; }
        builder.WriteString(text); lastWritten = text.Snapshot(); updateLineCountAndPosFor(text);
    }
    public virtual void Write(string text) => Write(ByteStringValue.FromString(text));
    public virtual void Write(ByteStringValue text) { if (text.Length != 0) hasTrailingCommentState = false; writeText(text); }
    public virtual void WriteComment(string text) => WriteComment(ByteStringValue.FromString(text));
    public virtual void WriteComment(ByteStringValue text)
    {
        if (text.Length != 0) hasTrailingCommentState = true;
        writeText(text);
    }
    private void writeLineRaw()
    {
        builder.WriteString(newLine); lastWritten = ByteStringValue.FromString(newLine);
        lineCount++; linePos = builder.Len(); lineStart = true; hasTrailingCommentState = false;
    }
    public virtual void WriteLine() { if (!lineStart) writeLineRaw(); }
    public virtual void WriteLineForce(bool force) { if (!lineStart || force) writeLineRaw(); }
    public virtual void WriteTrailingSemicolon(string text) => Write(text);
    public virtual void WriteKeyword(string text) => Write(text);
    public virtual void WriteOperator(string text) => Write(text);
    public virtual void WritePunctuation(string text) => Write(text);
    public virtual void WriteSpace(string text) => Write(text);
    public virtual void WriteStringLiteral(string text) => Write(text);
    public virtual void WriteParameter(string text) => Write(text);
    public virtual void WriteProperty(string text) => Write(text);
    public virtual void WriteSymbol(string text, Symbol symbol) => Write(text);
    public virtual void WriteKeyword(ByteStringValue text) => Write(text);
    public virtual void WriteOperator(ByteStringValue text) => Write(text);
    public virtual void WritePunctuation(ByteStringValue text) => Write(text);
    public virtual void WriteSpace(ByteStringValue text) => Write(text);
    public virtual void WriteStringLiteral(ByteStringValue text) => Write(text);
    public virtual void WriteParameter(ByteStringValue text) => Write(text);
    public virtual void WriteProperty(ByteStringValue text) => Write(text);
    public virtual void WriteSymbol(ByteStringValue text, Symbol symbol) => Write(text);
    public virtual void WriteLiteral(string text) => WriteLiteral(ByteStringValue.FromString(text));
    public virtual void WriteLiteral(ByteStringValue text) => Write(text);
}
