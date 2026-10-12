// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/transformers/chain.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
namespace TypeScript.Transformers;
internal sealed class TransformOptions
{
    internal EmitContext Context;
    internal CompilerOptions CompilerOptions;
    internal IReferenceResolver Resolver;
    internal IEmitResolver EmitResolver;
    internal Func<HasFileName, ModuleKind> GetEmitModuleFormatOfFile;
}
internal delegate Transformer TransformerFactory(TransformOptions options);
internal static partial class Transformers
{
    private sealed class ChainedTransformer : Transformer
    {
        internal Slice<Transformer> components;
        internal Node Visit(Node node)
        {
            if (node.Kind != Kind.SourceFile)
                throw new InvalidOperationException("Chained transform passed non-sourcefile initial node");
            var result = node.AsSourceFile();
            foreach (var transformer in components)
                result = transformer.TransformSourceFile(result);
            return result.AsNode();
        }
    }
    internal static TransformerFactory Chain(params TransformerFactory[] transforms)
    {
        if (transforms.Length < 2)
        {
            if (transforms.Length == 0)
                throw new InvalidOperationException("Expected some number of transforms to chain, but got none");
            return transforms[0];
        }
        return options =>
        {
            var constructed = Slice<Transformer>.Make(0, transforms.Length);
            foreach (var transform in transforms)
            {
                var result = transform(options);
                if (result != null) constructed = constructed.Append(result);
            }
            if (constructed.Count == 0) return null;
            if (constructed.Count == 1) return constructed[0];
            var chain = new ChainedTransformer { components = constructed };
            return chain.NewTransformer(chain.Visit, options.Context);
        };
    }
}
