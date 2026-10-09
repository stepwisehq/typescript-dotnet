// Copyright 2013 The Go Authors. All rights reserved.
// Use of this source code is governed by the BSD-style license in licenses/XText-LICENSE.txt.
// Modified Parse-reachable port of x/text v0.42.0 internal/language/parse.go.
// Managed string splices replace mutable byte-buffer moves without changing scanner offsets.
using TypeScript.Scanner;

namespace TypeScript.Locale.XText;
internal static partial class LanguageParser
{
    internal static readonly Exception ErrSyntax = new FormatException("language: tag is not well-formed");
    internal static readonly Exception ErrDuplicateKey = new FormatException("language: different values for same key in -u extension");
    internal sealed class ValueError : Exception
    {
        private readonly string subtag;
        internal ValueError(string bytes) : base("language: subtag " + GoQuote.Quote(bytes) + " is well-formed but unknown") => subtag = bytes;
        internal string Subtag() => subtag;
    }

    internal static Exception NewValueError(char[] token)
    {
        string bytes = new(token.Take(8).ToArray());
        int zero = bytes.IndexOf('\0');
        return new ValueError(zero >= 0 ? bytes[..zero] : bytes);
    }

    internal sealed class Scanner
    {
        internal string b;
        internal char[] token = [];
        internal int start, end, next;
        internal Exception? err;
        internal bool done;
        internal Scanner(string value)
        {
            b = value.Replace('_', '-');
            Scan();
        }

        internal void ToLower(int start, int end)
        {
            var chars = b.ToCharArray();
            for (int i = start; i < end; i++)
                if (chars[i] is >= 'A' and <= 'Z')
                    chars[i] += (char)('a' - 'A');
            b = new(chars);
            if (token.Length > 0 && this.start >= 0 && this.end <= b.Length)
                token = b[this.start..this.end].ToCharArray();
        }

        internal void SetError(Exception? error)
        {
            if (err == null || (ReferenceEquals(error, ErrSyntax) && !ReferenceEquals(err, ErrSyntax)))
                err = error;
        }

        internal void ResizeRange(int oldStart, int oldEnd, int newSize)
        {
            start = oldStart;
            int resizedEnd = oldStart + newSize;
            if (resizedEnd == oldEnd)
                return;
            b = b[..oldStart] + new string ('\0', newSize) + b[oldEnd..];
            next = resizedEnd + (next - end);
            end = resizedEnd;
        }

        internal void Replace(string replacement)
        {
            ResizeRange(start, end, replacement.Length);
            b = b[..start] + replacement + b[(start + replacement.Length)..];
        }

        internal void Gobble(Exception? error)
        {
            SetError(error);
            if (start == 0)
            {
                b = b[next..];
                end = 0;
            }
            else
            {
                b = b[..(start - 1)] + b[end..];
                end = start - 1;
            }

            next = start;
        }

        internal void DeleteRange(int first, int last)
        {
            b = b[..first] + b[last..];
            int diff = last - first;
            next -= diff;
            start -= diff;
            end -= diff;
        }

        internal int Scan()
        {
            int previousEnd = end;
            token = [];
            start = next;
            while (next < b.Length)
            {
                int separator = b.IndexOf('-', next);
                if (separator < 0)
                {
                    end = b.Length;
                    next = b.Length;
                }
                else
                {
                    end = separator;
                    next = end + 1;
                }

                int length = end - start;
                var value = b[start..end];
                if (length < 1 || length > 8 || !value.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9'))
                {
                    Gobble(ErrSyntax);
                    continue;
                }

                token = value.ToCharArray();
                return previousEnd;
            }

            if (b.Length > 0 && b[^1] == '-')
            {
                SetError(ErrSyntax);
                b = b[..^1];
            }

            done = true;
            return previousEnd;
        }

        internal int AcceptMinSize(int min)
        {
            int acceptedEnd = end;
            Scan();
            while (token.Length >= min)
            {
                acceptedEnd = end;
                Scan();
            }

            return acceptedEnd;
        }
    }

    internal static (Tag Tag, Exception? Error) Parse(string bytes)
    {
        if (bytes.Length == 0)
            return (new(), ErrSyntax);
        try
        {
            if (bytes.Length <= 11)
            {
                var key = bytes.ToCharArray();
                for (int i = 0; i < key.Length; i++)
                {
                    if (key[i] is >= 'A' and <= 'Z')
                        key[i] += (char)('a' - 'A');
                    else if (key[i] == '_')
                        key[i] = '-';
                }

                if (grandfatheredMap.TryGetValue(new(key), out string? replacement))
                    return Parse(replacement);
            }

            return Parse(new Scanner(bytes), bytes);
        }
        // Source Parse recovers parser panics and returns Und + ErrSyntax.
        catch (ArgumentOutOfRangeException)
        {
            return (new(), ErrSyntax);
        }
        catch (IndexOutOfRangeException)
        {
            return (new(), ErrSyntax);
        }
    }

    private static (Tag Tag, Exception? Error) Parse(Scanner scan, string original)
    {
        Tag t = new();
        int end;
        if (scan.token.Length <= 1)
        {
            scan.ToLower(0, scan.b.Length);
            if (scan.token.Length == 0 || scan.token[0] != 'x')
                return (t, ErrSyntax);
            end = ParseExtensions(scan);
        }
        else if (scan.token.Length >= 4)
            return (t, ErrSyntax);
        else
        {
            (t, end) = ParseTag(scan, true);
            if (scan.token.Length == 1)
            {
                t.pExt = unchecked((ushort)end);
                end = ParseExtensions(scan);
            }
            else if (end < scan.b.Length)
            {
                scan.SetError(ErrSyntax);
                scan.b = scan.b[..end];
            }
        }

        if (t.pVariant < scan.b.Length)
            t.str = scan.b;
        else
        {
            t.pVariant = 0;
            t.pExt = 0;
        }

        return (t, scan.err);
    }

    internal static (Tag Tag, int End) ParseTag(Scanner scan, bool doNorm)
    {
        Tag t = new();
        var(language, error) = GetLangID(scan.token);
        t.LangID = language;
        scan.SetError(error);
        scan.Replace(LanguageString(language));
        int langStart = scan.start, end = scan.Scan();
        while (scan.token.Length == 3 && scan.token[0] > '9')
        {
            if (doNorm)
            {
                (language, error) = GetLangID(scan.token);
                if (language != 0)
                {
                    t.LangID = language;
                    string text = LanguageString(language);
                    scan.b = scan.b[..langStart] + text + "-" + scan.b[(langStart + text.Length + 1)..];
                    scan.start = langStart + text.Length + 1;
                }

                scan.Gobble(error);
            }

            end = scan.Scan();
        }

        if (scan.token.Length == 4 && scan.token[0] > '9')
        {
            (t.ScriptID, error) = GetScriptID(scan.token);
            if (t.ScriptID == 0)
                scan.Gobble(error);
            else
                scan.Replace(new string (scan.token)); // findIndex fixes case through the original byte slice.
            end = scan.Scan();
        }

        if (scan.token.Length is >= 2 and <= 3)
        {
            (t.RegionID, error) = GetRegionID(scan.token);
            if (t.RegionID == 0)
                scan.Gobble(error);
            else
                scan.Replace(RegionString(t.RegionID));
            end = scan.Scan();
        }

        scan.ToLower(scan.start, scan.b.Length);
        // Token is a view into b in Go; refresh it after lowering the rest.
        if (scan.token.Length > 0)
            scan.token = scan.b[scan.start..scan.end].ToCharArray();
        t.pVariant = unchecked((byte)end);
        end = ParseVariants(scan, end, t);
        t.pExt = unchecked((ushort)end);
        return (t, end);
    }

    private static int ParseVariants(Scanner scan, int end, Tag t)
    {
        int start = scan.start, last = -1;
        bool needSort = false;
        var variants = new List<(int ID, string Text)>();
        while (scan.token.Length >= 4)
        {
            string text = new(scan.token);
            if (!variantIndex.TryGetValue(text, out int id))
            {
                scan.Gobble(NewValueError(scan.token));
                scan.Scan();
                continue;
            }

            variants.Add((id, text));
            if (!needSort)
            {
                if (last < id)
                    last = id;
                else
                {
                    needSort = true;
                    if (variants.Count > 8)
                        break;
                }
            }

            end = scan.end;
            scan.Scan();
        }

        if (needSort)
        {
            string sorted = string.Join('-', variants.OrderBy(v => v.ID).DistinctBy(v => v.ID).Select(v => v.Text));
            if (sorted.Length == 0)
                end = start - 1;
            else
            {
                scan.ResizeRange(start, end, sorted.Length);
                scan.b = scan.b[..scan.start] + sorted + scan.b[(scan.start + sorted.Length)..];
                end = scan.end;
            }
        }

        return end;
    }

    internal static int ParseExtensions(Scanner scan)
    {
        int start = scan.start, end = scan.end;
        var extensions = new List<string>();
        string privateUse = "";
        while (scan.token.Length == 1)
        {
            int extStart = scan.start;
            char ext = scan.token[0];
            end = ParseExtension(scan);
            string extension = scan.b[extStart..end];
            if (extension.Length < 3 || (ext != 'x' && extension.Length < 4))
            {
                scan.SetError(ErrSyntax);
                end = extStart;
                continue;
            }

            if (start == extStart && (ext == 'x' || scan.start == scan.b.Length))
            {
                scan.b = scan.b[..end];
                return end;
            }

            if (ext == 'x')
            {
                privateUse = extension;
                break;
            }

            extensions.Add(extension);
        }

        extensions.Sort((a, b) => a[0].CompareTo(b[0]));
        if (privateUse.Length > 0)
            extensions.Add(privateUse);
        scan.b = scan.b[..start];
        if (extensions.Count > 0)
            scan.b += string.Join('-', extensions);
        else if (start > 0)
            scan.b = scan.b[..(start - 1)];
        return end;
    }

    private static int ParseExtension(Scanner scan)
    {
        int start = scan.start, end = scan.end;
        switch (scan.token[0])
        {
            case 'u':
                int attrStart = end;
                scan.Scan();
                string lastAttribute = "";
                while (scan.token.Length > 2)
                {
                    string attribute = new(scan.token);
                    if (Math.Sign(string.CompareOrdinal(attribute, lastAttribute)) != -1)
                    {
                        int p = attrStart + 1;
                        scan.next = p;
                        var attrs = new List<string>();
                        for (scan.Scan(); scan.token.Length > 2; scan.Scan())
                        {
                            attrs.Add(new(scan.token));
                            end = scan.end;
                        }

                        attrs.Sort((a, b) => string.CompareOrdinal(a, 0, b, 0, 3));
                        string sorted = string.Join('-', attrs);
                        scan.b = scan.b[..p] + sorted + scan.b[(p + sorted.Length)..];
                        break;
                    }

                    lastAttribute = attribute;
                    end = scan.end;
                    scan.Scan();
                }

                int attrEnd = end;
                string lastKey = "";
                while (scan.token.Length == 2)
                {
                    string key = new(scan.token);
                    end = scan.end;
                    for (scan.Scan(); end < scan.end && scan.token.Length > 2; scan.Scan())
                        end = scan.end;
                    if (Math.Sign(string.CompareOrdinal(key, lastKey)) != 1 || scan.err != null)
                    {
                        int p = attrEnd + 1;
                        scan.next = p;
                        var keys = new List<string>();
                        scan.Scan();
                        while (scan.token.Length == 2)
                        {
                            int keyStart = scan.start;
                            end = scan.end;
                            for (scan.Scan(); end < scan.end && scan.token.Length > 2; scan.Scan())
                                end = scan.end;
                            keys.Add(scan.b[keyStart..end]);
                        }

                        keys = keys.OrderBy(k => k[..2], StringComparer.Ordinal).ToList();
                        var unique = new List<string>();
                        foreach (string entry in keys)
                        {
                            if (unique.Count == 0 || unique[^1][..2] != entry[..2])
                                unique.Add(entry);
                            else if (unique[^1] != entry)
                                scan.SetError(ErrDuplicateKey);
                        }

                        string sorted = string.Join('-', unique);
                        int resizedEnd = p + sorted.Length;
                        if (resizedEnd < end)
                        {
                            scan.DeleteRange(resizedEnd, end);
                            end = resizedEnd;
                        }

                        scan.b = scan.b[..p] + sorted + scan.b[(p + sorted.Length)..];
                        break;
                    }

                    lastKey = key;
                }

                break;
            case 't':
                scan.Scan();
                if (scan.token.Length is >= 2 and <= 3 && scan.token[1] > '9')
                {
                    (_, end) = ParseTag(scan, false);
                    scan.ToLower(start, end);
                }

                while (scan.token.Length == 2 && scan.token[1] <= '9')
                    end = scan.AcceptMinSize(3);
                break;
            case 'x':
                end = scan.AcceptMinSize(1);
                break;
            default:
                end = scan.AcceptMinSize(2);
                break;
        }

        return end;
    }
}
