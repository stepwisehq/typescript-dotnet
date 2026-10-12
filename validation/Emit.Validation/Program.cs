using Emit.Checks;
using System.Globalization;

if (args.Length > 0 && args[0] == "--benchmark")
{
    if (args.Length != 3 || !int.TryParse(args[2], NumberStyles.None, CultureInfo.InvariantCulture, out int iterations) || iterations <= 0)
    {
        Console.Error.WriteLine("Usage: --benchmark NAME ITERATIONS (positive integer)");
        return 2;
    }

    var observation = EmitBenchmarkChecks.Run(args[1], iterations);
    Console.WriteLine(FormattableString.Invariant($"Original emit workload observation: {observation.Name}; iterations={observation.Iterations}; elapsedMilliseconds={observation.ElapsedMilliseconds:R}; allocatedBytes={observation.AllocatedBytes}"));
    return 0;
}

int originalFunctions = SourceMapChecks.RunAll();
originalFunctions += NameGeneratorChecks.RunAll();
originalFunctions += PrinterChecks.RunAll();
originalFunctions += PrinterUtilityChecks.RunAll();
originalFunctions += TypeEraserChecks.RunAll();
originalFunctions += ImportElisionChecks.RunAll();
originalFunctions += OrderedSetChecks.RunAll();
originalFunctions += CommandLoggerChecks.RunAll();
Console.WriteLine($"Original emit test functions executed: {originalFunctions}");
int authoredControls = SourceMapChecks.AuthoredCaseCount + FoundationChecks.RunAll() + ResolverChecks.RunAll() + PrinterUtilityChecks.RunAuthoredControls() + FormatUintControls.RunAll() + PrinterCommentControls.RunAll() + MetadataChecks.RunAll() + TsRuntimeChecks.RunAll() + MultiMapControls.RunAll() + EsRuntimeChecks.RunAll() + ClassFieldsChecks.RunAll() + EsDecoratorChecks.RunAll() + OrderedSetChecks.RunAuthoredControls() + ModuleSharedChecks.RunAll() + CommonJSChecks.RunAll() + JsxInlinerChecks.RunAll() + EmitWriteFailureChecks.RunAll() + CommandLoggerChecks.RunAuthoredControls() + EmitModeChecks.RunAll();
Console.WriteLine($"Authored emit controls executed: {authoredControls}");
return 0;
