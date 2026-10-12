using TypeScript.LanguageServices.Utilities;

namespace LanguageServices.Checks;
public static partial class UserPreferenceChecks
{
    internal static void AssertOriginalPreferences(string name, UserPreferences expected, UserPreferences actual) => CheckPreferences(name, expected, actual);
}
