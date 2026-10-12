// Closed actual codelens.go Command.Arguments dynamic []lsproto.Location value.
// Reuse generated typed workers; no reflection, temporary buffer, or slice clone.
using TypeScript.Scanner;

namespace TypeScript.Lsp.Protocol;
internal static partial class ProtocolCodec
{
    internal static ProtocolType<Slice<Location>> CodeLensLocationsType => CodeLensLocationsDescriptor.Value;

    private static class CodeLensLocationsDescriptor
    {
        internal static readonly ProtocolType<Slice<Location>> Value = new("[]lsproto.Location", static (ref ProtocolDecoder decoder, ref Slice<Location> value) => ReadShape_5610d1960004ba0d(ref decoder, ref value), static (encoder, value) => WriteShape_5610d1960004ba0d(encoder, value));
    }
}
