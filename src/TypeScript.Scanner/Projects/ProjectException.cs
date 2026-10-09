// Authored unsupported/configuration/host boundary, distinct from compiler diagnostics.
namespace TypeScript.Compiler;
public sealed class ProjectException : NotSupportedException
{
    public string Feature { get; }
    public string Path { get; }

    public ProjectException(string feature, string message, string path = "") : base(message)
    {
        Feature = feature;
        Path = path;
    }
}
