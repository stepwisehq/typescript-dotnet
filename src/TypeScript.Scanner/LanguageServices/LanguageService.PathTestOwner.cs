// Original file_rename_test.go initializes only the retained Host field.
namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    internal LanguageService(ILanguageServiceHost host) : this()
    {
        this.host = host;
    }
}
