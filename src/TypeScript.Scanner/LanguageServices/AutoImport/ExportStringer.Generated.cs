// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete counterpart of internal/ls/autoimport/export_stringer_generated.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using System.Globalization;
using TypeScript.Scanner;

namespace TypeScript.LanguageServices.AutoImport;
internal static partial class AutoImport
{
    private static readonly byte[] exportSyntaxIndex = [0, 16, 36, 53, 80, 110, 128, 143, 159, 192, 227];
    private const string exportSyntaxName = "ExportSyntaxNoneExportSyntaxModifierExportSyntaxNamedExportSyntaxDefaultModifierExportSyntaxDefaultDeclarationExportSyntaxEqualsExportSyntaxUMDExportSyntaxStarExportSyntaxCommonJSModuleExportsExportSyntaxCommonJSExportsProperty";
    // internal/ls/autoimport/export_stringer_generated.go:26
    internal static ByteStringValue String(this ExportSyntax syntax)
    {
        var index = (long)syntax;
        if (index < 0 || index >= exportSyntaxIndex.Length - 1)
            return ByteStringValue.FromString("ExportSyntax(" + index.ToString(CultureInfo.InvariantCulture) + ")");
        return ByteStringValue.FromString(exportSyntaxName).Slice(exportSyntaxIndex[(int)index], exportSyntaxIndex[(int)index + 1]);
    }
}
