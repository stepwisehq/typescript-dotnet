// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Demanded complete tspath/path.go:1184 SplitVolumePath counterpart.
namespace TypeScript.Scanner;
public static partial class TsPath
{
    public static (ByteStringValue Volume, ByteStringValue Remainder, bool Ok) SplitVolumePath(ByteStringValue path)
    {
        if (path.Length >= 2 && IsVolumeCharacter(path[0]) && path[1] == ':')
            return (ToFileNameLowerCaseGoBytes(path.Slice(0, 2)), path.Slice(2), true);
        return (default, path, false);
    }
}
