// Copyright (c) Microsoft Corporation. Apache 2.0: licenses/TypeScript-LICENSE.txt.
// Original finite fuzz seeds and original reference predicates; no mutation campaign.
using System.Text.RegularExpressions;
using TypeScript.Scanner;

namespace Configuration.Checks;

public static class PathFuzzSeedChecks
{
    public static IReadOnlyList<string> Cases { get; } =
    [
        "Adapted.FuzzHasRelativePathSegment.Seeds",
        "Adapted.FuzzToFileNameLowerCase.Seeds"
    ];

    public static void Run(string name)
    {
        switch (name)
        {
            case "Adapted.FuzzHasRelativePathSegment.Seeds":
                foreach (string input in new[]
                {
                    "//", "foo/bar/baz", "foo/./baz", "foo/../baz", "foo/bar/baz/.",
                    "./some/path", "/foo//bar/", "/foo/./bar/../../.", string.Concat(Enumerable.Repeat("foo/", 100)) + ".."
                })
                {
                    bool originalReference = Regex.IsMatch(input, @"//|(?:^|/)\.\.?(?:$|/)", RegexOptions.CultureInvariant);
                    Check.Equal(TsPath.hasRelativePathSegment(input), originalReference, "Original relative-segment fuzz seed " + input);
                }
                break;
            case "Adapted.FuzzToFileNameLowerCase.Seeds":
                foreach (string input in new[]
                {
                    "/path/to/file.ext", "/PATH/TO/FILE.EXT", "/path/to/FILE.EXT",
                    "/user/UserName/projects/Project/file.ts", "/user/UserName/projects/projectß/file.ts",
                    "/user/UserName/projects/İproject/file.ts", "/user/UserName/projects/ı/file.ts",
                    string.Concat(Enumerable.Repeat("FoO/", 100))
                })
                {
                    // In all original seeds, matches contain only ASCII uppercase letters.
                    // Ignored Unicode scalars remain untouched by the source pattern.
                    string reference = Regex.Replace(input, @"[^\u0130\u0131\u00DFa-z0-9\\/:\-_. ]+",
                        match => match.Value.ToLowerInvariant(), RegexOptions.CultureInvariant);
                    Check.Equal(TsPath.ToFileNameLowerCaseGoBytes(GoString.ToUtf8(input)),
                        GoString.ToUtf8(reference), "Original filename-lowercase fuzz seed " + input);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(name));
        }
    }
}
