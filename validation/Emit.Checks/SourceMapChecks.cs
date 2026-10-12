// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original assertions ported from internal/sourcemap generator_test.go (32)
// and source_mapper_test.go (10), fed0bf24149fb1ed36039212648bafdafc1ea10e.
using System.Diagnostics.CodeAnalysis;
using TypeScript.SourceMap;
using TypeScript.Scanner;
namespace Emit.Checks;
public static class SourceMapChecks
{
    public static int OriginalCaseCount => 42;
    public const int AuthoredCaseCount = 19;
    public static readonly string[] Names = [
        "TestSourceMapGenerator_Empty",
        "TestSourceMapGenerator_Empty_Serialized",
        "TestSourceMapGenerator_AddSource",
        "TestSourceMapGenerator_SetSourceContent",
        "TestSourceMapGenerator_SetSourceContent_ForSecondSourceOnly",
        "TestSourceMapGenerator_SetSourceContent_SourceIndexOutOfRange",
        "TestSourceMapGenerator_SetSourceContent_ForSecondSourceOnly_Serialized",
        "TestSourceMapGenerator_AddName",
        "TestSourceMapGenerator_AddGeneratedMapping",
        "TestSourceMapGenerator_AddGeneratedMapping_ReplacesPendingSourceMapping",
        "TestSourceMapGenerator_AddGeneratedMapping_IsNotReplacedBySourceMapping",
        "TestSourceMapGenerator_AddGeneratedMapping_OnSecondLineOnly",
        "TestSourceMapGenerator_AddSourceMapping",
        "TestSourceMapGenerator_AddSourceMapping_NextGeneratedCharacter",
        "TestSourceMapGenerator_AddSourceMapping_NextGeneratedAndSourceCharacter",
        "TestSourceMapGenerator_AddSourceMapping_NextGeneratedLine",
        "TestSourceMapGenerator_AddSourceMapping_PreviousSourceCharacter",
        "TestSourceMapGenerator_AddNamedSourceMapping",
        "TestSourceMapGenerator_AddNamedSourceMapping_WithPreviousName",
        "TestSourceMapGenerator_AddGeneratedMapping_GeneratedLineCannotBacktrack",
        "TestSourceMapGenerator_AddGeneratedMapping_GeneratedCharacterCannotBeNegative",
        "TestSourceMapGenerator_AddSourceMapping_GeneratedLineCannotBacktrack",
        "TestSourceMapGenerator_AddSourceMapping_GeneratedCharacterCannotBeNegative",
        "TestSourceMapGenerator_AddSourceMapping_SourceIndexIsOutOfRange",
        "TestSourceMapGenerator_AddSourceMapping_SourceLineCannotBeNegative",
        "TestSourceMapGenerator_AddSourceMapping_SourceCharacterCannotBeNegative",
        "TestSourceMapGenerator_AddNamedSourceMapping_GeneratedLineCannotBacktrack",
        "TestSourceMapGenerator_AddNamedSourceMapping_GeneratedCharacterCannotBeNegative",
        "TestSourceMapGenerator_AddNamedSourceMapping_SourceIndexIsOutOfRange",
        "TestSourceMapGenerator_AddNamedSourceMapping_SourceLineCannotBeNegative",
        "TestSourceMapGenerator_AddNamedSourceMapping_SourceCharacterCannotBeNegative",
        "TestSourceMapGenerator_AddNamedSourceMapping_NameIndexIsOutOfRange",
        "TestSourceMapperPreservesEmptySourceEntries",
        "TestSourceMapperResolvesEmptySourceToSourceRoot",
        "TestSourceMapperResolvesEmptySourceToMapURLWithoutSourceRoot",
        "TestSourceMapperTreatsEmptySourceRootAsAbsent",
        "TestSourceMapperPrefixesAbsoluteSourceWithNonemptySourceRoot",
        "TestSourceMapperRetainsDuplicateSourceIndices",
        "TestSourceMapperPreservesNullSourceEntries",
        "TestSourceMapperIgnoresOutOfRangeSourceIndex",
        "TestSourceMapperIgnoresSourceURLSuffix",
        "TestSourceMapperIgnoresExternalMapURLSuffix",
    ];
    public static void Run(string name)
    {
        switch (name)
        {
            case "TestSourceMapGenerator_Empty": TestSourceMapGenerator_Empty(); break;
            case "TestSourceMapGenerator_Empty_Serialized": TestSourceMapGenerator_Empty_Serialized(); break;
            case "TestSourceMapGenerator_AddSource": TestSourceMapGenerator_AddSource(); break;
            case "TestSourceMapGenerator_SetSourceContent": TestSourceMapGenerator_SetSourceContent(); break;
            case "TestSourceMapGenerator_SetSourceContent_ForSecondSourceOnly": TestSourceMapGenerator_SetSourceContent_ForSecondSourceOnly(); break;
            case "TestSourceMapGenerator_SetSourceContent_SourceIndexOutOfRange": TestSourceMapGenerator_SetSourceContent_SourceIndexOutOfRange(); break;
            case "TestSourceMapGenerator_SetSourceContent_ForSecondSourceOnly_Serialized": TestSourceMapGenerator_SetSourceContent_ForSecondSourceOnly_Serialized(); break;
            case "TestSourceMapGenerator_AddName": TestSourceMapGenerator_AddName(); break;
            case "TestSourceMapGenerator_AddGeneratedMapping": TestSourceMapGenerator_AddGeneratedMapping(); break;
            case "TestSourceMapGenerator_AddGeneratedMapping_ReplacesPendingSourceMapping": TestSourceMapGenerator_AddGeneratedMapping_ReplacesPendingSourceMapping(); break;
            case "TestSourceMapGenerator_AddGeneratedMapping_IsNotReplacedBySourceMapping": TestSourceMapGenerator_AddGeneratedMapping_IsNotReplacedBySourceMapping(); break;
            case "TestSourceMapGenerator_AddGeneratedMapping_OnSecondLineOnly": TestSourceMapGenerator_AddGeneratedMapping_OnSecondLineOnly(); break;
            case "TestSourceMapGenerator_AddSourceMapping": TestSourceMapGenerator_AddSourceMapping(); break;
            case "TestSourceMapGenerator_AddSourceMapping_NextGeneratedCharacter": TestSourceMapGenerator_AddSourceMapping_NextGeneratedCharacter(); break;
            case "TestSourceMapGenerator_AddSourceMapping_NextGeneratedAndSourceCharacter": TestSourceMapGenerator_AddSourceMapping_NextGeneratedAndSourceCharacter(); break;
            case "TestSourceMapGenerator_AddSourceMapping_NextGeneratedLine": TestSourceMapGenerator_AddSourceMapping_NextGeneratedLine(); break;
            case "TestSourceMapGenerator_AddSourceMapping_PreviousSourceCharacter": TestSourceMapGenerator_AddSourceMapping_PreviousSourceCharacter(); break;
            case "TestSourceMapGenerator_AddNamedSourceMapping": TestSourceMapGenerator_AddNamedSourceMapping(); break;
            case "TestSourceMapGenerator_AddNamedSourceMapping_WithPreviousName": TestSourceMapGenerator_AddNamedSourceMapping_WithPreviousName(); break;
            case "TestSourceMapGenerator_AddGeneratedMapping_GeneratedLineCannotBacktrack": TestSourceMapGenerator_AddGeneratedMapping_GeneratedLineCannotBacktrack(); break;
            case "TestSourceMapGenerator_AddGeneratedMapping_GeneratedCharacterCannotBeNegative": TestSourceMapGenerator_AddGeneratedMapping_GeneratedCharacterCannotBeNegative(); break;
            case "TestSourceMapGenerator_AddSourceMapping_GeneratedLineCannotBacktrack": TestSourceMapGenerator_AddSourceMapping_GeneratedLineCannotBacktrack(); break;
            case "TestSourceMapGenerator_AddSourceMapping_GeneratedCharacterCannotBeNegative": TestSourceMapGenerator_AddSourceMapping_GeneratedCharacterCannotBeNegative(); break;
            case "TestSourceMapGenerator_AddSourceMapping_SourceIndexIsOutOfRange": TestSourceMapGenerator_AddSourceMapping_SourceIndexIsOutOfRange(); break;
            case "TestSourceMapGenerator_AddSourceMapping_SourceLineCannotBeNegative": TestSourceMapGenerator_AddSourceMapping_SourceLineCannotBeNegative(); break;
            case "TestSourceMapGenerator_AddSourceMapping_SourceCharacterCannotBeNegative": TestSourceMapGenerator_AddSourceMapping_SourceCharacterCannotBeNegative(); break;
            case "TestSourceMapGenerator_AddNamedSourceMapping_GeneratedLineCannotBacktrack": TestSourceMapGenerator_AddNamedSourceMapping_GeneratedLineCannotBacktrack(); break;
            case "TestSourceMapGenerator_AddNamedSourceMapping_GeneratedCharacterCannotBeNegative": TestSourceMapGenerator_AddNamedSourceMapping_GeneratedCharacterCannotBeNegative(); break;
            case "TestSourceMapGenerator_AddNamedSourceMapping_SourceIndexIsOutOfRange": TestSourceMapGenerator_AddNamedSourceMapping_SourceIndexIsOutOfRange(); break;
            case "TestSourceMapGenerator_AddNamedSourceMapping_SourceLineCannotBeNegative": TestSourceMapGenerator_AddNamedSourceMapping_SourceLineCannotBeNegative(); break;
            case "TestSourceMapGenerator_AddNamedSourceMapping_SourceCharacterCannotBeNegative": TestSourceMapGenerator_AddNamedSourceMapping_SourceCharacterCannotBeNegative(); break;
            case "TestSourceMapGenerator_AddNamedSourceMapping_NameIndexIsOutOfRange": TestSourceMapGenerator_AddNamedSourceMapping_NameIndexIsOutOfRange(); break;
            case "TestSourceMapperPreservesEmptySourceEntries": TestSourceMapperPreservesEmptySourceEntries(); break;
            case "TestSourceMapperResolvesEmptySourceToSourceRoot": TestSourceMapperResolvesEmptySourceToSourceRoot(); break;
            case "TestSourceMapperResolvesEmptySourceToMapURLWithoutSourceRoot": TestSourceMapperResolvesEmptySourceToMapURLWithoutSourceRoot(); break;
            case "TestSourceMapperTreatsEmptySourceRootAsAbsent": TestSourceMapperTreatsEmptySourceRootAsAbsent(); break;
            case "TestSourceMapperPrefixesAbsoluteSourceWithNonemptySourceRoot": TestSourceMapperPrefixesAbsoluteSourceWithNonemptySourceRoot(); break;
            case "TestSourceMapperRetainsDuplicateSourceIndices": TestSourceMapperRetainsDuplicateSourceIndices(); break;
            case "TestSourceMapperPreservesNullSourceEntries": TestSourceMapperPreservesNullSourceEntries(); break;
            case "TestSourceMapperIgnoresOutOfRangeSourceIndex": TestSourceMapperIgnoresOutOfRangeSourceIndex(); break;
            case "TestSourceMapperIgnoresSourceURLSuffix": TestSourceMapperIgnoresSourceURLSuffix(); break;
            case "TestSourceMapperIgnoresExternalMapURLSuffix": TestSourceMapperIgnoresExternalMapURLSuffix(); break;
            default: throw new ArgumentException("Unknown source-map original case", nameof(name));
        }
    }
    public static int RunAll()
    {
        foreach (string name in Names) Run(name);
        Equal(RunAuthoredControls(), AuthoredCaseCount);
        return OriginalCaseCount;
    }
private static void TestSourceMapGenerator_Empty() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	var sourceMap = gen.RawSourceMap();
	DeepEqual( sourceMap, new ExpectedRawSourceMap {
		Version =        3,
		File =           "main.js",
		SourceRoot =     "/",
		Sources =        new string[]{},
		Mappings =       "",
		Names =          new string[]{},
		SourcesContent = null,
	});
}

private static void TestSourceMapGenerator_Empty_Serialized() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	var actual = gen.String();
	var expected = "{\"version\":3,\"file\":\"main.js\",\"sourceRoot\":\"/\",\"sources\":[],\"names\":[],\"mappings\":\"\"}";
	Equal( actual, expected);
}

private static void TestSourceMapGenerator_AddSource() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	var sourceIndex = gen.AddSource(RootedFilePath.FromGoString("/main.ts"));
	var sourceMap = gen.RawSourceMap();
	Equal( sourceIndex, 0);
	DeepEqual( sourceMap, new ExpectedRawSourceMap {
		Version =        3,
		File =           "main.js",
		SourceRoot =     "/",
		Sources =        new string[]{"main.ts"},
		Mappings =       "",
		Names =          new string[]{},
		SourcesContent = null,
	});
}

private static void TestSourceMapGenerator_SetSourceContent() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	var sourceIndex = gen.AddSource(RootedFilePath.FromGoString("/main.ts"));
	var sourceContent = "foo";
	NilError( gen.SetSourceContent(sourceIndex, sourceContent));
	var sourceMap = gen.RawSourceMap();
	Equal( sourceIndex, 0);
	DeepEqual( sourceMap, new ExpectedRawSourceMap {
		Version =        3,
		File =           "main.js",
		SourceRoot =     "/",
		Sources =        new string[]{"main.ts"},
		Mappings =       "",
		Names =          new string[]{},
		SourcesContent = new string?[]{sourceContent},
	});
}

private static void TestSourceMapGenerator_SetSourceContent_ForSecondSourceOnly() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	gen.AddSource(RootedFilePath.FromGoString("/skipped.ts"));
	var sourceIndex = gen.AddSource(RootedFilePath.FromGoString("/main.ts"));
	var sourceContent = "foo";
	NilError( gen.SetSourceContent(sourceIndex, sourceContent));
	var sourceMap = gen.RawSourceMap();
	Equal( sourceIndex, 1);
	DeepEqual( sourceMap, new ExpectedRawSourceMap {
		Version =        3,
		File =           "main.js",
		SourceRoot =     "/",
		Sources =        new string[]{"skipped.ts", "main.ts"},
		Mappings =       "",
		Names =          new string[]{},
		SourcesContent = new string?[]{null, sourceContent},
	});
}

private static void TestSourceMapGenerator_SetSourceContent_SourceIndexOutOfRange() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	Error( gen.SetSourceContent(-1, ""), "sourceIndex is out of range");
	Error( gen.SetSourceContent(0, ""), "sourceIndex is out of range");
}

private static void TestSourceMapGenerator_SetSourceContent_ForSecondSourceOnly_Serialized() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	gen.AddSource(RootedFilePath.FromGoString("/skipped.ts"));
	var sourceIndex = gen.AddSource(RootedFilePath.FromGoString("/main.ts"));
	var sourceContent = "foo";
	NilError( gen.SetSourceContent(sourceIndex, sourceContent));
	var actual = gen.String();
	var expected = "{\"version\":3,\"file\":\"main.js\",\"sourceRoot\":\"/\",\"sources\":[\"skipped.ts\",\"main.ts\"],\"names\":[],\"mappings\":\"\",\"sourcesContent\":[null,\"foo\"]}";
	Equal( actual, expected);
}

private static void TestSourceMapGenerator_AddName() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	var nameIndex = gen.AddName("foo");
	var sourceMap = gen.RawSourceMap();
	Equal( nameIndex, 0);
	DeepEqual( sourceMap, new ExpectedRawSourceMap {
		Version =        3,
		File =           "main.js",
		SourceRoot =     "/",
		Sources =        new string[]{},
		Mappings =       "",
		Names =          new string[]{"foo"},
		SourcesContent = null,
	});
}

private static void TestSourceMapGenerator_AddGeneratedMapping() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	NilError( gen.AddGeneratedMapping(0, 0));
	var sourceMap = gen.RawSourceMap();
	DeepEqual( sourceMap, new ExpectedRawSourceMap {
		Version =        3,
		File =           "main.js",
		SourceRoot =     "/",
		Sources =        new string[]{},
		Mappings =       "A",
		Names =          new string[]{},
		SourcesContent = null,
	});
}

private static void TestSourceMapGenerator_AddGeneratedMapping_ReplacesPendingSourceMapping() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	var sourceIndex = gen.AddSource(RootedFilePath.FromGoString("/main.ts"));
	NilError( gen.AddSourceMapping(0, 0, sourceIndex, 0, 0));
	NilError( gen.AddGeneratedMapping(0, 0));
	var sourceMap = gen.RawSourceMap();
	Equal( sourceMap.Mappings.Materialize(), "A");
}

private static void TestSourceMapGenerator_AddGeneratedMapping_IsNotReplacedBySourceMapping() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	var sourceIndex = gen.AddSource(RootedFilePath.FromGoString("/main.ts"));
	NilError( gen.AddGeneratedMapping(0, 0));
	NilError( gen.AddSourceMapping(0, 0, sourceIndex, 0, 0));
	var sourceMap = gen.RawSourceMap();
	Equal( sourceMap.Mappings.Materialize(), "A");
}

private static void TestSourceMapGenerator_AddGeneratedMapping_OnSecondLineOnly() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	NilError( gen.AddGeneratedMapping(1, 0));
	var sourceMap = gen.RawSourceMap();
	DeepEqual( sourceMap, new ExpectedRawSourceMap {
		Version =        3,
		File =           "main.js",
		SourceRoot =     "/",
		Sources =        new string[]{},
		Mappings =       ";A",
		Names =          new string[]{},
		SourcesContent = null,
	});
}

private static void TestSourceMapGenerator_AddSourceMapping() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	var sourceIndex = gen.AddSource(RootedFilePath.FromGoString("/main.ts"));
	NilError( gen.AddSourceMapping(0, 0, sourceIndex, 0, 0));
	var sourceMap = gen.RawSourceMap();
	DeepEqual( sourceMap, new ExpectedRawSourceMap {
		Version =        3,
		File =           "main.js",
		SourceRoot =     "/",
		Sources =        new string[]{"main.ts"},
		Mappings =       "AAAA",
		Names =          new string[]{},
		SourcesContent = null,
	});
}

private static void TestSourceMapGenerator_AddSourceMapping_NextGeneratedCharacter() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	var sourceIndex = gen.AddSource(RootedFilePath.FromGoString("/main.ts"));
	NilError( gen.AddSourceMapping(0, 0, sourceIndex, 0, 0));
	NilError( gen.AddSourceMapping(0, 1, sourceIndex, 0, 0));
	var sourceMap = gen.RawSourceMap();
	DeepEqual( sourceMap, new ExpectedRawSourceMap {
		Version =        3,
		File =           "main.js",
		SourceRoot =     "/",
		Sources =        new string[]{"main.ts"},
		Mappings =       "AAAA,CAAA",
		Names =          new string[]{},
		SourcesContent = null,
	});
}

private static void TestSourceMapGenerator_AddSourceMapping_NextGeneratedAndSourceCharacter() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	var sourceIndex = gen.AddSource(RootedFilePath.FromGoString("/main.ts"));
	NilError( gen.AddSourceMapping(0, 0, sourceIndex, 0, 0));
	NilError( gen.AddSourceMapping(0, 1, sourceIndex, 0, 1));
	var sourceMap = gen.RawSourceMap();
	DeepEqual( sourceMap, new ExpectedRawSourceMap {
		Version =        3,
		File =           "main.js",
		SourceRoot =     "/",
		Sources =        new string[]{"main.ts"},
		Mappings =       "AAAA,CAAC",
		Names =          new string[]{},
		SourcesContent = null,
	});
}

private static void TestSourceMapGenerator_AddSourceMapping_NextGeneratedLine() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	var sourceIndex = gen.AddSource(RootedFilePath.FromGoString("/main.ts"));
	NilError( gen.AddSourceMapping(0, 0, sourceIndex, 0, 0));
	NilError( gen.AddSourceMapping(1, 0, sourceIndex, 0, 0));
	var sourceMap = gen.RawSourceMap();
	DeepEqual( sourceMap, new ExpectedRawSourceMap {
		Version =        3,
		File =           "main.js",
		SourceRoot =     "/",
		Sources =        new string[]{"main.ts"},
		Mappings =       "AAAA;AAAA",
		Names =          new string[]{},
		SourcesContent = null,
	});
}

private static void TestSourceMapGenerator_AddSourceMapping_PreviousSourceCharacter() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	var sourceIndex = gen.AddSource(RootedFilePath.FromGoString("/main.ts"));
	NilError( gen.AddSourceMapping(0, 0, sourceIndex, 0, 1));
	NilError( gen.AddSourceMapping(0, 1, sourceIndex, 0, 0));
	var sourceMap = gen.RawSourceMap();
	DeepEqual( sourceMap, new ExpectedRawSourceMap {
		Version =        3,
		File =           "main.js",
		SourceRoot =     "/",
		Sources =        new string[]{"main.ts"},
		Mappings =       "AAAC,CAAD",
		Names =          new string[]{},
		SourcesContent = null,
	});
}

private static void TestSourceMapGenerator_AddNamedSourceMapping() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	var sourceIndex = gen.AddSource(RootedFilePath.FromGoString("/main.ts"));
	var nameIndex = gen.AddName("foo");
	NilError( gen.AddNamedSourceMapping(0, 0, sourceIndex, 0, 0, nameIndex));
	var sourceMap = gen.RawSourceMap();
	DeepEqual( sourceMap, new ExpectedRawSourceMap {
		Version =        3,
		File =           "main.js",
		SourceRoot =     "/",
		Sources =        new string[]{"main.ts"},
		Mappings =       "AAAAA",
		Names =          new string[]{"foo"},
		SourcesContent = null,
	});
}

private static void TestSourceMapGenerator_AddNamedSourceMapping_WithPreviousName() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	var sourceIndex = gen.AddSource(RootedFilePath.FromGoString("/main.ts"));
	var nameIndex1 = gen.AddName("foo");
	var nameIndex2 = gen.AddName("bar");
	NilError( gen.AddNamedSourceMapping(0, 0, sourceIndex, 0, 0, nameIndex2));
	NilError( gen.AddNamedSourceMapping(0, 1, sourceIndex, 0, 0, nameIndex1));
	var sourceMap = gen.RawSourceMap();
	DeepEqual( sourceMap, new ExpectedRawSourceMap {
		Version =        3,
		File =           "main.js",
		SourceRoot =     "/",
		Sources =        new string[]{"main.ts"},
		Mappings =       "AAAAC,CAAAD",
		Names =          new string[]{"foo", "bar"},
		SourcesContent = null,
	});
}

private static void TestSourceMapGenerator_AddGeneratedMapping_GeneratedLineCannotBacktrack() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	NilError( gen.AddGeneratedMapping(1, 0));
	Error( gen.AddGeneratedMapping(0, 0), "generatedLine cannot backtrack");
}

private static void TestSourceMapGenerator_AddGeneratedMapping_GeneratedCharacterCannotBeNegative() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	NilError( gen.AddGeneratedMapping(0, 0));
	Error( gen.AddGeneratedMapping(0, -1), "generatedCharacter cannot be negative");
}

private static void TestSourceMapGenerator_AddSourceMapping_GeneratedLineCannotBacktrack() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	var sourceIndex = gen.AddSource(RootedFilePath.FromGoString("/main.ts"));
	NilError( gen.AddSourceMapping(1, 0, sourceIndex, 0, 0));
	Error( gen.AddSourceMapping(0, 0, sourceIndex, 0, 0), "generatedLine cannot backtrack");
}

private static void TestSourceMapGenerator_AddSourceMapping_GeneratedCharacterCannotBeNegative() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	var sourceIndex = gen.AddSource(RootedFilePath.FromGoString("/main.ts"));
	NilError( gen.AddSourceMapping(0, 0, sourceIndex, 0, 0));
	Error( gen.AddSourceMapping(0, -1, sourceIndex, 0, 0), "generatedCharacter cannot be negative");
}

private static void TestSourceMapGenerator_AddSourceMapping_SourceIndexIsOutOfRange() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	Error( gen.AddSourceMapping(0, 0, -1, 0, 0), "sourceIndex is out of range");
	Error( gen.AddSourceMapping(0, 0, 0, 0, 0), "sourceIndex is out of range");
}

private static void TestSourceMapGenerator_AddSourceMapping_SourceLineCannotBeNegative() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	var sourceIndex = gen.AddSource(RootedFilePath.FromGoString("/main.ts"));
	Error( gen.AddSourceMapping(0, 0, sourceIndex, -1, 0), "sourceLine cannot be negative");
}

private static void TestSourceMapGenerator_AddSourceMapping_SourceCharacterCannotBeNegative() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	var sourceIndex = gen.AddSource(RootedFilePath.FromGoString("/main.ts"));
	Error( gen.AddSourceMapping(0, 0, sourceIndex, 0, -1), "sourceCharacter cannot be negative");
}

private static void TestSourceMapGenerator_AddNamedSourceMapping_GeneratedLineCannotBacktrack() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	var sourceIndex = gen.AddSource(RootedFilePath.FromGoString("/main.ts"));
	var nameIndex = gen.AddName("foo");
	NilError( gen.AddNamedSourceMapping(1, 0, sourceIndex, 0, 0, nameIndex));
	Error( gen.AddNamedSourceMapping(0, 0, sourceIndex, 0, 0, nameIndex), "generatedLine cannot backtrack");
}

private static void TestSourceMapGenerator_AddNamedSourceMapping_GeneratedCharacterCannotBeNegative() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	var sourceIndex = gen.AddSource(RootedFilePath.FromGoString("/main.ts"));
	var nameIndex = gen.AddName("foo");
	NilError( gen.AddNamedSourceMapping(0, 0, sourceIndex, 0, 0, nameIndex));
	Error( gen.AddNamedSourceMapping(0, -1, sourceIndex, 0, 0, nameIndex), "generatedCharacter cannot be negative");
}

private static void TestSourceMapGenerator_AddNamedSourceMapping_SourceIndexIsOutOfRange() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	var nameIndex = gen.AddName("foo");
	Error( gen.AddNamedSourceMapping(0, 0, -1, 0, 0, nameIndex), "sourceIndex is out of range");
	Error( gen.AddNamedSourceMapping(0, 0, 0, 0, 0, nameIndex), "sourceIndex is out of range");
}

private static void TestSourceMapGenerator_AddNamedSourceMapping_SourceLineCannotBeNegative() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	var nameIndex = gen.AddName("foo");
	var sourceIndex = gen.AddSource(RootedFilePath.FromGoString("/main.ts"));
	Error( gen.AddNamedSourceMapping(0, 0, sourceIndex, -1, 0, nameIndex), "sourceLine cannot be negative");
}

private static void TestSourceMapGenerator_AddNamedSourceMapping_SourceCharacterCannotBeNegative() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	var nameIndex = gen.AddName("foo");
	var sourceIndex = gen.AddSource(RootedFilePath.FromGoString("/main.ts"));
	Error( gen.AddNamedSourceMapping(0, 0, sourceIndex, 0, -1, nameIndex), "sourceCharacter cannot be negative");
}

private static void TestSourceMapGenerator_AddNamedSourceMapping_NameIndexIsOutOfRange() {
	var gen = NewGenerator("main.js", "/", "/", CaseSensitivity.CaseInsensitive);
	var sourceIndex = gen.AddSource(RootedFilePath.FromGoString("/main.ts"));
	Error( gen.AddNamedSourceMapping(0, 0, sourceIndex, 0, 0, -1), "nameIndex is out of range");
	Error( gen.AddNamedSourceMapping(0, 0, sourceIndex, 0, 0, 0), "nameIndex is out of range");
}
private static void TestSourceMapperPreservesEmptySourceEntries() {


	var host = new sourceMapperTestHost(new Dictionary<string,string>{
		["/project/out/out.d.ts"] = "generated",
		["/project/src/real.ts"] = "source",
	});
	var mapper = ConvertDocumentToSourceMapper(
		host,
		"{\"version\":3,\"file\":\"out.d.ts\",\"sourceRoot\":\"../src\",\"sources\":[\"\",\"real.ts\"],\"names\":[],\"mappings\":\"ACAA\"}",
		"/project/out/out.d.ts.map");
	Assert( mapper != null);
	DeepEqual( mapper.GetSourcePosition(new DocumentPosition(RootedFilePath.FromGoString("/project/out/out.d.ts"), 0)), new DocumentPosition(RootedFilePath.FromGoString("/project/src/real.ts"), 0));
	DeepEqual( mapper.GetGeneratedPosition(new DocumentPosition(RootedFilePath.FromGoString("/project/src/real.ts"), 0)), new DocumentPosition(RootedFilePath.FromGoString("/project/out/out.d.ts"), 0));
}
private static void TestSourceMapperResolvesEmptySourceToSourceRoot() {


	var host = new sourceMapperTestHost(new Dictionary<string,string>{
		["/project/out/out.d.ts"] = "generated",
		["/project/src"] = "source",
	});
	var mapper = ConvertDocumentToSourceMapper(
		host,
		"{\"version\":3,\"file\":\"out.d.ts\",\"sourceRoot\":\"../src\",\"sources\":[\"\"],\"names\":[],\"mappings\":\"AAAA\"}",
		"/project/out/out.d.ts.map");
	Assert( mapper != null);
	DeepEqual( mapper.GetSourcePosition(new DocumentPosition(RootedFilePath.FromGoString("/project/out/out.d.ts"), 0)), new DocumentPosition(RootedFilePath.FromGoString("/project/src"), 0));
}
private static void TestSourceMapperResolvesEmptySourceToMapURLWithoutSourceRoot() {


	var host = new sourceMapperTestHost(new Dictionary<string,string>{
		["/project/out/out.d.ts"] = "generated",
		["/project/out/out.d.ts.map"] = "source",
	});
	var mapper = ConvertDocumentToSourceMapper(
		host,
		"{\"version\":3,\"file\":\"out.d.ts\",\"sources\":[\"\"],\"names\":[],\"mappings\":\"AAAA\"}",
		"/project/out/out.d.ts.map");
	Assert( mapper != null);
	DeepEqual( mapper.GetSourcePosition(new DocumentPosition(RootedFilePath.FromGoString("/project/out/out.d.ts"), 0)), new DocumentPosition(RootedFilePath.FromGoString("/project/out/out.d.ts.map"), 0));
}
private static void TestSourceMapperPrefixesAbsoluteSourceWithNonemptySourceRoot() {


	var host = new sourceMapperTestHost(new Dictionary<string,string>{
		["/project/out/out.d.ts"] = "generated",
		["/project/src/actual/a.ts"] = "prefixed source",
		["/actual/a.ts"] = "unprefixed source",
	});
	var mapper = ConvertDocumentToSourceMapper(
		host,
		"{\"version\":3,\"file\":\"out.d.ts\",\"sourceRoot\":\"../src\",\"sources\":[\"/actual/a.ts\"],\"names\":[],\"mappings\":\"AAAA\"}",
		"/project/out/out.d.ts.map");
	Assert( mapper != null);
	DeepEqual( mapper.GetSourcePosition(new DocumentPosition(RootedFilePath.FromGoString("/project/out/out.d.ts"), 0)), new DocumentPosition(RootedFilePath.FromGoString("/project/src/actual/a.ts"), 0));
}
private static void TestSourceMapperRetainsDuplicateSourceIndices() {


	var host = new sourceMapperTestHost(new Dictionary<string,string>{
		["/project/out/out.d.ts"] = "generated",
		["/project/src"] = "source",
	});
	var mapper = ConvertDocumentToSourceMapper(
		host,
		"{\"version\":3,\"file\":\"out.d.ts\",\"sourceRoot\":\"../src\",\"sources\":[\"\",\"\"],\"names\":[],\"mappings\":\"AAAA\"}",
		"/project/out/out.d.ts.map");
	Assert( mapper != null);
	DeepEqual( mapper.GetGeneratedPosition(new DocumentPosition(RootedFilePath.FromGoString("/project/src"), 0)), new DocumentPosition(RootedFilePath.FromGoString("/project/out/out.d.ts"), 0));
}
private static void TestSourceMapperPreservesNullSourceEntries() {


	var host = new sourceMapperTestHost(new Dictionary<string,string>{
		["/project/out/out.d.ts"] = "generated",
		["/project/out/real.ts"] = "source",
	});
	var mapper = ConvertDocumentToSourceMapper(
		host,
		"{\"version\":3,\"file\":\"out.d.ts\",\"sources\":[null,\"real.ts\"],\"names\":[],\"mappings\":\"ACAA\"}",
		"/project/out/out.d.ts.map");
	Assert( mapper != null);
	DeepEqual( mapper.GetSourcePosition(new DocumentPosition(RootedFilePath.FromGoString("/project/out/out.d.ts"), 0)), new DocumentPosition(RootedFilePath.FromGoString("/project/out/real.ts"), 0));

	var nullMapper = ConvertDocumentToSourceMapper(
		host,
		"{\"version\":3,\"file\":\"out.d.ts\",\"sources\":[null],\"names\":[],\"mappings\":\"AAAA\"}",
		"/project/out/out.d.ts.map");
	Assert( nullMapper != null);
	Assert( nullMapper.GetSourcePosition(new DocumentPosition(RootedFilePath.FromGoString("/project/out/out.d.ts"), 0)) == null);
}
private static void TestSourceMapperIgnoresOutOfRangeSourceIndex() {


	var mapper = ConvertDocumentToSourceMapper(
		new sourceMapperTestHost(new Dictionary<string,string>()),
		"{\"version\":3,\"file\":\"out.d.ts\",\"sources\":[\"real.ts\"],\"names\":[],\"mappings\":\"ACAA\"}",
		"/project/out/out.d.ts.map");
	Assert( mapper != null);
	Assert( mapper.GetSourcePosition(new DocumentPosition(RootedFilePath.FromGoString("/project/out/out.d.ts"), 0)) == null);
}
private static void TestSourceMapperIgnoresSourceURLSuffix() {


	var host = new sourceMapperTestHost(new Dictionary<string,string>{
		["/project/out/out.d.ts"] = "generated",
	});
	var mapper = ConvertDocumentToSourceMapper(
		host,
		"{\"version\":3,\"file\":\"out.d.ts\",\"sources\":[\"https://example.com/source.ts?version=1\"],\"names\":[],\"mappings\":\"AAAA\"}",
		"/project/out/out.d.ts.map");
	Assert( mapper != null);
	Assert( mapper.GetSourcePosition(new DocumentPosition(RootedFilePath.FromGoString("/project/out/out.d.ts"), 0)) == null);
}
private static void TestSourceMapperIgnoresExternalMapURLSuffix() {


	const string generatedFile = "/project/out/out.d.ts";
	var host = new sourceMapperTestHost(new Dictionary<string,string>{
		[generatedFile] = "declare const value: number;\n//# sourceMappingURL=https://example.com/out.d.ts.map?version=1",
	});
	Assert( SourceMaps.GetDocumentPositionMapper(host, RootedFilePath.FromGoString(generatedFile)) == null);
}
    private sealed class ExpectedRawSourceMap
    {
        internal int Version;
        internal string File = "", SourceRoot = "", Mappings = "";
        internal string[] Sources = [], Names = [];
        internal string?[]? SourcesContent;
    }
    private static Generator NewGenerator(string file, string sourceRoot, string directory, CaseSensitivity sensitivity) => Generator.NewGenerator(file, sourceRoot, RootedDirectoryPath.FromGoString(directory), sensitivity);
    private static void Assert([DoesNotReturnIf(false)] bool value) { if (!value) throw new InvalidOperationException("SourceMap assertion failed"); }
    private static void Equal<T>(T actual, T expected) { if (!EqualityComparer<T>.Default.Equals(actual, expected)) throw new InvalidOperationException($"SourceMap expected {expected}, actual {actual}"); }
    private static void NilError(Exception? error) { if (error != null) throw new InvalidOperationException("Unexpected source-map error", error); }
    private static void Error(Exception? error, string expected) { Assert(error != null); Equal(error!.Message, expected); }
    private static void DeepEqual(DocumentPosition? actual, DocumentPosition? expected) => Equal(actual,expected);
    private static void DeepEqual(RawSourceMap actual, ExpectedRawSourceMap expected)
    {
        Equal(actual.Version, expected.Version); Equal(actual.File.Materialize(), expected.File); Equal(actual.SourceRoot.Materialize(), expected.SourceRoot); Equal(actual.Mappings.Materialize(), expected.Mappings);
        Assert(actual.Sources.Select(x=>x.Materialize()).SequenceEqual(expected.Sources)); Assert(actual.Names.Select(x=>x.Materialize()).SequenceEqual(expected.Names));
        if (expected.SourcesContent == null) Assert(actual.SourcesContent == null);
        else { Assert(actual.SourcesContent != null); Assert(actual.SourcesContent!.Select(x=>x?.Materialize()).SequenceEqual(expected.SourcesContent)); }
    }
    private sealed class sourceMapperTestHost(Dictionary<string,string> files) : Host
    {
        public CaseSensitivity CaseSensitivity() => TypeScript.Scanner.CaseSensitivity.CaseSensitive;
        public ECMALineInfo? GetECMALineInfo(RootedFilePath fileName) => files.TryGetValue(fileName.AsString(), out var text) ? ECMALineInfo.CreateECMALineInfo(text, Core.ComputeECMALineStarts(text)) : null;
        public (ByteStringValue Text,bool Ok) ReadFile(RootedFilePath fileName) => files.TryGetValue(fileName.AsString(), out var text) ? (ByteStringValue.FromString(text),true) : (default,false);
    }
    private static DocumentPositionMapper? ConvertDocumentToSourceMapper(Host host,string text,string path) => SourceMaps.convertDocumentToSourceMapper(host,text,RootedFilePath.FromGoString(path));


    // Original nested table: four source cases x absent/empty sourceRoot.
    private static void TestSourceMapperTreatsEmptySourceRootAsAbsent()
    {
        var host = new sourceMapperTestHost(new Dictionary<string,string> {
            ["/project/out/out.d.ts"] = "generated",
            ["/project/out/out.d.ts.map"] = "map-relative empty source",
            ["/project/out/a.ts"] = "map-relative source",
            ["/project/src/a.ts"] = "parent-relative source",
            ["/"] = "unrelated root", ["/a.ts"] = "unrelated root source", ["/missing.ts"] = "unrelated root source",
        });
        foreach (var test in new (string Name, string Source, string FileName)[] {
            ("empty source", "", "/project/out/out.d.ts.map"),
            ("relative source", "a.ts", "/project/out/a.ts"),
            ("parent-relative source", "../src/a.ts", "/project/src/a.ts"),
            ("missing source", "missing.ts", ""),
        })
        foreach (var root in new (string Name, string Field)[] { ("absent", ""), ("empty", "\"sourceRoot\":\"\",") })
        {
            var mapper = ConvertDocumentToSourceMapper(host, "{\"version\":3,\"file\":\"out.d.ts\","+root.Field+"\"sources\":[\""+test.Source+"\"],\"names\":[],\"mappings\":\"AAAA\"}", "/project/out/out.d.ts.map");
            Assert(mapper != null);
            var sourcePosition = mapper!.GetSourcePosition(new(RootedFilePath.FromGoString("/project/out/out.d.ts"), 0));
            if (test.FileName == "") { Assert(sourcePosition == null); continue; }
            DeepEqual(sourcePosition, new(RootedFilePath.FromGoString(test.FileName), 0));
            DeepEqual(mapper.GetGeneratedPosition(new(RootedFilePath.FromGoString(test.FileName), 0)), new(RootedFilePath.FromGoString("/project/out/out.d.ts"), 0));
        }
    }
    public static int RunAuthoredControls()
    {
        int authoredCaseCount = 0;
        foreach (var test in new (string Text, string Error)[] {
            ("!", "Invalid character in VLQ"),
            ("g", "Error in decoding base64VLQFormatDecode, past the mapping string"),
            ("D", "Invalid generatedCharacter found"),
            ("ADAA", "Invalid sourceIndex found"),
            ("AA", "Unsupported Format: No entries after sourceIndex"),
            ("AADA", "Invalid sourceLine found"),
            ("AAA", "Unsupported Format: No entries after sourceLine"),
            ("AAAD", "Invalid sourceCharacter found"),
            ("AAAAD", "Invalid nameIndex found"),
            ("AAAAAA", "Unsupported Error Format: Entries after nameIndex"),
        })
        {
            var decoder = SourceMaps.DecodeMappings(test.Text);
            Assert(decoder.Next().Done); Error(decoder.Error(), test.Error); Assert(decoder.Next().Done);
            authoredCaseCount++;
        }
        {
            var decoder = SourceMaps.DecodeMappings("AAAAA,CAAC;A");
            var first = decoder.Next().Value!; Equal(first, new Mapping(0,0,0,0,0,0));
            var second = decoder.Next().Value!; Equal(second, new Mapping(0,1,0,0,1,-1));
            var third = decoder.Next().Value!; Equal(third, new Mapping(1,0,-1,-1,-1,-1));
            Equal(first, new Mapping(0,0,0,0,0,0));
            Equal(decoder.State(),new Mapping(1,0,0,0,1,0));
            Equal(decoder.Pos(), "AAAAA,CAAC;A".Length); Assert(decoder.Next().Done); NilError(decoder.Error());
            authoredCaseCount++;
        }
        {
            // VLQ accumulates Go's 64-bit int and variable shifts >=64 yield zero.
            var decoder = SourceMaps.DecodeMappings("ggggggggggggggggggggA");
            Equal(decoder.Next().Value!.GeneratedCharacter,0L); NilError(decoder.Error());
            authoredCaseCount++;
        }
        {
            var gen = NewGenerator("out.js", "", "/", CaseSensitivity.CaseSensitive);
            var source = gen.AddSource(RootedFilePath.FromGoString("/a.ts")); Equal(gen.AddSource(RootedFilePath.FromGoString("/a.ts")),source);
            Equal(gen.AddName("n"),0); Equal(gen.AddName("n"),0);
            NilError(gen.SetSourceContent(source,""));
            var raw = gen.RawSourceMap(); raw.Sources[0] = ByteStringValue.FromString("changed"); raw.Names[0] = ByteStringValue.FromString("changed"); raw.SourcesContent![0] = ByteStringValue.FromString("changed");
            var next = gen.RawSourceMap(); Equal(next.Sources[0].Materialize(),"a.ts"); Equal(next.Names[0].Materialize(),"n"); Equal(next.SourcesContent![0]!.Value.Materialize(),"");
            Equal(gen.String(),"{\"version\":3,\"file\":\"out.js\",\"sources\":[\"a.ts\"],\"names\":[\"n\"],\"mappings\":\"\",\"sourcesContent\":[\"\"]}");
            Equal(gen.Base64DataURL(),"data:application/json;base64,"+Convert.ToBase64String(GoString.Bytes(gen.String())));
            authoredCaseCount++;
        }
        {
            // Source UTF8 offsets and UTF16 columns remain distinct.
            var host = new sourceMapperTestHost(new Dictionary<string,string> { ["/o.js"] = GoString.ToUtf8("😀x"), ["/a.ts"] = GoString.ToUtf8("😀y") });
            var mapper = ConvertDocumentToSourceMapper(host,"{\"version\":3,\"file\":\"o.js\",\"sources\":[\"a.ts\"],\"mappings\":\"EAAE\"}","/o.js.map")!;
            DeepEqual(mapper.GetSourcePosition(new(RootedFilePath.FromGoString("/o.js"),4)),new(RootedFilePath.FromGoString("/a.ts"),4));
            DeepEqual(mapper.GetGeneratedPosition(new(RootedFilePath.FromGoString("/a.ts"),4)),new(RootedFilePath.FromGoString("/o.js"),4));
            Assert(mapper.GetSourcePosition(new(RootedFilePath.FromGoString("/o.js"),5)) == null);
            authoredCaseCount++;
        }
        {
            var host = new sourceMapperTestHost(new Dictionary<string,string> { ["/o.js"] = "x", ["/a.ts"] = "y" });
            foreach (string map in new[] {
                "{\"version\":3.0,\"file\":\"o.js\",\"sources\":[\"a.ts\"],\"mappings\":\"AAAA\"}",
                "{\"version\":3,\"file\":\"o.js\",\"sources\":[1],\"mappings\":\"AAAA\"}",
                "{\"version\":3,\"file\":\"o.js\",\"sources\":[\"a.ts\"],\"mappings\":\"AAAA\",\"sourcesContent\":[\"y\"]}",
                "{\"version\":3,\"version\":3,\"file\":\"o.js\",\"sources\":[\"a.ts\"],\"mappings\":\"AAAA\"}",
            }) Assert(ConvertDocumentToSourceMapper(host,map,"/o.js.map") == null);
            authoredCaseCount++;
        }
        {
            var gen = NewGenerator("o.js", "", "/", CaseSensitivity.CaseSensitive);
            gen.AddSource(RootedFilePath.FromGoString("/a.ts")); NilError(gen.AddSourceMapping(0,0,0,0,0));
            var host = new sourceMapperTestHost(new Dictionary<string,string> { ["/o.js"] = "x\n//# sourceMappingURL="+gen.Base64DataURL()+"\n", ["/a.ts"] = "y" });
            var mapper = SourceMaps.GetDocumentPositionMapper(host,RootedFilePath.FromGoString("/o.js")); Assert(mapper != null);
            DeepEqual(mapper!.GetSourcePosition(new(RootedFilePath.FromGoString("/o.js"),0)),new(RootedFilePath.FromGoString("/a.ts"),0));
            authoredCaseCount++;
        }
        {
            var text = GoString.ToUtf8("x\n\u0085//@ sourceMappingURL=a.map\u00a0\n//# other=x\n");
            Equal(SourceMaps.TryGetSourceMappingURL(ECMALineInfo.CreateECMALineInfo(text,Core.ComputeECMALineStarts(text))).Materialize(),"a.map");
            var stop = "//# sourceMappingURL=a.map\nx";
            Equal(SourceMaps.TryGetSourceMappingURL(ECMALineInfo.CreateECMALineInfo(stop,Core.ComputeECMALineStarts(stop))).Length,0);
            authoredCaseCount++;
        }
        {
            var host = new sourceMapperTestHost(new Dictionary<string,string> { ["/o.js"] = "x", ["/a.ts"] = "y" });
            const string prefix = "{\"version\":3,\"file\":\"o.js\",\"sources\":[\"a.ts\"],\"mappings\":\"AAAA\",";
            foreach (string unknown in new[] { "\"unknown\":1e1000}", "\"unknown\":{\"deep\":[1e1000]}}" })
            {
                var mapper = ConvertDocumentToSourceMapper(host,prefix+unknown,"/o.js.map"); Assert(mapper != null);
                DeepEqual(mapper!.GetSourcePosition(new(RootedFilePath.FromGoString("/o.js"),0)),new(RootedFilePath.FromGoString("/a.ts"),0));
            }
            foreach (string duplicate in new[] { "\"unknown\":1e1000,\"unknown\":0}", "\"unknown\":{\"deep\":1e1000,\"deep\":0}}" })
                Assert(ConvertDocumentToSourceMapper(host,prefix+duplicate,"/o.js.map") == null);
            authoredCaseCount++;
        }
        {
            var host = new sourceMapperTestHost(new Dictionary<string,string> { ["/o.js"] = "x", ["/a.ts"] = "y" });
            var mapper = ConvertDocumentToSourceMapper(host,"{\"version\":3,\"file\":\"o.js\",\"sources\":[\"a.ts\"],\"mappings\":\"AAAA\"}","/o.js.map")!;
            DeepEqual(mapper.GetSourcePosition(new(RootedFilePath.FromGoString("/o.js"),int.MinValue)),new(RootedFilePath.FromGoString("/a.ts"),0));
            DeepEqual(mapper.GetGeneratedPosition(new(RootedFilePath.FromGoString("/a.ts"),int.MinValue)),new(RootedFilePath.FromGoString("/o.js"),0));
            authoredCaseCount++;
        }
        return authoredCaseCount;
    }

}
