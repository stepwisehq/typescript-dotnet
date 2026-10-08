// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/ast/diagnostic.go; parse diagnostic slice.
#nullable disable
namespace TypeScript.Scanner;
public sealed class Diagnostic
{
    internal SourceFile file;
    internal TextRange loc;
    internal Message message;
    internal List<string> messageArgs;
    internal List<Diagnostic> relatedInformation, messageChain;
    public SourceFile File() => file;
    public int Pos() => loc.Pos();
    public int End() => loc.End();
    public int Len() => loc.Len();
    public TextRange Loc() => loc;
    public int Code() => message.Code();
    public DiagnosticCategory Category() => message.Category();
    public string MessageKey() => message.Key();
    public string MessageText() => "";
    public List<string> MessageArgs() => messageArgs;
    public List<Diagnostic> RelatedInformation() => relatedInformation;
    public List<Diagnostic> MessageChain() => messageChain;
    public bool ReportsUnnecessary() => message.ReportsUnnecessary();
    public bool ReportsDeprecated() => message.ReportsDeprecated();
    public string Source() => "";
    public void SetFile(SourceFile value) => file = value;
    public void SetLocation(TextRange value) => loc = value;
    public Diagnostic AddRelatedInfo(Diagnostic value)
    {
        if (value is not null)
            (relatedInformation ??= new()).Add(value);
        return this;
    }

    public Diagnostic SetRelatedInfo(List<Diagnostic> value)
    {
        relatedInformation = value;
        return this;
    }

    public Diagnostic AddMessageChain(Diagnostic value)
    {
        if (value is not null)
            (messageChain ??= new()).Add(value);
        return this;
    }

    public Diagnostic SetMessageChain(List<Diagnostic> value)
    {
        messageChain = value;
        return this;
    }

    public string String() => message.Format(messageArgs?.Select(s => (object)ScannerString.ToJsString(s)).ToArray() ?? []);
    public override string ToString() => String();
    public Diagnostic Clone() => (Diagnostic)MemberwiseClone();
}

public static partial class Ast
{
    public static Diagnostic NewDiagnostic(SourceFile file, TextRange loc, Message message, params object[] args) => new()
    {
        file = file,
        loc = loc,
        message = message,
        messageArgs = args is { Length: > 0 } ? args.Select(StringifyArgument).ToList() : null
    };
    private static string StringifyArgument(object value) => value switch
    {
        string s => s,
        bool b => b ? "true" : "false",
        null => "<nil>",
        _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "",
    };
}
