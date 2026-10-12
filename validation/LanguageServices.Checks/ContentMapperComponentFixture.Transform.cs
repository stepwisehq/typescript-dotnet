// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete contentmappertest/component.go transformation/name/identifier workers.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using TypeScript.SpanMap;
using RawJsonValue = TypeScript.JsonRpc.RawJsonValue;
using MappingKind = TypeScript.SpanMap.Kind;

namespace LanguageServices.Checks;
internal static partial class ContentMapperComponentFixture
{
    private static (ByteStringValue Text, RawJsonValue? Mappings, Exception? Error) TransformComponent(ByteStringValue content)
    {
        var output = new GoBuilder();
        List<Segment>? segments = null;
        void WriteSynthesized(ByteStringValue text) => output.WriteString(text);
        void WriteMapped(ByteStringValue text, int originalStart, int originalEnd, MappingKind kind)
        {
            int virtualStart = output.Len();
            output.WriteString(text);
            (segments ??= new()).Add(new(virtualStart, output.Len(), originalStart, originalEnd, kind, Feature.All));
        }

        void WriteAnchored(ByteStringValue text, int originalPosition, Feature features)
        {
            int virtualStart = output.Len();
            output.WriteString(text);
            (segments ??= new()).Add(new(virtualStart, output.Len(), originalPosition, originalPosition, MappingKind.Atom, features));
        }

        int scriptOpen = content.IndexOf(ByteStringValue.FromString("<script"));
        if (scriptOpen >= 0)
        {
            int openEndRelative = content.Slice(scriptOpen).IndexOf((byte)'>');
            if (openEndRelative < 0)
                return (default, null, new Exception("contentmappertest: unclosed <script> tag"));
            int scriptStart = scriptOpen + openEndRelative + 1;
            int closeRelative = content.Slice(scriptStart).IndexOf(ByteStringValue.FromString("</script>"));
            if (closeRelative < 0)
                return (default, null, new Exception("contentmappertest: missing </script> tag"));
            int scriptEnd = scriptStart + closeRelative;
            WriteMapped(content.Slice(scriptStart, scriptEnd), scriptStart, scriptEnd, MappingKind.Verbatim);
        }

        WriteSynthesized(ByteStringValue.FromString("\nfunction __render() {\n"));
        for (int searchStart = 0; searchStart < content.Length;)
        {
            int openRelative = content.Slice(searchStart).IndexOf(ByteStringValue.FromString("{{"));
            if (openRelative < 0)
                break;
            int expressionStart = searchStart + openRelative + 2;
            int closeRelative = content.Slice(expressionStart).IndexOf(ByteStringValue.FromString("}}"));
            if (closeRelative < 0)
                return (default, null, new Exception("contentmappertest: unclosed template expression"));
            int expressionEnd = expressionStart + closeRelative;
            WriteSynthesized(ByteStringValue.FromString("  void ("));
            for (int position = expressionStart; position < expressionEnd;)
            {
                if (!IsIdentifierStart(content[position]))
                {
                    WriteSynthesized(content.Slice(position, position + 1));
                    position++;
                    continue;
                }

                int end = position + 1;
                while (end < expressionEnd && IsIdentifierPart(content[end]))
                    end++;
                WriteMapped(content.Slice(position, end), position, end, MappingKind.Atom);
                position = end;
            }

            WriteSynthesized(ByteStringValue.FromString(");\n"));
            searchStart = expressionEnd + 2;
        }

        WriteSynthesized(ByteStringValue.FromString("}\n"));
        var(nameStart, nameEnd, found) = ComponentNameRange(content);
        if (found)
        {
            WriteSynthesized(ByteStringValue.FromString("export class "));
            WriteMapped(content.Slice(nameStart, nameEnd), nameStart, nameEnd, MappingKind.Atom);
            WriteSynthesized(ByteStringValue.FromString(" {}\n"));
        }

        WriteAnchored(ByteStringValue.FromString("export default {};\n"), 0, Feature.Definition | Feature.References);
        var(mappings, error) = SpanMaps.New(segments).Marshal();
        if (error is not null)
            return (default, null, error);
        return (output.Value(), RawJsonValue.FromRetainedBytes(mappings!), null);
    }

    private static (int Start, int End, bool Found) ComponentNameRange(ByteStringValue content)
    {
        int componentStart = content.IndexOf(ByteStringValue.FromString("<component"));
        if (componentStart < 0)
            return (0, 0, false);
        int tagEndRelative = content.Slice(componentStart).IndexOf((byte)'>');
        if (tagEndRelative < 0)
            return (0, 0, false);
        var tag = content.Slice(componentStart, componentStart + tagEndRelative);
        int nameRelative = tag.IndexOf(ByteStringValue.FromString("name=\""));
        if (nameRelative < 0)
            return (0, 0, false);
        int start = componentStart + nameRelative + 6;
        int endRelative = content.Slice(start).IndexOf((byte)'"');
        return endRelative < 0 ? (0, 0, false) : (start, start + endRelative, true);
    }

    private static bool IsIdentifierStart(int ch) => ch is '_' or '$' || ch is >= 'A' and <= 'Z' || ch is >= 'a' and <= 'z';
    private static bool IsIdentifierPart(int ch) => IsIdentifierStart(ch) || ch is >= '0' and <= '9';
}
