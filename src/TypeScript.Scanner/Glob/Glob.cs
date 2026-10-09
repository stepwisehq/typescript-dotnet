// Copyright 2023 The Go Authors. All rights reserved.
// Licensed under the BSD-style license in licenses/Go-LICENSE.txt.
// Modified C# port of tsc/internal/glob/glob.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;

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

    private sealed record Literal(string Text) : Element
    {
        internal override string String() => Text;
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

    private sealed record Group(List<Glob> Members) : Element
    {
        internal override string String() => "{" + string.Join(",", Members.Select(member => member.String())) + "}";
    }

    private sealed record CharRange(bool Negate, int Low, int High) : Element
    {
        // The source String and Match deliberately do not consult Negate.
        internal override string String() => "[" + runeString(Low) + "-" + runeString(High) + "]";
    }

    private readonly List<Element> elems = new();
    private static readonly Exception errBadRange = new InvalidOperationException("'[' patterns must be of the form [x-y]");
    private static readonly Exception errInvalidUTF8 = new InvalidOperationException("invalid UTF-8 encoding");
    public static (Glob? Glob, Exception? Error) Parse(string pattern)
    {
        var(glob, _, error) = parse(pattern, false);
        return (glob, error);
    }

    private static (Glob? Glob, string Remaining, Exception? Error) parse(string pattern, bool nested)
    {
        var glob = new Glob();
        while (pattern.Length > 0)
        {
            switch (pattern[0])
            {
                case '/':
                    pattern = pattern[1..];
                    glob.elems.Add(new Slash());
                    break;
                case '*':
                    if (pattern.Length > 1 && pattern[1] == '*')
                    {
                        if (glob.elems.Count > 0 && glob.elems[^1] is not Slash || pattern.Length > 2 && pattern[2] != '/')
                            return (null, "", new InvalidOperationException("** may only be adjacent to '/'"));
                        pattern = pattern[2..];
                        glob.elems.Add(new StarStar());
                        break;
                    }

                    pattern = pattern[1..];
                    glob.elems.Add(new Star());
                    break;
                case '?':
                    pattern = pattern[1..];
                    glob.elems.Add(new AnyChar());
                    break;
                case '{':
                    var members = new List<Glob>();
                    while (pattern[0] != '}')
                    {
                        pattern = pattern[1..];
                        var(member, remaining, error) = parse(pattern, true);
                        if (error != null)
                            return (null, "", error);
                        if (remaining.Length == 0)
                            return (null, "", new InvalidOperationException("unmatched '{'"));
                        pattern = remaining;
                        members.Add(member!);
                    }

                    pattern = pattern[1..];
                    glob.elems.Add(new Group(members));
                    break;
                case '}' or ',':
                    if (nested)
                        return (glob, pattern, null);
                    pattern = glob.parseLiteral(pattern, false);
                    break;
                case '[':
                    pattern = pattern[1..];
                    if (pattern.Length == 0)
                        return (null, "", errBadRange);
                    bool negate = false;
                    if (pattern[0] == '!')
                    {
                        pattern = pattern[1..];
                        negate = true;
                    }

                    var(low, size, lowError) = readRangeRune(pattern);
                    if (lowError != null)
                        return (null, "", lowError);
                    pattern = pattern[size..];
                    if (pattern.Length == 0 || pattern[0] != '-')
                        return (null, "", errBadRange);
                    pattern = pattern[1..];
                    var(high, highSize, highError) = readRangeRune(pattern);
                    if (highError != null)
                        return (null, "", highError);
                    pattern = pattern[highSize..];
                    if (pattern.Length == 0 || pattern[0] != ']')
                        return (null, "", errBadRange);
                    pattern = pattern[1..];
                    glob.elems.Add(new CharRange(negate, low, high));
                    break;
                default:
                    pattern = glob.parseLiteral(pattern, nested);
                    break;
            }
        }

        return (glob, "", null);
    }

    private static (int Rune, int Size, Exception? Error) readRangeRune(string input)
    {
        var(rune, size) = Utf8.DecodeRuneInString(input);
        Exception? error = rune == Utf8.RuneError ? size == 0 ? errBadRange : size == 1 ? errInvalidUTF8 : null : null;
        return (rune, size, error);
    }

    private string parseLiteral(string pattern, bool nested)
    {
        string specialChars = nested ? "*?{[/}," : "*?{[/";
        int end = pattern.IndexOfAny(specialChars.ToCharArray());
        if (end < 0)
            end = pattern.Length;
        elems.Add(new Literal(pattern[..end]));
        return pattern[end..];
    }

    public string String() => string.Concat(elems.Select(element => element.String()));
    public override string ToString() => String();
    public bool Match(string input) => match(elems, input);
    private static bool match(List<Element> elements, string input)
    {
        int index = 0;
        while (index < elements.Count)
        {
            var element = elements[index++];
            switch (element)
            {
                case Slash:
                    if (input.Length == 0 || input[0] != '/')
                        return false;
                    while (input[0] == '/')
                        input = input[1..];
                    break;
                case StarStar:
                    if (index < elements.Count)
                        index++;
                    if (index == elements.Count)
                        return true;
                    while (input.Length != 0)
                    {
                        if (match(elements.GetRange(index, elements.Count - index), input))
                            return true;
                        (_, input) = split(input);
                    }

                    return false;
                case Literal literal:
                    if (!input.StartsWith(literal.Text, StringComparison.Ordinal))
                        return false;
                    input = input[literal.Text.Length..];
                    break;
                case Star:
                    var(segment, remaining) = split(input);
                    input = remaining;
                    int elementEnd = elements.Count;
                    for (int i = index; i < elements.Count; i++)
                        if (elements[i] is Slash)
                        {
                            elementEnd = i;
                            break;
                        }

                    var segmentElements = elements.GetRange(index, elementEnd - index);
                    index = elementEnd;
                    if (segmentElements.Count == 0)
                        break;
                    bool matched = false;
                    for (int i = 0; i < segment.Length; i++)
                        if (match(segmentElements, segment[i..]))
                        {
                            matched = true;
                            break;
                        }

                    if (!matched)
                        return false;
                    break;
                case AnyChar:
                    if (input.Length == 0 || input[0] == '/')
                        return false;
                    input = input[1..];
                    break;
                case Group group:
                    foreach (var member in group.Members)
                    {
                        var branch = new List<Element>(member.elems);
                        branch.AddRange(elements.GetRange(index, elements.Count - index));
                        if (match(branch, input))
                            return true;
                    }

                    return false;
                case CharRange range:
                    if (input.Length == 0 || input[0] == '/')
                        return false;
                    var(rune, size) = Utf8.DecodeRuneInString(input);
                    if (rune < range.Low || rune > range.High)
                        return false;
                    input = input[size..];
                    break;
                default:
                    throw new InvalidOperationException("segment type " + element.GetType().Name + " not implemented");
            }
        }

        return input.Length == 0;
    }

    private static (string First, string Remaining) split(string input)
    {
        int index = input.IndexOf('/');
        if (index < 0)
            return (input, "");
        string first = input[..index];
        for (int i = index; i < input.Length; i++)
            if (input[i] != '/')
                return (first, input[i..]);
        return (first, "");
    }

    private static string runeString(int rune)
    {
        var builder = new GoBuilder();
        builder.WriteRune(rune);
        return builder.String();
    }
}
