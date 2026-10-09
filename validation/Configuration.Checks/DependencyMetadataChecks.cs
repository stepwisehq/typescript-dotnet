// Copyright (c) Microsoft Corporation. Apache 2.0: licenses/TypeScript-LICENSE.txt.
// Complete original metadata-only assertion bodies; no mapper host/process execution.
using TypeScript.Scanner;
using TypeScript.ContentMapper;

namespace Configuration.Checks;
public static class DependencyMetadataChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["TestPatternOverlappingMatch", "TestMapperDiagnosticName"];

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestPatternOverlappingMatch":
                var pattern = Core.TryParsePattern("ab*ab");
                Check.True(!pattern.Matches("ab"), "Original overlapping prefix/suffix must not match");
                Check.True(pattern.Matches("abXab"), "Original separated prefix/suffix");
                Check.Equal(pattern.MatchedText("abXab"), "X", "Original middle text");
                Check.True(pattern.Matches("abab"), "Original touching prefix/suffix");
                Check.Equal(pattern.MatchedText("abab"), "", "Original empty middle");
                break;
            case "TestMapperDiagnosticName":
                foreach (var row in new[]
                {
                    (new Mapper { Package = "configured", Name = "resolved", ContributionID = "contributed" }, "resolved"),
                    (new Mapper { Package = "configured", ContributionID = "contributed" }, "configured"),
                    (new Mapper { ContributionID = "contributed" }, "contributed")
                }

                )
                    Check.Equal(row.Item1.DiagnosticName(), row.Item2, "Original mapper diagnostic metadata name");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(name));
        }
    }
}
