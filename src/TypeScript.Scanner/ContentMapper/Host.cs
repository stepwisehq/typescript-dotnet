// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/contentmapper/host.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Interfaces/results/errors/timing arithmetic; subprocess/IPC hostimpl.go is external.
using TypeScript.Scanner;
using Span = TypeScript.SpanMap.SpanMap;
using LocaleValue = TypeScript.Locale.Locale;

namespace TypeScript.ContentMapper;
public enum TransformErrorKind : byte
{
    Unknown,
    Initialize,
    Project,
    Request,
    Response,
    Mappings
}

public sealed class TransformError : Exception
{
    public TransformErrorKind Kind;
    public TransformError(TransformErrorKind kind, Exception? error) : base(null, error) => Kind = kind;
    public static TransformError NewTransformError(TransformErrorKind kind, Exception? error) => new(kind, error);
    public override string Message => "content mapper transform failed: " + (InnerException?.Message ?? "<nil>");

    public Exception? Unwrap() => InnerException;
}

public enum DiagnosticDirectivePolicy : byte
{
    Ignore,
    Expect
}

public enum DiagnosticDirectiveErrorKind : byte
{
    InvalidRange,
    InvalidPolicy,
    ExpectMissingUnusedDiagnostic,
    InvalidUnusedDiagnosticIndex,
    Overlap
}

public sealed class DiagnosticDirectiveError : Exception
{
    public DiagnosticDirectiveErrorKind Kind;
    public long Index, SupplementalIndex;
    public DiagnosticDirectivePolicy Policy;
    public override string Message => $"invalid content mapper diagnostic directive {Index}";
}

public sealed class InvalidVirtualExtensionError : Exception
{
    public string Extension = "";
    public override string Message => "invalid virtual extension " + GoQuote.Quote(Extension);
}

public enum ProjectErrorKind : byte
{
    MalformedResponse,
    MissingConfigIdentity,
    NonAbsoluteWatchedFile,
    UnexpectedConfigIdentity,
    UnexpectedWatchedFiles
}

public sealed class ProjectError : Exception
{
    public ProjectErrorKind Kind;
    public override string Message => Kind switch
    {
        ProjectErrorKind.MalformedResponse => "content mapper returned a malformed project response",
        ProjectErrorKind.MissingConfigIdentity => "content mapper did not return configIdentity for dynamic configuration",
        ProjectErrorKind.NonAbsoluteWatchedFile => "content mapper returned a non-absolute path in watchedFiles",
        ProjectErrorKind.UnexpectedConfigIdentity => "content mapper returned configIdentity without declaring dynamicConfig",
        ProjectErrorKind.UnexpectedWatchedFiles => "content mapper returned watchedFiles without declaring dynamicConfig",
        _ => "content mapper returned an invalid project response"
    };
}

// Selected model dependency from hostimpl.go:93/125; no process implementation.
public readonly record struct PositionEncoding
{
    private readonly string? value;
    public PositionEncoding(string value) => this.value = value;
    public string AsString() => value ?? "";
    public static readonly PositionEncoding UTF8 = new("utf-8"), UTF16 = new("utf-16");
}

public enum InitializeErrorKind : byte
{
    ProcessStart,
    ProcessExit,
    NoResponse,
    InvalidResponse,
    Request,
    PositionEncoding,
    EmptyDiagnosticSource,
    ReservedDiagnosticSource
}

public sealed class InitializeError : Exception
{
    public InitializeErrorKind Kind;
    public string MapperName = "", Command = "", Detail = "", DiagnosticSource = "";
    public long ExitCode, TimeoutSeconds;
    public PositionEncoding PositionEncoding;
    public override string Message => Kind switch
    {
        InitializeErrorKind.ProcessStart => "could not start content mapper command " + GoQuote.Quote(Command) + ": " + Detail,
        InitializeErrorKind.ProcessExit => $"content mapper process exited before initialization with code {ExitCode}",
        InitializeErrorKind.NoResponse => "content mapper did not respond to the initialize request",
        InitializeErrorKind.InvalidResponse => "content mapper returned an invalid initialize response: " + Detail,
        InitializeErrorKind.Request => "content mapper initialize request failed: " + Detail,
        InitializeErrorKind.PositionEncoding => "unsupported position encoding " + GoQuote.Quote(PositionEncoding.AsString()),
        InitializeErrorKind.EmptyDiagnosticSource => "diagnostic source must not be empty",
        InitializeErrorKind.ReservedDiagnosticSource => "diagnostic source " + GoQuote.Quote(DiagnosticSource) + " is reserved by TypeScript",
        _ => "content mapper initialization failed"
    };
}

public sealed class SupplementalFileCollisionError : Exception
{
    public RootedFilePath FileName;
    public override string Message => "content mapper supplemental output file " + GoQuote.Quote(FileName.AsString()) + " already exists";
}

public struct Result
{
    private string? text, virtualExtension;
    public string Text { readonly get => text ?? ""; set => text = value; }
    public string VirtualExtension { readonly get => virtualExtension ?? ""; set => virtualExtension = value; }

    public List<Diagnostic>? Diagnostics;
    public Span? Mappings;
    public List<MappedDiagnosticDirective>? DiagnosticDirectives;
    public List<MappedResult>? Supplemental;
}

public struct MappedResult
{
    private string? text, virtualExtension;
    public string Text { readonly get => text ?? ""; set => text = value; }
    public string VirtualExtension { readonly get => virtualExtension ?? ""; set => virtualExtension = value; }

    public Span? Mappings;
    public List<MappedDiagnosticDirective>? DiagnosticDirectives;
}

public struct Request
{
    public RootedFilePath FileName;
    private string? content;
    public string Content { readonly get => content ?? ""; set => content = value; }
}

public struct ProjectSpec
{
    public RootedFilePath ConfigFileName;
    public List<Mapper>? Mappers;
    public CompilerOptions? CompilerOptions;
}

public struct OptionDiagnostic
{
    public Mapper? Mapper;
    public List<OptionPathSegment>? Path;
    private string? source, messageText;
    public string Source { readonly get => source ?? ""; set => source = value; }
    public string MessageText { readonly get => messageText ?? ""; set => messageText = value; }

    public int Code;
}

// Go time.Duration stores signed nanoseconds. Do not narrow to TimeSpan's 100 ns units.
public struct OperationTiming
{
    public ulong Count;
    public long Duration;
}

public struct MapperTimings
{
    public OperationTiming Spawn, Initialize, OpenProject, CloseProject, Transform;
}

public struct Timings
{
    public Dictionary<string, MapperTimings>? Mappers;
    public long RequestWait;
    public readonly Timings Since(Timings previous)
    {
        var result = new Timings
        {
            Mappers = new(StringComparer.Ordinal),
            RequestWait = Math.Max(unchecked(RequestWait - previous.RequestWait), 0)
        };
        foreach (var(identity, current)in Mappers ?? [])
        {
            var before = previous.Mappers?.GetValueOrDefault(identity) ?? default;
            result.Mappers[identity] = new()
            {
                Spawn = operationTimingSince(current.Spawn, before.Spawn),
                Initialize = operationTimingSince(current.Initialize, before.Initialize),
                OpenProject = operationTimingSince(current.OpenProject, before.OpenProject),
                CloseProject = operationTimingSince(current.CloseProject, before.CloseProject),
                Transform = operationTimingSince(current.Transform, before.Transform)
            };
        }

        return result;
    }

    internal static OperationTiming operationTimingSince(OperationTiming current, OperationTiming previous) => new()
    {
        Count = current.Count - Math.Min(current.Count, previous.Count),
        Duration = Math.Max(unchecked(current.Duration - previous.Duration), 0)
    };
}

public interface IProject
{
    Exception? Refresh();
    (List<string>? Identities, Exception? Error) Identities();
    (string Identity, Exception? Error) Identity(Mapper mapper);
    (List<RootedFilePath>? Files, Exception? Error) WatchedFiles();
    List<OptionDiagnostic>? Diagnostics();
    (Result Result, Exception? Error) Transform(Mapper mapper, Request request);
    Exception? Close();
}

public interface IHost
{
    Timings Timings();
    IProject Project(ProjectSpec spec);
    Action Acquire(List<Mapper>? mappers);
    void SetLocale(LocaleValue locale);
    (Result Result, Exception? Error) Transform(Mapper mapper, Request request);
    Exception? Close();
}
