// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/printer/semicolon_writer.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
namespace TypeScript.DiagnosticPrinting;
internal sealed class trailingSemicolonDeferringWriter : EmitTextWriter
{
    private readonly EmitTextWriter inner;
    private bool hasPendingSemicolon;
    private trailingSemicolonDeferringWriter(EmitTextWriter writer) => inner = writer;
    internal static EmitTextWriter getTrailingSemicolonDeferringWriter(EmitTextWriter writer) => new trailingSemicolonDeferringWriter(writer);
    private void commitSemicolon()
    {
        if (hasPendingSemicolon) { inner.WriteTrailingSemicolon(";"); hasPendingSemicolon = false; }
    }
    public void Write(string text) { commitSemicolon(); inner.Write(text); }
    public void WriteComment(string text) { commitSemicolon(); inner.WriteComment(text); }
    public void WriteKeyword(string text) { commitSemicolon(); inner.WriteKeyword(text); }
    public void WriteOperator(string text) { commitSemicolon(); inner.WriteOperator(text); }
    public void WritePunctuation(string text) { commitSemicolon(); inner.WritePunctuation(text); }
    public void WriteSpace(string text) { commitSemicolon(); inner.WriteSpace(text); }
    public void WriteStringLiteral(string text) { commitSemicolon(); inner.WriteStringLiteral(text); }
    public void WriteParameter(string text) { commitSemicolon(); inner.WriteParameter(text); }
    public void WriteProperty(string text) { commitSemicolon(); inner.WriteProperty(text); }
    public void WriteSymbol(string text, Symbol symbol) { commitSemicolon(); inner.WriteSymbol(text, symbol); }
    public void WriteLine() { commitSemicolon(); inner.WriteLine(); }
    public void WriteLineForce(bool force) { commitSemicolon(); inner.WriteLineForce(force); }
    public void RawWrite(string text) { commitSemicolon(); inner.RawWrite(text); }
    public void WriteLiteral(string text) { commitSemicolon(); inner.WriteLiteral(text); }
    public void WriteTrailingSemicolon(string text) => hasPendingSemicolon = true;
    public void Clear() { hasPendingSemicolon = false; inner.Clear(); }
    public void IncreaseIndent() { commitSemicolon(); inner.IncreaseIndent(); }
    public void DecreaseIndent() { commitSemicolon(); inner.DecreaseIndent(); }
    public void Write(ByteStringValue text) { commitSemicolon(); inner.Write(text); }
    public void WriteLiteral(ByteStringValue text) { commitSemicolon(); inner.WriteLiteral(text); }
    public void RawWrite(ByteStringValue text) { commitSemicolon(); inner.RawWrite(text); }
    public void WriteComment(ByteStringValue text) { commitSemicolon(); inner.WriteComment(text); }
    public void WriteKeyword(ByteStringValue text) { commitSemicolon(); inner.WriteKeyword(text); }
    public void WriteOperator(ByteStringValue text) { commitSemicolon(); inner.WriteOperator(text); }
    public void WritePunctuation(ByteStringValue text) { commitSemicolon(); inner.WritePunctuation(text); }
    public void WriteSpace(ByteStringValue text) { commitSemicolon(); inner.WriteSpace(text); }
    public void WriteStringLiteral(ByteStringValue text) { commitSemicolon(); inner.WriteStringLiteral(text); }
    public void WriteParameter(ByteStringValue text) { commitSemicolon(); inner.WriteParameter(text); }
    public void WriteProperty(ByteStringValue text) { commitSemicolon(); inner.WriteProperty(text); }
    public void WriteSymbol(ByteStringValue text, Symbol symbol) { commitSemicolon(); inner.WriteSymbol(text, symbol); }
    public string String() => inner.String();
    public ByteStringValue TextValue() => inner.TextValue();
    public int GetTextPos() => inner.GetTextPos();
    public int GetLine() => inner.GetLine();
    public int GetColumn() => inner.GetColumn();
    public int GetIndent() => inner.GetIndent();
    public bool IsAtStartOfLine() => inner.IsAtStartOfLine();
    public bool HasTrailingComment() => inner.HasTrailingComment();
    public bool HasTrailingWhitespace() => inner.HasTrailingWhitespace();
}
