#!/usr/bin/env python3
"""Emit exact error text from the pinned x/sys v0.48.0 Linux amd64 table."""
import argparse
import pathlib
import re

parser = argparse.ArgumentParser()
parser.add_argument("source", type=pathlib.Path, help="x/sys v0.48.0 unix/zerrors_linux_amd64.go")
parser.add_argument("destination", type=pathlib.Path)
args = parser.parse_args()
source = args.source.read_text()
start = source.index("var errorList =")
end = source.index("var signalList =", start)
rows = re.findall(r'\{(\d+), "[^"\\]+", "([^"\\]*(?:\\.[^"\\]*)*)"\}', source[start:end])
if len(rows) != 131:
    raise ValueError(f"Pinned error-list count changed: {len(rows)}")
header = """// Copyright 2009 The Go Authors. All rights reserved.
// BSD license: licenses/XSys-LICENSE.txt (x/sys v0.48.0).
// Exact Linux amd64 errorList text from unix/zerrors_linux_amd64.go.
namespace TypeScript.FsWatch;
internal static partial class LinuxKernel
{
    private static string ErrorText(int number) => number switch
    {
"""
body = "".join(f'        {number} => "{message}",\n' for number, message in rows)
footer = """        _ => "errno " + number.ToString(System.Globalization.CultureInfo.InvariantCulture)
    };
}
"""
args.destination.write_text(header + body + footer)
