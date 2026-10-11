// Authored strict tscheck.json subset; not a tsconfig/JSONC implementation.
using TypeScript.Collections;
using TypeScript.Json;
using TypeScript.TsOptions;
using TypeScript.Scanner;

namespace TypeScript.Compiler;
internal sealed class ProjectConfiguration
{
    internal string FileName { get; }
    internal string Root { get; }
    internal SourceFile SourceFile { get; }
    internal IReadOnlyList<string> Files { get; }
    internal CompilerOptions Options { get; } = CreateOptions();
    internal ParsedCommandLine? Parsed { get; }

    internal static CompilerOptions CreateOptions() => new()
    {
        Target = ScriptTarget.ESNext,
        Module = ModuleKind.ESNext,
        ModuleResolution = ModuleResolutionKind.Bundler,
        NoEmit = Tristate.True,
        NoLib = Tristate.True,
        NoResolve = Tristate.False,
        SkipLibCheck = Tristate.True,
        AllowImportingTsExtensions = Tristate.True,
        NoUncheckedSideEffectImports = Tristate.True,
        ResolveJsonModule = Tristate.False,
        AllowJs = Tristate.False,
        StrictNullChecks = Tristate.True,
        StrictFunctionTypes = Tristate.True,
        NoImplicitAny = Tristate.True,
        NoImplicitThis = Tristate.True,
        StrictBindCallApply = Tristate.False,
        NoImplicitReturns = Tristate.False,
        ExactOptionalPropertyTypes = Tristate.False,
        NoUncheckedIndexedAccess = Tristate.False,
        StrictPropertyInitialization = Tristate.False,
        StrictBuiltinIteratorReturn = Tristate.False,
        UseUnknownInCatchVariables = Tristate.False,
        ForceConsistentCasingInFileNames = Tristate.True,
        Types = []
    };
    internal ProjectConfiguration(string path)
    {
        FileName = TsPath.GetNormalizedAbsolutePath(path, Directory.GetCurrentDirectory());
        ProjectDiskHost.CheckSymlinks(FileName);
        Root = TsPath.GetDirectoryPath(FileName);
        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(FileName);
        }
        catch (IOException ex)
        {
            throw new ProjectException("configurationRead", ex.Message, FileName);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new ProjectException("configurationRead", ex.Message, FileName);
        }

        // Own identity outside Program.SourceFiles, just as the original config AST.
        SourceFile = Parser.ParseSourceFile(new SourceFileParseOptions(FileName, FileName), bytes, ScriptKind.JSON);
        // The compatibility policy is deliberately strict JSON, but decoding uses
        // the shared raw-value adapter rather than a second configuration parser.
        ValidateCompatibilityStrings(SourceFile.AsNode());
        var(value, error) = JsonCodec.Parse(bytes, allowDuplicateNames: true);
        if (error is not null)
            throw new ProjectException(error is InvalidOperationException ? "configuration" : "configurationJson", error.Message, FileName);
        if (value is not OrderedMap<string, object?> root)
            throw Error("configuration must be an object.");
        RequireUniqueProperties(SourceFile.AsNode());
        object? files = null;
        foreach (var(name, item)in root.Entries())
        {
            switch (name)
            {
                case "files":
                    files = item;
                    break;
                case "compilerOptions":
                    ValidateOptions(item);
                    break;
                default:
                    throw Error("Unsupported configuration property: " + Decode(name));
            }
        }

        if (files is not List<object?> array || array.Count == 0)
            throw Error("files must be a nonempty array of relative .ts paths.");
        var paths = new List<string>();
        foreach (object? item in array)
        {
            if (item is not string raw)
                throw Error("files entries must be nonempty strings.");
            string text = Decode(raw);
            if (string.IsNullOrWhiteSpace(text))
                throw Error("files entries must be nonempty strings.");
            paths.Add(text);
        }

        Files = paths;
    }

    internal ProjectConfiguration(ConfigurationInspection inspection)
    {
        Parsed = inspection.Parsed ?? throw new InvalidOperationException("A parsed configuration is required.");
        FileName = inspection.ConfigurationPath;
        Root = Parsed.BaseDirectory().ToUnicode();
        SourceFile = Parsed.ConfigFile!.SourceFile;
        Options = Parsed.CompilerOptions().Clone();
        try
        {
            Files = (Parsed.FileNames() ?? []).Select(path => path.ToUnicode()).ToArray();
        }
        catch (System.Text.DecoderFallbackException ex)
        {
            throw new ProjectException("pathEncoding", ex.Message, FileName);
        }
    }

    // Graph-loader metadata uses the same parsed source and root reasons, with
    // original byte paths. Only the named legacy/disk checking adapters decode.
    internal ProjectConfiguration(TypeScript.TsOptions.ParsedCommandLine parsed)
    {
        Parsed = parsed;
        FileName = parsed.ConfigName().AsString();
        Root = parsed.BaseDirectory().AsString();
        SourceFile = parsed.ConfigFile?.SourceFile!;
        Options = parsed.CompilerOptions();
        Files = (parsed.FileNames() ?? []).Select(path => path.AsString()).ToArray();
    }

    internal Node? GetFileLiteral(int index)
    {
        if (SourceFile is null) return null;
        Node? result = null;
        void Visit(Node node)
        {
            if (node.Kind == Kind.PropertyAssignment && node.AsPropertyAssignment().Name().Text() == "files")
            {
                Node initializer = node.AsPropertyAssignment().Initializer;
                if (initializer.Kind == Kind.ArrayLiteralExpression && initializer.Elements()is { } elements && index < elements.Count)
                    result = elements[index];
                return;
            }

            node.ForEachChild(child =>
            {
                Visit(child);
                return false;
            });
        }

        Visit(SourceFile.AsNode());
        return result;
    }

    private ProjectException Error(string message) => new("configuration", message, FileName);
    private string Decode(string raw)
    {
        try
        {
            return PathByteText.ToUnicode(raw);
        }
        catch (System.Text.DecoderFallbackException ex)
        {
            throw Error("Invalid configuration string: " + ex.Message);
        }
    }

    private void ValidateCompatibilityStrings(Node node)
    {
        if (Ast.IsStringLiteral(node))
            Decode(node.Text());
        node.ForEachChild(child =>
        {
            ValidateCompatibilityStrings(child);
            return false;
        });
    }

    private void RequireUniqueProperties(Node node)
    {
        if (node.Kind == Kind.ObjectLiteralExpression)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (Node property in node.AsObjectLiteralExpression().Properties?.Nodes ?? default)
                if (property.Kind == Kind.PropertyAssignment && !seen.Add(property.AsPropertyAssignment().Name().Text()))
                    throw Error("Duplicate configuration property: " + Decode(property.AsPropertyAssignment().Name().Text()));
        }

        node.ForEachChild(child =>
        {
            RequireUniqueProperties(child);
            return false;
        });
    }

    private void ValidateOptions(object? value)
    {
        if (value is not OrderedMap<string, object?> fields)
            throw Error("compilerOptions must be an object.");
        foreach (var(rawName, item)in fields.Entries())
        {
            string name = Decode(rawName);
            string? requiredString = name switch
            {
                "target" or "module" => "ESNext",
                "moduleResolution" => "Bundler",
                _ => null
            };
            if (requiredString is not null)
            {
                if (item is not string text || Decode(text) != requiredString)
                    throw Error(name + " must equal " + requiredString + ".");
                continue;
            }

            if (name == "types")
            {
                if (item is not List<object?> { Count: 0 })
                    throw Error("types must be [].");
                continue;
            }

            bool? required = name switch
            {
                "noEmit" or "allowImportingTsExtensions" or "noUncheckedSideEffectImports" or "noLib" or "skipLibCheck" or "strictNullChecks" or "strictFunctionTypes" or "noImplicitAny" or "noImplicitThis" or "forceConsistentCasingInFileNames" => true,
                "noResolve" or "allowJs" or "resolveJsonModule" or "strictBindCallApply" or "noImplicitReturns" or "exactOptionalPropertyTypes" or "noUncheckedIndexedAccess" => false,
                _ => null
            };
            if (required is null)
                throw Error("Unsupported compiler option: " + name);
            if (item is not bool flag || flag != required.Value)
                throw Error(name + " must equal " + required.Value.ToString().ToLowerInvariant() + ".");
        }
    }
}
