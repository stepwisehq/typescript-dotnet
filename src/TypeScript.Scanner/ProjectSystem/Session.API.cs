// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/project/api.go state workers; no embedding transport endpoint.
using TypeScript.Lsp.Protocol;

namespace TypeScript.ProjectSystem;
internal sealed partial class Session
{
    internal (Snapshot? Snapshot, Exception? Error) APIUpdate(ProtocolRequestContext context, FileChangeSummary apiFileChanges, APISnapshotRequest? request)
    {
        lock (snapshotUpdateMu)
        {
            CancelScheduledSnapshotUpdate();
            var(hostFileChanges, overlays, ataChanges, _) = FlushChanges(context);
            var fileChanges = hostFileChanges.Clone();
            FileChanges.MergeFileChangeSummary(ref fileChanges, apiFileChanges);
            var next = UpdateSnapshotRef(context, overlays, new SnapshotChange { apiRequest = request, fs = request?.FileSystem, fileSystemOverride = request?.FileSystem is not null, replaceFileSystem = request?.ReplaceFileSystem ?? false, fileChanges = fileChanges, ataChanges = ataChanges });
            if (next.apiError is not null)
            {
                var error = next.apiError;
                next.Deref();
                if (!hostFileChanges.IsEmpty() || ataChanges.Count != 0)
                    UpdateSnapshot(context, overlays, new SnapshotChange { fileChanges = hostFileChanges, ataChanges = ataChanges });
                return (null, error);
            }

            return (next, null);
        }
    }

    internal void TryAdoptSnapshotInBackground(Snapshot baseline, Snapshot next)
    {
        SnapshotHost.RetainSnapshot(next);
        TryAdoptSnapshotChangeInBackground(baseline, next);
    }
}
