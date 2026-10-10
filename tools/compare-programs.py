#!/usr/bin/env python3
"""Strict full-record Program comparison; never generate expected output."""
import importlib.util
from pathlib import Path

spec = importlib.util.spec_from_file_location("retained_exact_comparator", Path(__file__).with_name("compare-resolution.py"))
comparison = importlib.util.module_from_spec(spec)
spec.loader.exec_module(comparison)
comparison.__doc__ = __doc__

if __name__ == "__main__":
    raise SystemExit(comparison.main())
