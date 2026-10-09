// Authored verification access for source-file binding completion.
// Allows isolated atomic-ID boundary controls; does not add a public compiler API.
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("BindingCompletion.Checks")]
