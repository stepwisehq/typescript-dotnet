// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/lsp/lsproto/util.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using System.Globalization;
using TypeScript.Scanner;
namespace TypeScript.Lsp.Protocol;

internal static class ProtocolUtilities
{
    internal static int ComparePositions(Position position, Position other)
    {
        int lineComparison = position.Line.CompareTo(other.Line);
        return lineComparison != 0 ? lineComparison : position.Character.CompareTo(other.Character);
    }

    internal static int CompareRanges(Range range, Range other)
    {
        int startComparison = ComparePositions(range.Start, other.Start);
        return startComparison != 0 ? startComparison : ComparePositions(range.End, other.End);
    }

    private static bool diagnosticExistsInSlice(Diagnostic element, Slice<Diagnostic?> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
            if (diagnosticsEqual(element, diagnostic!))
                return true;
        return false;
    }

    private static bool diagnosticsEqual(Diagnostic first, Diagnostic second) =>
        diagnosticCodesEqual(first.Code!, second.Code!) &&
        diagnosticMessagesEqual(first.Message, second.Message) &&
        CompareRanges(first.Range, second.Range) == 0;

    private static bool diagnosticCodesEqual(IntegerOrString first, IntegerOrString second)
    {
        if (first.String is { } firstString && second.String is { } secondString)
            return firstString.EqualsValue(secondString);
        if (first.Integer is { } firstInteger && second.Integer is { } secondInteger)
            return firstInteger == secondInteger;
        return false;
    }

    private static bool diagnosticMessagesEqual(StringOrMarkupContent first, StringOrMarkupContent second)
    {
        if (first.String is { } firstString && second.String is { } secondString)
            return firstString.EqualsValue(secondString);
        if (first.MarkupContent is { } firstMarkup && second.MarkupContent is { } secondMarkup)
            return firstMarkup.Kind == secondMarkup.Kind && firstMarkup.Value.EqualsValue(secondMarkup.Value);
        return false;
    }

    internal static (Slice<Diagnostic?> MissingFromFirst, Slice<Diagnostic?> MissingFromSecond) CompareDiagnostics(Slice<Diagnostic?> first, Slice<Diagnostic?> second)
    {
        var missingFromFirst = Slice<Diagnostic?>.Make(0);
        var missingFromSecond = Slice<Diagnostic?>.Make(0);
        foreach (var element in first)
            if (!diagnosticExistsInSlice(element!, second))
                missingFromSecond = missingFromSecond.Append(element);
        foreach (var element in second)
            if (!diagnosticExistsInSlice(element!, first))
                missingFromFirst = missingFromFirst.Append(element);
        return (missingFromFirst, missingFromSecond);
    }
}

internal sealed partial class StringOrMarkupContent
{
    internal ByteStringValue AsString() => String ?? MarkupContent?.Value ?? default;
}

internal sealed partial class IntegerOrString
{
    internal ByteStringValue AsString() => String ?? ByteStringValue.FromString(Integer is { } integer
        ? integer.ToString(CultureInfo.InvariantCulture)
        : "-1");
}

internal sealed partial class Diagnostic
{
    internal ByteStringValue AsString() => ByteStringValue.Concat(
        Code!.AsString(),
        ByteStringValue.FromString(" (" + Range.Start.Line.ToString(CultureInfo.InvariantCulture) + ":" + Range.Start.Character.ToString(CultureInfo.InvariantCulture) + "-" + Range.End.Line.ToString(CultureInfo.InvariantCulture) + ":" + Range.End.Character.ToString(CultureInfo.InvariantCulture) + "): "),
        Message.AsString());

    internal ByteStringValue CodeAsString() => ByteStringValue.Concat(ByteStringValue.FromString("Code("), Code!.AsString(), ByteStringValue.FromString(")"));
}
