// Authored differential observations of the pinned core.Filter; not an upstream test.
package main
import "github.com/microsoft/TypeScript/tsc/internal/core"
type filterRow struct{Name string;Input []int;Visited []int;Result []int;ResultNil bool;Same bool}
func filterRows()[]filterRow{
 rows:=[]filterRow{}
 for _,name:=range []string{"nil","empty","all","first-failure","append-all","append-first-failure"}{
  var input []int
  if name=="empty"{input=[]int{}}else if name!="nil"{input=make([]int,3,8);copy(input,[]int{1,2,3})}
  var visited []int
  result:=core.Filter(input,func(value int)bool{
   visited=append(visited,value)
   if name=="append-all"||name=="append-first-failure"{input=append(input,9)}
   return !((name=="first-failure"||name=="append-first-failure")&&value==2)
  })
  rows=append(rows,filterRow{name,input,visited,result,result==nil,core.Same(result,input)})
 }
 return rows
}
