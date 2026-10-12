// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Pinned internal/lsp/server.go:299–357. Ignored genuine project-client owner draft.
using TypeScript.Lsp.Protocol;
using TypeScript.ProjectSystem;
using TypeScript.Scanner;
using ProtocolWatcher = TypeScript.Lsp.Protocol.FileSystemWatcher;

namespace TypeScript.Lsp;
internal sealed partial class Server
{
    internal Exception? WatchFiles(ProtocolRequestContext context, WatcherID id, Slice<ProtocolWatcher> requests)
    {
        Exception? error;
        if (builtinWatcher is not null)
            error = builtinWatcher.WatchFiles(id.Value, requests);
        else
        {
            var parameters = new RegistrationParams
            {
                Registrations = Slice<Registration?>.FromArray([new Registration { Id = id.Value, RegisterOptions = new RegisterOptions { WorkspaceDidChangeWatchedFiles = new DidChangeWatchedFilesRegistrationOptions { Watchers = requests! } } }])
            };
            (_, error) = SendClientRequest(context, Method.ClientRegisterCapabilityInfo, parameters);
        }

        if (error is not null)
            return new InvalidOperationException("failed to register file watcher: " + error.Message, error);
        watchers.Add(id);
        return null;
    }

    internal Exception? UnwatchFiles(ProtocolRequestContext context, WatcherID id)
    {
        if (!watchers.Has(id))
            return new InvalidOperationException("no file watcher exists with ID " + GoString.ToString(id.Value.Materialize()!));
        Exception? error;
        if (builtinWatcher is not null)
            error = builtinWatcher.UnwatchFiles(id.Value);
        else
        {
            var parameters = new UnregistrationParams
            {
                Unregisterations = Slice<Unregistration?>.FromArray([new Unregistration { Id = id.Value, Method = Method.WorkspaceDidChangeWatchedFiles.Value }])
            };
            (_, error) = SendClientRequest(context, Method.ClientUnregisterCapabilityInfo, parameters);
        }

        if (error is not null)
            return new InvalidOperationException("failed to unregister file watcher: " + error.Message, error);
        watchers.Delete(id);
        return null;
    }
}
