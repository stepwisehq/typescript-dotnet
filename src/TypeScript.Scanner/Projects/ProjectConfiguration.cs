// Authored strict tscheck.json subset; not a tsconfig/JSONC implementation.
using System.Text.Json;
using TypeScript.Scanner;

namespace TypeScript.Compiler;
internal sealed class ProjectConfiguration
{
    internal string FileName { get; }
    internal string Root { get; }
    internal SourceFile SourceFile { get; }
    internal IReadOnlyList<string> Files { get; }
    internal CompilerOptions Options { get; } = CreateOptions();

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
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(bytes);
        }
        catch (JsonException ex)
        {
            throw new ProjectException("configurationJson", ex.Message, FileName);
        }

        using (document)
        {
            JsonElement root = document.RootElement;
            RequireObject(root, "configuration");
            JsonElement? files = null;
            foreach (JsonProperty property in root.EnumerateObject())
            {
                string name = ReadPropertyName(property);
                switch (name)
                {
                    case "files":
                        files = property.Value;
                        break;
                    case "compilerOptions":
                        ValidateOptions(property.Value);
                        break;
                    default:
                        throw Error("Unsupported configuration property: " + name);
                }
            }

            if (files is not JsonElement array || array.ValueKind != JsonValueKind.Array || array.GetArrayLength() == 0)
                throw Error("files must be a nonempty array of relative .ts paths.");
            var paths = new List<string>();
            foreach (JsonElement item in array.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                    throw Error("files entries must be nonempty strings.");
                string text = ReadString(item);
                if (string.IsNullOrWhiteSpace(text))
                    throw Error("files entries must be nonempty strings.");
                paths.Add(text);
            }

            Files = paths;
        }
    }

    internal Node? GetFileLiteral(int index)
    {
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
    // JsonDocument accepts escaped lone surrogates, but decoding those strings
    // throws InvalidOperationException. Catch only the actual decoding boundary.
    private string ReadString(JsonElement value)
    {
        try
        {
            return value.GetString()!;
        }
        catch (InvalidOperationException ex)
        {
            throw Error("Invalid configuration string: " + ex.Message);
        }
    }

    private string ReadPropertyName(JsonProperty property)
    {
        try
        {
            return property.Name;
        }
        catch (InvalidOperationException ex)
        {
            throw Error("Invalid configuration property name: " + ex.Message);
        }
    }

    private void RequireObject(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw Error(name + " must be an object.");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonProperty property in element.EnumerateObject())
        {
            string propertyName = ReadPropertyName(property);
            if (!seen.Add(propertyName))
                throw Error("Duplicate " + name + " property: " + propertyName);
        }
    }

    private void ValidateOptions(JsonElement element)
    {
        RequireObject(element, "compilerOptions");
        foreach (JsonProperty property in element.EnumerateObject())
        {
            string name = ReadPropertyName(property);
            JsonElement value = property.Value;
            string? requiredString = name switch
            {
                "target" or "module" => "ESNext",
                "moduleResolution" => "Bundler",
                _ => null
            };
            if (requiredString is not null)
            {
                if (value.ValueKind != JsonValueKind.String || ReadString(value) != requiredString)
                    throw Error(name + " must equal " + requiredString + ".");
                continue;
            }

            if (name == "types")
            {
                if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != 0)
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
            if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False) || value.GetBoolean() != required.Value)
                throw Error(name + " must equal " + required.Value.ToString().ToLowerInvariant() + ".");
        }
    }
}
