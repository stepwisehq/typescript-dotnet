#!/usr/bin/env python3
"""Reuse exact reversed/repeated canonical-request controls for configuration."""
import pathlib,runpy
runpy.run_path(str(pathlib.Path(__file__).with_name('check-collections-order.py')),run_name='__main__')
