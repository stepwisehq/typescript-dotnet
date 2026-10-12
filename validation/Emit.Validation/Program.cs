using Emit.Checks;

int originalFunctions = SourceMapChecks.RunAll();
originalFunctions += NameGeneratorChecks.RunAll();
Console.WriteLine($"Original emit test functions executed: {originalFunctions}");
int authoredControls = SourceMapChecks.AuthoredCaseCount + FoundationChecks.RunAll();
Console.WriteLine($"Authored emit controls executed: {authoredControls}");
