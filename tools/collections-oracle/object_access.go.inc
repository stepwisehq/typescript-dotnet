// Authored provenance of original fresh-literal and regularization producers.
package checker

import "github.com/microsoft/TypeScript/tsc/internal/ast"

type OracleCollectionsApplicabilityOrigin struct {
	Phase        string
	Node         *ast.Node
	Signature    *Signature
	Relation     string
	Mode         uint32
	ReportErrors *bool
}

var oracleCollectionsApplicabilityOrigins = map[*Checker]*OracleCollectionsApplicabilityOrigin{}

func oracleCollectionsEnterApplicability(c *Checker, node *ast.Node, signature *Signature, relation *Relation, mode CheckMode, reportErrors bool) func() {
	return oracleCollectionsEnterCheck(c, "applicability", node, signature, relation, mode, &reportErrors)
}
func oracleCollectionsEnterOverload(c *Checker, node *ast.Node, relation *Relation, mode CheckMode) func() {
	return oracleCollectionsEnterCheck(c, "choose-overload", node, nil, relation, mode, nil)
}
func oracleCollectionsEnterCheck(c *Checker, phase string, node *ast.Node, signature *Signature, relation *Relation, mode CheckMode, reportErrors *bool) func() {
	category := "unknown"
	switch relation {
	case c.assignableRelation:
		category = "assignable"
	case c.comparableRelation:
		category = "comparable"
	case c.identityRelation:
		category = "identity"
	case c.subtypeRelation:
		category = "subtype"
	case c.strictSubtypeRelation:
		category = "strict-subtype"
	}
	previous := oracleCollectionsApplicabilityOrigins[c]
	oracleCollectionsApplicabilityOrigins[c] = &OracleCollectionsApplicabilityOrigin{phase, node, signature, category, uint32(mode), reportErrors}
	return func() {
		if previous == nil {
			delete(oracleCollectionsApplicabilityOrigins, c)
		} else {
			oracleCollectionsApplicabilityOrigins[c] = previous
		}
	}
}

type OracleCollectionsObjectOrigin struct {
	Node           *ast.Node
	Mode           uint32
	ContextualType *Type
	Source         *Type
	Applicability  *OracleCollectionsApplicabilityOrigin
}

var oracleCollectionsObjectOrigins = map[*Type]OracleCollectionsObjectOrigin{}

func oracleCollectionsCaptureObject(c *Checker, result *Type, node *ast.Node, mode CheckMode, contextualType *Type) {
	oracleCollectionsObjectOrigins[result] = OracleCollectionsObjectOrigin{node, uint32(mode), contextualType, nil, oracleCollectionsApplicabilityOrigins[c]}
}
func oracleCollectionsCaptureRegularObject(c *Checker, result, source *Type) {
	if origin, ok := oracleCollectionsObjectOrigins[source]; ok {
		origin.Source = source
		oracleCollectionsObjectOrigins[result] = origin
	}
}
func OracleCollectionsObjectProducer(c *Checker, t *Type) (OracleCollectionsObjectOrigin, bool) {
	if t.checker != nil && t.checker != c {
		panic("Foreign literal producer owner")
	}
	origin, ok := oracleCollectionsObjectOrigins[t]
	if origin.Applicability != nil && origin.Applicability.Relation == "unknown" {
		panic("Unknown literal applicability relation")
	}
	if frame := origin.Applicability; frame != nil {
		if frame.Phase == "applicability" {
			if frame.Signature == nil || frame.ReportErrors == nil {
				panic("Invalid literal applicability inputs")
			}
		} else if frame.Phase != "choose-overload" || frame.Signature != nil || frame.ReportErrors != nil {
			panic("Invalid literal overload inputs")
		}
	}
	return origin, ok
}
