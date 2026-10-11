// Complete pinned checkerpool_test.go assertions and table domains. Only test
// scheduling and bounded array indices adapt the Go runtime representation.
using Configuration.Checks;
using TypeScript.Compiler;
using TypeScript.Modules;
using TypeScript.Scanner;
using CompilerProgram = TypeScript.Compiler.Program;

namespace Program.Checks;

public static class CheckerPoolChecks
{
    public static IReadOnlyList<string> Cases { get; } =
    [
        "TestGetSourceFileForResolvedModuleUsesResolvedPath",
        "TestGetCheckerAssociationBaseWeight", "TestShouldPrioritizeSourceFiles",
        "TestGetCheckerAssociationPolicy", "TestGetCheckerAssociationOrder",
        "TestGetCheckerAssociationWeights", "TestGetCheckerAssociations",
    ];

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestGetSourceFileForResolvedModuleUsesResolvedPath":
            {
                PathKey path = PathKey.FromGoString("/resolved.ts");
                SourceFile file = new();
                // Existing compiler tables retain canonical PathKey content keys.
                CompilerProgram program = new() { filesByPath = new() { [ByteStringKey.FromValue(path.AsValue())] = file } };
                ResolvedModule resolved = new()
                {
                    ResolvedFileName = RootedFilePath.FromGoString("/different.ts"),
                    ResolvedPath = path,
                };
                Check.True(ReferenceEquals(program.GetSourceFileForResolvedModule(resolved), file), "lookup uses actual ResolvedPath");
                break;
            }
            case "TestGetCheckerAssociationBaseWeight":
                Check.Equal(CheckerPool.getCheckerAssociationBaseWeight(100, 2500), 125L, name);
                break;
            case "TestShouldPrioritizeSourceFiles":
                Check.True(CheckerPool.shouldPrioritizeSourceFiles(1000, 100, 4), "source dominated");
                Check.True(CheckerPool.shouldPrioritizeSourceFiles(1000, 125, 4), "boundary inclusive");
                Check.True(!CheckerPool.shouldPrioritizeSourceFiles(1000, 126, 4), "above boundary");
                break;
            case "TestGetCheckerAssociationPolicy":
                Policy();
                break;
            case "TestGetCheckerAssociationOrder":
                Check.Sequence(CheckerPool.getCheckerAssociationOrder(Slice<long>.FromArray([5, 10, 7, 2]), Slice<bool>.FromArray([true, false, false, true]), true).AsSpan().ToArray(), [1, 2, 0, 3], "prioritized order");
                Check.True(CheckerPool.getCheckerAssociationOrder(Slice<long>.FromArray([5]), Slice<bool>.FromArray([false]), false).IsNil, "unprioritized nil order");
                break;
            case "TestGetCheckerAssociationWeights":
                Check.Sequence(CheckerPool.getCheckerAssociationWeights(Slice<long>.FromArray([100, 50, 25]), Slice<long>.FromArray([0, 1, 3])).AsSpan().ToArray(), [100L, 93L, 154L], "normalizes import work");
                Check.Sequence(CheckerPool.getCheckerAssociationWeights(Slice<long>.FromArray([100, 50, 25]), Slice<long>.FromArray([0, 0, 0])).AsSpan().ToArray(), [100L, 50L, 25L], "no imports");
                break;
            case "TestGetCheckerAssociations":
                Associations();
                break;
            default:
                throw new ArgumentException("Unknown original checkerpool test.", nameof(name));
        }
    }

    private static void Policy()
    {
        (string Name, long Total, long Declaration, long Count, CheckerAssociationPolicy Want)[] rows =
        [
            ("source dominated at any checker count", 1000, 100, 2, new(true, 1, CheckerPool.checkerAssociationPrioritizedSourcePenalty)),
            ("declaration heavy with few checkers", 1000, 400, 2, new(false, 1, 1)),
            ("source dominated with many checkers", 1000, 50, 8, new(true, 1, CheckerPool.checkerAssociationPrioritizedSourcePenalty)),
            ("declaration heavy with many checkers", 1000, 400, 4, new(false, CheckerPool.checkerAssociationSourceFileWeightMultiplier, CheckerPool.checkerAssociationBalancePenaltyMultiplier)),
        ];
        foreach (var row in rows)
            Check.Equal(CheckerPool.getCheckerAssociationPolicy(row.Total, row.Declaration, row.Count), row.Want, row.Name);
    }

    private static Slice<Slice<int>> Disconnected(int count) => Slice<Slice<int>>.Make(count);

    private static void Associations()
    {
        Check.True(CheckerPool.getCheckerAssociationsInOrder(default, default, default, 4, CheckerPool.checkerAssociationBalancePenaltyMultiplier).IsNil, "empty nil inputs");
        Check.Sequence(CheckerPool.getCheckerAssociationsInOrder(Slice<long>.FromArray([1, 1, 1, 1, 1, 1]), Disconnected(6), default, 3, 1).AsSpan().ToArray(), [0, 1, 2, 0, 1, 2], "balances disconnected files");
        Check.Sequence(CheckerPool.getCheckerAssociationsInOrder(Slice<long>.FromArray([1, 3, 2]), Disconnected(3), default, 2, 1).AsSpan().ToArray(), [0, 1, 0], "uses program order");
        Check.Sequence(CheckerPool.getCheckerAssociationsInOrder(
            Slice<long>.FromArray([1, 1, 1, 1, 1, 1]),
            Slice<Slice<int>>.FromArray([
                Slice<int>.FromArray([1, 2]), Slice<int>.FromArray([0, 2]), Slice<int>.FromArray([0, 1]),
                Slice<int>.FromArray([4, 5]), Slice<int>.FromArray([3, 5]), Slice<int>.FromArray([3, 4])]),
            default, 2, 1).AsSpan().ToArray(), [0, 0, 0, 1, 1, 1], "keeps dense components together");
        Slice<long> weights = Slice<long>.FromArray([8, 7, 6, 5, 4, 3, 2, 1]);
        Slice<int> associations = CheckerPool.getCheckerAssociationsInOrder(weights, Disconnected(weights.Length), default, 3, 1);
        long[] loads = new long[3];
        for (int i = 0; i < associations.Length; i++) loads[associations[i]] += weights[i];
        for (int i = 0; i < loads.Length; i++) Check.True(loads[i] <= 13, "weighted balance cap checker " + i);
    }
}
