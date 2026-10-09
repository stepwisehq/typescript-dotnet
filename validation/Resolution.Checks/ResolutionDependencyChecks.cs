// Authored source controls, not upstream bodies: core/core.go has no direct
// Deduplicate unit body. Stable order, header/storage aliasing and mutation matter.
using Configuration.Checks;
using TypeScript.Scanner;

namespace Resolution.Checks;
public static class ResolutionDependencyChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["Core.Deduplicate.NilAliasAndMutation"];

    public static void Run(string name)
    {
        if (name != Cases[0])
            throw new ArgumentException("Unknown authored dependency control.", nameof(name));
        Check.True(Core.Deduplicate<string>(null)is null, "Source nil slice remains nil");
        foreach (List<string> input in new List<string>[]
        {
            [],
            ["a"],
            ["a", "b", "c"]
        }

        )
        {
            List<string>? output = Core.Deduplicate(input);
            Check.True(ReferenceEquals(output, input), "Source no-duplicate header keeps original storage");
            Check.Sequence(output, input, "Source no-duplicate values");
        }

        foreach (var row in new (List<string> Input, string[] Expected)[]
        {
            (["a", "a"], ["a"]),
            (["a", "b", "a", "c", "b", "d"], ["a", "b", "c", "d"]),
            (["", "b", ""], ["", "b"])
        }

        )
        {
            string[] before = row.Input.ToArray();
            List<string>? output = Core.Deduplicate(row.Input);
            Check.True(!ReferenceEquals(output, row.Input), "Source first duplicate copies prefix storage");
            Check.Sequence(output, row.Expected, "Source stable first-occurrence order");
            Check.Sequence(row.Input, before, "Source input retained after deduplication");
            output![0] = "changed-output";
            Check.Sequence(row.Input, before, "Source copied result mutation does not change input");
        }
    }
}
