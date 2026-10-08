// Repository formatting tool; uses Roslyn shipped with the selected .NET SDK.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
foreach (string path in args)
{
    SyntaxNode root = CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot();
    if (root.ContainsDiagnostics) throw new InvalidOperationException("Invalid C# syntax: " + path);
    File.WriteAllText(path, root.NormalizeWhitespace(eol: "\n").ToFullString() + "\n");
}
