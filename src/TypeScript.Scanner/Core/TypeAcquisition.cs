// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/core/typeacquisition.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.Scanner;
public sealed partial class TypeAcquisition
{
    public bool Equals(TypeAcquisition? other) => ReferenceEquals(this, other) || other != null && Enable == other.Enable && OptionModelEquality.SlicesEqual(Include, other.Include) && OptionModelEquality.SlicesEqual(Exclude, other.Exclude) && DisableFilenameBasedTypeAcquisition == other.DisableFilenameBasedTypeAcquisition;
}
