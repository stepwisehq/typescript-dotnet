// Authored independent read-only project relationship fields; no resolver/getter work.
package checker

import (
	"github.com/microsoft/TypeScript/tsc/internal/ast"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"reflect"
	"unsafe"
)

type OracleProjectAlias struct {
	Present             bool
	ImmediateTarget     *ast.Symbol
	AliasTarget         *ast.Symbol
	Referenced          bool
	TypeOnlyDeclaration *ast.Node
}

func OracleProjectAliasState(c *Checker, s *ast.Symbol) OracleProjectAlias {
	x := c.aliasSymbolLinks.TryGet(s)
	if x == nil {
		return OracleProjectAlias{}
	}
	return OracleProjectAlias{true, x.immediateTarget, x.aliasTarget, x.referenced, x.typeOnlyDeclaration}
}

type OracleProjectModule struct {
	Present       bool
	Exports       ast.SymbolTable
	TypeOnlyStars map[string]*ast.Node
	Checked       bool
}

func OracleProjectModuleState(c *Checker, s *ast.Symbol) OracleProjectModule {
	x := c.moduleSymbolLinks.TryGet(s)
	if x == nil {
		return OracleProjectModule{}
	}
	return OracleProjectModule{true, x.resolvedExports, x.typeOnlyExportStarMap, x.exportsChecked}
}
func oracleProjectKeys[V any](s *core.LinkStore[*ast.Symbol, V]) []*ast.Symbol {
	v := reflect.ValueOf(s).Elem().FieldByName("entries")
	entries := *(*map[*ast.Symbol]*V)(unsafe.Pointer(v.UnsafeAddr()))
	if entries == nil {
		return nil
	}
	out := []*ast.Symbol{}
	for k := range entries {
		out = append(out, k)
	}
	return out
}

type OracleProjectStore struct {
	Name string
	Keys []*ast.Symbol
}

func OracleProjectStoreKeys(c *Checker) []OracleProjectStore {
	alias := oracleProjectKeys(&c.aliasSymbolLinks)
	modules := oracleProjectKeys(&c.moduleSymbolLinks)
	return []OracleProjectStore{{"aliasSymbolLinks", alias}, {"moduleSymbolLinks", modules}, {"exportTypeLinks", oracleProjectKeys(&c.exportTypeLinks)}}
}

type OracleProjectTypeFields struct {
	Target                  *Type
	Arguments, Constituents []*Type
	Members                 ast.SymbolTable
	Properties              []*ast.Symbol
	Signatures              []*Signature
	CallSignatureCount      int
	Constraint, Default     *Type
}

func OracleProjectTypeState(t *Type) OracleProjectTypeFields {
	out := OracleProjectTypeFields{}
	if x := t.AsStructuredType(); x != nil {
		out.Members = x.members
		out.Properties = x.properties
		out.Signatures = x.signatures
		out.CallSignatureCount = x.callSignatureCount
	}
	if x := t.AsObjectType(); x != nil {
		out.Target = x.target
	}
	if x := t.AsTypeReference(); x != nil {
		out.Arguments = x.resolvedTypeArguments
	}
	if t.flags&TypeFlagsUnionOrIntersection != 0 {
		out.Constituents = t.AsUnionOrIntersectionType().types
	}
	if t.flags&TypeFlagsTypeParameter != 0 {
		x := t.AsTypeParameter()
		out.Target = x.target
		out.Constraint = x.constraint
		out.Default = x.resolvedDefaultType
	}
	if t.flags&TypeFlagsIndex != 0 {
		x := t.AsIndexType()
		out.Target = x.target
	}
	return out
}

type OracleProjectSignatureFields struct{ ResolvedMinArgumentCount int }

func OracleProjectSignatureState(s *Signature) OracleProjectSignatureFields {
	return OracleProjectSignatureFields{int(s.resolvedMinArgumentCount)}
}

func OracleProjectPayloadName(c *Checker, t *Type) string {
	if t.checker != nil && t.checker != c {
		panic("Foreign project type owner")
	}
	return reflect.TypeOf(t.data).Elem().Name()
}

func OracleProjectGlobals(c *Checker) ast.SymbolTable { return c.globals }

type OracleProjectExportType struct {
	Present           bool
	Target            *ast.Symbol
	OriginatingImport *ast.Node
}

func OracleProjectExportTypeState(c *Checker, s *ast.Symbol) OracleProjectExportType {
	x := c.exportTypeLinks.TryGet(s)
	if x == nil {
		return OracleProjectExportType{}
	}
	return OracleProjectExportType{true, x.target, x.originatingImport}
}
