// Authored compatibility regressions for shared JSDoc fragments, not upstream test ports.
// Source contracts: ast generated JSDocText/Link{,Plain,Code} clone/update/text fields;
// parser/jsdoc.go parseJSDocCommentWorker/parseJSDocLink; scanner.go Mark/Rewind.
using TypeScript.Scanner;
using System.Runtime.InteropServices;
using ScannerApi = TypeScript.Scanner.Scanner;

namespace TypeScript.Foundation;

public static class JSDocFragmentChecks
{
    public static IReadOnlyList<string> Cases { get; } =
    [
        "public-list-identity", "null-empty-update", "clone-before-projection",
        "clone-after-projection", "setter-detaches-owner", "visitor-name-update",
        "syntax-projection", "raw-fragment-bytes", "scanner-borrowed-rewind",
        "scanner-token-reset", "borrowed-source-backing", "factory-hook-publication",
        "concurrent-first-projection"
    ];

    public static void Run(string name)
    {
        switch (name)
        {
            case "public-list-identity": PublicLists(); break;
            case "null-empty-update": NullEmpty(); break;
            case "clone-before-projection": CloneSharing(false); break;
            case "clone-after-projection": CloneSharing(true); break;
            case "setter-detaches-owner": SetterDetach(); break;
            case "visitor-name-update": VisitorUpdate(); break;
            case "syntax-projection": SyntaxProjection(); break;
            case "raw-fragment-bytes": RawBytes(); break;
            case "scanner-borrowed-rewind": ScannerRewind(); break;
            case "scanner-token-reset": ScannerReset(); break;
            case "borrowed-source-backing": BorrowedBacking(); break;
            case "factory-hook-publication": HookPublication(); break;
            case "concurrent-first-projection": ConcurrentProjection(); break;
            default: throw new ArgumentException("Unknown JSDoc fragment check: " + name, nameof(name));
        }
    }

    public static int RunAll()
    {
        foreach (var name in Cases) Run(name);
        return Cases.Count;
    }

    private static readonly Kind[] Kinds = [Kind.JSDocText, Kind.JSDocLink, Kind.JSDocLinkPlain, Kind.JSDocLinkCode];
    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
    private static JSDocCommentBase Fragments(Node node) => (JSDocCommentBase)node.data;
    private static Node Create(NodeFactory f, Kind kind, List<string>? parts, Node? name = null) => kind switch
    {
        Kind.JSDocText => f.NewJSDocText(parts!),
        Kind.JSDocLink => f.NewJSDocLink(name!, parts!),
        Kind.JSDocLinkPlain => f.NewJSDocLinkPlain(name!, parts!),
        Kind.JSDocLinkCode => f.NewJSDocLinkCode(name!, parts!),
        _ => throw new InvalidOperationException("Unexpected fragment kind")
    };
    private static Node Update(NodeFactory f, Node node, Node? name, List<string>? parts) => node.Kind switch
    {
        Kind.JSDocLink => f.UpdateJSDocLink(node.AsJSDocLink(), name!, parts!),
        Kind.JSDocLinkPlain => f.UpdateJSDocLinkPlain(node.AsJSDocLinkPlain(), name!, parts!),
        Kind.JSDocLinkCode => f.UpdateJSDocLinkCode(node.AsJSDocLinkCode(), name!, parts!),
        _ => throw new InvalidOperationException("Only named fragments have generated updates")
    };

    private static List<Node> ParsedFragments(string? bytes = null)
    {
        bytes ??= "/** prose {@link target first} {@linkplain target second} {@linkcode target third} */\nfunction f() {}";
        var file = Parser.ParseSourceFile(new("/fragments.js", "/fragments.js"), GoString.Bytes(bytes), ScriptKind.JS);
        Require((file.Diagnostics()?.Count ?? 0) == 0 && (file.JSDocDiagnostics()?.Count ?? 0) == 0, "Fragment source parses without diagnostics");
        var found = new List<Node>();
        void Walk(Node node)
        {
            if (Kinds.Contains(node.Kind)) found.Add(node);
            node.ForEachChild(child => { Walk(child); return false; });
        }
        foreach (var doc in file.Statements.Nodes[0].JSDoc(file)) Walk(doc);
        Require(Kinds.All(kind => found.Any(node => node.Kind == kind)), "All four parsed fragment models present");
        return Kinds.Select(kind => found.First(node => node.Kind == kind)).ToList();
    }

    private static void PublicLists()
    {
        var f = new NodeFactory();
        foreach (var kind in Kinds)
        {
            List<string> parts = ["a", "b"];
            var node = Create(f, kind, parts, f.NewIdentifier("target"));
            Require(ReferenceEquals(parts, Fragments(node).text), "Factory retains supplied mutable list: " + kind);
            parts[0] = "changed";
            parts.Add("tail");
            Require(node.Text() == "changedbtail", "External list mutations remain observable: " + kind);
            Require(ReferenceEquals(parts, Fragments(node).text), "Repeated getter retains list identity");
        }
    }

    private static void NullEmpty()
    {
        var f = new NodeFactory();
        foreach (var kind in Kinds)
        {
            var node = Create(f, kind, null, f.NewIdentifier("target"));
            Require(Fragments(node).text is null && node.Text() == "", "Null fragments remain null with empty public text");
            var clone = node.Clone(f);
            Require(Fragments(clone).text is null, "Cloning null retains null");
            List<string> empty = [];
            if (kind != Kind.JSDocText)
                Require(ReferenceEquals(node, Update(f, node, node.Name(), empty)), "Existing empty/null update equivalence retained");
            Fragments(node).text = empty;
            Require(ReferenceEquals(empty, Fragments(node).text), "Empty setter retains supplied list");
            if (kind != Kind.JSDocText)
                Require(ReferenceEquals(node, Update(f, node, node.Name(), null)), "Existing null/empty update equivalence retained");
            Fragments(node).text = null!;
            Require(Fragments(node).text is null, "Null setter restores null");
        }
    }

    private static void CloneSharing(bool projectFirst)
    {
        var f = new NodeFactory();
        foreach (var node in ParsedFragments())
        {
            if (projectFirst) _ = Fragments(node).text;
            var clone = node.Clone(f);
            Require(!ReferenceEquals(node, clone), "Clone is a distinct node");
            var parts = Fragments(clone).text;
            Require(ReferenceEquals(parts, Fragments(node).text), "Clone shares canonical mutable list before/after projection");
            parts.Add("mutation");
            Require(clone.Text() == node.Text() && node.Text().EndsWith("mutation", StringComparison.Ordinal), "Clone list mutation reaches original");
            parts[0] = "replacement";
            Require(node.Text().StartsWith("replacement", StringComparison.Ordinal), "Replacement visible in original");
        }
    }

    private static void SetterDetach()
    {
        var f = new NodeFactory();
        foreach (var node in ParsedFragments())
        {
            var clone = node.Clone(f);
            var original = Fragments(node).text;
            List<string> replacement = ["detached"];
            Fragments(clone).text = replacement;
            original.Add("shared-original");
            Require(ReferenceEquals(original, Fragments(node).text) && ReferenceEquals(replacement, Fragments(clone).text), "Setter detaches only clone owner");
            Require(clone.Text() == "detached" && node.Text().EndsWith("shared-original", StringComparison.Ordinal), "Detached text remains independent");
            Fragments(clone).text = null!;
            Require(Fragments(clone).text is null && ReferenceEquals(original, Fragments(node).text), "Null setter leaves original intact");
        }
    }

    private static void VisitorUpdate()
    {
        var f = new NodeFactory();
        foreach (var node in ParsedFragments().Where(n => n.Kind != Kind.JSDocText))
        {
            Require(ReferenceEquals(node, node.VisitEachChild(new(n => n, f))), "No-op visitor preserves node identity");
            var name = f.NewIdentifier("renamed");
            var changed = node.VisitEachChild(new(n => ReferenceEquals(n, node.Name()) ? name : n, f));
            Require(!ReferenceEquals(node, changed) && ReferenceEquals(name, changed.Name()), "Name-changing visitor updates name");
            var parts = Fragments(changed).text;
            Require(ReferenceEquals(parts, Fragments(node).text), "Name update shares text list");
            parts.Add("visitor-mutation");
            Require(node.Text() == changed.Text(), "Visitor-created node shares future mutations");
            List<string> newParts = ["explicit update"];
            var updated = Update(f, changed, name, newParts);
            Require(!ReferenceEquals(changed, updated) && ReferenceEquals(newParts, Fragments(updated).text), "Explicit nonempty update retains replacement list");
        }
    }

    private static void SyntaxProjection()
    {
        foreach (var node in ParsedFragments())
        {
            var field = node.SyntaxFields().Single(f => f.Role == "text");
            Require(field.Type == "strings" && ReferenceEquals(field.Value, Fragments(node).text), "Schema exposes canonical public string list");
            var parts = Fragments(node).text;
            parts.Clear(); parts.Add("schema");
            Require(node.Text() == "schema" && ReferenceEquals(node.SyntaxFields().Single(f => f.Role == "text").Value, parts), "Schema and Node.Text observe mutable projection");
        }
    }

    private static void RawBytes()
    {
        var bytes = GoString.ToUtf8("é 🦀") + GoString.FromBytes([0xed, 0xa0, 0x80, 0xff]);
        var f = new NodeFactory();
        foreach (var kind in Kinds)
        {
            var node = Create(f, kind, [bytes[..2], bytes[2..]], f.NewIdentifier("target"));
            Require(node.Text() == bytes && GoString.Bytes(node.Text()).SequenceEqual(GoString.Bytes(bytes)), "Fragments preserve malformed/WTF8 bytes without Unicode decoding");
            Require(node.Clone(f).Text() == bytes, "Cloned raw fragments preserve bytes");
        }
    }

    private static void ScannerRewind()
    {
        string bytes = GoString.ToUtf8("é 🦀") + GoString.FromBytes([0xff]) + " @tag";
        var s = new ScannerApi();
        s.SetByteTextInternal(bytes);
        var kind = s.ScanJSDocCommentTextToken(false);
        var state = s.Mark();
        var expected = s.TokenValueBytes();
        int start = s.TokenStart(), end = s.TokenEnd();
        Require(expected.Length > 0, "Borrowed prose token present");
        s.SetText("different"); s.Scan();
        s.SetByteTextInternal(bytes); s.Rewind(state);
        Require(s.Token() == kind && s.TokenStart() == start && s.TokenEnd() == end, "Saved token boundaries restored after text replacement");
        Require(s.TokenValueBytes().SequenceEqual(expected), "Saved borrowed token value restored byte-exact");
        Require(s.TokenTextBytes().SequenceEqual(GoString.Bytes(bytes[start..end])), "Token text remains bounded to original source");
    }

    private static void ScannerReset()
    {
        var s = new ScannerApi();
        s.SetByteTextInternal("prose @tag"); s.ScanJSDocCommentTextToken(false);
        s.SetText("'café'");
        Require(s.TokenValue() == "", "SetText clears borrowed token value");
        Require(s.Scan() == Kind.StringLiteral && s.TokenValue() == "café", "Owned token replaces borrowed value");
        s.Reset();
        Require(s.TokenValue() == "" && s.Text() == "", "Reset clears token and source");
        s.SetText("name");
        Require(s.Scan() == Kind.Identifier && s.TokenValue() == "name", "Identifier after reset does not expose stale prose");
    }

    private static JSDocTextFragments Storage(Node node) => node.Kind switch
    {
        Kind.JSDocText => node.AsJSDocText().TextFragments,
        Kind.JSDocLink => node.AsJSDocLink().TextFragments,
        Kind.JSDocLinkPlain => node.AsJSDocLinkPlain().TextFragments,
        Kind.JSDocLinkCode => node.AsJSDocLinkCode().TextFragments,
        _ => throw new InvalidOperationException("Unexpected fragment kind")
    };

    private static void BorrowedBacking()
    {
        foreach (var node in ParsedFragments())
        {
            var source = Ast.GetSourceFileOfNode(node).TextView();
            Require(MemoryMarshal.TryGetArray(source.Memory, out var sourceMemory), "Source owns byte-array storage");
            var parts = Storage(node);
            Require(parts.Count > 0, "Parsed text has fragments: " + node.Kind);
            bool shared = false;
            for (int i = 0; i < parts.Count; i++)
            {
                if (MemoryMarshal.TryGetArray(parts[i].Memory, out var memory)
                    && ReferenceEquals(sourceMemory.Array, memory.Array) && memory.Count > 0)
                {
                    shared = true;
                    Require(memory.Offset >= sourceMemory.Offset && memory.Offset + memory.Count <= sourceMemory.Offset + sourceMemory.Count, "Fragment bounds stay inside source");
                }
            }
            Require(shared, "Parsed model borrows immutable source before public projection: " + node.Kind);
            var clone = node.Clone(new NodeFactory());
            Require(ReferenceEquals(parts, Storage(clone)), "Clone shares storage before public projection");
            var list = Fragments(node).text;
            list.Clear(); list.Add("public mutation");
            Require(node.Text() == "public mutation" && clone.Text() == "public mutation", "Internal joins follow canonical projection after mutation");
        }
    }

    private static void HookPublication()
    {
        foreach (var node in ParsedFragments())
        {
            string expected = node.Text();
            int creates = 0, clones = 0;
            List<string>? observed = null;
            var f = new NodeFactory(new()
            {
                OnCreate = created =>
                {
                    creates++;
                    Require(created.Kind == node.Kind && created.Text() == expected, "Create hook observes complete cloned text");
                    observed = Fragments(created).text;
                },
                OnClone = (created, original) =>
                {
                    clones++;
                    Require(ReferenceEquals(original, node) && ReferenceEquals(observed, Fragments(created).text), "Clone hook sees canonical text published before creation");
                }
            });
            var clone = node.Clone(f);
            Require(creates == 1 && clones == 1 && f.TextCount() == 1, "Clone retains exact factory hook/text-count publication");
            Require(ReferenceEquals(observed, Fragments(node).text) && ReferenceEquals(observed, Fragments(clone).text), "Hook projection retains shared mutable alias");
        }
    }

    private static void ConcurrentProjection()
    {
        foreach (var node in ParsedFragments())
        {
            // Reader concurrency is supported; mutable-list writes remain externally synchronized.
            var clones = Enumerable.Range(0, 16).Select(_ => node.Clone(new NodeFactory())).ToArray();
            var lists = new List<string>[clones.Length];
            Parallel.For(0, clones.Length, i => lists[i] = Fragments(clones[i]).text);
            Require(lists.All(list => ReferenceEquals(list, lists[0])), "Concurrent first getters publish one canonical mutable list");
            lists[0].Clear(); lists[0].Add("after readers");
            Require(node.Text() == "after readers" && clones.All(clone => clone.Text() == "after readers"), "All aliases observe subsequent synchronized list mutation");
        }
    }
}
