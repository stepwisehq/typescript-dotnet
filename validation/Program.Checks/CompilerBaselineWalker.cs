// Authored assertion adapter, faithfully following the complete original
// tsc/internal/testutil/tsbaseline/type_symbol_baseline.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// This traverses public compiler behavior; it observes no private type graph.
#nullable disable
using System.Text;
using System.Text.RegularExpressions;
using TypeScript.Checking;
using TypeScript.Scanner;
using BuilderFlags = TypeScript.NodeBuilding.Flags;
using InternalFlags = TypeScript.NodeBuilding.InternalFlags;
using CompilerProgram = TypeScript.Compiler.Program;
using CheckerType = TypeScript.Checking.Type;

namespace Program.Checks;

public static class CompilerBaselineWalker
{
    // Text and names have the original Go-byte string role. No display/path normalization.
    public sealed record File(string UnitName, string Content);
    public sealed record Baselines(string Types, string Symbols);
    private sealed record Result(int Line, string SourceText, string Symbol = "", string Type = "", string Underline = "");
    // Go RE2 alternatives are kept in original order, including the CR-before-CRLF quirk.
    private static readonly Regex CodeLines = new("[\\r]|\\xE2\\x80[\\xA8\\xA9]|\\r?\\n", RegexOptions.CultureInvariant);
    private static readonly Regex BracketLine = new("^[\\t\\n\\f\\r ]*[{|}][\\t\\n\\f\\r ]*$", RegexOptions.CultureInvariant);
    private static readonly Regex LineDelimiter = new("\\r?\\n", RegexOptions.CultureInvariant);

    public static Baselines Observe(CompilerProgram program, IReadOnlyList<File> allFiles, string header, bool hasCheckingErrors)
    {
        var walker = new Walker(program, hasCheckingErrors);
        // Original shared walker, complete type pass before complete symbol pass.
        return new(Generate(allFiles, walker, header, false), Generate(allFiles, walker, header, true));
    }

    private static string Generate(IReadOnlyList<File> files, Walker walker, string header, bool symbols)
    {
        var output = new StringBuilder();
        foreach (string baseline in Iterate(files, walker, symbols)) output.Append(baseline);
        // Preserve the actual baseline.NoContent sentinel.
        return output.Length > 0 ? "//// [" + header + "] ////\r\n\r\n" + output : "<no content>";
    }

    private static List<string> Iterate(IReadOnlyList<File> files, Walker walker, bool symbols)
    {
        var baselines = new List<string>();
        foreach (File file in files)
        {
            var lines = new StringBuilder("=== " + file.UnitName + " ===\r\n");
            string[] code = CodeLines.Split(file.Content);
            List<Result> results = walker.VisitFile(file.UnitName, symbols);
            int last = -1;
            foreach (Result result in results)
            {
                if (symbols && result.Symbol == "") return baselines;
                if (last == -1)
                    lines.AppendJoin("\r\n", code.Take(result.Line + 1)).Append("\r\n");
                else if (last != result.Line)
                {
                    if (!(last + 1 < code.Length && (BracketLine.IsMatch(code[last + 1]) || TrimSpace(code[last + 1]) == ""))) lines.Append("\r\n");
                    lines.AppendJoin("\r\n", code.Skip(last + 1).Take(result.Line - last)).Append("\r\n");
                }
                last = result.Line;
                string text = LineDelimiter.Replace(result.SourceText, "");
                lines.Append('>').Append(text).Append(" : ").Append(symbols ? result.Symbol : result.Type).Append("\r\n");
                if (result.Underline != "")
                    lines.Append('>').Append(' ', text.Length).Append(" : ").Append(result.Underline).Append("\r\n");
            }
            if (last + 1 < code.Length)
            {
                if (!(last + 1 < code.Length && (BracketLine.IsMatch(code[last + 1]) || TrimSpace(code[last + 1]) == ""))) lines.Append("\r\n");
                lines.AppendJoin("\r\n", code.Skip(last + 1));
            }
            lines.Append("\r\n");
            baselines.Add(RemoveTestPathPrefixes(lines.ToString()));
        }
        return baselines;
    }

    private static string TrimSpace(string bytes) => GoStrings.TrimRightFunc(GoStrings.TrimLeftFunc(bytes, GoUnicode.IsSpace), GoUnicode.IsSpace);
    private static string RemoveTestPathPrefixes(string value)
    {
        // Original strings.NewReplacer: single left-to-right pass, first matching prefix.
        (string From, string To)[] replacements = [("/.ts/", ""), ("/.lib/", ""), ("/.src/", ""), ("bundled:///libs/", ""), ("file:///./ts/", "file:///"), ("file:///./lib/", "file:///"), ("file:///./src/", "file:///")];
        var output = new StringBuilder();
        for (int i = 0; i < value.Length;)
        {
            bool matched = false;
            foreach (var (from, to) in replacements)
            {
                if (!value.AsSpan(i).StartsWith(from.AsSpan(), StringComparison.Ordinal)) continue;
                output.Append(to); i += from.Length; matched = true; break;
            }
            if (!matched) output.Append(value[i++]);
        }
        return output.ToString();
    }

    private sealed class Walker(CompilerProgram program, bool hadCheckingErrors)
    {
        private SourceFile currentFile;
        // Original source reads this cache but never populates it; preserve that behavior.
        private readonly Dictionary<Node, string> declarationTextCache = new(ReferenceEqualityComparer.Instance);
        public List<Result> VisitFile(string filename, bool symbols)
        {
            currentFile = program.GetSourceFile(TsPath.ToRootedFilePath(filename, program.BaseDirectory()));
            var results = new List<Result>();
            foreach (Node node in ForEachAstNode(currentFile.AsNode()))
            {
                if (Ast.IsExpressionNode(node) || node.Kind == Kind.Identifier || Ast.IsDeclarationName(node) ||
                    Ast.IsQualifiedName(node) && Ast.IsNameOfHeritageClauseTypeReference(node) && (symbols || Ast.IsQualifiedName(node.Parent)))
                {
                    Result result = Write(node, symbols);
                    if (result != null) results.Add(result);
                }
            }
            return results;
        }

        private static List<Node> ForEachAstNode(Node node)
        {
            var result = new List<Node>();
            var work = new List<Node> { node };
            var children = new List<Node>();
            while (work.Count > 0)
            {
                Node item = work[^1]; work.RemoveAt(work.Count - 1);
                if ((item.Flags & NodeFlags.Reparsed) == 0 || item.Kind is Kind.AsExpression or Kind.SatisfiesExpression ||
                    item.Parent.Kind is Kind.SatisfiesExpression or Kind.AsExpression && item == item.Parent.Expression())
                {
                    if ((item.Flags & NodeFlags.Reparsed) == 0 || item.Parent.Kind is Kind.AsExpression or Kind.SatisfiesExpression) result.Add(item);
                    item.ForEachChild(child => { children.Add(child); return false; });
                    children.Reverse(); work.AddRange(children); children.Clear();
                }
            }
            return result;
        }

        private Result Write(Node node, bool symbols)
        {
            int actualPos = Scanner.SkipTrivia(GoString.Bytes(currentFile.Text()), node.Pos());
            int line = Scanner.GetECMALineOfPosition(currentFile, actualPos);
            string sourceText = Scanner.GetSourceTextOfNodeFromSourceFileByteString(currentFile, node, false);
            var (checker, done) = program.GetTypeCheckerForFile(default(CancellationToken), currentFile);
            try
            {
                var context = new TypeScript.DiagnosticPrinting.EmitContext();
                if (!symbols)
                {
                    if (Ast.IsPartOfTypeNode(node) || node.Kind is Kind.AsExpression or Kind.SatisfiesExpression && (node.Type().Flags & NodeFlags.Reparsed) != 0 ||
                        Ast.IsIdentifier(node) && (Ast.GetMeaningFromDeclaration(node.Parent) & SemanticMeaning.Value) == 0 &&
                        !(Ast.IsTypeOrJSTypeAliasDeclaration(node.Parent) && node == node.Parent.Name())) return null;
                    if (Ast.IsOmittedExpression(node)) return null;
                    CheckerType type = null;
                    if (Ast.IsExpressionWithTypeArgumentsInClassExtendsClause(node.Parent)) type = checker.GetTypeAtLocation(node.Parent);
                    if (type == null || Checker.IsTypeAny(type)) type = checker.GetTypeAtLocation(node);
                    string display;
                    if (!hadCheckingErrors && Checker.IsTypeAny(type) && !Ast.IsBindingElement(node.Parent) &&
                        !Ast.IsPropertyAccessOrQualifiedName(node.Parent) && !Ast.IsLabelName(node) && !Ast.IsGlobalScopeAugmentation(node.Parent) &&
                        !Ast.IsMetaProperty(node.Parent) && !IsImportStatementName(node) && !IsExportStatementName(node) && !IsIntrinsicJsxTag(node, currentFile))
                        display = type.AsIntrinsicType().IntrinsicName();
                    else
                    {
                        var builder = new NodeBuilder(checker, context);
                        var flags = BuilderFlags.NoTruncation | BuilderFlags.AllowUniqueESSymbolType | BuilderFlags.GenerateNamesForShadowedTypeParams | BuilderFlags.IgnoreErrors;
                        Node typeNode = builder.TypeToTypeNode(type, node.Parent, flags, InternalFlags.AllowUnresolvedNames, null);
                        if (Ast.IsIdentifier(node) && Ast.IsTypeAliasDeclaration(node.Parent) && node.Parent.Name() == node && Ast.IsIdentifier(typeNode) && typeNode.Text() == node.Text())
                            typeNode = builder.TypeToTypeNode(type, node.Parent, flags | BuilderFlags.InTypeAlias, InternalFlags.AllowUnresolvedNames, null);
                        var writer = TypeScript.DiagnosticPrinting.TextWriter.NewTextWriter("", 0);
                        new TypeScript.DiagnosticPrinting.Printer(context, removeComments: true).Write(typeNode, writer, currentFile);
                        display = writer.String();
                    }
                    return new(line, sourceText, Type: display);
                }
                Symbol symbol = checker.GetSymbolAtLocation(node);
                if (symbol == null) return null;
                var text = new StringBuilder("Symbol(");
                text.Append(Ast.EscapeAllInternalSymbolNames(GoString.ToUtf8(checker.SymbolToStringEx(symbol, node.Parent, SymbolFlags.None, SymbolFormatFlags.AllowAnyNodeKind))));
                int count = 0;
                foreach (Node declaration in symbol.Declarations)
                {
                    if (count >= 5) { text.Append(" ... and ").Append(symbol.Declarations.Count - count).Append(" more"); break; }
                    count++; text.Append(", ");
                    if (declarationTextCache.TryGetValue(declaration, out string cached)) { text.Append(cached); continue; }
                    SourceFile file = Ast.GetSourceFileOfNode(declaration);
                    var (declLine, declChar) = Scanner.GetECMALineAndUTF16CharacterOfPosition(file, declaration.Pos());
                    string name = TsPath.GetBaseFileName(file.FileNameGoBytes());
                    text.Append("Decl(").Append(name).Append(", ");
                    if (name.StartsWith("lib.", StringComparison.Ordinal) && name.EndsWith(".d.ts", StringComparison.Ordinal)) text.Append("--, --)");
                    else text.Append(declLine).Append(", ").Append(declChar).Append(')');
                }
                return new(line, sourceText, Symbol: text.Append(')').ToString());
            }
            finally { done(); }
        }
    }

    private static bool IsImportStatementName(Node node) =>
        Ast.IsImportSpecifier(node.Parent) && (node == node.Parent.Name() || node == node.Parent.PropertyName()) ||
        Ast.IsImportClause(node.Parent) && node == node.Parent.Name() || Ast.IsImportEqualsDeclaration(node.Parent) && node == node.Parent.Name();
    private static bool IsExportStatementName(Node node) =>
        Ast.IsExportAssignment(node.Parent) && node == node.Parent.Expression() || Ast.IsExportSpecifier(node.Parent) && (node == node.Parent.Name() || node == node.Parent.PropertyName());
    private static bool IsIntrinsicJsxTag(Node node, SourceFile file) =>
        (Ast.IsJsxOpeningElement(node.Parent) || Ast.IsJsxClosingElement(node.Parent) || Ast.IsJsxSelfClosingElement(node.Parent)) &&
        node.Parent.TagName() == node && Scanner.IsIntrinsicJsxName(Scanner.GetSourceTextOfNodeFromSourceFileByteString(file, node, false));
}
