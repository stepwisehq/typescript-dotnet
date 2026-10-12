// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Go error-string byte ownership at the .NET Exception display/log boundary.
using TypeScript.Scanner;
using TypeScript.ProjectSystem.Logging;

namespace TypeScript.FsWatch;
internal class RawWatchError(string bytes, Exception? inner = null) : IOException(GoString.ToString(bytes), inner), IGoLogStringer
{
    private readonly string bytes = bytes;
    public string GoLogString() => bytes;
}

internal static partial class FsWatch
{
    internal static string ErrorString(Exception error) => error is IGoLogStringer raw ? raw.GoLogString() : GoString.ToUtf8(error.Message);
}
