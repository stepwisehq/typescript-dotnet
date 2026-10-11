// Authored reflection-free storage mirror for Collections validation.
// Reads actual model slots only; no resolution, mapper invocation or synthetic edges.
// Type/signature allocation IDs are canonicalized by the independent graph; only
// allocation presence is exposed here. Checker ownership references are validated.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    // Named cold boundaries retain standard Memory locations, never payload
    // snapshots. Default equality on boxed Memory still compares owner/offset/length.
    internal static object VerificationLinkLocation<T>(Memory<T> location) where T : struct => location.IsEmpty ? null : location;

    internal static bool VerificationCollectionsTryLinkLocation(object value, out bool isEmpty)
    {
        var state = value switch
        {
            Memory<TypeAliasLinks> location => (true, location.IsEmpty),
            Memory<DeclaredTypeLinks> location => (true, location.IsEmpty),
            Memory<DeferredSymbolLinks> location => (true, location.IsEmpty),
            Memory<SymbolReferenceLinks> location => (true, location.IsEmpty),
            Memory<MappedSymbolLinks> location => (true, location.IsEmpty),
            Memory<ReverseMappedSymbolLinks> location => (true, location.IsEmpty),
            Memory<ArrayLiteralLinks> location => (true, location.IsEmpty),
            Memory<ComputedNameNodeLinks> location => (true, location.IsEmpty),
            Memory<ModuleSymbolLinks> location => (true, location.IsEmpty),
            Memory<SignatureLinks> location => (true, location.IsEmpty),
            Memory<NodeLinks> location => (true, location.IsEmpty),
            Memory<SymbolNodeLinks> location => (true, location.IsEmpty),
            Memory<TypeNodeLinks> location => (true, location.IsEmpty),
            Memory<MarkedAssignmentSymbolLinks> location => (true, location.IsEmpty),
            Memory<SourceFileLinks> location => (true, location.IsEmpty),
            Memory<VarianceLinks> location => (true, location.IsEmpty),
            Memory<ContainingSymbolLinks> location => (true, location.IsEmpty),
            Memory<ExportTypeLinks> location => (true, location.IsEmpty),
            Memory<ValueSymbolLinks> location => (true, location.IsEmpty),
            Memory<MembersAndExportsLinks> location => (true, location.IsEmpty),
            _ => (false, false),
        };
        isEmpty = state.Item2;
        return state.Item1;
    }

    internal (string Name, List<(string Name, string Kind, object Value)> Fields) VerificationCollectionsDescribe(object payload)
    {
        switch (payload)
        {
            case Type t:
            {
                if (t.checker is not null && !ReferenceEquals(t.checker, this))
                    throw new InvalidOperationException("Foreign stored Type owner");
                var fields = new List<(string Name, string Kind, object Value)>
                {
                    ("flags", "number", t.flags),
                    ("objectFlags", "number", t.objectFlags),
                    ("idAllocated", "bool", t.id != 0),
                    ("symbol", "symbol", t.symbol),
                    ("alias", "reference", t.alias),
                };
                fields.AddRange(VerificationCollectionsTypeFields(t.data));
                return (t.DataType(), fields);
            }

            case TypeMapper mapper when mapper is not TypeMapperData:
                return VerificationCollectionsDescribe(mapper.data);
            case TypeAlias value:
            {
                return ("TypeAlias", new List<(string Name, string Kind, object Value)> { ("symbol", "symbol", value.symbol), ("typeArguments", "list", GoSlices.ToListOrNull(value.typeArguments)), });
            }

            case Signature value:
            {
                return ("Signature", new List<(string Name, string Kind, object Value)> { ("idAllocated", "bool", value.id != 0), ("flags", "number", value.flags), ("minArgumentCount", "number", value.minArgumentCount), ("resolvedMinArgumentCount", "number", value.resolvedMinArgumentCount), ("declaration", "node", value.declaration), ("typeParameters", "list", GoSlices.ToListOrNull(value.typeParameters)), ("parameters", "list", GoSlices.ToListOrNull(value.parameters)), ("thisParameter", "symbol", value.thisParameter), ("resolvedReturnType", "reference", value.resolvedReturnType), ("isolatedSignatureType", "reference", value.isolatedSignatureType), ("resolvedTypePredicate", "reference", value.resolvedTypePredicate), ("target", "reference", value.target), ("mapper", "reference", value.mapper), ("composite", "reference", value.composite), });
            }

            case TypePredicate value:
            {
                return ("TypePredicate", new List<(string Name, string Kind, object Value)> { ("kind", "number", value.kind), ("parameterIndex", "number", value.parameterIndex), ("parameterName", "bytes", value.parameterName), ("t", "reference", value.t), });
            }

            case CompositeSignature value:
            {
                return ("CompositeSignature", new List<(string Name, string Kind, object Value)> { ("isUnion", "bool", value.isUnion), ("signatures", "list", GoSlices.ToListOrNull(value.signatures)), });
            }

            case IndexInfo value:
            {
                return ("IndexInfo", new List<(string Name, string Kind, object Value)> { ("keyType", "reference", value.keyType), ("valueType", "reference", value.valueType), ("isReadonly", "bool", value.isReadonly), ("declaration", "node", value.declaration), ("indexSymbol", "symbol", value.indexSymbol), ("components", "list", GoSlices.ToListOrNull(value.components)), });
            }

            case ConditionalRoot value:
            {
                return ("ConditionalRoot", new List<(string Name, string Kind, object Value)> { ("node", "node", value.node), ("checkType", "reference", value.checkType), ("extendsType", "reference", value.extendsType), ("isDistributive", "bool", value.isDistributive), ("inferTypeParameters", "list", GoSlices.ToListOrNull(value.inferTypeParameters)), ("outerTypeParameters", "list", GoSlices.ToListOrNull(value.outerTypeParameters)), ("instantiations", "cache", value.instantiations), ("alias", "reference", value.alias), });
            }

            case TupleElementInfo value:
            {
                return ("TupleElementInfo", new List<(string Name, string Kind, object Value)> { ("flags", "number", value.flags), ("labeledDeclaration", "node", value.labeledDeclaration), });
            }

            case InferenceContext value:
            {
                return ("InferenceContext", new List<(string Name, string Kind, object Value)> { ("inferences", "list", GoSlices.ToListOrNull(value.inferences)), ("signature", "reference", value.signature), ("flags", "number", value.flags), ("compareTypes", "function", value.compareTypes), ("mapper", "reference", value.mapper), ("nonFixingMapper", "reference", value.nonFixingMapper), ("returnMapper", "reference", value.returnMapper), ("outerReturnMapper", "reference", value.outerReturnMapper), ("inferredTypeParameters", "list", GoSlices.ToListOrNull(value.inferredTypeParameters)), ("intraExpressionInferenceSites", "list", GoSlices.ToListOrNull(value.intraExpressionInferenceSites)), });
            }

            case InferenceInfo value:
            {
                return ("InferenceInfo", new List<(string Name, string Kind, object Value)> { ("typeParameter", "reference", value.typeParameter), ("inferredType", "reference", value.inferredType), ("candidates", "list", GoSlices.ToListOrNull(value.candidates)), ("contraCandidates", "list", GoSlices.ToListOrNull(value.contraCandidates)), ("priority", "number", value.priority), ("topLevel", "bool", value.topLevel), ("isFixed", "bool", value.isFixed), ("impliedArity", "number", value.impliedArity), });
            }

            case IntraExpressionInferenceSite value:
            {
                return ("IntraExpressionInferenceSite", new List<(string Name, string Kind, object Value)> { ("node", "node", value.node), ("t", "reference", value.t), });
            }

            case Memory<TypeAliasLinks> location:
            {
                ref var value = ref location.Span[0];
                return ("TypeAliasLinks", new List<(string Name, string Kind, object Value)> { ("declaredType", "reference", value.declaredType), ("typeParameters", "list", GoSlices.ToListOrNull(value.typeParameters)), ("instantiations", "cache", value.instantiations), ("isConstructorDeclaredProperty", "bool", value.isConstructorDeclaredProperty), });
            }

            case Memory<DeclaredTypeLinks> location:
            {
                ref var value = ref location.Span[0];
                return ("DeclaredTypeLinks", new List<(string Name, string Kind, object Value)> { ("declaredType", "reference", value.declaredType), ("interfaceChecked", "bool", value.interfaceChecked), ("indexSignaturesChecked", "bool", value.indexSignaturesChecked), ("typeParametersChecked", "bool", value.typeParametersChecked), ("enumChecked", "bool", value.enumChecked), });
            }

            case Memory<DeferredSymbolLinks> location:
            {
                ref var value = ref location.Span[0];
                return ("DeferredSymbolLinks", new List<(string Name, string Kind, object Value)> { ("parent", "reference", value.parent), ("constituents", "list", GoSlices.ToListOrNull(value.constituents)), ("writeConstituents", "list", GoSlices.ToListOrNull(value.writeConstituents)), });
            }

            case Memory<SymbolReferenceLinks> location:
            {
                ref var value = ref location.Span[0];
                return ("SymbolReferenceLinks", new List<(string Name, string Kind, object Value)> { ("referenceKinds", "number", value.referenceKinds), });
            }

            case Memory<ValueSymbolLinks> location:
            {
                ref var value = ref location.Span[0];
                return ("ValueSymbolLinks", new List<(string Name, string Kind, object Value)> { ("functionOrConstructorChecked", "bool", value.functionOrConstructorChecked), ("resolvedType", "reference", value.resolvedType), ("writeType", "reference", value.writeType), ("target", "symbol", value.target), ("mapper", "reference", value.mapper), ("nameType", "reference", value.nameType), ("containingType", "reference", value.containingType), });
            }

            case Memory<MappedSymbolLinks> location:
            {
                ref var value = ref location.Span[0];
                return ("MappedSymbolLinks", new List<(string Name, string Kind, object Value)> { ("keyType", "reference", value.keyType), ("syntheticOrigin", "symbol", value.syntheticOrigin), });
            }

            case Memory<ReverseMappedSymbolLinks> location:
            {
                ref var value = ref location.Span[0];
                return ("ReverseMappedSymbolLinks", new List<(string Name, string Kind, object Value)> { ("propertyType", "reference", value.propertyType), ("mappedType", "reference", value.mappedType), ("constraintType", "reference", value.constraintType), });
            }

            case Memory<ArrayLiteralLinks> location:
            {
                ref var value = ref location.Span[0];
                return ("ArrayLiteralLinks", new List<(string Name, string Kind, object Value)> { ("indicesComputed", "bool", value.indicesComputed), ("firstSpreadIndex", "number", value.firstSpreadIndex), ("lastSpreadIndex", "number", value.lastSpreadIndex), });
            }

            case Memory<ComputedNameNodeLinks> location:
            {
                ref var value = ref location.Span[0];
                return ("ComputedNameNodeLinks", new List<(string Name, string Kind, object Value)> { ("hasName", "bool", value.hasName), ("name", "bytes", value.name.Materialize()), });
            }

            case Memory<ModuleSymbolLinks> location:
            {
                ref var value = ref location.Span[0];
                return ("ModuleSymbolLinks", new List<(string Name, string Kind, object Value)> { ("resolvedExports", "table", value.resolvedExports), ("typeOnlyExportStarMap", "cache", value.typeOnlyExportStarMap), ("exportsChecked", "bool", value.exportsChecked), });
            }

            case Memory<SignatureLinks> location:
            {
                ref var value = ref location.Span[0];
                return ("SignatureLinks", new List<(string Name, string Kind, object Value)> { ("resolvedSignature", "reference", value.resolvedSignature), ("effectsSignature", "reference", value.effectsSignature), ("decoratorSignature", "reference", value.decoratorSignature), });
            }

            case Memory<NodeLinks> location:
            {
                ref var value = ref location.Span[0];
                return ("NodeLinks", new List<(string Name, string Kind, object Value)> { ("flags", "number", value.flags), ("declarationRequiresScopeChange", "number", value.declarationRequiresScopeChange), ("hasReportedStatementInAmbientContext", "bool", value.hasReportedStatementInAmbientContext), });
            }

            case Memory<SymbolNodeLinks> location:
            {
                ref var value = ref location.Span[0];
                return ("SymbolNodeLinks", new List<(string Name, string Kind, object Value)> { ("resolvedSymbol", "symbol", value.resolvedSymbol), });
            }

            case Memory<TypeNodeLinks> location:
            {
                ref var value = ref location.Span[0];
                return ("TypeNodeLinks", new List<(string Name, string Kind, object Value)> { ("resolvedType", "reference", value.resolvedType), ("outerTypeParameters", "list", GoSlices.ToListOrNull(value.outerTypeParameters)), });
            }

            case Memory<MarkedAssignmentSymbolLinks> location:
            {
                ref var value = ref location.Span[0];
                return ("MarkedAssignmentSymbolLinks", new List<(string Name, string Kind, object Value)> { ("lastAssignmentPos", "number", value.lastAssignmentPos), ("hasDefiniteAssignment", "bool", value.hasDefiniteAssignment), });
            }

            case Memory<SourceFileLinks> location:
            {
                ref var value = ref location.Span[0];
                // Existing named validation list boundary only; ordinary checking
                // keeps the identifier slice header and does not project an array.
                return ("SourceFileLinks", new List<(string Name, string Kind, object Value)> { ("typeChecked", "bool", value.typeChecked), ("unusedChecked", "bool", value.unusedChecked), ("externalHelpersModule", "symbol", value.externalHelpersModule), ("requestedExternalEmitHelpers", "number", value.requestedExternalEmitHelpers), ("deferredNodes", "list", value.deferredNodes), ("identifierCheckNodes", "list", value.identifierCheckNodes.IsNil ? null : value.identifierCheckNodes.AsSpan().ToArray()), ("localJsxNamespace", "bytes", value.localJsxNamespace.Materialize()), ("localJsxFragmentNamespace", "bytes", value.localJsxFragmentNamespace.Materialize()), ("localJsxFactory", "node", value.localJsxFactory), ("localJsxFragmentFactory", "node", value.localJsxFragmentFactory), ("jsxFragmentType", "reference", value.jsxFragmentType), });
            }

            case Memory<VarianceLinks> location:
            {
                ref var value = ref location.Span[0];
                return ("VarianceLinks", new List<(string Name, string Kind, object Value)> { ("variances", "list", GoSlices.ToListOrNull(value.variances)), });
            }

            case Memory<ContainingSymbolLinks> location:
            {
                ref var value = ref location.Span[0];
                return ("ContainingSymbolLinks", new List<(string Name, string Kind, object Value)> { ("accessibleChainCache", "cache", value.accessibleChainCache?.ToDictionary(entry => entry.Key, entry => GoSlices.ToListOrNull(entry.Value))), });
            }

            case Memory<ExportTypeLinks> location:
            {
                ref var value = ref location.Span[0];
                return ("ExportTypeLinks", new List<(string Name, string Kind, object Value)> { ("originatingImport", "node", value.originatingImport), });
            }

            case SimpleTypeMapper value:
            {
                return ("SimpleTypeMapper", new List<(string Name, string Kind, object Value)> { ("source", "reference", value.source), ("target", "reference", value.target), });
            }

            case ArrayTypeMapper value:
            {
                return ("ArrayTypeMapper", new List<(string Name, string Kind, object Value)> { ("sources", "list", GoSlices.ToListOrNull(value.sources)), ("targets", "list", GoSlices.ToListOrNull(value.targets)), });
            }

            case ArrayToSingleTypeMapper value:
            {
                return ("ArrayToSingleTypeMapper", new List<(string Name, string Kind, object Value)> { ("sources", "list", GoSlices.ToListOrNull(value.sources)), ("target", "reference", value.target), });
            }

            case DeferredTypeMapper value:
            {
                return ("DeferredTypeMapper", new List<(string Name, string Kind, object Value)> { ("sources", "list", GoSlices.ToListOrNull(value.sources)), ("targets", "list", value.targets), });
            }

            case FunctionTypeMapper value:
            {
                return ("FunctionTypeMapper", new List<(string Name, string Kind, object Value)> { ("fn", "function", value.fn), });
            }

            case MergedTypeMapper value:
            {
                return ("MergedTypeMapper", new List<(string Name, string Kind, object Value)> { ("m1", "reference", value.m1), ("m2", "reference", value.m2), });
            }

            case CompositeTypeMapper value:
            {
                if (!ReferenceEquals(value.c, this))
                    throw new InvalidOperationException("Foreign stored mapper owner");
                return ("CompositeTypeMapper", new List<(string Name, string Kind, object Value)> { ("m1", "reference", value.m1), ("m2", "reference", value.m2), });
            }

            case InferenceTypeMapper value:
            {
                if (!ReferenceEquals(value.c, this))
                    throw new InvalidOperationException("Foreign stored mapper owner");
                return ("InferenceTypeMapper", new List<(string Name, string Kind, object Value)> { ("n", "reference", value.n), ("fixing", "bool", value.fixing), });
            }

            case TypeMapperBase value:
            {
                return ("TypeMapperBase", new List<(string Name, string Kind, object Value)> { });
            }

            case Memory<MembersAndExportsLinks> location:
            {
                ref var value = ref location.Span[0];
                return ("MembersAndExportsLinks", new List<(string Name, string Kind, object Value)> { ("tables", "list", new List<SymbolTable> { value[MembersOrExportsResolutionKind.ResolvedExports], value[MembersOrExportsResolutionKind.ResolvedMembers] }) });
            }
            default:
                throw new InvalidOperationException("Unsupported stored Collections payload: " + (payload?.GetType().Name ?? "null"));
        }
    }

    private static List<(string Name, string Kind, object Value)> VerificationCollectionsTypeFields(TypeData payload)
    {
        switch (payload)
        {
            case IntrinsicType value:
                return new List<(string Name, string Kind, object Value)>
                {
                    ("intrinsicName", "bytes", value.intrinsicName),
                };
            case LiteralType value:
                return new List<(string Name, string Kind, object Value)>
                {
                    ("value", "null", value.Value()),
                    ("freshType", "reference", value.freshType),
                    ("regularType", "reference", value.regularType),
                };
            case UniqueESSymbolType value:
                return new List<(string Name, string Kind, object Value)>
                {
                    ("name", "bytes", value.name),
                };
            case TupleType value:
                return new List<(string Name, string Kind, object Value)>
                {
                    ("resolvedBaseConstraint", "reference", value.resolvedBaseConstraint),
                    ("members", "table", value.members),
                    ("properties", "list", GoSlices.ToListOrNull(value.properties)),
                    ("signatures", "list", GoSlices.ToListOrNull(value.signatures)),
                    ("callSignatureCount", "number", value.callSignatureCount),
                    ("indexInfos", "list", GoSlices.ToListOrNull(value.indexInfos)),
                    ("objectTypeWithoutAbstractConstructSignatures", "reference", value.objectTypeWithoutAbstractConstructSignatures),
                    ("target", "reference", value.target),
                    ("mapper", "reference", value.mapper),
                    ("instantiations", "cache", value.instantiations),
                    ("node", "node", value.node),
                    ("resolvedTypeArguments", "list", GoSlices.ToListOrNull(value.resolvedTypeArguments)),
                    ("allTypeParameters", "list", GoSlices.ToListOrNull(value.allTypeParameters)),
                    ("outerTypeParameterCount", "number", value.outerTypeParameterCount),
                    ("thisType", "reference", value.thisType),
                    ("baseTypesResolved", "bool", value.baseTypesResolved),
                    ("declaredMembersResolved", "bool", value.declaredMembersResolved),
                    ("resolvedBaseConstructorType", "reference", value.resolvedBaseConstructorType),
                    ("resolvedBaseTypes", "list", GoSlices.ToListOrNull(value.resolvedBaseTypes)),
                    ("declaredMembers", "table", value.declaredMembers),
                    ("declaredCallSignatures", "list", GoSlices.ToListOrNull(value.declaredCallSignatures)),
                    ("declaredConstructSignatures", "list", GoSlices.ToListOrNull(value.declaredConstructSignatures)),
                    ("declaredIndexInfos", "list", GoSlices.ToListOrNull(value.declaredIndexInfos)),
                    ("elementInfos", "list", GoSlices.ToListOrNull(value.elementInfos)),
                    ("minLength", "number", value.minLength),
                    ("fixedLength", "number", value.fixedLength),
                    ("combinedFlags", "number", value.combinedFlags),
                    ("readonly", "bool", value.@readonly),
                };
            case InterfaceType value:
                return new List<(string Name, string Kind, object Value)>
                {
                    ("resolvedBaseConstraint", "reference", value.resolvedBaseConstraint),
                    ("members", "table", value.members),
                    ("properties", "list", GoSlices.ToListOrNull(value.properties)),
                    ("signatures", "list", GoSlices.ToListOrNull(value.signatures)),
                    ("callSignatureCount", "number", value.callSignatureCount),
                    ("indexInfos", "list", GoSlices.ToListOrNull(value.indexInfos)),
                    ("objectTypeWithoutAbstractConstructSignatures", "reference", value.objectTypeWithoutAbstractConstructSignatures),
                    ("target", "reference", value.target),
                    ("mapper", "reference", value.mapper),
                    ("instantiations", "cache", value.instantiations),
                    ("node", "node", value.node),
                    ("resolvedTypeArguments", "list", GoSlices.ToListOrNull(value.resolvedTypeArguments)),
                    ("allTypeParameters", "list", GoSlices.ToListOrNull(value.allTypeParameters)),
                    ("outerTypeParameterCount", "number", value.outerTypeParameterCount),
                    ("thisType", "reference", value.thisType),
                    ("baseTypesResolved", "bool", value.baseTypesResolved),
                    ("declaredMembersResolved", "bool", value.declaredMembersResolved),
                    ("resolvedBaseConstructorType", "reference", value.resolvedBaseConstructorType),
                    ("resolvedBaseTypes", "list", GoSlices.ToListOrNull(value.resolvedBaseTypes)),
                    ("declaredMembers", "table", value.declaredMembers),
                    ("declaredCallSignatures", "list", GoSlices.ToListOrNull(value.declaredCallSignatures)),
                    ("declaredConstructSignatures", "list", GoSlices.ToListOrNull(value.declaredConstructSignatures)),
                    ("declaredIndexInfos", "list", GoSlices.ToListOrNull(value.declaredIndexInfos)),
                };
            case TypeReference value:
                return new List<(string Name, string Kind, object Value)>
                {
                    ("resolvedBaseConstraint", "reference", value.resolvedBaseConstraint),
                    ("members", "table", value.members),
                    ("properties", "list", GoSlices.ToListOrNull(value.properties)),
                    ("signatures", "list", GoSlices.ToListOrNull(value.signatures)),
                    ("callSignatureCount", "number", value.callSignatureCount),
                    ("indexInfos", "list", GoSlices.ToListOrNull(value.indexInfos)),
                    ("objectTypeWithoutAbstractConstructSignatures", "reference", value.objectTypeWithoutAbstractConstructSignatures),
                    ("target", "reference", value.target),
                    ("mapper", "reference", value.mapper),
                    ("instantiations", "cache", value.instantiations),
                    ("node", "node", value.node),
                    ("resolvedTypeArguments", "list", GoSlices.ToListOrNull(value.resolvedTypeArguments)),
                };
            case MappedType value:
                return new List<(string Name, string Kind, object Value)>
                {
                    ("resolvedBaseConstraint", "reference", value.resolvedBaseConstraint),
                    ("members", "table", value.members),
                    ("properties", "list", GoSlices.ToListOrNull(value.properties)),
                    ("signatures", "list", GoSlices.ToListOrNull(value.signatures)),
                    ("callSignatureCount", "number", value.callSignatureCount),
                    ("indexInfos", "list", GoSlices.ToListOrNull(value.indexInfos)),
                    ("objectTypeWithoutAbstractConstructSignatures", "reference", value.objectTypeWithoutAbstractConstructSignatures),
                    ("target", "reference", value.target),
                    ("mapper", "reference", value.mapper),
                    ("instantiations", "cache", value.instantiations),
                    ("declaration", "node", value.declaration),
                    ("typeParameter", "reference", value.typeParameter),
                    ("constraintType", "reference", value.constraintType),
                    ("nameType", "reference", value.nameType),
                    ("templateType", "reference", value.templateType),
                    ("modifiersType", "reference", value.modifiersType),
                    ("resolvedApparentType", "reference", value.resolvedApparentType),
                    ("containsError", "bool", value.containsError),
                };
            case ReverseMappedType value:
                return new List<(string Name, string Kind, object Value)>
                {
                    ("resolvedBaseConstraint", "reference", value.resolvedBaseConstraint),
                    ("members", "table", value.members),
                    ("properties", "list", GoSlices.ToListOrNull(value.properties)),
                    ("signatures", "list", GoSlices.ToListOrNull(value.signatures)),
                    ("callSignatureCount", "number", value.callSignatureCount),
                    ("indexInfos", "list", GoSlices.ToListOrNull(value.indexInfos)),
                    ("objectTypeWithoutAbstractConstructSignatures", "reference", value.objectTypeWithoutAbstractConstructSignatures),
                    ("target", "reference", value.target),
                    ("mapper", "reference", value.mapper),
                    ("instantiations", "cache", value.instantiations),
                    ("source", "reference", value.source),
                    ("mappedType", "reference", value.mappedType),
                    ("constraintType", "reference", value.constraintType),
                };
            case EvolvingArrayType value:
                return new List<(string Name, string Kind, object Value)>
                {
                    ("resolvedBaseConstraint", "reference", value.resolvedBaseConstraint),
                    ("members", "table", value.members),
                    ("properties", "list", GoSlices.ToListOrNull(value.properties)),
                    ("signatures", "list", GoSlices.ToListOrNull(value.signatures)),
                    ("callSignatureCount", "number", value.callSignatureCount),
                    ("indexInfos", "list", GoSlices.ToListOrNull(value.indexInfos)),
                    ("objectTypeWithoutAbstractConstructSignatures", "reference", value.objectTypeWithoutAbstractConstructSignatures),
                    ("target", "reference", value.target),
                    ("mapper", "reference", value.mapper),
                    ("instantiations", "cache", value.instantiations),
                    ("elementType", "reference", value.elementType),
                    ("finalArrayType", "reference", value.finalArrayType),
                };
            case ObjectType value:
                return new List<(string Name, string Kind, object Value)>
                {
                    ("resolvedBaseConstraint", "reference", value.resolvedBaseConstraint),
                    ("members", "table", value.members),
                    ("properties", "list", GoSlices.ToListOrNull(value.properties)),
                    ("signatures", "list", GoSlices.ToListOrNull(value.signatures)),
                    ("callSignatureCount", "number", value.callSignatureCount),
                    ("indexInfos", "list", GoSlices.ToListOrNull(value.indexInfos)),
                    ("objectTypeWithoutAbstractConstructSignatures", "reference", value.objectTypeWithoutAbstractConstructSignatures),
                    ("target", "reference", value.target),
                    ("mapper", "reference", value.mapper),
                    ("instantiations", "cache", value.instantiations),
                };
            case TypeParameter value:
                return new List<(string Name, string Kind, object Value)>
                {
                    ("resolvedBaseConstraint", "reference", value.resolvedBaseConstraint),
                    ("constraint", "reference", value.constraint),
                    ("target", "reference", value.target),
                    ("mapper", "reference", value.mapper),
                    ("isThisType", "bool", value.isThisType),
                    ("isDistributed", "bool", value.isDistributed),
                    ("resolvedDefaultType", "reference", value.resolvedDefaultType),
                    ("distributedType", "reference", value.distributedType),
                };
            case IndexType value:
                return new List<(string Name, string Kind, object Value)>
                {
                    ("resolvedBaseConstraint", "reference", value.resolvedBaseConstraint),
                    ("target", "reference", value.target),
                    ("indexFlags", "number", value.indexFlags),
                };
            case IndexedAccessType value:
                return new List<(string Name, string Kind, object Value)>
                {
                    ("resolvedBaseConstraint", "reference", value.resolvedBaseConstraint),
                    ("objectType", "reference", value.objectType),
                    ("indexType", "reference", value.indexType),
                    ("accessFlags", "number", value.accessFlags),
                };
            case TemplateLiteralType value:
                return new List<(string Name, string Kind, object Value)>
                {
                    ("resolvedBaseConstraint", "reference", value.resolvedBaseConstraint),
                    ("texts", "list", value.Texts()),
                    ("types", "list", value.types),
                };
            case SubstitutionType value:
                return new List<(string Name, string Kind, object Value)>
                {
                    ("resolvedBaseConstraint", "reference", value.resolvedBaseConstraint),
                    ("baseType", "reference", value.baseType),
                    ("constraint", "reference", value.constraint),
                };
            case ConditionalType value:
                return new List<(string Name, string Kind, object Value)>
                {
                    ("resolvedBaseConstraint", "reference", value.resolvedBaseConstraint),
                    ("root", "reference", value.root),
                    ("checkType", "reference", value.checkType),
                    ("extendsType", "reference", value.extendsType),
                    ("resolvedTrueType", "reference", value.resolvedTrueType),
                    ("resolvedFalseType", "reference", value.resolvedFalseType),
                    ("resolvedInferredTrueType", "reference", value.resolvedInferredTrueType),
                    ("resolvedDefaultConstraint", "reference", value.resolvedDefaultConstraint),
                    ("resolvedConstraintOfDistributive", "reference", value.resolvedConstraintOfDistributive),
                    ("mapper", "reference", value.mapper),
                    ("combinedMapper", "reference", value.combinedMapper),
                };
            case UnionType value:
                return new List<(string Name, string Kind, object Value)>
                {
                    ("resolvedBaseConstraint", "reference", value.resolvedBaseConstraint),
                    ("members", "table", value.members),
                    ("properties", "list", GoSlices.ToListOrNull(value.properties)),
                    ("signatures", "list", GoSlices.ToListOrNull(value.signatures)),
                    ("callSignatureCount", "number", value.callSignatureCount),
                    ("indexInfos", "list", GoSlices.ToListOrNull(value.indexInfos)),
                    ("objectTypeWithoutAbstractConstructSignatures", "reference", value.objectTypeWithoutAbstractConstructSignatures),
                    ("types", "list", value.types),
                    ("propertyCache", "table", value.propertyCache),
                    ("propertyCacheWithoutFunctionPropertyAugment", "table", value.propertyCacheWithoutFunctionPropertyAugment),
                    ("resolvedProperties", "list", GoSlices.ToListOrNull(value.resolvedProperties)),
                    ("resolvedReducedType", "reference", value.resolvedReducedType),
                    ("regularType", "reference", value.regularType),
                    ("origin", "reference", value.origin),
                    ("keyPropertyName", "bytes", value.keyPropertyName.Materialize()),
                    ("constituentMap", "cache", value.constituentMap),
                };
            case IntersectionType value:
                return new List<(string Name, string Kind, object Value)>
                {
                    ("resolvedBaseConstraint", "reference", value.resolvedBaseConstraint),
                    ("members", "table", value.members),
                    ("properties", "list", GoSlices.ToListOrNull(value.properties)),
                    ("signatures", "list", GoSlices.ToListOrNull(value.signatures)),
                    ("callSignatureCount", "number", value.callSignatureCount),
                    ("indexInfos", "list", GoSlices.ToListOrNull(value.indexInfos)),
                    ("objectTypeWithoutAbstractConstructSignatures", "reference", value.objectTypeWithoutAbstractConstructSignatures),
                    ("types", "list", value.types),
                    ("propertyCache", "table", value.propertyCache),
                    ("propertyCacheWithoutFunctionPropertyAugment", "table", value.propertyCacheWithoutFunctionPropertyAugment),
                    ("resolvedProperties", "list", GoSlices.ToListOrNull(value.resolvedProperties)),
                    ("resolvedApparentType", "reference", value.resolvedApparentType),
                    ("uniqueLiteralFilledInstantiation", "reference", value.uniqueLiteralFilledInstantiation),
                };
            default:
                throw new InvalidOperationException("Unsupported stored Collections type data: " + (payload?.GetType().Name ?? "null"));
        }
    }
}
