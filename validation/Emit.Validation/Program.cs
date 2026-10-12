using Emit.Checks;

int originalFunctions = SourceMapChecks.RunAll();
originalFunctions += NameGeneratorChecks.RunAll();
originalFunctions += PrinterChecks.RunAll();
originalFunctions += PrinterUtilityChecks.RunAll();
originalFunctions += TypeEraserChecks.RunAll();
originalFunctions += ImportElisionChecks.RunAll();
originalFunctions += OrderedSetChecks.RunAll();
Console.WriteLine($"Original emit test functions executed: {originalFunctions}");
int authoredControls = SourceMapChecks.AuthoredCaseCount + FoundationChecks.RunAll() + ResolverChecks.RunAll() + PrinterUtilityChecks.RunAuthoredControls() + FormatUintControls.RunAll() + PrinterCommentControls.RunAll() + MetadataChecks.RunAll() + TsRuntimeChecks.RunAll() + MultiMapControls.RunAll() + EsRuntimeChecks.RunAll() + ClassFieldsChecks.RunAll() + EsDecoratorChecks.RunAll() + OrderedSetChecks.RunAuthoredControls();
Console.WriteLine($"Authored emit controls executed: {authoredControls}");
