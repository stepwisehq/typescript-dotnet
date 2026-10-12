// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/printer/emitcontext.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
namespace TypeScript.DiagnosticPrinting;

internal struct AutoGenerateOptions
{
    internal GeneratedIdentifierFlags Flags;
    private string prefix, suffix;
    internal string Prefix { readonly get => prefix ?? ""; set => prefix = value; }
    internal string Suffix { readonly get => suffix ?? ""; set => suffix = value; }
}
internal sealed class AutoGenerateInfo
{
    internal GeneratedIdentifierFlags Flags;
    internal uint Id;
    internal string Prefix, Suffix;
    internal Node Node;
}
internal enum SnippetKind { TabStop }
internal readonly record struct SnippetElement(SnippetKind Kind, int Order);
internal readonly record struct SynthesizedComment(Kind Kind, TextRange Loc, bool HasLeadingNewLine, bool HasTrailingNewLine, string Text);

// Go's embedded record is retained in the established stable LinkStore arena.
internal struct emitNode
{
    internal uint flags;
    internal EmitFlags emitFlags;
    internal TextRange commentRange, sourceMapRange;
    internal Dictionary<Kind, TextRange> tokenSourceMapRanges;
    internal Slice<EmitHelper> helpers;
    internal Node externalHelpersModuleName;
    internal Slice<SynthesizedComment> leadingComments, trailingComments;
    internal Node typeNode;
    internal SnippetElement? snippetElement;
    internal void copyFrom(in emitNode source)
    {
        flags = source.flags;
        emitFlags = source.emitFlags;
        commentRange = source.commentRange;
        sourceMapRange = source.sourceMapRange;
        tokenSourceMapRanges = source.tokenSourceMapRanges == null ? null : new(source.tokenSourceMapRanges);
        helpers = GoSlices.Clone(source.helpers);
        externalHelpersModuleName = source.externalHelpersModuleName;
        if (source.snippetElement is not null) snippetElement = source.snippetElement.Value;
    }
}
internal sealed class EmitContext
{
    internal readonly NodeFactory Factory;
    private readonly NodeVisitorHooks visitorHooks;
    internal Dictionary<Node, AutoGenerateInfo> autoGenerate;
    internal Dictionary<Node, Node> textSource;
    private Dictionary<Node, Node> original;
    private readonly LinkStore<Node, emitNode> emitNodes = new();
    private Dictionary<Node, Node> assignedName, classThis;
    private readonly Stack<varScope> varScopeStack = new(), letScopeStack = new();
    // OrderedSet pointer identity: insertion ordered header plus membership owner.
    private Slice<EmitHelper> emitHelpers;
    private readonly HashSet<EmitHelper> emitHelperSet = new(ReferenceEqualityComparer.Instance);
    internal static int nextAutoGenerateId;
    private const uint hasCommentRange = 1, hasSourceMapRange = 2;
    [Flags] private enum environmentFlags { None, InParameters = 1, VariablesHoistedInParameters = 2 }
    private sealed class varScope
    {
        internal Slice<Node> variables, functions, initializationStatements;
        internal environmentFlags flags;
    }
    internal EmitContext()
    {
        Factory = new(this);
        // emitcontext.go:69: the immutable method receivers live with this context.
        // Cache CLR delegates once; each visitor still receives its own Go Hooks
        // value copy, so later visitor hook mutation cannot affect another visitor.
        visitorHooks = new NodeVisitorHooks
        {
            VisitParameters = VisitParameters,
            VisitFunctionBody = VisitFunctionBody,
            VisitIterationBody = VisitIterationBody,
            VisitTopLevelStatements = VisitVariableEnvironment,
            VisitEmbeddedStatement = VisitEmbeddedStatement
        };
    }
    internal void onCreate(Node node) => node.Flags |= NodeFlags.Synthesized;
    internal void onUpdate(Node updated, Node original) => SetOriginal(updated, original);
    internal void onClone(Node updated, Node original)
    {
        SetOriginal(updated, original);
        if ((Ast.IsIdentifier(updated) || Ast.IsPrivateIdentifier(updated)) && autoGenerate?.TryGetValue(original, out var info) == true)
            autoGenerate[updated] = new() { Id = info.Id, Flags = info.Flags, Prefix = info.Prefix, Suffix = info.Suffix, Node = info.Node };
    }
    internal NodeVisitor NewNodeVisitor(Func<Node, Node> visit) => new(visit, Factory.AsNodeFactory(), visitorHooks);
    internal void StartVariableEnvironment() { varScopeStack.Push(new()); StartLexicalEnvironment(); }
    internal Slice<Node> EndVariableEnvironment()
    {
        var scope = varScopeStack.Pop();
        Slice<Node> statements = default;
        if (scope.functions.Length > 0) statements = GoSlices.Clone(scope.functions);
        if (scope.variables.Length > 0)
        {
            var list = Factory.NewVariableDeclarationList(Factory.NewNodeList(scope.variables), NodeFlags.None);
            var statement = Factory.NewVariableStatement(null, list);
            SetEmitFlags(statement, TypeScript.DiagnosticPrinting.EmitFlags.CustomPrologue);
            statements = statements.Append(statement);
        }
        if (scope.initializationStatements.Length > 0) statements = statements.Append(scope.initializationStatements);
        return statements.Append(EndLexicalEnvironment());
    }
    internal NodeList EndAndMergeVariableEnvironmentList(NodeList statements)
    {
        var (result, changed) = endAndMergeVariableEnvironment(statements?.Nodes ?? default);
        if (!changed) return statements;
        var list = Factory.NewNodeList(result);
        list.Loc = statements.Loc;
        return list;
    }
    internal Slice<Node> EndAndMergeVariableEnvironment(Slice<Node> statements) => endAndMergeVariableEnvironment(statements).Item1;
    private (Slice<Node>, bool) endAndMergeVariableEnvironment(Slice<Node> statements) => mergeEnvironment(statements, EndVariableEnvironment());
    internal void AddVariableDeclaration(Node name)
    {
        var declaration = Factory.NewVariableDeclaration(name, null, null, null);
        SetEmitFlags(declaration, TypeScript.DiagnosticPrinting.EmitFlags.NoNestedSourceMaps);
        var scope = varScopeStack.Peek();
        scope.variables = scope.variables.Append(declaration);
        if ((scope.flags & environmentFlags.InParameters) != 0) scope.flags |= environmentFlags.VariablesHoistedInParameters;
    }
    internal void AddHoistedFunctionDeclaration(Node node)
    {
        SetEmitFlags(node, TypeScript.DiagnosticPrinting.EmitFlags.CustomPrologue);
        var scope = varScopeStack.Peek(); scope.functions = scope.functions.Append(node);
    }
    internal void StartLexicalEnvironment() => letScopeStack.Push(new());
    internal Slice<Node> EndLexicalEnvironment()
    {
        var scope = letScopeStack.Pop(); Slice<Node> statements = default;
        if (scope.variables.Length > 0)
        {
            var list = Factory.NewVariableDeclarationList(Factory.NewNodeList(scope.variables), NodeFlags.Let);
            var statement = Factory.NewVariableStatement(null, list);
            SetEmitFlags(statement, TypeScript.DiagnosticPrinting.EmitFlags.CustomPrologue);
            statements = statements.Append(statement);
        }
        return statements;
    }
    internal NodeList EndAndMergeLexicalEnvironmentList(NodeList statements)
    {
        var (result, changed) = endAndMergeLexicalEnvironment(statements?.Nodes ?? default);
        if (!changed) return statements;
        var list = Factory.NewNodeList(result); list.Loc = statements.Loc; return list;
    }
    internal Slice<Node> EndAndMergeLexicalEnvironment(Slice<Node> statements) => endAndMergeLexicalEnvironment(statements).Item1;
    private (Slice<Node>, bool) endAndMergeLexicalEnvironment(Slice<Node> statements) => mergeEnvironment(statements, EndLexicalEnvironment());
    internal void AddLexicalDeclaration(Node name)
    {
        var declaration = Factory.NewVariableDeclaration(name, null, null, null);
        SetEmitFlags(declaration, TypeScript.DiagnosticPrinting.EmitFlags.NoNestedSourceMaps);
        var scope = letScopeStack.Peek(); scope.variables = scope.variables.Append(declaration);
    }
    internal NodeList MergeEnvironmentList(NodeList statements, Slice<Node> declarations)
    {
        var (result, changed) = mergeEnvironment(statements.Nodes, declarations);
        if (!changed) return statements;
        var list = Factory.NewNodeList(result); list.Loc = statements.Loc; return list;
    }
    internal Slice<Node> MergeEnvironment(Slice<Node> statements, Slice<Node> declarations) => mergeEnvironment(statements, declarations).Item1;
    private (Slice<Node>, bool) mergeEnvironment(Slice<Node> statements, Slice<Node> declarations)
    {
        if (declarations.Length == 0) return (statements, false);
        var changed = false;
        var leftStandardPrologueEnd = PrinterUtilities.findSpanEnd(statements, Ast.IsPrologueDirective, 0);
        var leftHoistedFunctionsEnd = PrinterUtilities.findSpanEndWithEmitContext(this, statements, static (c, n) => c.isHoistedFunction(n), leftStandardPrologueEnd);
        var leftHoistedVariablesEnd = PrinterUtilities.findSpanEndWithEmitContext(this, statements, static (c, n) => c.isHoistedVariableStatement(n), leftHoistedFunctionsEnd);
        var rightStandardPrologueEnd = PrinterUtilities.findSpanEnd(declarations, Ast.IsPrologueDirective, 0);
        var rightHoistedFunctionsEnd = PrinterUtilities.findSpanEndWithEmitContext(this, declarations, static (c, n) => c.isHoistedFunction(n), rightStandardPrologueEnd);
        var rightHoistedVariablesEnd = PrinterUtilities.findSpanEndWithEmitContext(this, declarations, static (c, n) => c.isHoistedVariableStatement(n), rightHoistedFunctionsEnd);
        var rightCustomPrologueEnd = PrinterUtilities.findSpanEndWithEmitContext(this, declarations, static (c, n) => c.isCustomPrologue(n), rightHoistedVariablesEnd);
        if (rightCustomPrologueEnd != declarations.Length) throw new InvalidOperationException("Expected declarations to be valid standard or custom prologues");
        var left = statements;
        if (rightCustomPrologueEnd > rightHoistedVariablesEnd)
        {
            left = Core.Splice(left, leftHoistedVariablesEnd, 0, declarations.Slice2(rightHoistedVariablesEnd, rightCustomPrologueEnd)); changed = true;
        }
        if (rightHoistedVariablesEnd > rightHoistedFunctionsEnd)
        {
            left = Core.Splice(left, leftHoistedFunctionsEnd, 0, declarations.Slice2(rightHoistedFunctionsEnd, rightHoistedVariablesEnd)); changed = true;
        }
        if (rightHoistedFunctionsEnd > rightStandardPrologueEnd)
        {
            left = Core.Splice(left, leftStandardPrologueEnd, 0, declarations.Slice2(rightStandardPrologueEnd, rightHoistedFunctionsEnd)); changed = true;
        }
        if (rightStandardPrologueEnd > 0)
        {
            if (leftStandardPrologueEnd == 0)
            {
                left = Core.Splice(left, 0, 0, declarations.Slice2(0, rightStandardPrologueEnd)); changed = true;
            }
            else
            {
                var leftPrologues = new HashSet<ByteStringKey>();
                for (int i = 0; i < leftStandardPrologueEnd; i++) leftPrologues.Add(ByteStringKey.FromValue(statements[i].Expression().TextValue()));
                for (int i = rightStandardPrologueEnd - 1; i >= 0; i--)
                    if (!leftPrologues.Contains(ByteStringKey.FromValue(declarations[i].Expression().TextValue())))
                    {
                        left = Core.Concatenate(Slice<Node>.FromArray([declarations[i]]), left); changed = true;
                    }
            }
        }
        return (left, changed);
    }
    private bool isCustomPrologue(Node node) => (EmitFlags(node) & TypeScript.DiagnosticPrinting.EmitFlags.CustomPrologue) != 0;
    private bool isHoistedFunction(Node node) => isCustomPrologue(node) && Ast.IsFunctionDeclaration(node);
    private static bool isHoistedVariable(Node node) => Ast.IsIdentifier(node.Name()) && node.Initializer() is null;
    private bool isHoistedVariableStatement(Node node) => isCustomPrologue(node) && Ast.IsVariableStatement(node) && Core.Every(node.AsVariableStatement().DeclarationList.AsVariableDeclarationList().Declarations.Nodes, isHoistedVariable);
    internal bool HasAutoGenerateInfo(Node node) => node != null && autoGenerate?.ContainsKey(node) == true;
    internal AutoGenerateInfo GetAutoGenerateInfo(Node name) => name != null && autoGenerate?.TryGetValue(name, out var value) == true ? value : null;
    internal Node GetNodeForGeneratedName(Node name)
    {
        var info = GetAutoGenerateInfo(name);
        return info?.Flags.IsNode() == true ? getNodeForGeneratedNameWorker(info.Node, info.Id) : name;
    }
    internal Node getNodeForGeneratedNameWorker(Node node, uint autoGenerateId)
    {
        var source = Original(node);
        while (source != null)
        {
            node = source;
            if (Ast.IsMemberName(node))
            {
                var info = GetAutoGenerateInfo(node);
                if (info == null || info.Flags.IsNode() && info.Id != autoGenerateId) break;
                if (info.Flags.IsNode()) { source = info.Node; continue; }
            }
            source = Original(node);
        }
        return node;
    }
    internal void SetOriginal(Node node, Node source) => SetOriginalEx(node, source, false);
    internal void UnsetOriginal(Node node) => original?.Remove(node);
    internal void SetOriginalEx(Node node, Node source, bool allowOverwrite)
    {
        if (source == null) throw new InvalidOperationException("Original cannot be nil.");
        original ??= new(ReferenceEqualityComparer.Instance);
        if (!original.TryGetValue(node, out var existing))
        {
            original[node] = source;
            var location = emitNodes.TryGet(source);
            if (!location.IsEmpty) emitNodes.Get(node).copyFrom(in location.Span[0]);
        }
        else if (!allowOverwrite && existing != source) throw new InvalidOperationException("Original node already set.");
        else if (allowOverwrite) original[node] = source;
    }
    internal Node Original(Node node) => node != null && original?.TryGetValue(node, out var value) == true ? value : null;
    internal Node MostOriginal(Node node)
    {
        if (node != null) for (var source = Original(node); source != null; source = Original(source)) node = source;
        return node;
    }
    internal Node ParseNode(Node node) { node = MostOriginal(node); return node != null && Ast.IsParseTreeNode(node) ? node : null; }
    internal bool IsFileLevelUniqueName(SourceFile sourceFile, string name, Func<string, bool> hasGlobalName)
    {
        if (hasGlobalName?.Invoke(name) == true) return false;
        return !MostOriginal(sourceFile.AsNode()).AsSourceFile().HasIdentifier(name);
    }
    internal EmitFlags EmitFlags(Node node)
    {
        var location = emitNodes.TryGet(node); return location.IsEmpty ? TypeScript.DiagnosticPrinting.EmitFlags.None : location.Span[0].emitFlags;
    }
    internal void SetEmitFlags(Node node, EmitFlags flags) => emitNodes.Get(node).emitFlags = flags;
    internal void AddEmitFlags(Node node, EmitFlags flags) => emitNodes.Get(node).emitFlags |= flags;
    internal SnippetElement? SnippetElement(Node node) { var loc = emitNodes.TryGet(node); return loc.IsEmpty ? null : loc.Span[0].snippetElement; }
    internal void SetSnippetElement(Node node, SnippetElement snippetElement) => emitNodes.Get(node).snippetElement = snippetElement;
    internal TextRange CommentRange(Node node)
    {
        var loc = emitNodes.TryGet(node); return !loc.IsEmpty && (loc.Span[0].flags & hasCommentRange) != 0 ? loc.Span[0].commentRange : node.Loc;
    }
    internal void SetCommentRange(Node node, TextRange loc) { ref var data = ref emitNodes.Get(node); data.commentRange = loc; data.flags |= hasCommentRange; }
    internal void AssignCommentRange(Node to, Node from) => SetCommentRange(to, CommentRange(from));
    internal TextRange SourceMapRange(Node node)
    {
        var loc = emitNodes.TryGet(node); return !loc.IsEmpty && (loc.Span[0].flags & hasSourceMapRange) != 0 ? loc.Span[0].sourceMapRange : node.Loc;
    }
    internal void SetSourceMapRange(Node node, TextRange loc) { ref var data = ref emitNodes.Get(node); data.sourceMapRange = loc; data.flags |= hasSourceMapRange; }
    internal void AssignSourceMapRange(Node to, Node from) => SetSourceMapRange(to, SourceMapRange(from));
    internal void AssignCommentAndSourceMapRanges(Node to, Node from)
    {
        ref var data = ref emitNodes.Get(to); data.commentRange = CommentRange(from); data.sourceMapRange = SourceMapRange(from); data.flags |= hasCommentRange | hasSourceMapRange;
    }
    internal (TextRange, bool) TokenSourceMapRange(Node node, Kind kind)
    {
        var loc = emitNodes.TryGet(node);
        return !loc.IsEmpty && loc.Span[0].tokenSourceMapRanges?.TryGetValue(kind, out var range) == true ? (range, true) : (default, false);
    }
    internal void SetTokenSourceMapRange(Node node, Kind kind, TextRange loc)
    {
        ref var data = ref emitNodes.Get(node); data.tokenSourceMapRanges ??= new(); data.tokenSourceMapRanges[kind] = loc;
    }
    internal Node AssignedName(Node node) => assignedName?.TryGetValue(node, out var value) == true ? value : null;
    internal Node TextSource(Node node) => textSource?.TryGetValue(node, out var value) == true ? value : null;
    internal void SetAssignedName(Node node, Node name) { assignedName ??= new(ReferenceEqualityComparer.Instance); assignedName[node] = name; }
    internal Node ClassThis(Node node) => classThis?.TryGetValue(node, out var value) == true ? value : null;
    internal void SetClassThis(Node node, Node name) { classThis ??= new(ReferenceEqualityComparer.Instance); classThis[node] = name; }
    internal void RequestEmitHelper(EmitHelper helper)
    {
        if (helper.Scoped) throw new InvalidOperationException("Cannot request a scoped emit helper");
        foreach (var dependency in helper.Dependencies) RequestEmitHelper(dependency);
        if (emitHelperSet.Add(helper)) emitHelpers = emitHelpers.Append(helper);
    }
    internal Slice<EmitHelper> ReadEmitHelpers()
    {
        // slices.Collect: a new owner even though the ordered set's internal header remains reusable.
        Slice<EmitHelper> helpers = default;
        foreach (var helper in emitHelpers) helpers = helpers.Append(helper);
        emitHelpers.AsSpan().Clear(); emitHelpers = emitHelpers.Slice2(0, 0); emitHelperSet.Clear(); return helpers;
    }
    internal void AddEmitHelper(Node node, params EmitHelper[] helpers) => AddEmitHelper(node, (ReadOnlySpan<EmitHelper>)helpers);
    // New variadic slice callers borrow the Go header without an owned array copy.
    internal void AddEmitHelper(Node node, Slice<EmitHelper> helpers) => AddEmitHelper(node, helpers.AsSpan());
    private void AddEmitHelper(Node node, ReadOnlySpan<EmitHelper> helpers)
    {
        ref var data = ref emitNodes.Get(node);
        foreach (var helper in helpers) data.helpers = Core.AppendIfUnique(data.helpers, helper);
    }
    internal void MoveEmitHelpers(Node source, Node target, Func<EmitHelper, bool> predicate)
    {
        var sourceLoc = emitNodes.TryGet(source); if (sourceLoc.IsEmpty) return;
        var helpers = sourceLoc.Span[0].helpers; if (helpers.Length == 0) return;
        ref var targetData = ref emitNodes.Get(target); int removed = 0;
        for (int i = 0; i < helpers.Length; i++)
        {
            var helper = helpers[i];
            if (predicate(helper)) { removed++; targetData.helpers = Core.AppendIfUnique(targetData.helpers, helper); }
            else if (removed > 0) helpers[i - removed] = helper;
        }
        if (removed > 0) sourceLoc.Span[0].helpers = helpers.Slice2(0, helpers.Length - removed);
    }
    internal Slice<EmitHelper> GetEmitHelpers(Node node) { var loc = emitNodes.TryGet(node); return loc.IsEmpty ? default : loc.Span[0].helpers; }
    internal Node GetExternalHelpersModuleName(SourceFile node)
    {
        var parseNode = ParseNode(node.AsNode()); if (parseNode == null) return null;
        var loc = emitNodes.TryGet(parseNode); return loc.IsEmpty ? null : loc.Span[0].externalHelpersModuleName;
    }
    internal void SetExternalHelpersModuleName(SourceFile node, Node name)
    {
        var parseNode = ParseNode(node.AsNode());
        if (parseNode == null) throw new InvalidOperationException("Node must be a parse tree node or have an Original pointer to a parse tree node.");
        emitNodes.Get(parseNode).externalHelpersModuleName = name;
    }
    internal bool HasRecordedExternalHelpers(SourceFile node)
    {
        var parseNode = ParseNode(node.AsNode()); if (parseNode == null) return false;
        var loc = emitNodes.TryGet(parseNode);
        return !loc.IsEmpty && (loc.Span[0].externalHelpersModuleName != null || (loc.Span[0].emitFlags & TypeScript.DiagnosticPrinting.EmitFlags.ExternalHelpers) != 0);
    }
    internal bool IsCallToHelper(Node firstSegment, string helperName) => Ast.IsCallExpression(firstSegment) && Ast.IsIdentifier(firstSegment.Expression()) && (EmitFlags(firstSegment.Expression()) & TypeScript.DiagnosticPrinting.EmitFlags.HelperName) != 0 && firstSegment.Expression().TextValue().EqualsString(helperName);
    internal NodeList VisitVariableEnvironment(NodeList nodes, NodeVisitor visitor)
    { StartVariableEnvironment(); return EndAndMergeVariableEnvironmentList(visitor.VisitNodes(nodes)); }
    internal NodeList VisitParameters(NodeList nodes, NodeVisitor visitor)
    {
        StartVariableEnvironment(); var scope = varScopeStack.Peek(); var oldFlags = scope.flags;
        scope.flags |= environmentFlags.InParameters; nodes = visitor.VisitNodes(nodes);
        if ((scope.flags & environmentFlags.VariablesHoistedInParameters) != 0) nodes = addDefaultValueAssignmentsIfNeeded(nodes);
        scope.flags = oldFlags; return nodes;
    }
    private NodeList addDefaultValueAssignmentsIfNeeded(NodeList nodeList)
    {
        if (nodeList == null) return nodeList;
        Slice<Node> result = default; var nodes = nodeList.Nodes;
        for (int i = 0; i < nodes.Length; i++)
        {
            var parameter = nodes[i]; var updated = addDefaultValueAssignmentIfNeeded(parameter.AsParameterDeclaration());
            if (updated != parameter)
            { if (result.IsNil) result = GoSlices.Clone(nodes); result[i] = updated; }
        }
        if (result.IsNil) return nodeList;
        var list = Factory.NewNodeList(result); list.Loc = nodeList.Loc; return list;
    }
    private Node addDefaultValueAssignmentIfNeeded(ParameterDeclaration parameter)
    {
        if (parameter.DotDotDotToken != null) return parameter.AsNode();
        if (Ast.IsBindingPattern(parameter.Name())) return addDefaultValueAssignmentForBindingPattern(parameter);
        if (parameter.Initializer != null) return addDefaultValueAssignmentForInitializer(parameter, parameter.Name(), parameter.Initializer);
        return parameter.AsNode();
    }
    private Node addDefaultValueAssignmentForBindingPattern(ParameterDeclaration parameter)
    {
        Node initNode;
        if (parameter.Initializer != null)
            initNode = Factory.NewConditionalExpression(
                Factory.NewStrictEqualityExpression(Factory.NewGeneratedNameForNode(parameter.AsNode()), Factory.NewVoidZeroExpression()),
                Factory.NewToken(Kind.QuestionToken), parameter.Initializer, Factory.NewToken(Kind.ColonToken), Factory.NewGeneratedNameForNode(parameter.AsNode()));
        else initNode = Factory.NewGeneratedNameForNode(parameter.AsNode());
        AddInitializationStatement(Factory.NewVariableStatement(null,
            Factory.NewVariableDeclarationList(Factory.NewNodeList(Slice<Node>.FromArray([
                Factory.NewVariableDeclaration(parameter.Name(), null, parameter.Type, initNode)])), NodeFlags.None)));
        return Factory.UpdateParameterDeclaration(parameter, parameter.Modifiers(), parameter.DotDotDotToken,
            Factory.NewGeneratedNameForNode(parameter.AsNode()), parameter.QuestionToken, parameter.Type, null);
    }
    private Node addDefaultValueAssignmentForInitializer(ParameterDeclaration parameter, Node name, Node initializer)
    {
        AddEmitFlags(initializer, TypeScript.DiagnosticPrinting.EmitFlags.NoSourceMap | TypeScript.DiagnosticPrinting.EmitFlags.NoComments);
        var nameClone = name.Clone(Factory); AddEmitFlags(nameClone, TypeScript.DiagnosticPrinting.EmitFlags.NoSourceMap);
        var assignment = Factory.NewAssignmentExpression(nameClone, initializer); assignment.Loc = parameter.Loc;
        AddEmitFlags(assignment, TypeScript.DiagnosticPrinting.EmitFlags.NoComments);
        var block = Factory.NewBlock(Factory.NewNodeList(Slice<Node>.FromArray([Factory.NewExpressionStatement(assignment)])), false);
        block.Loc = parameter.Loc;
        AddEmitFlags(block, TypeScript.DiagnosticPrinting.EmitFlags.SingleLine | TypeScript.DiagnosticPrinting.EmitFlags.NoTrailingSourceMap | TypeScript.DiagnosticPrinting.EmitFlags.NoTokenSourceMaps | TypeScript.DiagnosticPrinting.EmitFlags.NoComments);
        AddInitializationStatement(Factory.NewIfStatement(Factory.NewTypeCheck(name.Clone(Factory), "undefined"), block, null));
        return Factory.UpdateParameterDeclaration(parameter, parameter.Modifiers(), parameter.DotDotDotToken,
            parameter.Name(), parameter.QuestionToken, parameter.Type, null);
    }
    internal void AddInitializationStatement(Node node)
    {
        var scope = varScopeStack.Peek();
        if (scope == null) throw new InvalidOperationException("Tried to add an initialization statement without a surrounding variable scope");
        AddEmitFlags(node, TypeScript.DiagnosticPrinting.EmitFlags.CustomPrologue);
        scope.initializationStatements = scope.initializationStatements.Append(node);
    }
    internal Node ConvertToFunctionBlock(Node node, bool multiLine)
    {
        if (Ast.IsBlock(node)) return node;
        var statement = Factory.NewReturnStatement(node); statement.Loc = node.Loc;
        var statements = Factory.NewNodeList(Slice<Node>.FromArray([statement])); statements.Loc = node.Loc;
        var block = Factory.NewBlock(statements, multiLine); block.Loc = node.Loc; return block;
    }
    internal Node VisitFunctionBody(Node node, NodeVisitor visitor)
    {
        var updated = visitor.VisitNode(node); var declarations = EndVariableEnvironment();
        if (declarations.Length == 0) return updated;
        if (updated == null) return Factory.NewBlock(Factory.NewNodeList(declarations), true);
        if (!Ast.IsBlock(updated))
        {
            AddEmitFlags(updated, TypeScript.DiagnosticPrinting.EmitFlags.NoComments);
            var block = ConvertToFunctionBlock(updated, false);
            return Factory.UpdateBlock(block.AsBlock(), MergeEnvironmentList(block.StatementList(), declarations), block.AsBlock().MultiLine);
        }
        return Factory.UpdateBlock(updated.AsBlock(), MergeEnvironmentList(updated.StatementList(), declarations), updated.AsBlock().MultiLine);
    }
    internal Node VisitIterationBody(Node body, NodeVisitor visitor)
    {
        if (body == null) return null;
        StartLexicalEnvironment(); var updated = VisitEmbeddedStatement(body, visitor);
        if (updated == null) throw new InvalidOperationException("Expected visitor to return a statement.");
        var statements = EndLexicalEnvironment();
        if (statements.Length > 0)
        {
            if (Ast.IsBlock(updated))
            {
                statements = statements.Append(updated.Statements()); var list = Factory.NewNodeList(statements);
                list.Loc = updated.StatementList().Loc; return Factory.UpdateBlock(updated.AsBlock(), list, updated.AsBlock().MultiLine);
            }
            return Factory.NewBlock(Factory.NewNodeList(statements.Append(updated)), true);
        }
        return updated;
    }
    internal Node VisitEmbeddedStatement(Node node, NodeVisitor visitor)
    {
        if (node == null) return null;
        var statement = visitor.VisitEmbeddedStatement(node);
        if (statement == null || Ast.IsNotEmittedStatement(statement))
        {
            var empty = visitor.Factory.NewEmptyStatement(); empty.Loc = node.Loc;
            SetOriginal(empty, node); AssignCommentRange(empty, node); return empty;
        }
        return statement;
    }
    internal Node SetSyntheticLeadingComments(Node node, Slice<SynthesizedComment> comments) { emitNodes.Get(node).leadingComments = comments; return node; }
    internal Node AddSyntheticLeadingComment(Node node, Kind kind, string text, bool hasTrailingNewLine)
    {
        ref var data = ref emitNodes.Get(node); data.leadingComments = data.leadingComments.Append(new SynthesizedComment(kind, new(-1, -1), false, hasTrailingNewLine, text)); return node;
    }
    internal Slice<SynthesizedComment> GetSyntheticLeadingComments(Node node) => emitNodes.Has(node) ? emitNodes.Get(node).leadingComments : default;
    internal Node SetSyntheticTrailingComments(Node node, Slice<SynthesizedComment> comments) { emitNodes.Get(node).trailingComments = comments; return node; }
    internal Node AddSyntheticTrailingComment(Node node, Kind kind, string text, bool hasTrailingNewLine)
    {
        ref var data = ref emitNodes.Get(node); data.trailingComments = data.trailingComments.Append(new SynthesizedComment(kind, new(-1, -1), false, hasTrailingNewLine, text)); return node;
    }
    internal Slice<SynthesizedComment> GetSyntheticTrailingComments(Node node) => emitNodes.Has(node) ? emitNodes.Get(node).trailingComments : default;
    internal void SetTypeNode(Node node, Node typeNode) => emitNodes.Get(node).typeNode = typeNode;
    internal Node GetTypeNode(Node node) { var loc = emitNodes.TryGet(node); return loc.IsEmpty ? null : loc.Span[0].typeNode; }
    internal Node NewNotEmittedStatement(Node node)
    {
        var statement = Factory.NewNotEmittedStatement(); statement.Loc = node.Loc;
        SetOriginal(statement, node); AssignCommentRange(statement, node); return statement;
    }
}
