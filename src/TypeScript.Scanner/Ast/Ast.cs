// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/ast/ast.go; syntax and parser support slice.
#nullable disable
namespace TypeScript.Scanner;
public enum CommentDirectiveKind
{
    Unknown,
    ExpectError,
    Ignore
}

public readonly record struct CommentDirective(TextRange Loc, CommentDirectiveKind Kind);
public delegate bool Visitor(Node node);
public readonly record struct SyntaxField(string Role, string Type, object Value);
public interface NodeFactoryCoercible
{
    NodeFactory AsNodeFactory();
}

public sealed class NodeFactoryHooks
{
    public Action<Node> OnCreate;
    public Action<Node, Node> OnUpdate;
    public Action<Node, Node> OnClone;
}

public class NodeList
{
    // The pure shared header keeps public missing-state reads independent of
    // Parser's static constructor and its lazy-JSDoc registration callback.
    internal static readonly Slice<Node> MissingNodes = Slice<Node>.Make(0, 1);
    public TextRange Loc;
    // ast.go:123. Header clones share slots but have independent len/cap.
    public Slice<Node> Nodes;
    // parser.go:111–116. Missing state belongs to the shared sentinel header,
    // so header clones and subsequent Nodes mutations determine it directly.
    public bool IsMissing => Nodes.Capacity == 1 &&
        Nodes.Slice2(0, 1).SameStorageRange(MissingNodes.Slice2(0, 1));
    public int Pos() => Loc.Pos();
    public int End() => Loc.End();
    public bool HasTrailingComma() => Nodes is { Count: > 0 } && Nodes[^1].End() < End();
    public NodeList Clone(NodeFactoryCoercible f)
    {
        var list = f.AsNodeFactory().NewNodeList(Nodes);
        list.Loc = Loc;
        return list;
    }
}

public sealed class ModifierList : NodeList
{
    public ModifierFlags ModifierFlags;
    public new ModifierList Clone(NodeFactoryCoercible f) => new()
    {
        Loc = Loc,
        Nodes = Nodes,
        ModifierFlags = ModifierFlags
    };
}

public partial class Node
{
    public Kind Kind;
    public NodeFlags Flags;
    public TextRange Loc;
    public Node Parent;
    // ast.go:180 stores the Go interface needed for embedded-payload dispatch.
    // The CLR object's runtime type already identifies that same payload.
    // A bare Node has no payload, preserving its previous null dispatch view.
    internal NodeData data => this as NodeData;
    public Node AsNode() => this;
    public Node AsMutable() => this;
    public int Pos() => Loc.Pos();
    public int End() => Loc.End();
    public short KindValue() => (short)Kind;
    public string KindString() => Kind.String();
    public SubtreeFacts SubtreeFacts() => (SubtreeFacts)SubtreeFactsValue();
    internal virtual uint SubtreeFactsValue() => data.SubtreeFactsValue();
    public virtual Node Name() => data.Name();
    public virtual ModifierList Modifiers() => data.Modifiers();
    public virtual void SetModifiers(ModifierList modifiers) => data.SetModifiers(modifiers);
    public FunctionLikeBase FunctionLikeData() => data as FunctionLikeBase;
    public ClassLikeBase ClassLikeData() => data as ClassLikeBase;
    public BodyBase BodyData() => data as BodyBase;
    public LiteralLikeNodeBase LiteralLikeData() => data as LiteralLikeNodeBase;
    public TemplateLiteralLikeNodeBase TemplateLiteralLikeData() => data as TemplateLiteralLikeNodeBase;
    public NodeList ParameterList() => FunctionLikeData().Parameters;
    public Slice<Node> Parameters() => ParameterList().Nodes;
    public Node Body() => BodyData()?.Body;
    public ModifierFlags ModifierFlags() => Modifiers()?.ModifierFlags ?? TypeScript.Scanner.ModifierFlags.None;
    public Slice<Node> ModifierNodes() => Modifiers()?.Nodes ?? default;
    public Slice<Node> Decorators()
    {
        if (Modifiers()is null)
            return default;
        return Core.Filter(Modifiers().Nodes, Ast.IsDecorator);
    }

    public Slice<Node> Arguments() => ArgumentList()?.Nodes ?? default;
    public Slice<Node> TypeArguments() => TypeArgumentList()?.Nodes ?? default;
    public Slice<Node> TypeParameters() => TypeParameterList()?.Nodes ?? default;
    public Slice<Node> Members() => MemberList()?.Nodes ?? default;
    public Slice<Node> Statements() => StatementList()?.Nodes ?? default;
    public Slice<Node> Comments() => CommentList()?.Nodes ?? default;
    public Slice<Node> Properties() => PropertyList()?.Nodes ?? default;
    public Slice<Node> Elements() => ElementList()?.Nodes ?? default;
    public virtual bool ForEachChild(Visitor visitor) => data.ForEachChild(visitor);
    public virtual string DataType() => data.DataType();
    public virtual IEnumerable<SyntaxField> SyntaxFields() => data.SyntaxFields();
    public virtual IEnumerable<Node> IterChildren() => data.IterChildren();
    public bool Contains(Node descendant)
    {
        for (var n = descendant; n is not null; n = n.Parent)
        {
            if (n == this)
                return true;
            if (n.Parent is null && n.Kind != Kind.SourceFile)
                throw new InvalidOperationException("descendant is not parented");
        }

        return false;
    }

    // Text is the upstream byte-string. Decode only at public presentation boundaries.
    public string Text() => Kind switch
    {
        Kind.Identifier => AsIdentifier().Text,
        Kind.PrivateIdentifier => AsPrivateIdentifier().Text,
        Kind.StringLiteral => AsStringLiteral().Text,
        Kind.NumericLiteral => AsNumericLiteral().Text,
        Kind.BigIntLiteral => AsBigIntLiteral().Text,
        Kind.NoSubstitutionTemplateLiteral => AsNoSubstitutionTemplateLiteral().Text,
        Kind.TemplateHead => AsTemplateHead().Text,
        Kind.TemplateMiddle => AsTemplateMiddle().Text,
        Kind.TemplateTail => AsTemplateTail().Text,
        Kind.RegularExpressionLiteral => AsRegularExpressionLiteral().Text,
        Kind.MetaProperty => AsMetaProperty().Name().Text(),
        Kind.JsxNamespacedName => AsJsxNamespacedName().Namespace.Text() + ":" + Name().Text(),
        Kind.JSDocText => AsJSDocText().TextFragments?.Join() ?? "",
        Kind.JSDocLink => AsJSDocLink().TextFragments?.Join() ?? "",
        Kind.JSDocLinkCode => AsJSDocLinkCode().TextFragments?.Join() ?? "",
        Kind.JSDocLinkPlain => AsJSDocLinkPlain().TextFragments?.Join() ?? "",
        _ => throw new InvalidOperationException("Unhandled Node.Text kind " + Kind),
    };
    // ast.go:273. Internal Go text reads retain a source/owned-text header.
    internal ByteStringValue TextValue() => Kind switch
    {
        Kind.BigIntLiteral => AsBigIntLiteral().TextValue,
        Kind.Identifier => AsIdentifier().TextValue,
        Kind.JsxText => AsJsxText().TextValue,
        Kind.NoSubstitutionTemplateLiteral => AsNoSubstitutionTemplateLiteral().TextValue,
        Kind.NumericLiteral => AsNumericLiteral().TextValue,
        Kind.PrivateIdentifier => AsPrivateIdentifier().TextValue,
        Kind.RegularExpressionLiteral => AsRegularExpressionLiteral().TextValue,
        Kind.StringLiteral => AsStringLiteral().TextValue,
        Kind.TemplateHead => AsTemplateHead().TextValue,
        Kind.TemplateMiddle => AsTemplateMiddle().TextValue,
        Kind.TemplateTail => AsTemplateTail().TextValue,
        Kind.MetaProperty => AsMetaProperty().Name().TextValue(),
        Kind.JsxNamespacedName => Ast.TryGetTextValueOfPropertyName(this).Item1,
        Kind.JSDocText => AsJSDocText().TextFragments?.JoinValue() ?? default,
        Kind.JSDocLink => AsJSDocLink().TextFragments?.JoinValue() ?? default,
        Kind.JSDocLinkCode => AsJSDocLinkCode().TextFragments?.JoinValue() ?? default,
        Kind.JSDocLinkPlain => AsJSDocLinkPlain().TextFragments?.JoinValue() ?? default,
        _ => ByteStringValue.FromString(Text()),
    };
    internal bool TextEquals(ReadOnlySpan<byte> text) => TextValue().EqualsBytes(text);
    public byte[] TextBytes()
    {
        var text = TextValue();
        return text.IsBytes ? text.ByteSpan.ToArray() : GoString.Bytes(text.Materialize());
    }
    public string DecodedText() => ScannerString.ToJsString(Text());
    public bool IsJSDoc() => Kind == Kind.JSDoc;
    public Slice<Node> JSDoc(SourceFile file) => (Flags & NodeFlags.HasJSDoc) == 0 ? default : (file ?? Ast.GetSourceFileOfNode(this))?.JSDoc(this) ?? default;
    public Slice<Node> EagerJSDoc(SourceFile file) => (Flags & NodeFlags.HasJSDoc) == 0 ? default : (file ?? Ast.GetSourceFileOfNode(this))?.EagerJSDoc(this) ?? default;
    public virtual Node Clone(NodeFactoryCoercible factory) => data.Clone(factory);
    public virtual Node VisitEachChild(NodeVisitor visitor) => data.VisitEachChild(visitor);
}

// ast.go:1210/1252 embeds the common Node header in each concrete payload.
// CLR inheritance keeps that header and payload in one object. Node's virtual
// surface dispatches directly to the payload override; bare headers retain the
// original null-view forwarding fallback.
public abstract partial class NodeData : Node, INodeData
{
    internal virtual uint ComputeSubtreeFacts() => 0;

    internal virtual uint PropagateSubtreeFacts() => SubtreeFactsValue();
    internal virtual bool CacheSubtreeFacts => false;
    internal virtual uint CachedSubtreeFacts => 0;

    // ast.go:1234. Ordinary payloads compute without a retained cache field.
    internal override uint SubtreeFactsValue() => ComputeSubtreeFacts();

    public override Node Name() => null;
    public override ModifierList Modifiers() => null;
    public override void SetModifiers(ModifierList value)
    {
    }

    public override bool ForEachChild(Visitor visitor) => false;
    public override IEnumerable<Node> IterChildren()
    {
        yield break;
    }

    public override Node Clone(NodeFactoryCoercible factory) => null;
    public override Node VisitEachChild(NodeVisitor visitor) => AsNode();
    public abstract override string DataType();
    public abstract override IEnumerable<SyntaxField> SyntaxFields();
    public override IEnumerable<SyntaxField> BoundFields()
    {
        yield break;
    }
}

// ast_generated.go:148 and ast.go:1602. Only CompositeBase embeds facts.
// CLR inheritance places its cache in the same concrete node object; virtual
// worker dispatch retains the original uncached versus cached source algorithms.
public abstract class CompositeNodeData : NodeData
{
    private int cachedSubtreeFacts;
    internal sealed override bool CacheSubtreeFacts => true;
    internal sealed override uint CachedSubtreeFacts =>
        (uint)System.Threading.Volatile.Read(ref cachedSubtreeFacts);

    internal override uint ComputeSubtreeFacts() =>
        throw new InvalidOperationException("not implemented");

    internal sealed override uint SubtreeFactsValue()
    {
        uint facts = CachedSubtreeFacts;
        if ((facts & SubtreeFactValues.FactsComputed) == 0)
        {
            facts |= ComputeSubtreeFacts() | SubtreeFactValues.FactsComputed;
            System.Threading.Volatile.Write(ref cachedSubtreeFacts, (int)facts);
        }

        return facts & ~SubtreeFactValues.FactsComputed;
    }
}

public sealed partial class NodeFactory : NodeFactoryCoercible
{
    internal NodeFactoryHooks hooks;
    private int nodeCount, textCount;
    public NodeFactory(NodeFactoryHooks hooks = null)
    {
        this.hooks = hooks ?? new();
    }

    public NodeFactory AsNodeFactory() => this;
    public void ReleaseArenas()
    {
    } // Managed allocation has no arenas to release.

    public int NodeCount() => nodeCount;
    public int TextCount() => textCount;
    // parser.go:129 zeroes its embedded NodeFactory on pool return. This managed
    // factory object stays private to that parser; clear its value state in place.
    internal void Reset()
    {
        nodeCount = textCount = 0;
        hooks.OnCreate = null;
        hooks.OnUpdate = null;
        hooks.OnClone = null;
    }
    internal Node newNode(Kind kind, NodeData data)
    {
        nodeCount++;
        var n = data.AsNode();
        n.Loc = new(-1, -1);
        n.Kind = kind;
        hooks.OnCreate?.Invoke(n);
        return n;
    }

    public NodeList NewNodeList(Slice<Node> nodes) => new()
    {
        Loc = new(-1, -1),
        Nodes = nodes
    };
    public ModifierList NewModifierList(Slice<Node> nodes) => new()
    {
        Loc = new(-1, -1),
        Nodes = nodes,
        ModifierFlags = Ast.ModifiersToFlags(nodes)
    };
    // Explicit managed input bridges; public headers retain Go storage semantics.
    public NodeList NewNodeList(List<Node> nodes) => NewNodeList(nodes is null ? default(Slice<Node>) : Slice<Node>.FromArray(nodes.ToArray()));
    public ModifierList NewModifierList(List<Node> nodes) => NewModifierList(nodes is null ? default(Slice<Node>) : Slice<Node>.FromArray(nodes.ToArray()));
    public Node NewModifier(Kind kind) => NewToken(kind);
    public Node NewSourceFile(SourceFileParseOptions opts, string text, NodeList statements, Node endOfFileToken) => newNode(Kind.SourceFile, new SourceFile { parseOptions = opts, text = text, Statements = statements, EndOfFileToken = endOfFileToken });
    // ast.go source construction shares immutable source bytes before hooks run.
    internal Node NewSourceFileWithTextView(SourceFileParseOptions opts, ByteStringView text, NodeList statements, Node endOfFileToken)
        => newNode(Kind.SourceFile, new SourceFile { parseOptions = opts, sourceText = text, Statements = statements, EndOfFileToken = endOfFileToken });
}

public static partial class Ast
{
    // Capture the slice reference/count when its traversal begins, as a Go range does.
    internal static IEnumerable<Node> IterateSyntaxNodes(List<Node> nodes)
    {
        if (nodes is null)
            yield break;
        var count = nodes.Count;
        for (var i = 0; i < count; i++)
            yield return nodes[i];
    }

    internal static bool SameSyntaxSlice<T>(List<T> left, List<T> right) => (left?.Count ?? 0) == (right?.Count ?? 0) && ((left?.Count ?? 0) == 0 || ReferenceEquals(left, right));
    internal static bool SameSyntaxSlice<T>(Slice<T> left, Slice<T> right) => left.SameStorageRange(right);
    // The header itself is the Go range source; direct foreach needs no yield wrapper.
    internal static Slice<Node> IterateSyntaxNodes(Slice<Node> nodes) => nodes;
    internal static bool ForEachChildJSDocParameterOrPropertyTag(JSDocParameterOrPropertyTag node, Visitor v) => Visit(v, node.TagName) || (node.IsNameFirst && (Visit(v, node.name) || Visit(v, node.TypeExpression))) || (!node.IsNameFirst && (Visit(v, node.TypeExpression) || Visit(v, node.name))) || VisitNodeList(v, node.Comment);
    internal static Node VisitEachChildJSDocParameterOrPropertyTag(JSDocParameterOrPropertyTag node, NodeVisitor v) => v.Factory.UpdateJSDocParameterOrPropertyTag(node, v.visitNode(node.TagName), v.visitNode(node.name), node.IsBracketed, v.visitNode(node.TypeExpression), node.IsNameFirst, v.visitNodes(node.Comment));
    internal static Slice<Node> SameMapNodes(Slice<Node> nodes, Func<Node, Node> map)
        => Core.SameMap(nodes, map);
    internal static List<Node> SameMapNodes(List<Node> nodes, Func<Node, Node> map)
    {
        if (nodes is null)
            return null;
        var count = nodes.Count;
        List<Node> result = null;
        for (var i = 0; i < count; i++)
        {
            var node = nodes[i];
            var mapped = map(node);
            if (result is null && mapped != node)
                result = nodes.GetRange(0, i);
            result?.Add(mapped);
        }

        // A Go parameter retains its slice-header length if a callback appends to the caller.
        return result ?? (nodes.Count == count ? nodes : nodes.GetRange(0, count));
    }

    internal static Func<SourceFile, Node, Slice<Node>> ParseJSDocForNode;
    public static void SetParseJSDocForNode(Func<SourceFile, Node, Slice<Node>> parser) => ParseJSDocForNode = parser;
    internal static Node UpdateNode(Node updated, Node original, NodeFactoryHooks hooks)
    {
        if (updated != original)
        {
            updated.Flags = original.Flags;
            updated.Loc = original.Loc;
            hooks.OnUpdate?.Invoke(updated, original);
        }

        return updated;
    }

    internal static Node CloneNode(Node updated, Node original, NodeFactoryHooks hooks)
    {
        UpdateNode(updated, original, hooks);
        if (updated != original)
            hooks.OnClone?.Invoke(updated, original);
        return updated;
    }

    public static NodeFactory NewNodeFactory(NodeFactoryHooks hooks) => new(hooks);
    internal static bool Visit(Visitor v, Node node) => node is not null && v(node);
    internal static bool VisitNodes(Visitor v, List<Node> nodes)
    {
        if (nodes is not null)
        {
            var count = nodes.Count;
            for (var i = 0; i < count; i++)
                if (v(nodes[i]))
                    return true;
        }

        return false;
    }

    internal static bool VisitNodes(Visitor v, Slice<Node> nodes)
    {
        foreach (var node in nodes) if (v(node)) return true;
        return false;
    }
    internal static bool VisitNodeList(Visitor v, NodeList nodes) => VisitNodes(v, nodes?.Nodes ?? default);
    internal static bool VisitModifiers(Visitor v, ModifierList nodes) => VisitNodes(v, nodes?.Nodes ?? default);
}

public sealed class CheckJsDirective
{
    public bool Enabled;
    public CommentRange Range;
}

public sealed class FileReference
{
    public TextRange TextRange;
    private ByteStringValue fileNameValue = ByteStringValue.FromString(null);
    public string FileName { get => fileNameValue.Project(); set => fileNameValue = ByteStringValue.FromString(value); }
    internal ByteStringValue FileNameValue { get => fileNameValue.Snapshot(); set => fileNameValue = value.Snapshot(); }
    public ResolutionMode ResolutionMode;
    public bool Preserve;
    public int Pos() => TextRange.Pos();
    public int End() => TextRange.End();
}

public sealed class PragmaArgument
{
    public TextRange TextRange;
    private ByteStringValue nameValue = ByteStringValue.FromString(null), valueValue = ByteStringValue.FromString(null);
    public string Name { get => nameValue.Project(); set => nameValue = ByteStringValue.FromString(value); }
    public string Value { get => valueValue.Project(); set => valueValue = ByteStringValue.FromString(value); }
    internal ByteStringValue NameValue { get => nameValue.Snapshot(); set => nameValue = value.Snapshot(); }
    internal ByteStringValue ValueValue { get => valueValue.Snapshot(); set => valueValue = value.Snapshot(); }
    public int Pos() => TextRange.Pos();
    public int End() => TextRange.End();
}

public sealed class Pragma
{
    public CommentRange CommentRange;
    public TextRange TextRange { get => new(CommentRange.Pos, CommentRange.End); set => CommentRange = CommentRange with
        {
            Pos = value.Pos(),
            End = value.End()
        }; }

    private ByteStringValue nameValue = ByteStringValue.FromString(null);
    public string Name { get => nameValue.Project(); set => nameValue = ByteStringValue.FromString(value); }
    internal ByteStringValue NameValue { get => nameValue.Snapshot(); set => nameValue = value.Snapshot(); }
    public Dictionary<ByteStringKey, PragmaArgument> Args;
}

// Authored internal byte-source bridge; legacy public text-only adapters can
// project at their boundary, while real SourceFiles and diagnostics borrow bytes.
internal interface ISourceTextFileLike
{
    ByteStringView TextView();
}
internal static class SourceTextViews
{
    internal static ByteStringView TextView(this SourceFileLike source)
        => source is ISourceTextFileLike bytes ? bytes.TextView() : ByteStringView.FromGoBytes(source.Text());
}

public interface SourceFileLike
{
    string Text();
    Slice<int> ECMALineMap();
}

// ast.go: SourceFileMetaData. Loader-owned file metadata, independent of checker admission.
public readonly record struct SourceFileMetaData
{
    private readonly string packageJsonType;
    public string PackageJsonType => packageJsonType ?? "";
    public RootedDirectoryPath PackageJsonDirectory { get; }
    public ResolutionMode ImpliedNodeFormat { get; }
    public SourceFileMetaData(string packageJsonType, RootedDirectoryPath packageJsonDirectory, ResolutionMode impliedNodeFormat) =>
        (this.packageJsonType, PackageJsonDirectory, ImpliedNodeFormat) = (packageJsonType, packageJsonDirectory, impliedNodeFormat);
}

// ast.go:2611–2674. Metadata is set once before mapped source publication.
public enum MappedDiagnosticDirectivePolicy : byte { Ignore, Expect }
public struct MappedDiagnosticDirective
{
    public TextRange OriginalRange, VirtualRange;
    public MappedDiagnosticDirectivePolicy Policy;
    public int UnusedCode;
    private string unusedMessageText, source;
    public string UnusedMessageText { readonly get => unusedMessageText ?? ""; set => unusedMessageText = value; }
    public string Source { readonly get => source ?? ""; set => source = value; }
}
public struct ContentMapperSourceFileInfo
{
    private string contentMapper, transformIdentity, originalText;
    public string ContentMapper { readonly get => contentMapper ?? ""; set => contentMapper = value; }
    public string TransformIdentity { readonly get => transformIdentity ?? ""; set => transformIdentity = value; }
    public string OriginalText { readonly get => originalText ?? ""; set => originalText = value; }
    public SourceFileParseOptions ParseOptions;
    public RootedFilePath VirtualFileName;
    public TypeScript.SpanMap.SpanMap SpanMap;
    public Slice<MappedDiagnosticDirective> DiagnosticDirectives;
    public Slice<SourceFile> SupplementalSourceFiles;
    public SourceFile CanonicalSourceFile;
}

public sealed partial class SourceFile : CompositeNodeData, SourceFileLike, ISourceTextFileLike
{
    internal SourceFileParseOptions parseOptions;
    // Authored filename-role boundary: Go ast.SourceFile stores the raw filename
    // directly. Retain that immutable string when a .NET adapter exposes Unicode.
    // Publish source/bytes together so concurrent readers cannot mix cache pairs.
    private sealed record FileNameByteText(string Source, string Bytes);
    private FileNameByteText fileNameByteText;
    internal ByteStringView sourceText = "";
    private string projectedText;
    // Only the legacy public raw-string surface requests this projection. Normal
    // compiler/parser/scanner consumers borrow canonical bytes through TextView.
    internal string text
    {
        get
        {
            var saved = Volatile.Read(ref projectedText);
            if (saved != null) return saved;
            return Interlocked.CompareExchange(ref projectedText, sourceText.Materialize(), null) ?? projectedText;
        }
        set { sourceText = ByteStringView.FromGoBytes(value); projectedText = value; }
    }
    internal ByteStringView TextView() => sourceText;
    ByteStringView ISourceTextFileLike.TextView() => sourceText;
    public new NodeList Statements;
    public Node EndOfFileToken;
    public LanguageVariant LanguageVariant;
    public ScriptKind ScriptKind;
    public bool IsDeclarationFile;
    public Tristate UsesUriStyleNodeCoreModules;
    public int IdentifierCount, NodeCount, TextCount;
    public Slice<Node> ModuleAugmentations;
    public Slice<Node> ReparsedClones;
    public Slice<ByteStringValue> AmbientModuleNames;
    public Slice<CommentDirective> CommentDirectives;
    public Slice<Pragma> Pragmas;
    public Slice<FileReference> ReferencedFiles, TypeReferenceDirectives, LibReferenceDirectives;
    public CheckJsDirective CheckJsDirective;
    public Node CommonJSModuleIndicator, ExternalModuleIndicator;
    private Slice<Diagnostic> diagnostics, jsDiagnostics, jsdocDiagnostics;
    internal Slice<Node> imports;
    private Dictionary<Node, Slice<Node>> jsdocCache;
    private bool hasLazyJSDoc;
    private readonly object positionMapLock = new();
    private Lazy<PositionMap> positionMap;
    public PositionMap GetPositionMap()
    {
        Lazy<PositionMap> once;
        lock (positionMapLock)
            once = positionMap ??= new(() => Ast.ComputePositionMapByteString(sourceText), LazyThreadSafetyMode.ExecutionAndPublication);
        return once.Value;
    }

    public UInt128 Hash;
    private ContentMapperSourceFileInfo? contentMapperInfo;
    public string OriginalText() => ContentMapper() != "" ? contentMapperInfo.Value.OriginalText : text;
    public RootedFilePath OriginalFileName() => RootedFilePath.FromValue((CanonicalSourceFile() ?? this).FileNameGoValue());
    public TypeScript.SpanMap.SpanMap SpanMap() => contentMapperInfo?.SpanMap;
    public bool IsContentMapped() => contentMapperInfo.HasValue;
    public string ContentMapper() => contentMapperInfo?.ContentMapper ?? "";
    public bool IsContentMapperFailureStub() => ContentMapper() != "" && SpanMap() is null;
    public string ContentMapperTransformIdentity() => contentMapperInfo?.TransformIdentity ?? "";
    public RootedFilePath VirtualFileName() => contentMapperInfo?.VirtualFileName ?? default;
    public SourceFileParseOptions ContentMapperParseOptions() => contentMapperInfo?.ParseOptions ?? default;
    public void SetContentMapperInfo(ContentMapperSourceFileInfo info)
    {
        if (contentMapperInfo.HasValue) throw new InvalidOperationException("content mapper source file info already set");
        contentMapperInfo = info;
    }
    public Slice<MappedDiagnosticDirective> DiagnosticDirectives() => contentMapperInfo is null ? default : contentMapperInfo.Value.DiagnosticDirectives;
    public Slice<SourceFile> SupplementalSourceFiles() => contentMapperInfo is null ? default : contentMapperInfo.Value.SupplementalSourceFiles;
    public SourceFile CanonicalSourceFile() => contentMapperInfo?.CanonicalSourceFile;
    public bool IsContentMapperSupplemental() => CanonicalSourceFile() is not null;

    public SourceFileParseOptions ParseOptions() => parseOptions;
    public string FileName() => parseOptions.FileName;
    // Authored compatibility seam: legacy parser filenames are Unicode, typed
    // graph filenames are Go bytes. Source text is always a Go byte string.
    internal ByteStringValue FileNameGoValue()
    {
        var name = parseOptions.FileNameValue;
        if (name.IsNull) return default;
        if (parseOptions.FileNameIsGoBytes || name.Length == 0) return name;
        // The legacy scalar Unicode ingress is the actual encoding boundary.
        return ByteStringValue.FromString(FileNameGoBytes());
    }
    internal ByteStringValue PathKeyValue() => parseOptions.PathKeyValue.IsNull ? default : parseOptions.PathKeyValue;
    internal ByteStringKey PathKeyKey() => ByteStringKey.FromValue(PathKeyValue());
    public string FileNameGoBytes()
    {
        // A default managed parse-options struct has a null filename; the
        // corresponding Go string zero value is empty in either filename role.
        var fileName = FileName() ?? "";
        if (parseOptions.FileNameIsGoBytes || fileName.Length == 0)
        {
            Volatile.Write(ref fileNameByteText, null);
            return fileName;
        }
        var saved = Volatile.Read(ref fileNameByteText);
        if (saved != null && ReferenceEquals(saved.Source, fileName))
            return saved.Bytes;
        var bytes = GoString.ToUtf8(fileName);
        Volatile.Write(ref fileNameByteText, new(fileName, bytes));
        return bytes;
    }
    // Only the disk Project adapter changes the public filename role. Preserve
    // its original bytes rather than encoding them again on every Program query
    // (program.go:1767–1780 uses the original filename string directly).
    internal void SetFileNameToUnicode()
    {
        var bytes = FileNameGoBytes();
        var unicode = RootedFilePath.FromGoString(bytes).ToUnicode();
        parseOptions = parseOptions with { FileName = unicode, FileNameIsGoBytes = false };
        Volatile.Write(ref fileNameByteText, new(unicode, bytes));
    }
    public string PathKey() => parseOptions.PathKey;
    public new string Text() => text;
    public new byte[] TextBytes() => sourceText.ToArray();
    public new string DecodedText() => sourceText.DecodeUnicode();
    public Slice<Node> Imports() => imports;
    public Slice<Diagnostic> Diagnostics() => diagnostics;
    public Slice<Diagnostic> JSDiagnostics() => jsDiagnostics;
    public Slice<Diagnostic> JSDocDiagnostics() => jsdocDiagnostics;
    public void SetDiagnostics(Slice<Diagnostic> value) => diagnostics = value;
    public void SetJSDiagnostics(Slice<Diagnostic> value) => jsDiagnostics = value;
    public void SetJSDocDiagnostics(Slice<Diagnostic> value) => jsdocDiagnostics = value;
    public void SetJSDocCache(Dictionary<Node, Slice<Node>> value) => jsdocCache = value;
    public void SetHasLazyJSDoc(bool value) => hasLazyJSDoc = value;
    internal Slice<Node> JSDoc(Node n)
    {
        if ((n.Flags & NodeFlags.HasJSDoc) == 0)
            return default;
        if (!hasLazyJSDoc)
            return jsdocCache is not null && jsdocCache.TryGetValue(n, out var eager) ? eager : default;
        lock (this)
        {
            jsdocCache ??= new();
            if (jsdocCache.TryGetValue(n, out var value))
                return value;
            var parser = Ast.ParseJSDocForNode ?? throw new InvalidOperationException("JSDoc parser not registered.");
            var result = parser(this, n);
            jsdocCache[n] = result;
            return result;
        }
    }

    internal Slice<Node> EagerJSDoc(Node n)
    {
        if ((n.Flags & NodeFlags.HasJSDoc) == 0)
            return default;
        if (!hasLazyJSDoc)
            return jsdocCache is not null && jsdocCache.TryGetValue(n, out var eager) ? eager : default;
        lock (this)
            return jsdocCache is not null && jsdocCache.TryGetValue(n, out var value) ? value : default;
    }

    public override bool ForEachChild(Visitor v) => Ast.VisitNodeList(v, Statements) || Ast.Visit(v, EndOfFileToken);

    internal override uint ComputeSubtreeFacts() => Ast.PropagateNodeListSubtreeFacts(Statements, Ast.PropagateSubtreeFacts);
    public override string DataType() => "SourceFile";
    public override IEnumerable<SyntaxField> SyntaxFields()
    {
        yield return new("Statements", "list", Statements);
        yield return new("EndOfFileToken", "node", EndOfFileToken);
    }
}

public sealed partial class SourceFile
{
    internal void copyFrom(SourceFile original)
    {
        this.LanguageVariant = original.LanguageVariant;
        this.ScriptKind = original.ScriptKind;
        this.IsDeclarationFile = original.IsDeclarationFile;
        this.UsesUriStyleNodeCoreModules = original.UsesUriStyleNodeCoreModules;
        this.imports = original.imports;
        this.ModuleAugmentations = original.ModuleAugmentations;
        this.AmbientModuleNames = original.AmbientModuleNames;
        this.CommentDirectives = original.CommentDirectives;
        this.Pragmas = original.Pragmas;
        this.ReferencedFiles = original.ReferencedFiles;
        this.TypeReferenceDirectives = original.TypeReferenceDirectives;
        this.LibReferenceDirectives = original.LibReferenceDirectives;
        this.CommonJSModuleIndicator = original.CommonJSModuleIndicator;
        this.ExternalModuleIndicator = original.ExternalModuleIndicator;
        this.Flags |= original.Flags;
    }

    public override Node Clone(NodeFactoryCoercible f)
    {
        var updated = f.AsNodeFactory().NewSourceFileWithTextView(parseOptions, sourceText, Statements, EndOfFileToken);
        var copy = updated.AsSourceFile();
        copy.copyFrom(this);
        return Ast.CloneNode(updated, AsNode(), f.AsNodeFactory().hooks);
    }

    public override Node VisitEachChild(NodeVisitor visitor) => visitor.Factory.UpdateSourceFile(this, visitor.visitTopLevelStatements(Statements), visitor.visitToken(EndOfFileToken));
}

public sealed partial class NodeFactory
{
    public Node UpdateSourceFile(SourceFile node, NodeList statements, Node endOfFileToken)
    {
        if (statements == node.Statements && endOfFileToken == node.EndOfFileToken)
            return node.AsNode();
        var updated = NewSourceFileWithTextView(node.parseOptions, node.sourceText, statements, endOfFileToken).AsSourceFile();
        updated.copyFrom(node);
        return Ast.UpdateNode(updated.AsNode(), node.AsNode(), hooks);
    }
}

// Binder-owned state from ast.go. Interfaces retain exact upstream embedding tests.
public partial class Node
{
    internal long id;
    public DeclarationBase DeclarationData() => data as DeclarationBase;
    public ExportableBase ExportableData() => data as ExportableBase;
    public LocalsContainerBase LocalsContainerData() => data as LocalsContainerBase;
    public FlowNodeBase FlowNodeData() => data as FlowNodeBase;
    public Symbol Symbol() => DeclarationData()?.Symbol;
    public Symbol LocalSymbol() => ExportableData()?.LocalSymbol;
    public SymbolTable Locals() => LocalsContainerData()?.Locals;
    public virtual IEnumerable<SyntaxField> BoundFields() => data.BoundFields();
}

public sealed class PatternAmbientModule
{
    public Pattern Pattern;
    public Symbol Symbol;
}

public sealed partial class SourceFile : DeclarationBase, LocalsContainerBase
{
    public new Symbol Symbol { get; set; }
    public new SymbolTable Locals { get; set; }
    public Node NextContainer { get; set; }

    public int SymbolCount;
    public Slice<PatternAmbientModule> PatternAmbientModules;
    public SymbolTable GlobalExports;
    private Slice<Diagnostic> bindDiagnostics;
    private readonly System.Threading.SemaphoreSlim bindMutex = new(1, 1);
    private bool bindInvoked, isBound;
    private readonly object ecmaLineMapMutex = new();
    private Slice<int> ecmaLineMap;
    public Slice<Diagnostic> BindDiagnostics() => bindDiagnostics;
    public void SetBindDiagnostics(Slice<Diagnostic> diagnostics) => bindDiagnostics = diagnostics;
    public bool IsBound() => System.Threading.Volatile.Read(ref isBound);
    public void BindOnce(Action bind)
    {
        // A non-reentrant gate preserves sync.Once's callback serialization.
        bindMutex.Wait();
        try
        {
            if (bindInvoked)
                return;
            // sync.Once marks the invocation done even when its callback panics.
            bindInvoked = true;
            bind();
            System.Threading.Volatile.Write(ref isBound, true);
        }
        finally
        {
            bindMutex.Release();
        }
    }

    public Slice<int> ECMALineMap()
    {
        lock (ecmaLineMapMutex)
        {
            if (ecmaLineMap.IsNil) ecmaLineMap = Core.ComputeECMALineStarts(TextView());
            return ecmaLineMap;
        }
    }

    public override IEnumerable<SyntaxField> BoundFields()
    {
        yield return new("Symbol", "symbol", Symbol);
        yield return new("Locals", "table", Locals);
        yield return new("NextContainer", "node", NextContainer);
    }
}

public static partial class Ast
{
    public static bool IsDeclarationNode(Node node) => node.DeclarationData()is not null;
    public static bool IsLocalsContainer(Node node) => node.LocalsContainerData()is not null;
}

// BEGIN GENERATED SUBTREE FACTS (tools/generate-subtreefacts.go; upstream ast.go)
public sealed partial class Token
{
    internal override uint ComputeSubtreeFacts()
    {
        switch (this.Kind)
        {
            case Kind.UsingKeyword:
            {
                return SubtreeFactValues.ContainsUsing;
            }

            case Kind.PublicKeyword:
            case Kind.PrivateKeyword:
            case Kind.ProtectedKeyword:
            case Kind.ReadonlyKeyword:
            case Kind.AbstractKeyword:
            case Kind.DeclareKeyword:
            case Kind.ConstKeyword:
            case Kind.AnyKeyword:
            case Kind.NumberKeyword:
            case Kind.BigIntKeyword:
            case Kind.NeverKeyword:
            case Kind.ObjectKeyword:
            case Kind.InKeyword:
            case Kind.OutKeyword:
            case Kind.OverrideKeyword:
            case Kind.StringKeyword:
            case Kind.BooleanKeyword:
            case Kind.SymbolKeyword:
            case Kind.VoidKeyword:
            case Kind.UnknownKeyword:
            case Kind.UndefinedKeyword:
            case Kind.ExportKeyword:
            {
                return SubtreeFactValues.ContainsTypeScript;
            }

            case Kind.AccessorKeyword:
            {
                return SubtreeFactValues.ContainsClassFields;
            }

            case Kind.AsyncKeyword:
            {
                return SubtreeFactValues.ContainsAnyAwait;
            }

            case Kind.SuperKeyword:
            {
                return SubtreeFactValues.ContainsLexicalSuper;
            }

            case Kind.ThisKeyword:
            {
                return SubtreeFactValues.ContainsLexicalThis;
            }

            case Kind.AsteriskAsteriskToken:
            case Kind.AsteriskAsteriskEqualsToken:
            {
                return SubtreeFactValues.ContainsExponentiationOperator;
            }

            case Kind.QuestionQuestionToken:
            {
                return SubtreeFactValues.ContainsNullishCoalescing;
            }

            case Kind.QuestionDotToken:
            {
                return SubtreeFactValues.ContainsOptionalChaining;
            }

            case Kind.QuestionQuestionEqualsToken:
            case Kind.BarBarEqualsToken:
            case Kind.AmpersandAmpersandEqualsToken:
            {
                return SubtreeFactValues.ContainsLogicalAssignments;
            }
        }

        return SubtreeFactValues.FactsNone;
    }
}

public sealed partial class Identifier
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsIdentifier;
    }
}

public sealed partial class PrivateIdentifier
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsClassFields;
    }
}

public sealed partial class Decorator
{
    internal override uint ComputeSubtreeFacts()
    {
        return ((Ast.PropagateSubtreeFacts(this.Expression) | SubtreeFactValues.ContainsTypeScript) | SubtreeFactValues.ContainsDecorators);
    }
}

public sealed partial class ForInOrOfStatement
{
    internal override uint ComputeSubtreeFacts()
    {
        return (((Ast.PropagateSubtreeFacts(this.Initializer) | Ast.PropagateSubtreeFacts(this.Expression)) | Ast.PropagateSubtreeFacts(this.Statement)) | Core.IfElse((this.AwaitModifier != null), SubtreeFactValues.ContainsForAwaitOrAsyncGenerator, SubtreeFactValues.FactsNone));
    }
}

public sealed partial class ReturnStatement
{
    internal override uint ComputeSubtreeFacts()
    {
        return (Ast.PropagateSubtreeFacts(this.Expression) | SubtreeFactValues.ContainsForAwaitOrAsyncGenerator);
    }
}

public sealed partial class CatchClause
{
    internal override uint ComputeSubtreeFacts()
    {
        var res = (Ast.PropagateSubtreeFacts(this.VariableDeclaration) | Ast.PropagateSubtreeFacts(this.Block));
        if ((this.VariableDeclaration == null))
        {
            res |= SubtreeFactValues.ContainsMissingCatchClauseVariable;
        }

        return res;
    }
}

public sealed partial class CatchClause
{
    internal override uint PropagateSubtreeFacts()
    {
        return (this.SubtreeFactsValue() & ~SubtreeFactValues.ExclusionsCatchClause);
    }
}

public sealed partial class VariableStatement
{
    internal override uint ComputeSubtreeFacts()
    {
        if (((this.modifiers != null) && ((this.modifiers.ModifierFlags & TypeScript.Scanner.ModifierFlags.Ambient) != 0)))
        {
            return SubtreeFactValues.ContainsTypeScript;
        }
        else
        {
            return (Ast.PropagateModifierListSubtreeFacts(this.modifiers) | Ast.PropagateSubtreeFacts(this.DeclarationList));
        }
    }
}

public sealed partial class VariableDeclaration
{
    internal override uint ComputeSubtreeFacts()
    {
        return (((Ast.PropagateSubtreeFacts(this.name) | Ast.PropagateEraseableSyntaxSubtreeFacts(this.ExclamationToken)) | Ast.PropagateEraseableSyntaxSubtreeFacts(this.Type)) | Ast.PropagateSubtreeFacts(this.Initializer));
    }
}

public sealed partial class VariableDeclarationList
{
    internal override uint ComputeSubtreeFacts()
    {
        return (Ast.PropagateNodeListSubtreeFacts(this.Declarations, Ast.PropagateSubtreeFacts) | Core.IfElse(((this.Flags & TypeScript.Scanner.NodeFlags.Using) != 0), SubtreeFactValues.ContainsUsing, SubtreeFactValues.FactsNone));
    }
}

public sealed partial class VariableDeclarationList
{
    internal override uint PropagateSubtreeFacts()
    {
        return (this.SubtreeFactsValue() & ~SubtreeFactValues.ExclusionsVariableDeclarationList);
    }
}

public sealed partial class BindingPattern
{
    internal override uint ComputeSubtreeFacts()
    {
        switch (this.Kind)
        {
            case Kind.ObjectBindingPattern:
            {
                return Ast.PropagateNodeListSubtreeFacts(this.Elements, Ast.PropagateObjectBindingElementSubtreeFacts);
            }

            case Kind.ArrayBindingPattern:
            {
                return Ast.PropagateNodeListSubtreeFacts(this.Elements, Ast.PropagateBindingElementSubtreeFacts);
            }

            default:
            {
                return SubtreeFactValues.FactsNone;
            }
        }
    }
}

public sealed partial class BindingPattern
{
    internal override uint PropagateSubtreeFacts()
    {
        return (this.SubtreeFactsValue() & ~SubtreeFactValues.ExclusionsBindingPattern);
    }
}

public sealed partial class ParameterDeclaration
{
    internal override uint ComputeSubtreeFacts()
    {
        if (((this.name != null) && Ast.IsThisIdentifier(this.name)))
        {
            return SubtreeFactValues.ContainsTypeScript;
        }
        else
        {
            return ((((Ast.PropagateModifierListSubtreeFacts(this.modifiers) | Ast.PropagateSubtreeFacts(this.name)) | Ast.PropagateEraseableSyntaxSubtreeFacts(this.QuestionToken)) | Ast.PropagateEraseableSyntaxSubtreeFacts(this.Type)) | Ast.PropagateSubtreeFacts(this.Initializer));
        }
    }
}

public sealed partial class ParameterDeclaration
{
    internal override uint PropagateSubtreeFacts()
    {
        return (this.SubtreeFactsValue() & ~SubtreeFactValues.ExclusionsParameter);
    }
}

public sealed partial class BindingElement
{
    internal override uint ComputeSubtreeFacts()
    {
        return (((Ast.PropagateSubtreeFacts(this.PropertyName) | Ast.PropagateSubtreeFacts(this.name)) | Ast.PropagateSubtreeFacts(this.Initializer)) | Core.IfElse((this.DotDotDotToken != null), SubtreeFactValues.ContainsRestOrSpread, SubtreeFactValues.FactsNone));
    }
}

public sealed partial class FunctionDeclaration
{
    internal override uint ComputeSubtreeFacts()
    {
        if (((this.Body == null) || ((this.ModifierFlags() & TypeScript.Scanner.ModifierFlags.Ambient) != 0)))
        {
            return SubtreeFactValues.ContainsTypeScript;
        }
        else
        {
            var isAsync = ((this.ModifierFlags() & TypeScript.Scanner.ModifierFlags.Async) != 0);
            var isGenerator = (this.AsteriskToken != null);
            return (((((((((Ast.PropagateModifierListSubtreeFacts(this.modifiers) | Ast.PropagateSubtreeFacts(this.AsteriskToken)) | Ast.PropagateSubtreeFacts(this.name)) | Ast.PropagateEraseableSyntaxListSubtreeFacts(this.TypeParameters)) | Ast.PropagateNodeListSubtreeFacts(this.Parameters, Ast.PropagateSubtreeFacts)) | Ast.PropagateEraseableSyntaxSubtreeFacts(this.Type)) | Ast.PropagateEraseableSyntaxSubtreeFacts(this.FullSignature)) | Ast.PropagateSubtreeFacts(this.Body)) | Core.IfElse((isAsync && isGenerator), SubtreeFactValues.ContainsForAwaitOrAsyncGenerator, SubtreeFactValues.FactsNone)) | Core.IfElse((isAsync && !isGenerator), SubtreeFactValues.ContainsAnyAwait, SubtreeFactValues.FactsNone));
        }
    }
}

public sealed partial class FunctionDeclaration
{
    internal override uint PropagateSubtreeFacts()
    {
        return (this.SubtreeFactsValue() & ~SubtreeFactValues.ExclusionsFunction);
    }
}

public sealed partial class ClassDeclaration
{
    internal override uint ComputeSubtreeFacts()
    {
        if (((this.modifiers != null) && ((this.modifiers.ModifierFlags & TypeScript.Scanner.ModifierFlags.Ambient) != 0)))
        {
            return SubtreeFactValues.ContainsTypeScript;
        }
        else
        {
            return ((((Ast.PropagateModifierListSubtreeFacts(this.modifiers) | Ast.PropagateSubtreeFacts(this.name)) | Ast.PropagateEraseableSyntaxListSubtreeFacts(this.TypeParameters)) | Ast.PropagateNodeListSubtreeFacts(this.HeritageClauses, Ast.PropagateSubtreeFacts)) | Ast.PropagateNodeListSubtreeFacts(this.Members, Ast.PropagateSubtreeFacts));
        }
    }
}

public sealed partial class ClassDeclaration
{
    internal override uint PropagateSubtreeFacts()
    {
        return (this.SubtreeFactsValue() & ~SubtreeFactValues.ExclusionsClass);
    }
}

public sealed partial class ClassExpression
{
    internal override uint ComputeSubtreeFacts()
    {
        if (((this.modifiers != null) && ((this.modifiers.ModifierFlags & TypeScript.Scanner.ModifierFlags.Ambient) != 0)))
        {
            return SubtreeFactValues.ContainsTypeScript;
        }
        else
        {
            return ((((Ast.PropagateModifierListSubtreeFacts(this.modifiers) | Ast.PropagateSubtreeFacts(this.name)) | Ast.PropagateEraseableSyntaxListSubtreeFacts(this.TypeParameters)) | Ast.PropagateNodeListSubtreeFacts(this.HeritageClauses, Ast.PropagateSubtreeFacts)) | Ast.PropagateNodeListSubtreeFacts(this.Members, Ast.PropagateSubtreeFacts));
        }
    }
}

public sealed partial class ClassExpression
{
    internal override uint PropagateSubtreeFacts()
    {
        return (this.SubtreeFactsValue() & ~SubtreeFactValues.ExclusionsClass);
    }
}

public sealed partial class HeritageClause
{
    internal override uint ComputeSubtreeFacts()
    {
        switch (this.Token)
        {
            case Kind.ExtendsKeyword:
            {
                return Ast.PropagateNodeListSubtreeFacts(this.Types, Ast.PropagateSubtreeFacts);
            }

            case Kind.ImplementsKeyword:
            {
                return SubtreeFactValues.ContainsTypeScript;
            }

            default:
            {
                return SubtreeFactValues.FactsNone;
            }
        }
    }
}

public sealed partial class InterfaceDeclaration
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class InterfaceDeclaration
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class TypeAliasDeclaration
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class TypeAliasDeclaration
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class EnumMember
{
    internal override uint ComputeSubtreeFacts()
    {
        return ((Ast.PropagateSubtreeFacts(this.name) | Ast.PropagateSubtreeFacts(this.Initializer)) | SubtreeFactValues.ContainsTypeScript);
    }
}

public sealed partial class EnumDeclaration
{
    internal override uint ComputeSubtreeFacts()
    {
        if (((this.modifiers != null) && ((this.modifiers.ModifierFlags & TypeScript.Scanner.ModifierFlags.Ambient) != 0)))
        {
            return SubtreeFactValues.ContainsTypeScript;
        }
        else
        {
            return (((Ast.PropagateModifierListSubtreeFacts(this.modifiers) | Ast.PropagateSubtreeFacts(this.name)) | Ast.PropagateNodeListSubtreeFacts(this.Members, Ast.PropagateSubtreeFacts)) | SubtreeFactValues.ContainsTypeScript);
        }
    }
}

public sealed partial class ExportAssignment
{
    internal override uint ComputeSubtreeFacts()
    {
        return (((Ast.PropagateModifierListSubtreeFacts(this.modifiers) | Ast.PropagateSubtreeFacts(this.Type)) | Ast.PropagateSubtreeFacts(this.Expression)) | Core.IfElse(this.IsExportEquals, SubtreeFactValues.ContainsTypeScript, SubtreeFactValues.FactsNone));
    }
}

public sealed partial class NamespaceExportDeclaration
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class NamespaceExportDeclaration
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class ExportSpecifier
{
    internal override uint ComputeSubtreeFacts()
    {
        if (this.IsTypeOnly)
        {
            return SubtreeFactValues.ContainsTypeScript;
        }
        else
        {
            return (Ast.PropagateSubtreeFacts(this.PropertyName) | Ast.PropagateSubtreeFacts(this.name));
        }
    }
}

public sealed partial class CallSignatureDeclaration
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class CallSignatureDeclaration
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class ConstructSignatureDeclaration
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class ConstructSignatureDeclaration
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class ConstructorDeclaration
{
    internal override uint ComputeSubtreeFacts()
    {
        if ((this.Body == null))
        {
            return SubtreeFactValues.ContainsTypeScript;
        }
        else
        {
            return (((((Ast.PropagateModifierListSubtreeFacts(this.modifiers) | Ast.PropagateEraseableSyntaxListSubtreeFacts(this.TypeParameters)) | Ast.PropagateNodeListSubtreeFacts(this.Parameters, Ast.PropagateSubtreeFacts)) | Ast.PropagateEraseableSyntaxSubtreeFacts(this.Type)) | Ast.PropagateEraseableSyntaxSubtreeFacts(this.FullSignature)) | Ast.PropagateSubtreeFacts(this.Body));
        }
    }
}

public sealed partial class ConstructorDeclaration
{
    internal override uint PropagateSubtreeFacts()
    {
        return (this.SubtreeFactsValue() & ~SubtreeFactValues.ExclusionsConstructor);
    }
}

public sealed partial class GetAccessorDeclaration
{
    internal override uint ComputeSubtreeFacts()
    {
        if ((this.Body == null))
        {
            return SubtreeFactValues.ContainsTypeScript;
        }
        else
        {
            return ((((((Ast.PropagateModifierListSubtreeFacts(this.modifiers) | Ast.PropagateSubtreeFacts(this.name)) | Ast.PropagateEraseableSyntaxListSubtreeFacts(this.TypeParameters)) | Ast.PropagateNodeListSubtreeFacts(this.Parameters, Ast.PropagateSubtreeFacts)) | Ast.PropagateEraseableSyntaxSubtreeFacts(this.Type)) | Ast.PropagateEraseableSyntaxSubtreeFacts(this.FullSignature)) | Ast.PropagateSubtreeFacts(this.Body));
        }
    }
}

public sealed partial class GetAccessorDeclaration
{
    internal override uint PropagateSubtreeFacts()
    {
        return ((this.SubtreeFactsValue() & ~SubtreeFactValues.ExclusionsAccessor) | Ast.PropagateSubtreeFacts(this.name));
    }
}

public sealed partial class SetAccessorDeclaration
{
    internal override uint ComputeSubtreeFacts()
    {
        if ((this.Body == null))
        {
            return SubtreeFactValues.ContainsTypeScript;
        }
        else
        {
            return ((((((Ast.PropagateModifierListSubtreeFacts(this.modifiers) | Ast.PropagateSubtreeFacts(this.name)) | Ast.PropagateEraseableSyntaxListSubtreeFacts(this.TypeParameters)) | Ast.PropagateNodeListSubtreeFacts(this.Parameters, Ast.PropagateSubtreeFacts)) | Ast.PropagateEraseableSyntaxSubtreeFacts(this.Type)) | Ast.PropagateEraseableSyntaxSubtreeFacts(this.FullSignature)) | Ast.PropagateSubtreeFacts(this.Body));
        }
    }
}

public sealed partial class SetAccessorDeclaration
{
    internal override uint PropagateSubtreeFacts()
    {
        return ((this.SubtreeFactsValue() & ~SubtreeFactValues.ExclusionsAccessor) | Ast.PropagateSubtreeFacts(this.name));
    }
}

public sealed partial class IndexSignatureDeclaration
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class IndexSignatureDeclaration
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class MethodSignatureDeclaration
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class MethodSignatureDeclaration
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class MethodDeclaration
{
    internal override uint ComputeSubtreeFacts()
    {
        if ((this.Body == null))
        {
            return SubtreeFactValues.ContainsTypeScript;
        }
        else
        {
            var isAsync = ((this.modifiers != null) && ((this.modifiers.ModifierFlags & TypeScript.Scanner.ModifierFlags.Async) != 0));
            var isGenerator = (this.AsteriskToken != null);
            return ((((((((((Ast.PropagateModifierListSubtreeFacts(this.modifiers) | Ast.PropagateSubtreeFacts(this.AsteriskToken)) | Ast.PropagateSubtreeFacts(this.name)) | Ast.PropagateEraseableSyntaxSubtreeFacts(this.PostfixToken)) | Ast.PropagateEraseableSyntaxListSubtreeFacts(this.TypeParameters)) | Ast.PropagateNodeListSubtreeFacts(this.Parameters, Ast.PropagateSubtreeFacts)) | Ast.PropagateSubtreeFacts(this.Body)) | Ast.PropagateEraseableSyntaxSubtreeFacts(this.Type)) | Ast.PropagateEraseableSyntaxSubtreeFacts(this.FullSignature)) | Core.IfElse((isAsync && isGenerator), SubtreeFactValues.ContainsForAwaitOrAsyncGenerator, SubtreeFactValues.FactsNone)) | Core.IfElse((isAsync && !isGenerator), SubtreeFactValues.ContainsAnyAwait, SubtreeFactValues.FactsNone));
        }
    }
}

public sealed partial class MethodDeclaration
{
    internal override uint PropagateSubtreeFacts()
    {
        return ((this.SubtreeFactsValue() & ~SubtreeFactValues.ExclusionsMethod) | Ast.PropagateSubtreeFacts(this.name));
    }
}

public sealed partial class PropertySignatureDeclaration
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class PropertySignatureDeclaration
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class PropertyDeclaration
{
    internal override uint ComputeSubtreeFacts()
    {
        return (((((Ast.PropagateModifierListSubtreeFacts(this.modifiers) | Ast.PropagateSubtreeFacts(this.name)) | Ast.PropagateEraseableSyntaxSubtreeFacts(this.PostfixToken)) | Ast.PropagateEraseableSyntaxSubtreeFacts(this.Type)) | Ast.PropagateSubtreeFacts(this.Initializer)) | SubtreeFactValues.ContainsClassFields);
    }
}

public sealed partial class PropertyDeclaration
{
    internal override uint PropagateSubtreeFacts()
    {
        return ((this.SubtreeFactsValue() & ~SubtreeFactValues.ExclusionsProperty) | Ast.PropagateSubtreeFacts(this.name));
    }
}

public sealed partial class ClassStaticBlockDeclaration
{
    internal override uint ComputeSubtreeFacts()
    {
        return ((Ast.PropagateModifierListSubtreeFacts(this.modifiers) | Ast.PropagateSubtreeFacts(this.Body)) | SubtreeFactValues.ContainsClassFields);
    }
}

public sealed partial class KeywordExpression
{
    internal override uint ComputeSubtreeFacts()
    {
        switch (this.Kind)
        {
            case Kind.ThisKeyword:
            {
                return SubtreeFactValues.ContainsLexicalThis;
            }

            case Kind.SuperKeyword:
            {
                return SubtreeFactValues.ContainsLexicalSuper;
            }
        }

        return SubtreeFactValues.FactsNone;
    }
}

public sealed partial class BigIntLiteral
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.FactsNone;
    }
}

public sealed partial class NoSubstitutionTemplateLiteral
{
    internal override uint ComputeSubtreeFacts()
    {
        if (((this.TemplateFlags & TypeScript.Scanner.TokenFlags.ContainsInvalidEscape) != 0))
        {
            return SubtreeFactValues.ContainsInvalidTemplateEscape;
        }

        return SubtreeFactValues.FactsNone;
    }
}

public sealed partial class BinaryExpression
{
    internal override uint ComputeSubtreeFacts()
    {
        var facts = (((((Ast.PropagateModifierListSubtreeFacts(this.modifiers) | Ast.PropagateSubtreeFacts(this.Left)) | Ast.PropagateSubtreeFacts(this.Type)) | Ast.PropagateSubtreeFacts(this.OperatorToken)) | Ast.PropagateSubtreeFacts(this.Right)) | Core.IfElse(((this.OperatorToken.Kind == Kind.InKeyword) && Ast.IsPrivateIdentifier(this.Left)), (SubtreeFactValues.ContainsClassFields | SubtreeFactValues.ContainsPrivateIdentifierInExpression), SubtreeFactValues.FactsNone));
        if ((this.OperatorToken.Kind == Kind.EqualsToken))
        {
            if ((((Ast.IsObjectLiteralExpression(this.Left) || Ast.IsArrayLiteralExpression(this.Left))) && Ast.ContainsObjectRestOrSpread(this.Left)))
            {
                facts |= SubtreeFactValues.ContainsObjectRestOrSpread;
            }
        }

        return facts;
    }
}

public sealed partial class YieldExpression
{
    internal override uint ComputeSubtreeFacts()
    {
        return (Ast.PropagateSubtreeFacts(this.Expression) | SubtreeFactValues.ContainsForAwaitOrAsyncGenerator);
    }
}

public sealed partial class ArrowFunction
{
    internal override uint ComputeSubtreeFacts()
    {
        return ((((((Ast.PropagateModifierListSubtreeFacts(this.modifiers) | Ast.PropagateEraseableSyntaxListSubtreeFacts(this.TypeParameters)) | Ast.PropagateNodeListSubtreeFacts(this.Parameters, Ast.PropagateSubtreeFacts)) | Ast.PropagateEraseableSyntaxSubtreeFacts(this.Type)) | Ast.PropagateEraseableSyntaxSubtreeFacts(this.FullSignature)) | Ast.PropagateSubtreeFacts(this.Body)) | Core.IfElse(((this.ModifierFlags() & TypeScript.Scanner.ModifierFlags.Async) != 0), SubtreeFactValues.ContainsAnyAwait, SubtreeFactValues.FactsNone));
    }
}

public sealed partial class ArrowFunction
{
    internal override uint PropagateSubtreeFacts()
    {
        return (this.SubtreeFactsValue() & ~SubtreeFactValues.ExclusionsArrowFunction);
    }
}

public sealed partial class FunctionExpression
{
    internal override uint ComputeSubtreeFacts()
    {
        var isAsync = ((this.modifiers != null) && ((this.modifiers.ModifierFlags & TypeScript.Scanner.ModifierFlags.Async) != 0));
        var isGenerator = (this.AsteriskToken != null);
        return (((((((((Ast.PropagateModifierListSubtreeFacts(this.modifiers) | Ast.PropagateSubtreeFacts(this.AsteriskToken)) | Ast.PropagateSubtreeFacts(this.name)) | Ast.PropagateEraseableSyntaxListSubtreeFacts(this.TypeParameters)) | Ast.PropagateNodeListSubtreeFacts(this.Parameters, Ast.PropagateSubtreeFacts)) | Ast.PropagateEraseableSyntaxSubtreeFacts(this.Type)) | Ast.PropagateEraseableSyntaxSubtreeFacts(this.FullSignature)) | Ast.PropagateSubtreeFacts(this.Body)) | Core.IfElse((isAsync && isGenerator), SubtreeFactValues.ContainsForAwaitOrAsyncGenerator, SubtreeFactValues.FactsNone)) | Core.IfElse((isAsync && !isGenerator), SubtreeFactValues.ContainsAnyAwait, SubtreeFactValues.FactsNone));
    }
}

public sealed partial class FunctionExpression
{
    internal override uint PropagateSubtreeFacts()
    {
        return (this.SubtreeFactsValue() & ~SubtreeFactValues.ExclusionsFunction);
    }
}

public sealed partial class AsExpression
{
    internal override uint ComputeSubtreeFacts()
    {
        return (Ast.PropagateSubtreeFacts(this.Expression) | SubtreeFactValues.ContainsTypeScript);
    }
}

public sealed partial class AsExpression
{
    internal override uint PropagateSubtreeFacts()
    {
        return (this.SubtreeFactsValue() & ~SubtreeFactValues.ExclusionsOuterExpression);
    }
}

public sealed partial class SatisfiesExpression
{
    internal override uint ComputeSubtreeFacts()
    {
        return (Ast.PropagateSubtreeFacts(this.Expression) | SubtreeFactValues.ContainsTypeScript);
    }
}

public sealed partial class SatisfiesExpression
{
    internal override uint PropagateSubtreeFacts()
    {
        return (this.SubtreeFactsValue() & ~SubtreeFactValues.ExclusionsOuterExpression);
    }
}

public sealed partial class PropertyAccessExpression
{
    internal override uint ComputeSubtreeFacts()
    {
        var privateName = SubtreeFactValues.FactsNone;
        if (!Ast.IsIdentifier(this.name))
        {
            privateName = SubtreeFactValues.ContainsPrivateIdentifierInExpression;
        }

        return (((Ast.PropagateSubtreeFacts(this.Expression) | Ast.PropagateSubtreeFacts(this.QuestionDotToken)) | Ast.PropagateSubtreeFacts(this.name)) | privateName);
    }
}

public sealed partial class PropertyAccessExpression
{
    internal override uint PropagateSubtreeFacts()
    {
        return (this.SubtreeFactsValue() & ~SubtreeFactValues.ExclusionsPropertyAccess);
    }
}

public sealed partial class ElementAccessExpression
{
    internal override uint PropagateSubtreeFacts()
    {
        return (this.SubtreeFactsValue() & ~SubtreeFactValues.ExclusionsElementAccess);
    }
}

public sealed partial class CallExpression
{
    internal override uint ComputeSubtreeFacts()
    {
        return ((((Ast.PropagateSubtreeFacts(this.Expression) | Ast.PropagateSubtreeFacts(this.QuestionDotToken)) | Ast.PropagateEraseableSyntaxListSubtreeFacts(this.TypeArguments)) | Ast.PropagateNodeListSubtreeFacts(this.Arguments, Ast.PropagateSubtreeFacts)) | Core.IfElse(Ast.IsImportCall(this.AsNode()), SubtreeFactValues.ContainsDynamicImport, SubtreeFactValues.FactsNone));
    }
}

public sealed partial class CallExpression
{
    internal override uint PropagateSubtreeFacts()
    {
        return (this.SubtreeFactsValue() & ~SubtreeFactValues.ExclusionsCall);
    }
}

public sealed partial class NewExpression
{
    internal override uint ComputeSubtreeFacts()
    {
        return ((Ast.PropagateSubtreeFacts(this.Expression) | Ast.PropagateEraseableSyntaxListSubtreeFacts(this.TypeArguments)) | Ast.PropagateNodeListSubtreeFacts(this.Arguments, Ast.PropagateSubtreeFacts));
    }
}

public sealed partial class NewExpression
{
    internal override uint PropagateSubtreeFacts()
    {
        return (this.SubtreeFactsValue() & ~SubtreeFactValues.ExclusionsNew);
    }
}

public sealed partial class MetaProperty
{
    internal override uint ComputeSubtreeFacts()
    {
        return (Ast.PropagateSubtreeFacts(this.name) & ~SubtreeFactValues.ContainsIdentifier);
    }
}

public sealed partial class NonNullExpression
{
    internal override uint ComputeSubtreeFacts()
    {
        return (Ast.PropagateSubtreeFacts(this.Expression) | SubtreeFactValues.ContainsTypeScript);
    }
}

public sealed partial class SpreadElement
{
    internal override uint ComputeSubtreeFacts()
    {
        return (Ast.PropagateSubtreeFacts(this.Expression) | SubtreeFactValues.ContainsRestOrSpread);
    }
}

public sealed partial class TaggedTemplateExpression
{
    internal override uint ComputeSubtreeFacts()
    {
        return (((Ast.PropagateSubtreeFacts(this.Tag) | Ast.PropagateSubtreeFacts(this.QuestionDotToken)) | Ast.PropagateEraseableSyntaxListSubtreeFacts(this.TypeArguments)) | Ast.PropagateSubtreeFacts(this.Template));
    }
}

public sealed partial class ArrayLiteralExpression
{
    internal override uint PropagateSubtreeFacts()
    {
        return (this.SubtreeFactsValue() & ~SubtreeFactValues.ExclusionsArrayLiteral);
    }
}

public sealed partial class ObjectLiteralExpression
{
    internal override uint PropagateSubtreeFacts()
    {
        return (this.SubtreeFactsValue() & ~SubtreeFactValues.ExclusionsObjectLiteral);
    }
}

public sealed partial class SpreadAssignment
{
    internal override uint ComputeSubtreeFacts()
    {
        return ((Ast.PropagateSubtreeFacts(this.Expression) | SubtreeFactValues.ContainsESObjectRestOrSpread) | SubtreeFactValues.ContainsObjectRestOrSpread);
    }
}

public sealed partial class PropertyAssignment
{
    internal override uint ComputeSubtreeFacts()
    {
        return ((Ast.PropagateSubtreeFacts(this.name) | Ast.PropagateSubtreeFacts(this.Type)) | Ast.PropagateSubtreeFacts(this.Initializer));
    }
}

public sealed partial class ShorthandPropertyAssignment
{
    internal override uint ComputeSubtreeFacts()
    {
        return (((Ast.PropagateSubtreeFacts(this.name) | Ast.PropagateSubtreeFacts(this.Type)) | Ast.PropagateSubtreeFacts(this.ObjectAssignmentInitializer)) | SubtreeFactValues.ContainsTypeScript);
    }
}

public sealed partial class AwaitExpression
{
    internal override uint ComputeSubtreeFacts()
    {
        return (((Ast.PropagateSubtreeFacts(this.Expression) | SubtreeFactValues.ContainsAwait) | SubtreeFactValues.ContainsAnyAwait) | SubtreeFactValues.ContainsForAwaitOrAsyncGenerator);
    }
}

public sealed partial class TypeAssertion
{
    internal override uint ComputeSubtreeFacts()
    {
        return (Ast.PropagateSubtreeFacts(this.Expression) | SubtreeFactValues.ContainsTypeScript);
    }
}

public sealed partial class TypeAssertion
{
    internal override uint PropagateSubtreeFacts()
    {
        return (this.SubtreeFactsValue() & ~SubtreeFactValues.ExclusionsOuterExpression);
    }
}

public sealed partial class KeywordTypeNode
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class KeywordTypeNode
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class UnionTypeNode
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class UnionTypeNode
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class IntersectionTypeNode
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class IntersectionTypeNode
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class ConditionalTypeNode
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class ConditionalTypeNode
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class TypeOperatorNode
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class TypeOperatorNode
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class InferTypeNode
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class InferTypeNode
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class ArrayTypeNode
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class ArrayTypeNode
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class IndexedAccessTypeNode
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class IndexedAccessTypeNode
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class TypeReferenceNode
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class TypeReferenceNode
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class ExpressionWithTypeArguments
{
    internal override uint ComputeSubtreeFacts()
    {
        return (Ast.PropagateSubtreeFacts(this.Expression) | Ast.PropagateEraseableSyntaxListSubtreeFacts(this.TypeArguments));
    }
}

public sealed partial class LiteralTypeNode
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class LiteralTypeNode
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class ThisTypeNode
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class ThisTypeNode
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class TypePredicateNode
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class TypePredicateNode
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class TypeQueryNode
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class TypeQueryNode
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class MappedTypeNode
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class MappedTypeNode
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class TypeLiteralNode
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class TypeLiteralNode
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class TupleTypeNode
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class TupleTypeNode
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class NamedTupleMember
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class NamedTupleMember
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class OptionalTypeNode
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class OptionalTypeNode
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class RestTypeNode
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class RestTypeNode
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class ParenthesizedTypeNode
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class ParenthesizedTypeNode
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class FunctionTypeNode
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class FunctionTypeNode
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class ConstructorTypeNode
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class ConstructorTypeNode
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class TemplateHead
{
    internal override uint ComputeSubtreeFacts()
    {
        if (((this.TemplateFlags & TypeScript.Scanner.TokenFlags.ContainsInvalidEscape) != 0))
        {
            return SubtreeFactValues.ContainsInvalidTemplateEscape;
        }

        return SubtreeFactValues.FactsNone;
    }
}

public sealed partial class TemplateMiddle
{
    internal override uint ComputeSubtreeFacts()
    {
        if (((this.TemplateFlags & TypeScript.Scanner.TokenFlags.ContainsInvalidEscape) != 0))
        {
            return SubtreeFactValues.ContainsInvalidTemplateEscape;
        }

        return SubtreeFactValues.FactsNone;
    }
}

public sealed partial class TemplateTail
{
    internal override uint ComputeSubtreeFacts()
    {
        if (((this.TemplateFlags & TypeScript.Scanner.TokenFlags.ContainsInvalidEscape) != 0))
        {
            return SubtreeFactValues.ContainsInvalidTemplateEscape;
        }

        return SubtreeFactValues.FactsNone;
    }
}

public sealed partial class TemplateLiteralTypeNode
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class TemplateLiteralTypeNode
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class TemplateLiteralTypeSpan
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class TemplateLiteralTypeSpan
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class JsxElement
{
    internal override uint ComputeSubtreeFacts()
    {
        return (((Ast.PropagateSubtreeFacts(this.OpeningElement) | Ast.PropagateNodeListSubtreeFacts(this.Children, Ast.PropagateSubtreeFacts)) | Ast.PropagateSubtreeFacts(this.ClosingElement)) | SubtreeFactValues.ContainsJsx);
    }
}

public sealed partial class JsxAttributes
{
    internal override uint ComputeSubtreeFacts()
    {
        return (Ast.PropagateNodeListSubtreeFacts(this.Properties, Ast.PropagateSubtreeFacts) | SubtreeFactValues.ContainsJsx);
    }
}

public sealed partial class JsxNamespacedName
{
    internal override uint ComputeSubtreeFacts()
    {
        return ((Ast.PropagateSubtreeFacts(this.Namespace) | Ast.PropagateSubtreeFacts(this.name)) | SubtreeFactValues.ContainsJsx);
    }
}

public sealed partial class JsxOpeningElement
{
    internal override uint ComputeSubtreeFacts()
    {
        return (((Ast.PropagateSubtreeFacts(this.TagName) | Ast.PropagateEraseableSyntaxListSubtreeFacts(this.TypeArguments)) | Ast.PropagateSubtreeFacts(this.Attributes)) | SubtreeFactValues.ContainsJsx);
    }
}

public sealed partial class JsxSelfClosingElement
{
    internal override uint ComputeSubtreeFacts()
    {
        return (((Ast.PropagateSubtreeFacts(this.TagName) | Ast.PropagateEraseableSyntaxListSubtreeFacts(this.TypeArguments)) | Ast.PropagateSubtreeFacts(this.Attributes)) | SubtreeFactValues.ContainsJsx);
    }
}

public sealed partial class JsxFragment
{
    internal override uint ComputeSubtreeFacts()
    {
        return (Ast.PropagateNodeListSubtreeFacts(this.Children, Ast.PropagateSubtreeFacts) | SubtreeFactValues.ContainsJsx);
    }
}

public sealed partial class JsxOpeningFragment
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsJsx;
    }
}

public sealed partial class JsxClosingFragment
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsJsx;
    }
}

public sealed partial class JsxAttribute
{
    internal override uint ComputeSubtreeFacts()
    {
        return ((Ast.PropagateSubtreeFacts(this.name) | Ast.PropagateSubtreeFacts(this.Initializer)) | SubtreeFactValues.ContainsJsx);
    }
}

public sealed partial class JsxSpreadAttribute
{
    internal override uint ComputeSubtreeFacts()
    {
        return (Ast.PropagateSubtreeFacts(this.Expression) | SubtreeFactValues.ContainsJsx);
    }
}

public sealed partial class JsxClosingElement
{
    internal override uint ComputeSubtreeFacts()
    {
        return (Ast.PropagateSubtreeFacts(this.TagName) | SubtreeFactValues.ContainsJsx);
    }
}

public sealed partial class JsxExpression
{
    internal override uint ComputeSubtreeFacts()
    {
        return (Ast.PropagateSubtreeFacts(this.Expression) | SubtreeFactValues.ContainsJsx);
    }
}

public sealed partial class JsxText
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsJsx;
    }
}

public sealed partial class JSDocTypeExpression
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class JSDocTypeExpression
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class JSDocNonNullableType
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class JSDocNonNullableType
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class JSDocNullableType
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class JSDocNullableType
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class JSDocAllType
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class JSDocAllType
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class JSDocVariadicType
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class JSDocVariadicType
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class JSDocOptionalType
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class JSDocOptionalType
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class JSDocSignature
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class JSDocSignature
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class JSDocNameReference
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class JSDocNameReference
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class ModuleDeclaration
{
    internal override uint ComputeSubtreeFacts()
    {
        if (((this.ModifierFlags() & TypeScript.Scanner.ModifierFlags.Ambient) != 0))
        {
            return SubtreeFactValues.ContainsTypeScript;
        }
        else
        {
            return (((Ast.PropagateModifierListSubtreeFacts(this.modifiers) | Ast.PropagateSubtreeFacts(this.name)) | Ast.PropagateSubtreeFacts(this.Body)) | SubtreeFactValues.ContainsTypeScript);
        }
    }
}

public sealed partial class ModuleDeclaration
{
    internal override uint PropagateSubtreeFacts()
    {
        return (this.SubtreeFactsValue() & ~SubtreeFactValues.ExclusionsModule);
    }
}

public sealed partial class ImportEqualsDeclaration
{
    internal override uint ComputeSubtreeFacts()
    {
        if ((this.IsTypeOnly || !Ast.IsExternalModuleReference(this.ModuleReference)))
        {
            return SubtreeFactValues.ContainsTypeScript;
        }
        else
        {
            return ((Ast.PropagateModifierListSubtreeFacts(this.modifiers) | Ast.PropagateSubtreeFacts(this.name)) | Ast.PropagateSubtreeFacts(this.ModuleReference));
        }
    }
}

public sealed partial class ExportDeclaration
{
    internal override uint ComputeSubtreeFacts()
    {
        return ((((Ast.PropagateModifierListSubtreeFacts(this.modifiers) | Ast.PropagateSubtreeFacts(this.ExportClause)) | Ast.PropagateSubtreeFacts(this.ModuleSpecifier)) | Ast.PropagateSubtreeFacts(this.Attributes)) | Core.IfElse(this.IsTypeOnly, SubtreeFactValues.ContainsTypeScript, SubtreeFactValues.FactsNone));
    }
}

public sealed partial class ImportTypeNode
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class ImportTypeNode
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class ImportClause
{
    internal override uint ComputeSubtreeFacts()
    {
        if ((this.PhaseModifier == Kind.TypeKeyword))
        {
            return SubtreeFactValues.ContainsTypeScript;
        }
        else
        {
            return (Ast.PropagateSubtreeFacts(this.name) | Ast.PropagateSubtreeFacts(this.NamedBindings));
        }
    }
}

public sealed partial class ImportSpecifier
{
    internal override uint ComputeSubtreeFacts()
    {
        if (this.IsTypeOnly)
        {
            return SubtreeFactValues.ContainsTypeScript;
        }
        else
        {
            return (Ast.PropagateSubtreeFacts(this.PropertyName) | Ast.PropagateSubtreeFacts(this.name));
        }
    }
}

public sealed partial class TypeParameterDeclaration
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class TypeParameterDeclaration
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class JSDocTypeLiteral
{
    internal override uint ComputeSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

public sealed partial class JSDocTypeLiteral
{
    internal override uint PropagateSubtreeFacts()
    {
        return SubtreeFactValues.ContainsTypeScript;
    }
}

// END GENERATED SUBTREE FACTS
// BEGIN GENERATED AST ACCESSORS (tools/generate-ast-accessors.go)
public partial class Node
{
    public Node Expression()
    {
        switch (this.Kind)
        {
            case Kind.PropertyAccessExpression:
            {
                return this.AsPropertyAccessExpression().Expression;
            }

            case Kind.ElementAccessExpression:
            {
                return this.AsElementAccessExpression().Expression;
            }

            case Kind.ParenthesizedExpression:
            {
                return this.AsParenthesizedExpression().Expression;
            }

            case Kind.CallExpression:
            {
                return this.AsCallExpression().Expression;
            }

            case Kind.NewExpression:
            {
                return this.AsNewExpression().Expression;
            }

            case Kind.ExpressionWithTypeArguments:
            {
                return this.AsExpressionWithTypeArguments().Expression;
            }

            case Kind.ComputedPropertyName:
            {
                return this.AsComputedPropertyName().Expression;
            }

            case Kind.NonNullExpression:
            {
                return this.AsNonNullExpression().Expression;
            }

            case Kind.TypeAssertionExpression:
            {
                return this.AsTypeAssertion().Expression;
            }

            case Kind.AsExpression:
            {
                return this.AsAsExpression().Expression;
            }

            case Kind.SatisfiesExpression:
            {
                return this.AsSatisfiesExpression().Expression;
            }

            case Kind.TypeOfExpression:
            {
                return this.AsTypeOfExpression().Expression;
            }

            case Kind.SpreadAssignment:
            {
                return this.AsSpreadAssignment().Expression;
            }

            case Kind.SpreadElement:
            {
                return this.AsSpreadElement().Expression;
            }

            case Kind.TemplateSpan:
            {
                return this.AsTemplateSpan().Expression;
            }

            case Kind.DeleteExpression:
            {
                return this.AsDeleteExpression().Expression;
            }

            case Kind.VoidExpression:
            {
                return this.AsVoidExpression().Expression;
            }

            case Kind.AwaitExpression:
            {
                return this.AsAwaitExpression().Expression;
            }

            case Kind.YieldExpression:
            {
                return this.AsYieldExpression().Expression;
            }

            case Kind.PartiallyEmittedExpression:
            {
                return this.AsPartiallyEmittedExpression().Expression;
            }

            case Kind.IfStatement:
            {
                return this.AsIfStatement().Expression;
            }

            case Kind.DoStatement:
            {
                return this.AsDoStatement().Expression;
            }

            case Kind.WhileStatement:
            {
                return this.AsWhileStatement().Expression;
            }

            case Kind.WithStatement:
            {
                return this.AsWithStatement().Expression;
            }

            case Kind.ForInStatement:
            case Kind.ForOfStatement:
            {
                return this.AsForInOrOfStatement().Expression;
            }

            case Kind.SwitchStatement:
            {
                return this.AsSwitchStatement().Expression;
            }

            case Kind.CaseClause:
            {
                return this.AsCaseOrDefaultClause().Expression;
            }

            case Kind.ExpressionStatement:
            {
                return this.AsExpressionStatement().Expression;
            }

            case Kind.ReturnStatement:
            {
                return this.AsReturnStatement().Expression;
            }

            case Kind.ThrowStatement:
            {
                return this.AsThrowStatement().Expression;
            }

            case Kind.ExternalModuleReference:
            {
                return this.AsExternalModuleReference().Expression;
            }

            case Kind.ExportAssignment:
            {
                return this.AsExportAssignment().Expression;
            }

            case Kind.Decorator:
            {
                return this.AsDecorator().Expression;
            }

            case Kind.JsxExpression:
            {
                return this.AsJsxExpression().Expression;
            }

            case Kind.JsxSpreadAttribute:
            {
                return this.AsJsxSpreadAttribute().Expression;
            }
        }

        throw new InvalidOperationException(("Unhandled case in Node.Expression: " + this.Kind.ToString()));
    }

    public string RawText()
    {
        switch (this.Kind)
        {
            case Kind.TemplateHead:
            {
                return this.AsTemplateHead().RawText;
            }

            case Kind.TemplateMiddle:
            {
                return this.AsTemplateMiddle().RawText;
            }

            case Kind.TemplateTail:
            {
                return this.AsTemplateTail().RawText;
            }
        }

        throw new InvalidOperationException(("Unhandled case in Node.RawText: " + this.Kind.ToString()));
    }

    public void SetExpression(Node expr)
    {
        switch (this.Kind)
        {
            case Kind.PropertyAccessExpression:
            {
                this.AsPropertyAccessExpression().Expression = expr;
                break;
            }

            case Kind.ElementAccessExpression:
            {
                this.AsElementAccessExpression().Expression = expr;
                break;
            }

            case Kind.ParenthesizedExpression:
            {
                this.AsParenthesizedExpression().Expression = expr;
                break;
            }

            case Kind.CallExpression:
            {
                this.AsCallExpression().Expression = expr;
                break;
            }

            case Kind.NewExpression:
            {
                this.AsNewExpression().Expression = expr;
                break;
            }

            case Kind.ExpressionWithTypeArguments:
            {
                this.AsExpressionWithTypeArguments().Expression = expr;
                break;
            }

            case Kind.ComputedPropertyName:
            {
                this.AsComputedPropertyName().Expression = expr;
                break;
            }

            case Kind.NonNullExpression:
            {
                this.AsNonNullExpression().Expression = expr;
                break;
            }

            case Kind.TypeAssertionExpression:
            {
                this.AsTypeAssertion().Expression = expr;
                break;
            }

            case Kind.AsExpression:
            {
                this.AsAsExpression().Expression = expr;
                break;
            }

            case Kind.SatisfiesExpression:
            {
                this.AsSatisfiesExpression().Expression = expr;
                break;
            }

            case Kind.TypeOfExpression:
            {
                this.AsTypeOfExpression().Expression = expr;
                break;
            }

            case Kind.SpreadAssignment:
            {
                this.AsSpreadAssignment().Expression = expr;
                break;
            }

            case Kind.SpreadElement:
            {
                this.AsSpreadElement().Expression = expr;
                break;
            }

            case Kind.TemplateSpan:
            {
                this.AsTemplateSpan().Expression = expr;
                break;
            }

            case Kind.DeleteExpression:
            {
                this.AsDeleteExpression().Expression = expr;
                break;
            }

            case Kind.VoidExpression:
            {
                this.AsVoidExpression().Expression = expr;
                break;
            }

            case Kind.AwaitExpression:
            {
                this.AsAwaitExpression().Expression = expr;
                break;
            }

            case Kind.YieldExpression:
            {
                this.AsYieldExpression().Expression = expr;
                break;
            }

            case Kind.PartiallyEmittedExpression:
            {
                this.AsPartiallyEmittedExpression().Expression = expr;
                break;
            }

            case Kind.IfStatement:
            {
                this.AsIfStatement().Expression = expr;
                break;
            }

            case Kind.DoStatement:
            {
                this.AsDoStatement().Expression = expr;
                break;
            }

            case Kind.WhileStatement:
            {
                this.AsWhileStatement().Expression = expr;
                break;
            }

            case Kind.WithStatement:
            {
                this.AsWithStatement().Expression = expr;
                break;
            }

            case Kind.ForInStatement:
            case Kind.ForOfStatement:
            {
                this.AsForInOrOfStatement().Expression = expr;
                break;
            }

            case Kind.SwitchStatement:
            {
                this.AsSwitchStatement().Expression = expr;
                break;
            }

            case Kind.CaseClause:
            {
                this.AsCaseOrDefaultClause().Expression = expr;
                break;
            }

            case Kind.ExpressionStatement:
            {
                this.AsExpressionStatement().Expression = expr;
                break;
            }

            case Kind.ReturnStatement:
            {
                this.AsReturnStatement().Expression = expr;
                break;
            }

            case Kind.ThrowStatement:
            {
                this.AsThrowStatement().Expression = expr;
                break;
            }

            case Kind.ExternalModuleReference:
            {
                this.AsExternalModuleReference().Expression = expr;
                break;
            }

            case Kind.ExportAssignment:
            {
                this.AsExportAssignment().Expression = expr;
                break;
            }

            case Kind.Decorator:
            {
                this.AsDecorator().Expression = expr;
                break;
            }

            case Kind.JsxExpression:
            {
                this.AsJsxExpression().Expression = expr;
                break;
            }

            case Kind.JsxSpreadAttribute:
            {
                this.AsJsxSpreadAttribute().Expression = expr;
                break;
            }

            default:
            {
                throw new InvalidOperationException(("Unhandled case in mutableNode.SetExpression: " + this.Kind.ToString()));
            }
        }
    }

    public NodeList ArgumentList()
    {
        switch (this.Kind)
        {
            case Kind.CallExpression:
            {
                return this.AsCallExpression().Arguments;
            }

            case Kind.NewExpression:
            {
                return this.AsNewExpression().Arguments;
            }
        }

        throw new InvalidOperationException(("Unhandled case in Node.Arguments: " + this.Kind.ToString()));
    }

    public NodeList TypeArgumentList()
    {
        switch (this.Kind)
        {
            case Kind.CallExpression:
            {
                return this.AsCallExpression().TypeArguments;
            }

            case Kind.NewExpression:
            {
                return this.AsNewExpression().TypeArguments;
            }

            case Kind.TaggedTemplateExpression:
            {
                return this.AsTaggedTemplateExpression().TypeArguments;
            }

            case Kind.TypeReference:
            {
                return this.AsTypeReferenceNode().TypeArguments;
            }

            case Kind.ExpressionWithTypeArguments:
            {
                return this.AsExpressionWithTypeArguments().TypeArguments;
            }

            case Kind.ImportType:
            {
                return this.AsImportTypeNode().TypeArguments;
            }

            case Kind.TypeQuery:
            {
                return this.AsTypeQueryNode().TypeArguments;
            }

            case Kind.JsxOpeningElement:
            {
                return this.AsJsxOpeningElement().TypeArguments;
            }

            case Kind.JsxSelfClosingElement:
            {
                return this.AsJsxSelfClosingElement().TypeArguments;
            }
        }

        throw new InvalidOperationException("Unhandled case in Node.TypeArguments");
    }

    public NodeList TypeParameterList()
    {
        switch (this.Kind)
        {
            case Kind.ClassDeclaration:
            {
                return this.AsClassDeclaration().TypeParameters;
            }

            case Kind.ClassExpression:
            {
                return this.AsClassExpression().TypeParameters;
            }

            case Kind.InterfaceDeclaration:
            {
                return this.AsInterfaceDeclaration().TypeParameters;
            }

            case Kind.TypeAliasDeclaration:
            case Kind.JSTypeAliasDeclaration:
            {
                return this.AsTypeAliasDeclaration().TypeParameters;
            }

            case Kind.JSDocTemplateTag:
            {
                return this.AsJSDocTemplateTag().TypeParameters;
            }

            default:
            {
                var funcLike = this.FunctionLikeData();
                if ((funcLike != null))
                {
                    return funcLike.TypeParameters;
                }

                break;
            }
        }

        throw new InvalidOperationException("Unhandled case in Node.TypeParameterList");
    }

    public NodeList MemberList()
    {
        switch (this.Kind)
        {
            case Kind.ClassDeclaration:
            {
                return this.AsClassDeclaration().Members;
            }

            case Kind.ClassExpression:
            {
                return this.AsClassExpression().Members;
            }

            case Kind.InterfaceDeclaration:
            {
                return this.AsInterfaceDeclaration().Members;
            }

            case Kind.EnumDeclaration:
            {
                return this.AsEnumDeclaration().Members;
            }

            case Kind.TypeLiteral:
            {
                return this.AsTypeLiteralNode().Members;
            }

            case Kind.MappedType:
            {
                return this.AsMappedTypeNode().Members;
            }
        }

        throw new InvalidOperationException(("Unhandled case in Node.MemberList: " + this.Kind.ToString()));
    }

    public NodeList StatementList()
    {
        switch (this.Kind)
        {
            case Kind.SourceFile:
            {
                return this.AsSourceFile().Statements;
            }

            case Kind.Block:
            {
                return this.AsBlock().Statements;
            }

            case Kind.ModuleBlock:
            {
                return this.AsModuleBlock().Statements;
            }

            case Kind.CaseClause:
            case Kind.DefaultClause:
            {
                return this.AsCaseOrDefaultClause().Statements;
            }
        }

        throw new InvalidOperationException(("Unhandled case in Node.StatementList: " + this.Kind.ToString()));
    }

    public bool CanHaveStatements()
    {
        switch (this.Kind)
        {
            case Kind.SourceFile:
            case Kind.Block:
            case Kind.ModuleBlock:
            case Kind.CaseClause:
            case Kind.DefaultClause:
            {
                return true;
            }

            default:
            {
                return false;
            }
        }
    }

    public Node Type()
    {
        switch (this.Kind)
        {
            case Kind.VariableDeclaration:
            {
                return this.AsVariableDeclaration().Type;
            }

            case Kind.Parameter:
            {
                return this.AsParameterDeclaration().Type;
            }

            case Kind.PropertySignature:
            {
                return this.AsPropertySignatureDeclaration().Type;
            }

            case Kind.PropertyDeclaration:
            {
                return this.AsPropertyDeclaration().Type;
            }

            case Kind.PropertyAssignment:
            {
                return this.AsPropertyAssignment().Type;
            }

            case Kind.ShorthandPropertyAssignment:
            {
                return this.AsShorthandPropertyAssignment().Type;
            }

            case Kind.TypePredicate:
            {
                return this.AsTypePredicateNode().Type;
            }

            case Kind.ParenthesizedType:
            {
                return this.AsParenthesizedTypeNode().Type;
            }

            case Kind.TypeOperator:
            {
                return this.AsTypeOperatorNode().Type;
            }

            case Kind.MappedType:
            {
                return this.AsMappedTypeNode().Type;
            }

            case Kind.TypeAssertionExpression:
            {
                return this.AsTypeAssertion().Type;
            }

            case Kind.AsExpression:
            {
                return this.AsAsExpression().Type;
            }

            case Kind.SatisfiesExpression:
            {
                return this.AsSatisfiesExpression().Type;
            }

            case Kind.TypeAliasDeclaration:
            case Kind.JSTypeAliasDeclaration:
            {
                return this.AsTypeAliasDeclaration().Type;
            }

            case Kind.NamedTupleMember:
            {
                return this.AsNamedTupleMember().Type;
            }

            case Kind.OptionalType:
            {
                return this.AsOptionalTypeNode().Type;
            }

            case Kind.RestType:
            {
                return this.AsRestTypeNode().Type;
            }

            case Kind.TemplateLiteralTypeSpan:
            {
                return this.AsTemplateLiteralTypeSpan().Type;
            }

            case Kind.JSDocTypeExpression:
            {
                return this.AsJSDocTypeExpression().Type;
            }

            case Kind.JSDocParameterTag:
            case Kind.JSDocPropertyTag:
            {
                return this.AsJSDocParameterOrPropertyTag().TypeExpression;
            }

            case Kind.JSDocNullableType:
            {
                return this.AsJSDocNullableType().Type;
            }

            case Kind.JSDocNonNullableType:
            {
                return this.AsJSDocNonNullableType().Type;
            }

            case Kind.JSDocOptionalType:
            {
                return this.AsJSDocOptionalType().Type;
            }

            case Kind.ExportAssignment:
            {
                return this.AsExportAssignment().Type;
            }

            case Kind.BinaryExpression:
            {
                return this.AsBinaryExpression().Type;
            }

            default:
            {
                {
                    var funcLike = this.FunctionLikeData();
                    if ((funcLike != null))
                    {
                        return funcLike.Type;
                    }
                }

                break;
            }
        }

        return null;
    }

    public void SetType(Node t)
    {
        switch (this.Kind)
        {
            case Kind.VariableDeclaration:
            {
                this.AsVariableDeclaration().Type = t;
                break;
            }

            case Kind.Parameter:
            {
                this.AsParameterDeclaration().Type = t;
                break;
            }

            case Kind.PropertySignature:
            {
                this.AsPropertySignatureDeclaration().Type = t;
                break;
            }

            case Kind.PropertyDeclaration:
            {
                this.AsPropertyDeclaration().Type = t;
                break;
            }

            case Kind.PropertyAssignment:
            {
                this.AsPropertyAssignment().Type = t;
                break;
            }

            case Kind.ShorthandPropertyAssignment:
            {
                this.AsShorthandPropertyAssignment().Type = t;
                break;
            }

            case Kind.TypePredicate:
            {
                this.AsTypePredicateNode().Type = t;
                break;
            }

            case Kind.ParenthesizedType:
            {
                this.AsParenthesizedTypeNode().Type = t;
                break;
            }

            case Kind.TypeOperator:
            {
                this.AsTypeOperatorNode().Type = t;
                break;
            }

            case Kind.MappedType:
            {
                this.AsMappedTypeNode().Type = t;
                break;
            }

            case Kind.TypeAssertionExpression:
            {
                this.AsTypeAssertion().Type = t;
                break;
            }

            case Kind.AsExpression:
            {
                this.AsAsExpression().Type = t;
                break;
            }

            case Kind.SatisfiesExpression:
            {
                this.AsSatisfiesExpression().Type = t;
                break;
            }

            case Kind.TypeAliasDeclaration:
            case Kind.JSTypeAliasDeclaration:
            {
                this.AsTypeAliasDeclaration().Type = t;
                break;
            }

            case Kind.NamedTupleMember:
            {
                this.AsNamedTupleMember().Type = t;
                break;
            }

            case Kind.OptionalType:
            {
                this.AsOptionalTypeNode().Type = t;
                break;
            }

            case Kind.RestType:
            {
                this.AsRestTypeNode().Type = t;
                break;
            }

            case Kind.TemplateLiteralTypeSpan:
            {
                this.AsTemplateLiteralTypeSpan().Type = t;
                break;
            }

            case Kind.JSDocTypeExpression:
            {
                this.AsJSDocTypeExpression().Type = t;
                break;
            }

            case Kind.JSDocParameterTag:
            case Kind.JSDocPropertyTag:
            {
                this.AsJSDocParameterOrPropertyTag().TypeExpression = t;
                break;
            }

            case Kind.JSDocNullableType:
            {
                this.AsJSDocNullableType().Type = t;
                break;
            }

            case Kind.JSDocNonNullableType:
            {
                this.AsJSDocNonNullableType().Type = t;
                break;
            }

            case Kind.JSDocOptionalType:
            {
                this.AsJSDocOptionalType().Type = t;
                break;
            }

            case Kind.ExportAssignment:
            {
                this.AsExportAssignment().Type = t;
                break;
            }

            case Kind.BinaryExpression:
            {
                this.AsBinaryExpression().Type = t;
                break;
            }

            default:
            {
                {
                    var funcLike = this.FunctionLikeData();
                    if ((funcLike != null))
                    {
                        funcLike.Type = t;
                    }
                    else
                    {
                        throw new InvalidOperationException(("Unhandled case in mutableNode.SetType: " + this.Kind.ToString()));
                    }
                }

                break;
            }
        }
    }

    public Node Initializer()
    {
        switch (this.Kind)
        {
            case Kind.VariableDeclaration:
            {
                return this.AsVariableDeclaration().Initializer;
            }

            case Kind.Parameter:
            {
                return this.AsParameterDeclaration().Initializer;
            }

            case Kind.BindingElement:
            {
                return this.AsBindingElement().Initializer;
            }

            case Kind.PropertyDeclaration:
            {
                return this.AsPropertyDeclaration().Initializer;
            }

            case Kind.PropertySignature:
            {
                return this.AsPropertySignatureDeclaration().Initializer;
            }

            case Kind.PropertyAssignment:
            {
                return this.AsPropertyAssignment().Initializer;
            }

            case Kind.EnumMember:
            {
                return this.AsEnumMember().Initializer;
            }

            case Kind.ForStatement:
            {
                return this.AsForStatement().Initializer;
            }

            case Kind.ForInStatement:
            case Kind.ForOfStatement:
            {
                return this.AsForInOrOfStatement().Initializer;
            }

            case Kind.JsxAttribute:
            {
                return this.AsJsxAttribute().Initializer;
            }
        }

        throw new InvalidOperationException("Unhandled case in Node.Initializer");
    }

    public void SetInitializer(Node initializer)
    {
        switch (this.Kind)
        {
            case Kind.VariableDeclaration:
            {
                this.AsVariableDeclaration().Initializer = initializer;
                break;
            }

            case Kind.Parameter:
            {
                this.AsParameterDeclaration().Initializer = initializer;
                break;
            }

            case Kind.BindingElement:
            {
                this.AsBindingElement().Initializer = initializer;
                break;
            }

            case Kind.PropertyDeclaration:
            {
                this.AsPropertyDeclaration().Initializer = initializer;
                break;
            }

            case Kind.PropertySignature:
            {
                this.AsPropertySignatureDeclaration().Initializer = initializer;
                break;
            }

            case Kind.PropertyAssignment:
            {
                this.AsPropertyAssignment().Initializer = initializer;
                break;
            }

            case Kind.EnumMember:
            {
                this.AsEnumMember().Initializer = initializer;
                break;
            }

            case Kind.ForStatement:
            {
                this.AsForStatement().Initializer = initializer;
                break;
            }

            case Kind.ForInStatement:
            case Kind.ForOfStatement:
            {
                this.AsForInOrOfStatement().Initializer = initializer;
                break;
            }

            case Kind.JsxAttribute:
            {
                this.AsJsxAttribute().Initializer = initializer;
                break;
            }

            default:
            {
                throw new InvalidOperationException("Unhandled case in mutableNode.SetInitializer");
            }
        }
    }

    public Node TagName()
    {
        switch (this.Kind)
        {
            case Kind.JsxOpeningElement:
            {
                return this.AsJsxOpeningElement().TagName;
            }

            case Kind.JsxClosingElement:
            {
                return this.AsJsxClosingElement().TagName;
            }

            case Kind.JsxSelfClosingElement:
            {
                return this.AsJsxSelfClosingElement().TagName;
            }

            case Kind.JSDocUnknownTag:
            {
                return this.AsJSDocUnknownTag().TagName;
            }

            case Kind.JSDocAugmentsTag:
            {
                return this.AsJSDocAugmentsTag().TagName;
            }

            case Kind.JSDocImplementsTag:
            {
                return this.AsJSDocImplementsTag().TagName;
            }

            case Kind.JSDocDeprecatedTag:
            {
                return this.AsJSDocDeprecatedTag().TagName;
            }

            case Kind.JSDocPublicTag:
            {
                return this.AsJSDocPublicTag().TagName;
            }

            case Kind.JSDocPrivateTag:
            {
                return this.AsJSDocPrivateTag().TagName;
            }

            case Kind.JSDocProtectedTag:
            {
                return this.AsJSDocProtectedTag().TagName;
            }

            case Kind.JSDocReadonlyTag:
            {
                return this.AsJSDocReadonlyTag().TagName;
            }

            case Kind.JSDocOverrideTag:
            {
                return this.AsJSDocOverrideTag().TagName;
            }

            case Kind.JSDocCallbackTag:
            {
                return this.AsJSDocCallbackTag().TagName;
            }

            case Kind.JSDocOverloadTag:
            {
                return this.AsJSDocOverloadTag().TagName;
            }

            case Kind.JSDocParameterTag:
            case Kind.JSDocPropertyTag:
            {
                return this.AsJSDocParameterOrPropertyTag().TagName;
            }

            case Kind.JSDocReturnTag:
            {
                return this.AsJSDocReturnTag().TagName;
            }

            case Kind.JSDocThisTag:
            {
                return this.AsJSDocThisTag().TagName;
            }

            case Kind.JSDocTypeTag:
            {
                return this.AsJSDocTypeTag().TagName;
            }

            case Kind.JSDocTemplateTag:
            {
                return this.AsJSDocTemplateTag().TagName;
            }

            case Kind.JSDocTypedefTag:
            {
                return this.AsJSDocTypedefTag().TagName;
            }

            case Kind.JSDocSeeTag:
            {
                return this.AsJSDocSeeTag().TagName;
            }

            case Kind.JSDocSatisfiesTag:
            {
                return this.AsJSDocSatisfiesTag().TagName;
            }

            case Kind.JSDocThrowsTag:
            {
                return this.AsJSDocThrowsTag().TagName;
            }

            case Kind.JSDocImportTag:
            {
                return this.AsJSDocImportTag().TagName;
            }
        }

        throw new InvalidOperationException(("Unhandled case in Node.TagName: " + this.Kind.ToString()));
    }

    public Node PropertyName()
    {
        switch (this.Kind)
        {
            case Kind.ImportSpecifier:
            {
                return this.AsImportSpecifier().PropertyName;
            }

            case Kind.ExportSpecifier:
            {
                return this.AsExportSpecifier().PropertyName;
            }

            case Kind.BindingElement:
            {
                return this.AsBindingElement().PropertyName;
            }
        }

        return null;
    }

    public Node PropertyNameOrName()
    {
        var name = this.PropertyName();
        if ((name == null))
        {
            name = this.Name();
        }

        return name;
    }

    public bool IsTypeOnly()
    {
        switch (this.Kind)
        {
            case Kind.ImportEqualsDeclaration:
            {
                return this.AsImportEqualsDeclaration().IsTypeOnly;
            }

            case Kind.ImportSpecifier:
            {
                return this.AsImportSpecifier().IsTypeOnly;
            }

            case Kind.ImportClause:
            {
                return (this.AsImportClause().PhaseModifier == Kind.TypeKeyword);
            }

            case Kind.ExportDeclaration:
            {
                return this.AsExportDeclaration().IsTypeOnly;
            }

            case Kind.ExportSpecifier:
            {
                return this.AsExportSpecifier().IsTypeOnly;
            }
        }

        return false;
    }

    public NodeList CommentList()
    {
        switch (this.Kind)
        {
            case Kind.JSDoc:
            {
                return this.AsJSDoc().Comment;
            }

            case Kind.JSDocUnknownTag:
            {
                return this.AsJSDocUnknownTag().Comment;
            }

            case Kind.JSDocAugmentsTag:
            {
                return this.AsJSDocAugmentsTag().Comment;
            }

            case Kind.JSDocImplementsTag:
            {
                return this.AsJSDocImplementsTag().Comment;
            }

            case Kind.JSDocDeprecatedTag:
            {
                return this.AsJSDocDeprecatedTag().Comment;
            }

            case Kind.JSDocPublicTag:
            {
                return this.AsJSDocPublicTag().Comment;
            }

            case Kind.JSDocPrivateTag:
            {
                return this.AsJSDocPrivateTag().Comment;
            }

            case Kind.JSDocProtectedTag:
            {
                return this.AsJSDocProtectedTag().Comment;
            }

            case Kind.JSDocReadonlyTag:
            {
                return this.AsJSDocReadonlyTag().Comment;
            }

            case Kind.JSDocOverrideTag:
            {
                return this.AsJSDocOverrideTag().Comment;
            }

            case Kind.JSDocCallbackTag:
            {
                return this.AsJSDocCallbackTag().Comment;
            }

            case Kind.JSDocOverloadTag:
            {
                return this.AsJSDocOverloadTag().Comment;
            }

            case Kind.JSDocParameterTag:
            case Kind.JSDocPropertyTag:
            {
                return this.AsJSDocParameterOrPropertyTag().Comment;
            }

            case Kind.JSDocReturnTag:
            {
                return this.AsJSDocReturnTag().Comment;
            }

            case Kind.JSDocThisTag:
            {
                return this.AsJSDocThisTag().Comment;
            }

            case Kind.JSDocTypeTag:
            {
                return this.AsJSDocTypeTag().Comment;
            }

            case Kind.JSDocTemplateTag:
            {
                return this.AsJSDocTemplateTag().Comment;
            }

            case Kind.JSDocTypedefTag:
            {
                return this.AsJSDocTypedefTag().Comment;
            }

            case Kind.JSDocSeeTag:
            {
                return this.AsJSDocSeeTag().Comment;
            }

            case Kind.JSDocSatisfiesTag:
            {
                return this.AsJSDocSatisfiesTag().Comment;
            }

            case Kind.JSDocThrowsTag:
            {
                return this.AsJSDocThrowsTag().Comment;
            }

            case Kind.JSDocImportTag:
            {
                return this.AsJSDocImportTag().Comment;
            }
        }

        throw new InvalidOperationException(("Unhandled case in Node.CommentList: " + this.Kind.ToString()));
    }

    public Node Label()
    {
        switch (this.Kind)
        {
            case Kind.LabeledStatement:
            {
                return this.AsLabeledStatement().Label;
            }

            case Kind.BreakStatement:
            {
                return this.AsBreakStatement().Label;
            }

            case Kind.ContinueStatement:
            {
                return this.AsContinueStatement().Label;
            }
        }

        throw new InvalidOperationException(("Unhandled case in Node.Label: " + this.Kind.ToString()));
    }

    public Node Attributes()
    {
        switch (this.Kind)
        {
            case Kind.JsxOpeningElement:
            {
                return this.AsJsxOpeningElement().Attributes;
            }

            case Kind.JsxSelfClosingElement:
            {
                return this.AsJsxSelfClosingElement().Attributes;
            }

            case Kind.ModuleDeclaration:
            {
                return this.AsModuleDeclaration().Attributes;
            }
        }

        throw new InvalidOperationException(("Unhandled case in Node.Attributes: " + this.Kind.ToString()));
    }

    public NodeList Children()
    {
        switch (this.Kind)
        {
            case Kind.JsxElement:
            {
                return this.AsJsxElement().Children;
            }

            case Kind.JsxFragment:
            {
                return this.AsJsxFragment().Children;
            }
        }

        throw new InvalidOperationException(("Unhandled case in Node.Children: " + this.Kind.ToString()));
    }

    public Node ModuleSpecifier()
    {
        switch (this.Kind)
        {
            case Kind.ImportDeclaration:
            case Kind.JSImportDeclaration:
            {
                return this.AsImportDeclaration().ModuleSpecifier;
            }

            case Kind.ExportDeclaration:
            {
                return this.AsExportDeclaration().ModuleSpecifier;
            }

            case Kind.JSDocImportTag:
            {
                return this.AsJSDocImportTag().ModuleSpecifier;
            }
        }

        throw new InvalidOperationException(("Unhandled case in Node.ModuleSpecifier: " + this.Kind.ToString()));
    }

    public Node ImportClause()
    {
        switch (this.Kind)
        {
            case Kind.ImportDeclaration:
            case Kind.JSImportDeclaration:
            {
                return this.AsImportDeclaration().ImportClause;
            }

            case Kind.JSDocImportTag:
            {
                return this.AsJSDocImportTag().ImportClause;
            }
        }

        throw new InvalidOperationException(("Unhandled case in Node.ImportClause: " + this.Kind.ToString()));
    }

    public Node Statement()
    {
        switch (this.Kind)
        {
            case Kind.DoStatement:
            {
                return this.AsDoStatement().Statement;
            }

            case Kind.WhileStatement:
            {
                return this.AsWhileStatement().Statement;
            }

            case Kind.ForStatement:
            {
                return this.AsForStatement().Statement;
            }

            case Kind.ForInStatement:
            case Kind.ForOfStatement:
            {
                return this.AsForInOrOfStatement().Statement;
            }

            case Kind.WithStatement:
            {
                return this.AsWithStatement().Statement;
            }

            case Kind.LabeledStatement:
            {
                return this.AsLabeledStatement().Statement;
            }
        }

        throw new InvalidOperationException(("Unhandled case in Node.Statement: " + this.Kind.ToString()));
    }

    public NodeList PropertyList()
    {
        switch (this.Kind)
        {
            case Kind.ObjectLiteralExpression:
            {
                return this.AsObjectLiteralExpression().Properties;
            }

            case Kind.JsxAttributes:
            {
                return this.AsJsxAttributes().Properties;
            }
        }

        throw new InvalidOperationException(("Unhandled case in Node.PropertyList: " + this.Kind.ToString()));
    }

    public NodeList ElementList()
    {
        switch (this.Kind)
        {
            case Kind.NamedImports:
            {
                return this.AsNamedImports().Elements;
            }

            case Kind.NamedExports:
            {
                return this.AsNamedExports().Elements;
            }

            case Kind.ObjectBindingPattern:
            case Kind.ArrayBindingPattern:
            {
                return this.AsBindingPattern().Elements;
            }

            case Kind.ArrayLiteralExpression:
            {
                return this.AsArrayLiteralExpression().Elements;
            }

            case Kind.TupleType:
            {
                return this.AsTupleTypeNode().Elements;
            }
        }

        throw new InvalidOperationException(("Unhandled case in Node.ElementList: " + this.Kind.ToString()));
    }

    public Node PostfixToken()
    {
        switch (this.Kind)
        {
            case Kind.MethodDeclaration:
            {
                return this.AsMethodDeclaration().PostfixToken;
            }

            case Kind.ShorthandPropertyAssignment:
            {
                return this.AsShorthandPropertyAssignment().PostfixToken;
            }

            case Kind.MethodSignature:
            {
                return this.AsMethodSignatureDeclaration().PostfixToken;
            }

            case Kind.PropertySignature:
            {
                return this.AsPropertySignatureDeclaration().PostfixToken;
            }

            case Kind.PropertyAssignment:
            {
                return this.AsPropertyAssignment().PostfixToken;
            }

            case Kind.PropertyDeclaration:
            {
                return this.AsPropertyDeclaration().PostfixToken;
            }

            case Kind.EnumMember:
            {
                return this.AsEnumMember().PostfixToken;
            }

            case Kind.GetAccessor:
            {
                return this.AsGetAccessorDeclaration().PostfixToken;
            }

            case Kind.SetAccessor:
            {
                return this.AsSetAccessorDeclaration().PostfixToken;
            }
        }

        return null;
    }

    public Node QuestionToken()
    {
        switch (this.Kind)
        {
            case Kind.Parameter:
            {
                return this.AsParameterDeclaration().QuestionToken;
            }

            case Kind.ConditionalExpression:
            {
                return this.AsConditionalExpression().QuestionToken;
            }

            case Kind.MappedType:
            {
                return this.AsMappedTypeNode().QuestionToken;
            }

            case Kind.NamedTupleMember:
            {
                return this.AsNamedTupleMember().QuestionToken;
            }
        }

        var postfix = this.PostfixToken();
        if (((postfix != null) && (postfix.Kind == Kind.QuestionToken)))
        {
            return postfix;
        }

        return null;
    }

    public Node QuestionDotToken()
    {
        switch (this.Kind)
        {
            case Kind.ElementAccessExpression:
            {
                return this.AsElementAccessExpression().QuestionDotToken;
            }

            case Kind.PropertyAccessExpression:
            {
                return this.AsPropertyAccessExpression().QuestionDotToken;
            }

            case Kind.CallExpression:
            {
                return this.AsCallExpression().QuestionDotToken;
            }

            case Kind.TaggedTemplateExpression:
            {
                return this.AsTaggedTemplateExpression().QuestionDotToken;
            }
        }

        throw new InvalidOperationException(("Unhandled case in Node.QuestionDotToken: " + this.Kind.ToString()));
    }

    public Node TypeExpression()
    {
        switch (this.Kind)
        {
            case Kind.JSDocParameterTag:
            case Kind.JSDocPropertyTag:
            {
                return this.AsJSDocParameterOrPropertyTag().TypeExpression;
            }

            case Kind.JSDocReturnTag:
            {
                return this.AsJSDocReturnTag().TypeExpression;
            }

            case Kind.JSDocTypeTag:
            {
                return this.AsJSDocTypeTag().TypeExpression;
            }

            case Kind.JSDocTypedefTag:
            {
                return this.AsJSDocTypedefTag().TypeExpression;
            }

            case Kind.JSDocCallbackTag:
            {
                return this.AsJSDocCallbackTag().TypeExpression;
            }

            case Kind.JSDocSatisfiesTag:
            {
                return this.AsJSDocSatisfiesTag().TypeExpression;
            }

            case Kind.JSDocThrowsTag:
            {
                return this.AsJSDocThrowsTag().TypeExpression;
            }
        }

        throw new InvalidOperationException(("Unhandled case in Node.TypeExpression: " + this.Kind.ToString()));
    }

    public Node ClassName()
    {
        switch (this.Kind)
        {
            case Kind.JSDocAugmentsTag:
            {
                return this.AsJSDocAugmentsTag().ClassName;
            }

            case Kind.JSDocImplementsTag:
            {
                return this.AsJSDocImplementsTag().ClassName;
            }
        }

        throw new InvalidOperationException(("Unhandled case in Node.ClassName: " + this.Kind.ToString()));
    }
} // END GENERATED AST ACCESSORS

// Additional syntax access/declaration helpers from ast.go. Symbols are read-only existing state.
public enum AccessKind : int
{
    Read = 0,
    Write = 1,
    ReadWrite = 2
}

public static partial class Ast
{
    // Upstream upstream/TypeScript/tsc/internal/ast/ast.go:1268:1.
    public static bool IsWriteOnlyAccess(Node node)
    {
        return (accessKind(node) == AccessKind.Write);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/ast.go:1272:1.
    public static bool IsWriteAccess(Node node)
    {
        return (accessKind(node) != AccessKind.Read);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/ast.go:1276:1.
    public static bool IsWriteAccessForReference(Node node)
    {
        var decl = GetDeclarationFromName(node);
        return (((((decl != null) && declarationIsWriteAccess(decl))) || (node.Kind == Kind.DefaultKeyword)) || IsWriteAccess(node));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/ast.go:1281:1.
    public static Node GetDeclarationFromName(Node name)
    {
        if (((name == null) || (name.Parent == null)))
        {
            return null;
        }

        var parent = name.Parent;
        switch (name.Kind)
        {
            case Kind.StringLiteral:
            case Kind.NoSubstitutionTemplateLiteral:
            case Kind.NumericLiteral:
            {
                if (IsComputedPropertyName(parent))
                {
                    return parent.Parent;
                }

                goto case Kind.Identifier;
            }

            case Kind.Identifier:
            {
                if (IsDeclaration(parent))
                {
                    if ((parent.Name() == name))
                    {
                        return parent;
                    }

                    return null;
                }

                if (IsQualifiedName(parent))
                {
                    var tag = parent.Parent;
                    if ((IsJSDocParameterTag(tag) && (tag.Name() == parent)))
                    {
                        return tag;
                    }

                    return null;
                }

                var binExp = parent.Parent;
                if ((IsBinaryExpression(binExp) && (GetAssignmentDeclarationKind(binExp) != JSDeclarationKind.None)))
                {
                    var leftHasSymbol = false;
                    if (((binExp.AsBinaryExpression().Left != null) && (binExp.AsBinaryExpression().Left.Symbol() != null)))
                    {
                        leftHasSymbol = true;
                    }

                    if ((leftHasSymbol || (binExp.Symbol() != null)))
                    {
                        if ((GetNameOfDeclaration(binExp.AsNode()) == name))
                        {
                            return binExp.AsNode();
                        }
                    }
                }

                break;
            }

            case Kind.PrivateIdentifier:
            {
                if ((IsDeclaration(parent) && (parent.Name() == name)))
                {
                    return parent;
                }

                break;
            }
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/ast.go:1327:1.
    private static bool declarationIsWriteAccess(Node decl)
    {
        if ((decl == null))
        {
            return false;
        }

        if (((decl.Flags & NodeFlags.Ambient) != 0))
        {
            return true;
        }

        switch (decl.Kind)
        {
            case Kind.BinaryExpression:
            case Kind.BindingElement:
            case Kind.ClassDeclaration:
            case Kind.ClassExpression:
            case Kind.DefaultKeyword:
            case Kind.EnumDeclaration:
            case Kind.EnumMember:
            case Kind.ExportSpecifier:
            case Kind.ImportClause:
            case Kind.ImportEqualsDeclaration:
            case Kind.ImportSpecifier:
            case Kind.InterfaceDeclaration:
            case Kind.JSDocCallbackTag:
            case Kind.JSDocTypedefTag:
            case Kind.JsxAttribute:
            case Kind.ModuleDeclaration:
            case Kind.NamespaceExportDeclaration:
            case Kind.NamespaceImport:
            case Kind.NamespaceExport:
            case Kind.Parameter:
            case Kind.ShorthandPropertyAssignment:
            case Kind.TypeAliasDeclaration:
            case Kind.JSTypeAliasDeclaration:
            case Kind.TypeParameter:
            {
                return true;
            }

            case Kind.PropertyAssignment:
            {
                return !IsArrayLiteralOrObjectLiteralDestructuringPattern(decl.Parent);
            }

            case Kind.FunctionDeclaration:
            case Kind.FunctionExpression:
            case Kind.Constructor:
            case Kind.MethodDeclaration:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            {
                switch (decl.Kind)
                {
                    case Kind.FunctionDeclaration:
                    {
                        return (decl.AsFunctionDeclaration().Body != null);
                    }

                    case Kind.FunctionExpression:
                    {
                        return (decl.AsFunctionExpression().Body != null);
                    }

                    case Kind.Constructor:
                    {
                        return (decl.AsConstructorDeclaration().Body != null);
                    }

                    case Kind.MethodDeclaration:
                    {
                        return (decl.AsMethodDeclaration().Body != null);
                    }

                    case Kind.GetAccessor:
                    {
                        return (decl.AsGetAccessorDeclaration().Body != null);
                    }

                    case Kind.SetAccessor:
                    {
                        return (decl.AsSetAccessorDeclaration().Body != null);
                    }
                }

                return false;
            }

            case Kind.VariableDeclaration:
            case Kind.PropertyDeclaration:
            {
                bool hasInit = default;
                switch (decl.Kind)
                {
                    case Kind.VariableDeclaration:
                    {
                        hasInit = (decl.AsVariableDeclaration().Initializer != null);
                        break;
                    }

                    case Kind.PropertyDeclaration:
                    {
                        hasInit = (decl.AsPropertyDeclaration().Initializer != null);
                        break;
                    }
                }

                return (hasInit || IsCatchClause(decl.Parent));
            }

            case Kind.MethodSignature:
            case Kind.PropertySignature:
            case Kind.JSDocPropertyTag:
            case Kind.JSDocParameterTag:
            {
                return false;
            }

            default:
            {
                throw new InvalidOperationException("Unhandled case in declarationIsWriteAccess");
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/ast.go:1406:1.
    public static bool IsArrayLiteralOrObjectLiteralDestructuringPattern(Node node)
    {
        if (!((IsArrayLiteralExpression(node) || IsObjectLiteralExpression(node))))
        {
            return false;
        }

        var parent = node.Parent;
        if (((IsBinaryExpression(parent) && (parent.AsBinaryExpression().Left == node)) && (parent.AsBinaryExpression().OperatorToken.Kind == Kind.EqualsToken)))
        {
            return true;
        }

        if ((IsForOfStatement(parent) && (parent.Initializer() == node)))
        {
            return true;
        }

        if (IsPropertyAssignment(parent))
        {
            return IsArrayLiteralOrObjectLiteralDestructuringPattern(parent.Parent);
        }

        return IsArrayLiteralOrObjectLiteralDestructuringPattern(parent);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/ast.go:1430:1.
    private static AccessKind accessKind(Node node)
    {
        var parent = node.Parent;
        if ((parent == null))
        {
            return AccessKind.Read;
        }

        switch (parent.Kind)
        {
            case Kind.ParenthesizedExpression:
            {
                return accessKind(parent);
            }

            case Kind.PrefixUnaryExpression:
            {
                var @operator = parent.AsPrefixUnaryExpression().Operator;
                if (((@operator == Kind.PlusPlusToken) || (@operator == Kind.MinusMinusToken)))
                {
                    return AccessKind.ReadWrite;
                }

                return AccessKind.Read;
            }

            case Kind.PostfixUnaryExpression:
            {
                var operator2 = parent.AsPostfixUnaryExpression().Operator;
                if (((operator2 == Kind.PlusPlusToken) || (operator2 == Kind.MinusMinusToken)))
                {
                    return AccessKind.ReadWrite;
                }

                return AccessKind.Read;
            }

            case Kind.BinaryExpression:
            {
                if ((parent.AsBinaryExpression().Left == node))
                {
                    var operator3 = parent.AsBinaryExpression().OperatorToken;
                    if (IsAssignmentOperator(operator3.Kind))
                    {
                        if ((operator3.Kind == Kind.EqualsToken))
                        {
                            return AccessKind.Write;
                        }

                        return AccessKind.ReadWrite;
                    }
                }

                return AccessKind.Read;
            }

            case Kind.PropertyAccessExpression:
            {
                if ((parent.AsPropertyAccessExpression().Name() != node))
                {
                    return AccessKind.Read;
                }

                return accessKind(parent);
            }

            case Kind.PropertyAssignment:
            {
                var parentAccess = accessKind(parent.Parent);
                if ((node == parent.AsPropertyAssignment().Name()))
                {
                    return reverseAccessKind(parentAccess);
                }

                return parentAccess;
            }

            case Kind.ShorthandPropertyAssignment:
            {
                if ((node == parent.AsShorthandPropertyAssignment().ObjectAssignmentInitializer))
                {
                    return AccessKind.Read;
                }

                return accessKind(parent.Parent);
            }

            case Kind.ArrayLiteralExpression:
            {
                return accessKind(parent);
            }

            case Kind.ForInStatement:
            case Kind.ForOfStatement:
            {
                if ((node == parent.AsForInOrOfStatement().Initializer))
                {
                    return AccessKind.Write;
                }

                return AccessKind.Read;
            }

            default:
            {
                return AccessKind.Read;
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/ast.go:1491:1.
    private static AccessKind reverseAccessKind(AccessKind a)
    {
        switch (a)
        {
            case AccessKind.Read:
            {
                return AccessKind.Write;
            }

            case AccessKind.Write:
            {
                return AccessKind.Read;
            }

            case AccessKind.ReadWrite:
            {
                return AccessKind.ReadWrite;
            }
        }

        throw new InvalidOperationException("Unhandled case in reverseAccessKind");
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/ast.go:1832:1.
    public static bool IsTypeOrJSTypeAliasDeclaration(Node node)
    {
        return ((node.Kind == Kind.TypeAliasDeclaration) || (node.Kind == Kind.JSTypeAliasDeclaration));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/ast.go:1878:1.
    public static bool IsImportDeclarationOrJSImportDeclaration(Node node)
    {
        return ((node.Kind == Kind.ImportDeclaration) || (node.Kind == Kind.JSImportDeclaration));
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/ast.go:1904:1.
    public static bool IsAnyExportAssignment(Node node)
    {
        return (node.Kind == Kind.ExportAssignment);
    }

    // Upstream upstream/TypeScript/tsc/internal/ast/ast.go:3089:1.
    public static string GetDeclarationName(Node declaration) => GetDeclarationNameValue(declaration).Materialize();
    internal static ByteStringValue GetDeclarationNameValue(Node declaration)
    {
        var name = GetNonAssignedNameOfDeclaration(declaration);
        if ((name != null))
        {
            if (IsComputedPropertyName(name))
            {
                if (IsStringOrNumericLiteralLike(name.Expression()))
                {
                    return name.Expression().TextValue();
                }

                if (IsPropertyAccessExpression(name.Expression()))
                {
                    return name.Expression().Name().TextValue();
                }
            }
            else
            {
                if (IsPropertyName(name))
                {
                    return name.TextValue();
                }
            }
        }

        return default;
    }
}

public sealed partial class SourceFile
{
    private readonly object identifiersLock = new(), nameTableLock = new(), tokenCacheLock = new(), declarationMapLock = new();
    private bool identifiersInitialized, nameTableInitialized;
    private HashSet<ByteStringKey> identifiers;
    private bool hasNullIdentifier;
    private Dictionary<ByteStringKey, int> nameTable;
    private Dictionary<(Node Parent, TextRange Loc), Node> tokenCache;
    private NodeFactory tokenFactory;
    private Dictionary<ByteStringKey, Slice<Node>> declarationMap;
    /// <summary>Checks a raw UTF-8 byte-string name, as returned by Node.Text().</summary>
    public bool HasIdentifier(string name) => HasIdentifier(ByteStringValue.FromString(name));
    internal bool HasIdentifier(ByteStringValue name)
    {
        lock (identifiersLock)
        {
            if (!identifiersInitialized)
            {
                try
                {
                    identifiers = Ast.CollectIdentifiersForSourceFile(this, out hasNullIdentifier);
                }
                finally
                {
                    identifiersInitialized = true;
                }
            }

            return name.IsNull ? hasNullIdentifier : identifiers is not null && name.ContainsIn(identifiers);
        }
    }

    public Dictionary<ByteStringKey, int> GetNameTable()
    {
        lock (nameTableLock)
        {
            if (!nameTableInitialized)
            {
                try
                {
                    var table = new Dictionary<ByteStringKey, int>(IdentifierCount, ByteStringKey.Comparer);
                    bool Walk(Node n)
                    {
                        if (Ast.IsIdentifier(n) && !Ast.IsTagName(n) && !n.TextEquals(""u8) || Ast.IsStringOrNumericLiteralLike(n) && Ast.literalIsName(n) || Ast.IsPrivateIdentifier(n))
                        {
                            var value = ByteStringKey.FromValue(n.TextValue());
                            table[value] = table.ContainsKey(value) ? -1 : n.Pos();
                        }

                        n.ForEachChild(Walk);
                        foreach (var doc in n.JSDoc(this))
                            doc.ForEachChild(Walk);
                        return false;
                    }

                    ForEachChild(Walk);
                    nameTable = table;
                }
                finally
                {
                    nameTableInitialized = true;
                }
            }

            return nameTable;
        }
    }

    public bool IsJS() => Ast.IsSourceFileJS(this);
    public Node GetOrCreateToken(Kind kind, int pos, int end, Node parent, TokenFlags flags)
    {
        lock (tokenCacheLock)
        {
            var loc = new TextRange(pos, end);
            if (tokenCache is not null && tokenCache.TryGetValue((parent, loc), out var cached))
            {
                if (cached.Kind != kind)
                    throw new InvalidOperationException($"Token cache mismatch: {cached.Kind} != {kind}");
                return cached;
            }

            if ((parent.Flags & NodeFlags.Reparsed) != 0)
                throw new InvalidOperationException($"Cannot create token from reparsed node of kind {parent.Kind}");
            tokenCache ??= new();
            var token = createToken(kind, pos, end, flags);
            token.Loc = loc;
            token.Parent = parent;
            tokenCache[(parent, loc)] = token;
            return token;
        }
    }

    private Node createToken(Kind kind, int pos, int end, TokenFlags flags)
    {
        tokenFactory ??= new();
        var value = ByteStringValue.FromView(sourceText.Slice(pos, end));
        return kind switch
        {
            Kind.NumericLiteral => tokenFactory.NewNumericLiteralWithTextValue(value, flags),
            Kind.BigIntLiteral => tokenFactory.NewBigIntLiteralWithTextValue(value, flags),
            Kind.StringLiteral => tokenFactory.NewStringLiteralWithTextValue(value, flags),
            Kind.JsxText or Kind.JsxTextAllWhiteSpaces => tokenFactory.NewJsxTextWithTextValue(value, kind == Kind.JsxTextAllWhiteSpaces),
            Kind.RegularExpressionLiteral => tokenFactory.NewRegularExpressionLiteralWithTextValue(value, flags),
            Kind.NoSubstitutionTemplateLiteral => tokenFactory.NewNoSubstitutionTemplateLiteralWithTextValue(value, flags),
            Kind.TemplateHead => tokenFactory.NewTemplateHeadWithTextValue(value, default, flags),
            Kind.TemplateMiddle => tokenFactory.NewTemplateMiddleWithTextValue(value, default, flags),
            Kind.TemplateTail => tokenFactory.NewTemplateTailWithTextValue(value, default, flags),
            Kind.Identifier => tokenFactory.NewIdentifierWithTextValue(value),
            Kind.PrivateIdentifier => tokenFactory.NewPrivateIdentifierWithTextValue(value),
            _ => tokenFactory.NewToken(kind)};
    }
}

public static partial class Ast
{
    internal static HashSet<ByteStringKey> CollectIdentifiersForSourceFile(SourceFile sourceFile, out bool hasNullIdentifier)
    {
        var identifiers = new HashSet<ByteStringKey>(ByteStringKey.Comparer);
        bool containsNull = false;
        bool Collect(Node node)
        {
            switch (node.Kind)
            {
                case Kind.Identifier:
                case Kind.PrivateIdentifier:
                case Kind.StringLiteral:
                case Kind.NumericLiteral:
                case Kind.BigIntLiteral:
                case Kind.NoSubstitutionTemplateLiteral:
                    var text = node.TextValue();
                    if (text.IsNull) containsNull = true;
                    else identifiers.Add(ByteStringKey.FromValue(text));
                    break;
            }

            node.ForEachChild(Collect);
            return false;
        }

        Collect(sourceFile.AsNode());
        hasNullIdentifier = containsNull;
        return identifiers;
    }
}

public sealed partial class NodeFactory
{
    public CommentRange NewCommentRange(Kind kind, int pos, int end, bool hasTrailingNewLine) => new(kind, pos, end, hasTrailingNewLine);
}

[Flags]
public enum PragmaKindFlags : byte
{
    None = 0,
    TripleSlashXML = 1,
    SingleLine = 2,
    MultiLine = 4,
    All = TripleSlashXML | SingleLine | MultiLine,
    Default = All
}

public sealed class PragmaArgumentSpecification
{
    public string Name;
    public bool Optional, CaptureSpan;
}

public sealed class PragmaSpecification
{
    public List<PragmaArgumentSpecification> Args;
    public PragmaKindFlags Kind;
    public bool IsTripleSlash() => (Kind & PragmaKindFlags.TripleSlashXML) > 0;
}

public sealed partial class SourceFile
{
    /// <summary>Builds the declaration-name index. Overload grouping additionally observes existing symbol identity.</summary>
    public Dictionary<ByteStringKey, Slice<Node>> GetDeclarationMap()
    {
        lock (declarationMapLock)
            return declarationMap ??= computeDeclarationMap();
    }

    private Dictionary<ByteStringKey, Slice<Node>> computeDeclarationMap()
    {
        var result = new Dictionary<ByteStringKey, Slice<Node>>(ByteStringKey.Comparer);
        void AddDeclaration(Node declaration)
        {
            var name = Ast.GetDeclarationNameValue(declaration);
            if (!name.EqualsBytes(""u8))
            {
                name.TryGetValue(result, out var declarations);
                result[ByteStringKey.FromValue(name)] = declarations.Append(declaration);
            }
        }

        bool Visit(Node node)
        {
            switch (node.Kind)
            {
                case Kind.FunctionDeclaration:
                case Kind.FunctionExpression:
                case Kind.MethodDeclaration:
                case Kind.MethodSignature:
                    var declarationName = Ast.GetDeclarationNameValue(node);
                    if (!declarationName.EqualsBytes(""u8))
                    {
                        declarationName.TryGetValue(result, out var declarations);
                        var lastDeclaration = declarations.Length > 0 ? declarations[declarations.Length - 1] : null;
                        if (lastDeclaration is not null && node.Parent == lastDeclaration.Parent && node.Symbol() == lastDeclaration.Symbol())
                        {
                            if (node.Body()is not null && lastDeclaration.Body()is null)
                                declarations[declarations.Length - 1] = node;
                        }
                        else
                        {
                            result[ByteStringKey.FromValue(declarationName)] = declarations.Append(node);
                        }
                    }

                    node.ForEachChild(Visit);
                    break;
                case Kind.ClassDeclaration:
                case Kind.ClassExpression:
                case Kind.InterfaceDeclaration:
                case Kind.TypeAliasDeclaration:
                case Kind.EnumDeclaration:
                case Kind.ModuleDeclaration:
                case Kind.ImportEqualsDeclaration:
                case Kind.ImportClause:
                case Kind.NamespaceImport:
                case Kind.GetAccessor:
                case Kind.SetAccessor:
                case Kind.TypeLiteral:
                    AddDeclaration(node);
                    node.ForEachChild(Visit);
                    break;
                case Kind.ImportSpecifier:
                case Kind.ExportSpecifier:
                    if (node.PropertyName()is not null)
                        AddDeclaration(node);
                    break;
                case Kind.Parameter:
                    if (!Ast.HasSyntacticModifier(node, TypeScript.Scanner.ModifierFlags.ParameterPropertyModifier))
                        break;
                    goto case Kind.VariableDeclaration;
                case Kind.VariableDeclaration:
                case Kind.BindingElement:
                    var name = node.Name();
                    if (name is not null)
                    {
                        if (Ast.IsBindingPattern(name))
                            node.Name().ForEachChild(Visit);
                        else
                        {
                            if (node.Initializer()is not null)
                                Visit(node.Initializer());
                            AddDeclaration(node);
                        }
                    }

                    break;
                case Kind.EnumMember:
                case Kind.PropertyDeclaration:
                case Kind.PropertySignature:
                    AddDeclaration(node);
                    break;
                case Kind.ExportDeclaration:
                    var exportClause = node.AsExportDeclaration().ExportClause;
                    if (exportClause is not null)
                    {
                        if (Ast.IsNamedExports(exportClause))
                            foreach (var element in exportClause.Elements())
                                Visit(element);
                        else
                            Visit(exportClause.AsNamespaceExport().Name());
                    }

                    break;
                case Kind.ImportDeclaration:
                    var importClause = node.AsImportDeclaration().ImportClause;
                    if (importClause is not null)
                    {
                        if (importClause.Name()is not null)
                            AddDeclaration(importClause.Name());
                        var namedBindings = importClause.AsImportClause().NamedBindings;
                        if (namedBindings is not null)
                        {
                            if (namedBindings.Kind == Kind.NamespaceImport)
                                AddDeclaration(namedBindings);
                            else
                                foreach (var element in namedBindings.Elements())
                                    Visit(element);
                        }
                    }

                    break;
                case Kind.BinaryExpression:
                    switch (Ast.GetAssignmentDeclarationKind(node))
                    {
                        case JSDeclarationKind.ExportsProperty:
                        case JSDeclarationKind.ThisProperty:
                        case JSDeclarationKind.Property:
                            AddDeclaration(node);
                            break;
                    }

                    node.ForEachChild(Visit);
                    break;
                default:
                    node.ForEachChild(Visit);
                    break;
            }

            return false;
        }

        ForEachChild(Visit);
        return result;
    }
}

public static partial class Ast
{
    public static (ResolutionMode Mode, bool Found) GetResolutionModeOverride(this Node node, Func<Node, Message, object[], bool> grammarErrorOnNode = null)
    {
        if (node is null)
            return (ResolutionMode.None, false);
        var attributes = node.AsImportAttributes().Attributes;
        Node attribute = null;
        foreach (var candidate in attributes.Nodes)
            if (candidate.Name().TextValue().EqualsBytes("resolution-mode"u8))
            {
                attribute = candidate;
                break;
            }

        if (attribute is null)
            return (ResolutionMode.None, false);
        var elem = attribute.AsImportAttribute();
        if (!IsStringLiteralLike(elem.Value))
            return (ResolutionMode.None, false);
        if (!elem.Value.TextValue().EqualsBytes("import"u8) && !elem.Value.TextValue().EqualsBytes("require"u8))
        {
            grammarErrorOnNode?.Invoke(elem.Value, Diagnostics.X_resolution_mode_should_be_either_require_or_import, []);
            return (ResolutionMode.None, false);
        }

        if (elem.Value.TextValue().EqualsBytes("import"u8))
            return (ResolutionMode.ESNext, true);
        return (ResolutionMode.CommonJS, true);
    }
}

// BEGIN GENERATED SYNTAX ITERATORS
// Generated by tools/generate-syntax-iterators.py from ast.go.
public partial class SourceFile
{
    public override IEnumerable<Node> IterChildren()
    {
        foreach (var child in Ast.IterateSyntaxNodes(this.Statements?.Nodes ?? default))
            yield return child;
        if (this.EndOfFileToken is not null)
            yield return this.EndOfFileToken;
    }
}

// END GENERATED SYNTAX ITERATORS
// Stage-neutral lazy source-file attachment storage from ast.go.
public sealed class SourceFileDataKey<T>
{
    internal readonly ulong key;
    public SourceFileDataKey()
    {
    }

    internal SourceFileDataKey(ulong key)
    {
        this.key = key;
    }
}

internal sealed class SourceFileDataCell<T>
{
    private readonly object gate = new();
    private bool computing, completed;
    private T value;
    internal T GetOrCompute(SourceFile file, Func<SourceFile, T> compute)
    {
        lock (gate)
        {
            while (computing)
                Monitor.Wait(gate);
            if (completed)
                return value;
            computing = true;
        }

        try
        {
            value = compute(file);
            return value;
        }
        finally
        {
            // sync.Once marks the call complete even when its function panics.
            lock (gate)
            {
                completed = true;
                computing = false;
                Monitor.PulseAll(gate);
            }
        }
    }
}

public static partial class Ast
{
    private static long sourceFileDataKeyCounter;
    public static SourceFileDataKey<T> NewSourceFileDataKey<T>() => new(unchecked((ulong)Interlocked.Increment(ref sourceFileDataKeyCounter)));
}

public partial class SourceFile
{
    private readonly object dataMu = new();
    private Dictionary<ulong, object> sourceFileData;
    public T GetOrComputeData<T>(SourceFileDataKey<T> key, Func<SourceFile, T> compute)
    {
        var cell = getDataCell(key);
        return cell.GetOrCompute(this, compute);
    }

    private SourceFileDataCell<T> getDataCell<T>(SourceFileDataKey<T> key)
    {
        if (key is null || key.key == 0)
            throw new InvalidOperationException("invalid SourceFileDataKey; use NewSourceFileDataKey");
        lock (dataMu)
        {
            sourceFileData ??= new();
            if (sourceFileData.TryGetValue(key.key, out var cell))
                return (SourceFileDataCell<T>)cell;
            var created = new SourceFileDataCell<T>();
            sourceFileData.Add(key.key, created);
            return created;
        }
    }
}

// utilities.go:2962. Per-file directive takes precedence over the option.
public static partial class Ast
{
    public static bool IsCheckJSEnabledForFile(SourceFile sourceFile, CompilerOptions options) =>
        sourceFile.CheckJsDirective is { } directive ? directive.Enabled : options.CheckJs == Tristate.True;
}
