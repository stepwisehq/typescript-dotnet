// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete pinned declaration support, fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using ScannerApi = TypeScript.Scanner.Scanner;

namespace TypeScript.Transformers.Declarations;
// tracker.go:12. Retained shared pointer state and Go slice headers.
internal sealed class SymbolTrackerImpl : TypeScript.NodeBuilding.SymbolTracker
{
    internal IEmitResolver resolver;
    internal SymbolTrackerSharedState state;
    internal IDeclarationEmitHost host;
    private Slice<Node> fallbackStack;
    internal Symbol watchedClassSymbol;
    internal bool classSymbolTracked;
    private Func<Node, Diagnostic> getIsolatedDeclarationError;
    // PopErrorFallbackNode implements checker.SymbolTracker.
    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/tracker.go:27:1.
    public void PopErrorFallbackNode()
    {
        this.fallbackStack = this.fallbackStack.Slice2(0, (GoRuntime.Len(this.fallbackStack) - 1));
    }

    // PushErrorFallbackNode implements checker.SymbolTracker.
    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/tracker.go:32:1.
    public void PushErrorFallbackNode(Node node)
    {
        this.fallbackStack = this.fallbackStack.Append(node);
    }

    // ReportCyclicStructureError implements checker.SymbolTracker.
    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/tracker.go:37:1.
    public void ReportCyclicStructureError()
    {
        var location = this.errorLocation();
        if ((location != null))
        {
            this.state.addDiagnostic(DeclarationTransforms.CreateDiagnosticForNode(location, Diagnostics.The_inferred_type_of_0_references_a_type_with_a_cyclic_structure_which_cannot_be_trivially_serialized_A_type_annotation_is_necessary, this.errorDeclarationNameWithFallback()));
        }
    }

    // ReportInaccessibleThisError implements checker.SymbolTracker.
    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/tracker.go:45:1.
    public void ReportInaccessibleThisError()
    {
        var location = this.errorLocation();
        if ((location != null))
        {
            this.state.addDiagnostic(DeclarationTransforms.CreateDiagnosticForNode(location, Diagnostics.The_inferred_type_of_0_references_an_inaccessible_1_type_A_type_annotation_is_necessary, this.errorDeclarationNameWithFallback(), "this"));
        }
    }

    // ReportInaccessibleUniqueSymbolError implements checker.SymbolTracker.
    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/tracker.go:53:1.
    public void ReportInaccessibleUniqueSymbolError()
    {
        var location = this.errorLocation();
        if ((location != null))
        {
            this.state.addDiagnostic(DeclarationTransforms.CreateDiagnosticForNode(location, Diagnostics.The_inferred_type_of_0_references_an_inaccessible_1_type_A_type_annotation_is_necessary, this.errorDeclarationNameWithFallback(), "unique symbol"));
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/tracker.go:60:1.
    private bool isBoundExpando(Node node)
    {
        if (!((Ast.IsExpandoPropertyDeclaration(node) && Ast.IsPropertyAccessExpression(node.AsBinaryExpression().Left))))
        {
            return false;
        }

        var ns = Ast.GetLeftmostAccessExpression(node.AsBinaryExpression().Left);
        if (!Ast.IsIdentifier(ns))
        {
            return false;
        }

        var @ref = this.resolver.GetReferencedValueDeclarationUnsafe(ns);
        if ((@ref == null))
        {
            return false;
        }

        return this.resolver.IsExpandoFunctionDeclarationUnsafe(@ref);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/tracker.go:77:1.
    private bool isChildOfBoundExpando(Node node)
    {
        return (Ast.FindAncestorOrQuit(node, this, static (n, tracker) =>
        {
            if ((Ast.IsSourceFile(n) || Ast.IsBlock(n)))
            {
                return FindAncestorResult.Quit;
            }

            return Ast.ToFindAncestorResult(tracker.isBoundExpando(n));
        }) != null);
    }

    // ReportInferenceFallback implements checker.SymbolTracker.
    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/tracker.go:87:1.
    public void ReportInferenceFallback(Node node)
    {
        if (!this.state.isolatedDeclarations)
        {
            return;
        }

        if ((Ast.GetSourceFileOfNode(node) != this.state.currentSourceFile))
        {
            return;
        }

        if (this.state.resolver.IsExpandoFunctionDeclarationUnsafe(node))
        {
            this.state.reportExpandoFunctionErrors(node);
        }

        if (!this.isChildOfBoundExpando(node))
        {
            this.state.addDiagnostic(this.getIsolatedDeclarationError(node));
        }
    }

    // ReportLikelyUnsafeImportRequiredError implements checker.SymbolTracker.
    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/tracker.go:103:1.
    public void ReportLikelyUnsafeImportRequiredError(string specifier, string symbolName)
    {
        var location = this.errorLocation();
        if ((location != null))
        {
            if ((symbolName != ""))
            {
                this.state.addDiagnostic(DeclarationTransforms.CreateDiagnosticForNode(location, Diagnostics.The_inferred_type_of_0_cannot_be_named_without_a_reference_to_2_from_1_This_is_likely_not_portable_A_type_annotation_is_necessary, this.errorDeclarationNameWithFallback(), specifier, symbolName));
            }
            else
            {
                this.state.addDiagnostic(DeclarationTransforms.CreateDiagnosticForNode(location, Diagnostics.The_inferred_type_of_0_cannot_be_named_without_a_reference_to_1_This_is_likely_not_portable_A_type_annotation_is_necessary, this.errorDeclarationNameWithFallback(), specifier));
            }
        }
    }

    // ReportNonSerializableProperty implements checker.SymbolTracker.
    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/tracker.go:115:1.
    public void ReportNonSerializableProperty(string propertyName)
    {
        var location = this.errorLocation();
        if ((location != null))
        {
            this.state.addDiagnostic(DeclarationTransforms.CreateDiagnosticForNode(location, Diagnostics.The_type_of_this_node_cannot_be_serialized_because_its_property_0_cannot_be_serialized, propertyName));
        }
    }

    // ReportNonlocalAugmentation implements checker.SymbolTracker.
    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/tracker.go:123:1.
    public void ReportNonlocalAugmentation(SourceFile containingFile, Symbol parentSymbol, Symbol augmentingSymbol)
    {
        var primaryDeclaration = Core.Find(parentSymbol.Declarations, containingFile, static (d, file) =>
        {
            return (Ast.GetSourceFileOfNode(d) == file);
        });
        var augmentingDeclarations = Core.Filter(augmentingSymbol.Declarations, containingFile, static (d2, file) =>
        {
            return (Ast.GetSourceFileOfNode(d2) != file);
        });
        if (((primaryDeclaration != null) && (GoRuntime.Len(augmentingDeclarations) > 0)))
        {
            {
                foreach (var augmentations in GoRuntime.RangeValues(augmentingDeclarations))
                {
                    var diag = DeclarationTransforms.CreateDiagnosticForNode(augmentations, Diagnostics.Declaration_augments_declaration_in_another_file_This_cannot_be_serialized);
                    var related = DeclarationTransforms.CreateDiagnosticForNode(primaryDeclaration, Diagnostics.This_is_the_declaration_being_augmented_Consider_moving_the_augmenting_declaration_into_the_same_file);
                    diag.AddRelatedInfo(related);
                    this.state.addDiagnostic(diag);
                }
            }
        }
    }

    // ReportPrivateInBaseOfClassExpression implements checker.SymbolTracker.
    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/tracker.go:137:1.
    public void ReportPrivateInBaseOfClassExpression(string propertyName)
    {
        var location = this.errorLocation();
        if ((location != null))
        {
            var diag = DeclarationTransforms.CreateDiagnosticForNode(location, Diagnostics.Property_0_of_exported_anonymous_class_type_may_not_be_private_or_protected, propertyName);
            if (Ast.IsVariableDeclaration(location.Parent))
            {
                var related = DeclarationTransforms.CreateDiagnosticForNode(location, Diagnostics.Add_a_type_annotation_to_the_variable_0, this.errorDeclarationNameWithFallback());
                diag.AddRelatedInfo(related);
            }

            this.state.addDiagnostic(diag);
        }
    }

    // ReportTruncationError implements checker.SymbolTracker.
    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/tracker.go:150:1.
    public void ReportTruncationError()
    {
        var location = this.errorLocation();
        if ((location != null))
        {
            this.state.addDiagnostic(DeclarationTransforms.CreateDiagnosticForNode(location, Diagnostics.The_inferred_type_of_this_node_exceeds_the_maximum_length_the_compiler_will_serialize_An_explicit_type_annotation_is_needed));
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/tracker.go:157:1.
    private Node errorFallbackNode()
    {
        if ((GoRuntime.Len(this.fallbackStack) >= 1))
        {
            return this.fallbackStack[(GoRuntime.Len(this.fallbackStack) - 1)];
        }

        return null;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/tracker.go:164:1.
    private Node errorLocation()
    {
        var location = this.state.errorNameNode;
        if ((location == null))
        {
            location = this.errorFallbackNode();
        }

        return location;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/tracker.go:172:1.
    private ByteStringValue errorDeclarationNameWithFallback()
    {
        if ((this.state.errorNameNode != null))
        {
            return ScannerApi.DeclarationNameToStringValue(this.state.errorNameNode);
        }

        if (((this.errorFallbackNode() != null) && (Ast.GetNameOfDeclaration(this.errorFallbackNode()) != null)))
        {
            return ScannerApi.DeclarationNameToStringValue(Ast.GetNameOfDeclaration(this.errorFallbackNode()));
        }

        if (((this.errorFallbackNode() != null) && Ast.IsExportAssignment(this.errorFallbackNode())))
        {
            if (this.errorFallbackNode().AsExportAssignment().IsExportEquals)
            {
                return ByteStringValue.FromString("export=");
            }

            return ByteStringValue.FromString("default");
        }

        return ByteStringValue.FromString("(Missing)");
    }

    // TrackSymbol implements checker.SymbolTracker.
    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/tracker.go:189:1.
    public bool TrackSymbol(Symbol symbol, Node enclosingDeclaration, SymbolFlags meaning)
    {
        if (((symbol.Flags & SymbolFlags.TypeParameter) != 0))
        {
            return false;
        }

        if (((this.watchedClassSymbol != null) && (symbol == this.watchedClassSymbol)))
        {
            this.classSymbolTracked = true;
            return false;
        }

        var issuedDiagnostic = this.handleSymbolAccessibilityError(this.resolver.IsSymbolAccessible(symbol, enclosingDeclaration, meaning, true));
        return issuedDiagnostic;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/declarations/tracker.go:204:1.
    internal bool handleSymbolAccessibilityError(SymbolAccessibilityResult symbolAccessibilityResult)
    {
        if ((symbolAccessibilityResult.Accessibility == SymbolAccessibility.Accessible))
        {
            if ((GoRuntime.Len(symbolAccessibilityResult.AliasesToMakeVisible) > 0))
            {
                {
                    foreach (var @ref in GoRuntime.RangeValues(symbolAccessibilityResult.AliasesToMakeVisible))
                    {
                        this.state.lateMarkedStatements = Core.AppendIfUnique(this.state.lateMarkedStatements, @ref);
                    }
                }
            }
        }
        else
        {
            if ((symbolAccessibilityResult.Accessibility != SymbolAccessibility.NotResolved))
            {
                var errorInfo = this.state.getSymbolAccessibilityDiagnostic(symbolAccessibilityResult);
                if ((errorInfo != null))
                {
                    var info = errorInfo.Value;
                    var diagNode = symbolAccessibilityResult.ErrorNode;
                    if ((diagNode == null))
                    {
                        diagNode = errorInfo.Value.errorNode;
                    }

                    if ((info.typeName != null))
                    {
                        this.state.addDiagnostic(DeclarationTransforms.CreateDiagnosticForNode(diagNode, info.diagnosticMessage, ScannerApi.GetTextOfNodeValue(info.typeName), symbolAccessibilityResult.ErrorSymbolName, symbolAccessibilityResult.ErrorModuleName));
                    }
                    else
                    {
                        this.state.addDiagnostic(DeclarationTransforms.CreateDiagnosticForNode(diagNode, info.diagnosticMessage, symbolAccessibilityResult.ErrorSymbolName, symbolAccessibilityResult.ErrorModuleName));
                    }

                    return true;
                }
            }
        }

        return false;
    }

    // tracker.go:255.
    internal static SymbolTrackerImpl NewSymbolTracker(IDeclarationEmitHost host, IEmitResolver resolver, SymbolTrackerSharedState state)
    {
        return new SymbolTrackerImpl
        {
            host = host,
            resolver = resolver,
            state = state,
            getIsolatedDeclarationError = DeclarationTransforms.CreateGetIsolatedDeclarationErrors(resolver)
        };
    }
}

// tracker.go:239. The shared object is retained by transformer and tracker.
internal sealed class SymbolTrackerSharedState
{
    internal Slice<Node> lateMarkedStatements;
    internal Slice<Diagnostic> diagnostics;
    internal GetSymbolAccessibilityDiagnostic getSymbolAccessibilityDiagnostic;
    internal Node errorNameNode;
    internal bool isolatedDeclarations;
    internal bool stripInternal;
    internal SourceFile currentSourceFile;
    internal IEmitResolver resolver;
    internal Action<Node> reportExpandoFunctionErrors;
    // tracker.go:251.
    internal void addDiagnostic(Diagnostic diagnostic) => diagnostics = diagnostics.Append(diagnostic);
}

internal static partial class DeclarationTransforms
{
    // tracker.go:235. Diagnostic formatting is the actual owned-output boundary.
    internal static Diagnostic CreateDiagnosticForNode(Node node, Message message, params object[] args) => TypeScript.Checking.Checker.NewDiagnosticForNode(node, message, args);
}
