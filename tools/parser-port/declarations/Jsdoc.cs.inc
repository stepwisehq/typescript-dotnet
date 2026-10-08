// Declarations from jsdoc.go; its init registration maps to a static class constructor.
internal enum jsdocState { BeginningOfLine,SawAsterisk,SavingComments,SavingBackticks }
[Flags] internal enum propertyLikeParse { Property=1,Parameter=2,CallbackParameter=4 }
public partial class Parser
{
 static Parser() => Ast.SetParseJSDocForNode(parseJSDocForNode);
}
