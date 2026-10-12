// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete reader/writer owner from internal/lsp/server.go106–175, draft only.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using ProtocolMessage = TypeScript.Lsp.Protocol.Message;

namespace TypeScript.Lsp;
internal interface ILspReader
{
    (ProtocolMessage? Message, Exception? Error) Read();
}

internal interface ILspWriter
{
    Exception? Write(ProtocolMessage message);
}

internal sealed class MessageMarshalError(Exception error) : Exception("failed to marshal message: " + error.Message, error)
{
    // The source multi-error unwrap retains both the protocol classification and
    // original cause. Consumers must inspect this marker as InternalError while
    // the ordinary InnerException retains the original encoding error.
    internal ErrorCode Code => ErrorCode.InternalError;
}

internal static class LspTransport
{
    internal static ILspReader ToReader(Stream stream) => new LspReader(BaseReader.NewBaseReader(stream));
    internal static ILspWriter ToWriter(Stream stream) => new LspWriter(BaseWriter.NewBaseWriter(stream));
    private sealed class LspReader(BaseReader reader) : ILspReader
    {
        public (ProtocolMessage? Message, Exception? Error) Read()
        {
            byte[] data;
            try
            {
                data = reader.Read();
            }
            catch (Exception error)when (error is IOException or ObjectDisposedException or FormatException)
            {
                return (null, error);
            }

            var message = new ProtocolMessage();
            try
            {
                message.UnmarshalJSON(ByteStringValue.FromView(ByteStringView.FromOwnedBytes(data)));
            }
            catch (Exception error)when (error is System.Text.Json.JsonException or FormatException or ProtocolErrorException)
            {
                if (HasCode(error, ErrorCode.InvalidParams))
                    return (message, new ProtocolErrorException(ErrorCode.InvalidParams, error.Message, error));
                return (null, new ProtocolErrorException(ErrorCode.InvalidRequest, error.Message, error));
            }

            return (message, null);
        }
    }

    private sealed class LspWriter(BaseWriter writer) : ILspWriter
    {
        public Exception? Write(ProtocolMessage message)
        {
            ByteStringValue data;
            try
            {
                data = message.MarshalJSON();
            }
            catch (Exception error)when (error is System.Text.Json.JsonException or FormatException)
            {
                return new MessageMarshalError(error);
            }

            try
            {
                writer.Write(data.ByteSpan);
                return null;
            }
            catch (Exception error)when (error is IOException or ObjectDisposedException)
            {
                return error;
            }
        }
    }

    private static bool HasCode(Exception error, ErrorCode code)
    {
        for (Exception? current = error; current != null; current = current.InnerException)
            if (current is ProtocolErrorException protocol && protocol.Code == code)
                return true;
        return false;
    }
}
