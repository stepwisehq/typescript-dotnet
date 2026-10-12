// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// estransforms/esdecorator.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.Collections;
using TypeScript.DiagnosticPrinting;
using EmitNodeFactory = TypeScript.DiagnosticPrinting.NodeFactory;
using static TypeScript.Transformers.EsTransforms.EsTransforms;
namespace TypeScript.Transformers.EsTransforms;
internal sealed class ESDecoratorTransformer : Transformer
{
    private enum LexicalEntryKind { Class, ClassElement, Name, Other }
    private sealed class LexicalEntry
    {
        internal LexicalEntryKind kind;
        internal LexicalEntry next;
        internal ClassInfo classInfoData;
        internal Slice<Node> savedPendingExpressions;
        internal Node classThisData, classSuperData;
        internal int depth;
    }
    private sealed class MemberInfo
    {
        internal Node memberDecoratorsName, memberInitializersName, memberExtraInitializersName, memberDescriptorName;
    }
    private sealed class ClassInfo
    {
        internal Node @class, classDecoratorsName, classDescriptorName, classExtraInitializersName, classThis, classSuper, metadataReference;
        internal readonly OrderedMap<Node, MemberInfo> memberInfos = new();
        internal Node instanceMethodExtraInitializersName, staticMethodExtraInitializersName;
        internal Slice<Node> staticNonFieldDecorationStatements, nonStaticNonFieldDecorationStatements, staticFieldDecorationStatements, nonStaticFieldDecorationStatements;
        internal bool hasStaticInitializers, hasNonAmbientInstanceFields, hasStaticPrivateClassElements;
        internal Slice<Node> pendingStaticInitializers, pendingInstanceInitializers;
    }
    private Func<Node, ModifierList, Node> methodDescriptor, getAccessorDescriptor, setAccessorDescriptor, accessorPropertyDescriptor;
    // Upstream retains compiler options as transformer state even though this pass does not read it.
#pragma warning disable CS0414
    private CompilerOptions compilerOptions;
#pragma warning restore CS0414
    private LexicalEntry top;
    private ClassInfo classInfoStack;
    private Node classThis, classSuper, outerThis;
    private Slice<Node> pendingExpressions;
    private bool shouldTransformPrivateStaticElementsInFile;
    private NodeVisitor outerThisVisitor, discardedVisitor, modifierVisitor, exportStrippingModifierVisitor,
        classElementVisitor, nonConstructorClassElementVisitor, constructorClassElementVisitor,
        arrayAssignmentVisitor, objectAssignmentVisitor, staticOnlyModifierVisitor, asyncOnlyModifierVisitor, accessorStrippingModifierVisitor;
    // esdecorator.go:124.
    internal static Transformer NewESDecoratorTransformer(TransformOptions options)
    {
        if (options.CompilerOptions.ExperimentalDecorators == Tristate.True ||
            (options.CompilerOptions.GetEmitScriptTarget() >= ScriptTarget.ESNext && options.CompilerOptions.GetUseDefineForClassFields())) return null;
        var tx = new ESDecoratorTransformer { compilerOptions = options.CompilerOptions };
        var result = tx.NewTransformer(tx.visit, options.Context);
        tx.methodDescriptor = tx.createMethodDescriptorObject;
        tx.getAccessorDescriptor = tx.createGetAccessorDescriptorObject;
        tx.setAccessorDescriptor = tx.createSetAccessorDescriptorObject;
        tx.accessorPropertyDescriptor = tx.createAccessorPropertyDescriptorObject;
        var context = tx.EmitContext();
        tx.outerThisVisitor = context.NewNodeVisitor(tx.outerThisVisit);
        tx.discardedVisitor = context.NewNodeVisitor(tx.discardedValueVisit);
        tx.modifierVisitor = context.NewNodeVisitor(tx.modifierVisitorVisit);
        tx.exportStrippingModifierVisitor = context.NewNodeVisitor(tx.exportStrippingModifierVisit);
        tx.classElementVisitor = context.NewNodeVisitor(tx.classElementVisitorVisit);
        tx.nonConstructorClassElementVisitor = context.NewNodeVisitor(tx.nonConstructorClassElementVisit);
        tx.constructorClassElementVisitor = context.NewNodeVisitor(tx.constructorClassElementVisit);
        tx.arrayAssignmentVisitor = context.NewNodeVisitor(tx.visitArrayAssignmentElement);
        tx.objectAssignmentVisitor = context.NewNodeVisitor(tx.visitObjectAssignmentElement);
        tx.staticOnlyModifierVisitor = context.NewNodeVisitor(static node => node.Kind == Kind.StaticKeyword ? node : null);
        tx.asyncOnlyModifierVisitor = context.NewNodeVisitor(static node => node.Kind == Kind.AsyncKeyword ? node : null);
        tx.accessorStrippingModifierVisitor = context.NewNodeVisitor(static node => node.Kind == Kind.AccessorKeyword ? null : node);
        return result;
    }
    // esdecorator.go:165.
    private void updateState()
    {
        classInfoStack = null; classThis = null; classSuper = null;
        if (top == null) return;
        switch (top.kind)
        {
            case LexicalEntryKind.Class: classInfoStack = top.classInfoData; break;
            case LexicalEntryKind.ClassElement:
                classInfoStack = top.next.classInfoData; classThis = top.classThisData; classSuper = top.classSuperData; break;
            case LexicalEntryKind.Name:
                var grandparent = top.next.next.next;
                if (grandparent != null && grandparent.kind == LexicalEntryKind.ClassElement)
                {
                    classInfoStack = grandparent.next.classInfoData;
                    classThis = grandparent.classThisData; classSuper = grandparent.classSuperData;
                }
                break;
        }
    }
    // esdecorator.go:189.
    private void enterClass(ClassInfo info)
    {
        top = new LexicalEntry { kind = LexicalEntryKind.Class, next = top, classInfoData = info, savedPendingExpressions = pendingExpressions };
        pendingExpressions = default;
        updateState();
    }
    // esdecorator.go:200.
    private void exitClass()
    {
        if (top == null || top.kind != LexicalEntryKind.Class) throw new InvalidOperationException("Expected class lexical entry");
        pendingExpressions = top.savedPendingExpressions;
        top = top.next;
        updateState();
    }
    // esdecorator.go:207.
    private void enterClassElement(Node node)
    {
        if (top == null || top.kind != LexicalEntryKind.Class) throw new InvalidOperationException("Expected class lexical entry");
        top = new LexicalEntry { kind = LexicalEntryKind.ClassElement, next = top };
        if (Ast.IsClassStaticBlockDeclaration(node) || Ast.IsPropertyDeclaration(node) && Ast.HasStaticModifier(node))
            if (top.next.classInfoData != null)
            {
                top.classThisData = top.next.classInfoData.classThis;
                top.classSuperData = top.next.classInfoData.classSuper;
            }
        updateState();
    }
    // esdecorator.go:222.
    private void exitClassElement()
    {
        if (top == null || top.kind != LexicalEntryKind.ClassElement || top.next == null || top.next.kind != LexicalEntryKind.Class)
            throw new InvalidOperationException("Expected class-element lexical entry over class");
        top = top.next; updateState();
    }
    // esdecorator.go:229.
    private void enterName()
    {
        if (top == null || top.kind != LexicalEntryKind.ClassElement) throw new InvalidOperationException("Expected class-element lexical entry");
        top = new LexicalEntry { kind = LexicalEntryKind.Name, next = top }; updateState();
    }
    // esdecorator.go:238.
    private void exitName()
    {
        if (top == null || top.kind != LexicalEntryKind.Name) throw new InvalidOperationException("Expected name lexical entry");
        top = top.next; updateState();
    }
    // esdecorator.go:244.
    private void enterOther()
    {
        if (top != null && top.kind == LexicalEntryKind.Other)
        {
            if (pendingExpressions.Count != 0) throw new InvalidOperationException("Expected no pending expressions");
            top.depth++;
        }
        else
        {
            top = new LexicalEntry { kind = LexicalEntryKind.Other, next = top, savedPendingExpressions = pendingExpressions };
            pendingExpressions = default; updateState();
        }
    }
    // esdecorator.go:259.
    private void exitOther()
    {
        if (top == null || top.kind != LexicalEntryKind.Other) throw new InvalidOperationException("Expected other lexical entry");
        if (top.depth > 0)
        {
            if (pendingExpressions.Count != 0) throw new InvalidOperationException("Expected no pending expressions");
            top.depth--;
        }
        else { pendingExpressions = top.savedPendingExpressions; top = top.next; updateState(); }
    }
    // esdecorator.go:271.
    private Node visitSourceFile(SourceFile node)
    {
        top = null; shouldTransformPrivateStaticElementsInFile = false;
        var visited = Visitor().VisitEachChild(node.AsNode());
        EmitContext().AddEmitHelper(visited, EmitContext().ReadEmitHelpers());
        if (shouldTransformPrivateStaticElementsInFile)
        {
            EmitContext().AddEmitFlags(visited, EmitFlags.TransformPrivateStaticElements);
            shouldTransformPrivateStaticElementsInFile = false;
        }
        return visited;
    }
    // esdecorator.go:283.
    private Node outerThisVisit(Node node)
    {
        if (((uint)node.SubtreeFacts() & SubtreeFactValues.ContainsLexicalThis) == 0 && node.Kind != Kind.ThisKeyword) return node;
        if (node.Kind == Kind.ThisKeyword)
        {
            outerThis ??= Factory().NewUniqueNameEx("_outerThis", new AutoGenerateOptions { Flags = GeneratedIdentifierFlags.Optimistic });
            return outerThis;
        }
        return outerThisVisitor.VisitEachChild(node);
    }
    // esdecorator.go:298.
    private bool shouldVisitNode(Node node) => ((uint)node.SubtreeFacts() & SubtreeFactValues.ContainsDecorators) != 0 ||
        classThis != null && ((uint)node.SubtreeFacts() & SubtreeFactValues.ContainsLexicalThis) != 0 ||
        classThis != null && classSuper != null && ((uint)node.SubtreeFacts() & SubtreeFactValues.ContainsLexicalSuper) != 0;
    // esdecorator.go:304.
    private Node visit(Node node)
    {
        if (node.Kind == Kind.SourceFile) return visitSourceFile(node.AsSourceFile());
        if (!shouldVisitNode(node)) return node;
        switch (node.Kind)
        {
            case Kind.Decorator: return null;
            case Kind.ClassDeclaration: return visitClassDeclaration(node.AsClassDeclaration());
            case Kind.ClassExpression: return visitClassExpression(node.AsClassExpression());
            case Kind.Constructor: case Kind.PropertyDeclaration: case Kind.ClassStaticBlockDeclaration:
                throw new InvalidOperationException("Not supported outside a class; use classElementVisitor");
            case Kind.Parameter: return visitParameterDeclaration(node.AsParameterDeclaration());
            case Kind.BinaryExpression: return visitBinaryExpression(node, false);
            case Kind.PropertyAssignment: case Kind.VariableDeclaration: case Kind.BindingElement: return visitNamedEvaluationSite(node, node.Initializer());
            case Kind.ExportAssignment: return visitExportAssignment(node);
            case Kind.ThisKeyword: return visitThisExpression(node);
            case Kind.ForStatement: return visitForStatement(node);
            case Kind.ExpressionStatement: return visitExpressionStatement(node);
            case Kind.ParenthesizedExpression: return visitParenthesizedExpression(node, false);
            case Kind.PartiallyEmittedExpression: return visitPartiallyEmittedExpression(node, false);
            case Kind.CallExpression: return visitCallExpression(node);
            case Kind.TaggedTemplateExpression: return visitTaggedTemplateExpression(node);
            case Kind.PrefixUnaryExpression: case Kind.PostfixUnaryExpression: return visitPreOrPostfixUnaryExpression(node, false);
            case Kind.PropertyAccessExpression: return visitPropertyAccessExpression(node);
            case Kind.ElementAccessExpression: return visitElementAccessExpression(node);
            case Kind.ComputedPropertyName: return visitComputedPropertyName(node);
            case Kind.MethodDeclaration: case Kind.SetAccessor: case Kind.GetAccessor: case Kind.FunctionExpression: case Kind.FunctionDeclaration:
                enterOther();
                var result = Visitor().VisitEachChild(node);
                exitOther();
                return result;
            default: return Visitor().VisitEachChild(node);
        }
    }
    // esdecorator.go:369.
    private Node modifierVisitorVisit(Node node) => node.Kind == Kind.Decorator ? null : node;
    // esdecorator.go:376.
    private Node classElementVisitorVisit(Node node) => node.Kind switch
    {
        Kind.Constructor => visitConstructorDeclaration(node),
        Kind.MethodDeclaration => visitMethodDeclaration(node),
        Kind.GetAccessor => visitGetAccessorDeclaration(node),
        Kind.SetAccessor => visitSetAccessorDeclaration(node),
        Kind.PropertyDeclaration => visitPropertyDeclaration(node),
        Kind.ClassStaticBlockDeclaration => visitClassStaticBlockDeclaration(node),
        _ => visit(node)
    };
    // esdecorator.go:395.
    private Node discardedValueVisit(Node node) => node.Kind switch
    {
        Kind.PrefixUnaryExpression or Kind.PostfixUnaryExpression => visitPreOrPostfixUnaryExpression(node, true),
        Kind.BinaryExpression => visitBinaryExpression(node, true),
        Kind.ParenthesizedExpression => visitParenthesizedExpression(node, true),
        Kind.PartiallyEmittedExpression => visitPartiallyEmittedExpression(node, true),
        _ => visit(node)
    };
    // esdecorator.go:410.
    private Node nonConstructorClassElementVisit(Node node) => Ast.IsConstructorDeclaration(node) ? node : classElementVisitorVisit(node);
    // esdecorator.go:417.
    private Node constructorClassElementVisit(Node node) => Ast.IsConstructorDeclaration(node) ? classElementVisitorVisit(node) : node;
    // esdecorator.go:424.
    private Node exportStrippingModifierVisit(Node node) => node.Kind == Kind.ExportKeyword ? null : modifierVisitorVisit(node);
    // esdecorator.go:431.
    private static ByteStringValue getHelperVariableName(EmitContext context, Node node)
    {
        var name = node.Name();
        ByteStringValue declarationName = default;
        if (name != null && Ast.IsIdentifier(name) && !Transformers.IsGeneratedIdentifier(context, name))
            declarationName = name.TextValue();
        else if (name != null && Ast.IsPrivateIdentifier(name) && !context.HasAutoGenerateInfo(name))
        {
            var text = name.TextValue();
            if (text.Length > 1) declarationName = text.Slice(1);
        }
        else if (name != null && Ast.IsStringLiteral(name) && TypeScript.Scanner.Scanner.IsIdentifierTextByteString(name.TextValue(), LanguageVariant.Standard))
            declarationName = name.TextValue();
        else if (Ast.IsClassLike(node)) declarationName = ByteStringValue.FromString("class");
        else declarationName = ByteStringValue.FromString("member");
        if (Ast.IsGetAccessorDeclaration(node)) declarationName = ByteStringValue.Concat(ByteStringValue.FromString("get_"), declarationName);
        if (Ast.IsSetAccessorDeclaration(node)) declarationName = ByteStringValue.Concat(ByteStringValue.FromString("set_"), declarationName);
        if (name != null && Ast.IsPrivateIdentifier(name)) declarationName = ByteStringValue.Concat(ByteStringValue.FromString("private_"), declarationName);
        if (Ast.IsStatic(node)) declarationName = ByteStringValue.Concat(ByteStringValue.FromString("static_"), declarationName);
        return ByteStringValue.Concat(ByteStringValue.FromString("_"), declarationName);
    }
    // esdecorator.go:464.
    private Node createHelperVariable(Node node, ByteStringValue suffix) => Factory().NewUniqueNameEx(
        ByteStringValue.Concat(getHelperVariableName(EmitContext(), node), ByteStringValue.FromString("_"), suffix),
        new AutoGenerateOptions { Flags = GeneratedIdentifierFlags.Optimistic | GeneratedIdentifierFlags.ReservedInNestedScopes });
    // esdecorator.go:471.
    private Node createLet(Node name, Node initializer) => Factory().NewVariableStatement(null,
        Factory().NewVariableDeclarationList(Factory().NewNodeList(Slice<Node>.FromArray([
            Factory().NewVariableDeclaration(name, null, null, initializer)])), NodeFlags.Let));
    // esdecorator.go:483.
    private ClassInfo createClassInfo(Node node)
    {
        var factory = Factory();
        var info = new ClassInfo { @class = node, metadataReference = factory.NewUniqueNameEx("_metadata",
            new AutoGenerateOptions { Flags = GeneratedIdentifierFlags.Optimistic | GeneratedIdentifierFlags.FileLevel }) };
        if (Ast.NodeIsDecorated(false, node, null, null))
        {
            bool needsUniqueClassThis = false;
            foreach (var member in node.Members())
                if ((Ast.IsPrivateIdentifierClassElementDeclaration(member) || Ast.IsAutoAccessorPropertyDeclaration(member)) && Ast.HasStaticModifier(member))
                { needsUniqueClassThis = true; break; }
            var flags = GeneratedIdentifierFlags.Optimistic | GeneratedIdentifierFlags.FileLevel;
            if (needsUniqueClassThis) flags = GeneratedIdentifierFlags.Optimistic | GeneratedIdentifierFlags.ReservedInNestedScopes;
            info.classThis = factory.NewUniqueNameEx("_classThis", new AutoGenerateOptions { Flags = flags });
        }
        foreach (var member in node.Members())
        {
            if (Ast.IsMethodOrAccessor(member) && Ast.NodeOrChildIsDecorated(false, member, node, null))
            {
                if (Ast.HasStaticModifier(member))
                {
                    if (info.staticMethodExtraInitializersName == null)
                    {
                        info.staticMethodExtraInitializersName = factory.NewUniqueNameEx("_staticExtraInitializers",
                            new AutoGenerateOptions { Flags = GeneratedIdentifierFlags.Optimistic | GeneratedIdentifierFlags.FileLevel });
                        var renamedClassThis = info.classThis ?? factory.NewThisExpression();
                        var initializer = factory.NewRunInitializersHelper(renamedClassThis, info.staticMethodExtraInitializersName, null);
                        if (node.Name() != null) EmitContext().SetSourceMapRange(initializer, node.Name().Loc);
                        else EmitContext().SetSourceMapRange(initializer, Transformers.MoveRangePastDecorators(node));
                        info.pendingStaticInitializers = info.pendingStaticInitializers.Append(initializer);
                    }
                }
                else if (info.instanceMethodExtraInitializersName == null)
                {
                    info.instanceMethodExtraInitializersName = factory.NewUniqueNameEx("_instanceExtraInitializers",
                        new AutoGenerateOptions { Flags = GeneratedIdentifierFlags.Optimistic | GeneratedIdentifierFlags.FileLevel });
                    var initializer = factory.NewRunInitializersHelper(factory.NewThisExpression(), info.instanceMethodExtraInitializersName, null);
                    if (node.Name() != null) EmitContext().SetSourceMapRange(initializer, node.Name().Loc);
                    else EmitContext().SetSourceMapRange(initializer, Transformers.MoveRangePastDecorators(node));
                    info.pendingInstanceInitializers = info.pendingInstanceInitializers.Append(initializer);
                }
            }
            if (Ast.IsClassStaticBlockDeclaration(member))
            {
                if (!IsClassNamedEvaluationHelperBlock(EmitContext(), member)) info.hasStaticInitializers = true;
            }
            else if (Ast.IsPropertyDeclaration(member))
            {
                if (Ast.HasStaticModifier(member)) info.hasStaticInitializers |= member.Initializer() != null || Ast.HasDecorators(member);
                else info.hasNonAmbientInstanceFields |= !Ast.HasSyntacticModifier(member, ModifierFlags.Ambient);
            }
            if ((Ast.IsPrivateIdentifierClassElementDeclaration(member) || Ast.IsAutoAccessorPropertyDeclaration(member)) && Ast.HasStaticModifier(member))
                info.hasStaticPrivateClassElements = true;
            if (info.staticMethodExtraInitializersName != null && info.instanceMethodExtraInitializersName != null &&
                info.hasStaticInitializers && info.hasNonAmbientInstanceFields && info.hasStaticPrivateClassElements) break;
        }
        return info;
    }
    // esdecorator.go:993.
    private Slice<Node> emitMemberInfoDeclarations(ClassInfo info, bool isStatic)
    {
        var factory = Factory();
        Slice<Node> statements = default;
        foreach (var (member, memberInfo) in info.memberInfos.Entries())
        {
            if (Ast.IsStatic(member) != isStatic) continue;
            statements = statements.Append(createLet(memberInfo.memberDecoratorsName, null));
            if (memberInfo.memberInitializersName != null)
                statements = statements.Append(createLet(memberInfo.memberInitializersName, factory.NewArrayLiteralExpression(factory.NewNodeList(default(Slice<Node>)), false)));
            if (memberInfo.memberExtraInitializersName != null)
                statements = statements.Append(createLet(memberInfo.memberExtraInitializersName, factory.NewArrayLiteralExpression(factory.NewNodeList(default(Slice<Node>)), false)));
            if (memberInfo.memberDescriptorName != null) statements = statements.Append(createLet(memberInfo.memberDescriptorName, null));
        }
        return statements;
    }
    // esdecorator.go:1014.
    private static bool isDecoratedClassLike(Node node) => Ast.ClassOrConstructorParameterIsDecorated(false, node) || Ast.ChildIsDecorated(false, node, null);
    // esdecorator.go:1019.
    private Node visitClassDeclaration(ClassDeclaration node)
    {
        if (isDecoratedClassLike(node.AsNode()))
        {
            var factory = Factory(); var context = EmitContext();
            var statements = Slice<Node>.Make(0);
            var originalClass = context.MostOriginal(node.AsNode());
            if (!Ast.IsClassLike(originalClass)) originalClass = node.AsNode();
            var className = originalClass.Name() != null ? factory.NewStringLiteralFromNode(originalClass.Name()) : factory.NewStringLiteral("default", TokenFlags.None);
            bool isExport = Ast.HasSyntacticModifier(node.AsNode(), ModifierFlags.Export);
            bool isDefault = Ast.HasSyntacticModifier(node.AsNode(), ModifierFlags.Default);
            var classNode = node.AsNode();
            if (node.Name() == null) classNode = InjectClassNamedEvaluationHelperBlockIfMissing(context, classNode, className, null);
            if (isExport && isDefault)
            {
                var iife = transformClassLike(classNode);
                if (classNode.Name() != null)
                {
                    var declaration = factory.NewVariableDeclaration(factory.GetLocalName(classNode), null, null, iife);
                    context.SetOriginal(declaration, classNode);
                    var declarations = factory.NewVariableDeclarationList(factory.NewNodeList(Slice<Node>.FromArray([declaration])), NodeFlags.Let);
                    statements = statements.Append(factory.NewVariableStatement(null, declarations));
                    var exportStatement = factory.NewExportDefault(factory.GetDeclarationName(classNode));
                    context.SetOriginal(exportStatement, classNode); context.AssignCommentRange(exportStatement, classNode);
                    context.SetSourceMapRange(exportStatement, Transformers.MoveRangePastDecorators(classNode));
                    statements = statements.Append(exportStatement);
                }
                else
                {
                    var exportStatement = factory.NewExportDefault(iife);
                    context.SetOriginal(exportStatement, classNode); context.AssignCommentRange(exportStatement, classNode);
                    context.SetSourceMapRange(exportStatement, Transformers.MoveRangePastDecorators(classNode));
                    statements = statements.Append(exportStatement);
                }
            }
            else
            {
                if (classNode.Name() == null) throw new InvalidOperationException("Non-default class declaration must have a name");
                var iife = transformClassLike(classNode);
                var modifiers = exportStrippingModifierVisitor.VisitModifiers(classNode.Modifiers());
                var declarationName = factory.GetLocalNameEx(classNode, new AssignedNameOptions { AllowSourceMaps = true });
                var declaration = factory.NewVariableDeclaration(declarationName, null, null, iife);
                context.SetOriginal(declaration, classNode);
                var declarations = factory.NewVariableDeclarationList(factory.NewNodeList(Slice<Node>.FromArray([declaration])), NodeFlags.Let);
                var statement = factory.NewVariableStatement(modifiers, declarations);
                context.SetOriginal(statement, classNode); context.AssignCommentRange(statement, classNode);
                statements = statements.Append(statement);
                if (isExport)
                {
                    var exportStatement = factory.NewExternalModuleExport(declarationName);
                    context.SetOriginal(exportStatement, classNode); statements = statements.Append(exportStatement);
                }
            }
            return Transformers.SingleOrMany(statements, factory);
        }
        var updatedModifiers = modifierVisitor.VisitModifiers(node.Modifiers());
        var heritageClauses = Visitor().VisitNodes(node.HeritageClauses);
        enterClass(null);
        var members = classElementVisitor.VisitNodes(node.Members);
        exitClass();
        return Factory().UpdateClassDeclaration(node, updatedModifiers, node.Name(), null, heritageClauses, members);
    }
    // esdecorator.go:1107.
    private Node visitClassExpression(ClassExpression node)
    {
        if (isDecoratedClassLike(node.AsNode()))
        {
            var iife = transformClassLike(node.AsNode());
            EmitContext().SetOriginal(iife, node.AsNode()); return iife;
        }
        var modifiers = modifierVisitor.VisitModifiers(node.Modifiers());
        var heritageClauses = Visitor().VisitNodes(node.HeritageClauses);
        enterClass(null); var members = classElementVisitor.VisitNodes(node.Members); exitClass();
        return Factory().UpdateClassExpression(node, modifiers, node.Name(), null, heritageClauses, members);
    }
    // esdecorator.go:1122.
    private Slice<Node> prepareConstructor(ClassInfo info)
    {
        if (info.pendingInstanceInitializers.Count == 0) return default;
        var statements = Slice<Node>.FromArray([Factory().NewExpressionStatement(Factory().InlineExpressions(info.pendingInstanceInitializers))]);
        info.pendingInstanceInitializers = default;
        return statements;
    }
    // esdecorator.go:1138.
    private Slice<Node> transformConstructorBodyWorker(Slice<Node> statementsOut, Slice<Node> statementsIn, int statementOffset, Slice<int> superPath, int superPathDepth, Slice<Node> initializers)
    {
        int superIndex = superPath[superPathDepth];
        if (superIndex > statementOffset)
            foreach (var statement in statementsIn.Slice2(statementOffset, superIndex)) statementsOut = statementsOut.Append(Visitor().VisitNode(statement));
        var superStatement = statementsIn[superIndex];
        if (Ast.IsTryStatement(superStatement))
        {
            var tryNode = superStatement.AsTryStatement().TryBlock;
            var tryStatements = transformConstructorBodyWorker(default, tryNode.AsBlock().Statements.Nodes, 0, superPath, superPathDepth + 1, initializers);
            var newTryBlock = Factory().NewBlock(Factory().NewNodeList(tryStatements), true);
            newTryBlock.Loc = tryNode.Loc;
            Node catchClause = null, finallyBlock = null;
            if (superStatement.AsTryStatement().CatchClause != null) catchClause = Visitor().VisitNode(superStatement.AsTryStatement().CatchClause);
            if (superStatement.AsTryStatement().FinallyBlock != null) finallyBlock = Visitor().VisitNode(superStatement.AsTryStatement().FinallyBlock);
            statementsOut = statementsOut.Append(Factory().UpdateTryStatement(superStatement.AsTryStatement(), newTryBlock, catchClause, finallyBlock));
        }
        else
        {
            statementsOut = statementsOut.Append(Visitor().VisitNode(superStatement));
            statementsOut = statementsOut.Append(initializers);
        }
        if (superIndex + 1 < statementsIn.Count)
            foreach (var statement in statementsIn.Slice2(superIndex + 1, statementsIn.Count)) statementsOut = statementsOut.Append(Visitor().VisitNode(statement));
        return statementsOut;
    }
    // esdecorator.go:1184.
    private Node visitConstructorDeclaration(Node node)
    {
        enterClassElement(node);
        var modifiers = modifierVisitor.VisitModifiers(node.Modifiers());
        var parameters = Visitor().VisitNodes(node.ParameterList());
        Node body = null;
        var constructor = node.AsConstructorDeclaration();
        if (constructor.Body != null && classInfoStack != null)
        {
            var initializers = prepareConstructor(classInfoStack);
            if (initializers.Count > 0)
            {
                var statements = Slice<Node>.Make(0);
                var (prologue, rest) = Factory().SplitStandardPrologue(constructor.Body.AsBlock().Statements.Nodes);
                statements = statements.Append(prologue);
                var superPath = Transformers.FindSuperStatementIndexPath(rest, 0);
                if (superPath.Count > 0) statements = transformConstructorBodyWorker(statements, rest, 0, superPath, 0, initializers);
                else
                {
                    statements = statements.Append(initializers);
                    statements = statements.Append(Visitor().VisitSlice(rest).Item1);
                }
                body = Factory().NewBlock(Factory().NewNodeList(statements), true);
                EmitContext().SetOriginal(body, constructor.Body); body.Loc = constructor.Body.Loc;
            }
        }
        body ??= Visitor().VisitNode(constructor.Body);
        exitClassElement();
        return Factory().UpdateConstructorDeclaration(constructor, modifiers, null, parameters, null, null, body);
    }
    // esdecorator.go:1222.
    private Node finishClassElement(Node updated, Node original)
    {
        if (updated != original)
        {
            EmitContext().AssignCommentRange(updated, original);
            EmitContext().SetSourceMapRange(updated, Transformers.MoveRangePastDecorators(original));
        }
        return updated;
    }
    private struct PartialResult
    {
        internal ModifierList modifiers;
        internal Node referencedName, name, initializersName, extraInitializersName, descriptorName, thisArg;
    }
    // esdecorator.go:1244.
    private PartialResult partialTransformClassElement(Node member, ClassInfo info, Func<Node, ModifierList, Node> createDescriptor)
    {
        var factory = Factory(); var context = EmitContext();
        if (info == null)
        {
            var unmodified = modifierVisitor.VisitModifiers(member.Modifiers());
            enterName(); var name = visitPropertyName(member.Name()); exitName();
            return new PartialResult { modifiers = unmodified, name = name };
        }
        var savedClassThis = classThis; classThis = null;
        var decorators = transformAllDecoratorsOfDeclaration(member.Decorators());
        classThis = savedClassThis;
        var modifiers = modifierVisitor.VisitModifiers(member.Modifiers());
        var result = new PartialResult { modifiers = modifiers };
        if (decorators.Count > 0)
        {
            var decoratorsName = createHelperVariable(member, ByteStringValue.FromString("decorators"));
            var decoratorsArray = factory.NewArrayLiteralExpression(factory.NewNodeList(decorators), false);
            var assignment = factory.NewAssignmentExpression(decoratorsName, decoratorsArray);
            var memberInfo = new MemberInfo { memberDecoratorsName = decoratorsName };
            info.memberInfos.Set(member, memberInfo);
            pendingExpressions = pendingExpressions.Append(assignment);
            string kind;
            if (Ast.IsGetAccessorDeclaration(member)) kind = "getter";
            else if (Ast.IsSetAccessorDeclaration(member)) kind = "setter";
            else if (Ast.IsMethodDeclaration(member)) kind = "method";
            else if (Ast.IsAutoAccessorPropertyDeclaration(member)) kind = "accessor";
            else if (Ast.IsPropertyDeclaration(member)) kind = "field";
            else throw new InvalidOperationException("Unexpected class element kind");
            bool computed = false;
            Node propertyName = null;
            var memberName = member.Name();
            if (memberName != null && (Ast.IsIdentifier(memberName) || Ast.IsPrivateIdentifier(memberName))) propertyName = memberName;
            else if (memberName != null && Ast.IsPropertyNameLiteral(memberName))
            { computed = true; propertyName = factory.NewStringLiteralFromNode(memberName); }
            else if (memberName != null && Ast.IsComputedPropertyName(memberName))
            {
                var nameExpression = memberName.AsComputedPropertyName().Expression;
                if (Ast.IsPropertyNameLiteral(nameExpression) && !Ast.IsIdentifier(nameExpression))
                { computed = true; propertyName = factory.NewStringLiteralFromNode(nameExpression); }
                else
                {
                    enterName(); (result.referencedName, result.name) = visitReferencedPropertyName(memberName); exitName();
                    computed = true; propertyName = result.referencedName;
                }
            }
            var contextObject = factory.NewESDecorateClassElementContextObject(kind, computed, propertyName,
                Ast.IsStatic(member), memberName != null && Ast.IsPrivateIdentifier(memberName),
                Ast.IsPropertyDeclaration(member) || Ast.IsGetAccessorDeclaration(member) || Ast.IsMethodDeclaration(member),
                Ast.IsPropertyDeclaration(member) || Ast.IsSetAccessorDeclaration(member), info.metadataReference);
            if (Ast.IsMethodOrAccessor(member))
            {
                var extraInitializers = Ast.IsStatic(member) ? info.staticMethodExtraInitializersName : info.instanceMethodExtraInitializersName;
                if (extraInitializers == null) throw new InvalidOperationException("Expected method extra initializers");
                Node descriptorArgument;
                if (Ast.IsPrivateIdentifierClassElementDeclaration(member) && createDescriptor != null)
                {
                    var asyncModifiers = asyncOnlyModifierVisitor.VisitModifiers(modifiers);
                    var descriptor = createDescriptor(member, asyncModifiers);
                    memberInfo.memberDescriptorName = createHelperVariable(member, ByteStringValue.FromString("descriptor"));
                    result.descriptorName = memberInfo.memberDescriptorName;
                    descriptorArgument = factory.NewAssignmentExpression(memberInfo.memberDescriptorName, descriptor);
                }
                else descriptorArgument = factory.NewToken(Kind.NullKeyword);
                var decorate = factory.NewESDecorateHelper(factory.NewThisExpression(), descriptorArgument,
                    decoratorsName, contextObject, factory.NewToken(Kind.NullKeyword), extraInitializers);
                var statement = factory.NewExpressionStatement(decorate);
                context.SetSourceMapRange(statement, Transformers.MoveRangePastDecorators(member));
                appendDecorationStatement(info, member, statement);
            }
            else if (Ast.IsPropertyDeclaration(member))
            {
                memberInfo.memberInitializersName = createHelperVariable(member, ByteStringValue.FromString("initializers"));
                memberInfo.memberExtraInitializersName = createHelperVariable(member, ByteStringValue.FromString("extraInitializers"));
                result.initializersName = memberInfo.memberInitializersName;
                result.extraInitializersName = memberInfo.memberExtraInitializersName;
                if (Ast.IsStatic(member)) result.thisArg = info.classThis;
                var constructorArgument = Ast.IsAutoAccessorPropertyDeclaration(member) ? factory.NewThisExpression() : factory.NewToken(Kind.NullKeyword);
                Node descriptorArgument;
                if (Ast.IsPrivateIdentifierClassElementDeclaration(member) && Ast.HasAccessorModifier(member) && createDescriptor != null)
                {
                    var descriptor = createDescriptor(member, null);
                    memberInfo.memberDescriptorName = createHelperVariable(member, ByteStringValue.FromString("descriptor"));
                    result.descriptorName = memberInfo.memberDescriptorName;
                    descriptorArgument = factory.NewAssignmentExpression(memberInfo.memberDescriptorName, descriptor);
                }
                else descriptorArgument = factory.NewToken(Kind.NullKeyword);
                var decorate = factory.NewESDecorateHelper(constructorArgument, descriptorArgument, decoratorsName,
                    contextObject, memberInfo.memberInitializersName, memberInfo.memberExtraInitializersName);
                var statement = factory.NewExpressionStatement(decorate);
                context.SetSourceMapRange(statement, Transformers.MoveRangePastDecorators(member));
                appendDecorationStatement(info, member, statement);
            }
        }
        if (result.name == null) { enterName(); result.name = visitPropertyName(member.Name()); exitName(); }
        if ((modifiers == null || modifiers.Nodes.Count == 0) && (Ast.IsMethodDeclaration(member) || Ast.IsPropertyDeclaration(member)))
            context.SetEmitFlags(result.name, EmitFlags.NoLeadingComments);
        return result;
    }
    // esdecorator.go:1447.
    private void appendDecorationStatement(ClassInfo info, Node member, Node statement)
    {
        if (Ast.IsMethodOrAccessor(member) || Ast.IsAutoAccessorPropertyDeclaration(member))
        {
            if (Ast.IsStatic(member)) info.staticNonFieldDecorationStatements = info.staticNonFieldDecorationStatements.Append(statement);
            else info.nonStaticNonFieldDecorationStatements = info.nonStaticNonFieldDecorationStatements.Append(statement);
        }
        else if (Ast.IsPropertyDeclaration(member) && !Ast.IsAutoAccessorPropertyDeclaration(member))
        {
            if (Ast.IsStatic(member)) info.staticFieldDecorationStatements = info.staticFieldDecorationStatements.Append(statement);
            else info.nonStaticFieldDecorationStatements = info.nonStaticFieldDecorationStatements.Append(statement);
        }
        else throw new InvalidOperationException("Unexpected class element kind");
    }
    // esdecorator.go:1465.
    private Node visitMethodDeclaration(Node node)
    {
        enterClassElement(node);
        var result = partialTransformClassElement(node, classInfoStack, methodDescriptor);
        if (result.descriptorName != null)
        {
            exitClassElement();
            return finishClassElement(createMethodDescriptorForwarder(result.modifiers, result.name, result.descriptorName), node);
        }
        var parameters = Visitor().VisitNodes(node.ParameterList()); var body = Visitor().VisitNode(node.Body());
        exitClassElement(); var method = node.AsMethodDeclaration();
        return finishClassElement(Factory().UpdateMethodDeclaration(method, result.modifiers, method.AsteriskToken, result.name, null, null, parameters, null, null, body), node);
    }
    // esdecorator.go:1482.
    private Node visitGetAccessorDeclaration(Node node)
    {
        enterClassElement(node);
        var result = partialTransformClassElement(node, classInfoStack, getAccessorDescriptor);
        if (result.descriptorName != null)
        {
            exitClassElement();
            return finishClassElement(createGetAccessorDescriptorForwarder(result.modifiers, result.name, result.descriptorName), node);
        }
        var parameters = Visitor().VisitNodes(node.ParameterList()); var body = Visitor().VisitNode(node.Body());
        exitClassElement();
        return finishClassElement(Factory().UpdateGetAccessorDeclaration(node.AsGetAccessorDeclaration(), result.modifiers, result.name, null, parameters, null, null, body), node);
    }
    // esdecorator.go:1499.
    private Node visitSetAccessorDeclaration(Node node)
    {
        enterClassElement(node);
        var result = partialTransformClassElement(node, classInfoStack, setAccessorDescriptor);
        if (result.descriptorName != null)
        {
            exitClassElement();
            return finishClassElement(createSetAccessorDescriptorForwarder(result.modifiers, result.name, result.descriptorName), node);
        }
        var parameters = Visitor().VisitNodes(node.ParameterList()); var body = Visitor().VisitNode(node.Body());
        exitClassElement();
        return finishClassElement(Factory().UpdateSetAccessorDeclaration(node.AsSetAccessorDeclaration(), result.modifiers, result.name, null, parameters, null, null, body), node);
    }
    // esdecorator.go:1516.
    private Node visitClassStaticBlockDeclaration(Node node)
    {
        enterClassElement(node);
        var factory = Factory(); Node result;
        if (IsClassNamedEvaluationHelperBlock(EmitContext(), node))
        {
            result = Visitor().VisitEachChild(node);
            var assignedName = EmitContext().AssignedName(node);
            if (assignedName != null && result != node) EmitContext().SetAssignedName(result, assignedName);
        }
        else if (IsClassThisAssignmentBlock(EmitContext(), node))
        {
            var savedClassThis = classThis; classThis = null;
            result = Visitor().VisitEachChild(node); classThis = savedClassThis;
        }
        else
        {
            var context = EmitContext(); context.StartVariableEnvironment();
            result = Visitor().VisitEachChild(node);
            var declarations = context.EndVariableEnvironment();
            if (declarations.Count > 0)
            {
                var body = result.AsClassStaticBlockDeclaration().Body.AsBlock();
                var statements = Slice<Node>.Make(0, declarations.Count + body.Statements.Nodes.Count);
                statements = statements.Append(declarations).Append(body.Statements.Nodes);
                result = factory.NewClassStaticBlockDeclaration(null, factory.NewBlock(factory.NewNodeList(statements), body.MultiLine));
            }
            if (classInfoStack != null)
            {
                classInfoStack.hasStaticInitializers = true;
                if (classInfoStack.pendingStaticInitializers.Count > 0)
                {
                    var statements = Slice<Node>.Make(0);
                    foreach (var initializer in classInfoStack.pendingStaticInitializers)
                    {
                        var statement = factory.NewExpressionStatement(initializer);
                        context.SetSourceMapRange(statement, context.SourceMapRange(initializer));
                        statements = statements.Append(statement);
                    }
                    var staticBlock = factory.NewClassStaticBlockDeclaration(null, factory.NewBlock(factory.NewNodeList(statements), true));
                    classInfoStack.pendingStaticInitializers = default;
                    exitClassElement();
                    return Transformers.SingleOrMany(Slice<Node>.FromArray([staticBlock, result]), factory);
                }
            }
        }
        exitClassElement(); return result;
    }
    // esdecorator.go:1724.
    private Node visitThisExpression(Node node) => classThis ?? node;
    // esdecorator.go:1731.
    private Node visitCallExpression(Node node)
    {
        var call = node.AsCallExpression();
        if (Ast.IsSuperProperty(call.Expression) && classThis != null)
        {
            var expression = Visitor().VisitNode(call.Expression);
            var arguments = Visitor().VisitNodes(call.Arguments);
            var invocation = Factory().NewFunctionCallCall(expression, classThis, arguments.Nodes);
            EmitContext().SetOriginal(invocation, node); invocation.Loc = node.Loc;
            return invocation;
        }
        return Visitor().VisitEachChild(node);
    }
    // esdecorator.go:1744.
    private Node visitTaggedTemplateExpression(Node node)
    {
        var tagged = node.AsTaggedTemplateExpression();
        if (Ast.IsSuperProperty(tagged.Tag) && classThis != null)
        {
            var tag = Visitor().VisitNode(tagged.Tag);
            var bound = Factory().NewFunctionBindCall(tag, classThis, Slice<Node>.Make(0));
            EmitContext().SetOriginal(bound, node); bound.Loc = node.Loc;
            return Factory().UpdateTaggedTemplateExpression(tagged, bound, null, null, Visitor().VisitNode(tagged.Template), tagged.Flags);
        }
        return Visitor().VisitEachChild(node);
    }
    // esdecorator.go:1757.
    private Node visitPropertyAccessExpression(Node node)
    {
        var access = node.AsPropertyAccessExpression();
        if (Ast.IsSuperProperty(node) && Ast.IsIdentifier(access.Name()) && classThis != null && classSuper != null)
        {
            var name = Factory().NewStringLiteralFromNode(access.Name());
            var property = Factory().NewReflectGetCall(classSuper, name, classThis);
            EmitContext().SetOriginal(property, access.Expression); property.Loc = access.Expression.Loc;
            return property;
        }
        return Visitor().VisitEachChild(node);
    }
    // esdecorator.go:1769.
    private Node visitElementAccessExpression(Node node)
    {
        var access = node.AsElementAccessExpression();
        if (Ast.IsSuperProperty(node) && classThis != null && classSuper != null)
        {
            var name = Visitor().VisitNode(access.ArgumentExpression);
            var property = Factory().NewReflectGetCall(classSuper, name, classThis);
            EmitContext().SetOriginal(property, access.Expression); property.Loc = access.Expression.Loc;
            return property;
        }
        return Visitor().VisitEachChild(node);
    }
    // esdecorator.go:1798.
    private Node visitParameterDeclaration(ParameterDeclaration node)
    {
        var parameter = node.AsNode();
        if (IsNamedEvaluationAnd(EmitContext(), parameter, isAnonymousClassNeedingAssignedName))
        {
            parameter = TransformNamedEvaluation(EmitContext(), parameter, canIgnoreEmptyStringLiteralInAssignedName(parameter.Initializer()), default);
            node = parameter.AsParameterDeclaration();
        }
        var updated = Factory().UpdateParameterDeclaration(node, null, node.DotDotDotToken, Visitor().VisitNode(node.Name()), null, null, Visitor().VisitNode(node.Initializer));
        if (updated != parameter)
        {
            EmitContext().SetCommentRange(updated, parameter.Loc);
            var location = Transformers.MoveRangePastModifiers(parameter); updated.Loc = location;
            EmitContext().SetSourceMapRange(updated, location);
            EmitContext().SetEmitFlags(updated.Name(), EmitFlags.NoTrailingSourceMap);
        }
        return updated;
    }
    // esdecorator.go:1870.
    private Node visitNamedEvaluationSite(Node node, Node classExpression)
    {
        if (IsNamedEvaluationAnd(EmitContext(), node, isAnonymousClassNeedingAssignedName))
            node = TransformNamedEvaluation(EmitContext(), node, canIgnoreEmptyStringLiteralInAssignedName(classExpression), default);
        return Visitor().VisitEachChild(node);
    }
    // esdecorator.go:1877.
    private static bool isAnonymousClassNeedingAssignedName(Node node) => Ast.IsClassExpression(node) && node.Name() == null && isDecoratedClassLike(node);
    // esdecorator.go:1885.
    private static bool canIgnoreEmptyStringLiteralInAssignedName(Node node)
    {
        if (node == null) return false;
        var inner = Ast.SkipOuterExpressions(node, OuterExpressionKinds.All);
        return Ast.IsClassExpression(inner) && inner.Name() == null && !Ast.ClassOrConstructorParameterIsDecorated(false, inner);
    }
    // esdecorator.go:1893.
    private Node visitForStatement(Node node)
    {
        var statement = node.AsForStatement();
        return Factory().UpdateForStatement(statement, discardedVisitor.VisitNode(statement.Initializer), Visitor().VisitNode(statement.Condition),
            discardedVisitor.VisitNode(statement.Incrementor), EmitContext().VisitIterationBody(statement.Statement, Visitor()));
    }
    // esdecorator.go:1905.
    private Node visitExpressionStatement(Node node) => discardedVisitor.VisitEachChild(node);
    // esdecorator.go:2099.
    private (Node ReferencedName, Node Name) visitReferencedPropertyName(Node node)
    {
        if (Ast.IsPropertyNameLiteral(node) || Ast.IsPrivateIdentifier(node))
            return (Factory().NewStringLiteralFromNode(node), Visitor().VisitNode(node));
        var computed = node.AsComputedPropertyName();
        if (Ast.IsPropertyNameLiteral(computed.Expression) && !Ast.IsIdentifier(computed.Expression))
            return (Factory().NewStringLiteralFromNode(computed.Expression), Visitor().VisitNode(node));
        var referencedName = Factory().NewGeneratedNameForNode(node);
        EmitContext().AddVariableDeclaration(referencedName);
        var key = Factory().NewPropKeyHelper(Visitor().VisitNode(computed.Expression));
        var assignment = Factory().NewAssignmentExpression(referencedName, key);
        return (referencedName, Factory().UpdateComputedPropertyName(computed, injectPendingExpressions(assignment)));
    }
    // esdecorator.go:2118.
    private Node visitPropertyName(Node node) => Ast.IsComputedPropertyName(node) ? visitComputedPropertyName(node) : Visitor().VisitNode(node);
    // esdecorator.go:2125.
    private Node visitComputedPropertyName(Node node)
    {
        var computed = node.AsComputedPropertyName();
        var expression = Visitor().VisitNode(computed.Expression);
        if (!Transformers.IsSimpleInlineableExpression(expression)) expression = injectPendingExpressions(expression);
        return Factory().UpdateComputedPropertyName(computed, expression);
    }
    // esdecorator.go:2134.
    private Node visitDestructuringAssignmentTarget(Node node)
    {
        if (Ast.IsObjectLiteralExpression(node) || Ast.IsArrayLiteralExpression(node)) return visitAssignmentPattern(node);
        if (Ast.IsSuperProperty(node) && classThis != null && classSuper != null)
        {
            Node propertyName = null;
            if (Ast.IsElementAccessExpression(node)) propertyName = Visitor().VisitNode(node.AsElementAccessExpression().ArgumentExpression);
            else if (Ast.IsPropertyAccessExpression(node) && Ast.IsIdentifier(node.AsPropertyAccessExpression().Name()))
                propertyName = Factory().NewStringLiteralFromNode(node.AsPropertyAccessExpression().Name());
            if (propertyName != null)
            {
                var parameter = Factory().NewTempVariable();
                var expression = Factory().NewAssignmentTargetWrapper(parameter, Factory().NewReflectSetCall(classSuper, propertyName, parameter, classThis));
                EmitContext().SetOriginal(expression, node); expression.Loc = node.Loc;
                return expression;
            }
        }
        return Visitor().VisitEachChild(node);
    }
    // esdecorator.go:2168.
    private Node visitAssignmentElement(Node node)
    {
        if (Ast.IsAssignmentExpression(node, true))
        {
            var binary = node.AsBinaryExpression();
            if (IsNamedEvaluationAnd(EmitContext(), node, isAnonymousClassNeedingAssignedName))
            {
                node = TransformNamedEvaluation(EmitContext(), node, canIgnoreEmptyStringLiteralInAssignedName(binary.Right), default);
                binary = node.AsBinaryExpression();
            }
            return Factory().UpdateBinaryExpression(binary, null, visitDestructuringAssignmentTarget(binary.Left), null, binary.OperatorToken, Visitor().VisitNode(binary.Right));
        }
        return visitDestructuringAssignmentTarget(node);
    }
    // esdecorator.go:2190.
    private Node visitAssignmentRestElement(Node node)
    {
        var spread = node.AsSpreadElement();
        if (Ast.IsLeftHandSideExpression(spread.Expression)) return Factory().UpdateSpreadElement(spread, visitDestructuringAssignmentTarget(spread.Expression));
        return Visitor().VisitEachChild(node);
    }
    // esdecorator.go:2200.
    private Node visitArrayAssignmentElement(Node node)
    {
        if (node == null || !Ast.IsExpression(node)) throw new InvalidOperationException("Expected expression");
        if (Ast.IsSpreadElement(node)) return visitAssignmentRestElement(node);
        if (!Ast.IsOmittedExpression(node)) return visitAssignmentElement(node);
        return Visitor().VisitEachChild(node);
    }
    // esdecorator.go:2211.
    private Node visitAssignmentPropertyNode(Node node)
    {
        var property = node.AsPropertyAssignment();
        var name = Visitor().VisitNode(property.Name());
        if (Ast.IsAssignmentExpression(property.Initializer, true))
            return Factory().UpdatePropertyAssignment(property, null, name, null, null, visitAssignmentElement(property.Initializer));
        if (Ast.IsLeftHandSideExpression(property.Initializer))
            return Factory().UpdatePropertyAssignment(property, null, name, null, null, visitDestructuringAssignmentTarget(property.Initializer));
        return Visitor().VisitEachChild(node);
    }
    // esdecorator.go:2237.
    private Node visitShorthandAssignmentProperty(Node node)
    {
        if (IsNamedEvaluationAnd(EmitContext(), node, isAnonymousClassNeedingAssignedName))
            node = TransformNamedEvaluation(EmitContext(), node, canIgnoreEmptyStringLiteralInAssignedName(node.AsShorthandPropertyAssignment().ObjectAssignmentInitializer), default);
        return Visitor().VisitEachChild(node);
    }
    // esdecorator.go:2253.
    private Node visitAssignmentRestProperty(Node node)
    {
        var spread = node.AsSpreadAssignment();
        if (Ast.IsLeftHandSideExpression(spread.Expression)) return Factory().UpdateSpreadAssignment(spread, visitDestructuringAssignmentTarget(spread.Expression));
        return Visitor().VisitEachChild(node);
    }
    // esdecorator.go:2263.
    private Node visitObjectAssignmentElement(Node node)
    {
        if (node == null || !Ast.IsObjectLiteralElement(node)) throw new InvalidOperationException("Expected object literal element");
        if (Ast.IsSpreadAssignment(node)) return visitAssignmentRestProperty(node);
        if (Ast.IsShorthandPropertyAssignment(node)) return visitShorthandAssignmentProperty(node);
        if (Ast.IsPropertyAssignment(node)) return visitAssignmentPropertyNode(node);
        return Visitor().VisitEachChild(node);
    }
    // esdecorator.go:2277.
    private Node visitAssignmentPattern(Node node)
    {
        if (Ast.IsArrayLiteralExpression(node))
        {
            var array = node.AsArrayLiteralExpression();
            return Factory().UpdateArrayLiteralExpression(array, arrayAssignmentVisitor.VisitNodes(array.Elements), array.MultiLine);
        }
        var literal = node.AsObjectLiteralExpression();
        return Factory().UpdateObjectLiteralExpression(literal, objectAssignmentVisitor.VisitNodes(literal.Properties), literal.MultiLine);
    }
    // esdecorator.go:2289.
    private Node visitExportAssignment(Node node) => visitNamedEvaluationSite(node, node.Expression());
    // esdecorator.go:2298.
    private Node visitParenthesizedExpression(Node node, bool discarded)
    {
        var parenthesized = node.AsParenthesizedExpression();
        return Factory().UpdateParenthesizedExpression(parenthesized, (discarded ? discardedVisitor : Visitor()).VisitNode(parenthesized.Expression));
    }
    // esdecorator.go:2315.
    private Node visitPartiallyEmittedExpression(Node node, bool discarded)
    {
        var partial = node.AsPartiallyEmittedExpression();
        return Factory().UpdatePartiallyEmittedExpression(partial, (discarded ? discardedVisitor : Visitor()).VisitNode(partial.Expression));
    }
    // esdecorator.go:2329.
    private Node prependExpressions(Slice<Node> pending, Node expression)
    {
        if (pending.Count == 0) return expression;
        if (expression == null) return Factory().InlineExpressions(pending);
        var expressions = Slice<Node>.Make(pending.Count + 1);
        pending.AsSpan().CopyTo(expressions.AsSpan());
        if (Ast.IsParenthesizedExpression(expression))
        {
            var parenthesized = expression.AsParenthesizedExpression();
            expressions[pending.Count] = parenthesized.Expression;
            return Factory().UpdateParenthesizedExpression(parenthesized, Factory().InlineExpressions(expressions));
        }
        expressions[pending.Count] = expression;
        return Factory().InlineExpressions(expressions);
    }
    // esdecorator.go:2350.
    private Node injectPendingExpressions(Node expression)
    {
        var result = prependExpressions(pendingExpressions, expression);
        if (result == null) throw new InvalidOperationException("Expected injected expression");
        if (result != expression) pendingExpressions = default;
        return result;
    }
    // esdecorator.go:2359.
    private Node injectPendingInitializers(ClassInfo info, bool isStatic, Node expression)
    {
        ref var pending = ref (isStatic ? ref info.pendingStaticInitializers : ref info.pendingInstanceInitializers);
        var result = prependExpressions(pending, expression);
        if (result != expression) pending = default;
        return result;
    }
    // esdecorator.go:2374.
    private Slice<Node> transformAllDecoratorsOfDeclaration(Slice<Node> decorators)
    {
        if (decorators.Count == 0) return default;
        var result = Slice<Node>.Make(0, decorators.Count);
        foreach (var decorator in decorators) result = result.Append(transformDecorator(decorator));
        return result;
    }
    // esdecorator.go:2386.
    private Node transformDecorator(Node decorator)
    {
        var expression = Visitor().VisitNode(decorator.AsDecorator().Expression);
        EmitContext().SetEmitFlags(expression, EmitFlags.NoComments);
        var inner = Ast.SkipOuterExpressions(expression, OuterExpressionKinds.All);
        if (Ast.IsAccessExpression(inner))
        {
            var (target, thisArgument) = createCallBinding(expression);
            var bound = Factory().NewFunctionBindCall(target, thisArgument, default);
            return Factory().RestoreOuterExpressions(expression, bound, OuterExpressionKinds.All);
        }
        return expression;
    }
    // esdecorator.go:2400.
    private (Node Target, Node ThisArgument) createCallBinding(Node expression)
    {
        var factory = Factory(); var callee = Ast.SkipOuterExpressions(expression, OuterExpressionKinds.All);
        if (Ast.IsSuperProperty(callee) || callee.Kind == Kind.SuperKeyword) return (callee, factory.NewThisExpression());
        if ((EmitContext().EmitFlags(callee) & EmitFlags.HelperName) != 0) return (callee, factory.NewVoidZeroExpression());
        if (Ast.IsPropertyAccessExpression(callee))
        {
            var access = callee.AsPropertyAccessExpression();
            if (shouldBeCapturedInTempVariable(access.Expression))
            {
                var thisArgument = factory.NewTempVariable(); EmitContext().AddVariableDeclaration(thisArgument);
                var assignment = factory.NewAssignmentExpression(thisArgument, access.Expression); assignment.Loc = access.Expression.Loc;
                var target = factory.NewPropertyAccessExpression(assignment, null, access.Name(), NodeFlags.None); target.Loc = callee.Loc;
                return (target, thisArgument);
            }
            return (callee, access.Expression);
        }
        if (Ast.IsElementAccessExpression(callee))
        {
            var access = callee.AsElementAccessExpression();
            if (shouldBeCapturedInTempVariable(access.Expression))
            {
                var thisArgument = factory.NewTempVariable(); EmitContext().AddVariableDeclaration(thisArgument);
                var assignment = factory.NewAssignmentExpression(thisArgument, access.Expression); assignment.Loc = access.Expression.Loc;
                var target = factory.NewElementAccessExpression(assignment, null, access.ArgumentExpression, NodeFlags.None); target.Loc = callee.Loc;
                return (target, thisArgument);
            }
            return (callee, access.Expression);
        }
        return (expression, factory.NewVoidZeroExpression());
    }
    // esdecorator.go:2441.
    private bool shouldBeCapturedInTempVariable(Node node) => Ast.SkipParentheses(node).Kind switch
    {
        Kind.Identifier => true,
        Kind.ThisKeyword or Kind.NumericLiteral or Kind.BigIntLiteral or Kind.StringLiteral => false,
        _ => true
    };
    // esdecorator.go:2462.
    private Node createDescriptorMethod(Node original, Node name, ModifierList modifiers, Node asteriskToken, string kind, NodeList parameters, Node body)
    {
        var factory = Factory(); var context = EmitContext();
        body ??= factory.NewBlock(factory.NewNodeList(Slice<Node>.Make(0)), false);
        var function = factory.NewFunctionExpression(modifiers, asteriskToken, null, null, parameters, null, null, body);
        context.SetOriginal(function, original);
        context.SetSourceMapRange(function, Transformers.MoveRangePastDecorators(original)); context.SetEmitFlags(function, EmitFlags.NoComments);
        string prefix = kind is "get" or "set" ? kind : "";
        var functionName = factory.NewStringLiteralFromNode(name);
        var namedFunction = factory.NewSetFunctionNameHelper(function, functionName, prefix);
        var method = factory.NewPropertyAssignment(null, factory.NewIdentifier(kind), null, null, namedFunction);
        context.SetOriginal(method, original);
        context.SetSourceMapRange(method, Transformers.MoveRangePastDecorators(original)); context.SetEmitFlags(method, EmitFlags.NoComments);
        return method;
    }
    // esdecorator.go:2507.
    private Node createMethodDescriptorObject(Node member, ModifierList modifiers)
    {
        var parameters = Visitor().VisitNodes(member.ParameterList());
        var body = Visitor().VisitNode(member.Body());
        var method = createDescriptorMethod(member, member.Name(), modifiers, member.AsMethodDeclaration().AsteriskToken, "value", parameters, body);
        return Factory().NewObjectLiteralExpression(Factory().NewNodeList(Slice<Node>.FromArray([method])), false);
    }
    // esdecorator.go:2521.
    private Node createGetAccessorDescriptorObject(Node member, ModifierList modifiers)
    {
        var body = Visitor().VisitNode(member.Body());
        var method = createDescriptorMethod(member, member.Name(), modifiers, null, "get", Factory().NewNodeList(Slice<Node>.Make(0)), body);
        return Factory().NewObjectLiteralExpression(Factory().NewNodeList(Slice<Node>.FromArray([method])), false);
    }
    // esdecorator.go:2533.
    private Node createSetAccessorDescriptorObject(Node member, ModifierList modifiers)
    {
        var parameters = Visitor().VisitNodes(member.ParameterList());
        var body = Visitor().VisitNode(member.Body());
        var method = createDescriptorMethod(member, member.Name(), modifiers, null, "set", parameters, body);
        return Factory().NewObjectLiteralExpression(Factory().NewNodeList(Slice<Node>.FromArray([method])), false);
    }
    // esdecorator.go:2547.
    private Node createAccessorPropertyDescriptorObject(Node member, ModifierList ignored)
    {
        var factory = Factory();
        var backingName = factory.NewGeneratedPrivateNameForNodeEx(member.Name(), new AutoGenerateOptions { Suffix = "_accessor_storage" });
        var getterReturn = factory.NewReturnStatement(factory.NewPropertyAccessExpression(factory.NewThisExpression(), null, backingName, NodeFlags.None));
        var getterBody = factory.NewBlock(factory.NewNodeList(Slice<Node>.FromArray([getterReturn])), false);
        var getter = createDescriptorMethod(member, member.Name(), null, null, "get", factory.NewNodeList(Slice<Node>.Make(0)), getterBody);
        var setterParameter = factory.NewParameterDeclaration(null, null, factory.NewIdentifier("value"), null, null, null);
        var setterExpression = factory.NewAssignmentExpression(factory.NewPropertyAccessExpression(factory.NewThisExpression(), null, backingName, NodeFlags.None), factory.NewIdentifier("value"));
        var setterBody = factory.NewBlock(factory.NewNodeList(Slice<Node>.FromArray([factory.NewExpressionStatement(setterExpression)])), false);
        var setter = createDescriptorMethod(member, member.Name(), null, null, "set", factory.NewNodeList(Slice<Node>.FromArray([setterParameter])), setterBody);
        return factory.NewObjectLiteralExpression(factory.NewNodeList(Slice<Node>.FromArray([getter, setter])), false);
    }
    // esdecorator.go:2585.
    private Node createMethodDescriptorForwarder(ModifierList modifiers, Node name, Node descriptorName)
    {
        var factory = Factory(); var staticOnly = staticOnlyModifierVisitor.VisitModifiers(modifiers);
        var expression = factory.NewPropertyAccessExpression(descriptorName, null, factory.NewIdentifier("value"), NodeFlags.None);
        return factory.NewGetAccessorDeclaration(staticOnly, name, null, factory.NewNodeList(Slice<Node>.Make(0)), null, null,
            factory.NewBlock(factory.NewNodeList(Slice<Node>.FromArray([factory.NewReturnStatement(expression)])), false));
    }
    // esdecorator.go:2604.
    private Node createGetAccessorDescriptorForwarder(ModifierList modifiers, Node name, Node descriptorName)
    {
        var factory = Factory(); var staticOnly = staticOnlyModifierVisitor.VisitModifiers(modifiers);
        var getter = factory.NewPropertyAccessExpression(descriptorName, null, factory.NewIdentifier("get"), NodeFlags.None);
        var expression = factory.NewFunctionCallCall(getter, factory.NewThisExpression(), default);
        return factory.NewGetAccessorDeclaration(staticOnly, name, null, factory.NewNodeList(Slice<Node>.Make(0)), null, null,
            factory.NewBlock(factory.NewNodeList(Slice<Node>.FromArray([factory.NewReturnStatement(expression)])), false));
    }
    // esdecorator.go:2627.
    private Node createSetAccessorDescriptorForwarder(ModifierList modifiers, Node name, Node descriptorName)
    {
        var factory = Factory(); var staticOnly = staticOnlyModifierVisitor.VisitModifiers(modifiers);
        var setter = factory.NewPropertyAccessExpression(descriptorName, null, factory.NewIdentifier("set"), NodeFlags.None);
        var expression = factory.NewFunctionCallCall(setter, factory.NewThisExpression(), Slice<Node>.FromArray([factory.NewIdentifier("value")]));
        var parameter = factory.NewParameterDeclaration(null, null, factory.NewIdentifier("value"), null, null, null);
        return factory.NewSetAccessorDeclaration(staticOnly, name, null, factory.NewNodeList(Slice<Node>.FromArray([parameter])), null, null,
            factory.NewBlock(factory.NewNodeList(Slice<Node>.FromArray([factory.NewReturnStatement(expression)])), false));
    }
    // esdecorator.go:2651.
    private Node createMetadata(Node name, Node superClass)
    {
        var factory = Factory();
        var superMetadata = superClass != null ? createSymbolMetadataReference(superClass) : factory.NewToken(Kind.NullKeyword);
        var objectCreate = factory.NewCallExpression(factory.NewPropertyAccessExpression(factory.NewIdentifier("Object"), null, factory.NewIdentifier("create"), NodeFlags.None),
            null, null, factory.NewNodeList(Slice<Node>.FromArray([superMetadata])), NodeFlags.None);
        var symbolCheck = factory.NewLogicalANDExpression(factory.NewTypeCheck(factory.NewIdentifier("Symbol"), "function"),
            factory.NewPropertyAccessExpression(factory.NewIdentifier("Symbol"), null, factory.NewIdentifier("metadata"), NodeFlags.None));
        var conditional = factory.NewConditionalExpression(symbolCheck, factory.NewToken(Kind.QuestionToken), objectCreate, factory.NewToken(Kind.ColonToken), factory.NewVoidZeroExpression());
        var declaration = factory.NewVariableDeclaration(name, null, null, conditional);
        return factory.NewVariableStatement(null, factory.NewVariableDeclarationList(factory.NewNodeList(Slice<Node>.FromArray([declaration])), NodeFlags.Const));
    }
    // esdecorator.go:2686.
    private Node createSymbolMetadata(Node target, Node value)
    {
        var factory = Factory();
        var symbolMetadata = factory.NewPropertyAccessExpression(factory.NewIdentifier("Symbol"), null, factory.NewIdentifier("metadata"), NodeFlags.None);
        var properties = Slice<Node>.FromArray([
            factory.NewPropertyAssignment(null, factory.NewIdentifier("enumerable"), null, null, factory.NewTrueExpression()),
            factory.NewPropertyAssignment(null, factory.NewIdentifier("configurable"), null, null, factory.NewTrueExpression()),
            factory.NewPropertyAssignment(null, factory.NewIdentifier("writable"), null, null, factory.NewTrueExpression()),
            factory.NewPropertyAssignment(null, factory.NewIdentifier("value"), null, null, value)]);
        var descriptor = factory.NewObjectLiteralExpression(factory.NewNodeList(properties), false);
        var define = factory.NewCallExpression(factory.NewPropertyAccessExpression(factory.NewIdentifier("Object"), null, factory.NewIdentifier("defineProperty"), NodeFlags.None),
            null, null, factory.NewNodeList(Slice<Node>.FromArray([target, symbolMetadata, descriptor])), NodeFlags.None);
        var statement = factory.NewIfStatement(value, factory.NewExpressionStatement(define), null);
        EmitContext().SetEmitFlags(statement, EmitFlags.SingleLine);
        return statement;
    }
    // esdecorator.go:2712.
    private Node createSymbolMetadataReference(Node superClass)
    {
        var factory = Factory();
        var symbolMetadata = factory.NewPropertyAccessExpression(factory.NewIdentifier("Symbol"), null, factory.NewIdentifier("metadata"), NodeFlags.None);
        var access = factory.NewElementAccessExpression(superClass, null, symbolMetadata, NodeFlags.None);
        return factory.NewBinaryExpression(null, access, null, factory.NewToken(Kind.QuestionQuestionToken), factory.NewToken(Kind.NullKeyword));
    }
    // esdecorator.go:2719.
    private static Node injectClassThisAssignmentIfMissing(EmitContext context, EmitNodeFactory factory, Node node, Node classThis)
    {
        if (ClassHasClassThisAssignment(context, node)) return node;
        var expression = factory.NewAssignmentExpression(classThis, factory.NewThisExpression());
        var statement = factory.NewExpressionStatement(expression);
        var block = factory.NewClassStaticBlockDeclaration(null, factory.NewBlock(factory.NewNodeList(Slice<Node>.FromArray([statement])), false));
        context.SetClassThis(block, classThis);
        if (node.Name() != null) context.SetSourceMapRange(statement, node.Name().Loc);
        var members = Slice<Node>.Make(0, 1 + node.Members().Count);
        members = members.Append(block).Append(node.Members());
        var list = factory.NewNodeList(members); list.Loc = node.MemberList().Loc;
        Node updated;
        if (Ast.IsClassDeclaration(node))
        {
            var declaration = node.AsClassDeclaration();
            updated = factory.UpdateClassDeclaration(declaration, declaration.Modifiers(), declaration.Name(), null, declaration.HeritageClauses, list);
        }
        else
        {
            var classExpression = node.AsClassExpression();
            updated = factory.UpdateClassExpression(classExpression, classExpression.Modifiers(), classExpression.Name(), null, classExpression.HeritageClauses, list);
        }
        context.SetClassThis(updated, classThis);
        return updated;
    }
    // esdecorator.go:1909.
    private Node visitBinaryExpression(Node node, bool discarded)
    {
        var factory = Factory(); var context = EmitContext(); var binary = node.AsBinaryExpression();
        if (Ast.IsDestructuringAssignment(node))
            return factory.UpdateBinaryExpression(binary, null, visitAssignmentPattern(binary.Left), null, binary.OperatorToken, Visitor().VisitNode(binary.Right));
        if (Ast.IsAssignmentExpression(node, false))
        {
            if (IsNamedEvaluationAnd(context, node, isAnonymousClassNeedingAssignedName))
            {
                node = TransformNamedEvaluation(context, node, canIgnoreEmptyStringLiteralInAssignedName(binary.Right), default);
                return Visitor().VisitEachChild(node);
            }
            if (Ast.IsSuperProperty(binary.Left) && classThis != null && classSuper != null)
            {
                Node setterName = null;
                if (Ast.IsElementAccessExpression(binary.Left)) setterName = Visitor().VisitNode(binary.Left.AsElementAccessExpression().ArgumentExpression);
                else if (Ast.IsPropertyAccessExpression(binary.Left) && Ast.IsIdentifier(binary.Left.AsPropertyAccessExpression().Name()))
                    setterName = factory.NewStringLiteralFromNode(binary.Left.AsPropertyAccessExpression().Name());
                if (setterName != null)
                {
                    var expression = Visitor().VisitNode(binary.Right);
                    if (Ast.IsCompoundAssignment(binary.OperatorToken.Kind))
                    {
                        var getterName = setterName;
                        if (!Transformers.IsSimpleInlineableExpression(setterName))
                        {
                            getterName = factory.NewTempVariable(); context.AddVariableDeclaration(getterName);
                            setterName = factory.NewAssignmentExpression(getterName, setterName);
                        }
                        var superGet = factory.NewReflectGetCall(classSuper, getterName, classThis);
                        context.SetOriginal(superGet, binary.Left); superGet.Loc = binary.Left.Loc;
                        expression = factory.NewBinaryExpression(null, superGet, null,
                            factory.NewToken(Transformers.GetNonAssignmentOperatorForCompoundAssignment(binary.OperatorToken.Kind)), expression);
                        expression.Loc = node.Loc;
                    }
                    Node temporary = null;
                    if (!discarded) { temporary = factory.NewTempVariable(); context.AddVariableDeclaration(temporary); }
                    if (temporary != null) { expression = factory.NewAssignmentExpression(temporary, expression); expression.Loc = node.Loc; }
                    expression = factory.NewReflectSetCall(classSuper, setterName, expression, classThis);
                    context.SetOriginal(expression, node); expression.Loc = node.Loc;
                    if (temporary != null) { expression = factory.NewCommaExpression(expression, temporary); expression.Loc = node.Loc; }
                    return expression;
                }
            }
        }
        if (binary.OperatorToken.Kind == Kind.CommaToken)
        {
            var left = discardedVisitor.VisitNode(binary.Left);
            var right = (discarded ? discardedVisitor : Visitor()).VisitNode(binary.Right);
            return factory.UpdateBinaryExpression(binary, null, left, null, binary.OperatorToken, right);
        }
        return Visitor().VisitEachChild(node);
    }
    // esdecorator.go:2019.
    private Node visitPreOrPostfixUnaryExpression(Node node, bool discarded)
    {
        var factory = Factory(); var context = EmitContext();
        Kind @operator; Node operandNode;
        if (Ast.IsPrefixUnaryExpression(node)) { @operator = node.AsPrefixUnaryExpression().Operator; operandNode = node.AsPrefixUnaryExpression().Operand; }
        else { @operator = node.AsPostfixUnaryExpression().Operator; operandNode = node.AsPostfixUnaryExpression().Operand; }
        if (@operator is Kind.PlusPlusToken or Kind.MinusMinusToken)
        {
            var operand = Ast.SkipParentheses(operandNode);
            if (Ast.IsSuperProperty(operand) && classThis != null && classSuper != null)
            {
                Node setterName = null;
                if (Ast.IsElementAccessExpression(operand)) setterName = Visitor().VisitNode(operand.AsElementAccessExpression().ArgumentExpression);
                else if (Ast.IsPropertyAccessExpression(operand) && Ast.IsIdentifier(operand.AsPropertyAccessExpression().Name()))
                    setterName = factory.NewStringLiteralFromNode(operand.AsPropertyAccessExpression().Name());
                if (setterName != null)
                {
                    var getterName = setterName;
                    if (!Transformers.IsSimpleInlineableExpression(setterName))
                    {
                        getterName = factory.NewTempVariable(); context.AddVariableDeclaration(getterName);
                        setterName = factory.NewAssignmentExpression(getterName, setterName);
                    }
                    var expression = factory.NewReflectGetCall(classSuper, getterName, classThis);
                    context.SetOriginal(expression, node); expression.Loc = node.Loc;
                    Node temporary = null;
                    if (!discarded) { temporary = factory.NewTempVariable(); context.AddVariableDeclaration(temporary); }
                    expression = ExpandPreOrPostfixIncrementOrDecrementExpression(factory, context, node, expression, temporary);
                    expression = factory.NewReflectSetCall(classSuper, setterName, expression, classThis);
                    context.SetOriginal(expression, node); expression.Loc = node.Loc;
                    if (temporary != null) { expression = factory.NewCommaExpression(expression, temporary); expression.Loc = node.Loc; }
                    return expression;
                }
            }
        }
        return Visitor().VisitEachChild(node);
    }
    // esdecorator.go:1574.
    private Node visitPropertyDeclaration(Node node)
    {
        if (IsNamedEvaluationAnd(EmitContext(), node, isAnonymousClassNeedingAssignedName))
            node = TransformNamedEvaluation(EmitContext(), node, canIgnoreEmptyStringLiteralInAssignedName(node.Initializer()), default);
        enterClassElement(node);
        if (Ast.HasSyntacticModifier(node, ModifierFlags.Ambient)) throw new InvalidOperationException("Decorated declare field not yet implemented upstream");
        var factory = Factory(); var context = EmitContext();
        Func<Node, ModifierList, Node> createDescriptor = null;
        if (Ast.HasAccessorModifier(node)) createDescriptor = accessorPropertyDescriptor;
        var result = partialTransformClassElement(node, classInfoStack, createDescriptor);
        context.StartVariableEnvironment();
        var initializer = Visitor().VisitNode(node.Initializer());
        if (result.initializersName != null)
        {
            var thisArgument = result.thisArg ?? factory.NewThisExpression();
            initializer ??= factory.NewVoidZeroExpression();
            initializer = factory.NewRunInitializersHelper(thisArgument, result.initializersName, initializer);
        }
        if (Ast.IsStatic(node) && classInfoStack != null && initializer != null) classInfoStack.hasStaticInitializers = true;
        var declarations = context.EndVariableEnvironment();
        if (declarations.Count > 0)
        {
            var statements = Slice<Node>.Make(declarations.Count + 1);
            declarations.AsSpan().CopyTo(statements.AsSpan()); statements[declarations.Count] = factory.NewReturnStatement(initializer);
            initializer = factory.NewImmediatelyInvokedArrowFunction(statements);
        }
        if (classInfoStack != null)
        {
            if (Ast.IsStatic(node))
            {
                initializer = injectPendingInitializers(classInfoStack, true, initializer);
                if (result.extraInitializersName != null)
                {
                    var thisArgument = classInfoStack.classThis ?? factory.NewThisExpression();
                    classInfoStack.pendingStaticInitializers = classInfoStack.pendingStaticInitializers.Append(factory.NewRunInitializersHelper(thisArgument, result.extraInitializersName, null));
                }
            }
            else
            {
                initializer = injectPendingInitializers(classInfoStack, false, initializer);
                if (result.extraInitializersName != null)
                    classInfoStack.pendingInstanceInitializers = classInfoStack.pendingInstanceInitializers.Append(factory.NewRunInitializersHelper(factory.NewThisExpression(), result.extraInitializersName, null));
            }
        }
        exitClassElement();
        if (Ast.HasAccessorModifier(node) && result.descriptorName != null)
        {
            var comments = context.CommentRange(node); var sourceMap = context.SourceMapRange(node);
            var propertyName = node.Name(); var getterName = result.name; var setterName = result.name;
            if (Ast.IsComputedPropertyName(propertyName) && !Transformers.IsSimpleInlineableExpression(propertyName.Expression()))
            {
                var cachedAssignment = FindComputedPropertyNameCacheAssignment(context, propertyName);
                if (cachedAssignment != null)
                {
                    getterName = factory.UpdateComputedPropertyName(propertyName.AsComputedPropertyName(), Visitor().VisitNode(propertyName.Expression()));
                    setterName = factory.UpdateComputedPropertyName(propertyName.AsComputedPropertyName(), cachedAssignment.Left);
                }
                else
                {
                    var temporary = factory.NewTempVariable(); context.SetSourceMapRange(temporary, propertyName.Expression().Loc); context.AddVariableDeclaration(temporary);
                    var expression = Visitor().VisitNode(propertyName.Expression());
                    var assignment = factory.NewAssignmentExpression(temporary, expression); context.SetSourceMapRange(assignment, propertyName.Expression().Loc);
                    getterName = factory.UpdateComputedPropertyName(propertyName.AsComputedPropertyName(), assignment);
                    setterName = factory.UpdateComputedPropertyName(propertyName.AsComputedPropertyName(), temporary);
                }
            }
            var modifiers = accessorStrippingModifierVisitor.VisitModifiers(result.modifiers);
            var backingField = CreateAccessorPropertyBackingField(factory, node.AsPropertyDeclaration(), modifiers, initializer);
            context.SetOriginal(backingField, node); context.SetEmitFlags(backingField, EmitFlags.NoComments); context.SetSourceMapRange(backingField, sourceMap);
            context.SetSourceMapRange(backingField.AsPropertyDeclaration().Name(), context.SourceMapRange(node.Name()));
            var getter = createGetAccessorDescriptorForwarder(modifiers, getterName, result.descriptorName);
            context.SetOriginal(getter, node); context.SetCommentRange(getter, comments); context.SetSourceMapRange(getter, sourceMap);
            var setter = createSetAccessorDescriptorForwarder(modifiers, setterName, result.descriptorName);
            context.SetOriginal(setter, node); context.SetEmitFlags(setter, EmitFlags.NoComments); context.SetSourceMapRange(setter, sourceMap);
            return Transformers.SingleOrMany(Slice<Node>.FromArray([backingField, getter, setter]), factory);
        }
        return finishClassElement(factory.UpdatePropertyDeclaration(node.AsPropertyDeclaration(), result.modifiers, result.name, null, null, initializer), node);
    }
    // esdecorator.go:577.
    private Node transformClassLike(Node node)
    {
        var factory = Factory(); var context = EmitContext();
        context.StartVariableEnvironment();
        if (!ClassHasDeclaredOrExplicitlyAssignedName(context, node) && Ast.ClassOrConstructorParameterIsDecorated(false, node))
            node = InjectClassNamedEvaluationHelperBlockIfMissing(context, node, factory.NewStringLiteral("", TokenFlags.None), null);
        var classReference = factory.GetLocalNameEx(node, new AssignedNameOptions());
        var info = createClassInfo(node);
        var classStatements = Slice<Node>.Make(0);
        Slice<Node> leading = default, trailing = default;
        Node syntheticConstructor = null;
        NodeList heritageClauses = null;
        bool transformPrivateStatic = false;
        var decorators = transformAllDecoratorsOfDeclaration(node.Decorators());
        if (decorators.Count > 0)
        {
            if (info.classThis == null) throw new InvalidOperationException("Expected classThis for class decorator");
            info.classDecoratorsName = factory.NewUniqueNameEx("_classDecorators", new AutoGenerateOptions { Flags = GeneratedIdentifierFlags.Optimistic | GeneratedIdentifierFlags.FileLevel });
            info.classDescriptorName = factory.NewUniqueNameEx("_classDescriptor", new AutoGenerateOptions { Flags = GeneratedIdentifierFlags.Optimistic | GeneratedIdentifierFlags.FileLevel });
            info.classExtraInitializersName = factory.NewUniqueNameEx("_classExtraInitializers", new AutoGenerateOptions { Flags = GeneratedIdentifierFlags.Optimistic | GeneratedIdentifierFlags.FileLevel });
            var decoratorsArray = factory.NewArrayLiteralExpression(factory.NewNodeList(decorators), false);
            classStatements = classStatements.Append(createLet(info.classDecoratorsName, decoratorsArray));
            classStatements = classStatements.Append(createLet(info.classDescriptorName, null));
            classStatements = classStatements.Append(createLet(info.classExtraInitializersName, factory.NewArrayLiteralExpression(factory.NewNodeList(default(Slice<Node>)), false)));
            classStatements = classStatements.Append(createLet(info.classThis, null));
            if (decorators.Count > 0 && info.hasStaticPrivateClassElements)
            { transformPrivateStatic = true; shouldTransformPrivateStaticElementsInFile = true; }
        }
        var extendsClause = Ast.GetHeritageClause(node, Kind.ExtendsKeyword);
        Node extendsElement = null;
        if (extendsClause != null)
        {
            var clause = extendsClause.AsHeritageClause();
            if (clause.Types != null && clause.Types.Nodes.Count > 0) extendsElement = clause.Types.Nodes[0];
        }
        Node extendsExpression = null;
        if (extendsElement != null) extendsExpression = Visitor().VisitNode(extendsElement.AsExpressionWithTypeArguments().Expression);
        if (extendsExpression != null)
        {
            info.classSuper = factory.NewUniqueNameEx("_classSuper", new AutoGenerateOptions { Flags = GeneratedIdentifierFlags.Optimistic | GeneratedIdentifierFlags.FileLevel });
            var unwrapped = Ast.SkipOuterExpressions(extendsExpression, OuterExpressionKinds.All);
            var safeExtends = extendsExpression;
            if (Ast.IsClassExpression(unwrapped) && unwrapped.Name() == null || Ast.IsFunctionExpression(unwrapped) && unwrapped.Name() == null || Ast.IsArrowFunction(unwrapped))
                safeExtends = factory.NewCommaExpression(factory.NewNumericLiteral("0", TokenFlags.None), extendsExpression);
            classStatements = classStatements.Append(createLet(info.classSuper, safeExtends));
            var updatedElement = factory.UpdateExpressionWithTypeArguments(extendsElement.AsExpressionWithTypeArguments(), info.classSuper, null);
            var clause = extendsClause.AsHeritageClause();
            var updatedClause = factory.UpdateHeritageClause(clause, clause.Token, factory.NewNodeList(Slice<Node>.FromArray([updatedElement])));
            heritageClauses = factory.NewNodeList(Slice<Node>.FromArray([updatedClause]));
        }
        var renamedClassThis = info.classThis ?? factory.NewThisExpression();
        enterClass(info);
        leading = leading.Append(createMetadata(info.metadataReference, info.classSuper));
        var members = nonConstructorClassElementVisitor.VisitNodes(node.MemberList());
        members = constructorClassElementVisitor.VisitNodes(members);
        if (pendingExpressions.Count > 0)
        {
            outerThis = null;
            foreach (var pending in pendingExpressions)
            {
                var expression = pending;
                if (((uint)expression.SubtreeFacts() & SubtreeFactValues.ContainsLexicalThis) != 0) expression = outerThisVisitor.VisitNode(expression);
                leading = leading.Append(factory.NewExpressionStatement(expression));
            }
            if (outerThis != null) classStatements = Slice<Node>.FromArray([createLet(outerThis, factory.NewThisExpression())]).Append(classStatements);
            pendingExpressions = default;
        }
        exitClass();
        if (info.pendingInstanceInitializers.Count > 0 && Ast.GetFirstConstructorWithBody(node) == null)
        {
            var initializers = prepareConstructor(info);
            if (initializers.Count > 0)
            {
                bool derived = extendsElement != null && Ast.SkipOuterExpressions(extendsElement.AsExpressionWithTypeArguments().Expression, OuterExpressionKinds.All).Kind != Kind.NullKeyword;
                var constructorStatements = Slice<Node>.Make(0);
                if (derived)
                {
                    var spreadArguments = factory.NewSpreadElement(factory.NewIdentifier("arguments"));
                    var superCall = factory.NewCallExpression(factory.NewKeywordExpression(Kind.SuperKeyword), null, null,
                        factory.NewNodeList(Slice<Node>.FromArray([spreadArguments])), NodeFlags.None);
                    constructorStatements = constructorStatements.Append(factory.NewExpressionStatement(superCall));
                }
                constructorStatements = constructorStatements.Append(initializers);
                var body = factory.NewBlock(factory.NewNodeList(constructorStatements), true);
                syntheticConstructor = factory.NewConstructorDeclaration(null, null, factory.NewNodeList(default(Slice<Node>)), null, null, body);
            }
        }
        if (info.staticMethodExtraInitializersName != null)
            classStatements = classStatements.Append(createLet(info.staticMethodExtraInitializersName, factory.NewArrayLiteralExpression(factory.NewNodeList(default(Slice<Node>)), false)));
        if (info.instanceMethodExtraInitializersName != null)
            classStatements = classStatements.Append(createLet(info.instanceMethodExtraInitializersName, factory.NewArrayLiteralExpression(factory.NewNodeList(default(Slice<Node>)), false)));
        if (info.memberInfos.Size() > 0)
        {
            classStatements = classStatements.Append(emitMemberInfoDeclarations(info, true));
            classStatements = classStatements.Append(emitMemberInfoDeclarations(info, false));
        }
        leading = leading.Append(info.staticNonFieldDecorationStatements);
        leading = leading.Append(info.nonStaticNonFieldDecorationStatements);
        leading = leading.Append(info.staticFieldDecorationStatements);
        leading = leading.Append(info.nonStaticFieldDecorationStatements);
        if (info.classDescriptorName != null && info.classDecoratorsName != null && info.classExtraInitializersName != null && info.classThis != null)
        {
            var valueProperty = factory.NewPropertyAssignment(null, factory.NewIdentifier("value"), null, null, renamedClassThis);
            var descriptor = factory.NewObjectLiteralExpression(factory.NewNodeList(Slice<Node>.FromArray([valueProperty])), false);
            var assignment = factory.NewAssignmentExpression(info.classDescriptorName, descriptor);
            var nameReference = factory.NewPropertyAccessExpression(renamedClassThis, null, factory.NewIdentifier("name"), NodeFlags.None);
            var contextObject = factory.NewESDecorateClassContextObject(nameReference, info.metadataReference);
            var decorate = factory.NewESDecorateHelper(factory.NewToken(Kind.NullKeyword), assignment, info.classDecoratorsName,
                contextObject, factory.NewToken(Kind.NullKeyword), info.classExtraInitializersName);
            var statement = factory.NewExpressionStatement(decorate);
            context.SetSourceMapRange(statement, Transformers.MoveRangePastDecorators(node)); leading = leading.Append(statement);
            var descriptorValue = factory.NewPropertyAccessExpression(info.classDescriptorName, null, factory.NewIdentifier("value"), NodeFlags.None);
            var thisAssignment = factory.NewAssignmentExpression(info.classThis, descriptorValue);
            leading = leading.Append(factory.NewExpressionStatement(factory.NewAssignmentExpression(classReference, thisAssignment)));
        }
        leading = leading.Append(createSymbolMetadata(renamedClassThis, info.metadataReference));
        if (info.pendingStaticInitializers.Count > 0)
        {
            foreach (var initializer in info.pendingStaticInitializers)
            {
                var statement = factory.NewExpressionStatement(initializer);
                context.SetSourceMapRange(statement, context.SourceMapRange(initializer)); trailing = trailing.Append(statement);
            }
            info.pendingStaticInitializers = default;
        }
        if (info.classExtraInitializersName != null)
        {
            var runInitializers = factory.NewRunInitializersHelper(renamedClassThis, info.classExtraInitializersName, null);
            var statement = factory.NewExpressionStatement(runInitializers);
            if (node.Name() != null) context.SetSourceMapRange(statement, node.Name().Loc);
            else context.SetSourceMapRange(statement, Transformers.MoveRangePastDecorators(node));
            trailing = trailing.Append(statement);
        }
        if (leading.Count > 0 && trailing.Count > 0 && !info.hasStaticInitializers) { leading = leading.Append(trailing); trailing = default; }
        Node leadingBlock = null, trailingBlock = null;
        if (leading.Count > 0) leadingBlock = factory.NewClassStaticBlockDeclaration(null, factory.NewBlock(factory.NewNodeList(leading), true));
        if (leadingBlock != null && transformPrivateStatic) context.SetEmitFlags(leadingBlock, EmitFlags.TransformPrivateStaticElements);
        if (trailing.Count > 0) trailingBlock = factory.NewClassStaticBlockDeclaration(null, factory.NewBlock(factory.NewNodeList(trailing), true));
        if (leadingBlock != null || syntheticConstructor != null || trailingBlock != null)
        {
            var newMembers = Slice<Node>.Make(0, members.Nodes.Count + 3);
            int namedBlockIndex = -1;
            for (int i = 0; i < members.Nodes.Count; i++)
                if (IsClassNamedEvaluationHelperBlock(context, members.Nodes[i])) { namedBlockIndex = i; break; }
            if (leadingBlock != null)
            {
                newMembers = newMembers.Append(members.Nodes.Slice2(0, namedBlockIndex + 1));
                newMembers = newMembers.Append(leadingBlock);
                newMembers = newMembers.Append(members.Nodes.Slice2(namedBlockIndex + 1, members.Nodes.Count));
            }
            else newMembers = newMembers.Append(members.Nodes);
            if (syntheticConstructor != null) newMembers = newMembers.Append(syntheticConstructor);
            if (trailingBlock != null) newMembers = newMembers.Append(trailingBlock);
            var list = factory.NewNodeList(newMembers); list.Loc = members.Loc; members = list;
        }
        var environment = context.EndVariableEnvironment();
        Node classExpression;
        if (decorators.Count > 0)
        {
            classExpression = factory.NewClassExpression(null, null, null, heritageClauses, members);
            context.SetOriginal(classExpression, node);
            if (info.classThis != null) classExpression = injectClassThisAssignmentIfMissing(context, factory, classExpression, info.classThis);
            var referenceDeclaration = factory.NewVariableDeclaration(classReference, null, null, classExpression);
            var declarationList = factory.NewVariableDeclarationList(factory.NewNodeList(Slice<Node>.FromArray([referenceDeclaration])), NodeFlags.None);
            var returnExpression = info.classThis != null ? factory.NewAssignmentExpression(classReference, info.classThis) : classReference;
            classStatements = classStatements.Append(factory.NewVariableStatement(null, declarationList));
            classStatements = classStatements.Append(factory.NewReturnStatement(returnExpression));
        }
        else
        {
            classExpression = factory.NewClassExpression(null, node.Name(), null, heritageClauses, members);
            context.SetOriginal(classExpression, node); classStatements = classStatements.Append(factory.NewReturnStatement(classExpression));
        }
        if (transformPrivateStatic)
        {
            context.AddEmitFlags(classExpression, EmitFlags.TransformPrivateStaticElements);
            foreach (var member in classExpression.Members())
                if ((Ast.IsPrivateIdentifierClassElementDeclaration(member) || Ast.IsAutoAccessorPropertyDeclaration(member)) && Ast.HasStaticModifier(member))
                    context.AddEmitFlags(member, EmitFlags.TransformPrivateStaticElements);
        }
        return factory.NewImmediatelyInvokedArrowFunction(context.MergeEnvironment(classStatements, environment));
    }
}
