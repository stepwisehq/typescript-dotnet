// Authored reflection-free storage mirror for Collections validation.
// Reads actual model slots only; no resolution, mapper invocation or synthetic edges.
// Type/signature allocation IDs are canonicalized by the independent graph; only
// allocation presence is exposed here. Checker ownership references are validated.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.Checking;
public sealed partial class Checker
{
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

            case TypeMapper mapper:
                return VerificationCollectionsDescribe(mapper.data);
            case TypeAlias value:
            {
                return ("TypeAlias", new List<(string Name, string Kind, object Value)> { ("symbol", "symbol", value.symbol), ("typeArguments", "list", value.typeArguments), });
            }

            case Signature value:
            {
                return ("Signature", new List<(string Name, string Kind, object Value)> { ("idAllocated", "bool", value.id != 0), ("flags", "number", value.flags), ("minArgumentCount", "number", value.minArgumentCount), ("resolvedMinArgumentCount", "number", value.resolvedMinArgumentCount), ("declaration", "node", value.declaration), ("typeParameters", "list", value.typeParameters), ("parameters", "list", value.parameters), ("thisParameter", "symbol", value.thisParameter), ("resolvedReturnType", "reference", value.resolvedReturnType), ("isolatedSignatureType", "reference", value.isolatedSignatureType), ("resolvedTypePredicate", "reference", value.resolvedTypePredicate), ("target", "reference", value.target), ("mapper", "reference", value.mapper), ("composite", "reference", value.composite), });
            }

            case TypePredicate value:
            {
                return ("TypePredicate", new List<(string Name, string Kind, object Value)> { ("kind", "number", value.kind), ("parameterIndex", "number", value.parameterIndex), ("parameterName", "bytes", value.parameterName), ("t", "reference", value.t), });
            }

            case CompositeSignature value:
            {
                return ("CompositeSignature", new List<(string Name, string Kind, object Value)> { ("isUnion", "bool", value.isUnion), ("signatures", "list", value.signatures), });
            }

            case IndexInfo value:
            {
                return ("IndexInfo", new List<(string Name, string Kind, object Value)> { ("keyType", "reference", value.keyType), ("valueType", "reference", value.valueType), ("isReadonly", "bool", value.isReadonly), ("declaration", "node", value.declaration), ("indexSymbol", "symbol", value.indexSymbol), ("components", "list", value.components), });
            }

            case ConditionalRoot value:
            {
                return ("ConditionalRoot", new List<(string Name, string Kind, object Value)> { ("node", "node", value.node), ("checkType", "reference", value.checkType), ("extendsType", "reference", value.extendsType), ("isDistributive", "bool", value.isDistributive), ("inferTypeParameters", "list", value.inferTypeParameters), ("outerTypeParameters", "list", value.outerTypeParameters), ("instantiations", "cache", value.instantiations), ("alias", "reference", value.alias), });
            }

            case TupleElementInfo value:
            {
                return ("TupleElementInfo", new List<(string Name, string Kind, object Value)> { ("flags", "number", value.flags), ("labeledDeclaration", "node", value.labeledDeclaration), });
            }

            case InferenceContext value:
            {
                return ("InferenceContext", new List<(string Name, string Kind, object Value)> { ("inferences", "list", value.inferences), ("signature", "reference", value.signature), ("flags", "number", value.flags), ("compareTypes", "function", value.compareTypes), ("mapper", "reference", value.mapper), ("nonFixingMapper", "reference", value.nonFixingMapper), ("returnMapper", "reference", value.returnMapper), ("outerReturnMapper", "reference", value.outerReturnMapper), ("inferredTypeParameters", "list", value.inferredTypeParameters), ("intraExpressionInferenceSites", "list", value.intraExpressionInferenceSites), });
            }

            case InferenceInfo value:
            {
                return ("InferenceInfo", new List<(string Name, string Kind, object Value)> { ("typeParameter", "reference", value.typeParameter), ("inferredType", "reference", value.inferredType), ("candidates", "list", value.candidates), ("contraCandidates", "list", value.contraCandidates), ("priority", "number", value.priority), ("topLevel", "bool", value.topLevel), ("isFixed", "bool", value.isFixed), ("impliedArity", "number", value.impliedArity), });
            }

            case IntraExpressionInferenceSite value:
            {
                return ("IntraExpressionInferenceSite", new List<(string Name, string Kind, object Value)> { ("node", "node", value.node), ("t", "reference", value.t), });
            }

            case TypeAliasLinks value:
            {
                return ("TypeAliasLinks", new List<(string Name, string Kind, object Value)> { ("declaredType", "reference", value.declaredType), ("typeParameters", "list", value.typeParameters), ("instantiations", "cache", value.instantiations), ("isConstructorDeclaredProperty", "bool", value.isConstructorDeclaredProperty), });
            }

            case DeclaredTypeLinks value:
            {
                return ("DeclaredTypeLinks", new List<(string Name, string Kind, object Value)> { ("declaredType", "reference", value.declaredType), ("interfaceChecked", "bool", value.interfaceChecked), ("indexSignaturesChecked", "bool", value.indexSignaturesChecked), ("typeParametersChecked", "bool", value.typeParametersChecked), ("enumChecked", "bool", value.enumChecked), });
            }

            case DeferredSymbolLinks value:
            {
                return ("DeferredSymbolLinks", new List<(string Name, string Kind, object Value)> { ("parent", "reference", value.parent), ("constituents", "list", value.constituents), ("writeConstituents", "list", value.writeConstituents), });
            }

            case SymbolReferenceLinks value:
            {
                return ("SymbolReferenceLinks", new List<(string Name, string Kind, object Value)> { ("referenceKinds", "number", value.referenceKinds), });
            }

            case ValueSymbolLinks value:
            {
                return ("ValueSymbolLinks", new List<(string Name, string Kind, object Value)> { ("functionOrConstructorChecked", "bool", value.functionOrConstructorChecked), ("resolvedType", "reference", value.resolvedType), ("writeType", "reference", value.writeType), ("target", "symbol", value.target), ("mapper", "reference", value.mapper), ("nameType", "reference", value.nameType), ("containingType", "reference", value.containingType), });
            }

            case MappedSymbolLinks value:
            {
                return ("MappedSymbolLinks", new List<(string Name, string Kind, object Value)> { ("keyType", "reference", value.keyType), ("syntheticOrigin", "symbol", value.syntheticOrigin), });
            }

            case ReverseMappedSymbolLinks value:
            {
                return ("ReverseMappedSymbolLinks", new List<(string Name, string Kind, object Value)> { ("propertyType", "reference", value.propertyType), ("mappedType", "reference", value.mappedType), ("constraintType", "reference", value.constraintType), });
            }

            case ArrayLiteralLinks value:
            {
                return ("ArrayLiteralLinks", new List<(string Name, string Kind, object Value)> { ("indicesComputed", "bool", value.indicesComputed), ("firstSpreadIndex", "number", value.firstSpreadIndex), ("lastSpreadIndex", "number", value.lastSpreadIndex), });
            }

            case ComputedNameNodeLinks value:
            {
                return ("ComputedNameNodeLinks", new List<(string Name, string Kind, object Value)> { ("hasName", "bool", value.hasName), ("name", "bytes", value.name), });
            }

            case ModuleSymbolLinks value:
            {
                return ("ModuleSymbolLinks", new List<(string Name, string Kind, object Value)> { ("resolvedExports", "table", value.resolvedExports), ("typeOnlyExportStarMap", "cache", value.typeOnlyExportStarMap), ("exportsChecked", "bool", value.exportsChecked), });
            }

            case SignatureLinks value:
            {
                return ("SignatureLinks", new List<(string Name, string Kind, object Value)> { ("resolvedSignature", "reference", value.resolvedSignature), ("effectsSignature", "reference", value.effectsSignature), ("decoratorSignature", "reference", value.decoratorSignature), });
            }

            case NodeLinks value:
            {
                return ("NodeLinks", new List<(string Name, string Kind, object Value)> { ("flags", "number", value.flags), ("declarationRequiresScopeChange", "number", value.declarationRequiresScopeChange), ("hasReportedStatementInAmbientContext", "bool", value.hasReportedStatementInAmbientContext), });
            }

            case SymbolNodeLinks value:
            {
                return ("SymbolNodeLinks", new List<(string Name, string Kind, object Value)> { ("resolvedSymbol", "symbol", value.resolvedSymbol), });
            }

            case TypeNodeLinks value:
            {
                return ("TypeNodeLinks", new List<(string Name, string Kind, object Value)> { ("resolvedType", "reference", value.resolvedType), ("outerTypeParameters", "list", value.outerTypeParameters), });
            }

            case MarkedAssignmentSymbolLinks value:
            {
                return ("MarkedAssignmentSymbolLinks", new List<(string Name, string Kind, object Value)> { ("lastAssignmentPos", "number", value.lastAssignmentPos), ("hasDefiniteAssignment", "bool", value.hasDefiniteAssignment), });
            }

            case SourceFileLinks value:
            {
                // Existing named validation list boundary only; ordinary checking
                // keeps the identifier slice header and does not project an array.
                return ("SourceFileLinks", new List<(string Name, string Kind, object Value)> { ("typeChecked", "bool", value.typeChecked), ("unusedChecked", "bool", value.unusedChecked), ("externalHelpersModule", "symbol", value.externalHelpersModule), ("requestedExternalEmitHelpers", "number", value.requestedExternalEmitHelpers), ("deferredNodes", "list", value.deferredNodes), ("identifierCheckNodes", "list", value.identifierCheckNodes.IsNil ? null : value.identifierCheckNodes.AsSpan().ToArray()), ("localJsxNamespace", "bytes", value.localJsxNamespace), ("localJsxFragmentNamespace", "bytes", value.localJsxFragmentNamespace), ("localJsxFactory", "node", value.localJsxFactory), ("localJsxFragmentFactory", "node", value.localJsxFragmentFactory), ("jsxFragmentType", "reference", value.jsxFragmentType), });
            }

            case VarianceLinks value:
            {
                return ("VarianceLinks", new List<(string Name, string Kind, object Value)> { ("variances", "list", value.variances), });
            }

            case ContainingSymbolLinks value:
            {
                return ("ContainingSymbolLinks", new List<(string Name, string Kind, object Value)> { ("accessibleChainCache", "cache", value.accessibleChainCache), });
            }

            case ExportTypeLinks value:
            {
                return ("ExportTypeLinks", new List<(string Name, string Kind, object Value)> { ("originatingImport", "node", value.originatingImport), });
            }

            case SimpleTypeMapper value:
            {
                return ("SimpleTypeMapper", new List<(string Name, string Kind, object Value)> { ("source", "reference", value.source), ("target", "reference", value.target), });
            }

            case ArrayTypeMapper value:
            {
                return ("ArrayTypeMapper", new List<(string Name, string Kind, object Value)> { ("sources", "list", value.sources), ("targets", "list", value.targets), });
            }

            case ArrayToSingleTypeMapper value:
            {
                return ("ArrayToSingleTypeMapper", new List<(string Name, string Kind, object Value)> { ("sources", "list", value.sources), ("target", "reference", value.target), });
            }

            case DeferredTypeMapper value:
            {
                return ("DeferredTypeMapper", new List<(string Name, string Kind, object Value)> { ("sources", "list", value.sources), ("targets", "list", value.targets), });
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

            case MembersAndExportsLinks value:
                return ("MembersAndExportsLinks", new List<(string Name, string Kind, object Value)> { ("tables", "list", new List<SymbolTable> { value[MembersOrExportsResolutionKind.ResolvedExports], value[MembersOrExportsResolutionKind.ResolvedMembers] }) });
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
                    ("value", "null", value.value),
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
                    ("properties", "list", value.properties),
                    ("signatures", "list", value.signatures),
                    ("callSignatureCount", "number", value.callSignatureCount),
                    ("indexInfos", "list", value.indexInfos),
                    ("objectTypeWithoutAbstractConstructSignatures", "reference", value.objectTypeWithoutAbstractConstructSignatures),
                    ("target", "reference", value.target),
                    ("mapper", "reference", value.mapper),
                    ("instantiations", "cache", value.instantiations),
                    ("node", "node", value.node),
                    ("resolvedTypeArguments", "list", value.resolvedTypeArguments),
                    ("allTypeParameters", "list", value.allTypeParameters),
                    ("outerTypeParameterCount", "number", value.outerTypeParameterCount),
                    ("thisType", "reference", value.thisType),
                    ("baseTypesResolved", "bool", value.baseTypesResolved),
                    ("declaredMembersResolved", "bool", value.declaredMembersResolved),
                    ("resolvedBaseConstructorType", "reference", value.resolvedBaseConstructorType),
                    ("resolvedBaseTypes", "list", value.resolvedBaseTypes),
                    ("declaredMembers", "table", value.declaredMembers),
                    ("declaredCallSignatures", "list", value.declaredCallSignatures),
                    ("declaredConstructSignatures", "list", value.declaredConstructSignatures),
                    ("declaredIndexInfos", "list", value.declaredIndexInfos),
                    ("elementInfos", "list", value.elementInfos),
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
                    ("properties", "list", value.properties),
                    ("signatures", "list", value.signatures),
                    ("callSignatureCount", "number", value.callSignatureCount),
                    ("indexInfos", "list", value.indexInfos),
                    ("objectTypeWithoutAbstractConstructSignatures", "reference", value.objectTypeWithoutAbstractConstructSignatures),
                    ("target", "reference", value.target),
                    ("mapper", "reference", value.mapper),
                    ("instantiations", "cache", value.instantiations),
                    ("node", "node", value.node),
                    ("resolvedTypeArguments", "list", value.resolvedTypeArguments),
                    ("allTypeParameters", "list", value.allTypeParameters),
                    ("outerTypeParameterCount", "number", value.outerTypeParameterCount),
                    ("thisType", "reference", value.thisType),
                    ("baseTypesResolved", "bool", value.baseTypesResolved),
                    ("declaredMembersResolved", "bool", value.declaredMembersResolved),
                    ("resolvedBaseConstructorType", "reference", value.resolvedBaseConstructorType),
                    ("resolvedBaseTypes", "list", value.resolvedBaseTypes),
                    ("declaredMembers", "table", value.declaredMembers),
                    ("declaredCallSignatures", "list", value.declaredCallSignatures),
                    ("declaredConstructSignatures", "list", value.declaredConstructSignatures),
                    ("declaredIndexInfos", "list", value.declaredIndexInfos),
                };
            case TypeReference value:
                return new List<(string Name, string Kind, object Value)>
                {
                    ("resolvedBaseConstraint", "reference", value.resolvedBaseConstraint),
                    ("members", "table", value.members),
                    ("properties", "list", value.properties),
                    ("signatures", "list", value.signatures),
                    ("callSignatureCount", "number", value.callSignatureCount),
                    ("indexInfos", "list", value.indexInfos),
                    ("objectTypeWithoutAbstractConstructSignatures", "reference", value.objectTypeWithoutAbstractConstructSignatures),
                    ("target", "reference", value.target),
                    ("mapper", "reference", value.mapper),
                    ("instantiations", "cache", value.instantiations),
                    ("node", "node", value.node),
                    ("resolvedTypeArguments", "list", value.resolvedTypeArguments),
                };
            case MappedType value:
                return new List<(string Name, string Kind, object Value)>
                {
                    ("resolvedBaseConstraint", "reference", value.resolvedBaseConstraint),
                    ("members", "table", value.members),
                    ("properties", "list", value.properties),
                    ("signatures", "list", value.signatures),
                    ("callSignatureCount", "number", value.callSignatureCount),
                    ("indexInfos", "list", value.indexInfos),
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
                    ("properties", "list", value.properties),
                    ("signatures", "list", value.signatures),
                    ("callSignatureCount", "number", value.callSignatureCount),
                    ("indexInfos", "list", value.indexInfos),
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
                    ("properties", "list", value.properties),
                    ("signatures", "list", value.signatures),
                    ("callSignatureCount", "number", value.callSignatureCount),
                    ("indexInfos", "list", value.indexInfos),
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
                    ("properties", "list", value.properties),
                    ("signatures", "list", value.signatures),
                    ("callSignatureCount", "number", value.callSignatureCount),
                    ("indexInfos", "list", value.indexInfos),
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
                    ("texts", "list", value.texts),
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
                    ("properties", "list", value.properties),
                    ("signatures", "list", value.signatures),
                    ("callSignatureCount", "number", value.callSignatureCount),
                    ("indexInfos", "list", value.indexInfos),
                    ("objectTypeWithoutAbstractConstructSignatures", "reference", value.objectTypeWithoutAbstractConstructSignatures),
                    ("types", "list", value.types),
                    ("propertyCache", "table", value.propertyCache),
                    ("propertyCacheWithoutFunctionPropertyAugment", "table", value.propertyCacheWithoutFunctionPropertyAugment),
                    ("resolvedProperties", "list", value.resolvedProperties),
                    ("resolvedReducedType", "reference", value.resolvedReducedType),
                    ("regularType", "reference", value.regularType),
                    ("origin", "reference", value.origin),
                    ("keyPropertyName", "bytes", value.keyPropertyName),
                    ("constituentMap", "cache", value.constituentMap),
                };
            case IntersectionType value:
                return new List<(string Name, string Kind, object Value)>
                {
                    ("resolvedBaseConstraint", "reference", value.resolvedBaseConstraint),
                    ("members", "table", value.members),
                    ("properties", "list", value.properties),
                    ("signatures", "list", value.signatures),
                    ("callSignatureCount", "number", value.callSignatureCount),
                    ("indexInfos", "list", value.indexInfos),
                    ("objectTypeWithoutAbstractConstructSignatures", "reference", value.objectTypeWithoutAbstractConstructSignatures),
                    ("types", "list", value.types),
                    ("propertyCache", "table", value.propertyCache),
                    ("propertyCacheWithoutFunctionPropertyAugment", "table", value.propertyCacheWithoutFunctionPropertyAugment),
                    ("resolvedProperties", "list", value.resolvedProperties),
                    ("resolvedApparentType", "reference", value.resolvedApparentType),
                    ("uniqueLiteralFilledInstantiation", "reference", value.uniqueLiteralFilledInstantiation),
                };
            default:
                throw new InvalidOperationException("Unsupported stored Collections type data: " + (payload?.GetType().Name ?? "null"));
        }
    }
}
