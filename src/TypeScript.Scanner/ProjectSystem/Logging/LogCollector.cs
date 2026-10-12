// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/project/logging/logcollector.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;

namespace TypeScript.ProjectSystem.Logging;
internal interface ILogCollector : ILogger
{
    string String();
}

internal sealed class LogCollector : Logger, ILogCollector
{
    private readonly MemoryStream builder;
    private LogCollector(MemoryStream builder) : base(builder, static () => FormatTime(DateTimeOffset.FromUnixTimeSeconds(1349085672).ToLocalTime())) => this.builder = builder;
    public string String() => GoString.FromBytes(builder.GetBuffer().AsSpan(0, checked((int)builder.Length)));
    public override string ToString() => String();
    public static ILogCollector NewTestLogger() => new LogCollector(new MemoryStream());
}
