// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified complete C# port of tsc/internal/packagejson/validated.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.PackageJson;
public interface TypeValidatedField
{
    bool IsPresent();
    bool IsValid();
    string ExpectedJSONType();
    string ActualJSONType();
}
