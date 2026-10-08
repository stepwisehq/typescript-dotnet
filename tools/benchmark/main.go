// Scanner benchmark adapter for the pinned maintained TypeScript Go implementation.
package main

import (
 "encoding/json"
 "fmt"
 "os"
 "runtime"
 "strconv"
 "strings"
 "time"

 "github.com/microsoft/TypeScript/tsc/internal/ast"
 "github.com/microsoft/TypeScript/tsc/internal/scanner"
)

type result struct {
 Implementation string `json:"implementation"`
 Mode string `json:"mode"`
 InputBytes int64 `json:"inputBytes"`
 InputFiles int `json:"inputFiles"`
 Iterations int64 `json:"iterations"`
 TokensPerPass int64 `json:"tokensPerPass"`
 Checksum string `json:"checksum"`
 ElapsedSeconds float64 `json:"elapsedSeconds"`
 AllocatedBytes uint64 `json:"allocatedBytes"`
 GCCollections uint32 `json:"gcCollections"`
 RuntimeVersion string `json:"runtimeVersion"`
}
func main() {
 if len(os.Args)!=4 { panic("Usage: scanner-bench FILE_LIST SAMPLE_MS WARMUP_MS") }
 list,err:=os.ReadFile(os.Args[1]);if err!=nil {panic(err)}
 sources:=[]string{}
 var inputBytes int64
 for _,path:=range strings.Split(strings.TrimSpace(string(list)),"\n") {
  data,err:=os.ReadFile(path);if err!=nil {panic(err)}
  sources=append(sources,string(data));inputBytes+=int64(len(data))
 }
 if inputBytes==0 {panic("Empty benchmark workload")}
 sampleMs,err:=strconv.Atoi(os.Args[2]);if err!=nil {panic(err)}
 warmupMs,err:=strconv.Atoi(os.Args[3]);if err!=nil {panic(err)}
 s:=scanner.NewScanner()
 pass:=func()(uint64,int64) {
  checksum:=uint64(14695981039346656037)
  var tokens int64
  for _,source:=range sources {
   s.Reset();s.SetText(source)
   for guard:=0;guard<len(source)*3+10;guard++ {
    kind:=s.Scan()
    checksum=(checksum^uint64(kind))*1099511628211
    checksum=(checksum^uint64(s.TokenEnd()))*1099511628211
    checksum=(checksum^uint64(s.TokenFlags()))*1099511628211
    tokens++
    if kind==ast.KindEndOfFile {break}
    if guard==len(source)*3+9 {panic("No EOF within progress guard")}
   }
  }
  return checksum,tokens
 }
 expectedChecksum,expectedTokens:=pass()
 warmupStart:=time.Now()
 for time.Since(warmupStart)<time.Duration(warmupMs)*time.Millisecond {
  checksum,tokens:=pass();if checksum!=expectedChecksum||tokens!=expectedTokens {panic("Warmup checksum changed")}
 }
 runtime.GC()
 var before,after runtime.MemStats
 runtime.ReadMemStats(&before)
 started:=time.Now()
 var iterations int64
 var elapsed time.Duration
 for {
  checksum,tokens:=pass();if checksum!=expectedChecksum||tokens!=expectedTokens {panic("Measured checksum changed")}
  iterations++
  elapsed=time.Since(started)
  if elapsed>=time.Duration(sampleMs)*time.Millisecond {break}
 }
 runtime.ReadMemStats(&after)
 output:=result{"go","reset-settext-scan",inputBytes,len(sources),iterations,expectedTokens,fmt.Sprintf("%016x",expectedChecksum),elapsed.Seconds(),after.TotalAlloc-before.TotalAlloc,after.NumGC-before.NumGC,runtime.Version()}
 if err:=json.NewEncoder(os.Stdout).Encode(output);err!=nil {panic(err)}
}
