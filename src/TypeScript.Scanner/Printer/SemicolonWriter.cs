// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/printer/semicolon_writer.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Single-line diagnostic writer methods; general emit position/line/indent APIs excluded.
namespace TypeScript.DiagnosticPrinting;
internal sealed class trailingSemicolonDeferringWriter : TextWriter
{
    private readonly TextWriter inner;
    private bool hasPendingSemicolon;
    private trailingSemicolonDeferringWriter(TextWriter writer) => inner = writer;
    internal static TextWriter getTrailingSemicolonDeferringWriter(TextWriter writer) => new trailingSemicolonDeferringWriter(writer);
    private void commitSemicolon()
    {
        if (hasPendingSemicolon)
        {
            inner.WriteTrailingSemicolon(";");
            hasPendingSemicolon = false;
        }
    }

    internal override void Write(string text)
    {
        commitSemicolon();
        inner.Write(text);
    }

    internal override void WriteTrailingSemicolon(string text) => hasPendingSemicolon = true;
    internal override void WriteComment(string text)
    {
        commitSemicolon();
        inner.WriteComment(text);
    }

    internal override void WriteKeyword(string text)
    {
        commitSemicolon();
        inner.WriteKeyword(text);
    }

    internal override void WritePunctuation(string text)
    {
        commitSemicolon();
        inner.WritePunctuation(text);
    }

    internal override void WriteSpace(string text)
    {
        commitSemicolon();
        inner.WriteSpace(text);
    }

    internal override void WriteStringLiteral(string text)
    {
        commitSemicolon();
        inner.WriteStringLiteral(text);
    }

    internal override void Clear()
    {
        hasPendingSemicolon = false;
        inner.Clear();
    }

    internal override string String() => inner.String();
}
