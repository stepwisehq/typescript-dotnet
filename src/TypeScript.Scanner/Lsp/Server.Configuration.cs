// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Pinned internal/lsp/server.go:836–895. Ignored full configuration request owner.
using TypeScript.LanguageServices.Utilities;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;

namespace TypeScript.Lsp;
internal sealed partial class Server
{
    internal (UserPreferences Value, Exception? Error) RequestConfiguration(ProtocolRequestContext context)
    {
        if (!context.ClientCapabilities.Workspace.Configuration)
        {
            if (initializationOptions!.UserPreferences is { } pointer)
            {
                var preferences = pointer.Value;
                logger.Logf("received formatting options from initialization: %T\n%+v", preferences, preferences);
                if (preferences is Dictionary<string, object> config)
                    return (LsUtil.ParseUserPreferences(new Dictionary<string, object> { ["js/ts"] = config }), null);
            }

            return (LsUtil.NewDefaultUserPreferences(), null);
        }

        var(configs, error) = SendClientRequest(context, Method.WorkspaceConfigurationInfo, new ConfigurationParams { Items = Slice<ConfigurationItem?>.FromArray([new ConfigurationItem { Section = ByteStringValue.FromString("js/ts") }, new ConfigurationItem { Section = ByteStringValue.FromString("typescript") }, new ConfigurationItem { Section = ByteStringValue.FromString("javascript") }, new ConfigurationItem { Section = ByteStringValue.FromString("editor") }]) });
        if (error is not null)
            return (default, new InvalidOperationException("configure request failed: " + error.Message, error));
        var configMap = new Dictionary<string, object>();
        for (int index = 0; index < configs.Length; index++)
        {
            switch (index)
            {
                case 0:
                    configMap["js/ts"] = configs[index]!;
                    break;
                case 1:
                    configMap["typescript"] = configs[index]!;
                    break;
                case 2:
                    configMap["javascript"] = configs[index]!;
                    break;
                case 3:
                    configMap["editor"] = configs[index]!;
                    break;
            }
        }

        logger.Logf("received options from workspace/configuration request:\njs/ts: %+v\n\ntypescript: %+v\n\njavascript: %+v\n\neditor: %+v\n", configMap.GetValueOrDefault("js/ts"), configMap.GetValueOrDefault("typescript"), configMap.GetValueOrDefault("javascript"), configMap.GetValueOrDefault("editor"));
        return (LsUtil.ParseUserPreferences(configMap), null);
    }
}
