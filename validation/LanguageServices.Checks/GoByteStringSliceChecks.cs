using TypeScript.Scanner;

namespace LanguageServices.Checks;
public static class GoByteStringSliceChecks
{
    // Authored public file-rename dependency: slices.Contains must match equal
    // extension bytes independently of their retained UTF8 owners and offsets.
    public static int RunAll()
    {
        var extensions = Slice<ByteStringValue>.FromArray([ByteStringValue.FromView(ByteStringView.FromOwnedBytes(".css"u8.ToArray()))]);
        var otherOwner = ByteStringValue.FromView(ByteStringView.FromOwnedBytes("x.cssy"u8.ToArray())).Slice(1, 5);
        if (!GoSlices.Contains(extensions, otherOwner) || GoSlices.Index(extensions, ByteStringValue.FromString(".css")) != 0 || GoSlices.Contains(extensions, ByteStringValue.FromString(".js")) || GoSlices.Index(default(Slice<ByteStringValue>), otherOwner) != -1)
            throw new InvalidOperationException("Go string slice membership lost byte-content comparison");
        if (!GenericContains(extensions, otherOwner) || !extensions[0].Equals((object)otherOwner) || extensions[0].GetHashCode() != otherOwner.GetHashCode() || extensions[0].GetHashCode() != ByteStringValue.FromString(".css").GetHashCode() || default(ByteStringValue).Equals(ByteStringValue.FromString(null))
            || !default(ByteStringValue).Equals(ByteStringValue.FromString(""))
            || default(ByteStringValue).GetHashCode() != ByteStringValue.FromString("").GetHashCode())
            throw new InvalidOperationException("Go string owner equality/hash contract differs across backing roles");
        return 1;
    }

    private static bool GenericContains<T>(Slice<T> values, T target) => GoSlices.Contains(values, target);
}
