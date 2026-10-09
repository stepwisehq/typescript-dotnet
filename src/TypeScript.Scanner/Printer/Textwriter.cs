// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/printer/textwriter.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Only single-line scalar diagnostic writing; no public emit API.
using System.Text;

namespace TypeScript.DiagnosticPrinting;
internal class TextWriter
{
    private StringBuilder builder = new();
    private string lastWritten = "";
    private bool lineStart;
    internal bool hasTrailingCommentState;
    private int indent, indentSize;
    internal static TextWriter NewTextWriter(string newLine, int indentSize)
    {
        if (newLine != "")
            throw new NotSupportedException("Diagnostic printer newline option");
        var w = new TextWriter
        {
            indentSize = indentSize <= 0 ? 4 : indentSize
        };
        w.Clear();
        return w;
    }

    internal virtual void Clear()
    {
        builder.Clear();
        lastWritten = "";
        lineStart = true;
        indent = 0;
        hasTrailingCommentState = false;
    }

    internal virtual string String() => builder.ToString();
    private void writeText(string text)
    {
        if (text != "")
        {
            if (lineStart)
            {
                builder.Append(new string (' ', indent * indentSize));
                lineStart = false;
            }

            builder.Append(text);
            lastWritten = text;
        // Literal printer escapes all line separators; no physical newlines here.
        }
    }

    internal virtual void Write(string text)
    {
        if (text != "")
            hasTrailingCommentState = false;
        writeText(text);
    }

    // textwriter.go:150. Comment state is retained even though multiline output is excluded.
    internal virtual void WriteComment(string text)
    {
        if (text != "")
            hasTrailingCommentState = true;
        writeText(text);
    }

    internal virtual void WriteKeyword(string text) => Write(text);
    internal virtual void WriteOperator(string text) => Write(text);
    internal virtual void WriteStringLiteral(string text) => Write(text);
    // textwriter.go: token-category methods all forward to Write in this writer.
    internal virtual void WriteSpace(string text) => Write(text);
    internal virtual void WritePunctuation(string text) => Write(text);
    internal virtual void WriteTrailingSemicolon(string text) => Write(text);
}
