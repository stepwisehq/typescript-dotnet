// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/printer/textwriter.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Only single-line scalar diagnostic writing; no public emit API.
using System.Text;

namespace TypeScript.DiagnosticPrinting;
internal sealed class TextWriter
{
    private StringBuilder builder = new();
    private string lastWritten = "";
    private bool lineStart;
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

    internal void Clear()
    {
        builder.Clear();
        lastWritten = "";
        lineStart = true;
        indent = 0;
    }

    internal string String() => builder.ToString();
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

    internal void Write(string text) => writeText(text);
    internal void WriteKeyword(string text) => Write(text);
    internal void WriteStringLiteral(string text) => Write(text);
    // textwriter.go: token-category methods all forward to Write in this writer.
    internal void WriteSpace(string text) => Write(text);
    internal void WritePunctuation(string text) => Write(text);
    internal void WriteTrailingSemicolon(string text) => Write(text);
}
