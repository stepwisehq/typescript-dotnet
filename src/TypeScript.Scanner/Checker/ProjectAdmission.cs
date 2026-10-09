// Authored source boundary: the accepted collection syntax plus bounded ES modules.
using TypeScript.Scanner;

namespace TypeScript.Checking;

internal static class ProjectAdmission
{
    internal static void Validate(SourceFile file) => CollectionAdmission.Validate(file, project: true);

    internal static void ValidateSpecifier(SourceFile file, Node specifier)
    {
        if (specifier.Kind != Kind.StringLiteral)
            throw new UnsupportedSemanticFeatureException(file, specifier, "moduleSpecifier");
        string name = specifier.Text();
        if (!(name is "." or ".." || name.StartsWith("./", StringComparison.Ordinal) || name.StartsWith("../", StringComparison.Ordinal)))
            throw new UnsupportedSemanticFeatureException(file, specifier, "nonRelativeModuleSpecifier");
    }

    internal static bool AllowedModuleSyntax(Kind kind) => kind is
        Kind.ImportDeclaration or Kind.ImportClause or Kind.NamespaceImport or Kind.NamedImports or Kind.ImportSpecifier or
        Kind.ExportAssignment or Kind.ExportDeclaration or Kind.NamedExports or Kind.NamespaceExport or Kind.ExportSpecifier or
        Kind.ExportKeyword or Kind.DefaultKeyword;
}
