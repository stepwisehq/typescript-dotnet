// C# adaptation of ast.SourceFile satisfying internal/sourcemap.Source.
// Borrow the accepted immutable UTF8 owner and retain actual source-file identity.
using TypeScript.SourceMap;
namespace TypeScript.Scanner;
public sealed partial class SourceFile : Source
{
    ByteStringView Source.Text() => TextView();
    RootedFilePath Source.FileName() => RootedFilePath.FromValue(FileNameGoValue());
}
