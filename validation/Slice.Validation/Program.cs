using Program.Checks;

namespace Slice.Validation;

internal static class Entry
{
    private static int Main()
    {
        try
        {
            int count = SliceConformanceChecks.RunAll();
            Console.WriteLine($"slice-checks: {count} passed");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }
}
