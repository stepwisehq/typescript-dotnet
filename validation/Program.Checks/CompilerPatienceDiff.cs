// Copyright (c) 2022 Peter Evans. MIT license: licenses/Patience-LICENSE.txt.
// Modified direct port of github.com/peter-evans/patience v0.3.0:
// patience.go, lcs.go, unified.go and demanded format.go unified renderer.
// Public wrapper maps TypeScript internal/testutil/baseline/baseline.go:31.
using TypeScript.Scanner;

namespace Program.Checks;
public static partial class CompilerEmitBaselines
{
    public static string DiffText(string oldName, string newName, string expected, string actual)
    {
        var a = StringUtil.SplitLines(ByteStringValue.FromString(expected));
        var b = StringUtil.SplitLines(ByteStringValue.FromString(actual));
        var diffs = PatienceDiff(a.AsSpan(), b.AsSpan());
        var lines = new List<string>();
        if (oldName.Length != 0)
            lines.Add("--- " + oldName);
        if (newName.Length != 0)
            lines.Add("+++ " + newName);
        foreach (var hunk in PatienceMakeHunks(diffs, 3, 3))
        {
            lines.Add(FormattableString.Invariant($"@@ -{hunk.SrcStart},{hunk.SrcLines} +{hunk.DstStart},{hunk.DstLines} @@"));
            foreach (var line in hunk.Diffs)
            {
                string text = line.Text.Materialize()!;
                lines.Add(line.Type == 0 && text.Length == 0 ? "" : PatienceTypeSymbol(line.Type) + text);
            }
        }

        return string.Join('\n', lines);
    }

    private readonly record struct PatienceLine(ByteStringValue Text, int Type);
    private sealed class PatienceHunk
    {
        public Slice<PatienceLine> Diffs;
        public int SrcStart, SrcLines, DstStart, DstLines;
    }

    private static string PatienceTypeSymbol(int type) => type switch
    {
        -1 => "-",
        0 => " ",
        1 => "+",
        _ => throw new InvalidOperationException("unknown DiffType")};
    // patience.go:toDiffLines. Required owned result, retained borrowed line headers.
    private static List<PatienceLine> PatienceToDiffLines(ReadOnlySpan<ByteStringValue> lines, int type)
    {
        var result = new List<PatienceLine>(lines.Length);
        foreach (var line in lines)
            result.Add(new(line, type));
        return result;
    }

    private static (Slice<ByteStringValue> Elements, Slice<int> Indices) PatienceUniqueElements(ReadOnlySpan<ByteStringValue> lines)
    {
        var counts = new Dictionary<ByteStringKey, int>();
        foreach (var line in lines)
        {
            var key = ByteStringKey.FromValue(line);
            counts[key] = counts.GetValueOrDefault(key) + 1;
        }

        var elements = Slice<ByteStringValue>.Make(0);
        var indices = Slice<int>.Make(0);
        for (int i = 0; i < lines.Length; i++)
            if (counts[ByteStringKey.FromValue(lines[i])] == 1)
            {
                elements = elements.Append(lines[i]);
                indices = indices.Append(i);
            }

        return (elements, indices);
    }

    // patience.go:Diff. Exact recursive head/tail/unique-LCS gap ordering.
    private static List<PatienceLine> PatienceDiff(ReadOnlySpan<ByteStringValue> a, ReadOnlySpan<ByteStringValue> b)
    {
        if (a.Length == 0)
            return PatienceToDiffLines(b, 1);
        if (b.Length == 0)
            return PatienceToDiffLines(a, -1);
        int i = 0;
        while (i < a.Length && i < b.Length && a[i].EqualsValue(b[i]))
            i++;
        if (i > 0)
        {
            var result = PatienceToDiffLines(a[..i], 0);
            result.AddRange(PatienceDiff(a[i..], b[i..]));
            return result;
        }

        int j = 0;
        while (j < a.Length && j < b.Length && a[^(j + 1)].EqualsValue(b[^(j + 1)]))
            j++;
        if (j > 0)
        {
            var result = PatienceDiff(a[..^j], b[..^j]);
            result.AddRange(PatienceToDiffLines(a[^j..], 0));
            return result;
        }

        var ua = PatienceUniqueElements(a);
        var ub = PatienceUniqueElements(b);
        var lcs = PatienceLcs(ua.Elements.AsSpan(), ub.Elements.AsSpan());
        if (lcs.Count == 0)
        {
            var result = PatienceToDiffLines(a, -1);
            result.AddRange(PatienceToDiffLines(b, 1));
            return result;
        }

        for (i = 0; i < lcs.Count; i++)
            lcs[i] = (ua.Indices[lcs[i].A], ub.Indices[lcs[i].B]);
        var diffs = new List<PatienceLine>();
        int ga = 0, gb = 0;
        foreach (var pair in lcs)
        {
            diffs.AddRange(PatienceDiff(a[ga..pair.A], b[gb..pair.B]));
            diffs.Add(new(a[pair.A], 0));
            ga = pair.A + 1;
            gb = pair.B + 1;
        }

        diffs.AddRange(PatienceDiff(a[ga..], b[gb..]));
        return diffs;
    }

    // lcs.go:LCS. Required table and exact tie-breaking/backtracking reversal.
    private static List<(int A, int B)> PatienceLcs(ReadOnlySpan<ByteStringValue> a, ReadOnlySpan<ByteStringValue> b)
    {
        var table = new int[a.Length + 1][];
        for (int i = 0; i <= a.Length; i++)
            table[i] = new int[b.Length + 1];
        for (int i = 1; i <= a.Length; i++)
            for (int j = 1; j <= b.Length; j++)
                table[i][j] = a[i - 1].EqualsValue(b[j - 1]) ? table[i - 1][j - 1] + 1 : Math.Max(table[i - 1][j], table[i][j - 1]);
        int x = a.Length, y = b.Length;
        var result = new List<(int A, int B)>(table[x][y]);
        while (x > 0 && y > 0)
        {
            if (a[x - 1].EqualsValue(b[y - 1]))
            {
                result.Add((x - 1, y - 1));
                x--;
                y--;
            }
            else if (table[x - 1][y] > table[x][y - 1])
                x--;
            else
                y--;
        }

        result.Reverse();
        return result;
    }

    // unified.go:makeHunks. Heap owner represents mutable Go hunk slice element.
    private static List<PatienceHunk> PatienceMakeHunks(List<PatienceLine> diffs, int precontext, int postcontext)
    {
        var hunks = new List<PatienceHunk>();
        if (diffs.Count == 0)
            return hunks;
        void UpdateHunks(PatienceHunk block, bool lastBlock)
        {
            int current = hunks.Count - 1;
            if (block.Diffs[0].Type == 0)
            {
                if (hunks.Count == 0)
                {
                    int length = Math.Min(precontext, block.Diffs.Count);
                    hunks.Add(new() { Diffs = block.Diffs.Slice2(block.Diffs.Count - length), SrcStart = block.Diffs.Count - length + block.SrcStart, SrcLines = length, DstStart = block.Diffs.Count - length + block.DstStart, DstLines = length });
                }
                else
                {
                    int maxNonContext = lastBlock ? postcontext : precontext + postcontext;
                    var hunk = hunks[current];
                    if (block.Diffs.Count <= maxNonContext)
                    {
                        hunk.Diffs = hunk.Diffs.Append(block.Diffs);
                        hunk.SrcLines += block.Diffs.Count;
                        hunk.DstLines += block.Diffs.Count;
                    }
                    else
                    {
                        hunk.Diffs = hunk.Diffs.Append(block.Diffs.Slice2(0, postcontext));
                        hunk.SrcLines += postcontext;
                        hunk.DstLines += postcontext;
                        if (!lastBlock)
                            hunks.Add(new() { Diffs = block.Diffs.Slice2(block.Diffs.Count - precontext), SrcStart = block.Diffs.Count - precontext + block.SrcStart, SrcLines = precontext, DstStart = block.Diffs.Count - precontext + block.DstStart, DstLines = precontext });
                    }

                    if (hunk.SrcStart == 0)
                        hunk.SrcStart = block.SrcStart;
                    if (hunk.DstStart == 0)
                        hunk.DstStart = block.DstStart;
                }
            }
            else if (hunks.Count > 0)
            {
                var hunk = hunks[current];
                hunk.Diffs = hunk.Diffs.Append(block.Diffs);
                hunk.SrcLines += block.SrcLines;
                hunk.DstLines += block.DstLines;
            }
            else
                hunks.Add(block);
        }

        var block = new PatienceHunk();
        int modified = 0, srcLine = 0, dstLine = 0;
        foreach (var line in diffs)
        {
            if (block.Diffs.Count == 0 || block.Diffs[0].Type == line.Type || block.Diffs[0].Type != 0 && line.Type != 0)
                block.Diffs = block.Diffs.Append(line);
            else
            {
                UpdateHunks(block, false);
                block = new()
                {
                    Diffs = Slice<PatienceLine>.Make(0, 1).Append(line)
                };
            }

            switch (line.Type)
            {
                case -1:
                    srcLine++;
                    block.SrcLines++;
                    modified++;
                    break;
                case 1:
                    dstLine++;
                    block.DstLines++;
                    modified++;
                    break;
                case 0:
                    srcLine++;
                    dstLine++;
                    block.SrcLines++;
                    block.DstLines++;
                    break;
            }

            if (block.SrcStart == 0 && line.Type is 0 or -1)
                block.SrcStart = srcLine;
            if (block.DstStart == 0 && line.Type is 0 or 1)
                block.DstStart = dstLine;
        }

        UpdateHunks(block, true);
        return modified == 0 ? new() : hunks;
    }
}
