// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Partial counterpart of tsc/internal/checker/types.go; generated enum slice.
using TypeScript.Scanner;

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

// BEGIN MANUAL SCALAR TYPE MODEL
// Partial counterpart: unsupported general signatures, mappers, object/type-parameter
// models and corresponding casts are excluded. Selected fields retain Go identity.
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
    public UnionType AsUnionType() => (UnionType)data;
    public ConstrainedType AsConstrainedType() => data.AsConstrainedType();
    public StructuredType AsStructuredType() => data.AsStructuredType();
    public UnionOrIntersectionType AsUnionOrIntersectionType() => data.AsUnionOrIntersectionType();
    public ObjectType AsObjectType() => data.AsObjectType();
    // Upstream checker/types.go:740.
    public List<Type> Distributed()
    {
        if ((flags & TypeFlags.Union) != 0)
            return AsUnionType().types;
        if ((flags & TypeFlags.Never) != 0)
            return null;
        return[this ];
    }

    // Upstream checker/types.go:780; only the selected union payload is constructed.
    public List<Type> Types()
    {
        if ((flags & TypeFlags.UnionOrIntersection) != 0)
            return AsUnionOrIntersectionType().types;
        throw new NotSupportedException("Type.Types requires an unsupported type payload");
    }

    // Authored AOT-safe payload identity for the independent semantic graph.
    public string DataType() => data switch
    {
        IntrinsicType => "IntrinsicType",
        LiteralType => "LiteralType",
        UnionType => "UnionType",
        ObjectType => "ObjectType",
        _ => throw new NotSupportedException("Unselected type payload")};
    public bool IsUnion() => (flags & TypeFlags.Union) != 0;
    public bool IsString() => (flags & TypeFlags.String) != 0;
    public bool IsIntersection() => (flags & TypeFlags.Intersection) != 0;
    public bool IsStringLiteral() => (flags & TypeFlags.StringLiteral) != 0;
    public bool IsNumberLiteral() => (flags & TypeFlags.NumberLiteral) != 0;
    public bool IsBigIntLiteral() => (flags & TypeFlags.BigIntLiteral) != 0;
    public bool IsEnumLiteral() => (flags & TypeFlags.EnumLiteral) != 0;
    public bool IsBooleanLike() => (flags & TypeFlags.BooleanLike) != 0;
    public bool IsStringLike() => (flags & TypeFlags.StringLike) != 0;
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

public class ConstrainedType : TypeBase
{
    internal Type resolvedBaseConstraint;
    public override ConstrainedType AsConstrainedType() => this;
}

public class StructuredType : ConstrainedType
{
    internal SymbolTable members;
    internal List<Symbol> properties;
    internal Type objectTypeWithoutAbstractConstructSignatures;
    // signatures/callSignatureCount/indexInfos excluded from scalar initialization.
    public override StructuredType AsStructuredType() => this;
    public List<Symbol> Properties() => properties;
}

public sealed class ObjectType : StructuredType
{
    internal Type target;
    // Generic mapper/instantiation state excluded; only upstream initialization markers.
    public override ObjectType AsObjectType() => this;
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

// Selected fields only: mapper/contextual/signature caches remain excluded.
internal sealed class SymbolReferenceLinks
{
    internal SymbolFlags referenceKinds;
}

internal sealed class ValueSymbolLinks
{
    internal Type resolvedType, writeType;
    internal Symbol target;
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
