// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/flow.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Only declaration-assignment/start flows admitted; other flow flags throw.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    private readonly record struct FlowType(Type t, bool incomplete = false)
    {
        internal bool isNil() => t == null;
    }

    private sealed class FlowState
    {
        internal Node reference, flowContainer;
        internal Type declaredType, initialType;
        internal int depth, sharedFlowStart;
    }

    private readonly record struct SharedFlow(FlowNode flow, FlowType flowType);
    private readonly List<SharedFlow> sharedFlows = new();
    private readonly Dictionary<FlowNode, bool> flowNodeReachable = new();
    private FlowNode lastFlowNode;
    private bool lastFlowNodeReachable;
    private int flowInvocationCount;
    private readonly Dictionary<(Type, Type), Type> assignmentReducedTypes = new();
    private FlowType newFlowType(Type t, bool incomplete)
    {
        if (incomplete && (t.flags & TypeFlags.Never) != 0)
            t = silentNeverType;
        return new(t, incomplete);
    }

    private static FlowNode getFlowNodeOfNode(Node node) => node.FlowNodeData()?.FlowNode;
    // getFlowTypeOfReferenceEx (82), scalar result; evolving arrays/non-null syntax excluded.
    private Type getFlowTypeOfReferenceEx(Node reference, Type declaredType, Type initialType, Node container, FlowNode flowNode)
    {
        flowNode ??= getFlowNodeOfNode(reference);
        if (flowNode == null)
            return declaredType;
        var f = new FlowState
        {
            reference = reference,
            declaredType = declaredType,
            initialType = initialType ?? declaredType,
            flowContainer = container,
            sharedFlowStart = sharedFlows.Count
        };
        flowInvocationCount++;
        var evolvedType = getTypeAtFlowNode(f, flowNode).t;
        sharedFlows.RemoveRange(f.sharedFlowStart, sharedFlows.Count - f.sharedFlowStart);
        if ((evolvedType.objectFlags & ObjectFlags.EvolvingArray) != 0)
            throw unsupported("getFlowTypeOfReferenceEx: evolving arrays");
        if (evolvedType == unreachableNeverType)
            return declaredType;
        return evolvedType;
    }

    private FlowType getTypeAtFlowNode(FlowState f, FlowNode flow)
    {
        if (f.depth == 2000)
            throw unsupported("getTypeAtFlowNode: analysis depth limit");
        f.depth++;
        FlowNode sharedFlow = null;
        while (true)
        {
            var flags = flow.Flags;
            if ((flags & FlowFlags.Shared) != 0)
            {
                for (int i = f.sharedFlowStart; i < sharedFlows.Count; i++)
                    if (sharedFlows[i].flow == flow)
                    {
                        f.depth--;
                        return sharedFlows[i].flowType;
                    }

                sharedFlow = flow;
            }

            FlowType t;
            if ((flags & FlowFlags.Assignment) != 0)
            {
                t = getTypeAtFlowAssignment(f, flow);
                if (t.isNil())
                {
                    flow = flow.Antecedent;
                    continue;
                }
            }
            else if ((flags & (FlowFlags.Call | FlowFlags.Condition | FlowFlags.SwitchClause | FlowFlags.Label | FlowFlags.ArrayMutation | FlowFlags.ReduceLabel)) != 0)
                throw unsupported("getTypeAtFlowNode: branching/call/mutation flow");
            else if ((flags & FlowFlags.Start) != 0)
            {
                var container = flow.Node;
                if (container != null && container != f.flowContainer)
                {
                    if (container.Kind != Kind.SourceFile)
                        throw unsupported("getTypeAtFlowNode: containing function flow");
                    flow = container.FlowNodeData().FlowNode;
                    continue;
                }

                t = new(f.initialType);
            }
            else
                t = new(convertAutoToAny(f.declaredType));
            if (sharedFlow != null)
                sharedFlows.Add(new(sharedFlow, t));
            f.depth--;
            return t;
        }
    }

    private FlowType getTypeAtFlowAssignment(FlowState f, FlowNode flow)
    {
        var node = flow.Node;
        if (node.Kind != Kind.VariableDeclaration)
            throw unsupported("getTypeAtFlowAssignment: explicit assignment");
        if (isMatchingReference(f.reference, node))
        {
            if (!isReachableFlowNode(flow))
                return new(unreachableNeverType);
            if (f.declaredType == autoType)
                throw unsupported("getTypeAtFlowAssignment: auto inference");
            var t = f.declaredType;
            if ((t.flags & TypeFlags.Union) != 0)
                return new(getAssignmentReducedType(t, getInitialOrAssignedType(f, flow)));
            return new(t);
        }

        // containsMatchingReference only checks dotted prefixes; none are admitted.
        return default;
    }

    private bool isMatchingReference(Node source, Node target)
    {
        if (target.Kind == Kind.ParenthesizedExpression)
            return isMatchingReference(source, target.Expression());
        switch (source.Kind)
        {
            case Kind.Identifier:
                return Ast.IsIdentifier(target) && getResolvedSymbol(source) == getResolvedSymbol(target) || Ast.IsVariableDeclaration(target) && getExportSymbolOfValueSymbolIfExported(getResolvedSymbol(source)) == getSymbolOfDeclaration(target);
            case Kind.ParenthesizedExpression:
                return isMatchingReference(source.Expression(), target);
            default:
                throw unsupported("isMatchingReference: nonscalar reference");
        }
    }

    private Type getInitialOrAssignedType(FlowState f, FlowNode flow)
    {
        if (flow.Node.Kind != Kind.VariableDeclaration)
            throw unsupported("getInitialOrAssignedType: nondeclaration");
        return getNarrowableTypeForReference(getInitialType(flow.Node), f.reference);
    }

    private Type getInitialType(Node node)
    {
        if (node.Kind != Kind.VariableDeclaration)
            throw unsupported("getInitialType: binding element");
        return getInitialTypeOfVariableDeclaration(node);
    }

    private Type getInitialTypeOfVariableDeclaration(Node node)
    {
        if (node.Initializer() != null)
            return getTypeOfInitializer(node.Initializer());
        if (node.Parent.Parent.Kind != Kind.VariableStatement)
            throw unsupported("getInitialTypeOfVariableDeclaration: loop");
        return errorType;
    }

    private Type getTypeOfInitializer(Node node)
    {
        if (typeNodeLinks.Has(node))
        {
            var t = typeNodeLinks.Get(node).resolvedType;
            if (t != null)
                return t;
        }

        return getTypeOfExpression(node);
    }

    private Type getAssignmentReducedType(Type declaredType, Type assignedType)
    {
        if (declaredType == assignedType)
            return declaredType;
        if ((assignedType.flags & TypeFlags.Never) != 0)
            return assignedType;
        if (!assignmentReducedTypes.TryGetValue((declaredType, assignedType), out var result))
            assignmentReducedTypes[(declaredType, assignedType)] = result = getAssignmentReducedTypeWorker(declaredType, assignedType);
        return result;
    }

    private Type getAssignmentReducedTypeWorker(Type declaredType, Type assignedType)
    {
        var filteredType = filterType(declaredType, t => typeMaybeAssignableTo(assignedType, t));
        var reducedType = filteredType;
        if ((assignedType.flags & TypeFlags.BooleanLiteral) != 0 && isFreshLiteralType(assignedType))
            reducedType = mapType(filteredType, getFreshTypeOfLiteralType);
        return isTypeAssignableTo(assignedType, reducedType) ? reducedType : declaredType;
    }

    private bool typeMaybeAssignableTo(Type source, Type target)
    {
        if ((source.flags & TypeFlags.Union) == 0)
            return isTypeAssignableTo(source, target);
        if (source.Types().Contains(target))
            return true;
        foreach (var t in source.Types())
            if (isTypeAssignableTo(t, target))
                return true;
        return false;
    }

    private bool isReachableFlowNode(FlowNode flow)
    {
        bool result = isReachableFlowNodeWorker(flow, false);
        lastFlowNode = flow;
        lastFlowNodeReachable = result;
        return result;
    }

    private bool isReachableFlowNodeWorker(FlowNode flow, bool noCacheCheck)
    {
        while (true)
        {
            if (flow == lastFlowNode)
                return lastFlowNodeReachable;
            var flags = flow.Flags;
            if ((flags & FlowFlags.Shared) != 0)
            {
                if (!noCacheCheck)
                {
                    if (flowNodeReachable.TryGetValue(flow, out var reachable))
                        return reachable;
                    return flowNodeReachable[flow] = isReachableFlowNodeWorker(flow, true);
                }

                noCacheCheck = false;
            }

            if ((flags & FlowFlags.Assignment) != 0)
                flow = flow.Antecedent;
            else if ((flags & (FlowFlags.Call | FlowFlags.Condition | FlowFlags.SwitchClause | FlowFlags.Label | FlowFlags.ArrayMutation | FlowFlags.ReduceLabel)) != 0)
                throw unsupported("isReachableFlowNodeWorker: branching flow");
            else
                return (flags & FlowFlags.Unreachable) == 0;
        }
    }
}

public sealed partial class Checker
{
    private readonly symbolArenaLinkStore<MarkedAssignmentSymbolLinks> markedAssignmentSymbolLinks = new();
    private bool isSymbolAssignedDefinitely(Symbol symbol)
    {
        ensureAssignmentsMarked(symbol);
        return markedAssignmentSymbolLinks.Get(symbol).hasDefiniteAssignment;
    }

    private void ensureAssignmentsMarked(Symbol symbol)
    {
        var parent = Ast.FindAncestor(symbol.ValueDeclaration, Ast.IsFunctionOrSourceFile);
        if (parent == null)
            return;
        var links = nodeLinks.Get(parent);
        if ((links.flags & NodeCheckFlags.AssignmentsMarked) == 0)
        {
            links.flags |= NodeCheckFlags.AssignmentsMarked;
            if (!hasParentWithAssignmentsMarked(parent))
                markNodeAssignmentsWorker(parent);
        }
    }

    private bool hasParentWithAssignmentsMarked(Node node) => Ast.FindAncestor(node.Parent, n => Ast.IsFunctionOrSourceFile(n) && (nodeLinks.Get(n).flags & NodeCheckFlags.AssignmentsMarked) != 0) != null;
    private bool markNodeAssignmentsWorker(Node node)
    {
        if (node.Kind == Kind.Identifier)
        {
            // getAssignmentTargetKind: declaration names/initializer identifier reads
            // are None. Explicit assignment target syntax is not admitted.
            if (node.Parent.Kind is not (Kind.VariableDeclaration or Kind.ParenthesizedExpression))
                throw unsupported("markNodeAssignmentsWorker: assignment identifier");
            return false;
        }

        if (Ast.IsTypeNode(node))
            return false;
        if (node.Kind is not (Kind.SourceFile or Kind.Block or Kind.VariableStatement or Kind.VariableDeclarationList or Kind.VariableDeclaration or Kind.ParenthesizedExpression or Kind.NumericLiteral or Kind.StringLiteral or Kind.TrueKeyword or Kind.FalseKeyword or Kind.NullKeyword or Kind.EndOfFile))
            throw unsupported("markNodeAssignmentsWorker: unsupported syntax");
        return node.ForEachChild(markNodeAssignmentsWorker);
    }
}
