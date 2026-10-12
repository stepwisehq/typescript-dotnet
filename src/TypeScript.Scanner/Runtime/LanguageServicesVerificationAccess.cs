// Authored test access: original package-internal assertions are shared by xUnit
// and the Native AOT validation target without exposing compiler implementation APIs.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("LanguageServices.Checks")]
