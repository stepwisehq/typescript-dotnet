// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/crossproject.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using TypeScript.Collections;

using Kind = TypeScript.Scanner.Kind;

namespace TypeScript.LanguageServices;
// Go iter.Seq is an invoked yield function: the provider's snapshot lifetime
// surrounds every callback. It is deliberately not an escaping IEnumerable.
internal delegate void ProjectSequence(Func<IProject, bool> yield);
internal interface ICrossProjectOrchestrator
{
    IProject GetDefaultProject();
    Slice<IProject> GetAllProjectsForInitialRequest();
    LanguageService? GetLanguageServiceForProjectWithFile(ProtocolRequestContext context, IProject project, DocumentUri uri);
    (Slice<IProject> Projects, Exception? Error) GetProjectsForFile(ProtocolRequestContext context, DocumentUri uri);
    ProjectSequence GetProjectsLoadingProjectTree(ProtocolRequestContext context, HashSet<PathKey>? requestedProjectTrees);
}

internal struct ProjectAndTextDocumentPosition
{
    internal IProject project;
    internal LanguageService? ls;
    internal DocumentUri Uri;
    internal TypeScript.Lsp.Protocol.Position Position;
    internal SymbolAndEntriesDataOwner? symbolData;
    internal bool forOriginalLocation;
}

// The queued original *SymbolAndEntriesData may escape its caller stack;
// retain one pointer owner and copy its value only at the worker's source read.
internal sealed class SymbolAndEntriesDataOwner(SymbolAndEntriesData value)
{
    internal SymbolAndEntriesData Value = value;
}

internal sealed class CrossProjectResponse<TResponse>
{
    internal bool complete;
    internal TResponse result = default!;
    internal bool forOriginalLocation;
}

// Go iter.Seq is restartable: callers may enumerate this same yield owner twice.
internal delegate void ReferenceResultSequence<T>(Func<T, bool> yield);
internal sealed partial class LanguageService
{
    // crossproject.go:48. Queue work uses the accepted Go WorkGroup scheduling
    // adapter, with a fresh owner for each original outer-loop phase.
    private (TResponse Result, Exception? Error) handleCrossProject<TRequest, TResponse>(ProtocolRequestContext context, TRequest parameters, ICrossProjectOrchestrator? orchestrator, Func<LanguageService, ProtocolRequestContext, TRequest, SymbolAndEntriesData, SymbolEntryTransformOptions, (TResponse Result, Exception? Error)> transform, Func<ReferenceResultSequence<TResponse>, TResponse> combineResults, bool isRename, bool implementations, SymbolEntryTransformOptions options, SymbolAndEntriesDataOwner? defaultProjectData)
        where TRequest : IHasTextDocumentPosition
    {
        TResponse response = default!;
        Exception? error = null;
        if (orchestrator == null)
        {
            SymbolAndEntriesData data;
            if (defaultProjectData != null)
                data = defaultProjectData.Value;
            else
                (data, _) = provideSymbolsAndEntries(context, parameters.TextDocumentURI(), parameters.TextDocumentPosition(), isRename, implementations);
            return transform(this, context, parameters, data, options);
        }

        var defaultProject = orchestrator.GetDefaultProject();
        var allProjects = orchestrator.GetAllProjectsForInitialRequest();
        var results = new SyncMap<ByteStringKey, CrossProjectResponse<TResponse>>();
        NonLocalDefinition? defaultDefinition = null;
        bool canSearchProject(IProject project) => !results.Load(ByteStringKey.FromValue(project.Id())).Ok;
        var workGroup = Core.NewWorkGroup(false);
        var errorGate = new object();
        Slice<string> panicsOccurred = default;
        var panicGate = new object();
        void enqueueItem(ProjectAndTextDocumentPosition item)
        {
            var itemResponse = new CrossProjectResponse<TResponse>();
            if (results.LoadOrStore(ByteStringKey.FromValue(item.project.Id()), itemResponse).Loaded)
                return;
            workGroup.Queue(() =>
            {
                if (context.Cancellation.IsCancellationRequested)
                    return;
                try
                {
                    var service = item.ls;
                    if (service == null)
                    {
                        service = orchestrator.GetLanguageServiceForProjectWithFile(context, item.project, item.Uri);
                        if (service == null)
                            return;
                    }

                    SymbolAndEntriesData data;
                    bool found;
                    if (item.symbolData != null)
                    {
                        data = item.symbolData.Value;
                        found = true;
                    }
                    else
                        (data, found) = service.provideSymbolsAndEntries(context, item.Uri, item.Position, isRename, implementations);
                    if (context.Cancellation.IsCancellationRequested)
                        return;
                    if (found)
                    {
                        foreach (var entry in data.SymbolsAndEntries)
                        {
                            if (item.project == defaultProject && defaultDefinition == null)
                                defaultDefinition = service.getNonLocalDefinition(context, entry);
                            service.forEachOriginalDefinitionLocation(context, entry, (uri, position) =>
                            {
                                var (definitionProjects, projectError) = orchestrator.GetProjectsForFile(context, uri);
                                if (projectError != null)
                                    return;
                                foreach (var project in definitionProjects)
                                    if (canSearchProject(project))
                                        enqueueItem(new ProjectAndTextDocumentPosition { project = project, Uri = uri, Position = position, forOriginalLocation = true });
                            });
                        }
                    }

                    var (result, searchError) = transform(service, context, parameters, data, options);
                    if (searchError == null)
                    {
                        itemResponse.complete = true;
                        itemResponse.result = result;
                        itemResponse.forOriginalLocation = item.forOriginalLocation;
                    }
                    else
                        lock (errorGate)
                        {
                            error ??= searchError;
                        }
                }
                catch (Exception panic)
                {
                    // Go debug.Stack is represented by the actual CLR exception
                    // stack; no fabricated Go frames or addresses are supplied.
                    string occurred = "panic handling request: " + panic.Message + "\n" + panic.StackTrace;
                    lock (panicGate)
                        panicsOccurred = panicsOccurred.Append(occurred);
                }
            });
        }

        enqueueItem(new ProjectAndTextDocumentPosition { project = defaultProject, ls = this, Uri = parameters.TextDocumentURI(), Position = parameters.TextDocumentPosition(), symbolData = defaultProjectData });
        foreach (var project in allProjects)
            if (project != defaultProject)
                enqueueItem(new ProjectAndTextDocumentPosition { project = project, // Original TODO: symlinks need to change the URI.
 Uri = parameters.TextDocumentURI(), Position = parameters.TextDocumentPosition() });
        ReferenceResultSequence<TResponse> getResultsIterator() => yield =>
        {
            var seenProjects = new SyncSet<ByteStringKey>();
            var (defaultResponse, loaded) = results.Load(ByteStringKey.FromValue(defaultProject.Id()));
            if (loaded && defaultResponse.complete && !yield(defaultResponse.result))
                return;
            seenProjects.Add(ByteStringKey.FromValue(defaultProject.Id()));
            foreach (var project in allProjects)
                if (seenProjects.AddIfAbsent(ByteStringKey.FromValue(project.Id())))
                {
                    var (projectResponse, found) = results.Load(ByteStringKey.FromValue(project.Id()));
                    if (found && projectResponse.complete && !yield(projectResponse.result))
                        return;
                }

            results.Range((key, projectResponse) => !projectResponse.forOriginalLocation && seenProjects.AddIfAbsent(key) && projectResponse.complete ? yield(projectResponse.result) : true);
            results.Range((key, projectResponse) => projectResponse.forOriginalLocation && seenProjects.AddIfAbsent(key) && projectResponse.complete ? yield(projectResponse.result) : true);
        };
        while (true)
        {
            workGroup.RunAndWait();
            if (!panicsOccurred.IsNil)
            {
                var panicMessage = new GoBuilder();
                panicMessage.WriteString("Panics occurred during cross-project handling: [");
                for (int index = 0; index < panicsOccurred.Length; index++)
                {
                    if (index != 0)
                        panicMessage.WriteByte((byte)' ');
                    panicMessage.WriteString(panicsOccurred[index]);
                }

                panicMessage.WriteByte((byte)']');
                throw new InvalidOperationException(panicMessage.String());
            }

            if (context.Cancellation.IsCancellationRequested)
                return (response, new OperationCanceledException(context.Cancellation));
            if (error != null)
                return (response, error);
            workGroup = Core.NewWorkGroup(false);
            bool hasMoreWork = false;
            if (defaultDefinition != null)
            {
                HashSet<PathKey>? requestedProjectTrees = null;
                results.Range((key, projectResponse) =>
                {
                    if (projectResponse.complete)
                        (requestedProjectTrees ??= new()).Add(TsPath.PathKeyFromCanonical(key.Value));
                    return true;
                });
                bool canceled = false;
                orchestrator.GetProjectsLoadingProjectTree(context, requestedProjectTrees)(loadedProject =>
                {
                    if (context.Cancellation.IsCancellationRequested)
                    {
                        canceled = true;
                        return false;
                    }

                    if (!canSearchProject(loadedProject) || loadedProject.GetProgram() == null)
                        return true;
                    IHasTextDocumentPosition? location = null;
                    if (loadedProject.HasFile(defaultDefinition.TextDocumentURI().FileName()))
                        location = defaultDefinition;
                    else if (defaultDefinition.GetSourcePosition() is { } source && loadedProject.HasFile(source.TextDocumentURI().FileName()))
                        location = source;
                    else if (defaultDefinition.GetGeneratedPosition() is { } generated && loadedProject.HasFile(generated.TextDocumentURI().FileName()))
                        location = generated;
                    if (location != null)
                    {
                        enqueueItem(new ProjectAndTextDocumentPosition { project = loadedProject, Uri = location.TextDocumentURI(), Position = location.TextDocumentPosition() });
                        hasMoreWork = true;
                    }

                    return true;
                });
                if (canceled)
                    return (response, new OperationCanceledException(context.Cancellation));
            }

            if (!hasMoreWork)
                break;
        }

        if (results.Size() > 1)
            response = combineResults(getResultsIterator());
        else
            getResultsIterator()(value =>
            {
                response = value;
                return false;
            });
        return (response, null);
    }

    // crossproject.go:299. Location keys use Go value equality rather than CLR
    // protocol-class identity; the set's backing remains nil until first Add.
    private static Slice<T> combineLocationArray<T>(Slice<T> combined, Slice<T> locations, ref HashSet<DefinitionLocationKey>? seen)
        where T : IHasLocation?
    {
        foreach (var locationOwner in locations)
        {
            var location = locationOwner!.GetLocation();
            if ((seen ??= new()).Add(new DefinitionLocationKey(location.Uri, location.Range)))
                // lsproto.Location is a Go value; LocationLink remains a pointer.
                // Preserve the required value copy despite the protocol CLR class.
                combined = combined.Append(locationOwner is Location value ? (T)(object)new Location { Uri = value.Uri, Range = value.Range } : locationOwner);
        }

        return combined;
    }

    // crossproject.go:313. A returned nullable header is a nonnil Go slice pointer,
    // including the nil backing result when every project has no locations.
    private static Slice<Location> combineResponseLocations<T>(ReferenceResultSequence<T> results)
        where T : IHasLocations
    {
        Slice<Location> combined = default;
        HashSet<DefinitionLocationKey>? seen = null;
        results(response =>
        {
            var locations = response.GetLocations();
            if (locations != null)
                combined = combineLocationArray(combined, locations.Value, ref seen);
            return true;
        });
        return combined;
    }

    // crossproject.go:325.
    private static LocationsOrNull combineReferences(ReferenceResultSequence<LocationsOrNull> results) => new()
    {
        Locations = combineResponseLocations(results)
    };
    // crossproject.go:329. Copy the original pointed item value before renumbering;
    // optional slices/display pointers stay shared, and embedded location is copied.
    private static VSReferenceItemsOrNull combineVSReferences(ReferenceResultSequence<VSReferenceItemsOrNull> results)
    {
        Slice<VSReferenceItem?> combined = default;
        int nextId = 0;
        results(response =>
        {
            if (response.VSReferenceItems == null)
                return true;
            var idMap = new Dictionary<int, int>();
            foreach (var optionalItem in response.VSReferenceItems.Value)
            {
                var item = optionalItem!;
                int newId = nextId;
                idMap[item.VSId] = newId;
                nextId++;
                var newItem = new VSReferenceItem
                {
                    VSId = newId,
                    VSDefinitionId = item.VSDefinitionId is { } oldDefinitionId ? idMap.GetValueOrDefault(oldDefinitionId) : null,
                    VSKind = item.VSKind,
                    VSLocation = new Location
                    {
                        Uri = item.VSLocation.Uri,
                        Range = item.VSLocation.Range
                    },
                    VSDefinitionText = item.VSDefinitionText,
                    VSProjectName = item.VSProjectName,
                    VSContainingType = item.VSContainingType
                };
                combined = combined.Append(newItem);
            }

            return true;
        });
        return new()
        {
            VSReferenceItems = combined
        };
    }

    // crossproject.go:357. Source iterator restart is required when a locations
    // response is encountered after definition-link responses.
    private static LocationOrLocationsOrDefinitionLinksOrNull combineImplementations(ReferenceResultSequence<LocationOrLocationsOrDefinitionLinksOrNull> results)
    {
        Slice<LocationLink?> combined = default;
        HashSet<DefinitionLocationKey>? seen = null;
        Slice<Location>? locationResult = null;
        results(response =>
        {
            if (response.DefinitionLinks != null)
                combined = combineLocationArray(combined, response.DefinitionLinks.Value, ref seen);
            else if (response.Locations != null)
            {
                locationResult = combineResponseLocations(results);
                return false;
            }

            return true;
        });
        return locationResult != null ? new()
        {
            Locations = locationResult.Value
        }

        : new()
        {
            DefinitionLinks = combined
        };
    }

    // Original *Set pointer owns a separately nil-backed range set per document.
    private sealed class RenameSeenRanges
    {
        internal HashSet<TypeScript.Lsp.Protocol.Range>? values;
    }

    // crossproject.go:372. These made maps, nil set backing and optional result
    // headers preserve the original empty-versus-absent workspace edit outcomes.
    private static WorkspaceEditOrNull combineRenameResponse(ReferenceResultSequence<WorkspaceEditOrNull> results)
    {
        var combined = new Dictionary<DocumentUri, Slice<TextEdit?>>();
        var seenChanges = new Dictionary<DocumentUri, RenameSeenRanges>();
        Slice<TextDocumentEditOrCreateFileOrRenameFileOrDeleteFile> documentChanges = default;
        HashSet<(DocumentUri OldUri, DocumentUri NewUri)>? seenRenames = null;
        results(response =>
        {
            var edit = response.WorkspaceEdit;
            if (edit?.DocumentChanges != null)
            {
                foreach (var change in edit.DocumentChanges.Value)
                {
                    if (change.RenameFile != null)
                    {
                        var key = (change.RenameFile.OldUri, change.RenameFile.NewUri);
                        if ((seenRenames ??= new()).Add(key))
                            documentChanges = documentChanges.Append(change);
                    }
                    else
                        documentChanges = documentChanges.Append(change);
                }
            }

            if (edit?.Changes != null)
            {
                foreach (var (document, changes)in edit.Changes.Value)
                {
                    if (!seenChanges.TryGetValue(document, out var seen))
                    {
                        seen = new RenameSeenRanges();
                        seenChanges[document] = seen;
                    }

                    if (!combined.TryGetValue(document, out var changesForDocument))
                        changesForDocument = Slice<TextEdit?>.Make(0);
                    foreach (var optionalChange in changes)
                    {
                        var change = optionalChange!;
                        if (seen.values?.Contains(change.Range) != true)
                        {
                            (seen.values ??= new()).Add(change.Range);
                            changesForDocument = changesForDocument.Append(change);
                        }
                    }

                    combined[document] = changesForDocument;
                }
            }

            return true;
        });
        if (documentChanges.Length == 0 && combined.Count == 0)
            return new();
        var workspaceEdit = new WorkspaceEdit();
        if (documentChanges.Length > 0)
            workspaceEdit.DocumentChanges = documentChanges;
        if (combined.Count > 0)
            workspaceEdit.Changes = new ProtocolMap<DocumentUri, Slice<TextEdit?>>(combined);
        return new()
        {
            WorkspaceEdit = workspaceEdit
        };
    }

    // crossproject.go:423. Incoming-call duplicate keys use value locations.
    private static CallHierarchyIncomingCallsOrNull combineIncomingCalls(ReferenceResultSequence<CallHierarchyIncomingCallsOrNull> results)
    {
        Slice<CallHierarchyIncomingCall?> combined = default;
        HashSet<DefinitionLocationKey>? seenCalls = null;
        results(response =>
        {
            if (response.CallHierarchyIncomingCalls != null)
                foreach (var optionalCall in response.CallHierarchyIncomingCalls.Value)
                {
                    var call = optionalCall!;
                    var location = call.From!.GetLocation();
                    if ((seenCalls ??= new()).Add(new DefinitionLocationKey(location.Uri, location.Range)))
                        combined = combined.Append(call);
                }

            return true;
        });
        return new()
        {
            CallHierarchyIncomingCalls = combined
        };
    }
}
