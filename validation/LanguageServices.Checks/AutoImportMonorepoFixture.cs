// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Selected actual autoimporttestutil/fixtures.go monorepo/file-map closure.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using BaselineFS = Program.Checks.CompilerBaselineFileSystem;

namespace LanguageServices.Checks;
internal sealed class AutoImportMonorepoFixture : IDisposable
{
    internal readonly record struct PackageConfig(string Name, int FileCount, string[]? NodeModuleNames = null, string[]? DependencyNames = null);
    internal readonly record struct NodePackage(string Name, string Directory, BaselineFS.Input PackageJSON, BaselineFS.Input Declaration);
    internal sealed class ProjectHandle
    {
        internal required string Root;
        internal BaselineFS.Input[] Files = [];
        internal NodePackage[] NodeModules = [];
        internal BaselineFS.Input? PackageJSON;
        internal DocumentUri URI(int index) => URIFor(Files[index].Path);
    }

    internal readonly ProjectSessionFixture Fixture;
    internal readonly string Root;
    internal readonly NodePackage[] RootNodeModules;
    internal readonly BaselineFS.Input RootPackageJSON;
    internal readonly ProjectHandle[] Packages;
    internal readonly BaselineFS.Input[] ExtraFiles;
    private readonly Dictionary<string, BaselineFS.Input> files = new(StringComparer.Ordinal);
    private int nextPackageID, nextProjectID;
    internal AutoImportMonorepoFixture(string root, string name, string[]? nodeModuleNames, string[]? dependencyNames, PackageConfig[] packages, BaselineFS.Input[]? extraFiles = null, (string Link, string Target)[]? symlinks = null)
    {
        Root = Normalize(root);
        if (name == "")
            name = "monorepo";
        Add(new(Root + "/tsconfig.json", "{\n  \"compilerOptions\": {\n    \"module\": \"esnext\",\n    \"target\": \"esnext\",\n    \"strict\": true,\n    \"baseUrl\": \".\",\n    \"allowJs\": true,\n    \"checkJs\": true\n  }\n}\n"));
        RootNodeModules = AddPackages(Root + "/node_modules", nodeModuleNames);
        RootPackageJSON = PackageJSON(Root, name, SelectPackages(RootNodeModules, dependencyNames), true);
        Packages = new ProjectHandle[packages.Length];
        for (int i = 0; i < packages.Length; i++)
        {
            var config = packages[i];
            string directory = Root + "/packages/" + config.Name;
            var handle = AddLocalProject(directory, config.FileCount);
            handle.NodeModules = AddPackages(directory + "/node_modules", config.NodeModuleNames);
            // Original append(slices.Clone(rootNodeModules), pkgNodeModules...).
            var available = new NodePackage[RootNodeModules.Length + handle.NodeModules.Length];
            RootNodeModules.CopyTo(available, 0);
            handle.NodeModules.CopyTo(available, RootNodeModules.Length);
            var selected = SelectPackages(available, config.DependencyNames);
            if (selected.Length != 0)
                handle.PackageJSON = PackageJSON(directory, config.Name, selected, false);
            Packages[i] = handle;
        }

        ExtraFiles = new BaselineFS.Input[extraFiles?.Length ?? 0];
        for (int i = 0; i < ExtraFiles.Length; i++)
        {
            var extra = extraFiles![i];
            var normalized = new BaselineFS.Input(Normalize(extra.Path), extra.Contents);
            Add(normalized);
            ExtraFiles[i] = normalized;
        }

        foreach (var symlink in symlinks ?? [])
            Add(new(Normalize(symlink.Link), Symlink: Normalize(symlink.Target)));
        Fixture = new ProjectSessionFixture(files.Values);
    }

    internal BaselineFS.Input ExtraFile(string path)
    {
        string normalized = Normalize(path);
        foreach (var handle in ExtraFiles)
            if (handle.Path == normalized)
                return handle;
        throw new InvalidOperationException("extra file not found: " + path);
    }

    private void Add(BaselineFS.Input file) => files[file.Path] = file;
    private ProjectHandle AddLocalProject(string root, int fileCount)
    {
        if (fileCount < 0)
            throw new InvalidOperationException("fileCount must be non-negative");
        nextProjectID++;
        Add(new(root + "/tsconfig.json", "{\n  \"compilerOptions\": {\n    \"module\": \"esnext\",\n    \"target\": \"esnext\",\n    \"strict\": true,\n    \"allowJs\": true,\n    \"checkJs\": true\n  }\n}\n"));
        var handle = new ProjectHandle
        {
            Root = root,
            Files = new BaselineFS.Input[fileCount]
        };
        for (int i = 1; i <= fileCount; i++)
        {
            var file = new BaselineFS.Input(root + $"/file{i}.ts", $"export const localExport{nextProjectID}_{i} = {i};\n");
            Add(file);
            handle.Files[i - 1] = file;
        }

        return handle;
    }

    private NodePackage[] AddPackages(string directory, string[]? names)
    {
        if (names is null || names.Length == 0)
            return[];
        if (TsPath.GetBaseFileName(directory) != "node_modules")
            throw new InvalidOperationException("nodeModulesDir must point to a node_modules directory: " + directory);
        var result = new NodePackage[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            nextPackageID++;
            string name = names[i] == "" ? $"pkg{nextPackageID}" : names[i];
            string path = directory + "/" + name;
            var package = new BaselineFS.Input(path + "/package.json", $"{{\"name\":\"{name}\",\"types\":\"index.d.ts\"}}");
            var declaration = new BaselineFS.Input(path + "/index.d.ts", $"export declare const {SanitizeIdentifier(name)}_value: number;\n");
            Add(package);
            Add(declaration);
            result[i] = new(name, path, package, declaration);
        }

        return result;
    }

    private BaselineFS.Input PackageJSON(string root, string name, NodePackage[] dependencies, bool isRoot)
    {
        if (name == "")
            name = isRoot ? "monorepo-root" : $"local-project-{++nextProjectID}";
        var output = new GoBuilder();
        output.WriteString("{\n  \"name\": \"" + name + "\"");
        if (isRoot)
            output.WriteString(",\n  \"private\": true");
        if (dependencies.Length != 0)
        {
            output.WriteString(",\n  \"dependencies\": {\n    ");
            for (int i = 0; i < dependencies.Length; i++)
            {
                if (i != 0)
                    output.WriteString(",\n    ");
                output.WriteString("\"" + dependencies[i].Name + "\": \"*\"");
            }

            output.WriteString("\n  }\n");
        }
        else
            output.WriteString("\n");
        output.WriteString("}\n");
        var file = new BaselineFS.Input(root + "/package.json", output.String());
        Add(file);
        return file;
    }

    private static NodePackage[] SelectPackages(NodePackage[] available, string[]? names)
    {
        if (names is null || names.Length == 0)
            return (NodePackage[])available.Clone();
        var result = new NodePackage[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            bool found = false;
            foreach (var candidate in available)
                if (candidate.Name == names[i])
                {
                    result[i] = candidate;
                    found = true;
                    break;
                }

            if (!found)
                throw new InvalidOperationException("dependency not found: " + names[i]);
        }

        return result;
    }

    private static string SanitizeIdentifier(string name)
    {
        var sanitized = GoStrings.Map(static rune => rune is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' ? rune : rune is '_' or '-' ? '_' : -1, name);
        return sanitized.Length == 0 ? "pkg" : sanitized;
    }

    private static string Normalize(string path) => TsPath.NormalizePath(path);
    internal static DocumentUri URIFor(string file) => new(ByteStringValue.FromString("file://" + file));
    public void Dispose() => Fixture.Dispose();
}
