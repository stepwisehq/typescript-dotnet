#!/usr/bin/env python3
"""Reviewable inference.go scratch adaptations; production hand port is authoritative."""
from pathlib import Path
import re, sys
sys.path.insert(0, str(Path(__file__).parent))
from install import methods
source = Path(sys.argv[1]).read_text()
# Internal numericStringType requires real template inference even though source
# templates stay excluded. Only intrinsic string-mapping payloads retain guards.
start = source.index('                    this.inferFromTypes(n, source.AsStringMappingType().target, target.AsStringMappingType().target);')
source = source[:start] + source[start:].replace('                    this.inferFromTypes(n, source.AsStringMappingType().target, target.AsStringMappingType().target);', '                    throw unsupported("inferFromTypes: intrinsic string mapping");', 1)
# Go method expressions pass their receiver explicitly.
for name in re.findall(r'\(Checker.Value\)\.(\w+)', source):
    if name.startswith('infer'):
        source = source.replace('(Checker.Value).' + name, '(c, n, s, t) => c.' + name + '(n, s, t)')
    else:
        source = source.replace('(Checker.Value).' + name, '(c, s, t) => c.' + name + '(s, t)')
source = source.replace('Math.Min(n.inferencePriority, n.priority)', '(InferencePriority)Math.Min((int)n.inferencePriority, (int)n.priority)')
source = re.sub(r'Math.Min\(n.inferencePriority, (\w+)\)', r'(InferencePriority)Math.Min((int)n.inferencePriority, (int)\1)', source)
source = source.replace('new IntraExpressionInferenceSite { node = node, t = t }', 'new IntraExpressionInferenceSite(node, t)')
source = source.replace('hasTypeParameterDefault', 'hasInferenceTypeParameterDefault')
source = source.replace('.readonly', '.@readonly')
source = source.replace('jsnum.', 'JsNum.')
source = source.replace('InferencePriority.SpeculativeTuple, 0)', 'InferencePriority.SpeculativeTuple, InferencePriority.None)')
source = source.replace('CheckFlags.Readonly, 0)', 'CheckFlags.Readonly, CheckFlags.None)')
for enum in ['ExpandingFlags', 'UnionReduction']:
    source = re.sub(r'\b' + enum + r'([A-Z]\w*)\b', lambda m: enum + '.' + m.group(1), source)
source = source.replace('var ok = n.visited.TryGetValue(key, out var status);', 'var status = default(InferencePriority);\n            var ok = n.visited != null && n.visited.TryGetValue(key, out status);')
source = source.replace('var cached = this.reverseHomomorphicMappedCache[key];', 'this.reverseHomomorphicMappedCache.TryGetValue(key, out var cached);')
source = source.replace('this.patternForType[source] != null', 'this.patternForType.ContainsKey(source)')
source = source.replace('this.skipDirectInferenceNodes.Has(node)', 'this.skipDirectInferenceNodes?.Contains(node) == true')
for name in ['inferFromIntraExpressionSites','getInferredType','getTypeFromInference']:
    source = re.sub(r'private (\w+) ' + name + r'\(', r'internal \1 ' + name + '(', source)
source = source.replace('private static void clearCachedInferences', 'internal static void clearCachedInferences')
# Go resets a struct, preserving zero-length slice headers. Do not Clear the
# caller-owned inference collection when resetting the pooled state.
for name, start, end, body in reversed(list(methods(source))):
    if name == 'putInferenceState':
        body = '''    private void putInferenceState(InferenceState n)
    {
        n.visited?.Clear();
        n.inferences = n.inferences == null ? null : [];
        n.sourceStack = n.sourceStack == null ? null : [];
        n.targetStack = n.targetStack == null ? null : [];
        n.originalSource = n.originalTarget = n.propagationType = null;
        n.priority = n.inferencePriority = InferencePriority.None;
        n.contravariant = n.bivariant = false;
        n.expandingFlags = ExpandingFlags.None;
        n.next = freeinferenceState;
        freeinferenceState = n;
    }'''
        source = source[:start] + body + source[end:]
model = '''
    // inference.go:11/19. The original key is already a constituent value tuple.
    private readonly record struct InferenceKey(uint source, uint target,
        InferencePriority priority, bool contravariant, bool bivariant);
    private sealed class InferenceState
    {
        internal List<InferenceInfo> inferences;
        internal Type originalSource, originalTarget, propagationType;
        internal InferencePriority priority, inferencePriority;
        internal bool contravariant, bivariant;
        internal ExpandingFlags expandingFlags;
        internal Dictionary<InferenceKey, InferencePriority> visited;
        internal List<Type> sourceStack, targetStack;
        internal InferenceState next;
    }
'''
source = source.replace('public sealed partial class Checker\n{', 'public sealed partial class Checker\n{' + model, 1)
Path(sys.argv[2]).write_text(source)
