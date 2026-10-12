// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Pinned internal/fswatch/fanotify_linux.go:137–156,598–703; MIT license.
using System.Buffers.Binary;
using TypeScript.Scanner;

namespace TypeScript.FsWatch;
internal readonly record struct FanotifyHandleKey(int Fsid0, int Fsid1, int HandleType, ByteStringKey Handle)
{
    internal static FanotifyHandleKey Make(int fsid0, int fsid1, int handleType, ReadOnlySpan<byte> bytes) => new(fsid0, fsid1, handleType, ByteStringKey.FromValue(ByteStringValue.FromView(ByteStringView.CopyFrom(bytes))));
}

internal sealed class FanotifyDfidName(FanotifyHandleKey key, ByteStringValue name)
{
    internal readonly FanotifyHandleKey Key = key;
    internal readonly ByteStringValue Name = name;
}

internal sealed partial class FanotifyBackend
{
    internal static (FanotifyDfidName? Primary, FanotifyDfidName? Rename) ParseDfidNames(ReadOnlySpan<byte> data)
    {
        FanotifyDfidName? primary = null, rename = null;
        const int headerSize = 4;
        for (int offset = 0; offset + headerSize <= data.Length;)
        {
            byte type = data[offset];
            int length = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset + 2, 2));
            if (length < headerSize || offset + length > data.Length)
                break;
            switch (type)
            {
                case LinuxKernel.FAN_EVENT_INFO_TYPE_DFID_NAME:
                case LinuxKernel.FAN_EVENT_INFO_TYPE_OLD_DFID_NAME:
                    if (ParseFidRecord(data.Slice(offset, length), true)is { } parsedPrimary)
                        primary = parsedPrimary;
                    break;
                case LinuxKernel.FAN_EVENT_INFO_TYPE_NEW_DFID_NAME:
                    if (ParseFidRecord(data.Slice(offset, length), true)is { } parsedRename)
                        rename = parsedRename;
                    break;
                case LinuxKernel.FAN_EVENT_INFO_TYPE_DFID:
                    if (primary is null && ParseFidRecord(data.Slice(offset, length), false)is { } fallback)
                        primary = fallback;
                    break;
            }

            if (primary is not null && rename is not null)
                return (primary, rename);
            offset += length;
        }

        return (primary, rename);
    }

    internal static FanotifyDfidName? ParseFidRecord(ReadOnlySpan<byte> data, bool hasName)
    {
        const int headerSize = 4, fsidSize = 8, handleHeaderSize = 8;
        if (data.Length < headerSize + fsidSize + handleHeaderSize)
            return null;
        var body = data.Slice(headerSize);
        int fsid0 = BinaryPrimitives.ReadInt32LittleEndian(body);
        int fsid1 = BinaryPrimitives.ReadInt32LittleEndian(body.Slice(4));
        // Go int is 64-bit on the verified platform; retain unsigned handle-size
        // admission before taking the managed span, rather than int32 wrapping.
        long handleBytes = BinaryPrimitives.ReadUInt32LittleEndian(body.Slice(8));
        int handleType = BinaryPrimitives.ReadInt32LittleEndian(body.Slice(12));
        const int handleStart = fsidSize + handleHeaderSize;
        if (handleStart + handleBytes > body.Length)
            return null;
        var key = FanotifyHandleKey.Make(fsid0, fsid1, handleType, body.Slice(handleStart, (int)handleBytes));
        var name = FsWatch.Empty;
        if (hasName)
        {
            int nameStart = handleStart + (int)handleBytes;
            if (nameStart < body.Length)
            {
                var nameBytes = body.Slice(nameStart);
                int terminator = nameBytes.IndexOf((byte)0);
                if (terminator >= 0)
                    nameBytes = nameBytes.Slice(0, terminator);
                name = ByteStringValue.FromView(ByteStringView.CopyFrom(nameBytes));
            }
        }

        return new FanotifyDfidName(key, name);
    }
}
