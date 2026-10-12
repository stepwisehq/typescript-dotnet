// Authored actual Linux OS write/emit controls; not original assertions.
package compiler_test
import (
 "bytes"
 "context"
 "encoding/json"
 "fmt"
 "os"
 "path/filepath"
 "testing"
 "github.com/microsoft/TypeScript/tsc/internal/bundled"
 "github.com/microsoft/TypeScript/tsc/internal/compiler"
 "github.com/microsoft/TypeScript/tsc/internal/core"
 "github.com/microsoft/TypeScript/tsc/internal/stringutil"
 "github.com/microsoft/TypeScript/tsc/internal/tsoptions"
 "github.com/microsoft/TypeScript/tsc/internal/tspath"
 "github.com/microsoft/TypeScript/tsc/internal/vfs/osvfs"
)
func TestAuthoredActualWriteFailure(t *testing.T) {
 root:=t.TempDir();name:=filepath.Join(root,"main.ts")
 if err:=os.WriteFile(name,[]byte("export const value = 1;\n"),0600);err!=nil {t.Fatal(err)}
 blocker:=filepath.Join(root,"blocker");if err:=os.WriteFile(blocker,[]byte("retained blocker"),0600);err!=nil {t.Fatal(err)}
 fs:=bundled.WrapFS(osvfs.FS());outDir:=tspath.RootedDirectoryPath(filepath.Join(blocker,"out"));outName:=tspath.RootedFilePath(filepath.Join(string(outDir),"main.js"))
 runtimeError:=fs.WriteFile(outName,"probe");if runtimeError==nil {t.Fatal("regular-file parent must fail")}
 options:=&core.CompilerOptions{Target:core.ScriptTargetESNext,Module:core.ModuleKindESNext,SkipLibCheck:core.TSTrue,ListEmittedFiles:core.TSTrue,OutDir:outDir}
 p:=compiler.NewProgram(compiler.ProgramOptions{Config:tsoptions.NewParsedCommandLine(options,[]tspath.RootedFilePath{tspath.RootedFilePath(name)},nil,tspath.RootedDirectoryPath(root),fs.CaseSensitivity()),Host:compiler.NewCompilerHost(fs,bundled.LibPath(),nil,nil,nil),SingleThreaded:core.TSTrue})
 result:=p.Emit(context.Background(),compiler.EmitOptions{})
 if result==nil||result.EmitSkipped||len(result.EmittedFiles)!=0||len(result.Diagnostics)!=1 {t.Fatal("unexpected emit observations")}
 d:=result.Diagnostics[0];if d.Code()!=5033||len(d.MessageArgs())!=2||d.MessageArgs()[0]!=string(outName)||d.MessageArgs()[1]!=runtimeError.Error() {t.Fatal("write diagnostic lost actual path/error")}
 retained,err:=os.ReadFile(blocker);if err!=nil||string(retained)!="retained blocker" {t.Fatal("blocker modified")}
 encoded,err:=json.Marshal(struct{Root,RuntimeError string;EmitSkipped bool;EmittedFiles []tspath.RootedFilePath;Diagnostics []tsDiagnosticValue}{root,runtimeError.Error(),result.EmitSkipped,result.EmittedFiles,tsProjectDiagnostics(result.Diagnostics)});if err!=nil {t.Fatal(err)}
 fmt.Printf("WRITE_FAILURE_OUTPUT=%s\n",encoded)
}
func TestAuthoredActualOutputDirectoryFailure(t *testing.T) {
 root:=t.TempDir();name:=filepath.Join(root,"main.ts")
 if err:=os.WriteFile(name,[]byte("export const value = 1;\n"),0600);err!=nil {t.Fatal(err)}
 blocker:=filepath.Join(root,"out");if err:=os.MkdirAll(filepath.Join(blocker,"main.js"),0700);err!=nil {t.Fatal(err)}
 fs:=bundled.WrapFS(osvfs.FS());outDir:=tspath.RootedDirectoryPath(blocker);outName:=tspath.RootedFilePath(filepath.Join(string(outDir),"main.js"))
 runtimeError:=fs.WriteFile(outName,"probe");if runtimeError==nil {t.Fatal("existing output directory must fail")}
 options:=&core.CompilerOptions{Target:core.ScriptTargetESNext,Module:core.ModuleKindESNext,SkipLibCheck:core.TSTrue,ListEmittedFiles:core.TSTrue,OutDir:outDir}
 p:=compiler.NewProgram(compiler.ProgramOptions{Config:tsoptions.NewParsedCommandLine(options,[]tspath.RootedFilePath{tspath.RootedFilePath(name)},nil,tspath.RootedDirectoryPath(root),fs.CaseSensitivity()),Host:compiler.NewCompilerHost(fs,bundled.LibPath(),nil,nil,nil),SingleThreaded:core.TSTrue})
 result:=p.Emit(context.Background(),compiler.EmitOptions{})
 if result==nil||result.EmitSkipped||len(result.EmittedFiles)!=0||len(result.Diagnostics)!=1 {t.Fatal("unexpected emit observations")}
 d:=result.Diagnostics[0];if d.Code()!=5033||len(d.MessageArgs())!=2||d.MessageArgs()[0]!=string(outName)||d.MessageArgs()[1]!=runtimeError.Error() {t.Fatal("write diagnostic lost actual path/error")}
 encoded,err:=json.Marshal(struct{Root,RuntimeError string;EmitSkipped bool;EmittedFiles []tspath.RootedFilePath;Diagnostics []tsDiagnosticValue}{root,runtimeError.Error(),result.EmitSkipped,result.EmittedFiles,tsProjectDiagnostics(result.Diagnostics)});if err!=nil {t.Fatal(err)}
 fmt.Printf("WRITE_DIRECTORY_FAILURE_OUTPUT=%s\n",encoded)
}
func TestAuthoredActualRawOutputBytes(t *testing.T) {
 root:=t.TempDir();name:=tspath.RootedFilePath(filepath.Join(root,"nested","output.bin"));payload:=[]byte{0,255,128,195,169,13,10,10};expected:=append([]byte{239,187,191},payload...)
 content:=stringutil.AddUTF8ByteOrderMark(string(payload));if err:=osvfs.FS().WriteFile(name,content);err!=nil {t.Fatal(err)}
 actual,err:=os.ReadFile(string(name));if err!=nil||!bytes.Equal(actual,expected)||stringutil.AddUTF8ByteOrderMark(content)!=content {t.Fatal("raw/BOM/newline mismatch")}
 fmt.Printf("WRITE_BYTES_OUTPUT=%x\n",actual)
}
