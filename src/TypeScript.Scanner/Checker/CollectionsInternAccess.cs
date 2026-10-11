// Authored selected reached-object association view. Reuses the accepted normal
// union/intersection reader and also observes the genuine star-key family.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    internal (bool Normal, bool NoConstraintReduction) VerificationCollectionsInternMembership(Type type)
    {
        void allocated(Type value)
        {
            if (value == null || value.id == 0 || value.checker != null && !ReferenceEquals(value.checker, this))
                throw new InvalidOperationException("Unallocated or foreign composite key term");
        }

        allocated(type);
        foreach (Type child in type.Types())
            allocated(child);
        if (type.alias != null)
        {
            if (type.alias.symbol == null || type.alias.symbol.id == 0)
                throw new InvalidOperationException("Unallocated composite alias key symbol");
            foreach (Type child in type.alias.typeArguments)
                allocated(child);
        }

        if ((type.flags & TypeFlags.Union) != 0)
        {
            var origin = type.AsUnionType().origin;
            if (origin != null)
            {
                allocated(origin);
                if ((origin.flags & (TypeFlags.Union | TypeFlags.Intersection)) != 0)
                    foreach (Type child in origin.Types())
                        allocated(child);
            }

            return (VerificationDataInternMembership(type), false);
        }

        if ((type.flags & TypeFlags.Intersection) != 0)
        {
            bool normal = VerificationDataInternMembership(type);
            var key = getIntersectionKey(type.Types(), IntersectionFlags.NoConstraintReduction, type.alias);
            return (normal, intersectionTypes.TryGetValue(key, out var value) && ReferenceEquals(value, type));
        }

        throw new InvalidOperationException("Noncomposite intern membership");
    }
}
