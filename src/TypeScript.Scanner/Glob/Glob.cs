// Copyright 2023 The Go Authors. All rights reserved.
// Licensed under the BSD-style license in licenses/Go-LICENSE.txt.
// Modified C# port of tsc/internal/glob/glob.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using System.Buffers;

namespace TypeScript.Glob;
/// <summary>The original LSP glob algorithm, operating on Go byte strings.</summary>
public sealed class Glob
{
    private abstract record Element
    {
        internal abstract string String();
    }

    private sealed record Slash : Element
    {
        internal override string String() => "/";
    }

    private sealed record Literal(ByteStringValue Text) : Element
    {
        internal override string String() => Text.Materialize()!;
    }

    private sealed record Star : Element
    {
        internal override string String() => "*";
    }

    private sealed record AnyChar : Element
    {
        internal override string String() => "?";
    }

    private sealed record StarStar : Element
    {
        internal override string String() => "**";
    }

    private sealed record Group(Slice<Glob> Members) : Element
    {
        internal override string String()
        {
            var parts = new List<string>(Members.Length);
            foreach (var member in Members) parts.Add(member.String());
            return "{" + string.Join(",", parts) + "}";
        }
    }

    private sealed record CharRange(bool Negate, int Low, int High) : Element
    {
        // The source String and Match deliberately do not consult Negate.
        internal override string String() => "[" + runeString(Low) + "-" + runeString(High) + "]";
    }

    private Slice<Element> elems;
    private static readonly SearchValues<byte> specialBytes = SearchValues.Create("*?{[/"u8);
    private static readonly SearchValues<byte> nestedSpecialBytes = SearchValues.Create("*?{[/},"u8);
    private static readonly SearchValues<char> specialChars = SearchValues.Create("*?{[/");
    private static readonly SearchValues<char> nestedSpecialChars = SearchValues.Create("*?{[/},");
    private static readonly Exception errBadRange = new InvalidOperationException("'[' patterns must be of the form [x-y]");
    private static readonly Exception errInvalidUTF8 = new InvalidOperationException("invalid UTF-8 encoding");
    public static (Glob? Glob, Exception? Error) Parse(string pattern) => Parse(ByteStringValue.FromString(pattern));
    public static (Glob? Glob, Exception? Error) Parse(ByteStringValue pattern)
    {
        var(glob, _, error) = parse(pattern, false);
        return (glob, error);
    }

    private static (Glob? Glob, ByteStringValue Remaining, Exception? Error) parse(ByteStringValue pattern, bool nested)
    {
        if (pattern.IsNull) throw new NullReferenceException();
        var glob = new Glob();
        while (pattern.Length > 0)
        {
            switch (pattern[0])
            {
                case '/':
                    pattern = pattern.Slice(1);
                    glob.elems = glob.elems.Append(new Slash());
                    break;
                case '*':
                    if (pattern.Length > 1 && pattern[1] == '*')
                    {
                        if (glob.elems.Length > 0 && glob.elems[glob.elems.Length - 1] is not Slash || pattern.Length > 2 && pattern[2] != '/')
                            return (null, default, new InvalidOperationException("** may only be adjacent to '/'"));
                        pattern = pattern.Slice(2);
                        glob.elems = glob.elems.Append(new StarStar());
                        break;
                    }

                    pattern = pattern.Slice(1);
                    glob.elems = glob.elems.Append(new Star());
                    break;
                case '?':
                    pattern = pattern.Slice(1);
                    glob.elems = glob.elems.Append(new AnyChar());
                    break;
                case '{':
                    Slice<Glob> members = default;
                    while (pattern[0] != '}')
                    {
                        pattern = pattern.Slice(1);
                        var(member, remaining, error) = parse(pattern, true);
                        if (error != null)
                            return (null, default, error);
                        if (remaining.Length == 0)
                            return (null, default, new InvalidOperationException("unmatched '{'"));
                        pattern = remaining;
                        members = members.Append(member!);
                    }

                    pattern = pattern.Slice(1);
                    glob.elems = glob.elems.Append(new Group(members));
                    break;
                case '}' or ',':
                    if (nested)
                        return (glob, pattern, null);
                    pattern = glob.parseLiteral(pattern, false);
                    break;
                case '[':
                    pattern = pattern.Slice(1);
                    if (pattern.Length == 0)
                        return (null, default, errBadRange);
                    bool negate = false;
                    if (pattern[0] == '!')
                    {
                        pattern = pattern.Slice(1);
                        negate = true;
                    }

                    var(low, size, lowError) = readRangeRune(pattern);
                    if (lowError != null)
                        return (null, default, lowError);
                    pattern = pattern.Slice(size);
                    if (pattern.Length == 0 || pattern[0] != '-')
                        return (null, default, errBadRange);
                    pattern = pattern.Slice(1);
                    var(high, highSize, highError) = readRangeRune(pattern);
                    if (highError != null)
                        return (null, default, highError);
                    pattern = pattern.Slice(highSize);
                    if (pattern.Length == 0 || pattern[0] != ']')
                        return (null, default, errBadRange);
                    pattern = pattern.Slice(1);
                    glob.elems = glob.elems.Append(new CharRange(negate, low, high));
                    break;
                default:
                    pattern = glob.parseLiteral(pattern, nested);
                    break;
            }
        }

        return (glob, default, null);
    }

    private static (int Rune, int Size, Exception? Error) readRangeRune(ByteStringValue input)
    {
        var(rune, size) = decodeRune(input);
        Exception? error = rune == Utf8.RuneError ? size == 0 ? errBadRange : size == 1 ? errInvalidUTF8 : null : null;
        return (rune, size, error);
    }

    private ByteStringValue parseLiteral(ByteStringValue pattern, bool nested)
    {
        int end = pattern.IsBytes ? pattern.ByteSpan.IndexOfAny(nested ? nestedSpecialBytes : specialBytes)
            : pattern.CharSpan.IndexOfAny(nested ? nestedSpecialChars : specialChars);
        if (end < 0)
            end = pattern.Length;
        elems = elems.Append(new Literal(pattern.Slice(0, end)));
        return pattern.Slice(end);
    }

    public string String()
    {
        // Public formatting owns its managed string result, including legacy
        // raw-string callers; matching never enters this presentation boundary.
        var builder = new System.Text.StringBuilder();
        foreach (var element in elems) builder.Append(element.String());
        return builder.ToString();
    }
    public override string ToString() => String();
    public bool Match(string input) => Match(ByteStringValue.FromString(input));
    public bool Match(ByteStringValue input) => match(elems, input);
    // Preserve the existing public null failure exactly where the source reads length.
    private static int length(ByteStringValue input) => input.IsNull ? throw new NullReferenceException() : input.Length;
    private static (int Rune, int Size) decodeRune(ByteStringValue input) => input.IsBytes
        ? Utf8.DecodeRuneInString(input.ByteSpan) : Utf8.DecodeRuneInString(input.CharSpan);
    private static bool match(Slice<Element> elements, ByteStringValue input)
    {
        int index = 0;
        while (index < elements.Length)
        {
            var element = elements[index++];
            switch (element)
            {
                case Slash:
                    if (length(input) == 0 || input[0] != '/')
                        return false;
                    while (input[0] == '/')
                        input = input.Slice(1);
                    break;
                case StarStar:
                    if (index < elements.Length)
                        index++;
                    if (index == elements.Length)
                        return true;
                    while (length(input) != 0)
                    {
                        if (match(elements.Slice2(index, elements.Length), input))
                            return true;
                        (_, input) = split(input);
                    }

                    return false;
                case Literal literal:
                    if (!input.StartsWith(literal.Text))
                        return false;
                    input = input.Slice(literal.Text.Length);
                    break;
                case Star:
                    var(segment, remaining) = split(input);
                    input = remaining;
                    int elementEnd = elements.Length;
                    for (int i = index; i < elements.Length; i++)
                        if (elements[i] is Slash)
                        {
                            elementEnd = i;
                            break;
                        }

                    var segmentElements = elements.Slice2(index, elementEnd);
                    index = elementEnd;
                    if (segmentElements.Length == 0)
                        break;
                    bool matched = false;
                    for (int i = 0; i < segment.Length; i++)
                        if (match(segmentElements, segment.Slice(i)))
                        {
                            matched = true;
                            break;
                        }

                    if (!matched)
                        return false;
                    break;
                case AnyChar:
                    if (length(input) == 0 || input[0] == '/')
                        return false;
                    input = input.Slice(1);
                    break;
                case Group group:
                    Slice<Element> branch = default;
                    foreach (var member in group.Members)
                    {
                        branch = branch.Slice2(0, 0);
                        branch = branch.Append(member.elems).Append(elements.Slice2(index, elements.Length));
                        if (match(branch, input))
                            return true;
                    }

                    return false;
                case CharRange range:
                    if (length(input) == 0 || input[0] == '/')
                        return false;
                    var(rune, size) = decodeRune(input);
                    if (rune < range.Low || rune > range.High)
                        return false;
                    input = input.Slice(size);
                    break;
                default:
                    throw new InvalidOperationException("segment type " + element.GetType().Name + " not implemented");
            }
        }

        return length(input) == 0;
    }

    private static (ByteStringValue First, ByteStringValue Remaining) split(ByteStringValue input)
    {
        int index = input.IndexOf('/');
        if (index < 0)
            return (input, default);
        var first = input.Slice(0, index);
        for (int i = index; i < length(input); i++)
            if (input[i] != '/')
                return (first, input.Slice(i));
        return (first, default);
    }

    private static string runeString(int rune)
    {
        var builder = new GoBuilder();
        builder.WriteRune(rune);
        return builder.String();
    }
}
