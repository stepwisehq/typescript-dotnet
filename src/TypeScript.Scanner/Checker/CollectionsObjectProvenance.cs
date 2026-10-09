// Authored observation metadata for otherwise indistinguishable fresh literals.
// Producers supply computed inputs; scoped applicability metadata performs no
// checking, semantic getter, mapper or ID work and restores on every exit.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    internal sealed record CollectionsApplicabilityOrigin(string Phase, Node Node, Signature Signature, string Relation, uint Mode, bool? ReportErrors);
    private CollectionsApplicabilityOrigin collectionsApplicabilityOrigin;
    private readonly struct CollectionsApplicabilityScope(Checker checker, CollectionsApplicabilityOrigin previous) : IDisposable
    {
        public void Dispose()
        {
            if (checker != null)
                checker.collectionsApplicabilityOrigin = previous;
        }
    }

    private CollectionsApplicabilityScope verificationCollectionsApplicabilityScope(Node node, Signature signature, Relation relation, CheckMode mode, bool reportErrors) => verificationCollectionsCheckScope("applicability", node, signature, relation, mode, reportErrors);
    private CollectionsApplicabilityScope verificationCollectionsOverloadScope(Node node, Relation relation, CheckMode mode) => verificationCollectionsCheckScope("choose-overload", node, null, relation, mode, null);
    private CollectionsApplicabilityScope verificationCollectionsCheckScope(string phase, Node node, Signature signature, Relation relation, CheckMode mode, bool? reportErrors)
    {
        if (!authoredCollectionsProfile)
            return default;
        string category = ReferenceEquals(relation, assignableRelation) ? "assignable" : ReferenceEquals(relation, comparableRelation) ? "comparable" : ReferenceEquals(relation, identityRelation) ? "identity" : ReferenceEquals(relation, subtypeRelation) ? "subtype" : ReferenceEquals(relation, strictSubtypeRelation) ? "strict-subtype" : "unknown";
        var previous = collectionsApplicabilityOrigin;
        collectionsApplicabilityOrigin = new(phase, node, signature, category, (uint)mode, reportErrors);
        return new(this, previous);
    }

    private readonly record struct CollectionsObjectOrigin(Node Node, CheckMode Mode, Type ContextualType, Type Source, CollectionsApplicabilityOrigin Applicability);
    private readonly Dictionary<Type, CollectionsObjectOrigin> collectionsObjectOrigins = new();
    private void verificationCollectionsCaptureObject(Type result, Node node, CheckMode mode, Type contextualType)
    {
        if (authoredCollectionsProfile)
            collectionsObjectOrigins.Add(result, new(node, mode, contextualType, null, collectionsApplicabilityOrigin));
    }

    private void verificationCollectionsCaptureRegularObject(Type result, Type source)
    {
        if (authoredCollectionsProfile && collectionsObjectOrigins.TryGetValue(source, out var origin))
            collectionsObjectOrigins.Add(result, origin with { Source = source });
    }

    internal (bool Present, Node Node, uint Mode, Type ContextualType, Type Source, CollectionsApplicabilityOrigin Applicability) VerificationCollectionsObjectOrigin(Type type)
    {
        if (type.checker != null && !ReferenceEquals(type.checker, this))
            throw new InvalidOperationException("Foreign literal producer owner");
        if (!collectionsObjectOrigins.TryGetValue(type, out var origin))
            return (false, null, 0, null, null, null);
        if (origin.Applicability?.Relation == "unknown")
            throw new InvalidOperationException("Unknown literal applicability relation");
        if (origin.Applicability is { } frame && (frame.Phase == "applicability" ? frame.Signature == null || frame.ReportErrors == null : frame.Phase != "choose-overload" || frame.Signature != null || frame.ReportErrors != null))
            throw new InvalidOperationException("Invalid literal check phase inputs");
        return (true, origin.Node, (uint)origin.Mode, origin.ContextualType, origin.Source, origin.Applicability);
    }
}
