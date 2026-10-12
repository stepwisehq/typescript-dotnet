// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete counterpart of estransforms/definitions.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.Transformers.EsTransforms;
internal static partial class EsTransforms
{
    // definitions.go:9. Original ordered chains instantiate only genuine factories.
    private static readonly TransformerFactory esDecoratorAndClassFields = Transformers.Chain(ESDecoratorTransformer.NewESDecoratorTransformer, ClassFieldsTransformer.NewClassFieldsTransformer);
    internal static readonly TransformerFactory NewESNextTransformer = Transformers.Chain(UsingDeclarationTransformer.NewUsingDeclarationTransformer, esDecoratorAndClassFields);
    internal static readonly TransformerFactory NewES2021Transformer = Transformers.Chain(NewESNextTransformer, LogicalAssignmentTransformer.NewLogicalAssignmentTransformer);
    internal static readonly TransformerFactory NewES2020Transformer = Transformers.Chain(NewES2021Transformer, NullishCoalescingTransformer.NewNullishCoalescingTransformer, OptionalChainTransformer.NewOptionalChainTransformer);
    internal static readonly TransformerFactory NewES2019Transformer = Transformers.Chain(NewES2020Transformer, OptionalCatchTransformer.NewOptionalCatchTransformer);
    internal static readonly TransformerFactory NewES2018Transformer = Transformers.Chain(NewES2019Transformer, ObjectRestSpreadTransformer.NewObjectRestSpreadTransformer, ForAwaitTransformer.NewForAwaitTransformer, TaggedTemplateTransformer.NewTaggedTemplateLiftRestrictionTransformer);
    internal static readonly TransformerFactory NewES2017Transformer = Transformers.Chain(NewES2018Transformer, AsyncTransformer.NewAsyncTransformer);
    internal static readonly TransformerFactory NewES2016Transformer = Transformers.Chain(NewES2017Transformer, ExponentiationTransformer.NewExponentiationTransformer);
    // definitions.go:27.
    internal static Transformer GetESTransformer(TransformOptions opts) => opts.CompilerOptions.GetEmitScriptTarget() switch
    {
        ScriptTarget.ESNext => esDecoratorAndClassFields(opts),
        ScriptTarget.ES2026 or ScriptTarget.ES2025 or ScriptTarget.ES2024 or ScriptTarget.ES2023 or ScriptTarget.ES2022 or ScriptTarget.ES2021 => NewESNextTransformer(opts),
        ScriptTarget.ES2020 => NewES2021Transformer(opts),
        ScriptTarget.ES2019 => NewES2020Transformer(opts),
        ScriptTarget.ES2018 => NewES2019Transformer(opts),
        ScriptTarget.ES2017 => NewES2018Transformer(opts),
        ScriptTarget.ES2016 => NewES2017Transformer(opts),
        _ => NewES2016Transformer(opts)};
}
