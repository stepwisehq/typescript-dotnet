// Whole original project/customconfigfilename_test.go, eight sibling groups.
using System.Diagnostics.CodeAnalysis;
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using TypeScript.LanguageServices.Utilities;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static class ProjectCustomConfigChecks
{
    private static void Check([DoesNotReturnIf(false)] bool value)
    {
        if (!value)
            throw new InvalidOperationException("original custom config assertion failed");
    }

    public static int RunAll()
    {
        Original();
        return 1;
    }

    public static void Original()
    {
        string[] names = ["picks up custom config and switches on preference change", "uses tsconfig.json when customConfigFileName is empty", "falls back to tsconfig.json when custom config missing", "reverts to tsconfig.json when custom config preference is cleared", "schedules diagnostics refresh when custom config preference changes", "rejects path traversal in customConfigFileName", "accepts plain base file names in customConfigFileName", "cleans up inferred project when custom config covers file"];
        List<Exception>? failures = null;
        foreach (var name in names)
        {
            try
            {
                if (name.StartsWith("rejects", StringComparison.Ordinal) || name.StartsWith("accepts", StringComparison.Ordinal))
                {
                    bool invalid = name.StartsWith("rejects", StringComparison.Ordinal);
                    string[] values = invalid ? ["/etc/passwd", "../tsconfig.json", "configs/tsconfig.all.json", "..\\tsconfig.json", "sub\\dir\\tsconfig.json", "..", "."] : ["tsconfig.all.json", "tsconfig.editor.json", "jsconfig.custom.json"];
                    foreach (string value in values)
                    {
                        var prefs = LsUtil.ParseUserPreferences(new() { ["js/ts"] = new Dictionary<string, object> { ["customConfigFileName"] = value } });
                        Check(prefs.CustomConfigFileName == (invalid ? "" : value));
                    }

                    continue;
                }

                bool inferred = name.StartsWith("cleans", StringComparison.Ordinal);
                Input[] files = inferred ? [new("/src/tsconfig.all.json", "{\"compilerOptions\": {\"strict\": true}, \"include\": [\"./**/*\"]}"), new("/src/index.ts", "export const x = 1;")] : [new("/src/tsconfig.json", "{\"compilerOptions\": {\"strict\": false}}"), new("/src/tsconfig.all.json", "{\"compilerOptions\": {\"strict\": true}}"), new("/src/index.ts", "export const x = 1;")];
                using var fixture = new ProjectSessionFixture(files);
                var session = fixture.Session;
                var ctx = ProtocolRequestContext.Default;
                var uri = new DocumentUri(ByteStringValue.FromString("file:///src/index.ts"));
                void Configure(string config)
                {
                    var prefs = LsUtil.NewDefaultUserPreferences();
                    prefs.CustomConfigFileName = config;
                    session.Configure(prefs);
                }

                if (name.StartsWith("uses", StringComparison.Ordinal))
                {
                    var prefs = LsUtil.NewDefaultUserPreferences();
                    Check(prefs.CustomConfigFileName == "");
                    session.Configure(prefs);
                }

                if (name.StartsWith("falls", StringComparison.Ordinal))
                    Configure("tsconfig.nonexistent.json");
                session.DidOpenFile(ctx, uri, 1, ByteStringValue.FromString("export const x = 1;"), LanguageKind.TypeScript);
                var(service, error) = session.GetLanguageService(ctx, uri);
                Check(error is null);
                Check(service is not null);
                var project = session.Snapshot().GetDefaultProject(uri);
                Check(project is not null);
                if (inferred)
                {
                    Check(project.ID().Value.EqualsString("/dev/null/inferred"));
                    Check(session.Snapshot().GetLanguageServiceProjectsContainingFile(uri).Length == 1);
                }
                else
                    Check(project.ConfigFileName().AsValue().EqualsString("/src/tsconfig.json"));
                bool switchCase = name.StartsWith("picks", StringComparison.Ordinal) || name.StartsWith("reverts", StringComparison.Ordinal);
                if (switchCase)
                    Check(service.GetProgram().Options().Strict == Tristate.False);
                if (name.StartsWith("uses", StringComparison.Ordinal) || name.StartsWith("falls", StringComparison.Ordinal))
                    continue;
                int baseline = 0;
                if (name.StartsWith("schedules", StringComparison.Ordinal))
                {
                    session.WaitForBackgroundTasks();
                    baseline = fixture.Client.RefreshDiagnosticsCallCount();
                }

                Configure("tsconfig.all.json");
                (service, error) = session.GetLanguageService(ctx, uri);
                Check(error is null);
                Check(service is not null);
                project = session.Snapshot().GetDefaultProject(uri);
                Check(project is not null);
                if (switchCase || inferred)
                    Check(project.ConfigFileName().AsValue().EqualsString("/src/tsconfig.all.json"));
                if (switchCase)
                    Check(service.GetProgram().Options().Strict == Tristate.True);
                if (inferred)
                    Check(session.Snapshot().GetLanguageServiceProjectsContainingFile(uri).Length == 1);
                if (name.StartsWith("schedules", StringComparison.Ordinal))
                {
                    session.WaitForBackgroundTasks();
                    Check(fixture.Client.RefreshDiagnosticsCallCount() > baseline);
                }

                if (name.StartsWith("reverts", StringComparison.Ordinal))
                {
                    Configure("");
                    (service, error) = session.GetLanguageService(ctx, uri);
                    Check(error is null);
                    Check(service is not null);
                    project = session.Snapshot().GetDefaultProject(uri);
                    Check(project is not null);
                    Check(project.ConfigFileName().AsValue().EqualsString("/src/tsconfig.json"));
                    Check(service.GetProgram().Options().Strict == Tristate.False);
                }
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(new InvalidOperationException(name, error));
            }
        }

        if (failures is not null)
            throw new AggregateException(failures);
    }
}
