// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.Scanner;

namespace TypeScript.ProjectSystem.Ata;
internal enum NameValidationResult
{
    NameOk,
    EmptyName,
    NameTooLong,
    NameStartsWithDot,
    NameStartsWithUnderscore,
    NameContainsNonURISafeCharacters
}

internal static partial class AutomaticTypeAcquisition
{
    private const int maxPackageNameLength = 214;
    // internal/project/ata/validatepackagename.go:28.
    internal static (NameValidationResult Result, ByteStringValue Name, bool IsScopeName) ValidatePackageName(ByteStringValue packageName) => ValidatePackageNameWorker(packageName, true);
    // validatepackagename.go:32. All lengths/segments are raw Go byte headers.
    private static (NameValidationResult Result, ByteStringValue Name, bool IsScopeName) ValidatePackageNameWorker(ByteStringValue packageName, bool supportScopedPackage)
    {
        if (packageName.Length == 0)
            return (NameValidationResult.EmptyName, default, false);
        if (packageName.Length > maxPackageNameLength)
            return (NameValidationResult.NameTooLong, default, false);
        var(firstChar, _) = packageName.IsBytes ? Utf8.DecodeRuneInString(packageName.ByteSpan) : Utf8.DecodeRuneInString(packageName.CharSpan);
        if (firstChar == '.')
            return (NameValidationResult.NameStartsWithDot, default, false);
        if (firstChar == '_')
            return (NameValidationResult.NameStartsWithUnderscore, default, false);
        if (supportScopedPackage && packageName[0] == '@')
        {
            var withoutScope = packageName.Slice(1);
            int slash = withoutScope.IsBytes ? withoutScope.ByteSpan.IndexOf((byte)'/') : withoutScope.CharSpan.IndexOf('/');
            if (slash > 0 && slash < withoutScope.Length - 1)
            {
                var scope = withoutScope.Slice(0, slash);
                var scopedPackageName = withoutScope.Slice(slash + 1);
                if (!(scopedPackageName.IsBytes ? scopedPackageName.ByteSpan.Contains((byte)'/') : scopedPackageName.CharSpan.Contains('/')))
                {
                    var scopeResult = ValidatePackageNameWorker(scope, false).Result;
                    if (scopeResult != NameValidationResult.NameOk)
                        return (scopeResult, scope, true);
                    var packageResult = ValidatePackageNameWorker(scopedPackageName, false).Result;
                    if (packageResult != NameValidationResult.NameOk)
                        return (packageResult, scopedPackageName, false);
                    return (NameValidationResult.NameOk, default, false);
                }
            }
        }

        // net/url/url.go shouldEscape(mode=encodeQueryComponent): QueryEscape differs
        // iff a byte outside the exact unreserved set is present. Its output is not retained.
        for (int index = 0; index < packageName.Length; index++)
            if (packageName[index] is not (>= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_' or '.' or '~'))
                return (NameValidationResult.NameContainsNonURISafeCharacters, default, false);
        return (NameValidationResult.NameOk, default, false);
    }

    // validatepackagename.go:76. Logging is the original owned raw-byte message boundary.
    internal static string RenderPackageNameValidationFailure(ByteStringValue typing, NameValidationResult result, ByteStringValue name, bool isScopeName)
    {
        string kind = isScopeName ? "Scope" : "Package";
        if (name.Length == 0)
            name = typing;
        return result switch
        {
            NameValidationResult.EmptyName => Logging.LogFormat.Sprintf("'%s':: %s name '%s' cannot be empty", typing, kind, name),
            NameValidationResult.NameTooLong => Logging.LogFormat.Sprintf("'%s':: %s name '%s' should be less than %d characters", typing, kind, name, maxPackageNameLength),
            NameValidationResult.NameStartsWithDot => Logging.LogFormat.Sprintf("'%s':: %s name '%s' cannot start with '.'", typing, kind, name),
            NameValidationResult.NameStartsWithUnderscore => Logging.LogFormat.Sprintf("'%s':: %s name '%s' cannot start with '_'", typing, kind, name),
            NameValidationResult.NameContainsNonURISafeCharacters => Logging.LogFormat.Sprintf("'%s':: %s name '%s' contains non URI safe characters", typing, kind, name),
            NameValidationResult.NameOk => throw new InvalidOperationException("Unexpected Ok result"),
            _ => throw new InvalidOperationException("Unknown package name validation result")};
    }
}
