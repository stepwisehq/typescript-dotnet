// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Pinned internal/lsp/server.go:1470–1513; genuine multi-project draft.
using TypeScript.LanguageServices;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;
using ServiceProject = TypeScript.ProjectSystem.Project;

namespace TypeScript.Lsp;
internal sealed partial class Server
{
    private sealed class CrossProjectOrchestrator(Server server, RequestMessage request, ServiceProject defaultProject, Slice<IProject> allProjects) : ICrossProjectOrchestrator
    {
        // Retain the request owner just as the upstream orchestrator structure does.
        private RequestMessage Request { get; } = request;

        public IProject GetDefaultProject() => defaultProject;
        public Slice<IProject> GetAllProjectsForInitialRequest() => allProjects;
        public LanguageService? GetLanguageServiceForProjectWithFile(ProtocolRequestContext context, IProject project, DocumentUri uri) => server.session!.GetLanguageServiceForProjectWithFile(context, (ServiceProject)project, uri);
        public (Slice<IProject> Projects, Exception? Error) GetProjectsForFile(ProtocolRequestContext context, DocumentUri uri) => server.session!.GetProjectsForFile(context, uri);
        public ProjectSequence GetProjectsLoadingProjectTree(ProtocolRequestContext context, HashSet<PathKey>? requestedProjectTrees) => yield => server.session!.WithSnapshotLoadingProjectTree(context, requestedProjectTrees, snapshot =>
        {
            foreach (var project in snapshot.ProjectCollection.LanguageServiceProjects())
                if (!yield(project))
                    return;
        });
    }

    private (LanguageService? Service, ICrossProjectOrchestrator? Orchestrator, Exception? Error) GetLanguageServiceAndCrossProjectOrchestrator(ProtocolRequestContext context, DocumentUri uri, RequestMessage request)
    {
        var(project, service, allProjects, error) = session!.GetLanguageServiceAndProjectsForFile(context, uri);
        ICrossProjectOrchestrator? orchestrator = error is null ? new CrossProjectOrchestrator(this, request, project!, allProjects) : null;
        return (service, orchestrator, error);
    }
}
