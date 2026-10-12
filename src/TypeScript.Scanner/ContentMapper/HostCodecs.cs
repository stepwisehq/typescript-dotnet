// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Closed JSON/v2 field closure for hostimpl.go:43–218. Root descriptors retain
// Go value vs pointer admission independently of the mapped CLR owner.
using TypeScript.Scanner;
using TypeScript.JsonRpc;
using TypeScript.Lsp.Protocol;
using System.Globalization;

namespace TypeScript.ContentMapper;
internal static class ContentMapperCodecs
{
    internal static readonly ProtocolType<InitializeParams> InitializeParamsValue = new("contentmapper.InitializeParams", ReadInitializeParamsValue, WriteInitializeParamsValue);
    internal static readonly ProtocolType<InitializeParams?> InitializeParamsPointer = new("*contentmapper.InitializeParams", ReadInitializeParamsPointer, WriteInitializeParamsPointer);
    private static void ReadInitializeParamsValue(ref ProtocolDecoder decoder, ref InitializeParams value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            value = new();
            return;
        }

        value ??= new();
        decoder.ReadObjectStart("contentmapper.InitializeParams");
        while (decoder.PeekKind() != '}')
        {
            var name = decoder.ReadPropertyName();
            if (name.EqualsBytes("locale"u8))
                value.Locale = decoder.ReadString();
            else if (name.EqualsBytes("positionEncodings"u8))
                value.PositionEncodings = ReadArray_encoding(ref decoder, value.PositionEncodings);
            else
                decoder.SkipValue();
        }

        decoder.ReadToken((byte)'}');
    }

    private static void WriteInitializeParamsValue(ProtocolEncoder encoder, InitializeParams value)
    {
        value ??= new();
        encoder.BeginObject();
        if (value.Locale.Length != 0)
        {
            encoder.WriteProperty("locale"u8);
            encoder.WriteString(value.Locale);
        }

        encoder.WriteProperty("positionEncodings"u8);
        WriteArray_encoding(encoder, value.PositionEncodings);
        encoder.EndObject();
    }

    private static void ReadInitializeParamsPointer(ref ProtocolDecoder decoder, ref InitializeParams? value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            value = null;
            return;
        }

        var current = value ?? new InitializeParams();
        ReadInitializeParamsValue(ref decoder, ref current);
        value = current;
    }

    private static void WriteInitializeParamsPointer(ProtocolEncoder encoder, InitializeParams? value)
    {
        if (value is null)
        {
            encoder.WriteNull();
            return;
        }

        WriteInitializeParamsValue(encoder, value);
    }

    internal static readonly ProtocolType<InitializeResult> InitializeResultValue = new("contentmapper.InitializeResult", ReadInitializeResultValue, WriteInitializeResultValue);
    internal static readonly ProtocolType<InitializeResult?> InitializeResultPointer = new("*contentmapper.InitializeResult", ReadInitializeResultPointer, WriteInitializeResultPointer);
    private static void ReadInitializeResultValue(ref ProtocolDecoder decoder, ref InitializeResult value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            value = new();
            return;
        }

        value ??= new();
        decoder.ReadObjectStart("contentmapper.InitializeResult");
        while (decoder.PeekKind() != '}')
        {
            var name = decoder.ReadPropertyName();
            if (name.EqualsBytes("positionEncoding"u8))
                value.PositionEncoding = new PositionEncoding(decoder.ReadString("contentmapper.PositionEncoding").Materialize() ?? "");
            else if (name.EqualsBytes("diagnosticSource"u8))
                value.DiagnosticSource = decoder.ReadString();
            else
                decoder.SkipValue();
        }

        decoder.ReadToken((byte)'}');
    }

    private static void WriteInitializeResultValue(ProtocolEncoder encoder, InitializeResult value)
    {
        value ??= new();
        encoder.BeginObject();
        encoder.WriteProperty("positionEncoding"u8);
        encoder.WriteString(ByteStringValue.FromString(value.PositionEncoding.AsString()));
        encoder.WriteProperty("diagnosticSource"u8);
        encoder.WriteString(value.DiagnosticSource);
        encoder.EndObject();
    }

    private static void ReadInitializeResultPointer(ref ProtocolDecoder decoder, ref InitializeResult? value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            value = null;
            return;
        }

        var current = value ?? new InitializeResult();
        ReadInitializeResultValue(ref decoder, ref current);
        value = current;
    }

    private static void WriteInitializeResultPointer(ProtocolEncoder encoder, InitializeResult? value)
    {
        if (value is null)
        {
            encoder.WriteNull();
            return;
        }

        WriteInitializeResultValue(encoder, value);
    }

    internal static readonly ProtocolType<OpenProjectParams> OpenProjectParamsValue = new("contentmapper.OpenProjectParams", ReadOpenProjectParamsValue, WriteOpenProjectParamsValue);
    internal static readonly ProtocolType<OpenProjectParams?> OpenProjectParamsPointer = new("*contentmapper.OpenProjectParams", ReadOpenProjectParamsPointer, WriteOpenProjectParamsPointer);
    private static void ReadOpenProjectParamsValue(ref ProtocolDecoder decoder, ref OpenProjectParams value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            value = new();
            return;
        }

        value ??= new();
        decoder.ReadObjectStart("contentmapper.OpenProjectParams");
        while (decoder.PeekKind() != '}')
        {
            var name = decoder.ReadPropertyName();
            if (name.EqualsBytes("configFileName"u8))
                value.ConfigFileName = decoder.ReadString();
            else if (name.EqualsBytes("projectHandle"u8))
                value.ProjectHandle = decoder.ReadString();
            else if (name.EqualsBytes("options"u8))
                value.Options = decoder.ReadValue().Clone();
            else if (name.EqualsBytes("compilerOptions"u8))
                value.CompilerOptions = decoder.ReadValue().Clone();
            else
                decoder.SkipValue();
        }

        decoder.ReadToken((byte)'}');
    }

    private static void WriteOpenProjectParamsValue(ProtocolEncoder encoder, OpenProjectParams value)
    {
        value ??= new();
        encoder.BeginObject();
        encoder.WriteProperty("configFileName"u8);
        encoder.WriteString(value.ConfigFileName);
        encoder.WriteProperty("projectHandle"u8);
        encoder.WriteString(value.ProjectHandle);
        WriteRawOmitEmpty(encoder, "options"u8, value.Options);
        encoder.WriteProperty("compilerOptions"u8);
        WriteRaw(encoder, value.CompilerOptions);
        encoder.EndObject();
    }

    private static void ReadOpenProjectParamsPointer(ref ProtocolDecoder decoder, ref OpenProjectParams? value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            value = null;
            return;
        }

        var current = value ?? new OpenProjectParams();
        ReadOpenProjectParamsValue(ref decoder, ref current);
        value = current;
    }

    private static void WriteOpenProjectParamsPointer(ProtocolEncoder encoder, OpenProjectParams? value)
    {
        if (value is null)
        {
            encoder.WriteNull();
            return;
        }

        WriteOpenProjectParamsValue(encoder, value);
    }

    internal static readonly ProtocolType<OpenProjectResult> OpenProjectResultValue = new("contentmapper.OpenProjectResult", ReadOpenProjectResultValue, WriteOpenProjectResultValue);
    internal static readonly ProtocolType<OpenProjectResult?> OpenProjectResultPointer = new("*contentmapper.OpenProjectResult", ReadOpenProjectResultPointer, WriteOpenProjectResultPointer);
    private static void ReadOpenProjectResultValue(ref ProtocolDecoder decoder, ref OpenProjectResult value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            value = new();
            return;
        }

        value ??= new();
        decoder.ReadObjectStart("contentmapper.OpenProjectResult");
        while (decoder.PeekKind() != '}')
        {
            var name = decoder.ReadPropertyName();
            if (name.EqualsBytes("configIdentity"u8))
                value.ConfigIdentity = decoder.ReadString();
            else if (name.EqualsBytes("watchedFiles"u8))
                value.WatchedFiles = ReadArray_string(ref decoder, value.WatchedFiles);
            else if (name.EqualsBytes("optionDiagnostics"u8))
                value.OptionDiagnostics = ReadArray_OptionDiagnosticResult(ref decoder, value.OptionDiagnostics);
            else
                decoder.SkipValue();
        }

        decoder.ReadToken((byte)'}');
    }

    private static void WriteOpenProjectResultValue(ProtocolEncoder encoder, OpenProjectResult value)
    {
        value ??= new();
        encoder.BeginObject();
        encoder.WriteProperty("configIdentity"u8);
        encoder.WriteString(value.ConfigIdentity);
        if (value.WatchedFiles.Count != 0)
        {
            encoder.WriteProperty("watchedFiles"u8);
            WriteArray_string(encoder, value.WatchedFiles);
        }

        if (value.OptionDiagnostics.Count != 0)
        {
            encoder.WriteProperty("optionDiagnostics"u8);
            WriteArray_OptionDiagnosticResult(encoder, value.OptionDiagnostics);
        }

        encoder.EndObject();
    }

    private static void ReadOpenProjectResultPointer(ref ProtocolDecoder decoder, ref OpenProjectResult? value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            value = null;
            return;
        }

        var current = value ?? new OpenProjectResult();
        ReadOpenProjectResultValue(ref decoder, ref current);
        value = current;
    }

    private static void WriteOpenProjectResultPointer(ProtocolEncoder encoder, OpenProjectResult? value)
    {
        if (value is null)
        {
            encoder.WriteNull();
            return;
        }

        WriteOpenProjectResultValue(encoder, value);
    }

    internal static readonly ProtocolType<OptionDiagnosticResult> OptionDiagnosticResultValue = new("contentmapper.OptionDiagnosticResult", ReadOptionDiagnosticResultValue, WriteOptionDiagnosticResultValue);
    internal static readonly ProtocolType<OptionDiagnosticResult?> OptionDiagnosticResultPointer = new("*contentmapper.OptionDiagnosticResult", ReadOptionDiagnosticResultPointer, WriteOptionDiagnosticResultPointer);
    private static void ReadOptionDiagnosticResultValue(ref ProtocolDecoder decoder, ref OptionDiagnosticResult value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            value = new();
            return;
        }

        value ??= new();
        decoder.ReadObjectStart("contentmapper.OptionDiagnosticResult");
        while (decoder.PeekKind() != '}')
        {
            var name = decoder.ReadPropertyName();
            if (name.EqualsBytes("path"u8))
                value.Path = ReadArray_raw(ref decoder, value.Path);
            else if (name.EqualsBytes("messageText"u8))
                value.MessageText = decoder.ReadString();
            else if (name.EqualsBytes("code"u8))
                value.Code = decoder.ReadInt32();
            else
                decoder.SkipValue();
        }

        decoder.ReadToken((byte)'}');
    }

    private static void WriteOptionDiagnosticResultValue(ProtocolEncoder encoder, OptionDiagnosticResult value)
    {
        value ??= new();
        encoder.BeginObject();
        encoder.WriteProperty("path"u8);
        WriteArray_raw(encoder, value.Path);
        encoder.WriteProperty("messageText"u8);
        encoder.WriteString(value.MessageText);
        encoder.WriteProperty("code"u8);
        WriteInt(encoder, value.Code);
        encoder.EndObject();
    }

    private static void ReadOptionDiagnosticResultPointer(ref ProtocolDecoder decoder, ref OptionDiagnosticResult? value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            value = null;
            return;
        }

        var current = value ?? new OptionDiagnosticResult();
        ReadOptionDiagnosticResultValue(ref decoder, ref current);
        value = current;
    }

    private static void WriteOptionDiagnosticResultPointer(ProtocolEncoder encoder, OptionDiagnosticResult? value)
    {
        if (value is null)
        {
            encoder.WriteNull();
            return;
        }

        WriteOptionDiagnosticResultValue(encoder, value);
    }

    internal static readonly ProtocolType<CloseProjectParams> CloseProjectParamsValue = new("contentmapper.CloseProjectParams", ReadCloseProjectParamsValue, WriteCloseProjectParamsValue);
    internal static readonly ProtocolType<CloseProjectParams?> CloseProjectParamsPointer = new("*contentmapper.CloseProjectParams", ReadCloseProjectParamsPointer, WriteCloseProjectParamsPointer);
    private static void ReadCloseProjectParamsValue(ref ProtocolDecoder decoder, ref CloseProjectParams value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            value = new();
            return;
        }

        value ??= new();
        decoder.ReadObjectStart("contentmapper.CloseProjectParams");
        while (decoder.PeekKind() != '}')
        {
            var name = decoder.ReadPropertyName();
            if (name.EqualsBytes("projectHandle"u8))
                value.ProjectHandle = decoder.ReadString();
            else
                decoder.SkipValue();
        }

        decoder.ReadToken((byte)'}');
    }

    private static void WriteCloseProjectParamsValue(ProtocolEncoder encoder, CloseProjectParams value)
    {
        value ??= new();
        encoder.BeginObject();
        encoder.WriteProperty("projectHandle"u8);
        encoder.WriteString(value.ProjectHandle);
        encoder.EndObject();
    }

    private static void ReadCloseProjectParamsPointer(ref ProtocolDecoder decoder, ref CloseProjectParams? value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            value = null;
            return;
        }

        var current = value ?? new CloseProjectParams();
        ReadCloseProjectParamsValue(ref decoder, ref current);
        value = current;
    }

    private static void WriteCloseProjectParamsPointer(ProtocolEncoder encoder, CloseProjectParams? value)
    {
        if (value is null)
        {
            encoder.WriteNull();
            return;
        }

        WriteCloseProjectParamsValue(encoder, value);
    }

    internal static readonly ProtocolType<TransformParams> TransformParamsValue = new("contentmapper.TransformParams", ReadTransformParamsValue, WriteTransformParamsValue);
    internal static readonly ProtocolType<TransformParams?> TransformParamsPointer = new("*contentmapper.TransformParams", ReadTransformParamsPointer, WriteTransformParamsPointer);
    private static void ReadTransformParamsValue(ref ProtocolDecoder decoder, ref TransformParams value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            value = new();
            return;
        }

        value ??= new();
        decoder.ReadObjectStart("contentmapper.TransformParams");
        while (decoder.PeekKind() != '}')
        {
            var name = decoder.ReadPropertyName();
            if (name.EqualsBytes("fileName"u8))
                value.FileName = decoder.ReadString();
            else if (name.EqualsBytes("content"u8))
                value.Content = decoder.ReadString();
            else if (name.EqualsBytes("projectHandle"u8))
                value.ProjectHandle = decoder.ReadString();
            else
                decoder.SkipValue();
        }

        decoder.ReadToken((byte)'}');
    }

    private static void WriteTransformParamsValue(ProtocolEncoder encoder, TransformParams value)
    {
        value ??= new();
        encoder.BeginObject();
        encoder.WriteProperty("fileName"u8);
        encoder.WriteString(value.FileName);
        encoder.WriteProperty("content"u8);
        encoder.WriteString(value.Content);
        encoder.WriteProperty("projectHandle"u8);
        encoder.WriteString(value.ProjectHandle);
        encoder.EndObject();
    }

    private static void ReadTransformParamsPointer(ref ProtocolDecoder decoder, ref TransformParams? value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            value = null;
            return;
        }

        var current = value ?? new TransformParams();
        ReadTransformParamsValue(ref decoder, ref current);
        value = current;
    }

    private static void WriteTransformParamsPointer(ProtocolEncoder encoder, TransformParams? value)
    {
        if (value is null)
        {
            encoder.WriteNull();
            return;
        }

        WriteTransformParamsValue(encoder, value);
    }

    internal static readonly ProtocolType<MappedOutput> MappedOutputValue = new("contentmapper.MappedOutput", ReadMappedOutputValue, WriteMappedOutputValue);
    internal static readonly ProtocolType<MappedOutput?> MappedOutputPointer = new("*contentmapper.MappedOutput", ReadMappedOutputPointer, WriteMappedOutputPointer);
    private static void ReadMappedOutputValue(ref ProtocolDecoder decoder, ref MappedOutput value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            value = new();
            return;
        }

        value ??= new();
        decoder.ReadObjectStart("contentmapper.MappedOutput");
        while (decoder.PeekKind() != '}')
        {
            var name = decoder.ReadPropertyName();
            if (name.EqualsBytes("text"u8))
                value.Text = decoder.ReadString();
            else if (name.EqualsBytes("extension"u8))
                value.Extension = decoder.ReadString();
            else if (name.EqualsBytes("mappings"u8))
                value.Mappings = decoder.ReadValue().Clone();
            else if (name.EqualsBytes("diagnosticDirectives"u8))
                ReadDiagnosticDirectivesPointer(ref decoder, ref value.DiagnosticDirectives);
            else
                decoder.SkipValue();
        }

        decoder.ReadToken((byte)'}');
    }

    private static void WriteMappedOutputValue(ProtocolEncoder encoder, MappedOutput value)
    {
        value ??= new();
        encoder.BeginObject();
        encoder.WriteProperty("text"u8);
        encoder.WriteString(value.Text);
        encoder.WriteProperty("extension"u8);
        encoder.WriteString(value.Extension);
        WriteRawOmitEmpty(encoder, "mappings"u8, value.Mappings);
        if (value.DiagnosticDirectives is not null)
        {
            encoder.WriteProperty("diagnosticDirectives"u8);
            WriteDiagnosticDirectivesPointer(encoder, value.DiagnosticDirectives);
        }

        encoder.EndObject();
    }

    private static void ReadMappedOutputPointer(ref ProtocolDecoder decoder, ref MappedOutput? value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            value = null;
            return;
        }

        var current = value ?? new MappedOutput();
        ReadMappedOutputValue(ref decoder, ref current);
        value = current;
    }

    private static void WriteMappedOutputPointer(ProtocolEncoder encoder, MappedOutput? value)
    {
        if (value is null)
        {
            encoder.WriteNull();
            return;
        }

        WriteMappedOutputValue(encoder, value);
    }

    internal static readonly ProtocolType<DiagnosticDirectives> DiagnosticDirectivesValue = new("contentmapper.DiagnosticDirectives", ReadDiagnosticDirectivesValue, WriteDiagnosticDirectivesValue);
    internal static readonly ProtocolType<DiagnosticDirectives?> DiagnosticDirectivesPointer = new("*contentmapper.DiagnosticDirectives", ReadDiagnosticDirectivesPointer, WriteDiagnosticDirectivesPointer);
    private static void ReadDiagnosticDirectivesValue(ref ProtocolDecoder decoder, ref DiagnosticDirectives value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            value = new();
            return;
        }

        value ??= new();
        decoder.ReadObjectStart("contentmapper.DiagnosticDirectives");
        while (decoder.PeekKind() != '}')
        {
            var name = decoder.ReadPropertyName();
            if (name.EqualsBytes("unusedExpectDirectiveDiagnostics"u8))
                value.UnusedExpectDirectiveDiagnostics = ReadArray_UnusedExpectDirectiveDiagnostic(ref decoder, value.UnusedExpectDirectiveDiagnostics);
            else if (name.EqualsBytes("directives"u8))
                value.Directives = ReadArray_MappedDiagnosticDirective(ref decoder, value.Directives);
            else
                decoder.SkipValue();
        }

        decoder.ReadToken((byte)'}');
    }

    private static void WriteDiagnosticDirectivesValue(ProtocolEncoder encoder, DiagnosticDirectives value)
    {
        value ??= new();
        encoder.BeginObject();
        encoder.WriteProperty("unusedExpectDirectiveDiagnostics"u8);
        WriteArray_UnusedExpectDirectiveDiagnostic(encoder, value.UnusedExpectDirectiveDiagnostics);
        encoder.WriteProperty("directives"u8);
        WriteArray_MappedDiagnosticDirective(encoder, value.Directives);
        encoder.EndObject();
    }

    private static void ReadDiagnosticDirectivesPointer(ref ProtocolDecoder decoder, ref DiagnosticDirectives? value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            value = null;
            return;
        }

        var current = value ?? new DiagnosticDirectives();
        ReadDiagnosticDirectivesValue(ref decoder, ref current);
        value = current;
    }

    private static void WriteDiagnosticDirectivesPointer(ProtocolEncoder encoder, DiagnosticDirectives? value)
    {
        if (value is null)
        {
            encoder.WriteNull();
            return;
        }

        WriteDiagnosticDirectivesValue(encoder, value);
    }

    internal static readonly ProtocolType<UnusedExpectDirectiveDiagnostic> UnusedExpectDirectiveDiagnosticValue = new("contentmapper.UnusedExpectDirectiveDiagnostic", ReadUnusedExpectDirectiveDiagnosticValue, WriteUnusedExpectDirectiveDiagnosticValue);
    private static void ReadUnusedExpectDirectiveDiagnosticValue(ref ProtocolDecoder decoder, ref UnusedExpectDirectiveDiagnostic value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            value = default;
            return;
        }

        decoder.ReadObjectStart("contentmapper.UnusedExpectDirectiveDiagnostic");
        while (decoder.PeekKind() != '}')
        {
            var name = decoder.ReadPropertyName();
            if (name.EqualsBytes("code"u8))
                value.Code = decoder.ReadInt32();
            else if (name.EqualsBytes("messageText"u8))
                value.MessageText = decoder.ReadString();
            else
                decoder.SkipValue();
        }

        decoder.ReadToken((byte)'}');
    }

    private static void WriteUnusedExpectDirectiveDiagnosticValue(ProtocolEncoder encoder, UnusedExpectDirectiveDiagnostic value)
    {
        encoder.BeginObject();
        encoder.WriteProperty("code"u8);
        WriteInt(encoder, value.Code);
        encoder.WriteProperty("messageText"u8);
        encoder.WriteString(value.MessageText);
        encoder.EndObject();
    }

    internal static readonly ProtocolType<SupplementalOutput> SupplementalOutputValue = new("contentmapper.SupplementalOutput", ReadSupplementalOutputValue, WriteSupplementalOutputValue);
    internal static readonly ProtocolType<SupplementalOutput?> SupplementalOutputPointer = new("*contentmapper.SupplementalOutput", ReadSupplementalOutputPointer, WriteSupplementalOutputPointer);
    private static void ReadSupplementalOutputValue(ref ProtocolDecoder decoder, ref SupplementalOutput value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            value = new();
            return;
        }

        value ??= new();
        decoder.ReadObjectStart("contentmapper.SupplementalOutput");
        while (decoder.PeekKind() != '}')
        {
            var name = decoder.ReadPropertyName();
            if (name.EqualsBytes("text"u8))
                value.Text = decoder.ReadString();
            else if (name.EqualsBytes("extension"u8))
                value.Extension = decoder.ReadString();
            else if (name.EqualsBytes("mappings"u8))
                value.Mappings = decoder.ReadValue().Clone();
            else if (name.EqualsBytes("diagnosticDirectives"u8))
                ReadDiagnosticDirectivesPointer(ref decoder, ref value.DiagnosticDirectives);
            else
                decoder.SkipValue();
        }

        decoder.ReadToken((byte)'}');
    }

    private static void WriteSupplementalOutputValue(ProtocolEncoder encoder, SupplementalOutput value)
    {
        value ??= new();
        encoder.BeginObject();
        encoder.WriteProperty("text"u8);
        encoder.WriteString(value.Text);
        encoder.WriteProperty("extension"u8);
        encoder.WriteString(value.Extension);
        WriteRawOmitEmpty(encoder, "mappings"u8, value.Mappings);
        if (value.DiagnosticDirectives is not null)
        {
            encoder.WriteProperty("diagnosticDirectives"u8);
            WriteDiagnosticDirectivesPointer(encoder, value.DiagnosticDirectives);
        }

        encoder.EndObject();
    }

    private static void ReadSupplementalOutputPointer(ref ProtocolDecoder decoder, ref SupplementalOutput? value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            value = null;
            return;
        }

        var current = value ?? new SupplementalOutput();
        ReadSupplementalOutputValue(ref decoder, ref current);
        value = current;
    }

    private static void WriteSupplementalOutputPointer(ProtocolEncoder encoder, SupplementalOutput? value)
    {
        if (value is null)
        {
            encoder.WriteNull();
            return;
        }

        WriteSupplementalOutputValue(encoder, value);
    }

    internal static readonly ProtocolType<TransformResult> TransformResultValue = new("contentmapper.TransformResult", ReadTransformResultValue, WriteTransformResultValue);
    internal static readonly ProtocolType<TransformResult?> TransformResultPointer = new("*contentmapper.TransformResult", ReadTransformResultPointer, WriteTransformResultPointer);
    private static void ReadTransformResultValue(ref ProtocolDecoder decoder, ref TransformResult value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            value = new();
            return;
        }

        value ??= new();
        decoder.ReadObjectStart("contentmapper.TransformResult");
        while (decoder.PeekKind() != '}')
        {
            var name = decoder.ReadPropertyName();
            if (name.EqualsBytes("text"u8))
                value.Text = decoder.ReadString();
            else if (name.EqualsBytes("extension"u8))
                value.Extension = decoder.ReadString();
            else if (name.EqualsBytes("mappings"u8))
                value.Mappings = decoder.ReadValue().Clone();
            else if (name.EqualsBytes("diagnosticDirectives"u8))
                ReadDiagnosticDirectivesPointer(ref decoder, ref value.DiagnosticDirectives);
            else if (name.EqualsBytes("diagnostics"u8))
                value.Diagnostics = ReadArray_Diagnostic(ref decoder, value.Diagnostics);
            else if (name.EqualsBytes("supplemental"u8))
                value.Supplemental = ReadArray_SupplementalOutput(ref decoder, value.Supplemental);
            else
                decoder.SkipValue();
        }

        decoder.ReadToken((byte)'}');
    }

    private static void WriteTransformResultValue(ProtocolEncoder encoder, TransformResult value)
    {
        value ??= new();
        encoder.BeginObject();
        encoder.WriteProperty("text"u8);
        encoder.WriteString(value.Text);
        encoder.WriteProperty("extension"u8);
        encoder.WriteString(value.Extension);
        WriteRawOmitEmpty(encoder, "mappings"u8, value.Mappings);
        if (value.DiagnosticDirectives is not null)
        {
            encoder.WriteProperty("diagnosticDirectives"u8);
            WriteDiagnosticDirectivesPointer(encoder, value.DiagnosticDirectives);
        }

        if (value.Diagnostics.Count != 0)
        {
            encoder.WriteProperty("diagnostics"u8);
            WriteArray_Diagnostic(encoder, value.Diagnostics);
        }

        if (value.Supplemental.Count != 0)
        {
            encoder.WriteProperty("supplemental"u8);
            WriteArray_SupplementalOutput(encoder, value.Supplemental);
        }

        encoder.EndObject();
    }

    private static void ReadTransformResultPointer(ref ProtocolDecoder decoder, ref TransformResult? value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            value = null;
            return;
        }

        var current = value ?? new TransformResult();
        ReadTransformResultValue(ref decoder, ref current);
        value = current;
    }

    private static void WriteTransformResultPointer(ProtocolEncoder encoder, TransformResult? value)
    {
        if (value is null)
        {
            encoder.WriteNull();
            return;
        }

        WriteTransformResultValue(encoder, value);
    }

    internal static readonly ProtocolType<WireDiagnostic> DiagnosticValue = new("contentmapper.Diagnostic", ReadDiagnosticValue, WriteDiagnosticValue);
    private static void ReadDiagnosticValue(ref ProtocolDecoder decoder, ref WireDiagnostic value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            value = default;
            return;
        }

        decoder.ReadObjectStart("contentmapper.Diagnostic");
        while (decoder.PeekKind() != '}')
        {
            var name = decoder.ReadPropertyName();
            if (name.EqualsBytes("messageText"u8))
                value.MessageText = decoder.ReadString();
            else if (name.EqualsBytes("start"u8))
                value.Start = ReadInt(ref decoder);
            else if (name.EqualsBytes("length"u8))
                value.Length = ReadInt(ref decoder);
            else if (name.EqualsBytes("code"u8))
                value.Code = decoder.ReadInt32();
            else
                decoder.SkipValue();
        }

        decoder.ReadToken((byte)'}');
    }

    private static void WriteDiagnosticValue(ProtocolEncoder encoder, WireDiagnostic value)
    {
        encoder.BeginObject();
        encoder.WriteProperty("messageText"u8);
        encoder.WriteString(value.MessageText);
        encoder.WriteProperty("start"u8);
        WriteInt(encoder, value.Start);
        encoder.WriteProperty("length"u8);
        WriteInt(encoder, value.Length);
        encoder.WriteProperty("code"u8);
        WriteInt(encoder, value.Code);
        encoder.EndObject();
    }

    private static Slice<WireDiagnostic> ReadArray_Diagnostic(ref ProtocolDecoder decoder, Slice<WireDiagnostic> value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            return default;
        }

        decoder.ReadArrayStart("[]contentmapper.Diagnostic");
        var result = value.IsNil ? Slice<WireDiagnostic>.Make(0) : value.Slice2(0, 0);
        while (decoder.PeekKind() != ']')
        {
            WireDiagnostic item = default;
            ReadDiagnosticValue(ref decoder, ref item);
            result = result.Append(item);
        }

        decoder.ReadToken((byte)']');
        return result;
    }

    private static void WriteArray_Diagnostic(ProtocolEncoder encoder, Slice<WireDiagnostic> value)
    {
        if (value.IsNil)
        {
            encoder.WriteNull();
            return;
        }

        encoder.BeginArray();
        foreach (var item in value)
            WriteDiagnosticValue(encoder, item);
        encoder.EndArray();
    }

    private static Slice<WireMappedDiagnosticDirective> ReadArray_MappedDiagnosticDirective(ref ProtocolDecoder decoder, Slice<WireMappedDiagnosticDirective> value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            return default;
        }

        decoder.ReadArrayStart("[]contentmapper.MappedDiagnosticDirective");
        var result = value.IsNil ? Slice<WireMappedDiagnosticDirective>.Make(0) : value.Slice2(0, 0);
        while (decoder.PeekKind() != ']')
        {
            WireMappedDiagnosticDirective item = default;
            ReadMappedDiagnosticDirectiveValue(ref decoder, ref item);
            result = result.Append(item);
        }

        decoder.ReadToken((byte)']');
        return result;
    }

    private static void WriteArray_MappedDiagnosticDirective(ProtocolEncoder encoder, Slice<WireMappedDiagnosticDirective> value)
    {
        if (value.IsNil)
        {
            encoder.WriteNull();
            return;
        }

        encoder.BeginArray();
        foreach (var item in value)
            WriteMappedDiagnosticDirectiveValue(encoder, item);
        encoder.EndArray();
    }

    private static Slice<OptionDiagnosticResult> ReadArray_OptionDiagnosticResult(ref ProtocolDecoder decoder, Slice<OptionDiagnosticResult> value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            return default;
        }

        decoder.ReadArrayStart("[]contentmapper.OptionDiagnosticResult");
        var result = value.IsNil ? Slice<OptionDiagnosticResult>.Make(0) : value.Slice2(0, 0);
        while (decoder.PeekKind() != ']')
        {
            OptionDiagnosticResult item = default!;
            ReadOptionDiagnosticResultValue(ref decoder, ref item);
            result = result.Append(item);
        }

        decoder.ReadToken((byte)']');
        return result;
    }

    private static void WriteArray_OptionDiagnosticResult(ProtocolEncoder encoder, Slice<OptionDiagnosticResult> value)
    {
        if (value.IsNil)
        {
            encoder.WriteNull();
            return;
        }

        encoder.BeginArray();
        foreach (var item in value)
            WriteOptionDiagnosticResultValue(encoder, item);
        encoder.EndArray();
    }

    private static Slice<SupplementalOutput> ReadArray_SupplementalOutput(ref ProtocolDecoder decoder, Slice<SupplementalOutput> value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            return default;
        }

        decoder.ReadArrayStart("[]contentmapper.SupplementalOutput");
        var result = value.IsNil ? Slice<SupplementalOutput>.Make(0) : value.Slice2(0, 0);
        while (decoder.PeekKind() != ']')
        {
            SupplementalOutput item = default!;
            ReadSupplementalOutputValue(ref decoder, ref item);
            result = result.Append(item);
        }

        decoder.ReadToken((byte)']');
        return result;
    }

    private static void WriteArray_SupplementalOutput(ProtocolEncoder encoder, Slice<SupplementalOutput> value)
    {
        if (value.IsNil)
        {
            encoder.WriteNull();
            return;
        }

        encoder.BeginArray();
        foreach (var item in value)
            WriteSupplementalOutputValue(encoder, item);
        encoder.EndArray();
    }

    private static Slice<UnusedExpectDirectiveDiagnostic> ReadArray_UnusedExpectDirectiveDiagnostic(ref ProtocolDecoder decoder, Slice<UnusedExpectDirectiveDiagnostic> value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            return default;
        }

        decoder.ReadArrayStart("[]contentmapper.UnusedExpectDirectiveDiagnostic");
        var result = value.IsNil ? Slice<UnusedExpectDirectiveDiagnostic>.Make(0) : value.Slice2(0, 0);
        while (decoder.PeekKind() != ']')
        {
            UnusedExpectDirectiveDiagnostic item = default;
            ReadUnusedExpectDirectiveDiagnosticValue(ref decoder, ref item);
            result = result.Append(item);
        }

        decoder.ReadToken((byte)']');
        return result;
    }

    private static void WriteArray_UnusedExpectDirectiveDiagnostic(ProtocolEncoder encoder, Slice<UnusedExpectDirectiveDiagnostic> value)
    {
        if (value.IsNil)
        {
            encoder.WriteNull();
            return;
        }

        encoder.BeginArray();
        foreach (var item in value)
            WriteUnusedExpectDirectiveDiagnosticValue(encoder, item);
        encoder.EndArray();
    }

    private static Slice<PositionEncoding> ReadArray_encoding(ref ProtocolDecoder decoder, Slice<PositionEncoding> value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            return default;
        }

        decoder.ReadArrayStart("[]contentmapper.PositionEncoding");
        var result = value.IsNil ? Slice<PositionEncoding>.Make(0) : value.Slice2(0, 0);
        while (decoder.PeekKind() != ']')
        {
            PositionEncoding item = default;
            item = new PositionEncoding(decoder.ReadString("contentmapper.PositionEncoding").Materialize() ?? "");
            result = result.Append(item);
        }

        decoder.ReadToken((byte)']');
        return result;
    }

    private static void WriteArray_encoding(ProtocolEncoder encoder, Slice<PositionEncoding> value)
    {
        if (value.IsNil)
        {
            encoder.WriteNull();
            return;
        }

        encoder.BeginArray();
        foreach (var item in value)
            encoder.WriteString(ByteStringValue.FromString(item.AsString()));
        encoder.EndArray();
    }

    private static Slice<RawJsonValue?> ReadArray_raw(ref ProtocolDecoder decoder, Slice<RawJsonValue?> value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            return default;
        }

        decoder.ReadArrayStart("[]json.Value");
        var result = value.IsNil ? Slice<RawJsonValue?>.Make(0) : value.Slice2(0, 0);
        while (decoder.PeekKind() != ']')
        {
            RawJsonValue? item = default;
            item = decoder.ReadValue().Clone();
            result = result.Append(item);
        }

        decoder.ReadToken((byte)']');
        return result;
    }

    private static void WriteArray_raw(ProtocolEncoder encoder, Slice<RawJsonValue?> value)
    {
        if (value.IsNil)
        {
            encoder.WriteNull();
            return;
        }

        encoder.BeginArray();
        foreach (var item in value)
            WriteRaw(encoder, item);
        encoder.EndArray();
    }

    private static Slice<ByteStringValue> ReadArray_string(ref ProtocolDecoder decoder, Slice<ByteStringValue> value)
    {
        if (decoder.PeekKind() == 'n')
        {
            decoder.ReadToken((byte)'n');
            return default;
        }

        decoder.ReadArrayStart("[]string");
        var result = value.IsNil ? Slice<ByteStringValue>.Make(0) : value.Slice2(0, 0);
        while (decoder.PeekKind() != ']')
        {
            ByteStringValue item = default;
            item = decoder.ReadString();
            result = result.Append(item);
        }

        decoder.ReadToken((byte)']');
        return result;
    }

    private static void WriteArray_string(ProtocolEncoder encoder, Slice<ByteStringValue> value)
    {
        if (value.IsNil)
        {
            encoder.WriteNull();
            return;
        }

        encoder.BeginArray();
        foreach (var item in value)
            encoder.WriteString(item);
        encoder.EndArray();
    }

    private static ByteStringValue FormatRaw(RawJsonValue? value)
    {
        if (value is null || value.IsNil)
            return ByteStringValue.FromString("null");
        var(bytes, error) = TypeScript.Json.JsonCodec.FormatRawValue(new TypeScript.Json.JsonValue(value.BorrowOwnedBytes()), allowInvalidUtf8: true);
        if (error is not null)
            throw error;
        return ByteStringValue.FromView(ByteStringView.FromOwnedBytes(bytes!));
    }

    private static void WriteRaw(ProtocolEncoder encoder, RawJsonValue? value)
    {
        var formatted = FormatRaw(value);
        if (formatted.IsBytes)
            encoder.WriteRaw(formatted.ByteSpan);
        else
            encoder.WriteNull(); // Only the source zero json.Value takes this branch.
    }

    private static void WriteRawOmitEmpty(ProtocolEncoder encoder, ReadOnlySpan<byte> name, RawJsonValue? value)
    {
        var formatted = FormatRaw(value);
        if (formatted.EqualsBytes("null"u8) || formatted.EqualsBytes("\"\""u8) || formatted.EqualsBytes("[]"u8) || formatted.EqualsBytes("{}"u8))
            return;
        encoder.WriteProperty(name);
        encoder.WriteRaw(formatted.ByteSpan);
    }

    private static void WriteInt(ProtocolEncoder encoder, long value)
    {
        Span<byte> digits = stackalloc byte[20];
        if (!System.Buffers.Text.Utf8Formatter.TryFormat(value, digits, out int length))
            throw new InvalidOperationException("int decimal buffer too small");
        encoder.WriteRaw(digits[..length]);
    }

    private static long ReadInt(ref ProtocolDecoder decoder)
    {
        var raw = decoder.ReadValue().TextValue;
        if (raw.EqualsBytes("null"u8))
            return 0;
        var text = raw.ByteSpan;
        if (text.Length == 0 || text[0] != (byte)'-' && (text[0] < (byte)'0' || text[0] > (byte)'9'))
            throw decoder.ErrorAfter("int");
        bool negative = text[0] == '-';
        ulong magnitude = 0;
        bool overflow = false;
        for (int i = negative ? 1 : 0; i < text.Length; i++)
        {
            uint digit = (uint)(text[i] - (byte)'0');
            if (digit > 9)
                throw decoder.ErrorAfter("int", "invalid syntax", true);
            if (magnitude > (ulong.MaxValue - digit) / 10)
                overflow = true;
            magnitude = unchecked(magnitude * 10 + digit);
        }

        if (overflow || magnitude > (negative ? 9223372036854775808UL : 9223372036854775807UL))
            throw decoder.ErrorAfter("int", "value out of range", true);
        return negative ? unchecked(-(long)magnitude) : (long)magnitude;
    }

    // hostimpl.go:156–193. The custom tuple first owns each json.Value and then
    // decodes its elements in order, preserving length/error admission ordering.
    private static readonly ProtocolType<long> IntValue = new("int", ReadIntValue, WriteInt);
    internal static long DecodeInt(ByteStringValue text)
    {
        long value = 0;
        ProtocolCodec.Unmarshal(IntValue, text, ref value);
        return value;
    }

    private static void ReadIntValue(ref ProtocolDecoder decoder, ref long value) => value = ReadInt(ref decoder);
    private static readonly ProtocolType<DiagnosticDirectivePolicy> PolicyValue = new("contentmapper.DiagnosticDirectivePolicy", ReadPolicy, WritePolicy);
    private static void ReadPolicy(ref ProtocolDecoder decoder, ref DiagnosticDirectivePolicy value)
    {
        uint numeric = decoder.ReadUint32("contentmapper.DiagnosticDirectivePolicy");
        if (numeric > byte.MaxValue)
            throw decoder.ErrorAfter("contentmapper.DiagnosticDirectivePolicy", "value out of range", true);
        value = (DiagnosticDirectivePolicy)numeric;
    }

    private static void WritePolicy(ProtocolEncoder encoder, DiagnosticDirectivePolicy value) => encoder.WriteNumber((uint)(byte)value);
    internal static readonly ProtocolType<WireMappedDiagnosticDirective> MappedDiagnosticDirectiveValue = new("contentmapper.MappedDiagnosticDirective", ReadMappedDiagnosticDirectiveValue, WriteMappedDiagnosticDirectiveValue);
    private static void ReadMappedDiagnosticDirectiveValue(ref ProtocolDecoder decoder, ref WireMappedDiagnosticDirective value)
    {
        Slice<RawJsonValue?> tuple = ReadArray_raw(ref decoder, default);
        if (tuple.Count != 5 && tuple.Count != 6)
            throw new System.Text.Json.JsonException("diagnostic directive tuple must contain 5 or 6 elements, got " + tuple.Count.ToString(CultureInfo.InvariantCulture));
        value = default;
        for (int i = 0; i < 5; i++)
        {
            try
            {
                var text = tuple[i]!.TextValue;
                switch (i)
                {
                    case 0:
                        ProtocolCodec.Unmarshal(IntValue, text, ref value.OriginalStart);
                        break;
                    case 1:
                        ProtocolCodec.Unmarshal(IntValue, text, ref value.OriginalLength);
                        break;
                    case 2:
                        ProtocolCodec.Unmarshal(IntValue, text, ref value.VirtualStart);
                        break;
                    case 3:
                        ProtocolCodec.Unmarshal(IntValue, text, ref value.VirtualEnd);
                        break;
                    case 4:
                        ProtocolCodec.Unmarshal(PolicyValue, text, ref value.Policy);
                        break;
                }
            }
            catch (Exception error)
            {
                throw new System.Text.Json.JsonException("invalid diagnostic directive tuple element " + i.ToString(CultureInfo.InvariantCulture) + ": " + error.Message, error);
            }
        }

        if (tuple.Count == 6)
        {
            long index = 0;
            value.UnusedExpectDirectiveIndex = index;
            try
            {
                ProtocolCodec.Unmarshal(IntValue, tuple[5]!.TextValue, ref index);
                value.UnusedExpectDirectiveIndex = index;
            }
            catch (Exception error)
            {
                throw new System.Text.Json.JsonException("invalid diagnostic directive tuple element 5: " + error.Message, error);
            }
        }
    }

    private static void WriteMappedDiagnosticDirectiveValue(ProtocolEncoder encoder, WireMappedDiagnosticDirective value)
    {
        encoder.BeginArray();
        WriteInt(encoder, value.OriginalStart);
        WriteInt(encoder, value.OriginalLength);
        WriteInt(encoder, value.VirtualStart);
        WriteInt(encoder, value.VirtualEnd);
        WritePolicy(encoder, value.Policy);
        if (value.UnusedExpectDirectiveIndex is { } index)
            WriteInt(encoder, index);
        encoder.EndArray();
    }
}
