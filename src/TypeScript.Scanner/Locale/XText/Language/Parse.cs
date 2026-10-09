// Copyright 2013 The Go Authors. All rights reserved.
// Use of this source code is governed by the BSD-style license in licenses/XText-LICENSE.txt.
// Modified Default.Parse/canonicalize selected port of x/text v0.42.0 language/parse.go.
namespace TypeScript.Locale.XText;
internal static class PublicLanguage
{
    internal static (string Tag, Exception? Error) Parse(string bytes)
    {
        try
        {
            var(tag, error) = LanguageParser.Parse(bytes);
            // Default.Parse intentionally does NOT canonicalize a partial tag on error.
            if (error == null && LanguageParser.CanonicalizeDefault(tag))
                tag.RemakeString();
            return (LanguageParser.CompactString(tag), error);
        }
        catch (ArgumentOutOfRangeException)
        {
            return ("und", LanguageParser.ErrSyntax);
        }
        catch (IndexOutOfRangeException)
        {
            return ("und", LanguageParser.ErrSyntax);
        }
    }
}
