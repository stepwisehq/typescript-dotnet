// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.ContentMapper;
using TypeScript.JsonRpc;
using TypeScript.Scanner;
using LocaleValue = TypeScript.Locale.Locale;

namespace LanguageServices.Checks;
public static partial class ContentMapperHostChecks
{
    private static Mapper DynamicMapper() => new()
    {
        Package = "dynamic",
        Name = "dynamic",
        Version = "1.0.0",
        Exec = ["mapper"],
        DynamicConfig = true
    };
    private static void AssertProjectError(RecordingMapper process, Mapper mapper, ProjectErrorKind kind, bool configFile = true)
    {
        var host = ProcessHosts.NewHost(CancellationToken.None, new FakeSpawner(process), TypeScript.Locale.Locales.Default);
        try
        {
            var project = host.Project(new ProjectSpec { ConfigFileName = configFile ? new RootedFilePath("/repo/tsconfig.json") : default, Mappers = [mapper], CompilerOptions = new() });
            try
            {
                var(_, error) = project.Transform(mapper, new Request { FileName = new RootedFilePath("/repo/file.ext"), Content = "x" });
                Assert(ErrorAs<TransformError>(error)is not null, "transform error wrapper");
                Assert(ErrorAs<ProjectError>(error)?.Kind == kind, "original project error kind");
            }
            finally
            {
                _ = project.Close();
            }
        }
        finally
        {
            _ = host.Close();
        }
    }

    private static void ProjectRejectsRelativeWatchedFiles() => AssertProjectError(new RecordingMapper { DynamicConfig = true, WatchedFiles = Slice<ByteStringValue>.FromArray([Text("mapper.config.js")]) }, DynamicMapper(), ProjectErrorKind.NonAbsoluteWatchedFile);
    private static void DynamicProjectRequiresConfigIdentity() => AssertProjectError(new RecordingMapper { DynamicConfig = true, ConfigIdentity = "" }, DynamicMapper(), ProjectErrorKind.MissingConfigIdentity);
    private static void StaticMapperRejectsDynamicProjectResponseFields()
    {
        var mapper = Mapper("static");
        mapper.Version = "1.0.0";
        AssertProjectError(new RecordingMapper { ConfigIdentity = "dynamic" }, mapper, ProjectErrorKind.UnexpectedConfigIdentity);
        AssertProjectError(new RecordingMapper { WatchedFiles = Slice<ByteStringValue>.FromArray([Text("/repo/mapper.config.js")]) }, mapper, ProjectErrorKind.UnexpectedWatchedFiles);
    }

    private static void ProjectRejectsInvalidOptionDiagnosticPath()
    {
        var process = new RecordingMapper
        {
            OptionDiagnostics = Slice<OptionDiagnosticResult>.FromArray([new() { Path = Slice<RawJsonValue?>.FromArray([new RawJsonValue("null"u8)]), MessageText = Text("Invalid option."), Code = 123 }])
        };
        var mapper = Mapper();
        mapper.Version = "1.0.0";
        AssertProjectError(process, mapper, ProjectErrorKind.MalformedResponse, configFile: false);
    }

    private static void ProjectMethodsAfterHostClose()
    {
        var host = ProcessHosts.NewHost(CancellationToken.None, new FakeSpawner(new RecordingMapper { DynamicConfig = true }), TypeScript.Locale.Locales.Default);
        var mapper = DynamicMapper();
        mapper.Package = "";
        var project = host.Project(new ProjectSpec { ConfigFileName = new RootedFilePath("/repo/tsconfig.json"), Mappers = [mapper], CompilerOptions = new() });
        try
        {
            var(_, transformError) = project.Transform(mapper, new Request { FileName = new RootedFilePath("/repo/file.ext"), Content = "x" });
            Assert(transformError is null, transformError?.Message ?? "initial project transform");
            var(unknown, unknownError) = project.Identity(new Mapper());
            Assert(unknownError is null && unknown == "", "unknown identity before close");
            Assert(host.Close()is null, "host close");
            Assert(project.Refresh()is null, "closed project refresh");
            var(identities, identitiesError) = project.Identities();
            Assert(identitiesError is null && (identities?.Count ?? 0) == 0, "closed identities");
            var(identity, identityError) = project.Identity(mapper);
            Assert(identityError is null && identity == "", "closed mapper identity");
            (unknown, unknownError) = project.Identity(new Mapper());
            Assert(unknownError is null && unknown == "", "unknown identity after close");
            var(files, filesError) = project.WatchedFiles();
            Assert(filesError is null && (files?.Count ?? 0) == 0, "closed watched files");
            Assert((project.Diagnostics()?.Count ?? 0) == 0, "closed diagnostics");
            var(_, error) = project.Transform(mapper, new Request { FileName = new RootedFilePath("/repo/file.ext"), Content = "x" });
            Assert(error is not null && error.Message.Contains("content mapper project is closed", StringComparison.Ordinal), "closed transform error");
        }
        finally
        {
            _ = project.Close();
            _ = host.Close();
        }
    }
}
