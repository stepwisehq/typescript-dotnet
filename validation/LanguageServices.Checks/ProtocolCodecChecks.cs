// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Named original protocol cases, excluding the four separately covered framing functions.
namespace LanguageServices.Checks;
public static partial class ProtocolCodecChecks
{
    public static IEnumerable<string> Names => ["TestUnmarshalCompletionItem", "TestTryDynamicFileNameToDocumentUri", "TestUnmarshalRejectsNullForOptionalNonNullableFields", "TestUnmarshalAcceptsNullForNullableFields", "TestUnmarshalAcceptsOmittedOptionalFields", "TestUnmarshalRejectsIncompleteObjects", "TestMarshalUnmarshalRoundTrip", "TestUnmarshalUnionTypes", "TestMarshalUnionTypes", "TestUnmarshalIgnoresUnknownFields", "TestUnmarshalRejectsWrongTypes", "TestUnmarshalUnionTypeWrongKind", "TestUnmarshalBooleanUnionTypes", "TestUnmarshalDiscriminatorUnion", "TestUnmarshalPresenceDiscriminatorUnion", "TestUnmarshalStringOrArrayUnion", "TestUnmarshalDocumentEditUnion", "TestUnmarshalFieldOrdering", "TestUnmarshalCompletionItemDataFileName", "TestUnmarshalEmptyObject", "TestMarshalOmitsZeroOptionalFields", "TestLiteralTypes", "TestEnumStringValues", "TestRoundTrip", "TestStrictnessMissingRequired", "TestStrictnessNotObject", "TestUnmarshalParamsRequiresParams", ];

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestUnmarshalCompletionItem":
                TestUnmarshalCompletionItem();
                break;
            case "TestTryDynamicFileNameToDocumentUri":
                TestTryDynamicFileNameToDocumentUri();
                break;
            case "TestUnmarshalRejectsNullForOptionalNonNullableFields":
                TestUnmarshalRejectsNullForOptionalNonNullableFields();
                break;
            case "TestUnmarshalAcceptsNullForNullableFields":
                TestUnmarshalAcceptsNullForNullableFields();
                break;
            case "TestUnmarshalAcceptsOmittedOptionalFields":
                TestUnmarshalAcceptsOmittedOptionalFields();
                break;
            case "TestUnmarshalRejectsIncompleteObjects":
                TestUnmarshalRejectsIncompleteObjects();
                break;
            case "TestMarshalUnmarshalRoundTrip":
                TestMarshalUnmarshalRoundTrip();
                break;
            case "TestUnmarshalUnionTypes":
                TestUnmarshalUnionTypes();
                break;
            case "TestMarshalUnionTypes":
                TestMarshalUnionTypes();
                break;
            case "TestUnmarshalIgnoresUnknownFields":
                TestUnmarshalIgnoresUnknownFields();
                break;
            case "TestUnmarshalRejectsWrongTypes":
                TestUnmarshalRejectsWrongTypes();
                break;
            case "TestUnmarshalUnionTypeWrongKind":
                TestUnmarshalUnionTypeWrongKind();
                break;
            case "TestUnmarshalBooleanUnionTypes":
                TestUnmarshalBooleanUnionTypes();
                break;
            case "TestUnmarshalDiscriminatorUnion":
                TestUnmarshalDiscriminatorUnion();
                break;
            case "TestUnmarshalPresenceDiscriminatorUnion":
                TestUnmarshalPresenceDiscriminatorUnion();
                break;
            case "TestUnmarshalStringOrArrayUnion":
                TestUnmarshalStringOrArrayUnion();
                break;
            case "TestUnmarshalDocumentEditUnion":
                TestUnmarshalDocumentEditUnion();
                break;
            case "TestUnmarshalFieldOrdering":
                TestUnmarshalFieldOrdering();
                break;
            case "TestUnmarshalCompletionItemDataFileName":
                TestUnmarshalCompletionItemDataFileName();
                break;
            case "TestUnmarshalEmptyObject":
                TestUnmarshalEmptyObject();
                break;
            case "TestMarshalOmitsZeroOptionalFields":
                TestMarshalOmitsZeroOptionalFields();
                break;
            case "TestLiteralTypes":
                TestLiteralTypes();
                break;
            case "TestEnumStringValues":
                TestEnumStringValues();
                break;
            case "TestRoundTrip":
                TestRoundTrip();
                break;
            case "TestStrictnessMissingRequired":
                TestStrictnessMissingRequired();
                break;
            case "TestStrictnessNotObject":
                TestStrictnessNotObject();
                break;
            case "TestUnmarshalParamsRequiresParams":
                TestUnmarshalParamsRequiresParams();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(name));
        }
    }

    public static int RunAll()
    {
        int count = 0;
        foreach (var name in Names)
        {
            Run(name);
            count++;
        }

        return count;
    }
}
