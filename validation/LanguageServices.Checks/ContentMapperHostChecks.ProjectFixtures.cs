// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.ContentMapper;
using TypeScript.Ipc;
using TypeScript.JsonRpc;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;

namespace LanguageServices.Checks;
public static partial class ContentMapperHostChecks
{
    // host_test.go:872. Exact retained recording mapper used by project tests.
    private class RecordingMapper : IHandler, IHandlesProjects
    {
        internal readonly object Gate = new();
        internal string Received = "", ReceivedOptions = "", ReceivedLocale = "";
        internal readonly List<string> ProjectHandles = [], ClosedHandles = [];
        internal string TransformHandle = "", TransformParameters = "";
        internal Slice<ByteStringValue> WatchedFiles;
        internal string? ConfigIdentity;
        internal bool DynamicConfig;
        internal Slice<OptionDiagnosticResult> OptionDiagnostics;
        public virtual (object? Result, Exception? Error) HandleRequest(CancellationToken context, ByteStringValue method, RawJsonValue? parameters)
        {
            if (method.EqualsValue(ProcessHosts.MethodInitialize))
            {
                var(value, error) = Ipc.UnmarshalParams(parameters, ContentMapperCodecs.InitializeParamsValue);
                if (error is not null)
                    return (null, error);
                lock (Gate)
                    ReceivedLocale = value!.Value.Locale.Materialize() ?? "";
                return (Payload(ContentMapperCodecs.InitializeResultValue, new TypeScript.ContentMapper.InitializeResult { PositionEncoding = PositionEncoding.UTF8, DiagnosticSource = Text("mapper") }), null);
            }

            if (method.EqualsValue(ProcessHosts.MethodOpenProject))
            {
                var(value, error) = Ipc.UnmarshalParams(parameters, ContentMapperCodecs.OpenProjectParamsValue);
                if (error is not null)
                    return (null, error);
                var p = value!.Value;
                var(raw, marshalError) = TypeScript.Json.JsonCodec.FormatRawValue(new TypeScript.Json.JsonValue(p.CompilerOptions?.BorrowOwnedBytes() ?? "null"u8.ToArray()), allowInvalidUtf8: true);
                if (marshalError is not null)
                    return (null, marshalError);
                Slice<ByteStringValue> files;
                bool dynamic;
                string? identityOverride;
                Slice<OptionDiagnosticResult> diagnostics;
                lock (Gate)
                {
                    ProjectHandles.Add(p.ProjectHandle.Materialize() ?? "");
                    Received = GoString.FromBytes(raw!);
                    ReceivedOptions = p.Options?.TextValue.Materialize() ?? "";
                    files = WatchedFiles;
                    dynamic = DynamicConfig;
                    identityOverride = ConfigIdentity;
                    diagnostics = OptionDiagnostics;
                }

                if (!dynamic && files.IsNil && identityOverride is null && diagnostics.Length == 0)
                    return (Payload(ContentMapperCodecs.OpenProjectResultValue, new OpenProjectResult()), null);
                if (dynamic && files.IsNil)
                    files = Slice<ByteStringValue>.FromArray([TypeScript.Scanner.TsPath.CombinePaths(TypeScript.Scanner.TsPath.GetDirectoryPath(p.ConfigFileName), Text("mapper.config.js"))]);
                var identity = dynamic ? ByteStringValue.Concat(Text("config:"), p.Options?.TextValue ?? default) : default;
                if (identityOverride is not null)
                    identity = Text(identityOverride);
                return (Payload(ContentMapperCodecs.OpenProjectResultValue, new OpenProjectResult { ConfigIdentity = identity, WatchedFiles = files, OptionDiagnostics = diagnostics }), null);
            }

            if (method.EqualsValue(ProcessHosts.MethodCloseProject))
            {
                var(value, error) = Ipc.UnmarshalParams(parameters, ContentMapperCodecs.CloseProjectParamsValue);
                if (error is not null)
                    return (null, error);
                lock (Gate)
                    ClosedHandles.Add(value!.Value.ProjectHandle.Materialize() ?? "");
                return (null, null);
            }

            if (method.EqualsValue(ProcessHosts.MethodTransform))
            {
                var(value, error) = Ipc.UnmarshalParams(parameters, ContentMapperCodecs.TransformParamsValue);
                if (error is not null)
                    return (null, error);
                lock (Gate)
                {
                    TransformHandle = value!.Value.ProjectHandle.Materialize() ?? "";
                    TransformParameters = parameters?.TextValue.Materialize() ?? "";
                }

                return (Payload(ContentMapperCodecs.TransformResultValue, new TransformResult { Text = value!.Value.Content, Extension = Text(".ts") }), null);
            }

            return (null, new InvalidOperationException("unexpected method " + method.Materialize()));
        }

        public Exception? HandleNotification(CancellationToken context, ByteStringValue method, RawJsonValue? parameters) => null;
    }

    private sealed class BlockingMapper : RecordingMapper
    {
        internal readonly ManualResetEventSlim Started = new(), Proceed = new();
        public override (object? Result, Exception? Error) HandleRequest(CancellationToken context, ByteStringValue method, RawJsonValue? parameters)
        {
            if (method.EqualsValue(ProcessHosts.MethodTransform))
            {
                Started.Set();
                Proceed.Wait();
            }

            return base.HandleRequest(context, method, parameters);
        }
    }
}
