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
    public TextRange Loc;
    public List<Node> Nodes;
    public bool IsMissing;
    public int Pos() => Loc.Pos();
    public int End() => Loc.End();
    public bool HasTrailingComma() => Nodes is { Count: > 0 } && Nodes[^1].End() < End();
    public NodeList Clone(NodeFactoryCoercible f)
    {
        var list = f.AsNodeFactory().NewNodeList(Nodes);
        list.Loc = Loc;
        list.IsMissing = IsMissing;
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
        ModifierFlags = ModifierFlags,
        IsMissing = IsMissing
    };
}

public sealed partial class Node
{
    public Kind Kind;
    public NodeFlags Flags;
    public TextRange Loc;
    public Node Parent;
    internal NodeData data;
    public Node AsNode() => this;
    public Node AsMutable() => this;
    public int Pos() => Loc.Pos();
    public int End() => Loc.End();
    public short KindValue() => (short)Kind;
    public string KindString() => Kind.String();
    internal uint JsxFacts() => data.JsxFacts();
    public Node Name() => data.Name();
    public ModifierList Modifiers() => data.Modifiers();
    public void SetModifiers(ModifierList modifiers) => data.SetModifiers(modifiers);
    public FunctionLikeBase FunctionLikeData() => data as FunctionLikeBase;
    public ClassLikeBase ClassLikeData() => data as ClassLikeBase;
    public BodyBase BodyData() => data as BodyBase;
    public LiteralLikeNodeBase LiteralLikeData() => data as LiteralLikeNodeBase;
    public TemplateLiteralLikeNodeBase TemplateLiteralLikeData() => data as TemplateLiteralLikeNodeBase;
    public NodeList ParameterList() => FunctionLikeData()?.Parameters;
    public List<Node> Parameters() => ParameterList()?.Nodes;
    public Node Body() => BodyData()?.Body;
    public ModifierFlags ModifierFlags() => Modifiers()?.ModifierFlags ?? TypeScript.Scanner.ModifierFlags.None;
    public List<Node> ModifierNodes() => Modifiers()?.Nodes;
    public List<Node> Arguments() => ArgumentList()?.Nodes;
    public List<Node> TypeArguments() => TypeArgumentList()?.Nodes;
    public List<Node> TypeParameters() => TypeParameterList()?.Nodes;
    public List<Node> Members() => MemberList()?.Nodes;
    public List<Node> Statements() => StatementList()?.Nodes;
    public List<Node> Comments() => CommentList()?.Nodes;
    public List<Node> Properties() => PropertyList()?.Nodes;
    public List<Node> Elements() => ElementList()?.Nodes;
    public bool ForEachChild(Visitor visitor) => data.ForEachChild(visitor);
    public string DataType() => data.DataType();
    public IEnumerable<SyntaxField> SyntaxFields() => data.SyntaxFields();
    public IEnumerable<Node> IterChildren()
    {
        var nodes = new List<Node>();
        ForEachChild(n =>
        {
            nodes.Add(n);
            return false;
        });
        return nodes;
    }

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
        Kind.MetaProperty => AsMetaProperty().Name().Text(),
        Kind.JsxNamespacedName => AsJsxNamespacedName().Namespace.Text() + ":" + Name().Text(),
        Kind.JSDocText => string.Concat(AsJSDocText().text ?? []),
        Kind.JSDocLink => string.Concat(AsJSDocLink().text ?? []),
        Kind.JSDocLinkCode => string.Concat(AsJSDocLinkCode().text ?? []),
        Kind.JSDocLinkPlain => string.Concat(AsJSDocLinkPlain().text ?? []),
        _ when data is LiteralLikeNodeBase literal => literal.Text,
        _ => throw new InvalidOperationException("Unhandled Node.Text kind " + Kind),
    };
    public byte[] TextBytes() => GoString.Bytes(Text());
    public string DecodedText() => ScannerString.ToJsString(Text());
    public bool IsJSDoc() => Kind == Kind.JSDoc;
    public List<Node> JSDoc(SourceFile file) => (Flags & NodeFlags.HasJSDoc) == 0 ? null : (file ?? Ast.GetSourceFileOfNode(this))?.JSDoc(this);
    public List<Node> EagerJSDoc(SourceFile file) => (Flags & NodeFlags.HasJSDoc) == 0 ? null : (file ?? Ast.GetSourceFileOfNode(this))?.EagerJSDoc(this);
    public Node Clone(NodeFactoryCoercible factory) => data.Clone(factory);
    public Node VisitEachChild(NodeVisitor visitor) => data.VisitEachChild(visitor);
}

public abstract partial class NodeData : INodeData
{
    private readonly Node node;
    protected NodeData()
    {
        node = new Node
        {
            data = this
        };
    }

    public Node AsNode() => node;
    public Kind Kind { get => node.Kind; set => node.Kind = value; }
    public NodeFlags Flags { get => node.Flags; set => node.Flags = value; }
    public TextRange Loc { get => node.Loc; set => node.Loc = value; }
    public Node Parent { get => node.Parent; set => node.Parent = value; }

    public Node AsMutable() => node;
    public ParameterDeclaration AsParameterDeclaration() => node.AsParameterDeclaration();
    public ClassLikeBase ClassLikeData() => this as ClassLikeBase;
    public FunctionLikeBase FunctionLikeData() => this as FunctionLikeBase;
    public ModifierFlags ModifierFlags() => Modifiers()?.ModifierFlags ?? TypeScript.Scanner.ModifierFlags.None;
    public int Pos() => node.Pos();
    public int End() => node.End();
    internal virtual uint ComputeJsxFacts() => 0;
    internal virtual uint PropagateJsxFacts() => JsxFacts();
    private int cachedJsxFacts = -1;
    internal virtual bool CacheJsxFacts => false;

    internal uint JsxFacts()
    {
        if (!CacheJsxFacts)
            return ComputeJsxFacts();
        var cached = System.Threading.Volatile.Read(ref cachedJsxFacts);
        if (cached >= 0)
            return (uint)cached;
        var computed = ComputeJsxFacts();
        System.Threading.Volatile.Write(ref cachedJsxFacts, (int)computed);
        return computed;
    }

    public virtual Node Name() => null;
    public virtual ModifierList Modifiers() => null;
    public virtual void SetModifiers(ModifierList value)
    {
    }

    public virtual bool ForEachChild(Visitor visitor) => false;
    public virtual Node Clone(NodeFactoryCoercible factory) => null;
    public virtual Node VisitEachChild(NodeVisitor visitor) => AsNode();
    public abstract string DataType();
    public abstract IEnumerable<SyntaxField> SyntaxFields();
    public virtual IEnumerable<SyntaxField> BoundFields()
    {
        yield break;
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
    internal Node newNode(Kind kind, NodeData data)
    {
        nodeCount++;
        var n = data.AsNode();
        n.Loc = new(-1, -1);
        n.Kind = kind;
        hooks.OnCreate?.Invoke(n);
        return n;
    }

    public NodeList NewNodeList(List<Node> nodes) => new()
    {
        Loc = new(-1, -1),
        Nodes = nodes
    };
    public ModifierList NewModifierList(List<Node> nodes) => new()
    {
        Loc = new(-1, -1),
        Nodes = nodes,
        ModifierFlags = Ast.ModifiersToFlags(nodes)
    };
    public Node NewModifier(Kind kind) => NewToken(kind);
    public Node NewSourceFile(SourceFileParseOptions opts, string text, NodeList statements, Node endOfFileToken) => newNode(Kind.SourceFile, new SourceFile { parseOptions = opts, text = text, Statements = statements, EndOfFileToken = endOfFileToken });
}

public static partial class Ast
{
    internal static bool SameSyntaxSlice<T>(List<T> left, List<T> right) => (left?.Count ?? 0) == (right?.Count ?? 0) && ((left?.Count ?? 0) == 0 || ReferenceEquals(left, right));
    internal static bool ForEachChildJSDocParameterOrPropertyTag(JSDocParameterOrPropertyTag node, Visitor v) => Visit(v, node.TagName) || (node.IsNameFirst && (Visit(v, node.name) || Visit(v, node.TypeExpression))) || (!node.IsNameFirst && (Visit(v, node.TypeExpression) || Visit(v, node.name))) || VisitNodeList(v, node.Comment);
    internal static Node VisitEachChildJSDocParameterOrPropertyTag(JSDocParameterOrPropertyTag node, NodeVisitor v) => v.Factory.UpdateJSDocParameterOrPropertyTag(node, v.visitNode(node.TagName), v.visitNode(node.name), node.IsBracketed, v.visitNode(node.TypeExpression), node.IsNameFirst, v.visitNodes(node.Comment));
    internal static List<Node> SameMapNodes(List<Node> nodes, Func<Node, Node> map)
    {
        if (nodes is null)
            return null;
        List<Node> result = null;
        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            var mapped = map(node);
            if (result is null && mapped != node)
                result = nodes.GetRange(0, i);
            result?.Add(mapped);
        }

        return result ?? nodes;
    }

    public static void SetParentInChildren(Node node)
    {
        Node parent = null;
        Visitor visitor = null;
        visitor = n =>
        {
            if (parent is not null)
                n.Parent = parent;
            var saveParent = parent;
            parent = n;
            n.ForEachChild(visitor);
            parent = saveParent;
            return false;
        };
        visitor(node);
    }

    internal static Func<SourceFile, Node, List<Node>> ParseJSDocForNode;
    public static void SetParseJSDocForNode(Func<SourceFile, Node, List<Node>> parser) => ParseJSDocForNode = parser;
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
            foreach (var node in nodes)
                if (v(node))
                    return true;
        return false;
    }

    internal static bool VisitNodeList(Visitor v, NodeList nodes) => VisitNodes(v, nodes?.Nodes);
    internal static bool VisitModifiers(Visitor v, ModifierList nodes) => VisitNodes(v, nodes?.Nodes);
}

public sealed class CheckJsDirective
{
    public bool Enabled;
    public CommentRange Range;
}

public sealed class FileReference
{
    public TextRange TextRange;
    public string FileName;
    public ResolutionMode ResolutionMode;
    public bool Preserve;
    public int Pos() => TextRange.Pos();
    public int End() => TextRange.End();
}

public sealed class PragmaArgument
{
    public TextRange TextRange;
    public string Name, Value;
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

    public string Name;
    public Dictionary<string, PragmaArgument> Args;
}

public sealed partial class SourceFile : NodeData
{
    internal SourceFileParseOptions parseOptions;
    internal string text = "";
    public NodeList Statements;
    public Node EndOfFileToken;
    public LanguageVariant LanguageVariant;
    public ScriptKind ScriptKind;
    public bool IsDeclarationFile;
    public Tristate UsesUriStyleNodeCoreModules;
    public int IdentifierCount, NodeCount, TextCount;
    public List<Node> ModuleAugmentations, ReparsedClones;
    public List<string> AmbientModuleNames;
    public List<CommentDirective> CommentDirectives;
    public List<Pragma> Pragmas;
    public List<FileReference> ReferencedFiles, TypeReferenceDirectives, LibReferenceDirectives;
    public CheckJsDirective CheckJsDirective;
    public Node CommonJSModuleIndicator, ExternalModuleIndicator;
    private List<Diagnostic> diagnostics, jsDiagnostics, jsdocDiagnostics;
    internal List<Node> imports;
    private Dictionary<Node, List<Node>> jsdocCache;
    private bool hasLazyJSDoc;
    public SourceFileParseOptions ParseOptions() => parseOptions;
    public string FileName() => parseOptions.FileName;
    public string PathKey() => parseOptions.PathKey;
    public string Text() => text;
    public byte[] TextBytes() => GoString.Bytes(text);
    public string DecodedText() => GoString.ToString(text);
    public List<Node> Imports() => imports;
    public List<Diagnostic> Diagnostics() => diagnostics;
    public List<Diagnostic> JSDiagnostics() => jsDiagnostics;
    public List<Diagnostic> JSDocDiagnostics() => jsdocDiagnostics;
    public void SetDiagnostics(List<Diagnostic> value) => diagnostics = value;
    public void SetJSDiagnostics(List<Diagnostic> value) => jsDiagnostics = value;
    public void SetJSDocDiagnostics(List<Diagnostic> value) => jsdocDiagnostics = value;
    public void SetJSDocCache(Dictionary<Node, List<Node>> value) => jsdocCache = value;
    public void SetHasLazyJSDoc(bool value) => hasLazyJSDoc = value;
    internal List<Node> JSDoc(Node n)
    {
        if ((n.Flags & NodeFlags.HasJSDoc) == 0)
            return null;
        if (!hasLazyJSDoc)
            return jsdocCache is not null && jsdocCache.TryGetValue(n, out var eager) ? eager : null;
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

    internal List<Node> EagerJSDoc(Node n)
    {
        if ((n.Flags & NodeFlags.HasJSDoc) == 0)
            return null;
        if (!hasLazyJSDoc)
            return jsdocCache is not null && jsdocCache.TryGetValue(n, out var eager) ? eager : null;
        lock (this)
            return jsdocCache is not null && jsdocCache.TryGetValue(n, out var value) ? value : null;
    }

    public override bool ForEachChild(Visitor v) => Ast.VisitNodeList(v, Statements) || Ast.Visit(v, EndOfFileToken);
    internal override bool CacheJsxFacts => true;

    internal override uint ComputeJsxFacts() => Ast.JsxListFacts(Statements, Ast.JsxPropagate);
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
        var updated = f.AsNodeFactory().NewSourceFile(parseOptions, text, Statements, EndOfFileToken);
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
        var updated = NewSourceFile(node.parseOptions, node.text, statements, endOfFileToken).AsSourceFile();
        updated.copyFrom(node);
        return Ast.UpdateNode(updated.AsNode(), node.AsNode(), hooks);
    }
}

// Binder-owned state from ast.go. Interfaces retain exact upstream embedding tests.
public sealed partial class Node
{
    internal long id;
    public DeclarationBase DeclarationData() => data as DeclarationBase;
    public ExportableBase ExportableData() => data as ExportableBase;
    public LocalsContainerBase LocalsContainerData() => data as LocalsContainerBase;
    public FlowNodeBase FlowNodeData() => data as FlowNodeBase;
    public Symbol Symbol() => DeclarationData()?.Symbol;
    public Symbol LocalSymbol() => ExportableData()?.LocalSymbol;
    public SymbolTable Locals() => LocalsContainerData()?.Locals;
    public IEnumerable<SyntaxField> BoundFields() => data.BoundFields();
}

public abstract partial class NodeData
{
    public DeclarationBase DeclarationData() => this as DeclarationBase;
    public ExportableBase ExportableData() => this as ExportableBase;
    public LocalsContainerBase LocalsContainerData() => this as LocalsContainerBase;
    public FlowNodeBase FlowNodeData() => this as FlowNodeBase;
}

public sealed class PatternAmbientModule
{
    public Pattern Pattern;
    public Symbol Symbol;
}

public sealed partial class SourceFile : DeclarationBase, LocalsContainerBase
{
    public Symbol Symbol { get; set; }
    public SymbolTable Locals { get; set; }
    public Node NextContainer { get; set; }

    public int SymbolCount;
    public List<PatternAmbientModule> PatternAmbientModules;
    public SymbolTable GlobalExports;
    private List<Diagnostic> bindDiagnostics;
    private readonly System.Threading.SemaphoreSlim bindMutex = new(1, 1);
    private bool bindInvoked, isBound;
    private readonly object ecmaLineMapMutex = new();
    private List<int> ecmaLineMap;
    public List<Diagnostic> BindDiagnostics() => bindDiagnostics;
    public void SetBindDiagnostics(List<Diagnostic> diagnostics) => bindDiagnostics = diagnostics;
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

    public List<int> ECMALineMap()
    {
        lock (ecmaLineMapMutex)
            return ecmaLineMap ??= Core.ComputeECMALineStarts(Text());
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

// BEGIN GENERATED JSX PROJECTION (tools/generate-jsx-projection.go; upstream ast.go)
public sealed partial class Token
{
    internal override uint ComputeJsxFacts()
    {
        switch (this.Kind)
        {
            case Kind.UsingKeyword:
            {
                return JsxProjection.ContainsUsing;
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
                return JsxProjection.ContainsTypeScript;
            }

            case Kind.AccessorKeyword:
            {
                return JsxProjection.ContainsClassFields;
            }

            case Kind.AsyncKeyword:
            {
                return JsxProjection.ContainsAnyAwait;
            }

            case Kind.SuperKeyword:
            {
                return JsxProjection.ContainsLexicalSuper;
            }

            case Kind.ThisKeyword:
            {
                return JsxProjection.ContainsLexicalThis;
            }

            case Kind.AsteriskAsteriskToken:
            case Kind.AsteriskAsteriskEqualsToken:
            {
                return JsxProjection.ContainsExponentiationOperator;
            }

            case Kind.QuestionQuestionToken:
            {
                return JsxProjection.ContainsNullishCoalescing;
            }

            case Kind.QuestionDotToken:
            {
                return JsxProjection.ContainsOptionalChaining;
            }

            case Kind.QuestionQuestionEqualsToken:
            case Kind.BarBarEqualsToken:
            case Kind.AmpersandAmpersandEqualsToken:
            {
                return JsxProjection.ContainsLogicalAssignments;
            }
        }

        return JsxProjection.FactsNone;
    }
}

public sealed partial class Identifier
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsIdentifier;
    }
}

public sealed partial class PrivateIdentifier
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsClassFields;
    }
}

public sealed partial class Decorator
{
    internal override uint ComputeJsxFacts()
    {
        return ((Ast.JsxPropagate(this.Expression) | JsxProjection.ContainsTypeScript) | JsxProjection.ContainsDecorators);
    }
}

public sealed partial class ForInOrOfStatement
{
    internal override uint ComputeJsxFacts()
    {
        return (((Ast.JsxPropagate(this.Initializer) | Ast.JsxPropagate(this.Expression)) | Ast.JsxPropagate(this.Statement)) | Core.IfElse((this.AwaitModifier != null), JsxProjection.ContainsForAwaitOrAsyncGenerator, JsxProjection.FactsNone));
    }
}

public sealed partial class ReturnStatement
{
    internal override uint ComputeJsxFacts()
    {
        return (Ast.JsxPropagate(this.Expression) | JsxProjection.ContainsForAwaitOrAsyncGenerator);
    }
}

public sealed partial class CatchClause
{
    internal override uint ComputeJsxFacts()
    {
        var res = (Ast.JsxPropagate(this.VariableDeclaration) | Ast.JsxPropagate(this.Block));
        if ((this.VariableDeclaration == null))
        {
            res |= JsxProjection.ContainsMissingCatchClauseVariable;
        }

        return res;
    }
}

public sealed partial class CatchClause
{
    internal override uint PropagateJsxFacts()
    {
        return (this.JsxFacts() & ~JsxProjection.ExclusionsCatchClause);
    }
}

public sealed partial class VariableStatement
{
    internal override uint ComputeJsxFacts()
    {
        if (((this.modifiers != null) && ((this.modifiers.ModifierFlags & TypeScript.Scanner.ModifierFlags.Ambient) != 0)))
        {
            return JsxProjection.ContainsTypeScript;
        }
        else
        {
            return (Ast.JsxModifierFacts(this.modifiers) | Ast.JsxPropagate(this.DeclarationList));
        }
    }
}

public sealed partial class VariableDeclaration
{
    internal override uint ComputeJsxFacts()
    {
        return (((Ast.JsxPropagate(this.name) | Ast.JsxEraseableFacts(this.ExclamationToken)) | Ast.JsxEraseableFacts(this.Type)) | Ast.JsxPropagate(this.Initializer));
    }
}

public sealed partial class VariableDeclarationList
{
    internal override uint ComputeJsxFacts()
    {
        return (Ast.JsxListFacts(this.Declarations, Ast.JsxPropagate) | Core.IfElse(((this.Flags & TypeScript.Scanner.NodeFlags.Using) != 0), JsxProjection.ContainsUsing, JsxProjection.FactsNone));
    }
}

public sealed partial class VariableDeclarationList
{
    internal override uint PropagateJsxFacts()
    {
        return (this.JsxFacts() & ~JsxProjection.ExclusionsVariableDeclarationList);
    }
}

public sealed partial class BindingPattern
{
    internal override uint ComputeJsxFacts()
    {
        switch (this.Kind)
        {
            case Kind.ObjectBindingPattern:
            {
                return Ast.JsxListFacts(this.Elements, Ast.JsxPropagate);
            }

            case Kind.ArrayBindingPattern:
            {
                return Ast.JsxListFacts(this.Elements, Ast.JsxPropagate);
            }

            default:
            {
                return JsxProjection.FactsNone;
            }
        }
    }
}

public sealed partial class BindingPattern
{
    internal override uint PropagateJsxFacts()
    {
        return (this.JsxFacts() & ~JsxProjection.ExclusionsBindingPattern);
    }
}

public sealed partial class ParameterDeclaration
{
    internal override uint ComputeJsxFacts()
    {
        if (((this.name != null) && Ast.IsThisIdentifier(this.name)))
        {
            return JsxProjection.ContainsTypeScript;
        }
        else
        {
            return ((((Ast.JsxModifierFacts(this.modifiers) | Ast.JsxPropagate(this.name)) | Ast.JsxEraseableFacts(this.QuestionToken)) | Ast.JsxEraseableFacts(this.Type)) | Ast.JsxPropagate(this.Initializer));
        }
    }
}

public sealed partial class ParameterDeclaration
{
    internal override uint PropagateJsxFacts()
    {
        return (this.JsxFacts() & ~JsxProjection.ExclusionsParameter);
    }
}

public sealed partial class BindingElement
{
    internal override uint ComputeJsxFacts()
    {
        return (((Ast.JsxPropagate(this.PropertyName) | Ast.JsxPropagate(this.name)) | Ast.JsxPropagate(this.Initializer)) | Core.IfElse((this.DotDotDotToken != null), JsxProjection.ContainsRestOrSpread, JsxProjection.FactsNone));
    }
}

public sealed partial class FunctionDeclaration
{
    internal override uint ComputeJsxFacts()
    {
        if (((this.Body == null) || ((this.ModifierFlags() & TypeScript.Scanner.ModifierFlags.Ambient) != 0)))
        {
            return JsxProjection.ContainsTypeScript;
        }
        else
        {
            var isAsync = ((this.ModifierFlags() & TypeScript.Scanner.ModifierFlags.Async) != 0);
            var isGenerator = (this.AsteriskToken != null);
            return (((((((((Ast.JsxModifierFacts(this.modifiers) | Ast.JsxPropagate(this.AsteriskToken)) | Ast.JsxPropagate(this.name)) | Ast.JsxEraseableListFacts(this.TypeParameters)) | Ast.JsxListFacts(this.Parameters, Ast.JsxPropagate)) | Ast.JsxEraseableFacts(this.Type)) | Ast.JsxEraseableFacts(this.FullSignature)) | Ast.JsxPropagate(this.Body)) | Core.IfElse((isAsync && isGenerator), JsxProjection.ContainsForAwaitOrAsyncGenerator, JsxProjection.FactsNone)) | Core.IfElse((isAsync && !isGenerator), JsxProjection.ContainsAnyAwait, JsxProjection.FactsNone));
        }
    }
}

public sealed partial class FunctionDeclaration
{
    internal override uint PropagateJsxFacts()
    {
        return (this.JsxFacts() & ~JsxProjection.ExclusionsFunction);
    }
}

public sealed partial class ClassDeclaration
{
    internal override uint ComputeJsxFacts()
    {
        if (((this.modifiers != null) && ((this.modifiers.ModifierFlags & TypeScript.Scanner.ModifierFlags.Ambient) != 0)))
        {
            return JsxProjection.ContainsTypeScript;
        }
        else
        {
            return ((((Ast.JsxModifierFacts(this.modifiers) | Ast.JsxPropagate(this.name)) | Ast.JsxEraseableListFacts(this.TypeParameters)) | Ast.JsxListFacts(this.HeritageClauses, Ast.JsxPropagate)) | Ast.JsxListFacts(this.Members, Ast.JsxPropagate));
        }
    }
}

public sealed partial class ClassDeclaration
{
    internal override uint PropagateJsxFacts()
    {
        return (this.JsxFacts() & ~JsxProjection.ExclusionsClass);
    }
}

public sealed partial class ClassExpression
{
    internal override uint ComputeJsxFacts()
    {
        if (((this.modifiers != null) && ((this.modifiers.ModifierFlags & TypeScript.Scanner.ModifierFlags.Ambient) != 0)))
        {
            return JsxProjection.ContainsTypeScript;
        }
        else
        {
            return ((((Ast.JsxModifierFacts(this.modifiers) | Ast.JsxPropagate(this.name)) | Ast.JsxEraseableListFacts(this.TypeParameters)) | Ast.JsxListFacts(this.HeritageClauses, Ast.JsxPropagate)) | Ast.JsxListFacts(this.Members, Ast.JsxPropagate));
        }
    }
}

public sealed partial class ClassExpression
{
    internal override uint PropagateJsxFacts()
    {
        return (this.JsxFacts() & ~JsxProjection.ExclusionsClass);
    }
}

public sealed partial class HeritageClause
{
    internal override uint ComputeJsxFacts()
    {
        switch (this.Token)
        {
            case Kind.ExtendsKeyword:
            {
                return Ast.JsxListFacts(this.Types, Ast.JsxPropagate);
            }

            case Kind.ImplementsKeyword:
            {
                return JsxProjection.ContainsTypeScript;
            }

            default:
            {
                return JsxProjection.FactsNone;
            }
        }
    }
}

public sealed partial class InterfaceDeclaration
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class InterfaceDeclaration
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class TypeAliasDeclaration
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class TypeAliasDeclaration
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class EnumMember
{
    internal override uint ComputeJsxFacts()
    {
        return ((Ast.JsxPropagate(this.name) | Ast.JsxPropagate(this.Initializer)) | JsxProjection.ContainsTypeScript);
    }
}

public sealed partial class EnumDeclaration
{
    internal override uint ComputeJsxFacts()
    {
        if (((this.modifiers != null) && ((this.modifiers.ModifierFlags & TypeScript.Scanner.ModifierFlags.Ambient) != 0)))
        {
            return JsxProjection.ContainsTypeScript;
        }
        else
        {
            return (((Ast.JsxModifierFacts(this.modifiers) | Ast.JsxPropagate(this.name)) | Ast.JsxListFacts(this.Members, Ast.JsxPropagate)) | JsxProjection.ContainsTypeScript);
        }
    }
}

public sealed partial class ExportAssignment
{
    internal override uint ComputeJsxFacts()
    {
        return (((Ast.JsxModifierFacts(this.modifiers) | Ast.JsxPropagate(this.Type)) | Ast.JsxPropagate(this.Expression)) | Core.IfElse(this.IsExportEquals, JsxProjection.ContainsTypeScript, JsxProjection.FactsNone));
    }
}

public sealed partial class NamespaceExportDeclaration
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class NamespaceExportDeclaration
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class ExportSpecifier
{
    internal override uint ComputeJsxFacts()
    {
        if (this.IsTypeOnly)
        {
            return JsxProjection.ContainsTypeScript;
        }
        else
        {
            return (Ast.JsxPropagate(this.PropertyName) | Ast.JsxPropagate(this.name));
        }
    }
}

public sealed partial class CallSignatureDeclaration
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class CallSignatureDeclaration
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class ConstructSignatureDeclaration
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class ConstructSignatureDeclaration
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class ConstructorDeclaration
{
    internal override uint ComputeJsxFacts()
    {
        if ((this.Body == null))
        {
            return JsxProjection.ContainsTypeScript;
        }
        else
        {
            return (((((Ast.JsxModifierFacts(this.modifiers) | Ast.JsxEraseableListFacts(this.TypeParameters)) | Ast.JsxListFacts(this.Parameters, Ast.JsxPropagate)) | Ast.JsxEraseableFacts(this.Type)) | Ast.JsxEraseableFacts(this.FullSignature)) | Ast.JsxPropagate(this.Body));
        }
    }
}

public sealed partial class ConstructorDeclaration
{
    internal override uint PropagateJsxFacts()
    {
        return (this.JsxFacts() & ~JsxProjection.ExclusionsConstructor);
    }
}

public sealed partial class GetAccessorDeclaration
{
    internal override uint ComputeJsxFacts()
    {
        if ((this.Body == null))
        {
            return JsxProjection.ContainsTypeScript;
        }
        else
        {
            return ((((((Ast.JsxModifierFacts(this.modifiers) | Ast.JsxPropagate(this.name)) | Ast.JsxEraseableListFacts(this.TypeParameters)) | Ast.JsxListFacts(this.Parameters, Ast.JsxPropagate)) | Ast.JsxEraseableFacts(this.Type)) | Ast.JsxEraseableFacts(this.FullSignature)) | Ast.JsxPropagate(this.Body));
        }
    }
}

public sealed partial class GetAccessorDeclaration
{
    internal override uint PropagateJsxFacts()
    {
        return ((this.JsxFacts() & ~JsxProjection.ExclusionsAccessor) | Ast.JsxPropagate(this.name));
    }
}

public sealed partial class SetAccessorDeclaration
{
    internal override uint ComputeJsxFacts()
    {
        if ((this.Body == null))
        {
            return JsxProjection.ContainsTypeScript;
        }
        else
        {
            return ((((((Ast.JsxModifierFacts(this.modifiers) | Ast.JsxPropagate(this.name)) | Ast.JsxEraseableListFacts(this.TypeParameters)) | Ast.JsxListFacts(this.Parameters, Ast.JsxPropagate)) | Ast.JsxEraseableFacts(this.Type)) | Ast.JsxEraseableFacts(this.FullSignature)) | Ast.JsxPropagate(this.Body));
        }
    }
}

public sealed partial class SetAccessorDeclaration
{
    internal override uint PropagateJsxFacts()
    {
        return ((this.JsxFacts() & ~JsxProjection.ExclusionsAccessor) | Ast.JsxPropagate(this.name));
    }
}

public sealed partial class IndexSignatureDeclaration
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class IndexSignatureDeclaration
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class MethodSignatureDeclaration
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class MethodSignatureDeclaration
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class MethodDeclaration
{
    internal override uint ComputeJsxFacts()
    {
        if ((this.Body == null))
        {
            return JsxProjection.ContainsTypeScript;
        }
        else
        {
            var isAsync = ((this.modifiers != null) && ((this.modifiers.ModifierFlags & TypeScript.Scanner.ModifierFlags.Async) != 0));
            var isGenerator = (this.AsteriskToken != null);
            return ((((((((((Ast.JsxModifierFacts(this.modifiers) | Ast.JsxPropagate(this.AsteriskToken)) | Ast.JsxPropagate(this.name)) | Ast.JsxEraseableFacts(this.PostfixToken)) | Ast.JsxEraseableListFacts(this.TypeParameters)) | Ast.JsxListFacts(this.Parameters, Ast.JsxPropagate)) | Ast.JsxPropagate(this.Body)) | Ast.JsxEraseableFacts(this.Type)) | Ast.JsxEraseableFacts(this.FullSignature)) | Core.IfElse((isAsync && isGenerator), JsxProjection.ContainsForAwaitOrAsyncGenerator, JsxProjection.FactsNone)) | Core.IfElse((isAsync && !isGenerator), JsxProjection.ContainsAnyAwait, JsxProjection.FactsNone));
        }
    }
}

public sealed partial class MethodDeclaration
{
    internal override uint PropagateJsxFacts()
    {
        return ((this.JsxFacts() & ~JsxProjection.ExclusionsMethod) | Ast.JsxPropagate(this.name));
    }
}

public sealed partial class PropertySignatureDeclaration
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class PropertySignatureDeclaration
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class PropertyDeclaration
{
    internal override uint ComputeJsxFacts()
    {
        return (((((Ast.JsxModifierFacts(this.modifiers) | Ast.JsxPropagate(this.name)) | Ast.JsxEraseableFacts(this.PostfixToken)) | Ast.JsxEraseableFacts(this.Type)) | Ast.JsxPropagate(this.Initializer)) | JsxProjection.ContainsClassFields);
    }
}

public sealed partial class PropertyDeclaration
{
    internal override uint PropagateJsxFacts()
    {
        return ((this.JsxFacts() & ~JsxProjection.ExclusionsProperty) | Ast.JsxPropagate(this.name));
    }
}

public sealed partial class ClassStaticBlockDeclaration
{
    internal override uint ComputeJsxFacts()
    {
        return ((Ast.JsxModifierFacts(this.modifiers) | Ast.JsxPropagate(this.Body)) | JsxProjection.ContainsClassFields);
    }
}

public sealed partial class KeywordExpression
{
    internal override uint ComputeJsxFacts()
    {
        switch (this.Kind)
        {
            case Kind.ThisKeyword:
            {
                return JsxProjection.ContainsLexicalThis;
            }

            case Kind.SuperKeyword:
            {
                return JsxProjection.ContainsLexicalSuper;
            }
        }

        return JsxProjection.FactsNone;
    }
}

public sealed partial class BigIntLiteral
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.FactsNone;
    }
}

public sealed partial class NoSubstitutionTemplateLiteral
{
    internal override uint ComputeJsxFacts()
    {
        if (((this.TemplateFlags & TypeScript.Scanner.TokenFlags.ContainsInvalidEscape) != 0))
        {
            return JsxProjection.ContainsInvalidTemplateEscape;
        }

        return JsxProjection.FactsNone;
    }
}

public sealed partial class BinaryExpression
{
    internal override uint ComputeJsxFacts()
    {
        var facts = (((((Ast.JsxModifierFacts(this.modifiers) | Ast.JsxPropagate(this.Left)) | Ast.JsxPropagate(this.Type)) | Ast.JsxPropagate(this.OperatorToken)) | Ast.JsxPropagate(this.Right)) | Core.IfElse(((this.OperatorToken.Kind == Kind.InKeyword) && Ast.IsPrivateIdentifier(this.Left)), (JsxProjection.ContainsClassFields | JsxProjection.ContainsPrivateIdentifierInExpression), JsxProjection.FactsNone));
        return facts;
    }
}

public sealed partial class YieldExpression
{
    internal override uint ComputeJsxFacts()
    {
        return (Ast.JsxPropagate(this.Expression) | JsxProjection.ContainsForAwaitOrAsyncGenerator);
    }
}

public sealed partial class ArrowFunction
{
    internal override uint ComputeJsxFacts()
    {
        return ((((((Ast.JsxModifierFacts(this.modifiers) | Ast.JsxEraseableListFacts(this.TypeParameters)) | Ast.JsxListFacts(this.Parameters, Ast.JsxPropagate)) | Ast.JsxEraseableFacts(this.Type)) | Ast.JsxEraseableFacts(this.FullSignature)) | Ast.JsxPropagate(this.Body)) | Core.IfElse(((this.ModifierFlags() & TypeScript.Scanner.ModifierFlags.Async) != 0), JsxProjection.ContainsAnyAwait, JsxProjection.FactsNone));
    }
}

public sealed partial class ArrowFunction
{
    internal override uint PropagateJsxFacts()
    {
        return (this.JsxFacts() & ~JsxProjection.ExclusionsArrowFunction);
    }
}

public sealed partial class FunctionExpression
{
    internal override uint ComputeJsxFacts()
    {
        var isAsync = ((this.modifiers != null) && ((this.modifiers.ModifierFlags & TypeScript.Scanner.ModifierFlags.Async) != 0));
        var isGenerator = (this.AsteriskToken != null);
        return (((((((((Ast.JsxModifierFacts(this.modifiers) | Ast.JsxPropagate(this.AsteriskToken)) | Ast.JsxPropagate(this.name)) | Ast.JsxEraseableListFacts(this.TypeParameters)) | Ast.JsxListFacts(this.Parameters, Ast.JsxPropagate)) | Ast.JsxEraseableFacts(this.Type)) | Ast.JsxEraseableFacts(this.FullSignature)) | Ast.JsxPropagate(this.Body)) | Core.IfElse((isAsync && isGenerator), JsxProjection.ContainsForAwaitOrAsyncGenerator, JsxProjection.FactsNone)) | Core.IfElse((isAsync && !isGenerator), JsxProjection.ContainsAnyAwait, JsxProjection.FactsNone));
    }
}

public sealed partial class FunctionExpression
{
    internal override uint PropagateJsxFacts()
    {
        return (this.JsxFacts() & ~JsxProjection.ExclusionsFunction);
    }
}

public sealed partial class AsExpression
{
    internal override uint ComputeJsxFacts()
    {
        return (Ast.JsxPropagate(this.Expression) | JsxProjection.ContainsTypeScript);
    }
}

public sealed partial class AsExpression
{
    internal override uint PropagateJsxFacts()
    {
        return (this.JsxFacts() & ~JsxProjection.ExclusionsOuterExpression);
    }
}

public sealed partial class SatisfiesExpression
{
    internal override uint ComputeJsxFacts()
    {
        return (Ast.JsxPropagate(this.Expression) | JsxProjection.ContainsTypeScript);
    }
}

public sealed partial class SatisfiesExpression
{
    internal override uint PropagateJsxFacts()
    {
        return (this.JsxFacts() & ~JsxProjection.ExclusionsOuterExpression);
    }
}

public sealed partial class PropertyAccessExpression
{
    internal override uint ComputeJsxFacts()
    {
        var privateName = JsxProjection.FactsNone;
        if (!Ast.IsIdentifier(this.name))
        {
            privateName = JsxProjection.ContainsPrivateIdentifierInExpression;
        }

        return (((Ast.JsxPropagate(this.Expression) | Ast.JsxPropagate(this.QuestionDotToken)) | Ast.JsxPropagate(this.name)) | privateName);
    }
}

public sealed partial class PropertyAccessExpression
{
    internal override uint PropagateJsxFacts()
    {
        return (this.JsxFacts() & ~JsxProjection.ExclusionsPropertyAccess);
    }
}

public sealed partial class ElementAccessExpression
{
    internal override uint PropagateJsxFacts()
    {
        return (this.JsxFacts() & ~JsxProjection.ExclusionsElementAccess);
    }
}

public sealed partial class CallExpression
{
    internal override uint ComputeJsxFacts()
    {
        return ((((Ast.JsxPropagate(this.Expression) | Ast.JsxPropagate(this.QuestionDotToken)) | Ast.JsxEraseableListFacts(this.TypeArguments)) | Ast.JsxListFacts(this.Arguments, Ast.JsxPropagate)) | Core.IfElse(Ast.IsImportCall(this.AsNode()), JsxProjection.ContainsDynamicImport, JsxProjection.FactsNone));
    }
}

public sealed partial class CallExpression
{
    internal override uint PropagateJsxFacts()
    {
        return (this.JsxFacts() & ~JsxProjection.ExclusionsCall);
    }
}

public sealed partial class NewExpression
{
    internal override uint ComputeJsxFacts()
    {
        return ((Ast.JsxPropagate(this.Expression) | Ast.JsxEraseableListFacts(this.TypeArguments)) | Ast.JsxListFacts(this.Arguments, Ast.JsxPropagate));
    }
}

public sealed partial class NewExpression
{
    internal override uint PropagateJsxFacts()
    {
        return (this.JsxFacts() & ~JsxProjection.ExclusionsNew);
    }
}

public sealed partial class MetaProperty
{
    internal override uint ComputeJsxFacts()
    {
        return (Ast.JsxPropagate(this.name) & ~JsxProjection.ContainsIdentifier);
    }
}

public sealed partial class NonNullExpression
{
    internal override uint ComputeJsxFacts()
    {
        return (Ast.JsxPropagate(this.Expression) | JsxProjection.ContainsTypeScript);
    }
}

public sealed partial class SpreadElement
{
    internal override uint ComputeJsxFacts()
    {
        return (Ast.JsxPropagate(this.Expression) | JsxProjection.ContainsRestOrSpread);
    }
}

public sealed partial class TaggedTemplateExpression
{
    internal override uint ComputeJsxFacts()
    {
        return (((Ast.JsxPropagate(this.Tag) | Ast.JsxPropagate(this.QuestionDotToken)) | Ast.JsxEraseableListFacts(this.TypeArguments)) | Ast.JsxPropagate(this.Template));
    }
}

public sealed partial class ArrayLiteralExpression
{
    internal override uint PropagateJsxFacts()
    {
        return (this.JsxFacts() & ~JsxProjection.ExclusionsArrayLiteral);
    }
}

public sealed partial class ObjectLiteralExpression
{
    internal override uint PropagateJsxFacts()
    {
        return (this.JsxFacts() & ~JsxProjection.ExclusionsObjectLiteral);
    }
}

public sealed partial class SpreadAssignment
{
    internal override uint ComputeJsxFacts()
    {
        return ((Ast.JsxPropagate(this.Expression) | JsxProjection.ContainsESObjectRestOrSpread) | JsxProjection.ContainsObjectRestOrSpread);
    }
}

public sealed partial class PropertyAssignment
{
    internal override uint ComputeJsxFacts()
    {
        return ((Ast.JsxPropagate(this.name) | Ast.JsxPropagate(this.Type)) | Ast.JsxPropagate(this.Initializer));
    }
}

public sealed partial class ShorthandPropertyAssignment
{
    internal override uint ComputeJsxFacts()
    {
        return (((Ast.JsxPropagate(this.name) | Ast.JsxPropagate(this.Type)) | Ast.JsxPropagate(this.ObjectAssignmentInitializer)) | JsxProjection.ContainsTypeScript);
    }
}

public sealed partial class AwaitExpression
{
    internal override uint ComputeJsxFacts()
    {
        return (((Ast.JsxPropagate(this.Expression) | JsxProjection.ContainsAwait) | JsxProjection.ContainsAnyAwait) | JsxProjection.ContainsForAwaitOrAsyncGenerator);
    }
}

public sealed partial class TypeAssertion
{
    internal override uint ComputeJsxFacts()
    {
        return (Ast.JsxPropagate(this.Expression) | JsxProjection.ContainsTypeScript);
    }
}

public sealed partial class TypeAssertion
{
    internal override uint PropagateJsxFacts()
    {
        return (this.JsxFacts() & ~JsxProjection.ExclusionsOuterExpression);
    }
}

public sealed partial class KeywordTypeNode
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class KeywordTypeNode
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class UnionTypeNode
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class UnionTypeNode
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class IntersectionTypeNode
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class IntersectionTypeNode
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class ConditionalTypeNode
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class ConditionalTypeNode
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class TypeOperatorNode
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class TypeOperatorNode
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class InferTypeNode
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class InferTypeNode
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class ArrayTypeNode
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class ArrayTypeNode
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class IndexedAccessTypeNode
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class IndexedAccessTypeNode
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class TypeReferenceNode
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class TypeReferenceNode
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class ExpressionWithTypeArguments
{
    internal override uint ComputeJsxFacts()
    {
        return (Ast.JsxPropagate(this.Expression) | Ast.JsxEraseableListFacts(this.TypeArguments));
    }
}

public sealed partial class LiteralTypeNode
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class LiteralTypeNode
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class ThisTypeNode
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class ThisTypeNode
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class TypePredicateNode
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class TypePredicateNode
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class TypeQueryNode
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class TypeQueryNode
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class MappedTypeNode
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class MappedTypeNode
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class TypeLiteralNode
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class TypeLiteralNode
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class TupleTypeNode
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class TupleTypeNode
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class NamedTupleMember
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class NamedTupleMember
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class OptionalTypeNode
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class OptionalTypeNode
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class RestTypeNode
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class RestTypeNode
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class ParenthesizedTypeNode
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class ParenthesizedTypeNode
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class FunctionTypeNode
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class FunctionTypeNode
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class ConstructorTypeNode
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class ConstructorTypeNode
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class TemplateHead
{
    internal override uint ComputeJsxFacts()
    {
        if (((this.TemplateFlags & TypeScript.Scanner.TokenFlags.ContainsInvalidEscape) != 0))
        {
            return JsxProjection.ContainsInvalidTemplateEscape;
        }

        return JsxProjection.FactsNone;
    }
}

public sealed partial class TemplateMiddle
{
    internal override uint ComputeJsxFacts()
    {
        if (((this.TemplateFlags & TypeScript.Scanner.TokenFlags.ContainsInvalidEscape) != 0))
        {
            return JsxProjection.ContainsInvalidTemplateEscape;
        }

        return JsxProjection.FactsNone;
    }
}

public sealed partial class TemplateTail
{
    internal override uint ComputeJsxFacts()
    {
        if (((this.TemplateFlags & TypeScript.Scanner.TokenFlags.ContainsInvalidEscape) != 0))
        {
            return JsxProjection.ContainsInvalidTemplateEscape;
        }

        return JsxProjection.FactsNone;
    }
}

public sealed partial class TemplateLiteralTypeNode
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class TemplateLiteralTypeNode
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class TemplateLiteralTypeSpan
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class TemplateLiteralTypeSpan
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class JsxElement
{
    internal override uint ComputeJsxFacts()
    {
        return (((Ast.JsxPropagate(this.OpeningElement) | Ast.JsxListFacts(this.Children, Ast.JsxPropagate)) | Ast.JsxPropagate(this.ClosingElement)) | JsxProjection.ContainsJsx);
    }
}

public sealed partial class JsxAttributes
{
    internal override uint ComputeJsxFacts()
    {
        return (Ast.JsxListFacts(this.Properties, Ast.JsxPropagate) | JsxProjection.ContainsJsx);
    }
}

public sealed partial class JsxNamespacedName
{
    internal override uint ComputeJsxFacts()
    {
        return ((Ast.JsxPropagate(this.Namespace) | Ast.JsxPropagate(this.name)) | JsxProjection.ContainsJsx);
    }
}

public sealed partial class JsxOpeningElement
{
    internal override uint ComputeJsxFacts()
    {
        return (((Ast.JsxPropagate(this.TagName) | Ast.JsxEraseableListFacts(this.TypeArguments)) | Ast.JsxPropagate(this.Attributes)) | JsxProjection.ContainsJsx);
    }
}

public sealed partial class JsxSelfClosingElement
{
    internal override uint ComputeJsxFacts()
    {
        return (((Ast.JsxPropagate(this.TagName) | Ast.JsxEraseableListFacts(this.TypeArguments)) | Ast.JsxPropagate(this.Attributes)) | JsxProjection.ContainsJsx);
    }
}

public sealed partial class JsxFragment
{
    internal override uint ComputeJsxFacts()
    {
        return (Ast.JsxListFacts(this.Children, Ast.JsxPropagate) | JsxProjection.ContainsJsx);
    }
}

public sealed partial class JsxOpeningFragment
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsJsx;
    }
}

public sealed partial class JsxClosingFragment
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsJsx;
    }
}

public sealed partial class JsxAttribute
{
    internal override uint ComputeJsxFacts()
    {
        return ((Ast.JsxPropagate(this.name) | Ast.JsxPropagate(this.Initializer)) | JsxProjection.ContainsJsx);
    }
}

public sealed partial class JsxSpreadAttribute
{
    internal override uint ComputeJsxFacts()
    {
        return (Ast.JsxPropagate(this.Expression) | JsxProjection.ContainsJsx);
    }
}

public sealed partial class JsxClosingElement
{
    internal override uint ComputeJsxFacts()
    {
        return (Ast.JsxPropagate(this.TagName) | JsxProjection.ContainsJsx);
    }
}

public sealed partial class JsxExpression
{
    internal override uint ComputeJsxFacts()
    {
        return (Ast.JsxPropagate(this.Expression) | JsxProjection.ContainsJsx);
    }
}

public sealed partial class JsxText
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsJsx;
    }
}

public sealed partial class JSDocTypeExpression
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class JSDocTypeExpression
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class JSDocNonNullableType
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class JSDocNonNullableType
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class JSDocNullableType
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class JSDocNullableType
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class JSDocAllType
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class JSDocAllType
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class JSDocVariadicType
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class JSDocVariadicType
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class JSDocOptionalType
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class JSDocOptionalType
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class JSDocSignature
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class JSDocSignature
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class JSDocNameReference
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class JSDocNameReference
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class ModuleDeclaration
{
    internal override uint ComputeJsxFacts()
    {
        if (((this.ModifierFlags() & TypeScript.Scanner.ModifierFlags.Ambient) != 0))
        {
            return JsxProjection.ContainsTypeScript;
        }
        else
        {
            return (((Ast.JsxModifierFacts(this.modifiers) | Ast.JsxPropagate(this.name)) | Ast.JsxPropagate(this.Body)) | JsxProjection.ContainsTypeScript);
        }
    }
}

public sealed partial class ModuleDeclaration
{
    internal override uint PropagateJsxFacts()
    {
        return (this.JsxFacts() & ~JsxProjection.ExclusionsModule);
    }
}

public sealed partial class ImportEqualsDeclaration
{
    internal override uint ComputeJsxFacts()
    {
        if ((this.IsTypeOnly || !Ast.IsExternalModuleReference(this.ModuleReference)))
        {
            return JsxProjection.ContainsTypeScript;
        }
        else
        {
            return ((Ast.JsxModifierFacts(this.modifiers) | Ast.JsxPropagate(this.name)) | Ast.JsxPropagate(this.ModuleReference));
        }
    }
}

public sealed partial class ExportDeclaration
{
    internal override uint ComputeJsxFacts()
    {
        return ((((Ast.JsxModifierFacts(this.modifiers) | Ast.JsxPropagate(this.ExportClause)) | Ast.JsxPropagate(this.ModuleSpecifier)) | Ast.JsxPropagate(this.Attributes)) | Core.IfElse(this.IsTypeOnly, JsxProjection.ContainsTypeScript, JsxProjection.FactsNone));
    }
}

public sealed partial class ImportTypeNode
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class ImportTypeNode
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class ImportClause
{
    internal override uint ComputeJsxFacts()
    {
        if ((this.PhaseModifier == Kind.TypeKeyword))
        {
            return JsxProjection.ContainsTypeScript;
        }
        else
        {
            return (Ast.JsxPropagate(this.name) | Ast.JsxPropagate(this.NamedBindings));
        }
    }
}

public sealed partial class ImportSpecifier
{
    internal override uint ComputeJsxFacts()
    {
        if (this.IsTypeOnly)
        {
            return JsxProjection.ContainsTypeScript;
        }
        else
        {
            return (Ast.JsxPropagate(this.PropertyName) | Ast.JsxPropagate(this.name));
        }
    }
}

public sealed partial class TypeParameterDeclaration
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class TypeParameterDeclaration
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class JSDocTypeLiteral
{
    internal override uint ComputeJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

public sealed partial class JSDocTypeLiteral
{
    internal override uint PropagateJsxFacts()
    {
        return JsxProjection.ContainsTypeScript;
    }
}

// END GENERATED JSX PROJECTION
// BEGIN GENERATED AST ACCESSORS (tools/generate-ast-accessors.go)
public sealed partial class Node
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

