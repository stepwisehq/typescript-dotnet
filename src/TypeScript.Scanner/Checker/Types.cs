// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Partial counterpart of tsc/internal/checker/types.go: source flags and selected declaration/type models.
using TypeScript.Scanner;
using CacheHashKey = TypeScript.Checking.Checker.CacheHashKey;

namespace TypeScript.Checking;

[Flags]
public enum TypeFlags : uint
{
    None = 0,
    Any = (1u << 0),
    Unknown = (1u << 1),
    Undefined = (1u << 2),
    Null = (1u << 3),
    Void = (1u << 4),
    String = (1u << 5),
    Number = (1u << 6),
    BigInt = (1u << 7),
    Boolean = (1u << 8),
    ESSymbol = (1u << 9), // Type of symbol primitive introduced in ES6
    StringLiteral = (1u << 10),
    NumberLiteral = (1u << 11),
    BigIntLiteral = (1u << 12),
    BooleanLiteral = (1u << 13),
    UniqueESSymbol = (1u << 14), // unique symbol
    EnumLiteral = (1u << 15), // Always combined with StringLiteral, NumberLiteral, or Union
    Enum = (1u << 16), // Numeric computed enum member value (must be right after EnumLiteral, see getSortOrderFlags)
    NonPrimitive = (1u << 17), // intrinsic object type
    Never = (1u << 18), // Never type
    TypeParameter = (1u << 19), // Type parameter
    Object = (1u << 20), // Object type
    Index = (1u << 21), // keyof T
    TemplateLiteral = (1u << 22), // Template literal type
    StringMapping = (1u << 23), // Uppercase/Lowercase type
    Substitution = (1u << 24), // Type parameter substitution
    IndexedAccess = (1u << 25), // T[K]
    Conditional = (1u << 26), // T extends U ? X : Y
    Union = (1u << 27), // Union (T | U)
    Intersection = (1u << 28), // Intersection (T & U)
    Reserved1 = (1u << 29), // Used by union/intersection type construction
    Reserved2 = (1u << 30), // Used by union/intersection type construction
    Reserved3 = (1u << 31),
    AnyOrUnknown = Any | Unknown,
    Nullable = Undefined | Null,
    Literal = StringLiteral | NumberLiteral | BigIntLiteral | BooleanLiteral,
    Unit = Enum | Literal | UniqueESSymbol | Nullable,
    Freshable = Enum | Literal,
    StringOrNumberLiteral = StringLiteral | NumberLiteral,
    StringOrNumberLiteralOrUnique = StringLiteral | NumberLiteral | UniqueESSymbol,
    DefinitelyFalsy = StringLiteral | NumberLiteral | BigIntLiteral | BooleanLiteral | Void | Undefined | Null,
    PossiblyFalsy = DefinitelyFalsy | String | Number | BigInt | Boolean,
    Intrinsic = Any | Unknown | String | Number | BigInt | ESSymbol | Void | Undefined | Null | Never | NonPrimitive,
    StringLike = String | StringLiteral | TemplateLiteral | StringMapping,
    NumberLike = Number | NumberLiteral | Enum,
    BigIntLike = BigInt | BigIntLiteral,
    BooleanLike = Boolean | BooleanLiteral,
    EnumLike = Enum | EnumLiteral,
    ESSymbolLike = ESSymbol | UniqueESSymbol,
    VoidLike = Void | Undefined,
    Primitive = StringLike | NumberLike | BigIntLike | BooleanLike | EnumLike | ESSymbolLike | VoidLike | Null,
    DefinitelyNonNullable = StringLike | NumberLike | BigIntLike | BooleanLike | EnumLike | ESSymbolLike | Object | NonPrimitive,
    DisjointDomains = NonPrimitive | StringLike | NumberLike | BigIntLike | BooleanLike | ESSymbolLike | VoidLike | Null,
    UnionOrIntersection = Union | Intersection,
    StructuredType = Object | Union | Intersection,
    TypeVariable = TypeParameter | IndexedAccess,
    InstantiableNonPrimitive = TypeVariable | Conditional | Substitution,
    InstantiablePrimitive = Index | TemplateLiteral | StringMapping,
    Instantiable = InstantiableNonPrimitive | InstantiablePrimitive,
    StructuredOrInstantiable = StructuredType | Instantiable,
    ObjectFlagsType = Any | Nullable | Never | Object | Union | Intersection,
    Simplifiable = IndexedAccess | Conditional | Index,
    Singleton = Any | Unknown | String | Number | Boolean | BigInt | ESSymbol | Void | Undefined | Null | Never | NonPrimitive,
    Narrowable = Any | Unknown | StructuredOrInstantiable | StringLike | NumberLike | BigIntLike | BooleanLike | ESSymbol | UniqueESSymbol | NonPrimitive,
    IncludesMask = Any | Unknown | Primitive | Never | Object | Union | Intersection | NonPrimitive | TemplateLiteral | StringMapping,
    IncludesMissingType = TypeParameter,
    IncludesNonWideningType = Index,
    IncludesWildcard = IndexedAccess,
    IncludesEmptyObject = Conditional,
    IncludesInstantiable = Substitution,
    IncludesConstrainedTypeVariable = Reserved1,
    IncludesError = Reserved2,
    NotPrimitiveUnion = Any | Unknown | Void | Never | Object | Intersection | IncludesInstantiable,
}

[Flags]
public enum ObjectFlags : uint
{
    None = 0,
    Class = (1u << 0), // Class
    Interface = (1u << 1), // Interface
    Reference = (1u << 2), // Generic type reference
    Tuple = (1u << 3), // Synthesized generic tuple type
    Anonymous = (1u << 4), // Anonymous
    Mapped = (1u << 5), // Mapped
    Instantiated = (1u << 6), // Instantiated anonymous or mapped type
    ObjectLiteral = (1u << 7), // Originates in an object literal
    EvolvingArray = (1u << 8), // Evolving array type
    ObjectLiteralPatternWithComputedProperties = (1u << 9), // Object literal pattern with computed properties
    ReverseMapped = (1u << 10), // Object contains a property from a reverse-mapped type
    JsxAttributes = (1u << 11), // Jsx attributes type
    JSLiteral = (1u << 12), // Object type declared in JS - disables errors on read/write of nonexisting members
    FreshLiteral = (1u << 13), // Fresh object literal
    ArrayLiteral = (1u << 14), // Originates in an array literal
    PrimitiveUnion = (1u << 15), // Union of only primitive types
    ContainsWideningType = (1u << 16), // Type is or contains undefined or null widening type
    ContainsObjectOrArrayLiteral = (1u << 17), // Type is or contains object literal type
    NonInferrableType = (1u << 18), // Type is or contains anyFunctionType or silentNeverType
    CouldContainTypeVariablesComputed = (1u << 19), // CouldContainTypeVariables flag has been computed
    CouldContainTypeVariables = (1u << 20), // Type could contain a type variable
    MembersResolved = (1u << 21), // Members have been resolved
    ClassOrInterface = Class | Interface,
    RequiresWidening = ContainsWideningType | ContainsObjectOrArrayLiteral,
    PropagatingFlags = ContainsWideningType | ContainsObjectOrArrayLiteral | NonInferrableType,
    InstantiatedMapped = Mapped | Instantiated,
    ObjectTypeKindMask = ClassOrInterface | Reference | Tuple | Anonymous | Mapped | ReverseMapped | EvolvingArray | InstantiationExpressionType | SingleSignatureType,
    ContainsSpread = (1u << 22), // Object literal contains spread operation
    ObjectRestType = (1u << 23), // Originates in object rest declaration
    InstantiationExpressionType = (1u << 24), // Originates in instantiation expression
    SingleSignatureType = (1u << 25), // A single signature type extracted from a potentially broader type
    IsClassInstanceClone = (1u << 26), // Type is a clone of a class instance type
    IdenticalBaseTypeCalculated = (1u << 27), // has had `getSingleBaseForNonAugmentingSubtype` invoked on it already
    IdenticalBaseTypeExists = (1u << 28), // has a defined cachedEquivalentBaseType member
    FromTypeNode = (1u << 29), // Originates in resolution of AST type node
    IsGenericTypeComputed = (1u << 22), // IsGenericObjectType flag has been computed
    IsGenericObjectType = (1u << 23), // Union or intersection contains generic object type
    IsGenericIndexType = (1u << 24), // Union or intersection contains generic index type
    IsGenericType = IsGenericObjectType | IsGenericIndexType,
    ContainsIntersections = (1u << 25), // Union contains intersections
    IsUnknownLikeUnionComputed = (1u << 26), // IsUnknownLikeUnion flag has been computed
    IsUnknownLikeUnion = (1u << 27), // Union of null, undefined, and empty object type
    IsUniformEnumComputed = (1u << 28), // IsUniformEnum flag has been computed
    IsUniformEnum = (1u << 29), // Union contains uniform literal types
    IsNeverIntersectionComputed = (1u << 25), // IsNeverLike flag has been computed
    IsNeverIntersection = (1u << 26), // Intersection reduces to never
    IsConstrainedTypeVariable = (1u << 27), // T & C, where T's constraint and C are primitives, object, or {}
}

[Flags]
public enum NodeCheckFlags : uint
{
    None = 0,
    TypeChecked = (1u << 0), // Node has been type checked
    ContextChecked = (1u << 6), // Contextual types have been assigned
    EnumValuesComputed = (1u << 10), // Values for enum members have been computed, and any errors have been reported for them.
    AssignmentsMarked = (1u << 17), // Parameter assignments have been marked
    ContainsClassWithPrivateIdentifiers = (1u << 20), // Marked on all block-scoped containers containing a class with private identifiers.
    ContainsSuperPropertyInStaticInitializer = (1u << 21), // Marked on all block-scoped containers containing a static initializer with 'super.x' or 'super[x]'.
    InCheckIdentifier = (1u << 22),
    InitializerIsUndefined = (1u << 24),
    InitializerIsUndefinedComputed = (1u << 25),
}

// BEGIN MANUAL TYPE MODEL
// Selected data-type models retain Go wrapper/payload identity, including generic
// references, inference-facing mapped/conditional types, arrays and tuples.
#nullable disable
public sealed class TypeAlias
{
    internal Symbol symbol;
    internal List<Type> typeArguments;
    public Symbol Symbol() => symbol;
    public List<Type> TypeArguments() => typeArguments;
}

public sealed class Type
{
    internal TypeFlags flags;
    internal ObjectFlags objectFlags;
    internal uint id;
    internal Symbol symbol;
    internal TypeAlias alias;
    internal Checker checker;
    internal TypeData data;
    internal Type(TypeData data) => this.data = data;
    public uint Id() => id;
    public TypeFlags Flags() => flags;
    public ObjectFlags ObjectFlags() => objectFlags;
    public Symbol Symbol() => symbol;
    public TypeAlias Alias() => alias;
    public IntrinsicType AsIntrinsicType() => (IntrinsicType)data;
    public LiteralType AsLiteralType() => (LiteralType)data;
    public UniqueESSymbolType AsUniqueESSymbolType() => (UniqueESSymbolType)data;
    public TupleType AsTupleType() => (TupleType)data;
    public MappedType AsMappedType() => (MappedType)data;
    public ReverseMappedType AsReverseMappedType() => (ReverseMappedType)data;
    public EvolvingArrayType AsEvolvingArrayType() => (EvolvingArrayType)data;
    public UnionType AsUnionType() => (UnionType)data;
    public IntersectionType AsIntersectionType() => (IntersectionType)data;
    public TypeParameter AsTypeParameter() => (TypeParameter)data;
    public IndexType AsIndexType() => (IndexType)data;
    public IndexedAccessType AsIndexedAccessType() => (IndexedAccessType)data;
    public TemplateLiteralType AsTemplateLiteralType() => (TemplateLiteralType)data;
    public StringMappingType AsStringMappingType() => (StringMappingType)data;
    public SubstitutionType AsSubstitutionType() => (SubstitutionType)data;
    public ConditionalType AsConditionalType() => (ConditionalType)data;
    public ConstrainedType AsConstrainedType() => data.AsConstrainedType();
    public StructuredType AsStructuredType() => data.AsStructuredType();
    public UnionOrIntersectionType AsUnionOrIntersectionType() => data.AsUnionOrIntersectionType();
    public ObjectType AsObjectType() => data.AsObjectType();
    public TypeReference AsTypeReference() => data.AsTypeReference();
    public InstantiationExpressionType AsInstantiationExpressionType() => (InstantiationExpressionType)data;
    public InterfaceType AsInterfaceType() => data.AsInterfaceType();
    // types.go:752/766. Actual target/mapper fields, including nullable origins.
    public Type Target()
    {
        if ((flags & TypeFlags.Object) != 0)
            return AsObjectType().target;
        if ((flags & TypeFlags.TypeParameter) != 0)
            return AsTypeParameter().target;
        if ((flags & TypeFlags.Index) != 0)
            return AsIndexType().target;
        if ((flags & TypeFlags.StringMapping) != 0)
            return AsStringMappingType().target;
        throw new InvalidOperationException("Unhandled case in Type.Target");
    }

    internal TypeMapper Mapper()
    {
        if ((flags & TypeFlags.Object) != 0)
            return AsObjectType().mapper;
        if ((flags & TypeFlags.TypeParameter) != 0)
            return AsTypeParameter().mapper;
        if ((flags & TypeFlags.Conditional) != 0)
            return AsConditionalType().mapper;
        throw new InvalidOperationException("Unhandled case in Type.Mapper");
    }

    public InterfaceType TargetInterfaceType() => Target().AsInterfaceType();
    public TupleType TargetTupleType() => AsTypeReference().target.AsTupleType();
    // Upstream checker/types.go:740.
    public List<Type> Distributed()
    {
        if ((flags & TypeFlags.Union) != 0)
            return AsUnionType().types;
        if ((flags & TypeFlags.Never) != 0)
            return null;
        return [this];
    }

    // Upstream checker/types.go:780; union/intersection and template payloads.
    public List<Type> Types()
    {
        if ((flags & TypeFlags.UnionOrIntersection) != 0)
            return AsUnionOrIntersectionType().types;
        if ((flags & TypeFlags.TemplateLiteral) != 0)
            return AsTemplateLiteralType().types;
        throw new InvalidOperationException("Unhandled case in Type.Types");
    }

    // Authored AOT-safe payload identity for the independent semantic graph.
    public string DataType() => data switch
    {
        IntrinsicType => "IntrinsicType",
        LiteralType => "LiteralType",
        UniqueESSymbolType => "UniqueESSymbolType",
        UnionType => "UnionType",
        IntersectionType => "IntersectionType",
        TupleType => "TupleType",
        InterfaceType => "InterfaceType",
        TypeReference => "TypeReference",
        MappedType => "MappedType",
        ReverseMappedType => "ReverseMappedType",
        EvolvingArrayType => "EvolvingArrayType",
        ObjectType => "ObjectType",
        TypeParameter => "TypeParameter",
        IndexType => "IndexType",
        IndexedAccessType => "IndexedAccessType",
        TemplateLiteralType => "TemplateLiteralType",
        SubstitutionType => "SubstitutionType",
        ConditionalType => "ConditionalType",
        _ => throw new NotSupportedException("Unselected type payload")
    };
    public bool IsUnion() => (flags & TypeFlags.Union) != 0;
    public bool IsString() => (flags & TypeFlags.String) != 0;
    public bool IsIntersection() => (flags & TypeFlags.Intersection) != 0;
    public bool IsStringLiteral() => (flags & TypeFlags.StringLiteral) != 0;
    public bool IsNumberLiteral() => (flags & TypeFlags.NumberLiteral) != 0;
    public bool IsBigIntLiteral() => (flags & TypeFlags.BigIntLiteral) != 0;
    public bool IsEnumLiteral() => (flags & TypeFlags.EnumLiteral) != 0;
    public bool IsBooleanLike() => (flags & TypeFlags.BooleanLike) != 0;
    public bool IsStringLike() => (flags & TypeFlags.StringLike) != 0;
    public bool IsClass() => (objectFlags & TypeScript.Checking.ObjectFlags.Class) != 0;
    public bool IsTypeParameter() => (flags & TypeFlags.TypeParameter) != 0;
    public bool IsIndex() => (flags & TypeFlags.Index) != 0;
    public bool IsTupleType() => (objectFlags & TypeScript.Checking.ObjectFlags.Reference) != 0 && (Target().objectFlags & TypeScript.Checking.ObjectFlags.Tuple) != 0;
}

// Go's TypeData interface/embedded TypeBase become a payload with one stable wrapper.
public abstract class TypeData
{
    private readonly Type type;
    protected TypeData() => type = new(this);
    public Type AsType() => type;
    public virtual ConstrainedType AsConstrainedType() => null;
    public virtual StructuredType AsStructuredType() => null;
    public virtual UnionOrIntersectionType AsUnionOrIntersectionType() => null;
    public virtual ObjectType AsObjectType() => null;
    public virtual TypeReference AsTypeReference() => null;
    public virtual InterfaceType AsInterfaceType() => null;
}

public class TypeBase : TypeData
{
}

public sealed class IntrinsicType : TypeBase
{
    internal string intrinsicName = "";
    public string IntrinsicName() => intrinsicName;
}

public sealed class LiteralType : TypeBase
{
    internal object value;
    internal Type freshType, regularType;
    public object Value() => value;
    public Type FreshType() => freshType;
    public Type RegularType() => regularType;
    // String() belongs to the selected ValueToString/escaping dependency closure.
}

// types.go:920. Internal const Symbol()/Symbol.for() results retain their actual
// owning symbol and opaque raw name; written unique-symbol syntax stays excluded.
public sealed class UniqueESSymbolType : TypeBase
{
    internal string name = "";
}

public class ConstrainedType : TypeBase
{
    internal Type resolvedBaseConstraint;
    public override ConstrainedType AsConstrainedType() => this;
}

public class StructuredType : ConstrainedType
{
    internal SymbolTable members;
    internal List<Symbol> properties;
    internal List<Signature> signatures;
    internal int callSignatureCount;
    internal List<IndexInfo> indexInfos;
    internal Type objectTypeWithoutAbstractConstructSignatures;
    public override StructuredType AsStructuredType() => this;
    // types.go:949/953. Preserve ordered call/construct slices and their nil state.
    public List<Signature> CallSignatures() => signatures?.GetRange(0, callSignatureCount);
    public List<Signature> ConstructSignatures() => signatures?.GetRange(callSignatureCount, signatures.Count - callSignatureCount);
    public List<Symbol> Properties() => properties;
}

public class ObjectType : StructuredType
{
    internal Type target;
    internal TypeMapper mapper;
    // The checker key preserves the original complete key preimage.
    internal Dictionary<CacheHashKey, Type> instantiations;
    public override ObjectType AsObjectType() => this;
}

// types.go:1005/1015. Nongeneric interfaces also embed the reference payload;
// alias-based heritage can create a real implicit-this parameter/reference.
// types.go:1103. The source expression is retained across type instantiation.
public sealed class InstantiationExpressionType : ObjectType
{
    internal Node node;
}

public class TypeReference : ObjectType
{
    internal Node node;
    internal List<Type> resolvedTypeArguments;
    public override TypeReference AsTypeReference() => this;
}

public class InterfaceType : TypeReference
{
    internal List<Type> allTypeParameters;
    internal int outerTypeParameterCount;
    internal Type thisType;
    internal bool baseTypesResolved, declaredMembersResolved;
    internal Type resolvedBaseConstructorType;
    internal List<Type> resolvedBaseTypes;
    internal SymbolTable declaredMembers;
    internal List<Signature> declaredCallSignatures, declaredConstructSignatures;
    internal List<IndexInfo> declaredIndexInfos;
    public override InterfaceType AsInterfaceType() => this;
    public List<Type> OuterTypeParameters() => allTypeParameters is not { Count: > 0 } ? null : allTypeParameters.GetRange(0, outerTypeParameterCount);
    public List<Type> LocalTypeParameters() => allTypeParameters is not { Count: > 0 } ? null : allTypeParameters.GetRange(outerTypeParameterCount, allTypeParameters.Count - outerTypeParameterCount - 1);
    public List<Type> TypeParameters() => allTypeParameters is not { Count: > 0 } ? null : allTypeParameters.GetRange(0, allTypeParameters.Count - 1);
    public Type ThisType() => thisType;
}

// types.go:1187. Declared and synthetic parameters retain their constraint,
// target/mapper, default and distribution resolution caches.
public sealed class TypeParameter : ConstrainedType
{
    internal Type constraint, target;
    internal TypeMapper mapper;
    internal bool isThisType, isDistributed;
    internal Type resolvedDefaultType, distributedType;
    public bool IsThisType() => isThisType;
}

// types.go:1213. Deferred keyof target and index flags.
public sealed class IndexType : ConstrainedType
{
    internal Type target;
    internal IndexFlags indexFlags;
    public Type Target() => target;
}

// types.go:1059/1073/1081. A tuple target is a real synthesized generic interface.
[Flags]
public enum ElementFlags : uint
{
    None = 0,
    Required = 1 << 0,
    Optional = 1 << 1,
    Rest = 1 << 2,
    Variadic = 1 << 3,
    Fixed = Required | Optional,
    Variable = Rest | Variadic,
    NonRequired = Optional | Rest | Variadic,
    NonRest = Required | Optional | Variadic
}

public struct TupleElementInfo
{
    internal ElementFlags flags;
    internal Node labeledDeclaration;
    public ElementFlags TupleElementFlags() => flags;
    public Node LabeledDeclaration() => labeledDeclaration;
}

public sealed class TupleType : InterfaceType
{
    internal List<TupleElementInfo> elementInfos;
    internal int minLength, fixedLength;
    internal ElementFlags combinedFlags;
    internal bool @readonly;
    public int FixedLength() => fixedLength;
    public bool IsReadonly() => @readonly;
    public List<ElementFlags> ElementFlags() => elementInfos.Select(info => info.flags).ToList();
    public List<TupleElementInfo> ElementInfos() => elementInfos;
}

// types.go:1110/1135/1144. Nullable lazy slots are distinct from resolved values.
public sealed class MappedType : ObjectType
{
    internal MappedTypeNode declaration;
    internal Type typeParameter, constraintType, nameType, templateType, modifiersType, resolvedApparentType;
    internal bool containsError;
    public Type TypeParameter() => typeParameter;
    public Type ConstraintType() => constraintType;
    public Type NameType() => nameType;
    public Type TemplateType() => templateType;
}

public sealed class ReverseMappedType : ObjectType
{
    internal Type source, mappedType, constraintType;
}

public sealed class EvolvingArrayType : ObjectType
{
    internal Type elementType, finalArrayType;
}

// types.go:1223. accessFlags stores only AccessFlags.Persistent.
public sealed class IndexedAccessType : ConstrainedType
{
    internal Type objectType, indexType;
    internal AccessFlags accessFlags;
    public Type ObjectType() => objectType;
    public Type IndexType() => indexType;
}

// types.go:1249. A substitution retains the constraint known to hold for its base.
public sealed class SubstitutionType : ConstrainedType
{
    internal Type baseType, constraint;
    public Type BaseType() => baseType;
    public Type SubstConstraint() => constraint;
}

// types.go:1258/1269. Roots are shared by conditional instantiations.
public sealed class ConditionalRoot
{
    internal ConditionalTypeNode node;
    internal Type checkType, extendsType;
    internal bool isDistributive;
    internal List<Type> inferTypeParameters, outerTypeParameters;
    internal Dictionary<CacheHashKey, Type> instantiations;
    internal TypeAlias alias;
}

public sealed class ConditionalType : ConstrainedType
{
    internal ConditionalRoot root;
    internal Type checkType, extendsType, resolvedTrueType, resolvedFalseType, resolvedInferredTrueType, resolvedDefaultConstraint, resolvedConstraintOfDistributive;
    internal TypeMapper mapper, combinedMapper;
    public Type CheckType() => checkType;
    public Type ExtendsType() => extendsType;
}

public class UnionOrIntersectionType : StructuredType
{
    internal List<Type> types;
    internal SymbolTable propertyCache, propertyCacheWithoutFunctionPropertyAugment;
    internal List<Symbol> resolvedProperties;
    public override UnionOrIntersectionType AsUnionOrIntersectionType() => this;
    public List<Type> Types() => types;
}

public sealed class UnionType : UnionOrIntersectionType
{
    internal Type resolvedReducedType, regularType, origin;
    internal string keyPropertyName = "";
    internal Dictionary<Type, Type> constituentMap;
}

// types.go:1179. Real intersection reductions and apparent-type cache.
public sealed class IntersectionType : UnionOrIntersectionType
{
    internal Type resolvedApparentType, uniqueLiteralFilledInstantiation;
}

// types.go:245/254. Alias and interface declaration resolution caches.
internal sealed class TypeAliasLinks
{
    internal Type declaredType;
    internal List<Type> typeParameters;
    internal Dictionary<CacheHashKey, Type> instantiations;
    internal bool isConstructorDeclaredProperty;
}

internal sealed class DeclaredTypeLinks
{
    internal Type declaredType;
    internal bool interfaceChecked, indexSignaturesChecked, typeParametersChecked, enumChecked;
}

// types.go:200. Deferred synthetic properties preserve their constituent graph.
internal sealed class DeferredSymbolLinks
{
    internal Type parent;
    internal List<Type> constituents, writeConstituents;
}

// Referenced-symbol meanings remain distinct from type-alias resolution links.
internal sealed class SymbolReferenceLinks
{
    internal SymbolFlags referenceKinds;
}

internal sealed class ValueSymbolLinks
{
    internal bool functionOrConstructorChecked;
    internal Type resolvedType, writeType;
    internal Symbol target;
    internal TypeMapper mapper;
    internal Type nameType, containingType;
}

// types.go:194. Additional links retain mapped/spread property provenance.
internal sealed class MappedSymbolLinks
{
    internal Type keyType;
    internal Symbol syntheticOrigin;
}

// types.go:224. Reverse inference preserves each property's original input type.
internal sealed class ReverseMappedSymbolLinks
{
    internal Type propertyType, mappedType, constraintType;
}

// types.go:281. Spread indices are lazily computed for an array literal.
// types.go:300; source properties contributing to a synthetic spread member.
internal sealed class SpreadLinks
{
    internal Symbol leftSpread;
    internal Symbol rightSpread;
}

internal enum ExhaustiveState : byte
{
    Unknown,
    Computing,
    False,
    True
}

internal sealed class SwitchStatementLinks
{
    internal ExhaustiveState exhaustiveState;
    internal bool switchTypesComputed, witnessesComputed;
    internal List<Type> switchTypes;
    internal List<string> witnesses;
}

internal sealed class ArrayLiteralLinks
{
    internal bool indicesComputed;
    internal int firstSpreadIndex, lastSpreadIndex;
}

// types.go:390. Nullable bool distinguishes an uncomputed name from no name.
internal sealed class ComputedNameNodeLinks
{
    internal bool? hasName;
    internal string name;
}

// types.go:209. Actual import/export alias links, including pure alias cycles.
internal sealed class AliasSymbolLinks
{
    internal Symbol immediateTarget;
    internal Symbol aliasTarget;
    internal bool referenced;
    internal Node typeOnlyDeclaration;
}

// types.go:218. Source module export resolution and type-only export-star state.
internal sealed class ModuleSymbolLinks
{
    internal SymbolTable resolvedExports;
    internal Dictionary<string, Node> typeOnlyExportStarMap;
    internal bool exportsChecked = false;
}

// types.go:290/296. Tables remain nullable independently and retain identity.
internal enum MembersOrExportsResolutionKind
{
    ResolvedExports = 0,
    ResolvedMembers = 1,
}

internal sealed class MembersAndExportsLinks
{
    private readonly SymbolTable[] tables = new SymbolTable[2];
    internal SymbolTable this[MembersOrExportsResolutionKind kind] { get => tables[(int)kind]; set => tables[(int)kind] = value; }
}

// types.go:1288. Call-chain flags are deliberately absent from PropagatingFlags.
[Flags]
public enum SignatureFlags : uint
{
    None = 0,
    HasRestParameter = 1 << 0,
    HasLiteralTypes = 1 << 1,
    Construct = 1 << 2,
    Abstract = 1 << 3,
    IsInnerCallChain = 1 << 4,
    IsOuterCallChain = 1 << 5,
    IsUntypedSignatureInJSFile = 1 << 6,
    IsNonInferrable = 1 << 7,
    IsSignatureCandidateForOverloadFailure = 1 << 8,
    PropagatingFlags = HasRestParameter | HasLiteralTypes | Construct | Abstract | IsUntypedSignatureInJSFile | IsSignatureCandidateForOverloadFailure,
    CallChainFlags = IsInnerCallChain | IsOuterCallChain,
}

// Complete types.go:1312 payload. Nullable references/slices preserve cache state.
public sealed class Signature
{
    internal uint id;
    internal SignatureFlags flags;
    internal int minArgumentCount, resolvedMinArgumentCount;
    internal Node declaration;
    internal List<Type> typeParameters;
    internal List<Symbol> parameters;
    internal Symbol thisParameter;
    internal Type resolvedReturnType, isolatedSignatureType;
    internal TypePredicate resolvedTypePredicate;
    internal Signature target;
    internal TypeMapper mapper;
    internal CompositeSignature composite;
    public uint Id() => id;
    public SignatureFlags Flags() => flags;
    public List<Type> TypeParameters() => typeParameters;
    public Node Declaration() => declaration;
    public Signature Target() => target;
    public Symbol ThisParameter() => thisParameter;
    public List<Symbol> Parameters() => parameters;
    public bool HasRestParameter() => (flags & SignatureFlags.HasRestParameter) != 0;
    public int MinArgumentCount() => minArgumentCount;
}

// types.go:1365. Constituent order is retained; target and composite are distinct.
internal sealed class CompositeSignature
{
    internal bool isUnion;
    internal List<Signature> signatures;
}

public enum TypePredicateKind : int
{
    This,
    Identifier,
    AssertsThis,
    AssertsIdentifier,
}

// types.go:1379. ParameterName is a Go-compatible raw byte string.
public sealed class TypePredicate
{
    internal TypePredicateKind kind;
    internal int parameterIndex;
    internal string parameterName;
    internal Type t;
    public Type Type() => t;
    public TypePredicateKind Kind() => kind;
    public int ParameterIndex() => parameterIndex;
    public string ParameterName() => parameterName;
}

// types.go:425. The owning store uses logical Node identity, not paged presence.
internal sealed class SignatureLinks
{
    internal Signature resolvedSignature, effectsSignature, decoratorSignature;
}

// types.go:1404. Real index payload/storage, including synthetic any-base indexes.
public sealed class IndexInfo
{
    internal Type keyType, valueType;
    internal bool isReadonly;
    internal Node declaration;
    internal Symbol indexSymbol;
    internal List<Node> components;
    public Type KeyType() => keyType;
    public Type ValueType() => valueType;
    public bool IsReadonly() => isReadonly;
    public Node Declaration() => declaration;
}

// types.go:1444. Bitwise combination preserves False < Unknown < Maybe < True.
public enum Ternary : sbyte
{
    False = 0,
    Unknown = 1,
    Maybe = 3,
    True = -1,
}

internal sealed class NodeLinks
{
    internal NodeCheckFlags flags;
    internal Tristate declarationRequiresScopeChange;
    internal bool hasReportedStatementInAmbientContext;
}

internal sealed class SymbolNodeLinks
{
    internal Symbol resolvedSymbol;
}

internal sealed class TypeNodeLinks
{
    internal Type resolvedType;
    internal List<Type> outerTypeParameters;
}

// Upstream checker/types.go:325; assignment-marking cache for admitted declarations.
internal sealed class MarkedAssignmentSymbolLinks
{
    internal int lastAssignmentPos;
    internal bool hasDefiniteAssignment;
}

// types.go:409. Source-file checking, unused, helper and deferred-node state.
internal sealed class SourceFileLinks
{
    internal bool typeChecked;
    internal bool unusedChecked = false;
    internal Symbol externalHelpersModule = null;
    internal ExternalEmitHelpers requestedExternalEmitHelpers = 0;
    internal GoOrderedNodeSet deferredNodes = new();
    internal List<Node> identifierCheckNodes = null;
    internal string localJsxNamespace = "", localJsxFragmentNamespace = "";
    internal Node localJsxFactory = null, localJsxFragmentFactory = null;
    internal Type jsxFragmentType = null;
}

// Language adapter for collections.OrderedSet[*ast.Node]: insertion order and
// reference identity, including duplicate Add suppression, remain observable.
internal sealed class GoOrderedNodeSet
{
    private readonly HashSet<Node> set = new(ReferenceEqualityComparer.Instance);
    private readonly List<Node> values = new();
    internal void Add(Node node)
    {
        if (set.Add(node))
            values.Add(node);
    }

    // collections/ordered_set.go:23,45; membership and size of the selected
    // ordered identity set, without exposing managed backing topology.
    internal bool Has(Node node) => set.Contains(node);
    internal int Size() => values.Count;
    internal IEnumerable<Node> Values()
    {
        // Go's iterator walks the live ordered slice, including later additions.
        for (int i = 0; i < values.Count; i++)
            yield return values[i];
    }
}

// types.go:342,1202; exact flags for retained property-key access helpers.
[Flags]
internal enum AccessFlags : uint
{
    None = 0,
    IncludeUndefined = 1 << 0,
    NoIndexSignatures = 1 << 1,
    Writing = 1 << 2,
    CacheSymbol = 1 << 3,
    AllowMissing = 1 << 4,
    ExpressionPosition = 1 << 5,
    ReportDeprecated = 1 << 6,
    SuppressNoImplicitAnyError = 1 << 7,
    Contextual = 1 << 8,
    Persistent = IncludeUndefined
}

[Flags]
internal enum IndexFlags : uint
{
    None = 0,
    StringsOnly = 1 << 0,
    NoIndexSignatures = 1 << 1,
    NoReducibleCheck = 1 << 2
}

// types.go:29. Call and construct signature categories.
public enum SignatureKind
{
    Call,
    Construct
}

// types.go:307/311. Variance links for nongeneric reference targets; hidden
// this arguments are compared covariantly without a declared parameter entry.
internal sealed class VarianceLinks
{
    internal List<VarianceFlags> variances;
}

[Flags]
internal enum VarianceFlags : uint
{
    Invariant = 0,
    Covariant = 1 << 0,
    Contravariant = 1 << 1,
    Bivariant = Covariant | Contravariant,
    Independent = 1 << 2,
    VarianceMask = Invariant | Covariant | Contravariant | Independent,
    Unmeasurable = 1 << 3,
    Unreliable = 1 << 4,
    AllowsStructuralFallback = Unmeasurable | Unreliable,
}

// types.go:330. Scope/meaning-sensitive accessibility cache, including cached nil.
internal readonly record struct accessibleChainCacheKey(bool useOnlyExternalAliasing, Node location, SymbolFlags meaning);
internal sealed class ContainingSymbolLinks
{
    internal Dictionary<ulong, List<Symbol>> extendedContainersByFile;
    // A nonnull cell whose Value is null preserves Go pointer-to-nil-slice state.
    internal LinkCell<List<Symbol>> extendedContainers;
    internal Dictionary<accessibleChainCacheKey, List<Symbol>> accessibleChainCache;
}

// types.go:1444. Actual relation callback; reportErrors controls diagnostic elaboration.
internal delegate Ternary TypeComparer(Type source, Type target, bool reportErrors);
// types.go:51. Contextual signature acquisition flags.
[Flags]
internal enum ContextFlags : uint
{
    None = 0,
    Signature = 1,
    NoConstraints = 2,
    IgnoreNodeInferences = 4,
    SkipBindingPatterns = 8
}

// types.go:128. Metadata flags only; general emit helper execution is excluded.
[Flags]
internal enum ExternalEmitHelpers : uint
{
    Rest = 1u << 0, // __rest (used by ESNext object rest transformation)
    Decorate = 1u << 1, // __decorate (used by TypeScript decorators transformation)
    Metadata = 1u << 2, // __metadata (used by TypeScript decorators transformation)
    Param = 1u << 3, // __param (used by TypeScript decorators transformation)
    Awaiter = 1u << 4, // __awaiter (used by ES2017 async functions transformation)
    Await = 1u << 5, // __await (used by ES2017 async generator transformation)
    AsyncGenerator = 1u << 6, // __asyncGenerator (used by ES2017 async generator transformation)
    AsyncDelegator = 1u << 7, // __asyncDelegator (used by ES2017 async generator yield* transformation)
    AsyncValues = 1u << 8, // __asyncValues (used by ES2017 for..await..of transformation)
    ExportStar = 1u << 9, // __exportStar (used by CommonJS/AMD/UMD module transformation)
    ImportStar = 1u << 10, // __importStar (used by CommonJS/AMD/UMD module transformation)
    ImportDefault = 1u << 11, // __importDefault (used by CommonJS/AMD/UMD module transformation)
    MakeTemplateObject = 1u << 12, // __makeTemplateObject (used for constructing template string array objects)
    ClassPrivateFieldGet = 1u << 13, // __classPrivateFieldGet (used by the class private field transformation)
    ClassPrivateFieldSet = 1u << 14, // __classPrivateFieldSet (used by the class private field transformation)
    ClassPrivateFieldIn = 1u << 15, // __classPrivateFieldIn (used by the class private field transformation)
    SetFunctionName = 1u << 16, // __setFunctionName (used by class fields and ECMAScript decorators)
    PropKey = 1u << 17, // __propKey (used by class fields and ECMAScript decorators)
    AddDisposableResourceAndDisposeResources = 1u << 18, // __addDisposableResource and __disposeResources (used by ESNext transformations)
    RewriteRelativeImportExtension = 1u << 19, // __rewriteRelativeImportExtension (used by --rewriteRelativeImportExtensions)
    ESDecorateAndRunInitializers = Decorate, // __esDecorate and __runInitializers (used by ECMAScript decorators transformation)
    FirstEmitHelper = Rest,
    LastEmitHelper = RewriteRelativeImportExtension,
    ForAwaitOfIncludes = AsyncValues,
    AsyncGeneratorIncludes = Await | AsyncGenerator,
    AsyncDelegatorIncludes = Await | AsyncDelegator | AsyncValues,
}

// types.go:1233. Template literal text segments and placeholder types.
public sealed class TemplateLiteralType : ConstrainedType
{
    internal List<string> texts;
    internal List<Type> types;
    public List<string> Texts() => texts;
    public List<Type> Types() => types;
}

// types.go:1242. Intrinsic string mappings retain the actual constrained target.
public sealed class StringMappingType : ConstrainedType
{
    internal Type target;
    public Type Target() => target;
}

internal sealed class EnumMemberLinks
{
    internal Evaluator.Result value;
}

internal sealed class LateBoundLinks
{
    internal Symbol lateSymbol;
}

// types.go:43. Source override check outcome.
internal enum MemberOverrideStatus { None, NeedsOverride, HasInvalidOverride }

// checker/types.go:379. Type assertions retain the original expression type for deferred comparability.
internal sealed class AssertionNodeLinks
{
    internal Type exprType;
}

// checker/types.go:1448–1492. Original language-feature target table.
internal static class LanguageFeatureMinimumTarget
{
    internal const ScriptTarget Exponentiation = ScriptTarget.ES2016;
    internal const ScriptTarget AsyncFunctions = ScriptTarget.ES2017;
    internal const ScriptTarget ForAwaitOf = ScriptTarget.ES2018;
    internal const ScriptTarget AsyncGenerators = ScriptTarget.ES2018;
    internal const ScriptTarget AsyncIteration = ScriptTarget.ES2018;
    internal const ScriptTarget ObjectSpreadRest = ScriptTarget.ES2018;
    internal const ScriptTarget RegularExpressionFlagsDotAll = ScriptTarget.ES2018;
    internal const ScriptTarget BindinglessCatch = ScriptTarget.ES2019;
    internal const ScriptTarget BigInt = ScriptTarget.ES2020;
    internal const ScriptTarget NullishCoalesce = ScriptTarget.ES2020;
    internal const ScriptTarget OptionalChaining = ScriptTarget.ES2020;
    internal const ScriptTarget LogicalAssignment = ScriptTarget.ES2021;
    internal const ScriptTarget TopLevelAwait = ScriptTarget.ES2022;
    internal const ScriptTarget ClassFields = ScriptTarget.ES2022;
    internal const ScriptTarget PrivateNamesAndClassStaticBlocks = ScriptTarget.ES2022;
    internal const ScriptTarget RegularExpressionFlagsHasIndices = ScriptTarget.ES2022;
    internal const ScriptTarget ShebangComments = ScriptTarget.ESNext;
    internal const ScriptTarget UsingAndAwaitUsing = ScriptTarget.ESNext;
    internal const ScriptTarget ClassAndClassElementDecorators = ScriptTarget.ESNext;
    internal const ScriptTarget RegularExpressionFlagsUnicodeSets = ScriptTarget.ESNext;
}

// types.go:102. Original symbol serialization context flags.
[Flags]
public enum SymbolFormatFlags : uint
{
    None = 0,
    WriteTypeParametersOrArguments = 1 << 0,
    UseOnlyExternalAliasing = 1 << 1,
    AllowAnyNodeKind = 1 << 2,
    UseAliasDefinedOutsideCurrentScope = 1 << 3,
    WriteComputedProps = 1 << 4,
    DoNotIncludeSymbolChain = 1 << 5
}
