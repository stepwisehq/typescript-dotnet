// Complete original ignoredpaths_test.go assertions, including unrooted helper
// inputs. The existing source helper is invoked directly through friend visibility.
using Configuration.Checks;
using TypeScript.Scanner;

namespace Program.Checks;

public static class IgnoredPathChecks
{
    public static IReadOnlyList<string> Cases { get; } =
        ["TestContainsIgnoredPath", "TestIgnoredPathsPatterns", "TestIgnoredPathsEdgeCases"];

    public static void Run(string name)
    {
        (string Path, bool Want)[] rows = name switch
        {
            "TestContainsIgnoredPath" =>
            [
                ("/project/node_modules/.pnpm/file.ts", true),
                ("/project/.git/hooks/pre-commit", true),
                ("/project/src/file.ts.#", true),
                ("/project/src/file.ts", false),
                ("/project/node_modules/lodash/index.js", false),
                ("", false),
                ("/project/node_modules/.pnpm/.git/.#file.ts", true),
                ("/project/NODE_MODULES/.PNPM/file.ts", false),
                ("/project/src/node_modules/.pnpm/dist/file.js", true),
                ("/project/src/file.ts.#", true),
            ],
            "TestIgnoredPathsPatterns" =>
            [
                ("/test/node_modules/./file.ts", true),
                ("/test/.git/file.ts", true),
                ("/test.#/file.ts", true),
            ],
            "TestIgnoredPathsEdgeCases" =>
            [
                ("/node_modules./file.ts", false),
                ("/project/file.ts.#", true),
                ("/project/.git/node_modules./.git/file.ts", true),
                ("node_modules.file.ts", false),
                ("/file.ts", false),
            ],
            _ => throw new ArgumentException("Unknown original ignored-path test.", nameof(name)),
        };
        foreach (var row in rows)
            Check.Equal(TsPath.containsIgnoredPath(row.Path), row.Want, name + " " + row.Path);
    }
}
