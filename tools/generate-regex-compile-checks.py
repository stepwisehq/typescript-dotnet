from pathlib import Path
import re
scope={};exec(Path('tools/generate-regex-checks.py').read_text().split('alltext=')[0].replace('root=Path(__file__).resolve().parents[1]','root=Path.cwd()'),scope)
src=Path('tools/.cache/go/src/regexp/syntax/prog_test.go').read_text().split('var compileTests =',1)[1].split('func TestCompile',1)[0]
rows=re.findall(r'\{("(?:[^"\\]|\\.)*"), (`[^`]*`)\}',src)
body='\n'.join('        ('+scope['cs'](scope['raw'](a))+', '+scope['cs'](scope['raw'](b))+'),' for a,b in rows)
p=Path('validation/LanguageServices.Checks/RegexCompileChecks.cs');s=p.read_text() if p.exists() else '';assert s, 'Retain the source-faithful dump observer when regenerating tables'
s=re.sub(r'(private static readonly \(string Pattern, string Expected\)\[\] Rows = \[\n).*?(\n    \];)', lambda m:m[1]+body+m[2],s,flags=re.S)
p.write_text(s)
print(len(rows))
