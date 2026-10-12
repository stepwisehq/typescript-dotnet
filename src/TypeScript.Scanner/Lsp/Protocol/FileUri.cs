// Copyright 2009 The Go Authors. All rights reserved.
// Licensed under the BSD-style license in licenses/Go-LICENSE.txt.
// Demanded file:// projection of Go1.27.1 net/url.Parse, parseAuthority,
// parseHost and unescape. Only Host and Path escape the lsproto URI boundary.
using System.Net;
using System.Net.Sockets;
using TypeScript.Scanner;

namespace TypeScript.Lsp.Protocol;

internal static class FileUri
{
    private enum EscapeMode { Path, Host, Zone }

    internal static (ByteStringValue Host, ByteStringValue Path) Parse(ByteStringValue uri)
    {
        try
        {
            // url.Parse splits the fragment before parse checks control bytes.
            int fragment = uri.IndexOf('#');
            var input = fragment < 0 ? uri : uri.Slice(0, fragment);
            for (int i = 0; i < input.Length; i++)
                if (input[i] < ' ' || input[i] == 0x7f)
                    throw new FormatException();
            var rest = input.Slice(7); // The caller has matched exact file://.
            int query = rest.IndexOf('?');
            if (query >= 0)
                rest = rest.Slice(0, query);
            int slash = rest.IndexOf('/');
            var authority = slash < 0 ? rest : rest.Slice(0, slash);
            var path = slash < 0 ? default : rest.Slice(slash);
            int at = authority.LastIndexOf('@');
            var host = parseHost(at < 0 ? authority : authority.Slice(at + 1));
            if (at >= 0)
            {
                var userinfo = authority.Slice(0, at);
                for (int i = 0; i < userinfo.Length; i++)
                {
                    int c = userinfo[i];
                    if (!isAlphaNumeric(c) && c is not ('-' or '.' or '_' or ':' or '~' or '!' or '$' or '&' or '\'' or '(' or ')' or '*' or '+' or ',' or ';' or '=' or '%' or '@'))
                        throw new FormatException();
                }
                // Username/password share the same percent-validation rules.
                unescape(userinfo, EscapeMode.Path);
            }
            var unescapedPath = unescape(path, EscapeMode.Path);
            if (fragment >= 0)
                unescape(uri.Slice(fragment + 1), EscapeMode.Path);
            return (host, unescapedPath);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("invalid file URI: " + uri.Materialize());
        }
    }

    private static ByteStringValue parseHost(ByteStringValue host)
    {
        int openBracket = host.LastIndexOf('[');
        if (openBracket > 0)
            throw new FormatException();
        if (openBracket == 0)
        {
            int closeBracket = host.LastIndexOf(']');
            if (closeBracket < 0)
                throw new FormatException();
            var colonPort = host.Slice(closeBracket + 1);
            if (!validOptionalPort(colonPort))
                throw new FormatException();
            var unescapedPort = unescape(colonPort, EscapeMode.Host);
            var hostname = host.Slice(1, closeBracket);
            ByteStringValue unescapedHostname;
            int zone = hostname.IndexOf(ByteStringValue.FromString("%25"));
            if (zone >= 0)
                unescapedHostname = ByteStringValue.Concat(unescape(hostname.Slice(0, zone), EscapeMode.Host), unescape(hostname.Slice(zone), EscapeMode.Zone));
            else
                unescapedHostname = unescape(hostname, EscapeMode.Host);
            int percent = unescapedHostname.IndexOf('%');
            if (percent == unescapedHostname.Length - 1)
                throw new FormatException();
            var address = percent < 0 ? unescapedHostname : unescapedHostname.Slice(0, percent);
            // netip.ParseAddr's required IPv6 result uses the standard .NET
            // IPv6 parser; a textual zone remains an opaque retained header.
            if (!IPAddress.TryParse(address.Materialize(), out var parsed) || parsed.AddressFamily != AddressFamily.InterNetworkV6)
                throw new FormatException();
            return ByteStringValue.Concat(ByteStringValue.FromString("["), unescapedHostname, ByteStringValue.FromString("]"), unescapedPort);
        }
        int colon = host.IndexOf(':');
        if (colon >= 0 && !validOptionalPort(host.Slice(host.LastIndexOf(':'))))
            throw new FormatException();
        return unescape(host, EscapeMode.Host);
    }

    private static bool validOptionalPort(ByteStringValue port)
    {
        if (port.Length == 0)
            return true;
        if (port[0] != ':')
            return false;
        for (int i = 1; i < port.Length; i++)
            if (port[i] is < '0' or > '9')
                return false;
        return true;
    }

    private static ByteStringValue unescape(ByteStringValue text, EscapeMode mode)
    {
        int count = 0;
        for (int i = 0; i < text.Length;)
        {
            int c = text[i];
            if (c == '%')
            {
                if (i + 2 >= text.Length || hex(text[i + 1]) < 0 || hex(text[i + 2]) < 0)
                    throw new FormatException();
                int value = hex(text[i + 1]) * 16 + hex(text[i + 2]);
                bool escapedPercent = text[i + 1] == '2' && text[i + 2] == '5';
                if (mode == EscapeMode.Host && value < 128 && !escapedPercent)
                    throw new FormatException();
                if (mode == EscapeMode.Zone && !escapedPercent && value != ' ' && shouldEscapeHost(value))
                    throw new FormatException();
                count++;
                i += 3;
            }
            else
            {
                if (mode != EscapeMode.Path && c < 128 && shouldEscapeHost(c))
                    throw new FormatException();
                i++;
            }
        }
        if (count == 0)
            return text;
        var result = new GoBuilder();
        result.Grow(text.Length - 2 * count);
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '%')
            {
                result.WriteByte(hex(text[i + 1]) * 16 + hex(text[i + 2]));
                i += 2;
            }
            else
                result.WriteByte(text[i]);
        }
        return result.Value();
    }

    private static bool isAlphaNumeric(int value) => value is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9';
    private static bool shouldEscapeHost(int value) => !isAlphaNumeric(value) && value is not ('!' or '$' or '&' or '\'' or '(' or ')' or '*' or '+' or ',' or ';' or '=' or ':' or '[' or ']' or '<' or '>' or '"' or '-' or '_' or '.' or '~');
    private static int hex(int value) => value is >= '0' and <= '9' ? value - '0' : value is >= 'a' and <= 'f' ? value - 'a' + 10 : value is >= 'A' and <= 'F' ? value - 'A' + 10 : -1;
}
