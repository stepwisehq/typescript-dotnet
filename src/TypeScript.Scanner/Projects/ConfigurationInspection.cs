// Authored disk integration over the complete TsOptions interpreter. Inspection
// preserves interpreted settings and discovered files without checker admission.
using TypeScript.Json;
using TypeScript.Scanner;
using TypeScript.TsOptions;
using TypeScript.Vfs.OsVfs;
using ConfigParser = TypeScript.TsOptions.TsOptions;

namespace TypeScript.Compiler;
public sealed class ConfigurationInspection
{
    public string ConfigurationPath { get; }
    public ParsedCommandLine? Parsed { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    internal ConfigurationInspection(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.IndexOf('\0') >= 0)
            throw new ProjectException("configurationPath", "The configuration path must be nonempty and contain no NUL characters.", path);
        ConfigurationPath = TsPath.GetNormalizedAbsolutePath(path, Directory.GetCurrentDirectory());
        RootedFilePath file;
        try
        {
            file = RootedFilePath.FromUnicode(ConfigurationPath);
        }
        catch (System.Text.EncoderFallbackException ex)
        {
            throw new ProjectException("pathEncoding", ex.Message, path);
        }

        var(parsed, errors) = ConfigParser.GetParsedCommandLineOfConfigFile(file, null, null, OsVfs.FS());
        Parsed = parsed;
        Diagnostics = (parsed?.GetConfigFileParsingDiagnostics() ?? errors ?? []).AsReadOnly();
    }

    // This is the original show-config projection, not a reconstruction of raw
    // input or a fixed checker profile. JSON strings contain ordinary UTF8 bytes.
    public byte[] ShowConfigurationJSON() => Parsed is null ? JsonCodec.Marshal(null) : JsonCodec.Marshal(ConfigParser.ConvertToTSConfig(Parsed, RootedFilePath.FromUnicode(ConfigurationPath)));
    internal void RequireValid()
    {
        if (Diagnostics.Count != 0)
            throw new ConfigurationDiagnosticsException(Diagnostics);
        if (Parsed is null)
            throw new InvalidOperationException("Configuration parsing returned neither a result nor a diagnostic.");
    }
}

// Configuration diagnostics precede semantic admission, including when the
// same input also requests checker settings that are outside its current scope.
public sealed class ConfigurationDiagnosticsException : Exception
{
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    internal ConfigurationDiagnosticsException(IReadOnlyList<Diagnostic> diagnostics) : base("The configuration contains compiler diagnostics.") => Diagnostics = diagnostics;
}
