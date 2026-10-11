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

    internal readonly record struct Segment(SegmentKind Kind, ByteStringValue Literal);
    internal readonly record struct Component(ComponentKind Kind, ByteStringValue Literal, Slice<Segment> Segments, bool SkipPackageFolders);
    internal static bool isHiddenPath(ByteStringValue name) => name.Length > 0 && name[0] == '.';
    internal static bool isPackageFolder(ByteStringValue name) => name.Length switch
    {
        12 => UnicodeCase.EqualFold(name.CharSpan, "node_modules".AsSpan()),
        13 => UnicodeCase.EqualFold(name.CharSpan, "jspm_packages".AsSpan()),
        16 => UnicodeCase.EqualFold(name.CharSpan, "bower_components".AsSpan()),
        _ => false
    };
    internal static Slice<Segment> parseSegments(string value)
    {
        int wildcards = 0;
        foreach (char c in value)
            if (c is '*' or '?') wildcards++;
        var result = Slice<Segment>.Make(0, 2 * wildcards + 1);
        var text = ByteStringValue.FromString(value);
        int start = 0;
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] is not ('*' or '?'))
                continue;
            if (i > start)
                result = result.Append(new Segment(SegmentKind.Literal, text.Slice(start, i)));
            result = result.Append(new Segment(value[i] == '*' ? SegmentKind.Star : SegmentKind.Question, default));
            start = i + 1;
        }

        if (start < value.Length)
            result = result.Append(new Segment(SegmentKind.Literal, text.Slice(start)));
        return result;
    }

    internal static Component parseComponent(string value, bool include)
    {
        if (value == "**")
            return new(ComponentKind.DoubleAsterisk, default, default, false);
        if (value.IndexOfAny(['*', '?']) < 0)
            return new(ComponentKind.Literal, ByteStringValue.FromString(value), default, false);
        return new(ComponentKind.Wildcard, default, parseSegments(value), include);
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

        return (new GlobPattern(Core.MapSlice(parts, usage, static (part, usage) => parseComponent(part, usage != Usage.Exclude)), usage == Usage.Exclude, sensitivity, usage == Usage.Files), true);
    }

    internal static (ByteStringValue Part, int NextOffset, bool Ok) nextPathPartSingle(string value, int offset)
    {
        if (offset >= value.Length)
            return (default, offset, false);
        if (offset == 0 && value[0] == '/')
            return (default, 1, true);
        while (offset < value.Length && value[offset] == '/')
            offset++;
        if (offset >= value.Length)
            return (default, offset, false);
        int index = value.IndexOf('/', offset);
        return index >= 0 ? (ByteStringValue.FromString(value).Slice(offset, index), index, true) : (ByteStringValue.FromString(value).Slice(offset), value.Length, true);
    }

    internal static (ByteStringValue Part, int NextOffset, bool Ok) nextPathPartParts(string prefix, string suffix, int offset)
    {
        if (suffix == "")
            return nextPathPartSingle(prefix, offset);
        if (prefix == "")
            return nextPathPartSingle(suffix, offset);
        if (offset >= prefix.Length + suffix.Length)
            return (default, offset, false);
        if (offset == 0 && prefix[0] == '/')
            return (default, 1, true);
        if (offset < prefix.Length)
        {
            while (offset < prefix.Length && prefix[offset] == '/')
                offset++;
            if (offset < prefix.Length)
            {
                int index = prefix.IndexOf('/', offset);
                if (index < 0)
                    throw new InvalidOperationException("virtual prefix must end with slash");
                return (ByteStringValue.FromString(prefix).Slice(offset, index), index, true);
            }
        }

        int suffixOffset = offset - prefix.Length;
        return suffixOffset >= suffix.Length ? (default, offset, false) : (ByteStringValue.FromString(suffix).Slice(suffixOffset), prefix.Length + suffix.Length, true);
    }

    internal readonly struct GlobPattern(Slice<Component> components, bool isExclude, CaseSensitivity sensitivity, bool excludeMinJs)
    {
        internal bool stringsEqual(ByteStringValue left, ByteStringValue right) => sensitivity.IsCaseSensitive() ? left.EqualsValue(right) : UnicodeCase.EqualFold(left.CharSpan, right.CharSpan);
        internal bool matches(string path) => matchPathParts(path, "", 0, 0, false);
        internal bool matchesParts(string prefix, string suffix) => matchPathParts(prefix, suffix, 0, 0, false);
        internal bool matchesPrefixParts(string prefix, string suffix) => matchPathParts(prefix, suffix, 0, 0, true);
        internal bool patternSatisfied(int index)
        {
            foreach (var component in components.Slice2(index, components.Length))
                if (component.Kind != ComponentKind.DoubleAsterisk) return false;
            return true;
        }
        internal bool matchPathParts(string prefix, string suffix, int pathOffset, int componentIndex, bool prefixOnly)
        {
            while (true)
            {
                var(part, next, ok) = nextPathPartParts(prefix, suffix, pathOffset);
                if (!ok)
                    return prefixOnly || patternSatisfied(componentIndex);
                if (componentIndex >= components.Length)
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

        internal bool matchWildcard(Slice<Segment> segments, ByteStringValue value)
        {
            if (!isExclude && segments.Length > 0 && isHiddenPath(value) && segments[0].Kind is SegmentKind.Star or SegmentKind.Question)
                return false;
            if (segments.Length == 2 && segments[0].Kind == SegmentKind.Star && segments[1].Kind == SegmentKind.Literal)
            {
                var suffix = segments[1].Literal;
                if (value.Length < suffix.Length || !stringsEqual(suffix, value.Slice(value.Length - suffix.Length)))
                    return false;
                return shouldIncludeMinJs(value, segments);
            }

            return matchSegments(segments, value) && shouldIncludeMinJs(value, segments);
        }

        internal bool matchSegments(Slice<Segment> segments, ByteStringValue value)
        {
            int segmentIndex = 0, stringIndex = 0, starSegment = -1, starString = 0;
            while (stringIndex < value.Length)
            {
                if (segmentIndex < segments.Length)
                {
                    Segment segment = segments[segmentIndex];
                    switch (segment.Kind)
                    {
                        case SegmentKind.Literal:
                            int end = stringIndex + segment.Literal.Length;
                            if (end <= value.Length && stringsEqual(segment.Literal, value.Slice(stringIndex, end)))
                            {
                                stringIndex = end;
                                segmentIndex++;
                                continue;
                            }

                            break;
                        case SegmentKind.Question:
                            if (value[stringIndex] != '/')
                            {
                                stringIndex += Utf8.DecodeRuneInString(value.CharSpan.Slice(stringIndex)).Size;
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
                    starString += Utf8.DecodeRuneInString(value.CharSpan.Slice(starString)).Size;
                    stringIndex = starString;
                    segmentIndex = starSegment + 1;
                    continue;
                }

                return false;
            }

            while (segmentIndex < segments.Length && segments[segmentIndex].Kind == SegmentKind.Star)
                segmentIndex++;
            return segmentIndex >= segments.Length;
        }

        internal bool hasMinJsSuffix(ByteStringValue filename) => filename.Length >= 7 && stringsEqual(filename.Slice(filename.Length - 7), ByteStringValue.FromString(".min.js"));
        internal bool patternMentionsMinSuffix(Slice<Segment> segments)
        {
            foreach (Segment segment in segments)
            {
                if (segment.Kind != SegmentKind.Literal)
                    continue;
                var literal = segment.Literal;
                if (sensitivity.IsCaseInsensitive()) literal = GoStrings.ToLower(literal);
                if (literal.IsBytes
                    ? literal.ByteSpan.IndexOf(".min.js"u8) >= 0 || literal.ByteSpan.IndexOf(".min."u8) >= 0
                    : literal.CharSpan.Contains(".min.js".AsSpan(), StringComparison.Ordinal) || literal.CharSpan.Contains(".min.".AsSpan(), StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        internal bool shouldIncludeMinJs(ByteStringValue filename, Slice<Segment> segments) => !excludeMinJs || !hasMinJsSuffix(filename) || patternMentionsMinSuffix(segments);
    }

    private sealed class GlobMatcher
    {
        internal Slice<GlobPattern> Includes;
        private Slice<GlobPattern> excludes;
        private readonly bool hadIncludes;
        internal GlobMatcher(int includeCount, int excludeCount)
        {
            hadIncludes = includeCount > 0;
            Includes = Slice<GlobPattern>.Make(0, includeCount);
            excludes = Slice<GlobPattern>.Make(0, excludeCount);
        }
        internal static GlobMatcher Create<T>(ReadOnlySpan<T> includes, ReadOnlySpan<T> excludeSpecs, RootedDirectoryPath path, CaseSensitivity sensitivity, Usage usage, Func<T, string> text)
        {
            var result = new GlobMatcher(includes.Length, excludeSpecs.Length);
            foreach (var spec in includes)
            {
                var (pattern, ok) = compileGlobPattern(text(spec), path, usage, sensitivity);
                if (ok) result.Includes = result.Includes.Append(pattern!.Value);
            }
            foreach (var spec in excludeSpecs)
            {
                var (pattern, ok) = compileGlobPattern(text(spec), path, Usage.Exclude, sensitivity);
                if (ok) result.excludes = result.excludes.Append(pattern!.Value);
            }
            return result;
        }

        internal (int Index, bool Ok) matchesFileParts(string prefix, string suffix)
        {
            foreach (var pattern in excludes)
                if (pattern.matchesParts(prefix, suffix)) return (0, false);
            if (Includes.Length == 0)
                return (0, !hadIncludes);
            for (int i = 0; i < Includes.Length; i++)
                if (Includes[i].matchesParts(prefix, suffix))
                    return (i, true);
            return (0, false);
        }

        internal bool matchesDirectoryParts(string prefix, string suffix)
        {
            foreach (var pattern in excludes)
                if (pattern.matchesParts(prefix, suffix)) return false;
            if (Includes.Length == 0) return !hadIncludes;
            foreach (var pattern in Includes)
                if (pattern.matchesPrefixParts(prefix, suffix)) return true;
            return false;
        }
    }

    internal static string getIncludeBasePath(string absolute)
    {
        int wildcard = absolute.IndexOfAny(['*', '?']);
        if (wildcard < 0)
            return !TsPath.GetBaseFileName(absolute).Contains('.') ? absolute : TsPath.RemoveTrailingDirectorySeparator(TsPath.GetDirectoryPath(absolute));
        return absolute[..Math.Max(absolute[..wildcard].LastIndexOf('/'), TsPath.GetRootLength(absolute))];
    }

    internal static Slice<RootedDirectoryPath> getBasePaths<T>(RootedDirectoryPath path, ReadOnlySpan<T> includes, CaseSensitivity sensitivity, Func<T, string> text)
    {
        var result = Slice<RootedDirectoryPath>.FromArray([path]);
        if (includes.IsEmpty) return result;
        var includePaths = Slice<RootedDirectoryPath>.Make(0);
        foreach (var spec in includes)
        {
            var include = text(spec);
            includePaths = includePaths.Append(TsPath.ToRootedDirectoryPath(getIncludeBasePath(TsPath.NormalizePath(TsPath.IsRootedDiskPath(include) ? include : TsPath.CombinePaths(path.AsString(), include))), path));
        }
        GoSlices.SortStableFunc(includePaths, (a, b) => sensitivity.ComparePaths(a.AsPath(), b.AsPath()));
        foreach (var candidate in includePaths)
        {
            bool contained = false;
            foreach (var existing in result)
                if (sensitivity.ContainsPath(existing, candidate.AsPath())) { contained = true; break; }
            if (!contained) result = result.Append(candidate);
        }
        return result;
    }

    private sealed class GlobVisitor(IFS host, GlobMatcher fileMatcher, GlobMatcher directoryMatcher, List<string>? extensions, CaseSensitivity sensitivity)
    {
        private readonly HashSet<PathKey> visited = [];
        internal Slice<Slice<RootedFilePath>> Results = Slice<Slice<RootedFilePath>>.Make(Math.Max(fileMatcher.Includes.Length, 1));
        internal void visit(RootedDirectoryPath path, int depth, RootedDirectoryPath realPath)
        {
            if (realPath.AsString() == "")
                realPath = TsPath.RootedDirectoryPathFromPath(host.Realpath(path.AsPath()));
            if (!visited.Add(sensitivity.PathKey(realPath.AsPath())))
                return;
            Entries entries = host.GetAccessibleEntries(path);
            string prefix = TsPath.EnsureTrailingDirectorySeparator(path.AsString());
            if (!entries.Files.IsNil)
                foreach (string file in entries.Files)
                {
                    if (extensions is { Count: > 0 })
                    {
                        bool hasExtension = false;
                        foreach (var extension in extensions)
                            if (file.Length > extension.Length && file.EndsWith(extension, StringComparison.Ordinal)) { hasExtension = true; break; }
                        if (!hasExtension) continue;
                    }
                    var(index, ok) = fileMatcher.matchesFileParts(prefix, file);
                    if (ok)
                        Results[index] = Results[index].Append(path.ResolveFile(file));
                }

            if (depth != UnlimitedDepth)
            {
                depth--;
                if (depth == 0)
                    return;
            }

            if (entries.Directories.IsNil)
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

    private static ReadOnlySpan<T> Borrow<T>(List<T>? values) => values is null ? default : System.Runtime.InteropServices.CollectionsMarshal.AsSpan(values);
    private static Slice<RootedFilePath> readDirectory<T>(IFS host, RootedDirectoryPath path, List<string>? extensions, ReadOnlySpan<T> excludes, ReadOnlySpan<T> includes, int depth, Func<T, string> text)
    {
        CaseSensitivity sensitivity = host.CaseSensitivity();
        var fileMatcher = GlobMatcher.Create(includes, excludes, path, sensitivity, Usage.Files, text);
        var visitor = new GlobVisitor(host, fileMatcher, GlobMatcher.Create(includes, excludes, path, sensitivity, Usage.Directories, text), extensions, sensitivity);
        foreach (var basePath in getBasePaths(path, includes, sensitivity, text))
            visitor.visit(basePath, depth, default);
        if (visitor.Results.Length == 1) return visitor.Results[0];
        Slice<RootedFilePath> result = default;
        foreach (var bucket in visitor.Results) result = result.Append(bucket);
        return result;
    }

    public static Slice<RootedFilePath> ReadDirectory(IFS host, RootedDirectoryPath path, List<string>? extensions, List<string>? excludes, List<string>? includes, int depth) =>
        readDirectory(host, path, extensions, Borrow(excludes), Borrow(includes), depth, static value => value);
    public static Slice<RootedFilePath> ReadDirectory(IFS host, RootedDirectoryPath path, List<string>? extensions, List<PathPattern>? excludes, List<PathPattern>? includes, int depth) =>
        readDirectory(host, path, extensions, Borrow(excludes), Borrow(includes), depth, static value => value.AsString());
    private static SpecMatcher? newSpecMatcher<T>(ReadOnlySpan<T> specs, RootedDirectoryPath path, Usage usage, CaseSensitivity sensitivity, Func<T, string> text)
    {
        if (specs.IsEmpty) return null;
        var patterns = Slice<GlobPattern>.Make(0, specs.Length);
        foreach (var spec in specs)
        {
            var (pattern, ok) = compileGlobPattern(text(spec), path, usage, sensitivity);
            if (ok) patterns = patterns.Append(pattern!.Value);
        }
        return patterns.Length == 0 ? null : new SpecMatcher(patterns);
    }
    public static SpecMatcher? NewSpecMatcher(List<string>? specs, RootedDirectoryPath path, Usage usage, CaseSensitivity sensitivity) =>
        newSpecMatcher(Borrow(specs), path, usage, sensitivity, static value => value);
    public static SpecMatcher? NewSpecMatcher(List<PathPattern>? specs, RootedDirectoryPath path, Usage usage, CaseSensitivity sensitivity) =>
        newSpecMatcher(Borrow(specs), path, usage, sensitivity, static value => value.AsString());

}

public sealed class SpecMatcher
{
    private readonly Slice<VfsMatch.GlobPattern> patterns;
    internal SpecMatcher(Slice<VfsMatch.GlobPattern> patterns) => this.patterns = patterns;
    public bool MatchString(string path)
    {
        foreach (var pattern in patterns)
            if (pattern.matches(path)) return true;
        return false;
    }
    public bool MatchFileName(RootedFilePath path) => MatchString(path.AsString());
    public int MatchIndex(string path)
    {
        for (int i = 0; i < patterns.Length; i++)
            if (patterns[i].matches(path))
                return i;
        return -1;
    }

    public int MatchFileNameIndex(RootedFilePath path) => MatchIndex(path.AsString());
}
