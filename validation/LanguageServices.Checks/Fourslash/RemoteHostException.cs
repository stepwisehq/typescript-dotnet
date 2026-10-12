// Preserve the original host's raw Go error bytes independently of CLR display text.
using TypeScript.Scanner;

namespace LanguageServices.Checks.Fourslash;
internal sealed class RemoteHostException(ByteStringValue raw) : IOException(GoString.ToString(raw.Materialize()!))
{
    internal ByteStringValue Raw { get; } = raw;
}
