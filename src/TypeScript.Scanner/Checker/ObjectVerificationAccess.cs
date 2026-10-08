// Authored read-only cache observation for independent structural verification.
namespace TypeScript.Checking;
public sealed partial class Checker
{
    internal Type? VerificationCachedObjectType(Type source, string kind)
    {
        var cacheKind = kind switch
        {
            "Widened" => CachedTypeKind.Widened,
            "RegularObjectLiteral" => CachedTypeKind.RegularObjectLiteral,
            _ => throw new InvalidDataException("Unknown object cache observation")
        };
        return cachedTypes.TryGetValue(new(cacheKind, source.Id()), out var value) ? value : null;
    }

    internal List<(string Name, bool BoolValue)> VerificationRelationKeyEquality()
    {
        var source = numberType;
        var target = stringType;
        var forward = getRelationKey(source, target, IntersectionState.None, false, false);
        var again = getRelationKey(source, target, IntersectionState.None, false, false);
        var reverse = getRelationKey(target, source, IntersectionState.None, false, false);
        var identity = getRelationKey(source, target, IntersectionState.None, true, false);
        var identityReverse = getRelationKey(target, source, IntersectionState.None, true, false);
        var stateSource = getRelationKey(source, target, IntersectionState.Source, false, false);
        var stateTarget = getRelationKey(source, target, IntersectionState.Target, false, false);
        var ignoreConstraints = getRelationKey(source, target, IntersectionState.None, false, true);
        return[("relation-key.same-input-stable", forward.key == again.key), ("relation-key.direction-distinct", forward.key != reverse.key), ("relation-key.identity-order-normalized", identity.key == identityReverse.key), ("relation-key.state-source-distinct", forward.key != stateSource.key), ("relation-key.state-target-distinct", forward.key != stateTarget.key && stateSource.key != stateTarget.key), ("relation-key.nongeneric-unconstrained", !forward.constrained && forward.key == ignoreConstraints.key)];
    }
}
