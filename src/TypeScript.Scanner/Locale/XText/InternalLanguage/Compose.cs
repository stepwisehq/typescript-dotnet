// Copyright 2018 The Go Authors. All rights reserved.
// Use of this source code is governed by licenses/XText-LICENSE.txt.
// Direct counterpart of x/text v0.42.0 internal/language/compose.go.
namespace TypeScript.Locale.XText;
internal static partial class LanguageParser
{
    internal sealed class TagBuilder
    {
        internal Tag Tag = new();
        private string private_ = "";
        private readonly List<string> variants = [], extensions = [];
        internal Tag Make()
        {
            Tag t = CopyTag(Tag);
            if (extensions.Count > 0 || variants.Count > 0)
            {
                variants.Sort(static (a, b) => variantIndex[a].CompareTo(variantIndex[b]));
                extensions.Sort(StringComparer.Ordinal);
                if (private_ != "")
                    extensions.Add(private_);
                int n = 12 + TokenLength(variants) + TokenLength(extensions);
                // All valid BCP47 components are ASCII. This is the same owned
                // byte buffer/genCoreBytes/appendTokens owner as the Go builder.
                byte[] buffer = new byte[n];
                string core = t.CoreString();
                int p = core.Length;
                for (int i = 0; i < p; i++)
                    buffer[i] = (byte)core[i];
                t.pVariant = unchecked((byte)p);
                p += AppendTokens(buffer.AsSpan(p), variants);
                t.pExt = unchecked((ushort)p);
                p += AppendTokens(buffer.AsSpan(p), extensions);
                t.str = System.Text.Encoding.Latin1.GetString(buffer.AsSpan(0, p));
                return Parse(t.str).Tag;
            }

            if (private_ != "")
            {
                t.str = private_;
                t.RemakeString();
            }

            return t;
        }

        internal void SetTag(Tag t)
        {
            Tag.LangID = t.LangID;
            Tag.RegionID = t.RegionID;
            Tag.ScriptID = t.ScriptID;
            variants.Clear();
            string value = Variants(t);
            if (value != "")
                foreach (string variant in value[1..].Split('-'))
                    variants.Add(variant);
            extensions.Clear();
            private_ = "";
            foreach (string extension in Extensions(t))
                AddExt(extension);
        }

        internal void AddExt(string extension)
        {
            if (extension[0] == 'x')
            {
                if (private_ == "")
                    private_ = extension;
                return;
            }

            for (int i = 0; i < extensions.Count; i++)
                if (extensions[i][0] == extension[0])
                {
                    if (extension[0] == 'u')
                        extensions[i] += extension[1..];
                    return;
                }

            extensions.Add(extension);
        }

        internal void SetExt(string extension)
        {
            if (extension[0] == 'x')
            {
                private_ = extension;
                return;
            }

            for (int i = 0; i < extensions.Count; i++)
                if (extensions[i][0] == extension[0])
                {
                    extensions[i] = extension[0] == 'u' ? extension + extensions[i][1..] : extension;
                    return;
                }

            extensions.Add(extension);
        }

        internal void AddVariant(params string[] values)
        {
            foreach (string value in values)
                if (value != "")
                    variants.Add(value);
        }

        internal void ClearVariants() => variants.Clear();
        internal void ClearExtensions()
        {
            private_ = "";
            extensions.Clear();
        }
    }

    private static int TokenLength(List<string> tokens)
    {
        int n = 0;
        foreach (string token in tokens)
            n += token.Length + 1;
        return n;
    }

    private static int AppendTokens(Span<byte> buffer, List<string> tokens)
    {
        int position = 0;
        foreach (string token in tokens)
        {
            buffer[position] = (byte)'-';
            for (int i = 0; i < token.Length; i++)
                buffer[position + 1 + i] = (byte)token[i];
            position += 1 + token.Length;
        }

        return position;
    }

    private static string Variants(Tag t) => t.pVariant == 0 ? "" : t.str[t.pVariant..t.pExt];
    private static string VariantOrPrivateUseTags(Tag t) => t.pExt > 0 ? t.str[t.pVariant..t.pExt] : t.str[t.pVariant..];
    internal static List<string> Extensions(Tag t)
    {
        List<string> result = [];
        for (int i = t.pExt; i < t.str.Length - 1;)
        {
            var(end, extension) = GetExtension(t.str, i);
            result.Add(extension);
            i = end;
        }

        return result;
    }

    private static (int End, string Extension) GetExtension(string value, int position)
    {
        if (value[position] == '-')
            position++;
        if (value[position] == 'x')
            return (value.Length, value[position..]);
        int end = NextExtension(value, position);
        return (end, value[position..end]);
    }
}
