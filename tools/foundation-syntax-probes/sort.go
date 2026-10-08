package main

import "slices"

func sortControls() any {
	rows := []any{}
	type value struct{ key, id int }
	for _, length := range []int{0, 1, 2, 11, 12, 13, 64, 129} {
		for _, pattern := range []string{"equal", "ascending", "descending", "modular"} {
			a := []value{}
			for i := 0; i < length; i++ {
				key := 0
				switch pattern {
				case "ascending":
					key = i / 3
				case "descending":
					key = (length - i) / 3
				case "modular":
					key = (i * 17) % 7
				}
				a = append(a, value{key, i})
			}
			slices.SortFunc(a, func(x, y value) int { return x.key - y.key })
			r := []any{}
			for _, v := range a {
				r = append(r, []int{v.key, v.id})
			}
			rows = append(rows, []any{length, pattern, r})
		}
	}
	return rows
}
