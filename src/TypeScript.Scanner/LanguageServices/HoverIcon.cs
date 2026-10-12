// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/hovericon.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.LanguageServices.Utilities;
using TypeScript.Lsp.Protocol;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    private const string vsImageCatalogGuid = "ae27a6b0-e345-4288-96df-5eaf394ee369";
    private const int imageIdWarning = 0x00000637;
    private const int imageIdKeyword = 0x00000635;
    private const int imageIdModulePrivate = 0x0000077D;
    private const int imageIdModuleProtected = 0x0000077E;
    private const int imageIdModulePublic = 0x0000077F;
    private const int imageIdType = 0x00000CA1;
    private const int imageIdNamespace = 0x0000079F;
    private const int imageIdClassPrivate = 0x000001D7;
    private const int imageIdClassProtected = 0x000001D8;
    private const int imageIdClassPublic = 0x000001D9;
    private const int imageIdInterfacePrivate = 0x00000646;
    private const int imageIdInterfaceProtected = 0x00000647;
    private const int imageIdInterfacePublic = 0x00000648;
    private const int imageIdEnumPrivate = 0x00000469;
    private const int imageIdEnumProtected = 0x0000046A;
    private const int imageIdEnumPublic = 0x0000046B;
    private const int imageIdEnumMember = 0x00000465;
    private const int imageIdLocalVariable = 0x000006D3;
    private const int imageIdPropertyPrivate = 0x00000982;
    private const int imageIdPropertyProtected = 0x00000983;
    private const int imageIdPropertyPublic = 0x00000984;
    private const int imageIdMethodPrivate = 0x00000756;
    private const int imageIdMethodProtected = 0x00000757;
    private const int imageIdMethodPublic = 0x00000758;
    private const int imageIdLabel = 0x0000067D;
    private const int imageIdAssembly = 0x000000C4;
    private const int imageIdConstantPrivate = 0x0000026A;
    private const int imageIdConstantProtected = 0x0000026B;
    private const int imageIdConstantPublic = 0x0000026C;
    private static VSImageId newVSImageId(int id) => new()
    {
        Guid = ByteStringValue.FromString(vsImageCatalogGuid),
        Id = id
    };
    // hovericon.go:56. Original ordered private/protected selection carries explicit state.
    private static VSImageId getVSHoverImageId(ScriptElementKind kind, ScriptElementKindModifier modifiers)
    {
        bool isPrivate = (modifiers & ScriptElementKindModifier.Private) != 0;
        bool isProtected = (modifiers & ScriptElementKindModifier.Protected) != 0;
        static VSImageId pick(bool isPrivate, bool isProtected, int privateId, int protectedId, int publicId) => newVSImageId(isPrivate ? privateId : isProtected ? protectedId : publicId);
        switch (kind)
        {
            case ScriptElementKind.Warning:
                return newVSImageId(imageIdWarning);
            case ScriptElementKind.Keyword:
                return newVSImageId(imageIdKeyword);
            case ScriptElementKind.ScriptElement:
                return pick(isPrivate, isProtected, imageIdModulePrivate, imageIdModuleProtected, imageIdModulePublic);
            case ScriptElementKind.PrimitiveType:
                return newVSImageId(imageIdType);
            case ScriptElementKind.ModuleElement:
                return newVSImageId(imageIdNamespace);
            case ScriptElementKind.ConstructorImplementationElement:
            case ScriptElementKind.ClassElement:
            case ScriptElementKind.LocalClassElement:
            case ScriptElementKind.TypeElement:
                return pick(isPrivate, isProtected, imageIdClassPrivate, imageIdClassProtected, imageIdClassPublic);
            case ScriptElementKind.InterfaceElement:
                return pick(isPrivate, isProtected, imageIdInterfacePrivate, imageIdInterfaceProtected, imageIdInterfacePublic);
            case ScriptElementKind.EnumElement:
                return pick(isPrivate, isProtected, imageIdEnumPrivate, imageIdEnumProtected, imageIdEnumPublic);
            case ScriptElementKind.EnumMemberElement:
                return newVSImageId(imageIdEnumMember);
            case ScriptElementKind.ParameterElement:
            case ScriptElementKind.VariableElement:
            case ScriptElementKind.LocalVariableElement:
            case ScriptElementKind.VariableUsingElement:
            case ScriptElementKind.VariableAwaitUsingElement:
            case ScriptElementKind.LetElement:
            case ScriptElementKind.String:
                return newVSImageId(imageIdLocalVariable);
            case ScriptElementKind.ConstElement:
                return pick(isPrivate, isProtected, imageIdConstantPrivate, imageIdConstantProtected, imageIdConstantPublic);
            case ScriptElementKind.MemberGetAccessorElement:
            case ScriptElementKind.MemberSetAccessorElement:
            case ScriptElementKind.MemberVariableElement:
            case ScriptElementKind.MemberAccessorVariableElement:
                return pick(isPrivate, isProtected, imageIdPropertyPrivate, imageIdPropertyProtected, imageIdPropertyPublic);
            case ScriptElementKind.FunctionElement:
            case ScriptElementKind.LocalFunctionElement:
            case ScriptElementKind.MemberFunctionElement:
            case ScriptElementKind.CallSignatureElement:
            case ScriptElementKind.IndexSignatureElement:
            case ScriptElementKind.ConstructSignatureElement:
                return pick(isPrivate, isProtected, imageIdMethodPrivate, imageIdMethodProtected, imageIdMethodPublic);
            case ScriptElementKind.TypeParameterElement:
                return newVSImageId(imageIdType);
            case ScriptElementKind.Label:
                return newVSImageId(imageIdLabel);
            case ScriptElementKind.Alias:
                return newVSImageId(imageIdModulePublic);
            default:
                return newVSImageId(imageIdAssembly);
        }
    }

    // hovericon.go:132. Required element slices and pointer DTO owners mirror the original result.
    private static VSContainerElement? buildVSHoverRawContent(VSImageId imageId, Slice<VSClassifiedTextRun?> quickInfoRuns, Slice<VSClassifiedTextRun?> documentationRuns)
    {
        if (quickInfoRuns.Count == 0)
            return null;
        var displayLine = new VSContainerElement
        {
            Style = VSContainerElementStyle.Wrapped,
            Elements = Slice<VSImageElementOrClassifiedTextElementOrContainerElement>.FromArray([new() { ImageElement = new VSImageElement { ImageId = imageId } }, new() { ClassifiedTextElement = new VSClassifiedTextElement { Runs = quickInfoRuns } }])
        };
        if (documentationRuns.Count == 0)
            return displayLine;
        return new VSContainerElement
        {
            Style = VSContainerElementStyle.Stacked,
            Elements = Slice<VSImageElementOrClassifiedTextElementOrContainerElement>.FromArray([new() { ContainerElement = displayLine }, new() { ClassifiedTextElement = new VSClassifiedTextElement { Runs = documentationRuns } }])
        };
    }
}
