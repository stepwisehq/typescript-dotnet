// Copyright 2011 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// regexp/syntax/prog_test.go TestCompile, unchanged original compiled-program strings.
// The test-only dump observer copies prog.go:296–358; production printing APIs stay outside scope.
using System.Globalization;
using System.Text;
using TypeScript.Scanner;
using TypeScript.RegularExpressions.Syntax;
namespace LanguageServices.Checks;
public static class RegexCompileChecks
{
    private static string Raw(string text) => ByteStringView.FromUnicode(text).Materialize();
    private static readonly (string Pattern, string Expected)[] Rows = [
        ("a", "  0\tfail\n  1*\trune1 \"a\" -> 2\n  2\tmatch\n"),
        ("[A-M][n-z]", "  0\tfail\n  1*\trune \"AM\" -> 2\n  2\trune \"nz\" -> 3\n  3\tmatch\n"),
        ("", "  0\tfail\n  1*\tnop -> 2\n  2\tmatch\n"),
        ("a?", "  0\tfail\n  1\trune1 \"a\" -> 3\n  2*\talt -> 1, 3\n  3\tmatch\n"),
        ("a??", "  0\tfail\n  1\trune1 \"a\" -> 3\n  2*\talt -> 3, 1\n  3\tmatch\n"),
        ("a+", "  0\tfail\n  1*\trune1 \"a\" -> 2\n  2\talt -> 1, 3\n  3\tmatch\n"),
        ("a+?", "  0\tfail\n  1*\trune1 \"a\" -> 2\n  2\talt -> 3, 1\n  3\tmatch\n"),
        ("a*", "  0\tfail\n  1\trune1 \"a\" -> 2\n  2*\talt -> 1, 3\n  3\tmatch\n"),
        ("a*?", "  0\tfail\n  1\trune1 \"a\" -> 2\n  2*\talt -> 3, 1\n  3\tmatch\n"),
        ("a+b+", "  0\tfail\n  1*\trune1 \"a\" -> 2\n  2\talt -> 1, 3\n  3\trune1 \"b\" -> 4\n  4\talt -> 3, 5\n  5\tmatch\n"),
        ("(a+)(b+)", "  0\tfail\n  1*\tcap 2 -> 2\n  2\trune1 \"a\" -> 3\n  3\talt -> 2, 4\n  4\tcap 3 -> 5\n  5\tcap 4 -> 6\n  6\trune1 \"b\" -> 7\n  7\talt -> 6, 8\n  8\tcap 5 -> 9\n  9\tmatch\n"),
        ("a+|b+", "  0\tfail\n  1\trune1 \"a\" -> 2\n  2\talt -> 1, 6\n  3\trune1 \"b\" -> 4\n  4\talt -> 3, 6\n  5*\talt -> 1, 3\n  6\tmatch\n"),
        ("A[Aa]", "  0\tfail\n  1*\trune1 \"A\" -> 2\n  2\trune \"A\"/i -> 3\n  3\tmatch\n"),
        ("(?:(?:^).)", "  0\tfail\n  1*\tempty 4 -> 2\n  2\tanynotnl -> 3\n  3\tmatch\n"),
        ("(?:|a)+", "  0\tfail\n  1\tnop -> 4\n  2\trune1 \"a\" -> 4\n  3*\talt -> 1, 2\n  4\talt -> 3, 5\n  5\tmatch\n"),
        ("(?:|a)*", "  0\tfail\n  1\tnop -> 4\n  2\trune1 \"a\" -> 4\n  3\talt -> 1, 2\n  4\talt -> 3, 6\n  5*\talt -> 3, 6\n  6\tmatch\n"),
    ];
    public static int RunAll()
    {
        foreach (var row in Rows)
        {
            var (expression, error) = RegexParser.Parse(ByteStringValue.FromString(row.Pattern), RegexFlags.Perl);
            if (error is not null || expression is null)
                throw new InvalidOperationException("Original compiler pattern rejected");
            string actual = Dump(RegexCompiler.Compile(expression));
            if (actual != row.Expected)
                throw new InvalidOperationException("Original compiler dump differs: " + row.Pattern);
        }
        return 1;
    }
    private static string Number(uint value) => value.ToString(CultureInfo.InvariantCulture);
    private static string Dump(RegexProgram program)
    {
        var output = new StringBuilder();
        for (int index = 0; index < program.Inst.Count; index++)
        {
            string pc = index.ToString(CultureInfo.InvariantCulture);
            if (pc.Length < 3)
                output.Append("   ".AsSpan(pc.Length));
            output.Append(pc);
            if (index == program.Start)
                output.Append('*');
            output.Append('\t');
            var instruction = program.Inst[index];
            string tail = " -> " + Number(instruction.Out);
            switch (instruction.Op)
            {
                case InstructionOp.Alt: output.Append("alt" + tail + ", " + Number(instruction.Arg)); break;
                case InstructionOp.AltMatch: output.Append("altmatch" + tail + ", " + Number(instruction.Arg)); break;
                case InstructionOp.Capture: output.Append("cap " + Number(instruction.Arg) + tail); break;
                case InstructionOp.EmptyWidth: output.Append("empty " + Number(instruction.Arg) + tail); break;
                case InstructionOp.Match: output.Append("match"); break;
                case InstructionOp.Fail: output.Append("fail"); break;
                case InstructionOp.Nop: output.Append("nop" + tail); break;
                case InstructionOp.Rune:
                    if (instruction.Rune.IsNil) output.Append("rune <nil>");
                    output.Append("rune "); Quote(output, instruction.Rune);
                    if (((RegexFlags)instruction.Arg & RegexFlags.FoldCase) != 0) output.Append("/i");
                    output.Append(tail); break;
                case InstructionOp.Rune1: output.Append("rune1 "); Quote(output, instruction.Rune); output.Append(tail); break;
                case InstructionOp.RuneAny: output.Append("any" + tail); break;
                case InstructionOp.RuneAnyNotNL: output.Append("anynotnl" + tail); break;
            }
            output.Append('\n');
        }
        return output.ToString();
    }
    // Exact QuoteToASCII transport for rune-array strings in the original program dumps.
    private static void Quote(StringBuilder output, RegexSlice<int> runes)
    {
        output.Append('"');
        for (int index = 0; index < runes.Count; index++)
        {
            int rune = runes[index];
            switch (rune)
            {
                case '"': output.Append("\\\""); break;
                case '\\': output.Append("\\\\"); break;
                case 7: output.Append("\\a"); break;
                case 8: output.Append("\\b"); break;
                case 9: output.Append("\\t"); break;
                case 10: output.Append("\\n"); break;
                case 11: output.Append("\\v"); break;
                case 12: output.Append("\\f"); break;
                case 13: output.Append("\\r"); break;
                default:
                    if (rune >= 32 && rune < 127) output.Append((char)rune);
                    else if (rune < 128) output.Append("\\x" + rune.ToString("x2", CultureInfo.InvariantCulture));
                    else if (rune <= 0xffff) output.Append("\\u" + rune.ToString("x4", CultureInfo.InvariantCulture));
                    else output.Append("\\U" + rune.ToString("x8", CultureInfo.InvariantCulture));
                    break;
            }
        }
        output.Append('"');
    }
}
