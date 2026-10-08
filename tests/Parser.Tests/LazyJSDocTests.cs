using TypeScript.Scanner;
using Xunit;

namespace TypeScript.Scanner.ParserTests;

public class LazyJSDocTests
{
    [Fact]
    public void RegisteredLazyParserCanCacheNoJSDoc()
    {
        var file = Parser.ParseSourceFile(new("/empty.ts", "/empty.ts", new(false, false)), "", ScriptKind.TS);
        var identifier = new NodeFactory().NewIdentifier("x");
        identifier.Loc = new TextRange(0, 0);
        identifier.Parent = file.AsNode();
        identifier.Flags |= NodeFlags.HasJSDoc;
        Assert.Null(identifier.JSDoc(file));
        Assert.Null(identifier.EagerJSDoc(file));
        Assert.Null(identifier.JSDoc(file));
        Assert.Null(identifier.EagerJSDoc(file));
    }
}
