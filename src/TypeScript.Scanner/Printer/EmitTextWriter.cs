// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/printer/emittextwriter.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
namespace TypeScript.DiagnosticPrinting;
internal interface EmitTextWriter
{
    void Write(string text);
    void WriteTrailingSemicolon(string text);
    void WriteComment(string text);
    void WriteKeyword(string text);
    void WriteOperator(string text);
    void WritePunctuation(string text);
    void WriteSpace(string text);
    void WriteStringLiteral(string text);
    void WriteParameter(string text);
    void WriteProperty(string text);
    void WriteSymbol(string text, Symbol symbol);
    void WriteLine();
    void WriteLineForce(bool force);
    void IncreaseIndent();
    void DecreaseIndent();
    void Clear();
    void RawWrite(string text);
    void WriteLiteral(string text);
    string String();
    int GetTextPos();
    int GetLine();
    int GetColumn();
    int GetIndent();
    bool IsAtStartOfLine();
    bool HasTrailingComment();
    bool HasTrailingWhitespace();
    // Byte-role counterparts preserve borrowed text through writer consumers.
    void Write(ByteStringValue text);
    void WriteComment(ByteStringValue text);
    void WriteKeyword(ByteStringValue text);
    void WriteOperator(ByteStringValue text);
    void WritePunctuation(ByteStringValue text);
    void WriteSpace(ByteStringValue text);
    void WriteStringLiteral(ByteStringValue text);
    void WriteParameter(ByteStringValue text);
    void WriteProperty(ByteStringValue text);
    void WriteSymbol(ByteStringValue text, Symbol symbol);
    void WriteLiteral(ByteStringValue text);
    void RawWrite(ByteStringValue text);
}
internal interface GrowableEmitTextWriter { void Grow(int count); }
