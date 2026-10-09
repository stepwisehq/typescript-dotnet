// Authored direct observations of original core.Find captured-range behavior.
package main

import "github.com/microsoft/TypeScript/tsc/internal/core"

func supportProjection() any {
	nilResult := core.Find[string](nil, func(string) bool { return true })
	absent := core.Find([]string{"a"}, func(string) bool { return false })
	values := make([]string, 2, 3)
	copy(values, []string{"a", "b"})
	visited := []string{}
	result := core.Find(values, func(item string) bool {
		visited = append(visited, item)
		if item == "a" {
			values = append(values, "c")
		}
		return false
	})
	match := core.Find(values, func(item string) bool { return item == "b" })
	return map[string]any{"nilResult": nilResult, "noMatch": absent, "capturedResult": result, "visited": visited, "caller": values, "match": match}
}
