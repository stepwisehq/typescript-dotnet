internal readonly record struct CliArguments(bool Json, bool NoEmit, bool NoGcRegion)
{
    internal static bool TryParse(string[] args, out CliArguments options)
    {
        options = default;
        if (args.Length < 2 || args[0] is not
            ("--project" or "--config" or "--inspect-config" or "--inspect-project" or "--inspect-program"))
            return false;
        bool json = false, noEmit = false, noGcRegion = false;
        foreach (string argument in args.Skip(2))
        {
            if (argument == "--json" && !json) json = true;
            else if (argument == "--noEmit" && args[0] == "--config" && !noEmit) noEmit = true;
            else if (argument == "--no-gc-region" && args[0] == "--config" && !noGcRegion) noGcRegion = true;
            else return false;
        }
        options = new(json, noEmit, noGcRegion);
        return true;
    }
}
