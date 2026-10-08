// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/core/tristate.go enum.
namespace TypeScript.Scanner;
public enum Tristate : byte
{
    Unknown = 0,
    False = 1,
    True = 2
}
