#!/usr/bin/env python3
"""Project differential comparison using the reviewed exact NDJSON comparer."""
import pathlib
import runpy
runpy.run_path(str(pathlib.Path(__file__).with_name("compare-collections.py")), run_name="__main__")
