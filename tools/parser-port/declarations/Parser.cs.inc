// Hand-adapted declarations from parser.go; bounded runtime/API bridges.
internal enum ParsingContext
{
 SourceElements,BlockStatements,SwitchClauses,SwitchClauseStatements,TypeMembers,ClassMembers,EnumMembers,
 HeritageClauseElement,VariableDeclarations,ObjectBindingElements,ArrayBindingElements,ArgumentExpressions,
 ObjectLiteralMembers,JsxAttributes,JsxChildren,ArrayLiteralMembers,Parameters,JSDocParameters,RestProperties,
 TypeParameters,TypeArguments,TupleElementTypes,HeritageClauses,ImportOrExportSpecifiers,ImportAttributes,JSDocComment,Count
}
[Flags] internal enum jsdocScannerInfo { HasJSDoc=1,HasDeprecated=2,HasSeeOrLink=4 }


internal sealed class JSDocInfo { public Node parent; public List<Node> jsDocs; }
internal struct ParserState
{
 public ScannerState scannerState;
 public NodeFlags contextFlags;
 public int diagnosticsLen,jsDiagnosticsLen,jsdocInfosLen,reparsedClonesLen;
 public bool statementHasAwaitIdentifier,hasParseError;
}
public partial class Parser
{
 private Scanner scanner;
 private NodeFactory factory = new();
 private SourceFileParseOptions opts;
 private string sourceText = "";
 private ScriptKind scriptKind;
 private LanguageVariant languageVariant;
 private List<Diagnostic> diagnostics,jsDiagnostics,jsdocDiagnostics;
 private Kind token;
 private NodeFlags sourceFlags,contextFlags;
 private int parsingContexts;
 private bool statementHasAwaitIdentifier,hasDeprecatedTag,hasParseError;
 private int identifierCount;
 private HashSet<int> notParenthesizedArrow = new();
 private GoArena<Node> nodeSliceArena = new();
 private GoArena<string> stringSliceArena = new();
 private List<JSDocInfo> jsdocInfos;
 private List<int> possibleAwaitSpans;
 private List<string> jsdocCommentsSpace,jsdocTagCommentsSpace;
 private List<CommentRange> jsdocCommentRangesSpace;
 private List<Node> jsdocTagCommentsPartsSpace,reparseList,reparsedClones;
 private Node currentParent;
 private Visitor setParentFromContext;
 private static readonly string[] viableKeywordSuggestions = Scanner.GetViableKeywordSuggestions();
 private static readonly List<Node> missingListNodes = new();
 // Go parser-pool adapter: each request obtains an isolated managed parser.
 private static Parser getParser() => newParser();
 private static bool isMissingNodeList(NodeList list) => list != null && ReferenceEquals(list.Nodes,missingListNodes);
 // Supported contract: JS1, JSX2, TS3, TSX4, JSON6. Unknown follows upstream failure; reserved numeric casts are outside the tested contract.
 public static SourceFile ParseSourceFile(SourceFileParseOptions opts, string sourceText, ScriptKind scriptKind) => ParseSourceFileRaw(opts,GoString.ToUtf8(sourceText),scriptKind);
 public static SourceFile ParseSourceFile(SourceFileParseOptions opts, byte[] sourceText, ScriptKind scriptKind) => ParseSourceFileRaw(opts,GoString.FromBytes(sourceText),scriptKind);
 public static Node ParseIsolatedEntityName(string text) => ParseIsolatedEntityNameRaw(GoString.ToUtf8(text));
 public static Node ParseIsolatedEntityName(byte[] text) => ParseIsolatedEntityNameRaw(GoString.FromBytes(text));
}
