from pathlib import Path
import re
root=Path.cwd()
source=(root/'upstream/TypeScript/tsc/internal/fswatch/watcher_test.go').read_text()
def raw(literal):
    s=literal[1:-1]; out=bytearray(); i=0
    while i<len(s):
        if s[i]!='\\': out.extend(s[i].encode());i+=1;continue
        i+=1;c=s[i];i+=1
        if c in 'uU': n=4 if c=='u' else 8;out.extend(chr(int(s[i:i+n],16)).encode());i+=n
        elif c=='x':out.append(int(s[i:i+2],16));i+=2
        else:out.extend({'n':b'\n','t':b'\t','r':b'\r','"':b'"','\\':b'\\'}[c])
    return 'Bytes('+','.join(str(x) for x in out)+')'
lit=r'"(?:[^"\\]|\\.)*"'
body=source.split('func TestPathComparer(t')[1].split('func TestPathComparerUnicodeAlignment')[0]
rows=re.findall(r'\{('+lit+r'), ('+lit+r'), ('+lit+r'), (true|false), (true|false|nativePathFolding)\}',body)
assert len(rows)==30
parts=re.search(r'parts := \[\]string\{([^\n]+)\}',source.split('func TestPathComparerUnicodeAlignment')[1]).group(1)
part_literals=re.findall(lit,parts)
text='''// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Original watcher_test.go path comparer/callback assertions; retained raw input bytes.
using TypeScript.Scanner;
using TypeScript.FsWatch;
namespace LanguageServices.Checks;
public static class FsWatchPathChecks
{
    private static ByteStringValue Bytes(params byte[] bytes) => ByteStringValue.FromView(ByteStringView.FromOwnedBytes(bytes));
    private static ByteStringValue Text(string display) => ByteStringValue.FromString(GoString.ToUtf8(display));
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private readonly record struct Case(ByteStringValue Root, ByteStringValue Path, ByteStringValue Suffix, bool Exact, bool IgnoreCase);
    private static readonly Case[] Cases = [
'''
for a,b,c,d,e in rows:text+='        new('+','.join([raw(a),raw(b),raw(c),d,'FsWatch.NativePathFolding' if e=='nativePathFolding' else e])+'),\n'
text+='''    ];
    public static void TestPathComparer()
    {
        foreach (var item in Cases)
            foreach (bool ignoreCase in new[] { false, true })
            {
                var comparer = new PathComparer(ignoreCase);
                bool expected = ignoreCase ? item.IgnoreCase : item.Exact;
                var (suffix, ok) = comparer.Suffix(item.Root, item.Path);
                Check(ok == expected && (!ok || suffix.EqualsValue(item.Suffix)), "suffix differs");
                Check(comparer.Contains(item.Root, item.Path) == expected, "contains differs");
                foreach (var destination in new[] { Text("/display"), Text("/") })
                {
                    var (path, rebased) = comparer.Rebase(item.Path, item.Root, destination);
                    Check(rebased == expected && (!rebased || path.EqualsValue(FsWatch.JoinPathSuffix(destination, item.Suffix))), "rebase differs");
                }
            }
    }
    public static void TestPathComparerUnicodeAlignment()
    {
        ByteStringValue[] parts = ['''+','.join(map(raw,part_literals))+'''];
        var comparer = new PathComparer(true);
        for (int padding = 0; padding < 16; padding++)
        {
            var prefix = Text("/" + new string('a', padding));
            foreach (var a in parts) foreach (var b in parts) foreach (string child in new[] { "", "/child" })
            {
                var root = ByteStringValue.Concat(prefix, a, Text(child));
                var path = ByteStringValue.Concat(prefix, b, Text(child.ToUpperInvariant() + "/File.ts"));
                bool expected = UnicodeCase.EqualFold(a, b);
                var (suffix, ok) = comparer.Suffix(root, path);
                Check(ok == expected && (!ok || suffix.EqualsBytes("/File.ts"u8)), "Unicode-aligned suffix differs");
            }
        }
    }
    public static void TestFileCallbackCaseSensitivity()
    {
        foreach (bool ignoreCase in new[] { false, true })
        {
            var values = Slice<WatchEvent>.Make(0);
            var watch = DirWatch.NewDirWatch(Text("/root"), Text("/root"), Debounce.NewDebounce());
            watch.Recursive = true;
            try
            {
                watch.SetComparer(new PathComparer(ignoreCase));
                watch.AddCallback(Text("/root"), Text("/root"), false, (events, _) => values = values.Append(events), null, Text("/root/file.ts"));
                watch.Events.Update(Text("/root/FILE.ts")); watch.Events.Update(Text("/root/other.ts"));
                watch.TriggerCallbacks();
                Check(ignoreCase ? values.Length == 1 && values[0].Path.EqualsBytes("/root/file.ts"u8) : values.Length == 0, "case-sensitive callback differs");
            }
            finally { watch.DestroyDebounce(); }
        }
    }
    public static void TestPathComparerExactKeys()
    {
        var comparer = new PathComparer();
        foreach (var path in new ByteStringValue[] {'''+','.join(raw(x) for x in ['"/A/File.ts"','"/straße/İ.ts"','"/cafe\\u0301.ts"','"/bad\\xff.ts"'])+'''})
            Check(comparer.Key(path).EqualsValue(path), "exact key differs");
        Check(!comparer.Rebase(Text("/A/file.ts"), Text("/a"), Text("/target")).Ok, "zero comparer must use exact matching");
        var (rebased, ok) = comparer.Rebase(Text("/a/file.ts"), Text("/a"), Text("/target"));
        Check(ok && rebased.EqualsBytes("/target/file.ts"u8), "exact rebase differs");
    }
    public static readonly string[] Names = [nameof(TestPathComparer), nameof(TestPathComparerUnicodeAlignment), nameof(TestFileCallbackCaseSensitivity), nameof(TestPathComparerExactKeys)];
    public static int RunAll() { foreach (var name in Names) Run(name); return Names.Length; }
    public static void Run(string name)
    {
        switch (name)
        {
            case nameof(TestPathComparer): TestPathComparer(); break;
            case nameof(TestPathComparerUnicodeAlignment): TestPathComparerUnicodeAlignment(); break;
            case nameof(TestFileCallbackCaseSensitivity): TestFileCallbackCaseSensitivity(); break;
            case nameof(TestPathComparerExactKeys): TestPathComparerExactKeys(); break;
            default: throw new ArgumentException(name);
        }
    }
}
'''
(root/'validation/LanguageServices.Checks/FsWatchPathChecks.cs').write_text(text)
