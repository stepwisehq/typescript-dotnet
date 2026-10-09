// Copyright (c) Microsoft Corporation. Apache 2.0: licenses/TypeScript-LICENSE.txt.
// Complete selected original path_test.go assertion bodies; raw strings are Go bytes.
using TypeScript.Scanner;

namespace Configuration.Checks;

public static class PathHelperChecks
{
    public static IReadOnlyList<string> Cases { get; } =
    [
        "TestGetLongestExtensionFromPath",
        "TestRemoveAnyFileExtension",
        "TestCaseSensitivityTrimPrefix"
    ];

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestGetLongestExtensionFromPath":
                string[] extensions = [".z", ".y.z", ".other"];
                Check.Equal(TsPath.GetLongestExtensionFromPath("/src/Component.y.z", extensions, CaseSensitivity.CaseSensitive), ".y.z", "Original longest extension");
                Check.Equal(TsPath.GetLongestExtensionFromPath("/src/Component.z", extensions, CaseSensitivity.CaseSensitive), ".z", "Original single extension");
                Check.Equal(TsPath.GetLongestExtensionFromPath("/src/Component.y.Z", extensions, CaseSensitivity.CaseSensitive), "", "Original sensitive mismatch");
                Check.Equal(TsPath.GetLongestExtensionFromPath("/src/Component.y.Z", extensions, CaseSensitivity.CaseInsensitive), ".y.Z", "Original insensitive spelling");
                break;
            case "TestRemoveAnyFileExtension":
                Check.Equal(TsPath.RemoveAnyFileExtension("/src/Component.vue"), "/src/Component", "Original custom extension");
                Check.Equal(TsPath.RemoveAnyFileExtension("/src/Component.d.ts"), "/src/Component", "Original declaration extension");
                Check.Equal(TsPath.RemoveAnyFileExtension("/src/Component"), "/src/Component", "Original absent extension");
                break;
            case "TestCaseSensitivityTrimPrefix":
                foreach (var row in new (string Name, CaseSensitivity Sensitivity, string Text, string Prefix, string Suffix, bool Ok)[]
                {
                    ("case-sensitive exact match", CaseSensitivity.CaseSensitive, "/project/src/file.ts", "/project/src", "/file.ts", true),
                    ("case-sensitive mismatch", CaseSensitivity.CaseSensitive, "/project/SRC/file.ts", "/project/src", "/project/SRC/file.ts", false),
                    ("case-insensitive match", CaseSensitivity.CaseInsensitive, "/project/SRC/file.ts", "/project/src", "/file.ts", true),
                    ("no match", CaseSensitivity.CaseInsensitive, "/other/file.ts", "/project/src", "/other/file.ts", false),
                    ("case-folding shrinks prefix byte length without changing rune count", CaseSensitivity.CaseInsensitive, "/kkk/a.ts", "/\u212A\u212A\u212A", "/a.ts", true),
                    ("path equal to prefix", CaseSensitivity.CaseSensitive, "/project/src", "/project/src", "", true)
                })
                {
                    var actual = row.Sensitivity.TrimPrefix(GoString.ToUtf8(row.Text), GoString.ToUtf8(row.Prefix));
                    Check.Equal(actual.Ok, row.Ok, row.Name + " original match");
                    Check.Equal(actual.Remaining, GoString.ToUtf8(row.Suffix), row.Name + " original suffix");
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(name));
        }
    }
}
