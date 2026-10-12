// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/sourcemap/generator.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Collections;
using TypeScript.Json;
using Path = TypeScript.Scanner.TsPath;
namespace TypeScript.SourceMap;

// SourceIndex/NameIndex/UTF16Offset use the pinned Go platform's 64-bit ints; source
// text offsets in the mapper are UTF8 byte offsets, never these UTF16 columns.
public sealed class RawSourceMap
{
    public int Version;
    public ByteStringValue File, SourceRoot, Mappings;
    public ByteStringValue[] Sources = [], Names = [];
    public ByteStringValue?[]? SourcesContent;
}
internal sealed class Generator
{
    private const int sourceIndexNotSet = -1, nameIndexNotSet = -1, notSet = -1, notSetUTF16 = -1;
    private readonly CaseSensitivity caseSensitivity;
    private readonly ByteStringValue file, sourceRoot;
    private readonly RootedDirectoryPath sourcesDirectoryPath;
    private Slice<ByteStringValue> rawSources, sources, names;
    private Dictionary<ByteStringKey,int>? sourceToSourceIndexMap, nameToNameIndexMap;
    private Slice<ByteStringValue?> sourcesContent;
    private readonly GoBuilder mappings = new();
    private long lastGeneratedLine, lastGeneratedCharacter, lastSourceIndex, lastSourceLine, lastSourceCharacter, lastNameIndex;
    private bool hasLast;
    private long pendingGeneratedLine, pendingGeneratedCharacter, pendingSourceIndex, pendingSourceLine, pendingSourceCharacter, pendingNameIndex;
    private bool hasPending, hasPendingSource, hasPendingName;
    private Generator(ByteStringValue file, ByteStringValue sourceRoot, RootedDirectoryPath sourcesDirectoryPath, CaseSensitivity caseSensitivity)
    {
        this.file = file.Snapshot(); this.sourceRoot = sourceRoot.Snapshot();
        this.sourcesDirectoryPath = sourcesDirectoryPath; this.caseSensitivity = caseSensitivity;
    }
    internal static Generator NewGenerator(ByteStringValue file, ByteStringValue sourceRoot, RootedDirectoryPath sourcesDirectoryPath, CaseSensitivity caseSensitivity) => new(file, sourceRoot, sourcesDirectoryPath, caseSensitivity);
    internal static Generator NewGenerator(string file, string sourceRoot, RootedDirectoryPath sourcesDirectoryPath, CaseSensitivity caseSensitivity) => NewGenerator(ByteStringValue.FromString(file), ByteStringValue.FromString(sourceRoot), sourcesDirectoryPath, caseSensitivity);
    internal Slice<ByteStringValue> Sources() => rawSources;
    internal int AddSource(RootedFilePath fileName)
    {
        var source = Path.GetRelativePathToDirectoryOrUrl(sourcesDirectoryPath.AsValue(), fileName.AsValue(), true, caseSensitivity);
        var key = ByteStringKey.FromValue(source);
        if (sourceToSourceIndexMap == null || !sourceToSourceIndexMap.TryGetValue(key, out int sourceIndex))
        {
            sourceIndex = sources.Length;
            sources = sources.Append(source.Snapshot()); rawSources = rawSources.Append(fileName.AsValue());
            sourceToSourceIndexMap ??= new(ByteStringKey.Comparer);
            sourceToSourceIndexMap[key] = sourceIndex;
        }
        return sourceIndex;
    }
    internal Exception? SetSourceContent(long sourceIndex, string content) => SetSourceContent(sourceIndex, ByteStringValue.FromString(content));
    internal Exception? SetSourceContent(long sourceIndex, ByteStringValue content)
    {
        if (sourceIndex < 0 || sourceIndex >= sources.Length) return new InvalidOperationException("sourceIndex is out of range");
        while (sourcesContent.Length <= sourceIndex) sourcesContent = sourcesContent.Append((ByteStringValue?)null);
        sourcesContent[(int)sourceIndex] = content.Snapshot();
        return null;
    }
    internal int AddName(string name) => AddName(ByteStringValue.FromString(name));
    internal int AddName(ByteStringValue name)
    {
        var key = ByteStringKey.FromValue(name);
        if (nameToNameIndexMap == null || !nameToNameIndexMap.TryGetValue(key, out int nameIndex))
        {
            nameIndex = names.Length; names = names.Append(name.Snapshot());
            nameToNameIndexMap ??= new(ByteStringKey.Comparer); nameToNameIndexMap[key] = nameIndex;
        }
        return nameIndex;
    }
    private bool isNewGeneratedPosition(long generatedLine, long generatedCharacter) => !hasPending || pendingGeneratedLine != generatedLine || pendingGeneratedCharacter != generatedCharacter;
    private bool isBacktrackingSourcePosition(long sourceIndex, long sourceLine, long sourceCharacter) => sourceIndex != sourceIndexNotSet && sourceLine != notSet && sourceCharacter != notSetUTF16 && pendingSourceIndex == sourceIndex && (pendingSourceLine > sourceLine || pendingSourceLine == sourceLine && pendingSourceCharacter > sourceCharacter);
    private bool shouldCommitMapping() => hasPending && (!hasLast || lastGeneratedLine != pendingGeneratedLine || lastGeneratedCharacter != pendingGeneratedCharacter || lastSourceIndex != pendingSourceIndex || lastSourceLine != pendingSourceLine || lastSourceCharacter != pendingSourceCharacter || lastNameIndex != pendingNameIndex);
    private void appendMappingCharCode(int charCode) => mappings.WriteRune(charCode);
    private void appendBase64VLQ(long inValue)
    {
        // Go int is 64-bit on the pinned platform. Use long for VLQ shifts,
        // including overflowing malformed decoder data; C# shifts are masked.
        inValue = inValue < 0 ? unchecked((-inValue << 1) + 1) : unchecked(inValue << 1);
        while (true)
        {
            int currentDigit = (int)(inValue & 31);
            inValue >>= 5;
            if (inValue > 0) currentDigit |= 32;
            appendMappingCharCode(base64FormatEncode(currentDigit));
            if (inValue <= 0) break;
        }
    }
    private void commitPendingMapping()
    {
        if (!shouldCommitMapping()) return;
        if (lastGeneratedLine < pendingGeneratedLine)
        {
            while (true)
            {
                appendMappingCharCode(';'); lastGeneratedLine++;
                if (lastGeneratedLine >= pendingGeneratedLine) break;
            }
            lastGeneratedCharacter = 0;
        }
        else
        {
            if (lastGeneratedLine != pendingGeneratedLine) throw new InvalidOperationException("generatedLine cannot backtrack");
            if (hasLast) appendMappingCharCode(',');
        }
        appendBase64VLQ((long)pendingGeneratedCharacter - lastGeneratedCharacter); lastGeneratedCharacter = pendingGeneratedCharacter;
        if (hasPendingSource)
        {
            appendBase64VLQ((long)pendingSourceIndex - lastSourceIndex); lastSourceIndex = pendingSourceIndex;
            appendBase64VLQ((long)pendingSourceLine - lastSourceLine); lastSourceLine = pendingSourceLine;
            appendBase64VLQ((long)pendingSourceCharacter - lastSourceCharacter); lastSourceCharacter = pendingSourceCharacter;
            if (hasPendingName) { appendBase64VLQ((long)pendingNameIndex - lastNameIndex); lastNameIndex = pendingNameIndex; }
        }
        hasLast = true;
    }
    private void addMapping(long generatedLine, long generatedCharacter, long sourceIndex, long sourceLine, long sourceCharacter, long nameIndex)
    {
        if (isNewGeneratedPosition(generatedLine, generatedCharacter) || isBacktrackingSourcePosition(sourceIndex, sourceLine, sourceCharacter))
        {
            commitPendingMapping(); pendingGeneratedLine = generatedLine; pendingGeneratedCharacter = generatedCharacter;
            hasPendingSource = false; hasPendingName = false; hasPending = true;
        }
        if (sourceIndex != sourceIndexNotSet && sourceLine != notSet && sourceCharacter != notSetUTF16)
        {
            pendingSourceIndex = sourceIndex; pendingSourceLine = sourceLine; pendingSourceCharacter = sourceCharacter; hasPendingSource = true;
            if (nameIndex != nameIndexNotSet) { pendingNameIndex = nameIndex; hasPendingName = true; }
        }
    }
    internal Exception? AddGeneratedMapping(long generatedLine, long generatedCharacter)
    {
        if (generatedLine < pendingGeneratedLine) return new InvalidOperationException("generatedLine cannot backtrack");
        if (generatedCharacter < 0) return new InvalidOperationException("generatedCharacter cannot be negative");
        addMapping(generatedLine, generatedCharacter, sourceIndexNotSet, notSet, notSetUTF16, nameIndexNotSet);
        hasPendingSource = false; hasPendingName = false; return null;
    }
    internal Exception? AddSourceMapping(long generatedLine, long generatedCharacter, long sourceIndex, long sourceLine, long sourceCharacter)
    {
        if (generatedLine < pendingGeneratedLine) return new InvalidOperationException("generatedLine cannot backtrack");
        if (generatedCharacter < 0) return new InvalidOperationException("generatedCharacter cannot be negative");
        if (sourceIndex < 0 || sourceIndex >= sources.Length) return new InvalidOperationException("sourceIndex is out of range");
        if (sourceLine < 0) return new InvalidOperationException("sourceLine cannot be negative");
        if (sourceCharacter < 0) return new InvalidOperationException("sourceCharacter cannot be negative");
        if (hasPending && !isNewGeneratedPosition(generatedLine, generatedCharacter) && !hasPendingSource) return null;
        addMapping(generatedLine, generatedCharacter, sourceIndex, sourceLine, sourceCharacter, nameIndexNotSet); return null;
    }
    internal Exception? AddNamedSourceMapping(long generatedLine, long generatedCharacter, long sourceIndex, long sourceLine, long sourceCharacter, long nameIndex)
    {
        if (generatedLine < pendingGeneratedLine) return new InvalidOperationException("generatedLine cannot backtrack");
        if (generatedCharacter < 0) return new InvalidOperationException("generatedCharacter cannot be negative");
        if (sourceIndex < 0 || sourceIndex >= sources.Length) return new InvalidOperationException("sourceIndex is out of range");
        if (sourceLine < 0) return new InvalidOperationException("sourceLine cannot be negative");
        if (sourceCharacter < 0) return new InvalidOperationException("sourceCharacter cannot be negative");
        if (nameIndex < 0 || nameIndex >= names.Length) return new InvalidOperationException("nameIndex is out of range");
        if (hasPending && !isNewGeneratedPosition(generatedLine, generatedCharacter) && !hasPendingSource) return null;
        addMapping(generatedLine, generatedCharacter, sourceIndex, sourceLine, sourceCharacter, nameIndex); return null;
    }
    internal RawSourceMap RawSourceMap()
    {
        commitPendingMapping();
        // Required slices.Clone ownership: callers may mutate the returned arrays.
        return new() { Version = 3, File = file, SourceRoot = sourceRoot, Sources = sources.AsSpan().ToArray(), Names = names.AsSpan().ToArray(), Mappings = mappings.Value(), SourcesContent = sourcesContent.IsNil ? null : sourcesContent.AsSpan().ToArray() };
    }
    private byte[] bytes()
    {
        var map = RawSourceMap();
        var fields = new OrderedMap<string, object?>();
        fields.Set("version", map.Version); fields.Set("file", map.File);
        if (map.SourceRoot.Length != 0) fields.Set("sourceRoot", map.SourceRoot);
        fields.Set("sources", map.Sources); fields.Set("names", map.Names); fields.Set("mappings", map.Mappings);
        if (map.SourcesContent != null) fields.Set("sourcesContent", map.SourcesContent.Select(x => x.HasValue ? (object)x.Value : null).ToArray());
        return JsonCodec.Marshal(fields);
    }
    internal string String() => GoString.FromBytes(bytes());
    internal ByteStringValue Value() => ByteStringValue.FromView(ByteStringView.FromOwnedBytes(bytes()));
    internal string Base64DataURL() => "data:application/json;base64," + Convert.ToBase64String(bytes());
    private static int base64FormatEncode(int value) => value switch { >=0 and <26 => 'A'+value, >=26 and <52 => 'a'+value-26, >=52 and <62 => '0'+value-52, 62 => '+', 63 => '/', _ => throw new InvalidOperationException("not a base64 value") };
}
