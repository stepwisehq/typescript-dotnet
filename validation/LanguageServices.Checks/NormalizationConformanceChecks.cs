// Copyright 2011 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// x/text v0.42.0 unicode/norm/ucd_test.go selected NFD.String conformance channels.
// Other forms/Bytes/Iter/Transform/Append/IsNormal assertions are not credited.
using System.Globalization;
using System.IO.Compression;
using TypeScript.Scanner;
using TypeScript.UnicodeNormalization;
namespace LanguageServices.Checks;
public static class NormalizationConformanceChecks
{
    public static readonly string[] Names = ["TestStandardTests.NfdString", "TestCharacterByCharacter.NfdString"];
    public static int RunAll()
    {
        foreach (string name in Names) Run(name);
        return Names.Length;
    }
    public static void Run(string name)
    {
        bool characters = name == Names[1];
        if (!characters && name != Names[0]) throw new ArgumentException(name);
        using var resource = typeof(NormalizationConformanceChecks).Assembly.GetManifestResourceStream(
            "LanguageServices.Checks.RegexData.NormalizationTest-17.0.0.txt.gz")
            ?? throw new InvalidOperationException("Missing original Unicode17 conformance data");
        using var compressed = new GZipStream(resource, CompressionMode.Decompress);
        using var reader = new StreamReader(compressed);
        int part = -1, last = 0, observations = 0;
        while (reader.ReadLine() is { } line)
        {
            if (line.StartsWith("@Part", StringComparison.Ordinal))
            {
                part = line[5] - '0';
                continue;
            }
            if (line.Length == 0 || line[0] == '#' || (characters ? part != 1 : part is not (0 or 2 or 3)))
                continue;
            var fields = line.Split(';');
            if (fields.Length < 6) throw new InvalidOperationException("Invalid original conformance row");
            var columns = new ByteStringValue[5];
            for (int index = 0; index < 5; index++)
            {
                var output = new GoBuilder();
                foreach (string hex in fields[index].Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    output.WriteString(GoString.FromRuneValue(int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture)));
                columns[index] = ByteStringValue.FromString(output.String());
            }
            if (characters)
            {
                int rune = int.Parse(fields[0].Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                for (last++; last < rune; last++) Check(GoString.FromRuneValue(last), GoString.FromRuneValue(last));
            }
            for (int index = 0; index <= 2; index++) Check(columns[index], columns[2]);
            for (int index = 3; index <= 4; index++) Check(columns[index], columns[4]);
            observations++;
        }
        if (characters)
            for (last++; last < 0x2FA1E; last++) Check(GoString.FromRuneValue(last), GoString.FromRuneValue(last));
        if (observations == 0) throw new InvalidOperationException("No original conformance rows observed");
    }
    private static void Check(ByteStringValue input, ByteStringValue expected)
    {
        if (!Norm.String(input).EqualsValue(expected))
            throw new InvalidOperationException("Original NFD.String Unicode17 conformance assertion differs");
    }
}
