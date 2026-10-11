// Additional syntax differential assertions, not a port of an original upstream test.
// Expected observations come from unchanged pinned ast utilities/precedence and parser APIs.
using System.Text.Json;
using TypeScript.Scanner;

namespace TypeScript.Foundation;
public static partial class ScannerChecks
{
    private static readonly Lazy<JsonDocument> syntaxFixtures = new(() => LoadFixture("TypeScript.Foundation.SyntaxHelperFixtures.json"));
    private static readonly Dictionary<string, Func<Node, bool>> kindPredicates = new(StringComparer.Ordinal)
    {
        ["IsModifierLike"] = Ast.IsModifierLike,
        ["IsObjectBindingOrAssignmentElement"] = Ast.IsObjectBindingOrAssignmentElement,
        ["IsAccessor"] = Ast.IsAccessor,
        ["IsMemberName"] = Ast.IsMemberName,
        ["IsClassOrInterfaceLike"] = Ast.IsClassOrInterfaceLike,
        ["IsTypeElement"] = Ast.IsTypeElement,
        ["IsJsxChild"] = Ast.IsJsxChild,
        ["IsJsxAttributeLike"] = Ast.IsJsxAttributeLike,
        ["IsStatement"] = Ast.IsStatement,
        ["IsStatementButNotDeclaration"] = Ast.IsStatementButNotDeclaration,
        ["CanHaveSymbol"] = Ast.CanHaveSymbol,
        ["CanHaveModifiers"] = Ast.CanHaveModifiers,
        ["CanHaveIllegalModifiers"] = Ast.CanHaveIllegalModifiers,
        ["HasImportAttributes"] = Ast.HasImportAttributes,
        ["IsJsxCallLike"] = Ast.IsJsxCallLike,
        ["IsParameterLike"] = Ast.IsParameterLike,
        ["IsVariableLike"] = Ast.IsVariableLike,
        ["IsObjectTypeDeclaration"] = Ast.IsObjectTypeDeclaration,
        ["IsClassOrTypeElement"] = Ast.IsClassOrTypeElement,
        ["IsTypeKeywordToken"] = Ast.IsTypeKeywordToken,
        ["IsStringTextContainingNode"] = Ast.IsStringTextContainingNode,
        ["IsTemplateLiteralToken"] = Ast.IsTemplateLiteralToken,
        ["IsImportOrImportEqualsDeclaration"] = Ast.IsImportOrImportEqualsDeclaration,
    };
    private static void AddSyntaxUtilityChecks(Dictionary<string, Action> cases)
    {
        cases.Add("AdditionalSyntax/kindPredicates", CheckKindPredicates);
        cases.Add("AdditionalSyntax/operatorPrecedence", CheckOperatorPrecedence);
        cases.Add("AdditionalSyntax/nilPragmaMap", () =>
        {
            var expected = syntaxFixtures.Value.RootElement.GetProperty("PragmaArguments");
            Equal(expected[0].GetString(), Ast.GetPragmaArgument(null, "factory"), "Nil pragma");
            Equal(expected[1].GetString(), Ast.GetPragmaArgument(new Pragma(), "factory"), "Nil pragma argument map");
            Equal(expected[2].GetString(), Ast.GetPragmaArgument(new Pragma { Args = new() { [ByteStringKey.FromString("factory")] = new PragmaArgument { Value = "react" } } }, "factory"), "Pragma argument value");
        });
        using var registrationFixture = LoadFixture("TypeScript.Foundation.SyntaxHelperFixtures.json");
        for (int i = 0; i < registrationFixture.RootElement.GetProperty("Parsed").GetArrayLength(); i++)
        {
            int index = i;
            cases.Add("AdditionalSyntax/parsedHelpers/" + i, () => CheckParsedSyntaxHelpers(index));
        }
    }

    private static void CheckKindPredicates()
    {
        var factory = new NodeFactory();
        int count = syntaxFixtures.Value.RootElement.GetProperty("KindCount").GetInt32();
        Equal((int)Kind.Count, count, "Pinned enum count");
        foreach (var row in syntaxFixtures.Value.RootElement.GetProperty("Predicates").EnumerateArray())
        {
            string name = row.GetProperty("Name").GetString()!;
            var expectedKinds = row.GetProperty("TrueKinds").EnumerateArray().Select(value => value.GetInt32()).ToHashSet();
            for (int kind = 0; kind < count; kind++)
                Equal(expectedKinds.Contains(kind), kindPredicates[name](factory.NewToken((Kind)kind)), name + " kind " + kind);
        }
    }

    private static void CheckOperatorPrecedence()
    {
        foreach (var row in syntaxFixtures.Value.RootElement.GetProperty("Operators").EnumerateArray())
        {
            var kind = (Kind)row.GetProperty("Kind").GetInt32();
            var flags = (OperatorPrecedenceFlags)row.GetProperty("Flags").GetInt32();
            Equal(row.GetProperty("Precedence").GetInt32(), (int)Ast.GetOperatorPrecedence(kind, Kind.PlusToken, flags), "Operator precedence " + kind + " flags " + flags);
        }

        int index = 0;
        foreach (var value in syntaxFixtures.Value.RootElement.GetProperty("Binary").EnumerateArray())
            Equal(value.GetInt32(), (int)Ast.GetBinaryOperatorPrecedence((Kind)index++), "Binary precedence " + index);
    }

    private static void CheckParsedSyntaxHelpers(int index)
    {
        var fixture = syntaxFixtures.Value.RootElement.GetProperty("Parsed")[index];
        string text = fixture.GetProperty("Text").GetString()!;
        var file = Parser.ParseSourceFile(new SourceFileParseOptions("/helpers.ts", "/helpers.ts"), text, (ScriptKind)fixture.GetProperty("ScriptKind").GetInt32());
        var factory = new NodeFactory();
        var nodes = new List<Node>();
        var ids = new Dictionary<Node, int>(ReferenceEqualityComparer.Instance);
        bool Collect(Node node)
        {
            ids.Add(node, nodes.Count);
            nodes.Add(node);
            node.ForEachChild(Collect);
            return false;
        }

        Collect(file.AsNode());
        int Id(Node? node) => node is null ? -1 : ids[node];
        var expectedNodes = fixture.GetProperty("Nodes");
        Equal(expectedNodes.GetArrayLength(), nodes.Count, "Ordinary syntax traversal count");
        for (int n = 0; n < nodes.Count; n++)
        {
            var node = nodes[n];
            var row = expectedNodes[n];
            string context = "Parsed helper source " + index + " node " + n;
            Equal(row.GetProperty("Kind").GetInt32(), (int)node.Kind, context + " kind");
            Equal(row.GetProperty("Pos").GetInt32(), node.Pos(), context + " position");
            Equal(row.GetProperty("End").GetInt32(), node.End(), context + " end");
            Equal(row.GetProperty("Meaning").GetInt32(), (int)Ast.GetMeaningFromDeclaration(node), context + " syntactic declaration meaning");
            bool[] predicates = [Ast.IsArrayBindingOrAssignmentElement(node), Ast.IsAliasSymbolDeclaration(node), Ast.IsCallLikeExpression(node), Ast.IsCallLikeOrFunctionLikeExpression(node), Ast.IsSourcePhaseImport(node), Ast.IsTypeOnlyImportOrExportDeclaration(node), Ast.IsValidTypeOnlyAliasUseSite(node), Ast.HasInitializer(node), Ast.IsFunctionLike(node) && Ast.HasContextSensitiveParameters(node), Ast.IsUnterminatedLiteral(node), Ast.IsNamedEvaluationSource(node), Ast.IsNonWhitespaceToken(node)];
            Equal(string.Join(",", row.GetProperty("Predicates").EnumerateArray().Select(value => value.GetBoolean())), string.Join(",", predicates), context + " parsed predicates");
            if (Ast.IsExpression(node))
            {
                Equal(row.GetProperty("ExpressionPrecedence").GetInt32(), (int)Ast.GetExpressionPrecedence(node), context + " expression precedence");
                CompareIds(row.GetProperty("Leftmost"), [Id(Ast.GetLeftmostExpression(node, false)), Id(Ast.GetLeftmostExpression(node, true))], context + " leftmost expression");
            }

            CompareIds(row.GetProperty("Ancestors"), GoSlices.Values(Ast.FindManyAncestors(node, Ast.IsSourceFile, Ast.IsClassLike, Ast.IsFunctionLike, Ast.IsSourceFile)).Select(Id), context + " ordered ancestor callbacks");
            Equal(row.GetProperty("AncestorOrQuit").GetInt32(), Id(Ast.FindAncestorOrQuit(node, current => Ast.IsBlock(current) ? FindAncestorResult.Quit : Ast.ToFindAncestorResult(Ast.IsSourceFile(current)))), context + " ancestor quit");
            if (Ast.IsBlock(node))
            {
                var returns = new List<int>();
                bool result = Ast.ForEachReturnStatement(node, statement =>
                {
                    returns.Add(Id(statement));
                    return false;
                });
                Equal(row.GetProperty("ReturnResult").GetBoolean(), result, context + " return traversal result");
                CompareIds(row.GetProperty("ReturnNodes"), returns, context + " returns skip nested functions");
                returns.Clear();
                result = Ast.ForEachReturnStatement(node, statement =>
                {
                    returns.Add(Id(statement));
                    return true;
                });
                Equal(row.GetProperty("StoppedReturnResult").GetBoolean(), result, context + " stopped return result");
                CompareIds(row.GetProperty("StoppedReturns"), returns, context + " stopped return order");
            }

            if (Ast.CanHaveModifiers(node))
            {
                Equal(row.GetProperty("ModifierSame").GetBoolean(), ReferenceEquals(node, Ast.ReplaceModifiers(factory, node, node.Modifiers())), context + " unchanged modifiers identity");
                var modifiers = factory.NewModifierList([factory.NewToken(Kind.ExportKeyword), factory.NewToken(Kind.AsyncKeyword)]);
                var modified = Ast.ReplaceModifiers(factory, node, modifiers);
                CompareIds(row.GetProperty("ModifiedKinds"), GoSlices.ToListOrNull(modified.Modifiers().Nodes)!.Select(value => (int)value.Kind), context + " modifier order");
            }

            if (Ast.IsAccessor(node))
            {
                var members = Ast.IsClassLike(node.Parent) ? node.Parent.Members() : node.Parent.Properties();
                var pair = Ast.GetAllAccessorDeclarations(members, node);
                CompareIds(row.GetProperty("Accessors"), [Id(pair.FirstAccessor), Id(pair.SecondAccessor), Id(pair.GetAccessor?.AsNode()), Id(pair.SetAccessor?.AsNode())], context + " accessor pair/static/dynamic name");
            }
        }
    }

    private static void CompareIds(JsonElement expected, IEnumerable<int> actual, string context)
    {
        string values = expected.ValueKind == JsonValueKind.Null ? "" : string.Join(",", expected.EnumerateArray().Select(value => value.GetInt32()));
        Equal(values, string.Join(",", actual), context);
    }
}
