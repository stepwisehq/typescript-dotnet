// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// refcountcache_test.go TestRefCountingCaches original case-only duplicate lifetime.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectSnapshotLifetimeChecks
{
    private static int ProjectParseEntryCount(Session session)
    {
        int count = 0;
        session.SnapshotHost.parseCache.entries.Range((key, _) =>
        {
            if (key.ParseOptions.FileNameValue.StartsWith("/user/username/projects/myproject/src/"u8))
                count++;
            return true;
        });
        return count;
    }

    private static void CaseOnlyDuplicateReleased()
    {
        const string text = "import { util as a } from \"./utils\";\nimport { util as b } from \"./UTILS\";\nconst x = a + b;";
        using var fixture = ParseFixture(text, "export const util = 1;");
        var session = fixture.Session;
        OpenFile(session, "file://" + Main, text);
        var program = CurrentProgram(session, Main);
        Check(ProjectParseEntryCount(session) == 3, "three case-only cache entries");
        Check(program.GetSourceFile(RootedFilePath.FromGoString(Utils))is not null, "canonical utils exists");
        session.DidCloseFile(ProtocolRequestContext.Default, URI("file://" + Main));
        OpenFile(session, "untitled:Untitled-1", "");
        session.WaitForBackgroundTasks();
        Check(ProjectParseEntryCount(session) == 0, "case-only cache entries released");
    }

    private static void DuplicateAcrossImportSitesRefCountedOnce()
    {
        const string directory = "/user/username/projects/myproject/src/";
        const string entryText = "import { dep } from './sub/dep';\nimport './a';\nimport './b';\nexport const e = dep;";
        using var fixture = new Fixture(new Input(directory + "entry.ts", entryText), new Input(directory + "a.ts", "import { dep } from './sub/DEP';\nexport const a = dep;"), new Input(directory + "b.ts", "import { dep } from './sub/DEP';\nexport const b = dep;"), new Input(directory + "sub/dep.ts", "export const dep = 1;"), new Input(directory + "c.ts", "export const c = 1;"));
        var session = fixture.Session;
        string entryPath = directory + "entry.ts";
        OpenFile(session, "file://" + entryPath, entryText);
        var program = CurrentProgram(session, entryPath);
        var duplicateKeys = new List<ParseCacheKey>();
        foreach (var duplicate in program.DuplicateSourceFiles())
            if (duplicate.ParseOptions.FileNameValue.EndsWith("/sub/DEP.ts"u8))
                duplicateKeys.Add(ParseCaches.KeyForDuplicate(duplicate));
        Check(duplicateKeys.Count == 1, "case-only duplicate recorded exactly once");
        var(duplicateEntry, exists) = session.SnapshotHost.parseCache.entries.Load(duplicateKeys[0]);
        Check(exists, "duplicate entry exists");
        Check(duplicateEntry.RefCount == 1, "duplicate retained once");
        Replace(session, entryPath, "import { dep } from \"./sub/dep\";\nimport \"./a\";\nimport \"./b\";\nimport \"./c\";\nexport const e = dep;");
        var rebuilt = CurrentProgram(session, entryPath);
        session.WaitForBackgroundTasks();
        foreach (var file in rebuilt.SourceFiles())
            Check(session.SnapshotHost.parseCache.entries.Load(ParseCaches.KeyForFile(file)).Ok, "live source references an existing cache entry");
        foreach (var duplicate in rebuilt.DuplicateSourceFiles())
            Check(session.SnapshotHost.parseCache.entries.Load(ParseCaches.KeyForDuplicate(duplicate)).Ok, "live duplicate references an existing cache entry");
        Replace(session, entryPath, "import { dep } from './sub/dep';\nimport './a';\nimport './b';\nimport './c';\nexport const e = dep + 0;", 3);
        CurrentProgram(session, entryPath);
        session.WaitForBackgroundTasks();
        session.DidCloseFile(ProtocolRequestContext.Default, URI("file://" + entryPath));
        OpenFile(session, "untitled:Untitled-1", "");
        session.WaitForBackgroundTasks();
        Check(ProjectParseEntryCount(session) == 0, "all duplicate/project entries released");
    }
}
