// Hand-adapted declarations from binder.go; managed pool/arena boundaries only.
[Flags]
public enum ContainerFlags
{
    None = 0,
    IsContainer = 1 << 0,
    IsBlockScopedContainer = 1 << 1,
    IsControlFlowContainer = 1 << 2,
    IsFunctionLike = 1 << 3,
    IsFunctionExpression = 1 << 4,
    HasLocals = 1 << 5,
    IsInterface = 1 << 6,
    IsObjectLiteralOrClassExpressionMethodOrAccessor = 1 << 7,
    IsThisContainer = 1 << 8,
    PropagatesThisKeyword = 1 << 9,
}
public partial class Binder
{
    private sealed class ActiveLabel
    {
        public ActiveLabel next;
        public FlowNode breakTarget, continueTarget;
        public string name;
        public bool referenced;
        public FlowNode BreakTarget() => breakTarget;
        public FlowNode ContinueTarget() => continueTarget;
    }
    private struct ExpandoAssignmentInfo
    {
        public Node node, container, blockScopeContainer;
    }
    private SourceFile file;
    private readonly Visitor bindFunc;
    private FlowNode unreachableFlow;
    private Node container, thisContainer, blockScopeContainer, lastContainer;
    private FlowNode currentFlow, currentBreakTarget, currentContinueTarget, currentReturnTarget;
    private FlowNode currentTrueTarget, currentFalseTarget, currentExceptionTarget, preSwitchCaseFlow;
    private ActiveLabel activeLabelList;
    private NodeFlags emitFlags;
    private bool seenThisKeyword, hasExplicitReturn, hasFlowEffects, inAssignmentPattern, seenParseError;
    private int symbolCount;
    private readonly HashSet<Symbol> notConstEnumOnlyModules = new();
    private readonly GoObjectArena<Symbol> symbolArena = new();
    private readonly GoObjectArena<FlowNode> flowNodeArena = new();
    private readonly GoObjectArena<FlowList> flowListArena = new();
    private readonly GoArena<Node> singleDeclarationsArena = new();
    private List<ExpandoAssignmentInfo> expandoAssignments;
    private Binder() => bindFunc = bind;
    // getBinder/putBinder adaptation: preserve isolated binding state and arena-owned identity,
    // replacing sync.Pool with one fresh managed Binder per file. No checker state is imported.
    private static Binder getBinder() => new();
}
