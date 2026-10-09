#!/usr/bin/env python3
from pathlib import Path
import sys
s=Path(sys.argv[1]).read_text().replace('keyBuilder b = default;', 'var b = new keyBuilder();')
s=s.replace('sb.Reset();','sb = new GoBuilder();')
s=s.replace('evaluator.AnyToString(', 'Evaluator.AnyToString(')
s=s.replace('var t6 = this.templateLiteralTypes[key];','this.templateLiteralTypes.TryGetValue(key, out var t6);')
Path(sys.argv[2]).write_text(s)
