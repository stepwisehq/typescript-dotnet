// Failure diagnostics only; never canonical ordering or successful output.
package checker

import (
	"fmt"
	"sort"
	"strings"
)

func OracleCollectionsDescriptorMembers(c *Checker, t *Type) string {
	if t == nil || t.AsStructuredType() == nil {
		return "none"
	}
	out := []string{}
	names := []string{}
	for name := range t.AsStructuredType().members {
		names = append(names, name)
	}
	sort.Strings(names)
	for _, name := range names {
		symbol := t.AsStructuredType().members[name]
		var value any
		if OracleCollectionsStoredSymbolID(symbol) != 0 {
			value = c.valueSymbolLinks.TryGet(symbol)
		}
		out = append(out, fmt.Sprintf("name=%q symbol=%#v valueLinks=%#v deferredLinks=%#v", name, symbol, value, c.deferredSymbolLinks.TryGet(symbol)))
	}
	return strings.Join(out, "; ")
}
