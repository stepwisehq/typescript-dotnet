// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified complete C# port of tsc/internal/vfs/vfsmatch/vfsmatch.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Strings contain original Go bytes.
using TypeScript.Scanner;

namespace TypeScript.Vfs.VfsMatch;
public enum Usage : sbyte
{
    Files,
    Directories,
    Exclude
}

public static class VfsMatch
{
    public const int UnlimitedDepth = int.MaxValue;
    public static bool IsImplicitGlob(string component) => component.IndexOfAny(['.', '*', '?']) < 0;
    internal enum ComponentKind
    {
        Literal,
        Wildcard,
        DoubleAsterisk
    }

    internal enum SegmentKind
    {
        Literal,
        Star,
        Question
    }

    internal readonly record struct Segment(SegmentKind Kind, string Literal);
    internal sealed record Component(ComponentKind Kind, string Literal, List<Segment>? Segments, bool SkipPackageFolders);
    internal static bool isHiddenPath(string name) => name.Length > 0 && name[0] == '.';
    internal static bool isPackageFolder(string name) => name.Length switch
    {
        12 => UnicodeCase.EqualFold(name, "node_modules"),
        13 => UnicodeCase.EqualFold(name, "jspm_packages"),
        16 => UnicodeCase.EqualFold(name, "bower_components"),
        _ => false
    };
    internal static List<Segment> parseSegments(string value)
    {
        var result = new List<Segment>();
        int start = 0;
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] is not ('*' or '?'))
                continue;
            if (i > start)
                result.Add(new(SegmentKind.Literal, value[start..i]));
            result.Add(new(value[i] == '*' ? SegmentKind.Star : SegmentKind.Question, ""));
            start = i + 1;
        }

        if (start < value.Length)
            result.Add(new(SegmentKind.Literal, value[start..]));
        return result;
    }

    internal static Component parseComponent(string value, bool include)
    {
        if (value == "**")
            return new(ComponentKind.DoubleAsterisk, "", null, false);
        if (value.IndexOfAny(['*', '?']) < 0)
            return new(ComponentKind.Literal, value, null, false);
        return new(ComponentKind.Wildcard, "", parseSegments(value), include);
    }

    internal static (GlobPattern? Pattern, bool Ok) compileGlobPattern(string spec, RootedDirectoryPath basePath, Usage usage, CaseSensitivity sensitivity)
    {
        List<string> parts = TsPath.ReducePathComponents(TsPath.GetPathComponents(TsPath.CombinePaths(basePath.AsString(), spec)));
        if (parts[0].StartsWith("^/~ts-uri~/", StringComparison.Ordinal))
            sensitivity = CaseSensitivity.CaseSensitive;
        if (usage != Usage.Exclude && parts[^1] == "**")
            return (null, false);
        parts[0] = TsPath.RemoveTrailingDirectorySeparator(parts[0]);
        if (parts[0].StartsWith("^/~ts-uri~/", StringComparison.Ordinal))
            parts = [..parts[0].Split('/'), ..parts.Skip(1)];
        if (IsImplicitGlob(parts[^1]))
        {
            parts.Add("**");
            parts.Add("*");
        }

        return (new GlobPattern(parts.Select(part => parseComponent(part, usage != Usage.Exclude)).ToList(), usage == Usage.Exclude, sensitivity, usage == Usage.Files), true);
    }

    internal static (string Part, int NextOffset, bool Ok) nextPathPartSingle(string value, int offset)
    {
        if (offset >= value.Length)
            return ("", offset, false);
        if (offset == 0 && value[0] == '/')
            return ("", 1, true);
        while (offset < value.Length && value[offset] == '/')
            offset++;
        if (offset >= value.Length)
            return ("", offset, false);
        int index = value.IndexOf('/', offset);
        return index >= 0 ? (value[offset..index], index, true) : (value[offset..], value.Length, true);
    }

    internal static (string Part, int NextOffset, bool Ok) nextPathPartParts(string prefix, string suffix, int offset)
    {
        if (suffix == "")
            return nextPathPartSingle(prefix, offset);
        if (prefix == "")
            return nextPathPartSingle(suffix, offset);
        if (offset >= prefix.Length + suffix.Length)
            return ("", offset, false);
        if (offset == 0 && prefix[0] == '/')
            return ("", 1, true);
        if (offset < prefix.Length)
        {
            while (offset < prefix.Length && prefix[offset] == '/')
                offset++;
            if (offset < prefix.Length)
            {
                int index = prefix.IndexOf('/', offset);
                if (index < 0)
                    throw new InvalidOperationException("virtual prefix must end with slash");
                return (prefix[offset..index], index, true);
            }
        }

        int suffixOffset = offset - prefix.Length;
        return suffixOffset >= suffix.Length ? ("", offset, false) : (suffix[suffixOffset..], prefix.Length + suffix.Length, true);
    }

    internal sealed class GlobPattern(List<Component> components, bool isExclude, CaseSensitivity sensitivity, bool excludeMinJs)
    {
        internal bool stringsEqual(string left, string right) => sensitivity.IsCaseSensitive() ? left == right : UnicodeCase.EqualFold(left, right);
        internal bool matches(string path) => matchPathParts(path, "", 0, 0, false);
        internal bool matchesParts(string prefix, string suffix) => matchPathParts(prefix, suffix, 0, 0, false);
        internal bool matchesPrefixParts(string prefix, string suffix) => matchPathParts(prefix, suffix, 0, 0, true);
        internal bool patternSatisfied(int index) => components.Skip(index).All(component => component.Kind == ComponentKind.DoubleAsterisk);
        internal bool matchPathParts(string prefix, string suffix, int pathOffset, int componentIndex, bool prefixOnly)
        {
            while (true)
            {
                var(part, next, ok) = nextPathPartParts(prefix, suffix, pathOffset);
                if (!ok)
                    return prefixOnly || patternSatisfied(componentIndex);
                if (componentIndex >= components.Count)
                    return isExclude && !prefixOnly;
                Component component = components[componentIndex];
                switch (component.Kind)
                {
                    case ComponentKind.DoubleAsterisk:
                        if (matchPathParts(prefix, suffix, pathOffset, componentIndex + 1, prefixOnly))
                            return true;
                        if (!isExclude && (isHiddenPath(part) || isPackageFolder(part)))
                            return false;
                        pathOffset = next;
                        continue;
                    case ComponentKind.Literal:
                        if (component.SkipPackageFolders && isPackageFolder(part))
                            throw new InvalidOperationException("unreachable: literal components never have skipPackageFolders");
                        if (!stringsEqual(component.Literal, part))
                            return false;
                        break;
                    case ComponentKind.Wildcard:
                        if (component.SkipPackageFolders && isPackageFolder(part))
                            return false;
                        if (!matchWildcard(component.Segments!, part))
                            return false;
                        break;
                }

                pathOffset = next;
                componentIndex++;
            }
        }

        internal bool matchWildcard(List<Segment> segments, string value)
        {
            if (!isExclude && segments.Count > 0 && isHiddenPath(value) && segments[0].Kind is SegmentKind.Star or SegmentKind.Question)
                return false;
            if (segments.Count == 2 && segments[0].Kind == SegmentKind.Star && segments[1].Kind == SegmentKind.Literal)
            {
                string suffix = segments[1].Literal;
                if (value.Length < suffix.Length || !stringsEqual(suffix, value[(value.Length - suffix.Length)..]))
                    return false;
                return shouldIncludeMinJs(value, segments);
            }

            return matchSegments(segments, value) && shouldIncludeMinJs(value, segments);
        }

        internal bool matchSegments(List<Segment> segments, string value)
        {
            int segmentIndex = 0, stringIndex = 0, starSegment = -1, starString = 0;
            while (stringIndex < value.Length)
            {
                if (segmentIndex < segments.Count)
                {
                    Segment segment = segments[segmentIndex];
                    switch (segment.Kind)
                    {
                        case SegmentKind.Literal:
                            int end = stringIndex + segment.Literal.Length;
                            if (end <= value.Length && stringsEqual(segment.Literal, value[stringIndex..end]))
                            {
                                stringIndex = end;
                                segmentIndex++;
                                continue;
                            }

                            break;
                        case SegmentKind.Question:
                            if (value[stringIndex] != '/')
                            {
                                stringIndex += Utf8.DecodeRuneInString(value.AsSpan(stringIndex)).Size;
                                segmentIndex++;
                                continue;
                            }

                            break;
                        case SegmentKind.Star:
                            starSegment = segmentIndex;
                            starString = stringIndex;
                            segmentIndex++;
                            continue;
                    }
                }

                if (starSegment >= 0 && starString < value.Length && value[starString] != '/')
                {
                    starString += Utf8.DecodeRuneInString(value.AsSpan(starString)).Size;
                    stringIndex = starString;
                    segmentIndex = starSegment + 1;
                    continue;
                }

                return false;
            }

            while (segmentIndex < segments.Count && segments[segmentIndex].Kind == SegmentKind.Star)
                segmentIndex++;
            return segmentIndex >= segments.Count;
        }

        internal bool hasMinJsSuffix(string filename) => filename.Length >= 7 && stringsEqual(filename[^7..], ".min.js");
        internal bool patternMentionsMinSuffix(List<Segment> segments)
        {
            foreach (Segment segment in segments)
            {
                if (segment.Kind != SegmentKind.Literal)
                    continue;
                string literal = segment.Literal;
                if (sensitivity.IsCaseInsensitive())
                {
                    var builder = new GoBuilder();
                    foreach (int rune in GoString.EnumerateRunes(literal))
                        builder.WriteRune(UnicodeCase.ToLower(rune));
                    literal = builder.String();
                }

                if (literal.Contains(".min.js", StringComparison.Ordinal) || literal.Contains(".min.", StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        internal bool shouldIncludeMinJs(string filename, List<Segment> segments) => !excludeMinJs || !hasMinJsSuffix(filename) || patternMentionsMinSuffix(segments);
    }

    private sealed class GlobMatcher
    {
        internal readonly List<GlobPattern> Includes = [];
        private readonly List<GlobPattern> excludes = [];
        private readonly bool hadIncludes;
        internal GlobMatcher(List<string>? includes, List<string>? excludeSpecs, RootedDirectoryPath path, CaseSensitivity sensitivity, Usage usage)
        {
            hadIncludes = includes is { Count: > 0 };
            if (includes != null)
                foreach (string spec in includes)
                {
                    var(pattern, ok) = compileGlobPattern(spec, path, usage, sensitivity);
                    if (ok)
                        Includes.Add(pattern!);
                }

            if (excludeSpecs != null)
                foreach (string spec in excludeSpecs)
                {
                    var(pattern, ok) = compileGlobPattern(spec, path, Usage.Exclude, sensitivity);
                    if (ok)
                        excludes.Add(pattern!);
                }
        }

        internal (int Index, bool Ok) matchesFileParts(string prefix, string suffix)
        {
            if (excludes.Any(pattern => pattern.matchesParts(prefix, suffix)))
                return (0, false);
            if (Includes.Count == 0)
                return (0, !hadIncludes);
            for (int i = 0; i < Includes.Count; i++)
                if (Includes[i].matchesParts(prefix, suffix))
                    return (i, true);
            return (0, false);
        }

        internal bool matchesDirectoryParts(string prefix, string suffix)
        {
            if (excludes.Any(pattern => pattern.matchesParts(prefix, suffix)))
                return false;
            return Includes.Count == 0 ? !hadIncludes : Includes.Any(pattern => pattern.matchesPrefixParts(prefix, suffix));
        }
    }

    internal static string getIncludeBasePath(string absolute)
    {
        int wildcard = absolute.IndexOfAny(['*', '?']);
        if (wildcard < 0)
            return !TsPath.GetBaseFileName(absolute).Contains('.') ? absolute : TsPath.RemoveTrailingDirectorySeparator(TsPath.GetDirectoryPath(absolute));
        return absolute[..Math.Max(absolute[..wildcard].LastIndexOf('/'), TsPath.GetRootLength(absolute))];
    }

    internal static List<RootedDirectoryPath> getBasePaths(RootedDirectoryPath path, List<string>? includes, CaseSensitivity sensitivity)
    {
        List<RootedDirectoryPath> result = [path];
        if (includes is not { Count: > 0 })
            return result;
        var includePaths = includes.Select(include => TsPath.ToRootedDirectoryPath(getIncludeBasePath(TsPath.NormalizePath(TsPath.IsRootedDiskPath(include) ? include : TsPath.CombinePaths(path.AsString(), include))), path));
        // LINQ OrderBy is stable, matching slices.SortStableFunc.
        foreach (var candidate in includePaths.OrderBy(value => value, Comparer<RootedDirectoryPath>.Create((a, b) => sensitivity.ComparePaths(a.AsPath(), b.AsPath()))))
            if (result.All(existing => !sensitivity.ContainsPath(existing, candidate.AsPath())))
                result.Add(candidate);
        return result;
    }

    private sealed class GlobVisitor(IFS host, GlobMatcher fileMatcher, GlobMatcher directoryMatcher, List<string>? extensions, CaseSensitivity sensitivity)
    {
        private readonly HashSet<PathKey> visited = [];
        internal readonly List<List<RootedFilePath>?> Results = Enumerable.Repeat<List<RootedFilePath>?>(null, Math.Max(fileMatcher.Includes.Count, 1)).ToList();
        internal void visit(RootedDirectoryPath path, int depth, RootedDirectoryPath realPath)
        {
            if (realPath.AsString() == "")
                realPath = TsPath.RootedDirectoryPathFromPath(host.Realpath(path.AsPath()));
            if (!visited.Add(sensitivity.PathKey(realPath.AsPath())))
                return;
            Entries entries = host.GetAccessibleEntries(path);
            string prefix = TsPath.EnsureTrailingDirectorySeparator(path.AsString());
            if (entries.Files != null)
                foreach (string file in entries.Files)
                {
                    if (extensions is { Count: > 0 } && !extensions.Any(extension => file.Length > extension.Length && file.EndsWith(extension, StringComparison.Ordinal)))
                        continue;
                    var(index, ok) = fileMatcher.matchesFileParts(prefix, file);
                    if (ok)
                        (Results[index] ??= []).Add(path.ResolveFile(file));
                }

            if (depth != UnlimitedDepth)
            {
                depth--;
                if (depth == 0)
                    return;
            }

            if (entries.Directories == null)
                return;
            foreach (string directory in entries.Directories)
            {
                if (!directoryMatcher.matchesDirectoryParts(prefix, directory))
                    continue;
                RootedDirectoryPath childRealPath = default;
                if (entries.Symlinks != null && !entries.Symlinks.Contains(directory))
                    childRealPath = realPath.ResolveDirectory(directory);
                visit(path.ResolveDirectory(directory), depth, childRealPath);
            }
        }
    }

    public static List<RootedFilePath>? ReadDirectory(IFS host, RootedDirectoryPath path, List<string>? extensions, List<string>? excludes, List<string>? includes, int depth)
    {
        CaseSensitivity sensitivity = host.CaseSensitivity();
        var fileMatcher = new GlobMatcher(includes, excludes, path, sensitivity, Usage.Files);
        var visitor = new GlobVisitor(host, fileMatcher, new GlobMatcher(includes, excludes, path, sensitivity, Usage.Directories), extensions, sensitivity);
        foreach (var basePath in getBasePaths(path, includes, sensitivity))
            visitor.visit(basePath, depth, default);
        if (visitor.Results.Count == 1)
            return visitor.Results[0];
        List<RootedFilePath>? result = null;
        foreach (var bucket in visitor.Results)
            if (bucket is { Count: > 0 })
                (result ??= []).AddRange(bucket);
        return result;
    }

    public static List<RootedFilePath>? ReadDirectory(IFS host, RootedDirectoryPath path, List<string>? extensions, List<PathPattern>? excludes, List<PathPattern>? includes, int depth) => ReadDirectory(host, path, extensions, excludes?.Select(value => value.AsString()).ToList(), includes?.Select(value => value.AsString()).ToList(), depth);
    public static SpecMatcher? NewSpecMatcher(List<string>? specs, RootedDirectoryPath path, Usage usage, CaseSensitivity sensitivity)
    {
        if (specs is not { Count: > 0 })
            return null;
        var patterns = new List<GlobPattern>();
        foreach (string spec in specs)
        {
            var(pattern, ok) = compileGlobPattern(spec, path, usage, sensitivity);
            if (ok)
                patterns.Add(pattern!);
        }

        return patterns.Count == 0 ? null : new SpecMatcher(patterns);
    }

    public static SpecMatcher? NewSpecMatcher(List<PathPattern>? specs, RootedDirectoryPath path, Usage usage, CaseSensitivity sensitivity) => NewSpecMatcher(specs?.Select(value => value.AsString()).ToList(), path, usage, sensitivity);
}

public sealed class SpecMatcher
{
    private readonly List<VfsMatch.GlobPattern> patterns;
    internal SpecMatcher(List<VfsMatch.GlobPattern> patterns) => this.patterns = patterns;
    public bool MatchString(string path) => patterns.Any(pattern => pattern.matches(path));
    public bool MatchFileName(RootedFilePath path) => MatchString(path.AsString());
    public int MatchIndex(string path)
    {
        for (int i = 0; i < patterns.Count; i++)
            if (patterns[i].matches(path))
                return i;
        return -1;
    }

    public int MatchFileNameIndex(RootedFilePath path) => MatchIndex(path.AsString());
}
