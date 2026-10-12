// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.ProjectSystem.Ata;
using TypeScript.Scanner;

namespace LanguageServices.Checks;
public static class AtaPackageNameChecks
{
    // Original internal/project/ata/validatepackagename_test.go TestValidatePackageName.
    // Parallel value-only subtests execute synchronously; original status/name/scope assertions remain.
    public static int RunAll()
    {
        var packageName = new GoBuilder();
        packageName.WriteString("a");
        for (int i = 0; i < 8; i++)
            packageName.WriteString(packageName.String());
        CheckStatus(ByteStringValue.FromString(packageName.String()), NameValidationResult.NameTooLong);
        CheckStatus(".foo", NameValidationResult.NameStartsWithDot);
        CheckStatus("_foo", NameValidationResult.NameStartsWithUnderscore);
        CheckStatus("  scope  ", NameValidationResult.NameContainsNonURISafeCharacters);
        CheckStatus("; say ‘Hello from TypeScript!’ #", NameValidationResult.NameContainsNonURISafeCharacters);
        CheckStatus("a/b/c", NameValidationResult.NameContainsNonURISafeCharacters);
        CheckStatus("@scope/bar", NameValidationResult.NameOk);
        CheckFull("@.scope/bar", NameValidationResult.NameStartsWithDot, ".scope", true);
        CheckFull("@.scope/.bar", NameValidationResult.NameStartsWithDot, ".scope", true);
        CheckFull("@_scope/bar", NameValidationResult.NameStartsWithUnderscore, "_scope", true);
        CheckFull("@_scope/_bar", NameValidationResult.NameStartsWithUnderscore, "_scope", true);
        CheckFull("@  scope  /bar", NameValidationResult.NameContainsNonURISafeCharacters, "  scope  ", true);
        CheckFull("@; say ‘Hello from TypeScript!’ #/bar", NameValidationResult.NameContainsNonURISafeCharacters, "; say ‘Hello from TypeScript!’ #", true);
        CheckFull("@  scope  /  bar  ", NameValidationResult.NameContainsNonURISafeCharacters, "  scope  ", true);
        CheckFull("@scope/.bar", NameValidationResult.NameStartsWithDot, ".bar", false);
        CheckFull("@scope/_bar", NameValidationResult.NameStartsWithUnderscore, "_bar", false);
        CheckFull("@scope/  bar  ", NameValidationResult.NameContainsNonURISafeCharacters, "  bar  ", false);
        CheckFull("@scope/; say ‘Hello from TypeScript!’ #", NameValidationResult.NameContainsNonURISafeCharacters, "; say ‘Hello from TypeScript!’ #", false);
        return 1;
    }

    // Authored owner coverage repeats each unchanged original expectation on both
    // retained raw-string and byte-buffer headers; no additional original credit.
    private static IEnumerable<ByteStringValue> BothOwners(ByteStringValue text)
    {
        yield return text;
        var bytes = new byte[text.Length];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = (byte)text[i];
        yield return ByteStringValue.FromView(ByteStringView.FromOwnedBytes(bytes));
    }

    private static ByteStringValue Bytes(string text) => ByteStringValue.FromString(GoString.ToUtf8(text));
    private static void CheckStatus(string text, NameValidationResult expected) => CheckStatus(Bytes(text), expected);
    private static void CheckStatus(ByteStringValue text, NameValidationResult expected)
    {
        foreach (var owner in BothOwners(text))
            if (AutomaticTypeAcquisition.ValidatePackageName(owner).Result != expected)
                throw new InvalidOperationException("Original package-name status assertion failed");
    }

    private static void CheckFull(string text, NameValidationResult expected, string name, bool isScope)
    {
        foreach (var owner in BothOwners(Bytes(text)))
        {
            var actual = AutomaticTypeAcquisition.ValidatePackageName(owner);
            if (actual.Result != expected || !actual.Name.EqualsValue(Bytes(name)) || actual.IsScopeName != isScope)
                throw new InvalidOperationException("Original scoped package-name assertions failed");
        }
    }
}
