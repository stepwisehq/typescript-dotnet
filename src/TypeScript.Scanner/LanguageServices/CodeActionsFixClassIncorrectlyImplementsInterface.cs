// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/codeactions_fixclassincorrectlyimplementsinterface.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Checking;
using TypeScript.LanguageServices.AutoImport;
using TypeScript.LanguageServices.Change;
using TypeScript.Lsp.Protocol;
using Type = TypeScript.Checking.Type;
using ScannerApi = TypeScript.Scanner.Scanner;
using AstNav = TypeScript.AstNavigation.AstNav;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    private const string fixClassIncorrectlyImplementsInterfaceFixID = "fixClassIncorrectlyImplementsInterface";
    private static readonly Slice<int> fixClassIncorrectlyImplementsInterfaceErrorCodes = Slice<int>.FromArray([Diagnostics.Class_0_incorrectly_implements_interface_1.Code(), Diagnostics.Class_0_incorrectly_implements_class_1_Did_you_mean_to_extend_1_and_inherit_its_members_as_a_subclass.Code()]);
    private static readonly CodeFixProvider FixClassIncorrectlyImplementsInterfaceProvider = new()
    {
        ErrorCodes = fixClassIncorrectlyImplementsInterfaceErrorCodes,
        GetCodeActions = getCodeActionsToFixClassIncorrectlyImplementsInterface,
        FixIds = Slice<ByteStringValue>.FromArray([ByteStringValue.FromString(fixClassIncorrectlyImplementsInterfaceFixID)]),
        GetAllCodeActions = getAllCodeActionsToFixClassIncorrectlyImplementsInterface
    };
    private static (Slice<CodeAction> Actions, Exception? Error) getCodeActionsToFixClassIncorrectlyImplementsInterface(ProtocolRequestContext context, CodeFixContext fixContext)
    {
        var declaration = getClass(fixContext.SourceFile, fixContext.Span);
        if (declaration == null)
            return (default, null);
        var implementedTypes = Ast.GetImplementsHeritageClauseElements(declaration);
        var(checker, done) = fixContext.Program.GetTypeCheckerForFile(context, fixContext.SourceFile);
        try
        {
            Slice<CodeAction> actions = default;
            foreach (var implementedType in implementedTypes)
            {
                var tracker = Tracker.NewTracker(context, fixContext.Program.Options(), fixContext.LS.FormatOptions(), fixContext.LS.converters);
                var(adder, error) = createImportAdder(context, fixContext, checker);
                if (error != null)
                    return (default, error);
                addChanges(context, fixContext, tracker, adder, checker, declaration, implementedType);
                var changes = getChanges(tracker, adder, fixContext.SourceFile);
                if (changes.Length == 0)
                    continue;
                actions = actions.Append(new CodeAction { Description = ByteStringValue.FromString(Diagnostics.Implement_interface_0.Localize(context.Locale, ScannerApi.GetTextOfNodeValue(implementedType))), Changes = changes, FixID = ByteStringValue.FromString(fixClassIncorrectlyImplementsInterfaceFixID), FixAllDescription = ByteStringValue.FromString(Diagnostics.Implement_all_unimplemented_interfaces.Localize(context.Locale)) });
            }

            return (actions, null);
        }
        finally
        {
            done();
        }
    }

    private static (CombinedCodeActions? Actions, Exception? Error) getAllCodeActionsToFixClassIncorrectlyImplementsInterface(ProtocolRequestContext context, CodeFixContext fixContext)
    {
        var diagnostics = getAllDiagnostics(context, fixContext.Program, fixContext.SourceFile);
        var(checker, done) = fixContext.Program.GetTypeCheckerForFile(context, fixContext.SourceFile);
        try
        {
            var tracker = Tracker.NewTracker(context, fixContext.Program.Options(), fixContext.LS.FormatOptions(), fixContext.LS.converters);
            var(adder, error) = createImportAdder(context, fixContext, checker);
            if (error != null)
                return (null, error);
            HashSet<Node>? seen = null;
            foreach (var diagnostic in diagnostics)
            {
                if (!isFixableDiagnostic(diagnostic, fixClassIncorrectlyImplementsInterfaceErrorCodes))
                    continue;
                var declaration = getClass(fixContext.SourceFile, new TextRange(diagnostic.Pos(), diagnostic.End()));
                if (declaration == null)
                    continue;
                if ((seen ??= new()).Add(declaration))
                {
                    foreach (var implementedType in Ast.GetImplementsHeritageClauseElements(declaration))
                        addChanges(context, fixContext, tracker, adder, checker, declaration, implementedType);
                }
            }

            var changes = getChanges(tracker, adder, fixContext.SourceFile);
            return changes.Length == 0 ? (null, null) : (new CombinedCodeActions { Description = ByteStringValue.FromString(Diagnostics.Implement_all_unimplemented_interfaces.Localize(context.Locale)), Changes = changes }, null);
        }
        finally
        {
            done();
        }
    }

    private static void addChanges(ProtocolRequestContext context, CodeFixContext fixContext, Tracker tracker, IImportAdder? adder, Checker checker, Node declaration, Node implementedTypeNode)
    {
        var fixer = newMissingMemberFixer(tracker, fixContext.Program, checker, fixContext.LS.UserPreferences(), adder, context.Locale);
        var constructor = getConstructor(declaration);
        var implementedType = checker.GetTypeAtLocation(implementedTypeNode);
        var classType = checker.GetTypeAtLocation(declaration);
        if (checker.GetNumberIndexType(classType) == null)
        {
            var member = fixer.createIndexSignatureDeclarationFromType(declaration, implementedType, checker.GetNumberType());
            if (member != null)
                insertInterfaceMemberNode(tracker, fixContext.SourceFile, declaration, constructor, member);
        }

        if (checker.GetStringIndexType(classType) == null)
        {
            var member = fixer.createIndexSignatureDeclarationFromType(declaration, implementedType, checker.GetStringType());
            if (member != null)
                insertInterfaceMemberNode(tracker, fixContext.SourceFile, declaration, constructor, member);
        }

        foreach (var member in getMissingMembers(checker, declaration, Slice<Type>.FromArray([implementedType])))
        {
            foreach (var node in fixer.createMemberFromSymbol(member, declaration, fixContext.SourceFile, null, PreserveOptionalFlags.All, false))
                insertInterfaceMemberNode(tracker, fixContext.SourceFile, declaration, constructor, node);
        }
    }

    private static Slice<TextEdit> getChanges(Tracker tracker, IImportAdder? adder, SourceFile file)
    {
        var(changes, unmappable) = tracker.GetChanges();
        if (unmappable.Length != 0)
            return default;
        changes.TryGetValue(file.OriginalFileName(), out var edits);
        if (adder != null && adder.HasFixes())
            edits = edits.Append(adder.Edits());
        return edits;
    }

    private static void insertInterfaceMemberNode(Tracker tracker, SourceFile file, Node declaration, Node? constructor, Node member)
    {
        if (constructor == null)
            tracker.InsertMemberAtStart(file, declaration, member);
        else
            tracker.InsertNodeAfter(file, constructor, member);
    }

    private static Node? getClass(SourceFile file, TextRange span)
    {
        var token = AstNav.GetTokenAtPosition(file, span.Pos());
        return token == null ? null : Ast.GetContainingClass(token);
    }

    private static Node? getConstructor(Node? declaration)
    {
        if (declaration?.MemberList() == null)
            return null;
        foreach (var member in declaration.MemberList().Nodes)
            if (member != null && Ast.IsConstructorDeclaration(member))
                return member;
        return null;
    }

    private static Slice<Symbol> getMissingMembers(Checker checker, Node declaration, Slice<Type> implementedTypes)
    {
        var inheritedMembers = getInheritedMembers(checker, declaration);
        var seenMembers = new Dictionary<ByteStringKey, Symbol>();
        var classMembers = declaration.Symbol()?.Members;
        Slice<Symbol> result = default;
        foreach (var type in implementedTypes)
        {
            foreach (var symbol in checker.GetPropertiesOfType(type))
            {
                if (symbol == null)
                    continue;
                if (classMembers?[symbol.NameValue] != null)
                    continue;
                if (inheritedMembers[symbol.NameValue] != null || symbol.NameValue.TryGetValue(seenMembers, out _))
                    continue;
                if ((Checker.GetDeclarationModifierFlagsFromSymbol(symbol) & ModifierFlags.Private) == 0)
                {
                    seenMembers[ByteStringKey.FromValue(symbol.NameValue)] = symbol;
                    result = result.Append(symbol);
                }
            }
        }

        return result;
    }

    private static SymbolTable getInheritedMembers(Checker checker, Node declaration)
    {
        var typeNode = Ast.GetClassExtendsHeritageElement(declaration);
        if (typeNode == null)
            return new SymbolTable();
        var baseType = checker.GetTypeAtLocation(typeNode);
        if (baseType == null)
            return new SymbolTable();
        var members = new SymbolTable();
        foreach (var symbol in checker.GetPropertiesOfType(baseType))
        {
            if (symbol == null)
                continue;
            if ((Checker.GetDeclarationModifierFlagsFromSymbol(symbol) & ModifierFlags.Private) == 0)
                members[symbol.NameValue] = symbol;
        }

        return members;
    }

    private static (IImportAdder? Adder, Exception? Error) createImportAdder(ProtocolRequestContext context, CodeFixContext fixContext, Checker checker)
    {
        var(view, error) = fixContext.LS.getPreparedAutoImportView(fixContext.SourceFile, checker);
        if (error != null)
            return (null, error);
        if (view == null)
            return (null, null);
        return (ImportAdder.NewImportAdder(context, fixContext.Program, checker, fixContext.SourceFile, view, fixContext.LS.FormatOptions(), fixContext.LS.converters, fixContext.LS.UserPreferences()), null);
    }
}
