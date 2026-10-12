// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class SelectionRangeTests
{
    [Fact]
    public void TestSelectionRangeDepthIsLimited() => SelectionRangeChecks.TestSelectionRangeDepthIsLimited();
}
