// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete original registry_test.go:946, actual component-mapper test spawner.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using TypeScript.ProjectSystem;
using BaselineFS = Program.Checks.CompilerBaselineFileSystem;

namespace LanguageServices.Checks;
public static partial class AutoImportRegistryChecks
{
    private static void TestContentMappedNodeModulesFileUsesProjectBucket()
    {
        BaselineFS.Input[] files = [new("/home/project/tsconfig.json", "{\"compilerOptions\":{\"module\":\"esnext\",\"moduleResolution\":\"bundler\",\"strict\":true,\"skipLibCheck\":true},\"contentMappers\":[{\"package\":\"mapper\",\"extensions\":[\".vue\"]}]}"), new("/home/project/node_modules/mapper/package.json", ContentMapperComponentFixture.PackageJSON), new("/home/project/node_modules/profile-package/ProfileCard.vue", "<component name=\"ProfileCard\">\n<script lang=\"ts\">\nexport const profileTitle = \"Profile\";\n</script>"), new("/home/project/node_modules/profile-package/HiddenCard.vue", "<component name=\"HiddenCard\">\n<script lang=\"ts\">\nexport const hiddenTitle = \"Hidden\";\n</script>"), new("/home/project/node_modules/profile-package/ordinary.ts", "export const ordinary = true;"), new("/home/project/load.ts", "import \"profile-package/ProfileCard.vue\";\nimport \"profile-package/ordinary\";"), new("/home/project/main.ts", "profileTitle;")];
        var options = new SessionOptions
        {
            CurrentDirectory = RootedDirectoryPath.FromGoString("/home/project"),
            DefaultLibraryPath = TypeScript.Bundled.Bundled.LibPath(),
            TypingsLocation = RootedDirectoryPath.FromGoString("/home/src/Library/Caches/typescript"),
            PositionEncoding = PositionEncodingKind.UTF8,
            RunExternalCode = true
        };
        using var fixture = new ProjectSessionFixture(files, options, spawner: ContentMapperComponentFixture.NewSpawner());
        var uri = new DocumentUri(ByteStringValue.FromString("file:///home/project/main.ts"));
        Open(fixture, uri, "profileTitle;");
        AutoImports(fixture, uri);
        fixture.Session.WaitForBackgroundTasks();
        Require(SingleBucket(Stats(fixture).ProjectBuckets).FileCount == 3, "expected the two project roots and referenced mapped package file");
    }
}
