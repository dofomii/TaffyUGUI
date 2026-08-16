#!/usr/bin/env python3
"""Canonical local TaffyUGUI build driver.

The implementation is stored in ordered source segments to keep the local-first build
workflow maintainable and easy to mirror. Segments execute in this module's shared
global namespace and preserve the original driver behavior.
"""
from pathlib import Path as _DriverPath

_PARTS = _DriverPath(__file__).resolve().parent / "driver_parts"
for _part in sorted(_PARTS.glob("[0-9][0-9].py")):
    exec(compile(_part.read_text(encoding="utf-8"), str(_part), "exec"), globals(), globals())
