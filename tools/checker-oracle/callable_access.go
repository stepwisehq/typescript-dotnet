// Independent read-only observations of actual upstream callable storage.
package checker

import "github.com/microsoft/TypeScript/tsc/internal/ast"

type OracleCallableCachedSignature struct {
	Name  string
	Value *Signature
}

func OracleObserveCallableSignatureLinks(c *Checker, n *ast.Node) *SignatureLinks {
	return c.signatureLinks.TryGet(n)
}
func OracleObserveCallableCachedSignaturesNil(c *Checker) bool { return c.cachedSignatures == nil }
func OracleObserveCallableCachedSignatures(c *Checker, s *Signature) []OracleCallableCachedSignature {
	out := []OracleCallableCachedSignature{}
	keys := []struct {
		Name string
		Key  CacheHashKey
	}{{"base", SignatureKeyBase}, {"canonical", SignatureKeyCanonical}, {"erased", SignatureKeyErased}, {"inner", SignatureKeyInner}, {"outer", SignatureKeyOuter}}
	for _, k := range keys {
		if value, ok := c.cachedSignatures[CachedSignatureKey{s, k.Key}]; ok {
			out = append(out, OracleCallableCachedSignature{k.Name, value})
		}
	}
	count := 0
	for key := range c.cachedSignatures {
		if key.sig == s {
			count++
		}
	}
	if count != len(out) {
		panic("Callable cached signature uses unobserved type-list key family")
	}
	return out
}

func OracleObserveCallableNoTypePredicate(c *Checker) *TypePredicate { return c.noTypePredicate }

func OracleObserveCallableSourceFileLinks(c *Checker, file *ast.SourceFile) *SourceFileLinks {
	return c.sourceFileLinks.TryGet(file)
}
func OracleObserveCallableDeferredNodes(links *SourceFileLinks) []*ast.Node {
	nodes := []*ast.Node{}
	for node := range links.deferredNodes.Values() {
		nodes = append(nodes, node)
	}
	return nodes
}

func OracleObserveCallableCachedSignatureCount(c *Checker) int { return len(c.cachedSignatures) }

func OracleObserveCallableUniqueSymbolCount(c *Checker) int { return len(c.uniqueESSymbolTypes) }
func OracleObserveCallableUniqueSymbolMembership(c *Checker, value *Type) bool {
	return value.symbol != nil && c.uniqueESSymbolTypes[value.symbol] == value
}
