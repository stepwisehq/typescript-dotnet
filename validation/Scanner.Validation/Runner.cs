using System.Globalization;
using System.Text;

namespace TypeScript.Scanner.Validation;

public static class Runner
{
    public static Response Run(Request request)
    {
        var scanner = new Scanner();
        byte[] bytes = request.TextBytesBase64 is { } encoded ? Convert.FromBase64String(encoded) : Encoding.UTF8.GetBytes(request.Text);
        scanner.SetText(bytes);
        scanner.SetSkipTrivia(request.SkipTrivia ?? true);
        scanner.SetSkipJSDocLeadingAsterisks(request.SkipJSDocLeadingAsterisks);
        scanner.SetLanguageVariant((LanguageVariant)request.LanguageVariant);
        scanner.SetScriptTarget((ScriptTarget)request.ScriptTarget);
        var result = new Response();
        scanner.SetOnError((diagnostic, start, length, args) => result.Diagnostics.Add(new(
            diagnostic.Code(), start, length, args.Select(arg => Convert.ToString(arg, CultureInfo.InvariantCulture) ?? "").ToArray())));
        void Capture() => result.Tokens.Add(new((int)scanner.Token(), scanner.TokenFullStart(), scanner.TokenStart(), scanner.TokenEnd(),
            Convert.ToBase64String(scanner.TokenTextBytes()), Convert.ToBase64String(scanner.TokenValueBytes()), (int)scanner.TokenFlags()));
        if (request.Operations is not { Length: > 0 })
        {
            for (int i = 0; i < bytes.Length * 3 + 10; i++)
            {
                scanner.Scan();
                Capture();
                if (scanner.Token() == Kind.EndOfFile) break;
                if (i == bytes.Length * 3 + 9) throw new InvalidOperationException("Scanner did not reach EOF within progress guard");
            }
        }
        else
        {
            ScannerState saved = null!;
            foreach (var op in request.Operations)
            {
                switch (op.Op)
                {
                    case "Reset": scanner.Reset(); continue;
                    case "SetText": scanner.SetText(op.Text); continue;
                    case "Mark": saved = scanner.Mark(); continue;
                    case "Rewind": scanner.Rewind(saved); continue;
                    case "ResetPos": scanner.ResetPos(op.Position); continue;
                    case "ResetTokenState": scanner.ResetTokenState(op.Position); continue;
                    case "Scan": scanner.Scan(); break;
                    case "ReScanLessThanToken": scanner.ReScanLessThanToken(); break;
                    case "ReScanGreaterThanToken": scanner.ReScanGreaterThanToken(); break;
                    case "ReScanTemplateToken": scanner.ReScanTemplateToken(op.Argument); break;
                    case "ReScanAsteriskEqualsToken": scanner.ReScanAsteriskEqualsToken(); break;
                    case "ReScanSlashToken": scanner.ReScanSlashToken(op.Argument); break;
                    case "ReScanJsxToken": scanner.ReScanJsxToken(op.Argument); break;
                    case "ReScanHashToken": scanner.ReScanHashToken(); break;
                    case "ReScanQuestionToken": scanner.ReScanQuestionToken(); break;
                    case "ScanJsxToken": scanner.ScanJsxToken(); break;
                    case "ScanJsxTokenEx": scanner.ScanJsxTokenEx(op.Argument); break;
                    case "ScanJsxIdentifier": scanner.ScanJsxIdentifier(); break;
                    case "ScanJsxAttributeValue": scanner.ScanJsxAttributeValue(); break;
                    case "ReScanJsxAttributeValue": scanner.ReScanJsxAttributeValue(); break;
                    case "ScanJSDocCommentTextToken": scanner.ScanJSDocCommentTextToken(op.Argument); break;
                    case "ScanJSDocToken": scanner.ScanJSDocToken(); break;
                    default: throw new ArgumentException($"Unknown operation {op.Op}");
                }
                Capture();
            }
        }
        if (request.UtilityOperations is { Length: > 0 })
        {
            result.UtilityResults = [];
            foreach (var op in request.UtilityOperations)
            {
                var item = new UtilityResult { Op = op.Op };
                switch (op.Op)
                {
                    case "SkipTrivia": item.Number = Scanner.SkipTrivia(bytes, op.Position); break;
                    case "SkipTriviaEx": item.Number = Scanner.SkipTriviaEx(bytes, op.Position, new() { StopAfterLineBreak = op.StopAfterLineBreak, StopAtComments = op.StopAtComments, InJSDoc = op.InJSDoc }); break;
                    case "GetShebang": item.TextBytesBase64 = Convert.ToBase64String(Scanner.GetShebang(bytes)); break;
                    case "IsValidIdentifier": item.Boolean = Scanner.IsValidIdentifier(bytes); break;
                    case "GetIdentifierToken": item.Number = (int)Scanner.GetIdentifierToken(request.Text); break;
                    case "IsIdentifierStart": item.Boolean = Scanner.IsIdentifierStart(op.Character); break;
                    case "IsIdentifierPart": item.Boolean = Scanner.IsIdentifierPart(op.Character); break;
                    case "IsIdentifierPartEx": item.Boolean = Scanner.IsIdentifierPartEx(op.Character, (LanguageVariant)request.LanguageVariant); break;
                    case "ComputeLineOfPosition": item.Number = Scanner.ComputeLineOfPosition(op.LineStarts, op.Position); break;
                    case "ComputePositionOfLineAndByteOffset": item.Number = Scanner.ComputePositionOfLineAndByteOffset(op.LineStarts, op.Line, op.Character); break;
                    case "ComputePositionOfLineAndUTF16Character": item.Number = Scanner.ComputePositionOfLineAndUTF16Character(op.LineStarts, op.Line, op.Character, bytes, op.AllowEdits); break;
                    case "GetLeadingCommentRanges": item.Comments = Scanner.GetLeadingCommentRanges(bytes, op.Position).Select(c => new Comment((int)c.Kind, c.Pos, c.End, c.HasTrailingNewLine)).ToList(); break;
                    case "GetTrailingCommentRanges": item.Comments = Scanner.GetTrailingCommentRanges(bytes, op.Position).Select(c => new Comment((int)c.Kind, c.Pos, c.End, c.HasTrailingNewLine)).ToList(); break;
                    default: throw new ArgumentException($"Unknown utility operation {op.Op}");
                }
                result.UtilityResults.Add(item);
            }
        }
        foreach (var d in scanner.CommentDirectives()) result.CommentDirectives.Add(new(d.Loc.Pos(), d.Loc.End(), (int)d.Kind));
        return result;
    }
}
