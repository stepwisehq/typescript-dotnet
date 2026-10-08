// Binder state used by the first, internal model integration. No partial public binding API.
public partial class Binder
{
    private int symbolCount;
    private FlowNode unreachableFlow = null;
    private readonly GoObjectArena<Symbol> symbolArena = new();
    private readonly GoObjectArena<FlowNode> flowNodeArena = new();
    private readonly GoObjectArena<FlowList> flowListArena = new();
    private readonly GoArena<Node> singleDeclarationsArena = new();
}
