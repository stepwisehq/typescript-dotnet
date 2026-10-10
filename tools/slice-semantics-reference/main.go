// Authored Go-spec conformance witness. Reports public slice observations, not
// allocator growth-capacity sequences. Run with the locked local Go toolchain.
package main

import (
 "encoding/json"
 "os"
 "slices"
)

func require(ok bool) { if !ok { panic("slice conformance assertion failed") } }
func equal(a, b []int) { require(slices.Equal(a, b)) }
func panics(action func()) (result bool) { defer func() { result = recover() != nil }(); action(); return }

func main() {
 var nilSlice []int
 empty := make([]int, 0)
 require(nilSlice == nil && empty != nil && len(nilSlice) == 0 && cap(nilSlice) == 0)
 require(nilSlice[:0] == nil && nilSlice[:0:0] == nil && empty[:0] != nil)
 require(append(nilSlice, empty...) == nil && append(empty, nilSlice...) != nil)
 original := make([]int, 2, 4)
 original[0], original[1] = 11, 22
 header := original
 header[0] = 33
 appended := append(header, 44)
 require(len(original) == 2 && len(header) == 2 && len(appended) == 3)
 equal(original[:3], []int{33, 22, 44})
 middle := []int{1,2,3,4}[1:2]
 require(len(middle) == 1 && cap(middle) == 3)
 equal(middle[:3], []int{2,3,4})
 equal(middle[2:3], []int{4})
 require(panics(func() { _ = middle[2:] }))
 limitedBacking := []int{1,2,3,4}
 limited := limitedBacking[1:2:2]
 grown := append(limited, 99)
 limited[0] = 8
 equal(limitedBacking, []int{1,8,3,4})
 equal(grown, []int{2,99})
 short := make([]int, 1, 4)
 require(panics(func() { _ = short[1] }))
 require(panics(func() { short[1] = 9 }))
 short[0] = 7
 equal(short[:4], []int{7,0,0,0})
 overlap := make([]int, 4, 6)
 copy(overlap, []int{1,2,3,4})
 overlapResult := append(overlap[:1], overlap[:3]...)
 equal(overlapResult, []int{1,1,2,3})
 full := []int{1,2,3}
 overlapGrowth := append(full[1:], full...)
 equal(overlapGrowth, []int{2,3,1,2,3})
 equal(full, []int{1,2,3})
 right := []int{1,2,3,4}
 require(copy(right[1:], right[:3]) == 3)
 equal(right, []int{1,1,2,3})
 left := []int{1,2,3,4}
 require(copy(left[:3], left[1:]) == 3)
 equal(left, []int{2,3,4,4})
 ranged := make([]int, 2, 4)
 ranged[0], ranged[1] = 1,2
 visited := []int{}
 for i, v := range ranged {
  visited = append(visited, v)
  if i == 0 { ranged = append(ranged, 3); ranged[1] = 20 }
 }
 equal(visited, []int{1,20})
 detachedRange := []int{1,2}
 detachedVisited := []int{}
 for i, v := range detachedRange {
  detachedVisited = append(detachedVisited,v)
  if i == 0 { detachedRange = append(detachedRange,3); detachedRange[1] = 20 }
 }
 equal(detachedVisited, []int{1,2})
 stack := make([]int,0,3)
 stack = append(stack,10,20)
 captured := stack
 stack = append(stack[:len(stack)-1],30)
 equal(captured, []int{10,30})
 stack = stack[:0]
 require(len(stack) == 0 && cap(stack) == 3)
 stack = append(stack,40)
 equal(stack, []int{40})
 equal(captured, []int{40,30})
 report := map[string]any{
  "nil": nilSlice == nil, "emptyNil": empty == nil,
  "copiedHeaderLength":len(header), "appendedLength":len(appended),
  "sharedValues":original[:3], "twoIndexValues":middle[:3],
  "fullSliceOriginal":limitedBacking, "fullSliceDetached":grown,
  "overlapAppend":overlapResult, "overlapGrowth":overlapGrowth,
  "copyRight":right, "copyLeft":left,
  "rangeShared":visited, "rangeDetached":detachedVisited,
  "pooledStack":stack, "capturedStack":captured,
 }
 if err := json.NewEncoder(os.Stdout).Encode(report); err != nil { panic(err) }
}
