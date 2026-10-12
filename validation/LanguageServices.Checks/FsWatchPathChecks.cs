// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Original watcher_test.go path comparer/callback assertions; retained raw input bytes.
using TypeScript.Scanner;
using TypeScript.FsWatch;
using FsWatch = TypeScript.FsWatch.FsWatch;

namespace LanguageServices.Checks;
public static class FsWatchPathChecks
{
    private static ByteStringValue Bytes(params byte[] bytes) => ByteStringValue.FromView(ByteStringView.FromOwnedBytes(bytes));
    private static ByteStringValue Text(string display) => ByteStringValue.FromString(GoString.ToUtf8(display));
    private static void Check(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message);
    }

    private readonly record struct Case(ByteStringValue Root, ByteStringValue Path, ByteStringValue Suffix, bool Exact, bool IgnoreCase);
    private static readonly Case[] Cases = [new(Bytes(47, 114, 111, 111, 116), Bytes(47, 114, 111, 111, 116), Bytes(), true, true), new(Bytes(47, 114, 111, 111, 116), Bytes(47, 114, 111, 111, 116, 47, 102, 105, 108, 101, 46, 116, 115), Bytes(47, 102, 105, 108, 101, 46, 116, 115), true, true), new(Bytes(47, 114, 111, 111, 116), Bytes(47, 82, 79, 79, 84), Bytes(), false, true), new(Bytes(47, 114, 111, 111, 116), Bytes(47, 82, 79, 79, 84, 47, 70, 105, 108, 101, 46, 116, 115), Bytes(47, 70, 105, 108, 101, 46, 116, 115), false, true), new(Bytes(47, 114, 111, 111, 116), Bytes(47, 82, 79, 79, 84, 47, 78, 101, 115, 116, 101, 100, 47, 70, 105, 108, 101, 46, 116, 115), Bytes(47, 78, 101, 115, 116, 101, 100, 47, 70, 105, 108, 101, 46, 116, 115), false, true), new(Bytes(47, 114, 111, 111, 116), Bytes(47, 82, 79, 79, 84, 50, 47, 70, 105, 108, 101, 46, 116, 115), Bytes(), false, false), new(Bytes(47, 114, 111, 111, 116), Bytes(47, 114, 111, 111), Bytes(), false, false), new(Bytes(47, 114, 111, 111, 116, 47, 115, 117, 98), Bytes(47, 82, 79, 79, 84), Bytes(), false, false), new(Bytes(47, 114, 111, 111, 116), Bytes(47, 111, 116, 104, 101, 114, 47, 70, 105, 108, 101, 46, 116, 115), Bytes(), false, false), new(Bytes(47, 114, 111, 111, 116), Bytes(47, 82, 79, 79, 84, 105, 115, 104, 47, 70, 105, 108, 101, 46, 116, 115), Bytes(), false, false), new(Bytes(47, 114, 111, 111, 116, 47, 115, 117, 98), Bytes(47, 82, 79, 79, 84, 47, 83, 85, 66), Bytes(), false, true), new(Bytes(47, 114, 111, 111, 116, 47, 115, 117, 98), Bytes(47, 82, 79, 79, 84, 47, 115, 117), Bytes(), false, false), new(Bytes(47, 114, 111, 111, 116, 47, 91), Bytes(47, 82, 79, 79, 84, 47, 123, 47, 70, 105, 108, 101, 46, 116, 115), Bytes(), false, false), new(Bytes(47, 114, 111, 111, 116, 47, 64), Bytes(47, 82, 79, 79, 84, 47, 96, 47, 70, 105, 108, 101, 46, 116, 115), Bytes(), false, false), new(Bytes(47), Bytes(47, 70, 105, 108, 101, 46, 116, 115), Bytes(70, 105, 108, 101, 46, 116, 115), true, true), new(Bytes(47), Bytes(47), Bytes(), true, true), new(Bytes(), Bytes(), Bytes(), false, false), new(Bytes(), Bytes(47, 70, 105, 108, 101, 46, 116, 115), Bytes(), false, false), new(Bytes(47, 99, 97, 102, 195, 169), Bytes(47, 67, 65, 70, 195, 137, 47, 70, 105, 108, 101, 46, 116, 115), Bytes(47, 70, 105, 108, 101, 46, 116, 115), false, true), new(Bytes(47, 115), Bytes(47, 197, 191, 47, 70, 105, 108, 101, 46, 116, 115), Bytes(47, 70, 105, 108, 101, 46, 116, 115), false, true), new(Bytes(47, 197, 191), Bytes(47, 83, 47, 70, 105, 108, 101, 46, 116, 115), Bytes(47, 70, 105, 108, 101, 46, 116, 115), false, true), new(Bytes(47, 115, 47, 115, 117, 98), Bytes(47, 197, 191, 47, 83, 85, 66, 47, 70, 105, 108, 101, 46, 116, 115), Bytes(47, 70, 105, 108, 101, 46, 116, 115), false, true), new(Bytes(47, 197, 191, 47, 115, 117, 98), Bytes(47, 83, 47, 83, 85, 66, 47, 70, 105, 108, 101, 46, 116, 115), Bytes(47, 70, 105, 108, 101, 46, 116, 115), false, true), new(Bytes(47, 115), Bytes(47, 197, 191, 111, 111, 47, 70, 105, 108, 101, 46, 116, 115), Bytes(), false, false), new(Bytes(47, 107), Bytes(47, 226, 132, 170, 47, 70, 105, 108, 101, 46, 116, 115), Bytes(47, 70, 105, 108, 101, 46, 116, 115), false, true), new(Bytes(47, 207, 131), Bytes(47, 207, 130, 47, 70, 105, 108, 101, 46, 116, 115), Bytes(47, 70, 105, 108, 101, 46, 116, 115), false, true), new(Bytes(47, 195, 169), Bytes(47, 195, 136, 47, 70, 105, 108, 101, 46, 116, 115), Bytes(), false, false), new(Bytes(47, 195, 159), Bytes(47, 83, 83, 47, 70, 105, 108, 101, 46, 116, 115), Bytes(47, 70, 105, 108, 101, 46, 116, 115), false, FsWatch.NativePathFolding), new(Bytes(47, 114, 111, 111, 116, 47, 115), Bytes(47, 82, 79, 79, 84, 47, 197, 191, 47, 70, 105, 108, 101, 46, 116, 115), Bytes(47, 70, 105, 108, 101, 46, 116, 115), false, true), new(Bytes(47, 114, 111, 111, 116, 47, 197, 191), Bytes(47, 82, 79, 79, 84, 47, 83), Bytes(), false, true), ];
    public static void TestPathComparer()
    {
        foreach (var item in Cases)
            foreach (bool ignoreCase in new[]
            {
                false,
                true
            }

            )
            {
                var comparer = new PathComparer(ignoreCase);
                bool expected = ignoreCase ? item.IgnoreCase : item.Exact;
                var(suffix, ok) = comparer.Suffix(item.Root, item.Path);
                Check(ok == expected && (!ok || suffix.EqualsValue(item.Suffix)), "suffix differs");
                Check(comparer.Contains(item.Root, item.Path) == expected, "contains differs");
                foreach (var destination in new[]
                {
                    Text("/display"),
                    Text("/")
                }

                )
                {
                    var(path, rebased) = comparer.Rebase(item.Path, item.Root, destination);
                    Check(rebased == expected && (!rebased || path.EqualsValue(FsWatch.JoinPathSuffix(destination, item.Suffix))), "rebase differs");
                }
            }
    }

    public static void TestPathComparerUnicodeAlignment()
    {
        ByteStringValue[] parts = [Bytes(115), Bytes(83), Bytes(197, 191), Bytes(107), Bytes(75), Bytes(226, 132, 170), Bytes(207, 131), Bytes(207, 130), Bytes(195, 169), Bytes(195, 137), Bytes(195, 136), Bytes(240, 144, 144, 128), Bytes(240, 144, 144, 168), Bytes(255), Bytes(254), Bytes(195)];
        var comparer = new PathComparer(true);
        for (int padding = 0; padding < 16; padding++)
        {
            var prefix = Text("/" + new string ('a', padding));
            foreach (var a in parts)
                foreach (var b in parts)
                    foreach (string child in new[]
                    {
                        "",
                        "/child"
                    }

                    )
                    {
                        var root = ByteStringValue.Concat(prefix, a, Text(child));
                        var path = ByteStringValue.Concat(prefix, b, Text(child.ToUpperInvariant() + "/File.ts"));
                        bool expected = UnicodeCase.EqualFold(a, b);
                        var(suffix, ok) = comparer.Suffix(root, path);
                        Check(ok == expected && (!ok || suffix.EqualsBytes("/File.ts"u8)), "Unicode-aligned suffix differs");
                    }
        }
    }

    public static void TestFileCallbackCaseSensitivity()
    {
        foreach (bool ignoreCase in new[]
        {
            false,
            true
        }

        )
        {
            var values = Slice<WatchEvent>.Make(0);
            var watch = DirWatch.NewDirWatch(Text("/root"), Text("/root"), Debounce.NewDebounce());
            watch.Recursive = true;
            try
            {
                watch.SetComparer(new PathComparer(ignoreCase));
                watch.AddCallback(Text("/root"), Text("/root"), false, (events, _) => values = values.Append(events), null, Text("/root/file.ts"));
                watch.Events.Update(Text("/root/FILE.ts"));
                watch.Events.Update(Text("/root/other.ts"));
                watch.TriggerCallbacks();
                Check(ignoreCase ? values.Length == 1 && values[0].Path.EqualsBytes("/root/file.ts"u8) : values.Length == 0, "case-sensitive callback differs");
            }
            finally
            {
                watch.DestroyDebounce();
            }
        }
    }

    public static void TestPathComparerExactKeys()
    {
        var comparer = new PathComparer();
        foreach (var path in new ByteStringValue[]
        {
            Bytes(47, 65, 47, 70, 105, 108, 101, 46, 116, 115),
            Bytes(47, 115, 116, 114, 97, 195, 159, 101, 47, 196, 176, 46, 116, 115),
            Bytes(47, 99, 97, 102, 101, 204, 129, 46, 116, 115),
            Bytes(47, 98, 97, 100, 255, 46, 116, 115)
        }

        )
            Check(comparer.Key(path).EqualsValue(path), "exact key differs");
        Check(!comparer.Rebase(Text("/A/file.ts"), Text("/a"), Text("/target")).Ok, "zero comparer must use exact matching");
        var(rebased, ok) = comparer.Rebase(Text("/a/file.ts"), Text("/a"), Text("/target"));
        Check(ok && rebased.EqualsBytes("/target/file.ts"u8), "exact rebase differs");
    }

    public static readonly string[] Names = [nameof(TestPathComparer), nameof(TestPathComparerUnicodeAlignment), nameof(TestFileCallbackCaseSensitivity), nameof(TestPathComparerExactKeys)];
    public static int RunAll()
    {
        foreach (var name in Names)
            Run(name);
        return Names.Length;
    }

    public static void Run(string name)
    {
        switch (name)
        {
            case nameof(TestPathComparer):
                TestPathComparer();
                break;
            case nameof(TestPathComparerUnicodeAlignment):
                TestPathComparerUnicodeAlignment();
                break;
            case nameof(TestFileCallbackCaseSensitivity):
                TestFileCallbackCaseSensitivity();
                break;
            case nameof(TestPathComparerExactKeys):
                TestPathComparerExactKeys();
                break;
            default:
                throw new ArgumentException(name);
        }
    }
}
