// Copyright 2020 The Go Authors. All rights reserved.
// Licensed under the BSD-style license in licenses/Go-LICENSE.txt.
// Selected SyntacticError.Error from Go1.27.1 encoding/json/jsontext/errors.go;
// selected SemanticError.Error from encoding/json/v2/errors.go.
// Raw byte error text is converted exactly once into the managed Exception role.
using System.Globalization;
using TypeScript.Scanner;

namespace TypeScript.Json.Internal;
internal static class JsonTextErrors
{
    internal static string SyntacticError(string error, string pointer, int offset, string? duplicateName = null)
    {
        string result = "jsontext: " + error;
        if (duplicateName is not null)
        {
            result += " " + GoQuote.Quote(duplicateName);
            offset = 0;
        }

        if (pointer != "")
            result += " within " + GoQuote.Quote(JsonWire.TruncatePointer(pointer, 100));
        if (offset > 0)
            result += " after offset " + offset.ToString(CultureInfo.InvariantCulture);
        return result;
    }

    internal static string SemanticError(string action, string kind, string value, string type, string pointer, int offset, string? error)
    {
        string result = "json: cannot " + action;
        string preposition = action == "marshal" ? " from" : " into";
        if (kind != "")
            result += " JSON " + kind;
        if (value.Length is> 0 and < 100)
            result += " " + value;
        result += preposition + " Go " + type;
        if (pointer != "")
            result += " within " + GoQuote.Quote(JsonWire.TruncatePointer(pointer, 100));
        else if (offset > 0)
            result += " after offset " + offset.ToString(CultureInfo.InvariantCulture);
        if (error is not null)
            result += ": " + error;
        return result;
    }

    internal static Exception AsException(string rawMessage) => new FormatException(GoString.ToString(rawMessage));
    internal static string AppendPointer(string pointer, string name) => pointer + "/" + name.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);
}
