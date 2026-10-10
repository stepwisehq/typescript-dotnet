// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/flow.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Original selected branch, assignment, capture, predicate-call, array evolution, loop,
// constructor/static property and post-super flow paths; broader body families remain external.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.Checking;

public sealed partial class Checker
{
    private struct FlowType
    {
        internal Type t;
        internal bool incomplete;
        internal FlowType(Type t, bool incomplete = false)
        {
            this.t = t;
            this.incomplete = incomplete;
        }

        internal bool isNil() => t == null;
    }

    private sealed class FlowState
    {
        internal Node reference, flowContainer;
        internal Type declaredType, initialType;
        internal int depth, sharedFlowStart;
        internal CacheHashKey refKey;
        internal List<FlowReduceLabelData> reduceLabels;
        internal FlowState next;
    }

    // flow.go1651: exact original raw "?" preimage replaces its opaque hash.
    private static readonly CacheHashKey nonDottedNameCacheKey = new("Pw==");
    private readonly record struct FlowLoopKey(FlowNode flowNode, CacheHashKey refKey);
    private readonly record struct FlowLoopInfo(FlowLoopKey key, List<Type> types);
    private readonly Dictionary<FlowLoopKey, Type> flowLoopCache = new();
    private List<FlowLoopInfo> flowLoopStack;
    private readonly record struct SharedFlow(FlowNode flow, FlowType flowType);
    private readonly List<SharedFlow> sharedFlows = new();
    private FlowState freeFlowState;
    private static readonly Dictionary<string, TypeFacts> typeofNEFacts = new()
    {
        ["string"] = TypeFacts.TypeofNEString,
        ["number"] = TypeFacts.TypeofNENumber,
        ["bigint"] = TypeFacts.TypeofNEBigInt,
        ["boolean"] = TypeFacts.TypeofNEBoolean,
        ["symbol"] = TypeFacts.TypeofNESymbol,
        ["undefined"] = TypeFacts.NEUndefined,
        ["object"] = TypeFacts.TypeofNEObject,
        ["function"] = TypeFacts.TypeofNEFunction
    };
    private bool flowAnalysisDisabled;
    private int inlineLevel;
    private List<Type> antecedentTypes;
    private readonly record struct NarrowedTypeKey(Type t, Type candidate, bool assumeTrue, bool checkDerived);
    private readonly Dictionary<NarrowedTypeKey, Type> narrowedTypes = new();
    private readonly HashSet<Symbol> resolvingExplicitTypeOfSymbol = new();
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
    // getFlowTypeOfReferenceEx (82), including authentic evolving-array finalization.
    private Type getFlowTypeOfReferenceEx(Node reference, Type declaredType, Type initialType, Node flowContainer, FlowNode flowNode)
    {
        if (this.flowAnalysisDisabled)
        {
            return this.errorType;
        }

        if ((flowNode == null))
        {
            flowNode = getFlowNodeOfNode(reference);
            if ((flowNode == null))
            {
                return declaredType;
            }
        }

        var f = this.getFlowState();
        f.reference = reference;
        f.declaredType = declaredType;
        f.initialType = initialType ?? declaredType;
        f.flowContainer = flowContainer;
        f.sharedFlowStart = GoRuntime.Len(this.sharedFlows);
        this.flowInvocationCount++;
        var evolvedType = this.getTypeAtFlowNode(f, flowNode).t;
        this.sharedFlows.RemoveRange(f.sharedFlowStart, this.sharedFlows.Count - f.sharedFlowStart);
        this.putFlowState(f);
        Type resultType = default;
        if ((((evolvedType.objectFlags & ObjectFlags.EvolvingArray) != 0) && this.isEvolvingArrayOperationTarget(reference)))
        {
            resultType = this.autoArrayType;
        }
        else
        {
            resultType = this.finalizeEvolvingArrayType(evolvedType);
        }

        if (((resultType == this.unreachableNeverType) || ((((reference.Parent != null) && Ast.IsNonNullExpression(reference.Parent)) && ((resultType.flags & TypeFlags.Never) == 0)) && ((this.getTypeWithFacts(resultType, TypeFacts.NEUndefinedOrNull).flags & TypeFlags.Never) != 0))))
        {
            return declaredType;
        }

        return resultType;
    }

    private FlowType getTypeAtFlowNode(FlowState f, FlowNode flow)
    {
        if (f.depth == 2000)
        {
            flowAnalysisDisabled = true;
            reportFlowControlError(f.reference);
            return new FlowType
            {
                t = errorType
            };
        }

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
            else if ((flags & FlowFlags.Call) != 0)
            {
                t = getTypeAtFlowCall(f, flow);
                if (t.isNil())
                {
                    flow = flow.Antecedent;
                    continue;
                }
            }
            else if ((flags & FlowFlags.Condition) != 0)
                t = getTypeAtFlowCondition(f, flow);
            else if ((flags & FlowFlags.BranchLabel) != 0)
            {
                var antecedents = getBranchLabelAntecedents(flow, f.reduceLabels);
                if (antecedents.Next == null)
                {
                    flow = antecedents.Flow;
                    continue;
                }

                t = getTypeAtFlowBranchLabel(f, flow, antecedents);
            }
            else if ((flags & FlowFlags.ReduceLabel) != 0)
            {
                f.reduceLabels = GoRuntime.Append(f.reduceLabels, flow.Node.AsFlowReduceLabelData());
                t = getTypeAtFlowNode(f, flow.Antecedent);
                f.reduceLabels.RemoveAt(f.reduceLabels.Count - 1);
            }
            else if ((flags & FlowFlags.LoopLabel) != 0)
            {
                if (flow.Antecedents.Next == null)
                {
                    flow = flow.Antecedents.Flow;
                    continue;
                }

                t = getTypeAtFlowLoopLabel(f, flow);
            }
            else if ((flags & FlowFlags.ArrayMutation) != 0)
            {
                t = getTypeAtFlowArrayMutation(f, flow);
                if (t.isNil())
                {
                    flow = flow.Antecedent;
                    continue;
                }
            }
            else if ((flags & FlowFlags.SwitchClause) != 0)
                throw unsupported("getTypeAtFlowNode: excluded switch flow");
            else if ((flags & FlowFlags.Start) != 0)
            {
                var container = flow.Node;
                if (container != null && container != f.flowContainer && !Ast.IsPropertyAccessExpression(f.reference) && !Ast.IsElementAccessExpression(f.reference) && !(f.reference.Kind == Kind.ThisKeyword && !Ast.IsArrowFunction(container)))
                {
                    flow = container.FlowNodeData().FlowNode;
                    continue;
                }

                t = new FlowType
                {
                    t = f.initialType
                };
            }
            else
                t = new FlowType
                {
                    t = convertAutoToAny(f.declaredType)
                };
            if (sharedFlow != null)
                sharedFlows.Add(new SharedFlow(sharedFlow, t));
            f.depth--;
            return t;
        }
    }

    private FlowType getTypeAtFlowAssignment(FlowState f, FlowNode flow)
    {
        var node = flow.Node;
        if (this.isMatchingReference(f.reference, node))
        {
            if (!this.isReachableFlowNode(flow))
            {
                return new FlowType
                {
                    t = this.unreachableNeverType
                };
            }

            if ((getAssignmentTargetKind(node) == AssignmentKind.Compound))
            {
                var flowType = this.getTypeAtFlowNode(f, flow.Antecedent);
                return this.newFlowType(this.getBaseTypeOfLiteralType(flowType.t), flowType.incomplete);
            }

            if (((f.declaredType == this.autoType) || (f.declaredType == this.autoArrayType)))
            {
                if (this.isEmptyArrayAssignment(node))
                {
                    return new FlowType
                    {
                        t = this.getEvolvingArrayType(this.neverType)
                    };
                }

                var assignedType = this.getWidenedLiteralType(this.getInitialOrAssignedType(f, flow));
                if (this.isTypeAssignableTo(assignedType, f.declaredType))
                {
                    return new FlowType
                    {
                        t = assignedType
                    };
                }

                return new FlowType
                {
                    t = this.anyArrayType
                };
            }

            var t = f.declaredType;
            if (isInCompoundLikeAssignment(node))
            {
                t = this.getBaseTypeOfLiteralType(t);
            }

            if (((t.flags & TypeFlags.Union) != 0))
            {
                return new FlowType
                {
                    t = this.getAssignmentReducedType(t, this.getInitialOrAssignedType(f, flow))
                };
            }

            return new FlowType
            {
                t = t
            };
        }

        if (this.containsMatchingReference(f.reference, node))
        {
            if (!this.isReachableFlowNode(flow))
            {
                return new FlowType
                {
                    t = this.unreachableNeverType
                };
            }

            if ((Ast.IsVariableDeclaration(node) && ((Ast.IsInJSFile(node) || Ast.IsVarConstLike(node)))))
            {
                {
                    var init = node.Initializer();
                    if (((init != null) && Ast.IsFunctionExpressionOrArrowFunction(init)))
                    {
                        return this.getTypeAtFlowNode(f, flow.Antecedent);
                    }
                }
            }

            return new FlowType
            {
                t = f.declaredType
            };
        }

        if (((Ast.IsVariableDeclaration(node) && Ast.IsForInStatement(node.Parent.Parent)) && ((this.isMatchingReference(f.reference, node.Parent.Parent.Expression()) || this.optionalChainContainsReference(node.Parent.Parent.Expression(), f.reference)))))
        {
            return new FlowType
            {
                t = this.getNonNullableTypeIfNeeded(this.finalizeEvolvingArrayType(this.getTypeAtFlowNode(f, flow.Antecedent).t))
            };
        }

        return new FlowType
        {
        };
    }

    private bool isMatchingReference(Node source, Node target)
    {
        switch (target.Kind)
        {
            case Kind.ParenthesizedExpression:
            case Kind.NonNullExpression:
                {
                    return this.isMatchingReference(source, target.Expression());
                }

            case Kind.BinaryExpression:
                {
                    return ((Ast.IsAssignmentExpression(target, false) && this.isMatchingReference(source, target.AsBinaryExpression().Left)) || ((Ast.IsBinaryExpression(target) && (target.AsBinaryExpression().OperatorToken.Kind == Kind.CommaToken)) && this.isMatchingReference(source, target.AsBinaryExpression().Right)));
                }
        }

        switch (source.Kind)
        {
            case Kind.MetaProperty:
                {
                    return ((Ast.IsMetaProperty(target) && (source.AsMetaProperty().KeywordToken == target.AsMetaProperty().KeywordToken)) && (source.Name().Text() == target.Name().Text()));
                }

            case Kind.Identifier:
            case Kind.PrivateIdentifier:
                {
                    if (Ast.IsThisInTypeQuery(source))
                    {
                        return (target.Kind == Kind.ThisKeyword);
                    }

                    return ((Ast.IsIdentifier(target) && (this.getResolvedSymbol(source) == this.getResolvedSymbol(target))) || (((Ast.IsVariableDeclaration(target) || Ast.IsBindingElement(target))) && (this.getExportSymbolOfValueSymbolIfExported(this.getResolvedSymbol(source)) == this.getSymbolOfDeclaration(target))));
                }

            case Kind.ThisKeyword:
                {
                    return (target.Kind == Kind.ThisKeyword);
                }

            case Kind.SuperKeyword:
                {
                    return (target.Kind == Kind.SuperKeyword);
                }

            case Kind.NonNullExpression:
            case Kind.ParenthesizedExpression:
            case Kind.SatisfiesExpression:
                {
                    return this.isMatchingReference(source.Expression(), target);
                }

            case Kind.PropertyAccessExpression:
            case Kind.ElementAccessExpression:
                {
                    {
                        var tuple4 = this.getAccessedPropertyName(source);
                        var sourcePropertyName = tuple4.Item1;
                        var ok = tuple4.Item2;
                        if (ok)
                        {
                            if (Ast.IsAccessExpression(target))
                            {
                                {
                                    var tuple5 = this.getAccessedPropertyName(target);
                                    var targetPropertyName = tuple5.Item1;
                                    var ok2 = tuple5.Item2;
                                    if (ok2)
                                    {
                                        return ((targetPropertyName == sourcePropertyName) && this.isMatchingReference(source.Expression(), target.Expression()));
                                    }
                                }
                            }
                        }
                    }

                    if ((Ast.IsElementAccessExpression(source) && Ast.IsElementAccessExpression(target)))
                    {
                        var sourceArg = source.AsElementAccessExpression().ArgumentExpression;
                        var targetArg = target.AsElementAccessExpression().ArgumentExpression;
                        if ((Ast.IsIdentifier(sourceArg) && Ast.IsIdentifier(targetArg)))
                        {
                            var symbol = this.getResolvedSymbol(sourceArg);
                            if (((symbol == this.getResolvedSymbol(targetArg)) && ((this.isConstantVariable(symbol) || (this.isParameterOrMutableLocalVariable(symbol) && !this.isSymbolAssigned(symbol))))))
                            {
                                return this.isMatchingReference(source.Expression(), target.Expression());
                            }
                        }
                    }

                    break;
                }

            case Kind.QualifiedName:
                {
                    if (Ast.IsAccessExpression(target))
                    {
                        {
                            var tuple6 = this.getAccessedPropertyName(target);
                            var targetPropertyName2 = tuple6.Item1;
                            var ok3 = tuple6.Item2;
                            if (ok3)
                            {
                                return ((source.AsQualifiedName().Right.Text() == targetPropertyName2) && this.isMatchingReference(source.AsQualifiedName().Left, target.Expression()));
                            }
                        }
                    }

                    break;
                }

            case Kind.BinaryExpression:
                {
                    return ((Ast.IsBinaryExpression(source) && (source.AsBinaryExpression().OperatorToken.Kind == Kind.CommaToken)) && this.isMatchingReference(source.AsBinaryExpression().Right, target));
                }
        }

        return false;
    }

    private Type getInitialOrAssignedType(FlowState f, FlowNode flow)
    {
        if ((Ast.IsVariableDeclaration(flow.Node) || Ast.IsBindingElement(flow.Node)))
        {
            return this.getNarrowableTypeForReference(this.getInitialType(flow.Node), f.reference, CheckMode.Normal);
        }

        return this.getNarrowableTypeForReference(this.getAssignedType(flow.Node), f.reference, CheckMode.Normal);
    }

    private Type getInitialType(Node node)
    {
        if (node.Kind == Kind.VariableDeclaration)
            return getInitialTypeOfVariableDeclaration(node);
        if (node.Kind == Kind.BindingElement)
            throw unsupported("getInitialType: excluded binding element");
        throw new InvalidOperationException("Unhandled case in getInitialType");
    }

    private Type getInitialTypeOfVariableDeclaration(Node node)
    {
        if ((node.Initializer() != null))
        {
            return this.getTypeOfInitializer(node.Initializer());
        }

        if (Ast.IsForInStatement(node.Parent.Parent))
        {
            return this.stringType;
        }

        if (Ast.IsForOfStatement(node.Parent.Parent))
        {
            var t = this.checkRightHandSideOfForOf(node.Parent.Parent);
            if ((t != null))
            {
                return t;
            }
        }

        return this.errorType;
    }

    private Type getTypeOfInitializer(Node node)
    {
        if (this.typeNodeLinks.Has(node))
        {
            var t = this.typeNodeLinks.Get(node).resolvedType;
            if ((t != null))
            {
                return t;
            }
        }

        return this.getTypeOfExpression(node);
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
        var filteredType = this.filterType(declaredType, (t) =>
        {
            return this.typeMaybeAssignableTo(assignedType, t);
        });
        var reducedType = filteredType;
        if ((((assignedType.flags & TypeFlags.BooleanLiteral) != 0) && isFreshLiteralType(assignedType)))
        {
            reducedType = this.mapType(filteredType, this.getFreshTypeOfLiteralType);
        }

        if (this.isTypeAssignableTo(assignedType, reducedType))
        {
            return reducedType;
        }

        return declaredType;
    }

    private bool typeMaybeAssignableTo(Type source, Type target)
    {
        if (((source.flags & TypeFlags.Union) == 0))
        {
            return this.isTypeAssignableTo(source, target);
        }

        if (containsType(source.Types(), target))
        {
            return true;
        }

        {
            foreach (var t in GoRuntime.RangeValues(source.Types()))
            {
                if (this.isTypeAssignableTo(t, target))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool isReachableFlowNode(FlowNode flow)
    {
        var f = this.getFlowState();
        var result = this.isReachableFlowNodeWorker(f, flow, false);
        this.putFlowState(f);
        this.lastFlowNode = flow;
        this.lastFlowNodeReachable = result;
        return result;
    }

    private bool isReachableFlowNodeWorker(FlowState f, FlowNode flow, bool noCacheCheck)
    {
        while (true)
        {
            if ((flow == this.lastFlowNode))
            {
                return this.lastFlowNodeReachable;
            }

            var flags = flow.Flags;
            if (((flags & FlowFlags.Shared) != 0))
            {
                if ((!noCacheCheck && (GoRuntime.Len(f.reduceLabels) == 0)))
                {
                    {
                        var ok = this.flowNodeReachable.TryGetValue(flow, out var reachable);
                        if (ok)
                        {
                            return reachable;
                        }
                    }

                    var reachable2 = this.isReachableFlowNodeWorker(f, flow, true);
                    this.flowNodeReachable[flow] = reachable2;
                    return reachable2;
                }

                noCacheCheck = false;
            }

            switch (true)
            {
                case true when ((flags & (((FlowFlags.Assignment | FlowFlags.Condition) | FlowFlags.ArrayMutation))) != 0):
                    {
                        flow = flow.Antecedent;
                        break;
                    }

                case true when ((flags & FlowFlags.Call) != 0):
                    {
                        {
                            var signature = this.getEffectsSignature(flow.Node);
                            if ((signature != null))
                            {
                                {
                                    var predicate = this.getTypePredicateOfSignature(signature);
                                    if ((((predicate != null) && (predicate.kind == TypePredicateKind.AssertsIdentifier)) && (predicate.t == null)))
                                    {
                                        {
                                            var arguments = flow.Node.Arguments();
                                            if ((((predicate.parameterIndex >= 0) && ((int)(predicate.parameterIndex) < GoRuntime.Len(arguments))) && this.isFalseExpression(arguments[predicate.parameterIndex])))
                                            {
                                                return false;
                                            }
                                        }
                                    }
                                }

                                if (((this.getReturnTypeOfSignature(signature).flags & TypeFlags.Never) != 0))
                                {
                                    return false;
                                }
                            }
                        }

                        flow = flow.Antecedent;
                        break;
                    }

                case true when ((flags & FlowFlags.BranchLabel) != 0):
                    {
                        {
                            var list = getBranchLabelAntecedents(flow, f.reduceLabels);
                            for (; (list != null); list = list.Next)
                            {
                                if (this.isReachableFlowNodeWorker(f, list.Flow, false))
                                {
                                    return true;
                                }
                            }
                        }

                        return false;
                    }

                case true when ((flags & FlowFlags.LoopLabel) != 0):
                    {
                        if ((flow.Antecedents == null))
                        {
                            return false;
                        }

                        flow = flow.Antecedents.Flow;
                        break;
                    }

                case true when ((flags & FlowFlags.SwitchClause) != 0):
                    {
                        throw unsupported("isReachableFlowNodeWorker: excluded switch");
                    }

                case true when ((flags & FlowFlags.ReduceLabel) != 0):
                    {
                        this.lastFlowNode = null;
                        f.reduceLabels = GoRuntime.Append(f.reduceLabels, flow.Node.AsFlowReduceLabelData());
                        var result = this.isReachableFlowNodeWorker(f, flow.Antecedent, false);
                        f.reduceLabels = GoRuntime.Slice(f.reduceLabels, 0, (GoRuntime.Len(f.reduceLabels) - 1));
                        return result;
                    }

                default:
                    {
                        return ((flags & FlowFlags.Unreachable) == 0);
                    }
            }
        }
    }
}

public sealed partial class Checker
{
    private readonly LinkStore<Symbol, MarkedAssignmentSymbolLinks> markedAssignmentSymbolLinks = new();
    private bool isSymbolAssignedDefinitely(Symbol symbol)
    {
        this.ensureAssignmentsMarked(symbol);
        return this.markedAssignmentSymbolLinks.Get(symbol).hasDefiniteAssignment;
    }

    private void ensureAssignmentsMarked(Symbol symbol)
    {
        var parent = Ast.FindAncestor(symbol.ValueDeclaration, Ast.IsFunctionOrSourceFile);
        if ((parent == null))
        {
            return;
        }

        var links = this.nodeLinks.Get(parent);
        if (((links.flags & NodeCheckFlags.AssignmentsMarked) == 0))
        {
            links.flags |= NodeCheckFlags.AssignmentsMarked;
            if (!this.hasParentWithAssignmentsMarked(parent))
            {
                this.markNodeAssignmentsWorker(parent);
            }
        }
    }

    private bool markNodeAssignmentsWorker(Node node)
    {
        switch (node.Kind)
        {
            case Kind.Identifier:
                {
                    var assignmentKind = getAssignmentTargetKind(node);
                    if ((assignmentKind != AssignmentKind.None))
                    {
                        var symbol = this.getResolvedSymbol(node);
                        if (this.isParameterOrMutableLocalVariable(symbol))
                        {
                            var links = this.markedAssignmentSymbolLinks.Get(symbol);
                            {
                                var pos = links.lastAssignmentPos;
                                if (((pos == 0) || (pos != int.MaxValue)))
                                {
                                    var referencingFunction = Ast.FindAncestor(node, Ast.IsFunctionOrSourceFile);
                                    var declaringFunction = Ast.FindAncestor(symbol.ValueDeclaration, Ast.IsFunctionOrSourceFile);
                                    if ((referencingFunction == declaringFunction))
                                    {
                                        links.lastAssignmentPos = (int)(this.extendAssignmentPosition(node, symbol.ValueDeclaration));
                                    }
                                    else
                                    {
                                        links.lastAssignmentPos = int.MaxValue;
                                    }
                                }
                            }

                            if ((assignmentKind == AssignmentKind.Definite))
                            {
                                links.hasDefiniteAssignment = true;
                            }
                        }
                    }

                    return false;
                }

            case Kind.ExportSpecifier:
                {
                    var exportDeclaration = node.AsExportSpecifier().Parent.Parent.AsExportDeclaration();
                    var name = node.PropertyNameOrName();
                    if ((((!node.IsTypeOnly() && !exportDeclaration.IsTypeOnly) && (exportDeclaration.ModuleSpecifier == null)) && !Ast.IsStringLiteral(name)))
                    {
                        var symbol2 = this.resolveEntityName(name, SymbolFlags.Value, true, true, null);
                        if (((symbol2 != null) && this.isParameterOrMutableLocalVariable(symbol2)))
                        {
                            var links2 = this.markedAssignmentSymbolLinks.Get(symbol2);
                            links2.lastAssignmentPos = int.MaxValue;
                        }
                    }

                    return false;
                }

            case Kind.InterfaceDeclaration:
            case Kind.TypeAliasDeclaration:
            case Kind.JSTypeAliasDeclaration:
            case Kind.EnumDeclaration:
                {
                    return false;
                }
        }

        if (Ast.IsTypeNode(node))
        {
            return false;
        }

        return node.ForEachChild(this.markNodeAssignmentsWorker);
    }
}

public sealed partial class Checker
{
    private bool containsMatchingReference(Node source, Node target)
    {
        while (Ast.IsAccessExpression(source))
        {
            source = source.Expression();
            if (this.isMatchingReference(source, target))
            {
                return true;
            }
        }

        return false;
    }
}

public sealed partial class Checker
{
    private Type getFlowTypeOfReference(Node reference, Type declaredType)
    {
        return this.getFlowTypeOfReferenceEx(reference, declaredType, declaredType, null, null);
    }
}

public sealed partial class Checker
{
    private static FlowList getBranchLabelAntecedents(FlowNode flow, List<FlowReduceLabelData> reduceLabels)
    {
        var i = GoRuntime.Len(reduceLabels);
        while ((i != 0))
        {
            i--;
            var data = reduceLabels[i];
            if ((data.Target == flow))
            {
                return data.Antecedents;
            }
        }

        return flow.Antecedents;
    }
}

public sealed partial class Checker
{
    private bool isEmptyArrayAssignment(Node node)
    {
        return Ast.IsVariableDeclaration(node) && node.Initializer() is not null && Ast.IsEmptyArrayLiteral(node.Initializer()) || !Ast.IsBindingElement(node) && Ast.IsBinaryExpression(node.Parent) && Ast.IsEmptyArrayLiteral(node.Parent.AsBinaryExpression().Right);
    }

    private FlowType getTypeAtFlowCall(FlowState f, FlowNode flow)
    {
        var signature = this.getEffectsSignature(flow.Node);
        if ((signature != null))
        {
            var predicate = this.getTypePredicateOfSignature(signature);
            if (((predicate != null) && (((predicate.kind == TypePredicateKind.AssertsThis) || (predicate.kind == TypePredicateKind.AssertsIdentifier)))))
            {
                var flowType = this.getTypeAtFlowNode(f, flow.Antecedent);
                var t = this.finalizeEvolvingArrayType(flowType.t);
                Type narrowedType = default;
                switch (true)
                {
                    case true when (predicate.t != null):
                        {
                            narrowedType = this.narrowTypeByTypePredicate(f, t, predicate, flow.Node, true);
                            break;
                        }

                    case true when (((predicate.kind == TypePredicateKind.AssertsIdentifier) && (predicate.parameterIndex >= 0)) && ((int)(predicate.parameterIndex) < GoRuntime.Len(flow.Node.Arguments()))):
                        {
                            narrowedType = this.narrowTypeByAssertion(f, t, flow.Node.Arguments()[predicate.parameterIndex]);
                            break;
                        }

                    default:
                        {
                            narrowedType = t;
                            break;
                        }
                }

                if ((narrowedType == t))
                {
                    return flowType;
                }

                return this.newFlowType(narrowedType, flowType.incomplete);
            }

            if (((this.getReturnTypeOfSignature(signature).flags & TypeFlags.Never) != 0))
            {
                return new FlowType
                {
                    t = this.unreachableNeverType
                };
            }
        }

        return new FlowType
        {
        };
    }
}

public sealed partial class Checker
{
    private Type narrowTypeByTypePredicate(FlowState f, Type t, TypePredicate predicate, Node callExpression, bool assumeTrue)
    {
        if (((predicate.t != null) && !((IsTypeAny(t) && (((predicate.t == this.globalObjectType) || (predicate.t == this.globalFunctionType)))))))
        {
            var predicateArgument = this.getTypePredicateArgument(predicate, callExpression);
            if ((predicateArgument != null))
            {
                if (this.isMatchingReference(f.reference, predicateArgument))
                {
                    return this.getNarrowedType(t, predicate.t, assumeTrue, false);
                }

                if (((this.strictNullChecks && this.optionalChainContainsReference(predicateArgument, f.reference)) && (((assumeTrue && !this.hasTypeFacts(predicate.t, TypeFacts.EQUndefined)) || (!assumeTrue && everyType(predicate.t, this.IsNullableType))))))
                {
                    t = this.getAdjustedTypeWithFacts(t, TypeFacts.NEUndefinedOrNull);
                }

                var access = this.getDiscriminantPropertyAccess(f, predicateArgument, t);
                if ((access != null))
                {
                    return this.narrowTypeByDiscriminant(t, access, (t2) =>
                    {
                        return this.getNarrowedType(t2, predicate.t, assumeTrue, false);
                    });
                }
            }
        }

        return t;
    }
}

public sealed partial class Checker
{
    private Type narrowTypeByAssertion(FlowState f, Type t, Node expr)
    {
        var node = Ast.SkipParentheses(expr);
        if ((node.Kind == Kind.FalseKeyword))
        {
            return this.unreachableNeverType;
        }

        if ((node.Kind == Kind.BinaryExpression))
        {
            if ((node.AsBinaryExpression().OperatorToken.Kind == Kind.AmpersandAmpersandToken))
            {
                return this.narrowTypeByAssertion(f, this.narrowTypeByAssertion(f, t, node.AsBinaryExpression().Left), node.AsBinaryExpression().Right);
            }

            if ((node.AsBinaryExpression().OperatorToken.Kind == Kind.BarBarToken))
            {
                return this.getUnionType(new List<Type> { this.narrowTypeByAssertion(f, t, node.AsBinaryExpression().Left), this.narrowTypeByAssertion(f, t, node.AsBinaryExpression().Right) });
            }
        }

        return this.narrowType(f, t, node, true);
    }
}

public sealed partial class Checker
{
    private FlowType getTypeAtFlowCondition(FlowState f, FlowNode flow)
    {
        var flowType = this.getTypeAtFlowNode(f, flow.Antecedent);
        if (((flowType.t.flags & TypeFlags.Never) != 0))
        {
            return flowType;
        }

        var assumeTrue = ((flow.Flags & FlowFlags.TrueCondition) != 0);
        var nonEvolvingType = this.finalizeEvolvingArrayType(flowType.t);
        var narrowedType = this.narrowType(f, nonEvolvingType, flow.Node, assumeTrue);
        if ((narrowedType == nonEvolvingType))
        {
            return flowType;
        }

        return this.newFlowType(narrowedType, flowType.incomplete);
    }
}

public sealed partial class Checker
{
    private Type narrowType(FlowState f, Type t, Node expr, bool assumeTrue)
    {
        if ((Ast.IsExpressionOfOptionalChainRoot(expr) || ((Ast.IsBinaryExpression(expr.Parent) && (((expr.Parent.AsBinaryExpression().OperatorToken.Kind == Kind.QuestionQuestionToken) || (expr.Parent.AsBinaryExpression().OperatorToken.Kind == Kind.QuestionQuestionEqualsToken)))) && (expr.Parent.AsBinaryExpression().Left == expr))))
        {
            return this.narrowTypeByOptionality(f, t, expr, assumeTrue);
        }

        switch (expr.Kind)
        {
            case Kind.Identifier:
                {
                    if ((!this.isMatchingReference(f.reference, expr) && (this.inlineLevel < 5)))
                    {
                        var symbol = this.getResolvedSymbol(expr);
                        if (this.isConstantVariable(symbol))
                        {
                            var declaration = symbol.ValueDeclaration;
                            if ((((((declaration != null) && Ast.IsVariableDeclaration(declaration)) && (declaration.Type() == null)) && (declaration.Initializer() != null)) && this.isConstantReference(f.reference)))
                            {
                                this.inlineLevel++;
                                var result = this.narrowType(f, t, declaration.Initializer(), assumeTrue);
                                this.inlineLevel--;
                                return result;
                            }
                        }
                    }

                    goto case Kind.ThisKeyword;
                }

            case Kind.ThisKeyword:
            case Kind.SuperKeyword:
            case Kind.PropertyAccessExpression:
            case Kind.ElementAccessExpression:
                {
                    return this.narrowTypeByTruthiness(f, t, expr, assumeTrue);
                }

            case Kind.CallExpression:
                {
                    return this.narrowTypeByCallExpression(f, t, expr, assumeTrue);
                }

            case Kind.ParenthesizedExpression:
            case Kind.NonNullExpression:
            case Kind.SatisfiesExpression:
                {
                    return this.narrowType(f, t, expr.Expression(), assumeTrue);
                }

            case Kind.BinaryExpression:
                {
                    return this.narrowTypeByBinaryExpression(f, t, expr.AsBinaryExpression(), assumeTrue);
                }

            case Kind.PrefixUnaryExpression:
                {
                    if ((expr.AsPrefixUnaryExpression().Operator == Kind.ExclamationToken))
                    {
                        return this.narrowType(f, t, expr.AsPrefixUnaryExpression().Operand, !assumeTrue);
                    }

                    break;
                }
        }

        return t;
    }
}

public sealed partial class Checker
{
    private Type narrowTypeByOptionality(FlowState f, Type t, Node expr, bool assumePresent)
    {
        if (this.isMatchingReference(f.reference, expr))
        {
            return this.getAdjustedTypeWithFacts(t, Core.IfElse(assumePresent, TypeFacts.NEUndefinedOrNull, TypeFacts.EQUndefinedOrNull));
        }

        var access = this.getDiscriminantPropertyAccess(f, expr, t);
        if ((access != null))
        {
            return this.narrowTypeByDiscriminant(t, access, (t2) =>
            {
                return this.getTypeWithFacts(t2, Core.IfElse(assumePresent, TypeFacts.NEUndefinedOrNull, TypeFacts.EQUndefinedOrNull));
            });
        }

        return t;
    }
}

public sealed partial class Checker
{
    private Type narrowTypeByTruthiness(FlowState f, Type t, Node expr, bool assumeTrue)
    {
        if (this.isMatchingReference(f.reference, expr))
        {
            return this.getAdjustedTypeWithFacts(t, Core.IfElse(assumeTrue, TypeFacts.Truthy, TypeFacts.Falsy));
        }

        if (((this.strictNullChecks && assumeTrue) && this.optionalChainContainsReference(expr, f.reference)))
        {
            t = this.getAdjustedTypeWithFacts(t, TypeFacts.NEUndefinedOrNull);
        }

        var access = this.getDiscriminantPropertyAccess(f, expr, t);
        if ((access != null))
        {
            return this.narrowTypeByDiscriminant(t, access, (t2) =>
            {
                return this.getTypeWithFacts(t2, Core.IfElse(assumeTrue, TypeFacts.Truthy, TypeFacts.Falsy));
            });
        }

        return t;
    }
}

public sealed partial class Checker
{
    private Type narrowTypeByCallExpression(FlowState f, Type t, Node callExpression, bool assumeTrue)
    {
        if (this.hasMatchingArgument(callExpression, f.reference))
        {
            TypePredicate predicate = default;
            if ((assumeTrue || !isCallChain(callExpression)))
            {
                var signature = this.getEffectsSignature(callExpression);
                if ((signature != null))
                {
                    predicate = this.getTypePredicateOfSignature(signature);
                }
            }

            if (((predicate != null) && (((predicate.kind == TypePredicateKind.This) || (predicate.kind == TypePredicateKind.Identifier)))))
            {
                return this.narrowTypeByTypePredicate(f, t, predicate, callExpression, assumeTrue);
            }
        }

        if (((this.containsMissingType(t) && Ast.IsAccessExpression(f.reference)) && Ast.IsPropertyAccessExpression(callExpression.Expression())))
        {
            var callAccess = callExpression.Expression();
            if ((((this.isMatchingReference(f.reference.Expression(), this.getReferenceCandidate(callAccess.Expression())) && Ast.IsIdentifier(callAccess.Name())) && (callAccess.Name().Text() == "hasOwnProperty")) && (GoRuntime.Len(callExpression.Arguments()) == 1)))
            {
                var argument = callExpression.Arguments()[0];
                {
                    var tuple1 = this.getAccessedPropertyName(f.reference);
                    var accessedName = tuple1.Item1;
                    var ok = tuple1.Item2;
                    if (((ok && Ast.IsStringLiteralLike(argument)) && (accessedName == argument.Text())))
                    {
                        return this.getTypeWithFacts(t, Core.IfElse(assumeTrue, TypeFacts.NEUndefined, TypeFacts.EQUndefined));
                    }
                }
            }
        }

        return t;
    }
}

public sealed partial class Checker
{
    private Type narrowTypeByBinaryExpression(FlowState f, Type t, BinaryExpression expr, bool assumeTrue)
    {
        switch (expr.OperatorToken.Kind)
        {
            case Kind.EqualsToken:
            case Kind.BarBarEqualsToken:
            case Kind.AmpersandAmpersandEqualsToken:
            case Kind.QuestionQuestionEqualsToken:
                {
                    return this.narrowTypeByTruthiness(f, this.narrowType(f, t, expr.Right, assumeTrue), expr.Left, assumeTrue);
                }

            case Kind.EqualsEqualsToken:
            case Kind.ExclamationEqualsToken:
            case Kind.EqualsEqualsEqualsToken:
            case Kind.ExclamationEqualsEqualsToken:
                {
                    var @operator = expr.OperatorToken.Kind;
                    var left = this.getReferenceCandidate(expr.Left);
                    var right = this.getReferenceCandidate(expr.Right);
                    if (((left.Kind == Kind.TypeOfExpression) && Ast.IsStringLiteralLike(right)))
                    {
                        return this.narrowTypeByTypeof(f, t, left.AsTypeOfExpression(), @operator, right, assumeTrue);
                    }

                    if (((right.Kind == Kind.TypeOfExpression) && Ast.IsStringLiteralLike(left)))
                    {
                        return this.narrowTypeByTypeof(f, t, right.AsTypeOfExpression(), @operator, left, assumeTrue);
                    }

                    if (this.isMatchingReference(f.reference, left))
                    {
                        return this.narrowTypeByEquality(t, @operator, right, assumeTrue);
                    }

                    if (this.isMatchingReference(f.reference, right))
                    {
                        return this.narrowTypeByEquality(t, @operator, left, assumeTrue);
                    }

                    if (this.strictNullChecks)
                    {
                        if (this.optionalChainContainsReference(left, f.reference))
                        {
                            throw unsupported("narrowTypeByOptionalChainContainment: excluded optional chain");
                        }
                        else
                        {
                            if (this.optionalChainContainsReference(right, f.reference))
                            {
                                throw unsupported("narrowTypeByOptionalChainContainment: excluded optional chain");
                            }
                        }
                    }

                    var leftAccess = this.getDiscriminantPropertyAccess(f, left, t);
                    if ((leftAccess != null))
                    {
                        return this.narrowTypeByDiscriminantProperty(t, leftAccess, @operator, right, assumeTrue);
                    }

                    var rightAccess = this.getDiscriminantPropertyAccess(f, right, t);
                    if ((rightAccess != null))
                    {
                        return this.narrowTypeByDiscriminantProperty(t, rightAccess, @operator, left, assumeTrue);
                    }

                    if (this.isMatchingConstructorReference(f, left))
                    {
                        return this.narrowTypeByConstructor(t, @operator, right, assumeTrue);
                    }

                    if (this.isMatchingConstructorReference(f, right))
                    {
                        return this.narrowTypeByConstructor(t, @operator, left, assumeTrue);
                    }

                    if ((Ast.IsBooleanLiteral(right) && !Ast.IsAccessExpression(left)))
                    {
                        return this.narrowTypeByBooleanComparison(f, t, left, right, @operator, assumeTrue);
                    }

                    if ((Ast.IsBooleanLiteral(left) && !Ast.IsAccessExpression(right)))
                    {
                        return this.narrowTypeByBooleanComparison(f, t, right, left, @operator, assumeTrue);
                    }

                    break;
                }

            case Kind.InstanceOfKeyword:
                {
                    throw unsupported("narrowTypeByBinaryExpression: excluded instanceof");
                }

            case Kind.InKeyword:
                {
                    throw unsupported("narrowTypeByBinaryExpression: excluded in");
                }

            case Kind.CommaToken:
                {
                    return this.narrowType(f, t, expr.Right, assumeTrue);
                }

            case Kind.AmpersandAmpersandToken:
                {
                    if (assumeTrue)
                    {
                        return this.narrowType(f, this.narrowType(f, t, expr.Left, true), expr.Right, true);
                    }

                    return this.getUnionType(new List<Type> { this.narrowType(f, t, expr.Left, false), this.narrowType(f, t, expr.Right, false) });
                }

            case Kind.BarBarToken:
                {
                    if (assumeTrue)
                    {
                        return this.getUnionType(new List<Type> { this.narrowType(f, t, expr.Left, true), this.narrowType(f, t, expr.Right, true) });
                    }

                    return this.narrowType(f, this.narrowType(f, t, expr.Left, false), expr.Right, false);
                }
        }

        return t;
    }
}

public sealed partial class Checker
{
    private Type narrowTypeByEquality(Type t, Kind @operator, Node value, bool assumeTrue)
    {
        if (((t.flags & TypeFlags.Any) != 0))
        {
            return t;
        }

        if (((@operator == Kind.ExclamationEqualsToken) || (@operator == Kind.ExclamationEqualsEqualsToken)))
        {
            assumeTrue = !assumeTrue;
        }

        var valueType = this.getTypeOfExpression(value);
        var doubleEquals = ((@operator == Kind.EqualsEqualsToken) || (@operator == Kind.ExclamationEqualsToken));
        if (((valueType.flags & TypeFlags.Nullable) != 0))
        {
            if (!this.strictNullChecks)
            {
                return t;
            }

            TypeFacts facts = default;
            switch (true)
            {
                case true when doubleEquals:
                    {
                        facts = Core.IfElse(assumeTrue, TypeFacts.EQUndefinedOrNull, TypeFacts.NEUndefinedOrNull);
                        break;
                    }

                case true when ((valueType.flags & TypeFlags.Null) != 0):
                    {
                        facts = Core.IfElse(assumeTrue, TypeFacts.EQNull, TypeFacts.NENull);
                        break;
                    }

                default:
                    {
                        facts = Core.IfElse(assumeTrue, TypeFacts.EQUndefined, TypeFacts.NEUndefined);
                        break;
                    }
            }

            return this.getAdjustedTypeWithFacts(t, facts);
        }

        if (assumeTrue)
        {
            if ((!doubleEquals && ((((t.flags & TypeFlags.Unknown) != 0) || someType(t, this.IsEmptyAnonymousObjectType)))))
            {
                if ((((valueType.flags & ((TypeFlags.Primitive | TypeFlags.NonPrimitive))) != 0) || this.IsEmptyAnonymousObjectType(valueType)))
                {
                    return valueType;
                }

                if (((valueType.flags & TypeFlags.Object) != 0))
                {
                    return this.nonPrimitiveType;
                }
            }

            if (((!doubleEquals && ((valueType.flags & TypeFlags.Primitive) != 0)) && this.isUniformUnionType(t)))
            {
                var regularType = this.getRegularTypeOfLiteralType(valueType);
                if (this.unionContainsType(t, regularType, false))
                {
                    return regularType;
                }
            }

            var filteredType = this.filterType(t, (t2) =>
            {
                return (this.areTypesComparable(t2, valueType) || (doubleEquals && isCoercibleUnderDoubleEquals(t2, valueType)));
            });
            return this.replacePrimitivesWithLiterals(filteredType, valueType);
        }

        if (isUnitType(valueType))
        {
            if (this.isUniformUnionType(t))
            {
                var filteredType2 = this.removeType(t, this.getRegularTypeOfLiteralType(valueType));
                if ((filteredType2 != t))
                {
                    return filteredType2;
                }
            }

            return this.filterType(t, (t3) =>
            {
                return !((this.isUnitLikeType(t3) && this.areTypesComparable(t3, valueType)));
            });
        }

        return t;
    }
}

public sealed partial class Checker
{
    private Type narrowTypeByTypeof(FlowState f, Type t, TypeOfExpression typeOfExpr, Kind @operator, Node literal, bool assumeTrue)
    {
        if (((@operator == Kind.ExclamationEqualsToken) || (@operator == Kind.ExclamationEqualsEqualsToken)))
        {
            assumeTrue = !assumeTrue;
        }

        var target = this.getReferenceCandidate(typeOfExpr.Expression);
        if (!this.isMatchingReference(f.reference, target))
        {
            if (((this.strictNullChecks && this.optionalChainContainsReference(target, f.reference)) && (assumeTrue == ((literal.Text() != "undefined")))))
            {
                t = this.getAdjustedTypeWithFacts(t, TypeFacts.NEUndefinedOrNull);
            }

            var propertyAccess = this.getDiscriminantPropertyAccess(f, target, t);
            if ((propertyAccess != null))
            {
                return this.narrowTypeByDiscriminant(t, propertyAccess, (t2) =>
                {
                    return this.narrowTypeByLiteralExpression(t2, literal, assumeTrue);
                });
            }

            return t;
        }

        return this.narrowTypeByLiteralExpression(t, literal, assumeTrue);
    }
}

public sealed partial class Checker
{
    private Type narrowTypeByLiteralExpression(Type t, Node literal, bool assumeTrue)
    {
        if (assumeTrue)
        {
            return this.narrowTypeByTypeName(t, literal.Text());
        }

        var ok = typeofNEFacts.TryGetValue(literal.Text(), out var facts);
        if (!ok)
        {
            facts = TypeFacts.TypeofNEHostObject;
        }

        return this.getAdjustedTypeWithFacts(t, facts);
    }
}

public sealed partial class Checker
{
    private Type narrowTypeByTypeName(Type t, string typeName)
    {
        switch (typeName)
        {
            case "string":
                {
                    return this.narrowTypeByTypeFacts(t, this.stringType, TypeFacts.TypeofEQString);
                }

            case "number":
                {
                    return this.narrowTypeByTypeFacts(t, this.numberType, TypeFacts.TypeofEQNumber);
                }

            case "bigint":
                {
                    return this.narrowTypeByTypeFacts(t, this.bigintType, TypeFacts.TypeofEQBigInt);
                }

            case "boolean":
                {
                    return this.narrowTypeByTypeFacts(t, this.booleanType, TypeFacts.TypeofEQBoolean);
                }

            case "symbol":
                {
                    return this.narrowTypeByTypeFacts(t, this.esSymbolType, TypeFacts.TypeofEQSymbol);
                }

            case "object":
                {
                    if (((t.flags & TypeFlags.Any) != 0))
                    {
                        return t;
                    }

                    return this.getUnionType(new List<Type> { this.narrowTypeByTypeFacts(t, this.nonPrimitiveType, TypeFacts.TypeofEQObject), this.narrowTypeByTypeFacts(t, this.nullType, TypeFacts.EQNull) });
                }

            case "function":
                {
                    if (((t.flags & TypeFlags.Any) != 0))
                    {
                        return t;
                    }

                    return this.narrowTypeByTypeFacts(t, this.globalFunctionType, TypeFacts.TypeofEQFunction);
                }

            case "undefined":
                {
                    return this.narrowTypeByTypeFacts(t, this.undefinedType, TypeFacts.EQUndefined);
                }
        }

        return this.narrowTypeByTypeFacts(t, this.nonPrimitiveType, TypeFacts.TypeofEQHostObject);
    }
}

public sealed partial class Checker
{
    private Type narrowTypeByTypeFacts(Type t, Type impliedType, TypeFacts facts)
    {
        return this.mapType(t, (t2) =>
        {
            switch (true)
            {
                case true when this.isTypeRelatedTo(t2, impliedType, this.strictSubtypeRelation):
                    {
                        if (this.hasTypeFacts(t2, facts))
                        {
                            return t2;
                        }

                        return this.neverType;
                    }

                case true when this.isTypeSubtypeOf(impliedType, t2):
                    {
                        return impliedType;
                    }

                case true when this.hasTypeFacts(t2, facts):
                    {
                        return this.getIntersectionType(new List<Type> { t2, impliedType });
                    }
            }

            return this.neverType;
        });
    }
}

public sealed partial class Checker
{
    private Type narrowTypeByDiscriminantProperty(Type t, Node access, Kind @operator, Node value, bool assumeTrue)
    {
        if (((((@operator == Kind.EqualsEqualsEqualsToken) || (@operator == Kind.ExclamationEqualsEqualsToken))) && ((t.flags & TypeFlags.Union) != 0)))
        {
            var keyPropertyName = this.getKeyPropertyName(t);
            if ((keyPropertyName != ""))
            {
                {
                    var tuple1 = this.getAccessedPropertyName(access);
                    var accessedName = tuple1.Item1;
                    var ok = tuple1.Item2;
                    if ((ok && (keyPropertyName == accessedName)))
                    {
                        var candidate = this.getConstituentTypeForKeyType(t, this.getTypeOfExpression(value));
                        if ((candidate != null))
                        {
                            if (((assumeTrue && (@operator == Kind.EqualsEqualsEqualsToken)) || (!assumeTrue && (@operator == Kind.ExclamationEqualsEqualsToken))))
                            {
                                return candidate;
                            }

                            {
                                var propType = this.getTypeOfPropertyOfType(candidate, keyPropertyName);
                                if (((propType != null) && isUnitType(propType)))
                                {
                                    return this.removeType(t, candidate);
                                }
                            }

                            return t;
                        }
                    }
                }
            }
        }

        return this.narrowTypeByDiscriminant(t, access, (t2) =>
        {
            return this.narrowTypeByEquality(t2, @operator, value, assumeTrue);
        });
    }
}

public sealed partial class Checker
{
    private Type narrowTypeByDiscriminant(Type t, Node access, Func<Type, Type> narrowType)
    {
        var tuple1 = this.getAccessedPropertyName(access);
        var propName = tuple1.Item1;
        var ok = tuple1.Item2;
        if (!ok)
        {
            return t;
        }

        var optionalChain = Ast.IsOptionalChain(access);
        var removeNullable = ((this.strictNullChecks && ((optionalChain || isNonNullAccess(access)))) && this.maybeTypeOfKind(t, TypeFlags.Nullable));
        var nonNullType = t;
        if (removeNullable)
        {
            nonNullType = this.getTypeWithFacts(t, TypeFacts.NEUndefinedOrNull);
        }

        var propType = this.getTypeOfPropertyOfType(nonNullType, propName);
        if ((propType == null))
        {
            return t;
        }

        if ((removeNullable && optionalChain))
        {
            propType = this.getOptionalType(propType, false);
        }

        var narrowedPropType = narrowType(propType);
        return this.filterType(t, (t3) =>
        {
            var discriminantType = Core.OrElse(this.getTypeOfPropertyOrIndexSignatureOfType(t3, propName), this.unknownType);
            return ((((discriminantType.flags & TypeFlags.Never) == 0) && ((narrowedPropType.flags & TypeFlags.Never) == 0)) && this.areTypesComparable(narrowedPropType, discriminantType));
        });
    }
}

public sealed partial class Checker
{
    private Type narrowTypeByBooleanComparison(FlowState f, Type t, Node expr, Node boolValue, Kind @operator, bool assumeTrue)
    {
        assumeTrue = (((assumeTrue != ((boolValue.Kind == Kind.TrueKeyword)))) != (((@operator != Kind.ExclamationEqualsEqualsToken) && (@operator != Kind.ExclamationEqualsToken))));
        return this.narrowType(f, t, expr, assumeTrue);
    }
}

public sealed partial class Checker
{
    private Type getNarrowedType(Type t, Type candidate, bool assumeTrue, bool checkDerived)
    {
        if (((t.flags & TypeFlags.Union) == 0))
        {
            return this.getNarrowedTypeWorker(t, candidate, assumeTrue, checkDerived);
        }

        var key = new NarrowedTypeKey(t, candidate, assumeTrue, checkDerived);
        {
            var ok = this.narrowedTypes.TryGetValue(key, out var narrowedType);
            if (ok)
            {
                return narrowedType;
            }
        }

        var narrowedType2 = this.getNarrowedTypeWorker(t, candidate, assumeTrue, checkDerived);
        this.narrowedTypes[key] = narrowedType2;
        return narrowedType2;
    }
}

public sealed partial class Checker
{
    private Type getNarrowedTypeWorker(Type t, Type candidate, bool assumeTrue, bool checkDerived)
    {
        if (!assumeTrue)
        {
            if ((t == candidate))
            {
                return this.neverType;
            }

            if (checkDerived)
            {
                return this.filterType(t, (t2) =>
                {
                    return !this.isTypeDerivedFrom(t2, candidate);
                });
            }

            if (((t.flags & TypeFlags.Unknown) != 0))
            {
                t = this.unknownUnionType;
            }

            var trueType = this.getNarrowedType(t, candidate, true, false);
            return this.recombineUnknownType(this.filterType(t, (t3) =>
            {
                return !this.isTypeSubsetOf(t3, trueType);
            }));
        }

        if (((t.flags & TypeFlags.AnyOrUnknown) != 0))
        {
            return candidate;
        }

        if ((t == candidate))
        {
            return candidate;
        }

        string keyPropertyName = "";
        if (((t.flags & TypeFlags.Union) != 0))
        {
            keyPropertyName = this.getKeyPropertyName(t);
        }

        var narrowedType = this.mapType(candidate, (n) =>
        {
            var matching = t;
            if ((keyPropertyName != ""))
            {
                {
                    var discriminant = this.getTypeOfPropertyOfType(n, keyPropertyName);
                    if ((discriminant != null))
                    {
                        {
                            var constituent = this.getConstituentTypeForKeyType(t, discriminant);
                            if ((constituent != null))
                            {
                                matching = constituent;
                            }
                        }
                    }
                }
            }

            Func<Type, Type> mapType = default;
            if (checkDerived)
            {
                mapType = (t4) =>
                {
                    switch (true)
                    {
                        case true when this.isTypeDerivedFrom(t4, n):
                            {
                                return t4;
                            }

                        case true when this.isTypeDerivedFrom(n, t4):
                            {
                                return n;
                            }
                    }

                    return this.neverType;
                };
            }
            else
            {
                mapType = (t5) =>
                {
                    switch (true)
                    {
                        case true when this.isTypeStrictSubtypeOf(t5, n):
                            {
                                return t5;
                            }

                        case true when this.isTypeStrictSubtypeOf(n, t5):
                            {
                                return n;
                            }

                        case true when this.isTypeSubtypeOf(t5, n):
                            {
                                return t5;
                            }

                        case true when this.isTypeSubtypeOf(n, t5):
                            {
                                return n;
                            }
                    }

                    return this.neverType;
                };
            }

            var directlyRelated = this.mapType(matching, mapType);
            if (((directlyRelated.flags & TypeFlags.Never) == 0))
            {
                return directlyRelated;
            }

            Func<Type, Type, bool> isRelated = default;
            if (checkDerived)
            {
                isRelated = this.isTypeDerivedFrom;
            }
            else
            {
                isRelated = this.isTypeSubtypeOf;
            }

            return this.mapType(t, (t6) =>
            {
                if (this.maybeTypeOfKind(t6, TypeFlags.Instantiable))
                {
                    var constraint = this.getBaseConstraintOfType(t6);
                    if (((constraint == null) || isRelated(n, constraint)))
                    {
                        return this.getIntersectionType(new List<Type> { t6, n });
                    }
                }

                return this.neverType;
            });
        });
        switch (true)
        {
            case true when ((narrowedType.flags & TypeFlags.Never) == 0):
                {
                    return narrowedType;
                }

            case true when this.isTypeSubtypeOf(candidate, t):
                {
                    return candidate;
                }

            case true when this.isTypeAssignableTo(t, candidate):
                {
                    return t;
                }

            case true when this.isTypeAssignableTo(candidate, t):
                {
                    return candidate;
                }
        }

        return this.getIntersectionType(new List<Type> { t, candidate });
    }
}

public sealed partial class Checker
{
    private FlowType getTypeAtFlowBranchLabel(FlowState f, FlowNode flow, FlowList antecedents)
    {
        var antecedentStart = GoRuntime.Len(antecedentTypes);
        bool subtypeReduction = false, seenIncomplete = false;
        for (var list = antecedents; list != null; list = list.Next)
        {
            var antecedent = list.Flow;
            if ((antecedent.Flags & FlowFlags.SwitchClause) != 0)
                throw unsupported("getTypeAtFlowBranchLabel: excluded switch bypass");
            var flowType = getTypeAtFlowNode(f, antecedent);
            if (flowType.t == f.declaredType && f.declaredType == f.initialType)
            {
                antecedentTypes = GoRuntime.Slice(antecedentTypes, 0, antecedentStart);
                return new FlowType
                {
                    t = flowType.t
                };
            }

            if (!(GoRuntime.Slice(antecedentTypes, antecedentStart)?.Contains(flowType.t) ?? false))
                antecedentTypes = GoRuntime.Append(antecedentTypes, flowType.t);
            if (!isTypeSubsetOf(flowType.t, f.initialType))
                subtypeReduction = true;
            if (flowType.incomplete)
                seenIncomplete = true;
        }

        var result = newFlowType(getUnionOrEvolvingArrayType(f, GoRuntime.Slice(antecedentTypes, antecedentStart), subtypeReduction ? UnionReduction.Subtype : UnionReduction.Literal), seenIncomplete);
        antecedentTypes = GoRuntime.Slice(antecedentTypes, 0, antecedentStart);
        return result;
    }
}

public sealed partial class Checker
{
    private Node getDiscriminantPropertyAccess(FlowState f, Node expr, Type computedType)
    {
        if ((((f.declaredType.flags & TypeFlags.Union) != 0) || ((computedType.flags & TypeFlags.Union) != 0)))
        {
            var access = this.getCandidateDiscriminantPropertyAccess(f, expr);
            if ((access != null))
            {
                {
                    var tuple1 = this.getAccessedPropertyName(access);
                    var name = tuple1.Item1;
                    var ok = tuple1.Item2;
                    if (ok)
                    {
                        var t = computedType;
                        if ((((f.declaredType.flags & TypeFlags.Union) != 0) && this.isTypeSubsetOf(computedType, f.declaredType)))
                        {
                            t = f.declaredType;
                        }

                        if (this.isDiscriminantProperty(t, name))
                        {
                            return access;
                        }
                    }
                }
            }
        }

        return null;
    }
}

public sealed partial class Checker
{
    private Node getCandidateDiscriminantPropertyAccess(FlowState f, Node expr)
    {
        switch (true)
        {
            case true when ((Ast.IsBindingPattern(f.reference) || Ast.IsFunctionExpressionOrArrowFunction(f.reference)) || Ast.IsObjectLiteralMethod(f.reference)):
                {
                    if (Ast.IsIdentifier(expr))
                    {
                        var symbol = this.getResolvedSymbol(expr);
                        var declaration = this.getExportSymbolOfValueSymbolIfExported(symbol).ValueDeclaration;
                        if ((((((declaration != null) && ((Ast.IsBindingElement(declaration) || Ast.IsParameterDeclaration(declaration)))) && (f.reference == declaration.Parent)) && (declaration.Initializer() == null)) && !hasDotDotDotToken(declaration)))
                        {
                            return declaration;
                        }
                    }

                    break;
                }

            case true when Ast.IsAccessExpression(expr):
                {
                    if (this.isMatchingReference(f.reference, expr.Expression()))
                    {
                        return expr;
                    }

                    break;
                }

            case true when Ast.IsIdentifier(expr):
                {
                    var symbol2 = this.getResolvedSymbol(expr);
                    if (this.isConstantVariable(symbol2))
                    {
                        var declaration2 = symbol2.ValueDeclaration;
                        var initializer = getCandidateVariableDeclarationInitializer(declaration2);
                        if ((((initializer != null) && Ast.IsAccessExpression(initializer)) && this.isMatchingReference(f.reference, initializer.Expression())))
                        {
                            return initializer;
                        }

                        if ((Ast.IsBindingElement(declaration2) && (declaration2.Initializer() == null)))
                        {
                            initializer = getCandidateVariableDeclarationInitializer(declaration2.Parent.Parent);
                            if ((((initializer != null) && ((Ast.IsIdentifier(initializer) || Ast.IsAccessExpression(initializer)))) && this.isMatchingReference(f.reference, initializer)))
                            {
                                return declaration2;
                            }
                        }
                    }

                    break;
                }
        }

        return null;
    }
}

public sealed partial class Checker
{
    private static Node getCandidateVariableDeclarationInitializer(Node node)
    {
        if ((Ast.IsVariableDeclaration(node) && (node.Type() == null)))
        {
            {
                var initializer = node.Initializer();
                if ((initializer != null))
                {
                    return Ast.SkipParentheses(initializer);
                }
            }
        }

        return null;
    }
}

public sealed partial class Checker
{
    private void reportFlowControlError(Node node)
    {
        var block = Ast.FindAncestor(node, Ast.IsFunctionOrModuleBlock);
        var sourceFile = Ast.GetSourceFileOfNode(node);
        var span = TypeScript.Scanner.Scanner.GetRangeOfTokenAtPosition(sourceFile, block.StatementList().Pos());
        this.addDiagnostic(Ast.NewDiagnostic(sourceFile, span, Diagnostics.The_containing_function_or_module_body_is_too_large_for_control_flow_analysis));
    }
}

public sealed partial class Checker
{
    private (string, bool) getAccessedPropertyName(Node access)
    {
        if (Ast.IsPropertyAccessExpression(access))
        {
            return (access.Name().Text(), true);
        }

        if (Ast.IsElementAccessExpression(access))
        {
            return this.tryGetElementAccessExpressionName(access.AsElementAccessExpression());
        }

        if (Ast.IsBindingElement(access))
        {
            throw unsupported("getAccessedPropertyName: excluded binding element");
        }

        if (Ast.IsParameterDeclaration(access))
        {
            return (Strconv.Itoa(GoSlices.Index(access.Parent.Parameters(), access)), true);
        }

        return ("", false);
    }
}

public sealed partial class Checker
{
    private bool isConstantReference(Node node)
    {
        switch (node.Kind)
        {
            case Kind.ThisKeyword:
                {
                    return true;
                }

            case Kind.Identifier:
                {
                    if (!Ast.IsThisInTypeQuery(node))
                    {
                        var symbol = this.getResolvedSymbol(node);
                        return ((this.isConstantVariable(symbol) || (this.isParameterOrMutableLocalVariable(symbol) && !this.isSymbolAssigned(symbol))) || ((symbol.ValueDeclaration != null) && Ast.IsFunctionExpression(symbol.ValueDeclaration)));
                    }

                    break;
                }

            case Kind.PropertyAccessExpression:
            case Kind.ElementAccessExpression:
                {
                    if (this.isConstantReference(node.Expression()))
                    {
                        var symbol2 = this.getResolvedSymbolOrNil(node);
                        if ((symbol2 != null))
                        {
                            return this.isReadonlySymbol(symbol2);
                        }
                    }

                    break;
                }

            case Kind.ObjectBindingPattern:
            case Kind.ArrayBindingPattern:
                {
                    throw unsupported("isConstantReference: excluded binding pattern");
                }
        }

        return false;
    }
}

public sealed partial class Checker
{
    private bool optionalChainContainsReference(Node source, Node target)
    {
        while (Ast.IsOptionalChain(source))
        {
            source = source.Expression();
            if (this.isMatchingReference(source, target))
            {
                return true;
            }
        }

        return false;
    }
}

public sealed partial class Checker
{
    private Node getReferenceCandidate(Node node)
    {
        switch (node.Kind)
        {
            case Kind.ParenthesizedExpression:
                {
                    return this.getReferenceCandidate(node.Expression());
                }

            case Kind.BinaryExpression:
                {
                    switch (node.AsBinaryExpression().OperatorToken.Kind)
                    {
                        case Kind.EqualsToken:
                        case Kind.BarBarEqualsToken:
                        case Kind.AmpersandAmpersandEqualsToken:
                        case Kind.QuestionQuestionEqualsToken:
                            {
                                return this.getReferenceCandidate(node.AsBinaryExpression().Left);
                            }

                        case Kind.CommaToken:
                            {
                                return this.getReferenceCandidate(node.AsBinaryExpression().Right);
                            }
                    }

                    break;
                }
        }

        return node;
    }
}

public sealed partial class Checker
{
    private Node getReferenceRoot(Node node)
    {
        var parent = node.Parent;
        if (((Ast.IsParenthesizedExpression(parent) || ((Ast.IsBinaryExpression(parent) && (parent.AsBinaryExpression().OperatorToken.Kind == Kind.EqualsToken)) && (parent.AsBinaryExpression().Left == node))) || ((Ast.IsBinaryExpression(parent) && (parent.AsBinaryExpression().OperatorToken.Kind == Kind.CommaToken)) && (parent.AsBinaryExpression().Right == node))))
        {
            return this.getReferenceRoot(parent);
        }

        return node;
    }
}

public sealed partial class Checker
{
    private bool hasMatchingArgument(Node expression, Node reference)
    {
        {
            foreach (var argument in GoRuntime.RangeValues(expression.Arguments()))
            {
                if ((this.isOrContainsMatchingReference(reference, argument) || this.optionalChainContainsReference(argument, reference)))
                {
                    return true;
                }
            }
        }

        if ((Ast.IsPropertyAccessExpression(expression.Expression()) && this.isOrContainsMatchingReference(reference, expression.Expression().Expression())))
        {
            return true;
        }

        return false;
    }
}

public sealed partial class Checker
{
    private bool isOrContainsMatchingReference(Node source, Node target)
    {
        return (this.isMatchingReference(source, target) || this.containsMatchingReference(source, target));
    }
}

public sealed partial class Checker
{
    private Type replacePrimitivesWithLiterals(Type typeWithPrimitives, Type typeWithLiterals)
    {
        if ((this.maybeTypeOfKind(typeWithPrimitives, (((TypeFlags.String | TypeFlags.TemplateLiteral) | TypeFlags.Number) | TypeFlags.BigInt)) && this.maybeTypeOfKind(typeWithLiterals, ((((TypeFlags.StringLiteral | TypeFlags.TemplateLiteral) | TypeFlags.StringMapping) | TypeFlags.NumberLiteral) | TypeFlags.BigIntLiteral))))
        {
            return this.mapType(typeWithPrimitives, (t) =>
            {
                switch (true)
                {
                    case true when ((t.flags & TypeFlags.String) != 0):
                        {
                            return this.extractTypesOfKind(typeWithLiterals, (((TypeFlags.String | TypeFlags.StringLiteral) | TypeFlags.TemplateLiteral) | TypeFlags.StringMapping));
                        }

                    case true when (this.isPatternLiteralType(t) && !this.maybeTypeOfKind(typeWithLiterals, ((TypeFlags.String | TypeFlags.TemplateLiteral) | TypeFlags.StringMapping))):
                        {
                            return this.extractTypesOfKind(typeWithLiterals, TypeFlags.StringLiteral);
                        }

                    case true when ((t.flags & TypeFlags.Number) != 0):
                        {
                            return this.extractTypesOfKind(typeWithLiterals, (TypeFlags.Number | TypeFlags.NumberLiteral));
                        }

                    case true when ((t.flags & TypeFlags.BigInt) != 0):
                        {
                            return this.extractTypesOfKind(typeWithLiterals, (TypeFlags.BigInt | TypeFlags.BigIntLiteral));
                        }

                    default:
                        {
                            return t;
                        }
                }
            });
        }

        return typeWithPrimitives;
    }
}

public sealed partial class Checker
{
    private static bool isCoercibleUnderDoubleEquals(Type source, Type target)
    {
        return (((source.flags & (((TypeFlags.Number | TypeFlags.String) | TypeFlags.BooleanLiteral))) != 0) && ((target.flags & (((TypeFlags.Number | TypeFlags.String) | TypeFlags.Boolean))) != 0));
    }
}

public sealed partial class Checker
{
    private Signature getEffectsSignature(Node node)
    {
        var links = this.signatureLinks.Get(node);
        var signature = links.effectsSignature;
        if ((signature == null))
        {
            Type funcType = default;
            if (Ast.IsBinaryExpression(node))
            {
                throw unsupported("getEffectsSignature: excluded instanceof effects");
            }
            else
            {
                if (Ast.IsExpressionStatement(node.Parent))
                {
                    funcType = this.getTypeOfDottedName(node.Expression(), null);
                }
                else
                {
                    if ((node.Expression().Kind != Kind.SuperKeyword))
                    {
                        if (Ast.IsOptionalChain(node))
                        {
                            throw unsupported("getEffectsSignature: excluded optional call");
                        }
                        else
                        {
                            funcType = this.checkNonNullExpression(node.Expression());
                        }
                    }
                }
            }

            Type apparentType = default;
            if ((funcType != null))
            {
                apparentType = this.getApparentType(funcType);
            }

            var signatures = this.getSignaturesOfType(Core.OrElse(apparentType, this.unknownType), SignatureKind.Call);
            switch (true)
            {
                case true when ((GoRuntime.Len(signatures) == 1) && (GoRuntime.Len(signatures[0].typeParameters) == 0)):
                    {
                        signature = signatures[0];
                        break;
                    }

                case true when Core.Some(signatures, this.hasTypePredicateOrNeverReturnType):
                    {
                        signature = this.getResolvedSignature(node, null, CheckMode.Normal);
                        break;
                    }
            }

            if (!(((signature != null) && this.hasTypePredicateOrNeverReturnType(signature))))
            {
                signature = this.unknownSignature;
            }

            links.effectsSignature = signature;
        }

        if ((signature == this.unknownSignature))
        {
            return null;
        }

        return signature;
    }
}

public sealed partial class Checker
{
    private Type getTypeOfDottedName(Node node, Diagnostic diagnostic)
    {
        if (((node.Flags & NodeFlags.InWithStatement) == 0))
        {
            switch (node.Kind)
            {
                case Kind.Identifier:
                    {
                        var symbol = this.getExportSymbolOfValueSymbolIfExported(this.getResolvedSymbol(node));
                        return this.getExplicitTypeOfSymbol(symbol, diagnostic);
                    }

                case Kind.ThisKeyword:
                    {
                        return this.getExplicitThisType(node);
                    }

                case Kind.SuperKeyword:
                    {
                        return this.checkSuperExpression(node);
                    }

                case Kind.PropertyAccessExpression:
                    {
                        var t = this.getTypeOfDottedName(node.Expression(), diagnostic);
                        if ((t != null))
                        {
                            var name = node.Name();
                            Symbol prop = default;
                            if (Ast.IsPrivateIdentifier(name))
                            {
                                if ((t.symbol != null))
                                {
                                    prop = this.getPropertyOfType(t, Binder.GetSymbolNameForPrivateIdentifier(t.symbol, name.Text()));
                                }
                            }
                            else
                            {
                                prop = this.getPropertyOfType(t, name.Text());
                            }

                            if ((prop != null))
                            {
                                return this.getExplicitTypeOfSymbol(prop, diagnostic);
                            }
                        }

                        break;
                    }

                case Kind.ParenthesizedExpression:
                    {
                        return this.getTypeOfDottedName(node.Expression(), diagnostic);
                    }
            }
        }

        return null;
    }
}

public sealed partial class Checker
{
    private bool isDeclarationWithExplicitTypeAnnotation(Node node)
    {
        return ((((((Ast.IsVariableDeclaration(node) || Ast.IsPropertyDeclaration(node)) || Ast.IsPropertySignatureDeclaration(node)) || Ast.IsParameterDeclaration(node))) && (node.Type() != null)) || this.isExpandoPropertyFunctionWithReturnTypeAnnotation(node));
    }
}

public sealed partial class Checker
{
    private bool isExpandoPropertyFunctionWithReturnTypeAnnotation(Node node)
    {
        if (Ast.IsBinaryExpression(node))
        {
            {
                var expr = node.AsBinaryExpression().Right;
                if ((Ast.IsFunctionLike(expr) && (expr.Type() != null)))
                {
                    return true;
                }
            }
        }

        return false;
    }
}

public sealed partial class Checker
{
    private bool hasTypePredicateOrNeverReturnType(Signature sig)
    {
        return ((this.getTypePredicateOfSignature(sig) != null) || ((sig.declaration != null) && ((Core.OrElse(this.getReturnTypeFromAnnotation(sig.declaration), this.unknownType).flags & TypeFlags.Never) != 0)));
    }
}

public sealed partial class Checker
{
    private Type getExplicitThisType(Node node)
    {
        var container = Ast.GetThisContainer(node, false, false);
        if (Ast.IsFunctionLike(container))
        {
            var signature = this.getSignatureFromDeclaration(container);
            if ((signature.thisParameter != null))
            {
                return this.getExplicitTypeOfSymbol(signature.thisParameter, null);
            }
        }

        if (((container.Parent != null) && Ast.IsClassLike(container.Parent)))
        {
            var symbol = this.getSymbolOfDeclaration(container.Parent);
            if (Ast.IsStatic(container))
            {
                return this.getTypeOfSymbol(symbol);
            }
            else
            {
                return this.getDeclaredTypeOfSymbol(symbol).AsInterfaceType().thisType;
            }
        }

        return null;
    }
}

public sealed partial class Checker
{
    private Type getTypeWithDefault(Type t, Node defaultExpression)
    {
        if ((defaultExpression != null))
        {
            return this.getUnionType(new List<Type> { this.getNonUndefinedType(t), this.getTypeOfExpression(defaultExpression) });
        }

        return t;
    }
}

public sealed partial class Checker
{
    private Node getTypePredicateArgument(TypePredicate predicate, Node callExpression)
    {
        if (((predicate.kind == TypePredicateKind.Identifier) || (predicate.kind == TypePredicateKind.AssertsIdentifier)))
        {
            var arguments = callExpression.Arguments();
            if (((predicate.parameterIndex >= 0) && ((int)(predicate.parameterIndex) < GoRuntime.Len(arguments))))
            {
                return arguments[predicate.parameterIndex];
            }
        }
        else
        {
            var invokedExpression = Ast.SkipParentheses(callExpression.Expression());
            if (Ast.IsAccessExpression(invokedExpression))
            {
                return Ast.SkipParentheses(invokedExpression.Expression());
            }
        }

        return null;
    }
}

public sealed partial class Checker
{
    private bool isFalseExpression(Node expr)
    {
        var node = Ast.SkipParentheses(expr);
        if ((node.Kind == Kind.FalseKeyword))
        {
            return true;
        }

        if (Ast.IsBinaryExpression(node))
        {
            var binary = node.AsBinaryExpression();
            return (((binary.OperatorToken.Kind == Kind.AmpersandAmpersandToken) && ((this.isFalseExpression(binary.Left) || this.isFalseExpression(binary.Right)))) || (((binary.OperatorToken.Kind == Kind.BarBarToken) && this.isFalseExpression(binary.Left)) && this.isFalseExpression(binary.Right)));
        }

        return false;
    }
}

public sealed partial class Checker
{
    private bool isSymbolAssigned(Symbol symbol)
    {
        this.ensureAssignmentsMarked(symbol);
        return (this.markedAssignmentSymbolLinks.Get(symbol).lastAssignmentPos != 0);
    }
}

public sealed partial class Checker
{
    private bool isPastLastAssignment(Symbol symbol, Node location)
    {
        this.ensureAssignmentsMarked(symbol);
        var lastAssignmentPos = this.markedAssignmentSymbolLinks.Get(symbol).lastAssignmentPos;
        return ((lastAssignmentPos == 0) || ((location != null) && ((int)(lastAssignmentPos) < location.Pos())));
    }
}

public sealed partial class Checker
{
    private bool hasParentWithAssignmentsMarked(Node node)
    {
        return (Ast.FindAncestor(node.Parent, (node2) =>
        {
            return (Ast.IsFunctionOrSourceFile(node2) && ((this.nodeLinks.Get(node2).flags & NodeCheckFlags.AssignmentsMarked) != 0));
        }) != null);
    }
}

public sealed partial class Checker
{
    private int extendAssignmentPosition(Node node, Node declaration)
    {
        var pos = node.Pos();
        while (((node != null) && (node.Pos() > declaration.Pos())))
        {
            switch (node.Kind)
            {
                case Kind.VariableStatement:
                case Kind.ExpressionStatement:
                case Kind.IfStatement:
                case Kind.DoStatement:
                case Kind.WhileStatement:
                case Kind.ForStatement:
                case Kind.ForInStatement:
                case Kind.ForOfStatement:
                case Kind.WithStatement:
                case Kind.SwitchStatement:
                case Kind.TryStatement:
                case Kind.ClassDeclaration:
                    {
                        pos = node.End();
                        break;
                    }
            }

            node = node.Parent;
        }

        return pos;
    }
}

public sealed partial class Checker
{
    private FlowState getFlowState()
    {
        var f = freeFlowState ?? new FlowState();
        freeFlowState = f.next;
        return f;
    }
}

public sealed partial class Checker
{
    private void putFlowState(FlowState f)
    {
        var labels = f.reduceLabels;
        if (labels != null)
            labels.Clear();
        f.reference = f.flowContainer = null;
        f.declaredType = f.initialType = null;
        f.depth = f.sharedFlowStart = 0;
        f.refKey = default;
        f.reduceLabels = labels;
        f.next = freeFlowState;
        freeFlowState = f;
    }
}

public sealed partial class Checker
{
    private bool isEvolvingArrayOperationTarget(Node node)
    {
        var root = this.getReferenceRoot(node);
        var parent = root.Parent;
        var isLengthPushOrUnshift = (Ast.IsPropertyAccessExpression(parent) && (((parent.Name().Text() == "length") || ((Ast.IsCallExpression(parent.Parent) && Ast.IsIdentifier(parent.Name())) && Ast.IsPushOrUnshiftIdentifier(parent.Name())))));
        var isElementAssignment = ((((((Ast.IsElementAccessExpression(parent) && (parent.Expression() == root)) && Ast.IsBinaryExpression(parent.Parent)) && (parent.Parent.AsBinaryExpression().OperatorToken.Kind == Kind.EqualsToken)) && (parent.Parent.AsBinaryExpression().Left == parent)) && !Ast.IsAssignmentTarget(parent.Parent)) && this.isTypeAssignableToKind(this.getTypeOfExpression(parent.AsElementAccessExpression().ArgumentExpression), TypeFlags.NumberLike));
        return (isLengthPushOrUnshift || isElementAssignment);
    }

    private Type finalizeEvolvingArrayType(Type t)
    {
        if (((t.objectFlags & ObjectFlags.EvolvingArray) != 0))
        {
            return this.getFinalArrayType(t.AsEvolvingArrayType());
        }

        return t;
    }
}

public sealed partial class Checker
{
    private Type getUnionOrEvolvingArrayType(FlowState f, List<Type> types, UnionReduction subtypeReduction)
    {
        if (isEvolvingArrayTypeList(types))
        {
            return this.getEvolvingArrayType(this.getUnionType(Core.Map(types, this.getElementTypeOfEvolvingArrayType)));
        }

        var result = this.recombineUnknownType(this.getUnionTypeEx(Core.SameMap(types, this.finalizeEvolvingArrayType), subtypeReduction, null, null));
        if ((((result != f.declaredType) && (((result.flags & f.declaredType.flags) & TypeFlags.Union) != 0)) && GoSlices.Equal(result.AsUnionType().types, f.declaredType.AsUnionType().types)))
        {
            return f.declaredType;
        }

        return result;
    }
}

public sealed partial class Checker
{
    private Type getExplicitTypeOfSymbol(Symbol symbol, Diagnostic diagnostic)
    {
        symbol = resolveSymbol(symbol);
        if (!resolvingExplicitTypeOfSymbol.Add(symbol))
            return null;
        try
        {
            if ((symbol.Flags & (SymbolFlags.Function | SymbolFlags.Method | SymbolFlags.Class | SymbolFlags.ValueModule)) != 0)
                return getTypeOfSymbol(symbol);
            if ((symbol.Flags & (SymbolFlags.Variable | SymbolFlags.Property)) != 0)
            {
                if ((symbol.CheckFlags & CheckFlags.Mapped) != 0)
                {
                    var origin = mappedSymbolLinks.Get(symbol).syntheticOrigin;
                    if (origin != null && getExplicitTypeOfSymbol(origin, diagnostic) != null)
                        return getTypeOfSymbol(symbol);
                }

                var declaration = symbol.ValueDeclaration;
                if (declaration != null)
                {
                    if (isDeclarationWithExplicitTypeAnnotation(declaration))
                        return getTypeOfSymbol(symbol);
                    if (Ast.IsVariableDeclaration(declaration) && Ast.IsForOfStatement(declaration.Parent.Parent))
                    {
                        var statement = declaration.Parent.Parent;
                        var expressionType = getTypeOfDottedName(statement.Expression(), null);
                        if (expressionType != null)
                        {
                            if (statement.AsForInOrOfStatement().AwaitModifier != null)
                                throw unsupported("getExplicitTypeOfSymbol: excluded for-await-of");
                            return checkIteratedTypeOrElementType(IterationUse.ForOf, expressionType, undefinedType, null);
                        }
                    }

                    if (diagnostic != null)
                        diagnostic.AddRelatedInfo(createDiagnosticForNode(declaration, Diagnostics.X_0_needs_an_explicit_type_annotation, symbolToString(symbol)));
                }
            }

            return null;
        }
        finally
        {
            resolvingExplicitTypeOfSymbol.Remove(symbol);
        }
    }
}

public sealed partial class Checker
{
    private Type getAssignedType(Node node)
    {
        var parent = node.Parent;
        if (parent.Kind == Kind.BinaryExpression)
            return getAssignedTypeOfBinaryExpression(parent);
        if (parent.Kind is Kind.DeleteExpression or Kind.ArrayLiteralExpression or Kind.SpreadElement or Kind.PropertyAssignment or Kind.ShorthandPropertyAssignment)
            throw unsupported("getAssignedType: excluded delete/destructuring");
        return errorType;
    }
}

public sealed partial class Checker
{
    private Type getAssignedTypeOfBinaryExpression(Node node)
    {
        if (Ast.IsArrayLiteralExpression(node.Parent) || Ast.IsPropertyAssignment(node.Parent))
            throw unsupported("getAssignedTypeOfBinaryExpression: excluded destructuring");
        return getTypeOfExpression(node.AsBinaryExpression().Right);
    }
}

public sealed partial class Checker
{
    private bool isMatchingConstructorReference(FlowState f, Node expr)
    {
        Node name = default;
        if (Ast.IsPropertyAccessExpression(expr))
        {
            name = expr.AsPropertyAccessExpression().Name();
        }
        else
        {
            if ((Ast.IsElementAccessExpression(expr) && Ast.IsStringLiteralLike(expr.AsElementAccessExpression().ArgumentExpression)))
            {
                name = expr.AsElementAccessExpression().ArgumentExpression;
            }
        }

        return (((name != null) && (name.Text() == "constructor")) && this.isMatchingReference(f.reference, expr.Expression()));
    }
}

public sealed partial class Checker
{
    private Type narrowTypeByConstructor(Type t, Kind @operator, Node identifier, bool assumeTrue)
    {
        if ((((assumeTrue && (@operator != Kind.EqualsEqualsToken)) && (@operator != Kind.EqualsEqualsEqualsToken)) || ((!assumeTrue && (@operator != Kind.ExclamationEqualsToken)) && (@operator != Kind.ExclamationEqualsEqualsToken))))
        {
            return t;
        }

        var identifierType = this.getTypeOfExpression(identifier);
        if ((!this.isFunctionType(identifierType) && !this.isConstructorType(identifierType)))
        {
            return t;
        }

        var prototypeProperty = this.getPropertyOfType(identifierType, "prototype");
        if ((prototypeProperty == null))
        {
            return t;
        }

        var prototypeType = this.getTypeOfSymbol(prototypeProperty);
        Type candidate = default;
        if (!IsTypeAny(prototypeType))
        {
            candidate = prototypeType;
        }

        if ((((candidate == null) || (candidate == this.globalObjectType)) || (candidate == this.globalFunctionType)))
        {
            return t;
        }

        if (IsTypeAny(t))
        {
            return candidate;
        }

        return this.filterType(t, (t2) =>
        {
            return this.isConstructedBy(t2, candidate);
        });
    }
}

public sealed partial class Checker
{
    private bool isConstructedBy(Type source, Type target)
    {
        if (((((source.flags & TypeFlags.Object) != 0) && ((source.objectFlags & ObjectFlags.Class) != 0)) || (((target.flags & TypeFlags.Object) != 0) && ((target.objectFlags & ObjectFlags.Class) != 0))))
        {
            return (source.symbol == target.symbol);
        }

        return this.isTypeSubtypeOf(source, target);
    }
}

public sealed partial class Checker
{
    private FlowType getTypeAtFlowLoopLabel(FlowState f, FlowNode flow)
    {
        if (f.refKey.preimage is null)
        {
            f.refKey = this.getFlowReferenceKey(f);
        }

        if ((f.refKey == nonDottedNameCacheKey))
        {
            return new FlowType
            {
                t = f.declaredType
            };
        }

        var key = new FlowLoopKey
        {
            flowNode = flow,
            refKey = f.refKey
        };
        {
            var cached = this.flowLoopCache.GetValueOrDefault(key);
            if ((cached != null))
            {
                return new FlowType
                {
                    t = cached
                };
            }
        }

        {
            foreach (var loopInfo in GoRuntime.RangeValues(this.flowLoopStack))
            {
                if (((loopInfo.key == key) && (GoRuntime.Len(loopInfo.types) != 0)))
                {
                    return this.newFlowType(this.getUnionOrEvolvingArrayType(f, loopInfo.types, UnionReduction.Literal), true);
                }
            }
        }

        var antecedentTypes = GoRuntime.MakeSlice<Type>(0);
        var subtypeReduction = false;
        FlowType firstAntecedentType = default;
        {
            var list = flow.Antecedents;
            for (; (list != null); list = list.Next)
            {
                FlowType flowType = default;
                if (firstAntecedentType.isNil())
                {
                    firstAntecedentType = this.getTypeAtFlowNode(f, list.Flow);
                    flowType = firstAntecedentType;
                }
                else
                {
                    this.flowLoopStack = GoRuntime.Append(this.flowLoopStack, new FlowLoopInfo(key, new List<Type>(antecedentTypes)));
                    var saveFlowTypeCache = this.flowTypeCache;
                    this.flowTypeCache = null;
                    flowType = this.getTypeAtFlowNode(f, list.Flow);
                    this.flowTypeCache = saveFlowTypeCache;
                    this.flowLoopStack = GoRuntime.Slice(this.flowLoopStack, 0, (GoRuntime.Len(this.flowLoopStack) - 1));
                    {
                        var cached2 = this.flowLoopCache.GetValueOrDefault(key);
                        if ((cached2 != null))
                        {
                            return new FlowType
                            {
                                t = cached2
                            };
                        }
                    }
                }

                antecedentTypes = Core.AppendIfUnique(antecedentTypes, flowType.t);
                if (!this.isTypeSubsetOf(flowType.t, f.initialType))
                {
                    subtypeReduction = true;
                }

                if ((flowType.t == f.declaredType))
                {
                    break;
                }
            }
        }

        var result = this.getUnionOrEvolvingArrayType(f, antecedentTypes, Core.IfElse(subtypeReduction, UnionReduction.Subtype, UnionReduction.Literal));
        if (firstAntecedentType.incomplete)
        {
            return this.newFlowType(result, true);
        }

        this.flowLoopCache[key] = result;
        return new FlowType
        {
            t = result
        };
    }
}

public sealed partial class Checker
{
    private FlowType getTypeAtFlowArrayMutation(FlowState f, FlowNode flow)
    {
        if (((f.declaredType == this.autoType) || (f.declaredType == this.autoArrayType)))
        {
            var node = flow.Node;
            Node expr = default;
            if (Ast.IsCallExpression(node))
            {
                expr = node.Expression().Expression();
            }
            else
            {
                expr = node.AsBinaryExpression().Left.Expression();
            }

            if (this.isMatchingReference(f.reference, this.getReferenceCandidate(expr)))
            {
                var flowType = this.getTypeAtFlowNode(f, flow.Antecedent);
                if (((flowType.t.objectFlags & ObjectFlags.EvolvingArray) != 0))
                {
                    var evolvedType = flowType.t;
                    if (Ast.IsCallExpression(node))
                    {
                        {
                            foreach (var arg in GoRuntime.RangeValues(node.Arguments()))
                            {
                                evolvedType = this.addEvolvingArrayElementType(evolvedType, arg);
                            }
                        }
                    }
                    else
                    {
                        var indexType = this.getContextFreeTypeOfExpression(node.AsBinaryExpression().Left.AsElementAccessExpression().ArgumentExpression);
                        if (this.isTypeAssignableToKind(indexType, TypeFlags.NumberLike))
                        {
                            evolvedType = this.addEvolvingArrayElementType(evolvedType, node.AsBinaryExpression().Right);
                        }
                    }

                    return this.newFlowType(evolvedType, flowType.incomplete);
                }

                return flowType;
            }
        }

        return new FlowType
        {
        };
    }
}

public sealed partial class Checker
{
    private Type getEvolvingArrayType(Type elementType)
    {
        var key = new CachedTypeKey(CachedTypeKind.EvolvingArrayType, elementType.id);
        var result = this.cachedTypes.GetValueOrDefault(key);
        if ((result == null))
        {
            result = this.newObjectType(ObjectFlags.EvolvingArray, null);
            result.AsEvolvingArrayType().elementType = elementType;
            this.cachedTypes[key] = result;
        }

        return result;
    }
}

public sealed partial class Checker
{
    private Type getElementTypeOfEvolvingArrayType(Type t)
    {
        if (((t.objectFlags & ObjectFlags.EvolvingArray) != 0))
        {
            return t.AsEvolvingArrayType().elementType;
        }

        return this.neverType;
    }
}

public sealed partial class Checker
{
    private static bool isEvolvingArrayTypeList(List<Type> types)
    {
        var hasEvolvingArrayType = false;
        {
            foreach (var t in GoRuntime.RangeValues(types))
            {
                if (((t.flags & TypeFlags.Never) == 0))
                {
                    if (((t.objectFlags & ObjectFlags.EvolvingArray) == 0))
                    {
                        return false;
                    }

                    hasEvolvingArrayType = true;
                }
            }
        }

        return hasEvolvingArrayType;
    }
}

public sealed partial class Checker
{
    private Type addEvolvingArrayElementType(Type evolvingArrayType, Node node)
    {
        var newElementType = this.getRegularTypeOfObjectLiteral(this.getBaseTypeOfLiteralType(this.getContextFreeTypeOfExpression(node)));
        var elementType = evolvingArrayType.AsEvolvingArrayType().elementType;
        if (this.isTypeSubsetOf(newElementType, elementType))
        {
            return evolvingArrayType;
        }

        return this.getEvolvingArrayType(this.getUnionType(new List<Type> { elementType, newElementType }));
    }
}

public sealed partial class Checker
{
    private Type getFinalArrayType(EvolvingArrayType t)
    {
        if ((t.finalArrayType == null))
        {
            t.finalArrayType = this.createFinalArrayType(t.elementType);
        }

        return t.finalArrayType;
    }
}

public sealed partial class Checker
{
    private Type createFinalArrayType(Type elementType)
    {
        switch (true)
        {
            case true when ((elementType.flags & TypeFlags.Never) != 0):
                {
                    return this.autoArrayType;
                }

            case true when ((elementType.flags & TypeFlags.Union) != 0):
                {
                    return this.createArrayType(this.getUnionTypeEx(elementType.Types(), UnionReduction.Subtype, null, null));
                }
        }

        return this.createArrayType(elementType);
    }
}

public sealed partial class Checker
{
    private CacheHashKey getFlowReferenceKey(FlowState f)
    {
        var b = new keyBuilder();
        verificationCollectionsFlowStart(b);
        if (this.writeFlowCacheKey(b, f.reference, f.declaredType, f.initialType, f.flowContainer))
        {
            var key = b.hash();
            verificationCollectionsFlowEnd(b, key, true);
            return key;
        }

        verificationCollectionsFlowEnd(b, nonDottedNameCacheKey, false);
        return nonDottedNameCacheKey;
    }
}

public sealed partial class Checker
{
    private bool writeFlowCacheKey(keyBuilder b, Node node, Type declaredType, Type initialType, Node flowContainer)
    {
        switch (node.Kind)
        {
            case Kind.Identifier:
                {
                    if (!Ast.IsThisInTypeQuery(node))
                    {
                        var symbol = this.getResolvedSymbol(node);
                        if ((symbol == this.unknownSymbol))
                        {
                            return false;
                        }

                        b.writeSymbol(symbol);
                        verificationCollectionsFlowSlot(b, "symbol", symbol, 8);
                    }

                    goto case Kind.ThisKeyword;
                }

            case Kind.ThisKeyword:
                {
                    b.writeByte((byte)':');
                    b.writeType(declaredType);
                    verificationCollectionsFlowSlot(b, "type", declaredType, 4);
                    if ((initialType != declaredType))
                    {
                        b.writeByte((byte)'=');
                        b.writeType(initialType);
                        verificationCollectionsFlowSlot(b, "type", initialType, 4);
                    }

                    if ((flowContainer != null))
                    {
                        b.writeByte((byte)'@');
                        b.writeNode(flowContainer);
                        verificationCollectionsFlowSlot(b, "node", flowContainer, 8);
                    }

                    return true;
                }

            case Kind.NonNullExpression:
            case Kind.ParenthesizedExpression:
                {
                    return this.writeFlowCacheKey(b, node.Expression(), declaredType, initialType, flowContainer);
                }

            case Kind.QualifiedName:
                {
                    if (!this.writeFlowCacheKey(b, node.AsQualifiedName().Left, declaredType, initialType, flowContainer))
                    {
                        return false;
                    }

                    b.writeByte((byte)'.');
                    b.writeString(node.AsQualifiedName().Right.Text());
                    return true;
                }

            case Kind.PropertyAccessExpression:
            case Kind.ElementAccessExpression:
                {
                    {
                        var tuple1 = this.getAccessedPropertyName(node);
                        var propName = tuple1.Item1;
                        var ok = tuple1.Item2;
                        if (ok)
                        {
                            if (!this.writeFlowCacheKey(b, node.Expression(), declaredType, initialType, flowContainer))
                            {
                                return false;
                            }

                            b.writeByte((byte)'.');
                            b.writeString(propName);
                            return true;
                        }
                    }

                    if ((Ast.IsElementAccessExpression(node) && Ast.IsIdentifier(node.AsElementAccessExpression().ArgumentExpression)))
                    {
                        var symbol2 = this.getResolvedSymbol(node.AsElementAccessExpression().ArgumentExpression);
                        if ((this.isConstantVariable(symbol2) || (this.isParameterOrMutableLocalVariable(symbol2) && !this.isSymbolAssigned(symbol2))))
                        {
                            if (!this.writeFlowCacheKey(b, node.Expression(), declaredType, initialType, flowContainer))
                            {
                                return false;
                            }

                            b.writeString(".@");
                            b.writeSymbol(symbol2);
                            verificationCollectionsFlowSlot(b, "symbol", symbol2, 8);
                            return true;
                        }
                    }

                    break;
                }

            case Kind.ObjectBindingPattern:
            case Kind.ArrayBindingPattern:
            case Kind.FunctionDeclaration:
            case Kind.FunctionExpression:
            case Kind.ArrowFunction:
            case Kind.MethodDeclaration:
                {
                    b.writeNode(node);
                    verificationCollectionsFlowSlot(b, "node", node, 8);
                    b.writeByte((byte)'#');
                    b.writeType(declaredType);
                    verificationCollectionsFlowSlot(b, "type", declaredType, 4);
                    return true;
                }
        }

        return false;
    }
}

public sealed partial class Checker
{
    private (string, bool) tryGetElementAccessExpressionName(ElementAccessExpression node)
    {
        switch (true)
        {
            case true when Ast.IsStringOrNumericLiteralLike(node.ArgumentExpression):
                {
                    return (node.ArgumentExpression.Text(), true);
                }

            case true when Ast.IsEntityNameExpression(node.ArgumentExpression):
                {
                    return this.tryGetNameFromEntityNameExpression(node.ArgumentExpression);
                }
        }

        return ("", false);
    }
}

public sealed partial class Checker
{
    private (string, bool) tryGetNameFromEntityNameExpression(Node node)
    {
        var symbol = this.resolveEntityName(node, SymbolFlags.Value, true, false, null);
        if (((symbol == null) || !((this.isConstantVariable(symbol) || (((symbol.Flags & SymbolFlags.EnumMember) != 0))))))
        {
            return ("", false);
        }

        var declaration = symbol.ValueDeclaration;
        if ((declaration == null))
        {
            return ("", false);
        }

        var t = this.tryGetTypeFromTypeNode(declaration);
        if ((t != null))
        {
            {
                var tuple1 = tryGetFlowNameFromType(t);
                var name = tuple1.Item1;
                var ok = tuple1.Item2;
                if (ok)
                {
                    return (name, true);
                }
            }
        }

        if (((hasOnlyExpressionInitializer(declaration) && !Ast.IsBindingElement(declaration)) && this.isBlockScopedNameDeclaredBeforeUse(declaration, node)))
        {
            {
                var initializer = declaration.Initializer();
                if ((initializer != null))
                {
                    {
                        var initializerType = this.getTypeOfExpression(initializer);
                        if ((initializerType != null))
                        {
                            return tryGetFlowNameFromType(initializerType);
                        }
                    }
                }
                else
                {
                    if (Ast.IsEnumMember(declaration))
                    {
                        return Ast.TryGetTextOfPropertyName(declaration.Name());
                    }
                }
            }
        }

        return ("", false);
    }
}

public sealed partial class Checker
{
    private static (string, bool) tryGetFlowNameFromType(Type t)
    {
        switch (true)
        {
            case true when ((t.flags & TypeFlags.UniqueESSymbol) != 0):
                {
                    return (t.AsUniqueESSymbolType().name, true);
                }

            case true when ((t.flags & TypeFlags.StringOrNumberLiteral) != 0):
                {
                    return (Evaluator.AnyToString(t.AsLiteralType().value), true);
                }
        }

        return ("", false);
    }
}

public sealed partial class Checker
{
    private string getPropertyNameForKnownSymbolName(string symbolName)
    {
        var ctorType = this.getGlobalESSymbolConstructorSymbolOrNil();
        if ((ctorType != null))
        {
            var uniqueType = this.getTypeOfPropertyOfType(this.getTypeOfSymbol(ctorType), symbolName);
            if (((uniqueType != null) && isTypeUsableAsPropertyName(uniqueType)))
            {
                return getPropertyNameFromType(uniqueType);
            }
        }

        return ((Ast.InternalSymbolNamePrefix + "@") + symbolName);
    }
}

public sealed partial class Checker
{
    // flow.go:2339. The fixed profile currently leaves the option false, but the
    // original helper retains the genuine missing-type edge when enabled.
    private Type includeUndefinedInIndexSignature(Type t)
    {
        if (t == null)
            return null;
        if (compilerOptions.NoUncheckedIndexedAccess == Tristate.True)
            return getUnionType(new List<Type> { t, missingType });
        return t;
    }
}

public sealed partial class Checker
{
    private readonly Dictionary<FlowNode, bool> flowNodePostSuper = new(ReferenceEqualityComparer.Instance);
}

public sealed partial class Checker
{
    private Type getFlowTypeInConstructor(Symbol symbol, Node constructor)
    {
        Node accessName = default;
        if (GoStrings.HasPrefix(symbol.Name, (Ast.InternalSymbolNamePrefix + "#")))
        {
            accessName = this.factory.NewPrivateIdentifier(GoRuntime.Slice(symbol.Name, (GoStrings.Index(symbol.Name, "@") + 1)));
        }
        else
        {
            accessName = this.factory.NewIdentifier(symbol.Name);
        }
        var reference = this.factory.NewPropertyAccessExpression(this.factory.NewKeywordExpression(Kind.ThisKeyword), null, accessName, NodeFlags.None);
        reference.Expression().Parent = reference;
        reference.Parent = constructor;
        reference.FlowNodeData().FlowNode = constructor.AsConstructorDeclaration().ReturnFlowNode;
        var flowType = this.getFlowTypeOfProperty(reference, symbol);
        if ((this.noImplicitAny && (((flowType == this.autoType) || (flowType == this.autoArrayType)))))
        {
            this.error(symbol.ValueDeclaration, Diagnostics.Member_0_implicitly_has_an_1_type, this.symbolToString(symbol), this.typeToStringByteString(flowType));
        }
        if (everyType(flowType, this.IsNullableType))
        {
            return null;
        }
        return this.convertAutoToAny(flowType);
    }
}

public sealed partial class Checker
{
    private Type getFlowTypeInStaticBlocks(Symbol symbol, List<Node> staticBlocks)
    {
        Node accessName = default;
        if (GoStrings.HasPrefix(symbol.Name, (Ast.InternalSymbolNamePrefix + "#")))
        {
            accessName = this.factory.NewPrivateIdentifier(GoRuntime.Slice(symbol.Name, (GoStrings.Index(symbol.Name, "@") + 1)));
        }
        else
        {
            accessName = this.factory.NewIdentifier(symbol.Name);
        }
        {
            foreach (var staticBlock in GoRuntime.RangeValues(staticBlocks))
            {
                var reference = this.factory.NewPropertyAccessExpression(this.factory.NewKeywordExpression(Kind.ThisKeyword), null, accessName, NodeFlags.None);
                reference.Expression().Parent = reference;
                reference.Parent = staticBlock;
                reference.FlowNodeData().FlowNode = staticBlock.AsClassStaticBlockDeclaration().ReturnFlowNode;
                var flowType = this.getFlowTypeOfProperty(reference, symbol);
                if ((this.noImplicitAny && (((flowType == this.autoType) || (flowType == this.autoArrayType)))))
                {
                    this.error(symbol.ValueDeclaration, Diagnostics.Member_0_implicitly_has_an_1_type, this.symbolToString(symbol), this.typeToStringByteString(flowType));
                }
                if (everyType(flowType, this.IsNullableType))
                {
                    continue;
                }
                return this.convertAutoToAny(flowType);
            }
        }
        return null;
    }
}

public sealed partial class Checker
{
    private bool isPostSuperFlowNode(FlowNode flow, bool noCacheCheck)
    {
        var f = this.getFlowState();
        var result = this.isPostSuperFlowNodeWorker(f, flow, noCacheCheck);
        this.putFlowState(f);
        return result;
    }
}

public sealed partial class Checker
{
    private bool isPostSuperFlowNodeWorker(FlowState f, FlowNode flow, bool noCacheCheck)
    {
        while (true)
        {
            var flags = flow.Flags;
            if (((flags & FlowFlags.Shared) != 0))
            {
                if (!noCacheCheck)
                {
                    {
                        var ok = this.flowNodePostSuper.TryGetValue(flow, out var postSuper);
                        if (ok)
                        {
                            return postSuper;
                        }
                    }
                    var postSuper2 = this.isPostSuperFlowNodeWorker(f, flow, true);
                    this.flowNodePostSuper[flow] = postSuper2;
                }
                noCacheCheck = false;
            }
            switch (true)
            {
                case true when ((flags & ((((FlowFlags.Assignment | FlowFlags.Condition) | FlowFlags.ArrayMutation) | FlowFlags.SwitchClause))) != 0):
                    {
                        flow = flow.Antecedent;
                        break;
                    }
                case true when ((flags & FlowFlags.Call) != 0):
                    {
                        if ((flow.Node.Expression().Kind == Kind.SuperKeyword))
                        {
                            return true;
                        }
                        flow = flow.Antecedent;
                        break;
                    }
                case true when ((flags & FlowFlags.BranchLabel) != 0):
                    {
                        {
                            var list = getBranchLabelAntecedents(flow, f.reduceLabels);
                            for (; (list != null); list = list.Next)
                            {
                                if (!this.isPostSuperFlowNodeWorker(f, list.Flow, false))
                                {
                                    return false;
                                }
                            }
                        }
                        return true;
                    }
                case true when ((flags & FlowFlags.LoopLabel) != 0):
                    {
                        flow = flow.Antecedents.Flow;
                        break;
                    }
                case true when ((flags & FlowFlags.ReduceLabel) != 0):
                    {
                        f.reduceLabels = GoRuntime.Append(f.reduceLabels, flow.Node.AsFlowReduceLabelData());
                        var result = this.isPostSuperFlowNodeWorker(f, flow.Antecedent, false);
                        f.reduceLabels = GoRuntime.Slice(f.reduceLabels, 0, (GoRuntime.Len(f.reduceLabels) - 1));
                        return result;
                    }
                default:
                    {
                        return ((flags & FlowFlags.Unreachable) != 0);
                    }
            }
        }
    }
}

public sealed partial class Checker
{
    private (string, bool) getDestructuringPropertyName(Node node)
    {
        var parent = node.Parent;
        if ((Ast.IsBindingElement(node) && Ast.IsObjectBindingPattern(parent)))
        {
            return this.getLiteralPropertyNameText(getBindingElementPropertyName(node));
        }
        if ((Ast.IsPropertyAssignment(node) || Ast.IsShorthandPropertyAssignment(node)))
        {
            return this.getLiteralPropertyNameText(node.Name());
        }
        if ((Ast.IsArrayLiteralExpression(parent) || Ast.IsArrayBindingPattern(parent)))
        {
            return (Strconv.Itoa(GoSlices.Index(parent.Elements(), node)), true);
        }
        return ("", false);
    }
}

public sealed partial class Checker
{
    private (string, bool) getLiteralPropertyNameText(Node name)
    {
        var t = this.getLiteralTypeFromPropertyName(name);
        if (((t.flags & ((TypeFlags.StringLiteral | TypeFlags.NumberLiteral))) != 0))
        {
            return (Evaluator.AnyToString(t.AsLiteralType().value), true);
        }
        return ("", false);
    }
}
