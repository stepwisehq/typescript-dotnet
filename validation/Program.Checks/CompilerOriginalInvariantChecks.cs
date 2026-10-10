// Assertion adapter for the complete verifyParentPointers/verifyUnionOrdering
// bodies in internal/testrunner/compiler_runner.go:533/554 at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. The original checker pool scheduling
// is retained. Assertions compare identities within this runtime only;
// they emit no private graph or cross-runtime allocation identifiers.
// Original compiler_runner.go SHA-256:
// 1d0fe0e64d74bb1993de04851c86e108553ee4bbeb4dcde580e1c1820a89c3fa.
// RNG dependency SHA-256 (Go math/rand/v2):
// pcg.go 6ebd291420193e3d127c3c5bc46e7b3ee99eb5db6c3729e3ac9786c912a0ffa5;
// rand.go 8b097f91d5db88095c9ce8d52b0b5192827ede1ed3fbd1ed3aa0dfa5a717d558.
using TypeScript.Checking;
using TypeScript.Scanner;
using CompilerProgram = TypeScript.Compiler.Program;
using CheckerType = TypeScript.Checking.Type;

namespace Program.Checks;

public static class CompilerOriginalInvariantChecks
{
    public static void Validate(CompilerProgram program)
    {
        VerifyUnionOrdering(program);
        VerifyParentPointers(program);
    }

    public static void VerifyParentPointers(CompilerProgram program)
    {
        Node? parent = null;
        bool Verify(Node? node)
        {
            if (node is null) return false;
            Assert(node.Parent is not null, "parent node does not exist");
            string text = Ast.NodeIsSynthesized(node)
                ? "!synthetic! no text available"
                : Ast.GetSourceFileOfNode(node).Text()[node.Pos()..node.End()];
            Assert(ReferenceEquals(node.Parent, parent),
                "parent node does not match traversed parent: " + node.Kind.String() + ": " + text);
            Node? oldParent = parent;
            parent = node;
            node.ForEachChild(Verify);
            parent = oldParent;
            return false;
        }

        foreach (SourceFile file in program.GetSourceFiles())
        {
            if (program.IsSourceFileDefaultLibrary(PathKey.FromGoString(file.PathKey()))) continue;
            parent = file.AsNode();
            file.AsNode().ForEachChild(Verify);
        }
    }

    public static void VerifyUnionOrdering(CompilerProgram program)
    {
        program.ForEachCheckerParallel((_, checker) =>
        {
            foreach (CheckerType union in checker.UnionTypes())
            {
                List<CheckerType> types = union.Types();
                List<CheckerType> reversed = [.. types];
                reversed.Reverse();
                reversed.Sort(Checker.CompareTypes);
                if (!SameTypes(reversed, types))
                {
                    int mismatch = Enumerable.Range(0, types.Count).First(index => !ReferenceEquals(types[index], reversed[index]));
                    var expected = types[mismatch]; var actual = reversed[mismatch];
                    throw new InvalidOperationException("compareTypes does not sort union types consistently; first reversed-sort mismatch at " + mismatch +
                        "; expected public type=" + checker.TypeToString(expected) + "; actual public type=" + checker.TypeToString(actual) +
                        "; CompareTypes(expected,actual)=" + Checker.CompareTypes(expected, actual) +
                        "; CompareTypes(actual,expected)=" + Checker.CompareTypes(actual, expected) +
                        "; ReferenceEquals=" + ReferenceEquals(expected, actual));
                }

                List<CheckerType> shuffled = [.. types];
                var rng = new Pcg(1234, 5678);
                for (int repetition = 0; repetition < 10; repetition++)
                {
                    rng.Shuffle(shuffled);
                    shuffled.Sort(Checker.CompareTypes);
                    Assert(SameTypes(shuffled, types), "compareTypes does not sort union types consistently");
                }
            }
        });
    }

    private static bool SameTypes(List<CheckerType> left, List<CheckerType> right)
    {
        if (left.Count != right.Count) return false;
        for (int index = 0; index < left.Count; index++)
            if (!ReferenceEquals(left[index], right[index])) return false;
        return true;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    // Go math/rand/v2 pcg.go next/Uint64 and rand.go uint64n/Shuffle.
    // UInt128 preserves the original modulo-2^128 state arithmetic and 64-bit
    // DXSM/rejection sampling. BCL indexes are bounded int; no PRNG substitution.
    private sealed class Pcg(ulong high, ulong low)
    {
        private UInt128 state = ((UInt128)high << 64) | low;

        private ulong Next()
        {
            unchecked
            {
                UInt128 multiplier = ((UInt128)2549297995355413924UL << 64) | 4865540595714422341UL;
                UInt128 increment = ((UInt128)6364136223846793005UL << 64) | 1442695040888963407UL;
                state = state * multiplier + increment;
                ulong hi = (ulong)(state >> 64), lo = (ulong)state;
                hi ^= hi >> 32;
                hi *= 0xda942042e4dd58b5UL;
                hi ^= hi >> 48;
                hi *= lo | 1;
                return hi;
            }
        }

        private ulong Below(ulong count)
        {
            if ((count & (count - 1)) == 0) return Next() & (count - 1);
            UInt128 product = (UInt128)Next() * count;
            if ((ulong)product < count)
            {
                ulong threshold = unchecked(0UL - count) % count;
                while ((ulong)product < threshold) product = (UInt128)Next() * count;
            }
            return (ulong)(product >> 64);
        }

        public void Shuffle(List<CheckerType> items)
        {
            for (int index = items.Count - 1; index > 0; index--)
            {
                int other = (int)Below((ulong)index + 1);
                (items[index], items[other]) = (items[other], items[index]);
            }
        }
    }
}
