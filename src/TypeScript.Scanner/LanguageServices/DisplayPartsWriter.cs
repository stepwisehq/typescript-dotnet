// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/displaypartswriter.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using TypeScript.DiagnosticPrinting;

namespace TypeScript.LanguageServices;
internal sealed class DisplayPartsWriter : EmitTextWriter
{
    private readonly GoBuilder builder = new();
    private Slice<VSClassifiedTextRun?> runs;
    private readonly bool vsCapability;
    private ByteStringValue lastWritten;
    private DisplayPartsWriter(bool vsCapability)
    {
        this.vsCapability = vsCapability;
    }

    internal static DisplayPartsWriter NewDisplayPartsWriter(bool vsCapability) => new(vsCapability);
    // displaypartswriter.go:32. Classified runs retain the same immutable input headers.
    private void addRun(ClassificationTypeName classification, ByteStringValue text)
    {
        if (text.Length == 0)
            return;
        if (vsCapability)
            runs = runs.Append(new VSClassifiedTextRun { ClassificationTypeName = classification.Value, Text = text });
        lastWritten = text;
        builder.WriteString(text);
    }

    internal void WriteClassified(ByteStringValue text, ClassificationTypeName classification) => addRun(classification, text);
    internal void WriteFrom(DisplayPartsWriter other)
    {
        builder.WriteString(other.TextValue());
        if (vsCapability)
            runs = runs.Append(other.GetRuns());
        if (other.lastWritten.Length != 0)
            lastWritten = other.lastWritten;
    }

    internal Slice<VSClassifiedTextRun?> GetRuns() => runs;
    public string String() => builder.String();
    public ByteStringValue TextValue() => builder.Value();
    public void Clear()
    {
        lastWritten = default;
        builder.Reset();
        runs = default;
    }

    public void DecreaseIndent()
    {
    }

    public int GetColumn() => 0;
    public int GetIndent() => 0;
    public int GetLine() => 0;
    public int GetTextPos() => builder.Len();
    public bool HasTrailingComment() => false;
    public bool HasTrailingWhitespace()
    {
        if (builder.Len() == 0)
            return false;
        var (rune, _) = lastWritten.IsBytes ? Utf8.DecodeLastRuneInString(lastWritten.ByteSpan) : Utf8.DecodeLastRuneInString(lastWritten.CharSpan);
        return rune != 0xfffd && StringUtil.IsWhiteSpaceLike(rune);
    }

    public void IncreaseIndent()
    {
    }

    public bool IsAtStartOfLine() => false;
    public void WriteLine() => addRun(ClassificationTypeName.WhiteSpace, ByteStringValue.FromString(" "));
    public void WriteLineForce(bool force) => WriteLine();
    public void RawWrite(string text) => RawWrite(ByteStringValue.FromString(text));
    public void RawWrite(ByteStringValue text) => addRun(ClassificationTypeName.Text, text);
    public void Write(string text) => Write(ByteStringValue.FromString(text));
    public void Write(ByteStringValue text) => addRun(ClassificationTypeName.Text, text);
    public void WriteComment(string text) => WriteComment(ByteStringValue.FromString(text));
    public void WriteComment(ByteStringValue text) => addRun(ClassificationTypeName.Text, text);
    public void WriteKeyword(string text) => WriteKeyword(ByteStringValue.FromString(text));
    public void WriteKeyword(ByteStringValue text) => addRun(ClassificationTypeName.Keyword, text);
    public void WriteLiteral(string text) => WriteLiteral(ByteStringValue.FromString(text));
    public void WriteLiteral(ByteStringValue text) => addRun(ClassificationTypeName.String, text);
    public void WriteOperator(string text) => WriteOperator(ByteStringValue.FromString(text));
    public void WriteOperator(ByteStringValue text) => addRun(ClassificationTypeName.Operator, text);
    public void WriteParameter(string text) => WriteParameter(ByteStringValue.FromString(text));
    public void WriteParameter(ByteStringValue text) => addRun(ClassificationTypeName.ParameterName, text);
    public void WriteProperty(string text) => WriteProperty(ByteStringValue.FromString(text));
    public void WriteProperty(ByteStringValue text) => addRun(ClassificationTypeName.PropertyName, text);
    public void WritePunctuation(string text) => WritePunctuation(ByteStringValue.FromString(text));
    public void WritePunctuation(ByteStringValue text) => addRun(ClassificationTypeName.Punctuation, text);
    public void WriteSpace(string text) => WriteSpace(ByteStringValue.FromString(text));
    public void WriteSpace(ByteStringValue text) => addRun(ClassificationTypeName.WhiteSpace, text);
    public void WriteStringLiteral(string text) => WriteStringLiteral(ByteStringValue.FromString(text));
    public void WriteStringLiteral(ByteStringValue text) => addRun(ClassificationTypeName.String, text);
    public void WriteTrailingSemicolon(string text) => WriteTrailingSemicolon(ByteStringValue.FromString(text));
    public void WriteTrailingSemicolon(ByteStringValue text) => addRun(ClassificationTypeName.Punctuation, text);
    public void WriteSymbol(string text, Symbol symbol) => WriteSymbol(ByteStringValue.FromString(text), symbol);
    public void WriteSymbol(ByteStringValue text, Symbol symbol) => addRun(classificationForSymbol(symbol), text);
    // displaypartswriter.go:173. Preserve the original ordered flag priority.
    private static ClassificationTypeName classificationForSymbol(Symbol? symbol)
    {
        if (symbol == null)
            return ClassificationTypeName.Text;
        var flags = symbol.Flags;
        if ((flags & SymbolFlags.Variable) != 0)
            return isFirstDeclarationOfSymbolParameter(symbol) ? ClassificationTypeName.ParameterName : ClassificationTypeName.LocalName;
        if ((flags & SymbolFlags.Property) != 0)
            return ClassificationTypeName.PropertyName;
        if ((flags & SymbolFlags.GetAccessor) != 0)
            return ClassificationTypeName.PropertyName;
        if ((flags & SymbolFlags.SetAccessor) != 0)
            return ClassificationTypeName.PropertyName;
        if ((flags & SymbolFlags.EnumMember) != 0)
            return ClassificationTypeName.FieldName;
        if ((flags & SymbolFlags.Function) != 0)
            return ClassificationTypeName.MethodName;
        if ((flags & SymbolFlags.Class) != 0)
            return ClassificationTypeName.ClassName;
        if ((flags & SymbolFlags.Interface) != 0)
            return ClassificationTypeName.InterfaceName;
        if ((flags & SymbolFlags.Enum) != 0)
            return ClassificationTypeName.EnumName;
        if ((flags & SymbolFlags.Module) != 0)
            return ClassificationTypeName.ModuleName;
        if ((flags & SymbolFlags.Method) != 0)
            return ClassificationTypeName.MethodName;
        if ((flags & SymbolFlags.TypeParameter) != 0)
            return ClassificationTypeName.TypeParameterName;
        if ((flags & SymbolFlags.TypeAlias) != 0)
            return ClassificationTypeName.Identifier;
        if ((flags & SymbolFlags.Alias) != 0)
            return ClassificationTypeName.Identifier;
        return ClassificationTypeName.Text;
    }

    // displaypartswriter.go:211.
    private static bool isFirstDeclarationOfSymbolParameter(Symbol symbol) => symbol.Declarations.Count != 0 && symbol.Declarations[0].Kind == Kind.Parameter;
}
