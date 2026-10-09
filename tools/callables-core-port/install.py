#!/usr/bin/env python3
"""Integrate reviewed scratch method bodies; never an unattended full-file regenerator.
Usage: install.py scratch.cs target.cs. Existing matched methods are replaced.
Hand-reviewed source remains authoritative; use scratch output for future comparisons.
"""
from pathlib import Path
import re
import sys

def methods(text):
    token = re.compile(r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|[{}]')
    pattern = re.compile(r'^    (?:(?:private|internal|public)\s+)(?:static\s+)?[^\n=]+?\s+(\w+)\([^\n]*\)\n    \{', re.M)
    for match in pattern.finditer(text):
        start = text.index('{', match.start()); depth = 0
        for part in token.finditer(text, start):
            if part.group() == '{': depth += 1
            elif part.group() == '}':
                depth -= 1
                if not depth:
                    yield match.group(1), match.start(), part.end(), text[match.start():part.end()]
                    break
        else: raise ValueError('Unclosed method ' + match.group(1))

if __name__ == '__main__':
    scratch, target = map(Path, sys.argv[1:]); text = target.read_text()
    for name, _, _, body in methods(scratch.read_text()):
        matches = [(start, end) for n, start, end, _ in methods(text) if n == name]
        if len(matches) > 1: raise ValueError('Overload ambiguity ' + name)
        if matches:
            start, end = matches[0]; text = text[:start] + body + text[end:]
        else:
            text += '\npublic sealed partial class Checker\n{\n' + body + '\n}\n'
    target.write_text(text)
