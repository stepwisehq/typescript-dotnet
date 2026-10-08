#!/usr/bin/env python3
"""Generate deterministic lexical requests; oracle results are generated separately."""
import base64, json, pathlib, random, re
root = pathlib.Path(__file__).resolve().parent.parent
upstream = root / 'upstream/TypeScript/tsc'
rows = []
def add(name, text='', **options): rows.append(dict(name=name, text=text, **options))
def ops(*names): return [dict(op=x) if isinstance(x,str) else x for x in names]
def rescan(name,text,op,argument=False,**kw): add(name,text,operations=ops('Scan',dict(op=op,argument=argument),'Scan'),**kw)
add('empty')
scanner = (upstream/'internal/scanner/scanner.go').read_text()
keywords = re.findall(r'^\s*"([a-z]+)":\s+ast.Kind\w+Keyword,', scanner, re.M)
add('all-keywords',' '.join(keywords))
add('source-keyword-upstream','source sourceValue')
punctuation = '{}()[] . ... ; , < > <= >= == != === !== => + - ** * / % ++ -- << >> >>> & | ^ ! ~ && || ? : @ ?? ` # = += -= *= **= /= %= <<= >>= >>>= &= |= ^= ||= &&= ??= ?. '
add('punctuation',punctuation)
texts = {
 'comments': '// hello\r\n/*block*/ /** @deprecated @see @link */ const x = 1;',
 'directives': '// @ts-ignore\nlet a;\n/* @ts-expect-error */\nconst b=0;\n/** @ts-ignore */\n',
 'jsdoc-tags': '/** @deprecated! @seeX @link\n * @deprecated */\n/** @see */ a',
 'trivia': '\ufeff \t\v\f\r\n\r\n\u00a0\u1680\u2000\u2028\u2029\u202f\u3000x',
 'shebang': '#!/usr/bin/env node\nconst x=1',
 'conflict': '<<<<<<< HEAD\nconst x=1;\n=======\nconst x=2;\n>>>>>>> other\n',
 'non-text': '\x00\x01\x02abc',
 'numbers': '0 1 123 1.2 .5 1. 1e2 1e+2 1e-2 01 077 08 09 0xff 0XAB 0b101 0o77 123n 0xffn 0b10n 0o70n 1_000 0xF_F 0b1_0 0o7_1 9_9n 1.2_3e4_5',
 'bad-numbers': '1__0 0x 0b 0o 0b2 0o8 1e 1e+ 1e_2 1_ 0x_1 0b_1 0o_1 01n 1.2n 1e2n 123abc 0b102 0o789 1e9999 0.0000001 9007199254740993',
 'identifier': 'α中文𐐀 café a\u200cb a\u200db $ _ #private \\u0061 \\u{61} \\uD801\\uDC00 \\u{10400} \\u0030 \\u{110000} \\uZZZZ',
 'strings': '"🦀\\ud7ff\\ud800\\ud801\\uD83E\\uDD80" "\\0\\1\\8\\9\\123\\x41\\u0041\\u{1F980}" \'\\n\\r\\t\\b\\v\\f\\\\\\\'\' "a\\\r\nb"',
 'bad-strings': '"\\xG1" "\\uGGGG" "\\u{}" "\\u{110000}" "\\u{1" "unterminated\n\'unterminated',
 'templates': '`plain` `a\\r\\n\\u{1F980}` `\\8\\9\\uZZZZ` `unterminated',
}
for name,text in texts.items():
 add(name,text)
 add(name+'-trivia',text,skipTrivia=False)
for data in [b'\xffabc',b'\xc0\x80',b'\xed\xa0\x80',b'\xf0\x9f',b'a\x80b',b'"\xff"']:
 add('malformed-'+data.hex(),textBytesBase64=base64.b64encode(data).decode())
for operator in ['>>','>=','>>=','>>>','>>>=']:
 rescan('greater-'+operator,operator+'x','ReScanGreaterThanToken')
rescan('less-shift','<<=x','ReScanLessThanToken')
rescan('asterisk-equals','*=x','ReScanAsteriskEqualsToken')
rescan('hash','#private','ReScanHashToken')
rescan('question','??x','ReScanQuestionToken')
for tagged in [False,True]:
 add('template-tail-'+str(tagged),'`head${x}tail\\8`',operations=ops('Scan','Scan','Scan',dict(op='ReScanTemplateToken',argument=tagged),'Scan'))
 add('template-middle-'+str(tagged),'`head${x}mid${y}tail`',operations=ops('Scan','Scan','Scan',dict(op='ReScanTemplateToken',argument=tagged),'Scan','Scan',dict(op='ReScanTemplateToken',argument=tagged),'Scan'))
regexes = ['/abc/g','/[a-z]+/iu','/[[]/','/a/ggz','/a/uv','/a/dsv','/(?<name>a)\\k<name>/u','/a{3,2}/u','/[z-a]/u','/(abc/u','/a\\/b/g','/unterminated;  ','/a\nnext','/\\p{Script=Greek}/u','/[a&&b]/v','/\\uD800/u','/\\1(a)/','/(?<=a)b/','/[\\q{ab|cd}]/v','/[\\d-a]/u','/a)/u','/\\c1/u','/\\xGG/u']
for i,text in enumerate(regexes):
 for report in [False,True]: rescan(f'regex-{i}-{report}',text,'ReScanSlashToken',report)
rescan('regex-es5','/(?<n>x)/dsuv','ReScanSlashToken',True,scriptTarget=1)
for text in ['text <tag>','\n   </tag>','bad > } <','abc\nnext<']:
 for multi in [False,True]:
  add('jsx-'+str(len(rows)),text,languageVariant=1,operations=ops(dict(op='ScanJsxTokenEx',argument=multi),'ScanJsxToken'))
rescan('jsx-rescan','hello-world <tag>','ReScanJsxToken',True,languageVariant=1)
add('jsx-identifier','foo-bar:baz',operations=ops('Scan','ScanJsxIdentifier','Scan'))
add('jsx-attribute','"a\\nb &amp;"',operations=ops('ScanJsxAttributeValue','Scan'))
add('jsx-attribute-rescan','"a\\nb &amp;"',operations=ops('Scan','ReScanJsxAttributeValue','Scan'))
for backticks in [False,True]:
 add('jsdoc-text-'+str(backticks),'foo @bar `code` {x}\n * line',operations=ops(*[dict(op='ScanJSDocCommentTextToken',argument=backticks)]*4))
add('jsdoc-token','@param {Foo.<T>} x\n * \\uD801\\uDC00 \\u0061',operations=ops(*(['ScanJSDocToken']*14)),skipJSDocLeadingAsterisks=True)
add('state-rewind','α beta 3',operations=ops('Scan','Mark','Scan','Scan','Rewind','Scan','Scan',dict(op='ResetPos',position=0),'Scan',dict(op='ResetTokenState',position=3),'Scan'))
rng=random.Random(0x5CA77E)
atoms=['foo','let','α','0xFF','1__2','"\\ud800"','"abc"','`x`','/** @deprecated */','// @ts-ignore\n','/*x*/','??=','>>>=','+','=>','#foo','@','\\u0061','\r\n','\u2028']
for i in range(100): add(f'seeded-{i:03}', ' '.join(rng.choice(atoms) for _ in range(rng.randint(1,40))),skipTrivia=i%2==0)

rescan('regex-unicode-name-suggestion',r'/(?<é>x)\k<É>/u','ReScanSlashToken',True)
add('misplaced-shebang','x\n#!abc')
rescan('regex-decimal-class',r'/[\8]/u','ReScanSlashToken',True)
rescan('regex-duplicate-groups-target-es2024',r'/(?<x>a)|(?<x>b)/u','ReScanSlashToken',True,scriptTarget=11)
rescan('regex-property-missing-name',r'/\p{=Greek}/u','ReScanSlashToken',True)
rescan('regex-property-unknown-value',r'/\p{Script=Nonexistent}/u','ReScanSlashToken',True)
# Boundary cases target rounding, Unicode table edges, malformed decoding, and regex diagnostics.
for marker in ['<<abcde rest', '==abcdeX', '>>>>>>> branch', '=======', '<<<<<<< branch']:
 add('conflict-boundary-'+str(len(rows)),marker+'\nconst y=2')
for exponent in [-400,-325,-324,-323,-308,-307,-7,-6,-5,20,21,22,307,308,309,400]:
 for mantissa in ['1','2.2250738585072014','4.9406564584124654','9.999999999999999','1.7976931348623157']:
  add(f'decimal-boundary-{mantissa}-{exponent}',f'{mantissa}e{exponent}')
for bits in [52,53,54,63,64,65,127,128,255,256,511,512,1023,1024,1100]:
 for delta in [-1,0,1]:
  n=2**bits+delta
  for base,prefix,fmt in [(2,'0b','b'),(8,'0o','o'),(16,'0x','x')]:
   value=prefix+format(n,fmt)
   add(f'base-boundary-{bits}-{delta}-{base}',value+' '+value+'n')
# Every eighth upstream identifier table range edge plus nearby code points.
id_source=(upstream/'internal/stringutil/identifier_parts_generated.go').read_text()
points=[int(x,0) for x in re.findall(r'\b(?:0x[0-9a-fA-F]+|[0-9]+)\b',id_source) if int(x,0)<=0x10FFFF]
for i,point in enumerate(points[::8][:100]):
 chars=' '.join(chr(max(0,min(0x10FFFF,point+delta))) for delta in [-1,0,1] if not 0xD800<=point+delta<=0xDFFF)
 add(f'unicode-edge-{i}',chars+' '+''.join('a'+c+' ' for c in chars))
for b in range(0x80,0x100):
 data=bytes([b])+b'a '+bytes([b,0x80])+b' z'
 add(f'utf8-leading-{b:02x}',textBytesBase64=base64.b64encode(data).decode())
patterns=[
 '*','+','?','a{','a{,2}','a{2,1}','a{2,}','a{2,3}','a{2,3}?','a**','^*','$+',
 '(',')','[',']','{','}','(?<x>a)(?<x>b)','(?<x>a)|(?<x>b)','(?<>a)','(?<1>a)',
 r'\k',r'\k<missing>',r'(?<name>a)\k<nane>',r'\9',r'(a)\2',r'\q',r'\c1',r'\xGG',r'\u{}',r'\u{110000}',
 '(?i:a)','(?-:a)','(?gg:a)','(?u:a)','(?ims-im:a)','(?z:a)',
 r'[\d-a]',r'[a-\d]',r'[z-a]',r'[a&&b--c]',r'[a--b&&c]',r'[&]',r'[&&]',r'[!!]',r'[^\q{ab|c}]',r'[\q]',r'[\q{ab|cd}]',
 r'\p',r'\p{}',r'\p{Script=}',r'\p{Unknown=Latin}',r'\p{Script=Unknown}',r'\p{ASCII_Hex_Digi}',r'\p{Script=Grek}',r'\p{Unknown}',r'\p{Basic_Emoji}',r'\P{Basic_Emoji}',r'[\P{Basic_Emoji}]',
 r'[\p{Script=Latin}&&\p{ASCII}]',r'[^\q{ab}]',r'[a-b-c]',r'[a&&]',r'[a--]',r'\a',r'\8',r'[\1]',r'\0',r'\00',
]
for i,pattern in enumerate(patterns):
 for flags in ['', 'u','v']:
  for target in [1,99]:
   rescan(f'regex-boundary-{i}-{flags or "none"}-{target}','/'+pattern+'/'+flags,'ReScanSlashToken',True,scriptTarget=target)


# Public pure helpers use the same Go oracle and published native executable.
for text in ['  /*a*/\r\n//b\n x','// one\r\n/* two */ x','/*unterminated',' \u2028/**x*/\n y','#!/usr/bin/env node\n x',' * Foo\n * Bar','x /*a*/ //b\n/*c*/ y']:
 utility=[dict(op='SkipTrivia'),dict(op='GetShebang'),dict(op='GetLeadingCommentRanges'),dict(op='GetTrailingCommentRanges')]
 utility += [dict(op='SkipTriviaEx',**flags) for flags in [dict(),dict(stopAtComments=True),dict(stopAfterLineBreak=True),dict(inJSDoc=True)]]
 add('helpers-trivia-'+str(len(rows)),text,utilityOperations=utility)
for text in ['α','a0','source','sourceValue','', 'a-b', '𐐀','a\u200cb','1x','é','K']:
 add('helpers-identifier-'+str(len(rows)),text,utilityOperations=[dict(op='IsValidIdentifier'),dict(op='GetIdentifierToken')])
for character in [0x24,0x30,0x41,0x2D,0x3B1,0x10400,0x200C,0x200D,0xD800,0x10FFFF]:
 add('helpers-id-char-'+str(character),'',languageVariant=1,utilityOperations=[dict(op=op,character=character) for op in ['IsIdentifierStart','IsIdentifierPart','IsIdentifierPartEx']])
for text,starts,raw in [('a🦀b\r\nΩ',[0,8],None),('',[0,3],b'a\xff\nZ')]:
 utility=[dict(op='ComputeLineOfPosition',lineStarts=starts,position=p) for p in range(0,(10 if raw is None else 4))]
 utility += [dict(op='ComputePositionOfLineAndByteOffset',lineStarts=starts,line=line,character=offset) for line in [0,1] for offset in [0,1]]
 utility += [dict(op='ComputePositionOfLineAndUTF16Character',lineStarts=starts,line=line,character=char,allowEdits=True) for line in [-1,0,1,2] for char in [0,1,2,3,4,9]]
 options=dict(utilityOperations=utility)
 if raw is not None: options['textBytesBase64']=base64.b64encode(raw).decode()
 add('helpers-line-'+str(len(rows)),text,**options)
add('reset-reuse','/** @deprecated */ abc',operations=ops('Scan','Reset',dict(op='SetText',text='β 123'),'Scan','Scan','Scan'))
case_pairs=[(931, 963), (931, 962), (963, 931), (963, 962), (962, 931), (962, 963), (75, 107), (75, 8490), (107, 75), (107, 8490), (8490, 75), (8490, 107), (83, 115), (83, 383), (115, 83), (115, 383), (383, 83), (383, 115), (223, 7838), (7838, 223), (73, 105), (73, 304), (73, 305), (105, 73), (105, 304), (105, 305), (304, 73), (304, 105), (304, 305), (305, 73), (305, 105), (305, 304), (66560, 66600), (66600, 66560), (201, 233), (233, 201)]
for first,second in case_pairs:
 rescan(f'regex-case-{first:x}-{second:x}',f'/(?<{chr(first)}>x)'+chr(92)+f'k<{chr(second)}>/u','ReScanSlashToken',True)
for name,reference in [('İabc','iabc'),('Kabc','Kabc'),('ßabc','ẞabc')]:
 rescan('regex-case-long-'+name,f'/(?<{name}>x)'+chr(92)+f'k<{reference}>/u','ReScanSlashToken',True)
# Include small upstream conformance sources deterministically (licensed Microsoft Apache-2.0).
paths=sorted((upstream/'testdata/tests/cases').rglob('*.ts'))
selected=[p for p in paths if any(s in p.name.lower() for s in ['scanner','unicodeescape','numericliteral','stringliteral','sourcekeyword']) and p.stat().st_size<10000][:120]
if not selected: selected=[p for p in paths if p.stat().st_size<5000][:20]
for p in selected: add('upstream-'+p.relative_to(upstream).as_posix(),textBytesBase64=base64.b64encode(p.read_bytes()).decode())
output=root/'validation/corpus/requests.jsonl'
output.write_text(''.join(json.dumps(row,ensure_ascii=True,separators=(',',':'))+'\n' for row in rows))
print(f'{len(rows)} requests ({len(selected)} upstream files)')

manifest=dict(upstreamRepository='https://github.com/microsoft/TypeScript',upstreamCommit='fed0bf24149fb1ed36039212648bafdafc1ea10e',goVersion='1.27.1',seed='0x5CA77E',totalCases=len(rows),upstreamFiles=[dict(path=p.relative_to(upstream).as_posix(),sha256=__import__('hashlib').sha256(p.read_bytes()).hexdigest(),license='Apache-2.0 (licenses/TypeScript-LICENSE.txt)') for p in selected])
(root/'validation/corpus/manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
