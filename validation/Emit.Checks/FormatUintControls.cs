// Authored boundary witnesses for the required Go strconv.FormatUint adapter.
using TypeScript.Scanner;
namespace Emit.Checks;
public static class FormatUintControls
{
    public static int RunAll()
    {
        (ulong value, int radix, string expected)[] cases =
        [
            (0, 2, "0"),
            (1024, 2, "10000000000"),
            (ulong.MaxValue, 2, "1111111111111111111111111111111111111111111111111111111111111111"),
            (ulong.MaxValue, 3, "11112220022122120101211020120210210211220"),
            (ulong.MaxValue, 10, "18446744073709551615"),
            (ulong.MaxValue, 16, "ffffffffffffffff"),
            (ulong.MaxValue, 36, "3w5e11264sgsf"),
        ];
        // Expected text was observed directly with the pinned Go toolchain strconv.
        foreach (var (value, radix, expected) in cases)
        {
            var actual = Strconv.FormatUint(value, radix);
            if (actual != expected)
                throw new InvalidOperationException($"FormatUint({value}, {radix}) expected {expected}, actual {actual}");
        }
        foreach (int radix in new[] { 1, 37 })
        {
            try
            {
                Strconv.FormatUint(0, radix);
            }
            catch (ArgumentOutOfRangeException exception) when (exception.ParamName == "numberBase")
            {
                continue;
            }
            throw new InvalidOperationException($"FormatUint accepted illegal radix {radix}");
        }
        return cases.Length + 2;
    }
}
