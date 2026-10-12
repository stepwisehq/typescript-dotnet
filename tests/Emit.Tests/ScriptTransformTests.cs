using Emit.Checks;
using Xunit;

namespace Emit.Tests;
public sealed class ScriptTransformTests
{
    public static IEnumerable<object[]> ModuleCases => ModuleSharedChecks.Names.Select(name => new object[] { name });
    public static IEnumerable<object[]> CommonJSCases => CommonJSChecks.Names.Select(name => new object[] { name });
    public static IEnumerable<object[]> JsxInlinerCases => JsxInlinerChecks.Names.Select(name => new object[] { name });

    [Theory, MemberData(nameof(ModuleCases))]
    public void AuthoredSharedModules(string name) => ModuleSharedChecks.Run(name);
    [Theory, MemberData(nameof(CommonJSCases))]
    public void AuthoredCommonJS(string name) => CommonJSChecks.Run(name);
    [Theory, MemberData(nameof(JsxInlinerCases))]
    public void AuthoredJSXInlining(string name) => JsxInlinerChecks.Run(name);
}
