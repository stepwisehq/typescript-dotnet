#!/usr/bin/env python3
"""Apply explicit selected-source guards to scratch call resolver; reviewed C# authoritative."""
from pathlib import Path
import sys
import importlib.util
spec=importlib.util.spec_from_file_location('install',Path(__file__).with_name('install.py'));m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
def close(text,start):
 depth=0
 for i in range(start,len(text)):
  if text[i]=='{':depth+=1
  elif text[i]=='}':
   depth-=1
   if not depth:return i+1
 raise ValueError('unclosed')
def guard(text,header,feature):
 i=text.index(header);a=text.index('{',i+len(header));b=close(text,a)
 return text[:a]+'{ throw unsupported("'+feature+'"); }'+text[b:]
def method(name,args,ret,body):return '    private '+ret+' '+name+'('+args+')\n    {\n'+body+'\n    }'
items=[]
for name,_,_,body in m.methods(Path(sys.argv[1]).read_text()):
 b=body.replace('List<Signature> candidatesOutArray','GoRef<List<Signature>> candidatesOutArray').replace('(Checker.Value).reportCannotInvokePossiblyNullOrUndefinedError','reportCannotInvokePossiblyNullOrUndefinedError').replace('this.TypeToString(funcType)','this.typeToStringByteString(funcType)').replace('Scanner.SkipTriviaEx','TypeScript.Scanner.Scanner.SkipTriviaEx').replace('UnionReductionSubtype','UnionReduction.Subtype').replace('CallState s = default;','CallState s = new();').replace('GoSlices.Contains(this.callResolutionStack, s.node)','(this.callResolutionStack?.Contains(s.node) == true)')
 if name=='checkCallExpression':
  b=guard(b,'if (Ast.IsNewExpression(node))','checkCallExpression: source new')
  b=guard(b,'if ((Ast.IsInJSFile(node) && this.isCommonJSRequire(node)))','checkCallExpression: JS require').replace('Ast.IsInJSFile(node) && this.isCommonJSRequire(node)','Ast.IsInJSFile(node)')
  b=guard(b,'if ((((returnType.flags & TypeFlags.ESSymbolLike) != 0) && this.isSymbolOrSymbolForCall(node)))','checkCallExpression: Symbol library call').replace('this.isSymbolOrSymbolForCall(node)','this.isGlobalSymbolConstructor(node.Expression())')
 if name=='resolveSignature':
  b=method(name,'Node node, GoRef<List<Signature>> candidatesOutArray, CheckMode checkMode','Signature','        if (node.Kind == Kind.CallExpression) return resolveCallExpression(node, candidatesOutArray, checkMode);\n        throw unsupported("resolveSignature: non-call syntax");')
 if name=='resolveCallExpression':
  b=guard(b,'if ((node.Expression().Kind == Kind.SuperKeyword))','resolveCallExpression: super')
  b=guard(b,'if (Ast.IsImportCall(node))','resolveCallExpression: import')
  b=guard(b,'if (isCallChain(node))','resolveCallExpression: optional chain')
  b=guard(b,'if (((((checkMode & CheckMode.SkipGenericFunctions) != 0) && (GoRuntime.Len(node.TypeArguments()) == 0)) && Core.Some(callSignatures, this.isGenericFunctionReturningFunction)))','resolveCallExpression: source generic return')
  b=b.replace('Core.Some(callSignatures, this.isGenericFunctionReturningFunction)','Core.Some(callSignatures, s => GoRuntime.Len(s.typeParameters) != 0)')
 if name=='resolveCall':
  b=b.replace('!isSuperCall(node)','node.Expression().Kind != Kind.SuperKeyword')
 if name=='reorderCandidates':
  b=guard(b,'if ((callChainFlags != 0))','reorderCandidates: optional chain')
 if name=='chooseOverload':
  b=guard(b,'if ((GoRuntime.Len(candidate2.typeParameters) != 0))','chooseOverload: source generic candidate')
  b=guard(b,'if ((inferenceContext != null))','chooseOverload: generic inference')
 if name=='hasCorrectArity':
  for h in ['case true when Ast.IsTaggedTemplateExpression(node):','case true when Ast.IsDecorator(node):','case true when Ast.IsBinaryExpression(node):','case true when Ast.IsJsxOpeningLikeElement(node):','case true when (Ast.IsNewExpression(node) && (node.ArgumentList() == null)):']:
   b=guard(b,h,'hasCorrectArity: excluded call syntax')
  b=guard(b,'if ((spreadArgIndex >= 0))','hasCorrectArity: spread')
 if name=='isSignatureApplicable':
  b=guard(b,'if (Ast.IsJsxCallLike(node))','isSignatureApplicable: JSX')
  # Two real rest branches require tuple/spread source machinery, excluded at source admission.
  i=b.rfind('if ((restType != null))');a=b.index('{',i);e=close(b,a);b=b[:a]+'{ throw unsupported("isSignatureApplicable: non-array rest"); }'+b[e:]
 if name=='pickLongestCandidateSignature':
  i=b.index('        List<Node> typeArgumentNodes = default;');b=b[:i]+'        throw unsupported("pickLongestCandidateSignature: source generic candidate");\n    }'
 if name=='createUnionOfSignaturesForOverloadFailure':
  b=guard(b,'if ((GoRuntime.Len(restParameterSymbols) != 0))','createUnionOfSignaturesForOverloadFailure: source rest')
 if name=='resolveUntypedCall':
  b=method(name,'Node node','Signature','        if (node.Kind != Kind.CallExpression) throw unsupported("resolveUntypedCall: non-call");\n        checkSourceElements(node.TypeArguments());\n        foreach (var argument in node.Arguments() ?? []) checkExpression(argument);\n        return anySignature;')
 if name=='getEffectiveCallArguments':
  b=method(name,'Node node','List<Node>','        if (node.Kind != Kind.CallExpression) throw unsupported("getEffectiveCallArguments: non-call");\n        var args = node.Arguments();\n        if (getSpreadArgumentIndex(args) >= 0) throw unsupported("getEffectiveCallArguments: spread");\n        return args;')
 items.append(b)
Path(sys.argv[2]).write_text('public sealed partial class Checker\n{\n'+'\n'.join(items)+'\n}\n')
