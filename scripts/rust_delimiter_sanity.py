#!/usr/bin/env python3
"""Lightweight local delimiter sanity pass for Rust when rustc is unavailable.

This is intentionally not a compiler substitute. It catches damaged/reconstructed source files
before the real local Rust gate runs.
"""
from pathlib import Path
import sys
from pygments import lex
from pygments.lexers import RustLexer
from pygments.token import Comment, String

ROOT = Path(__file__).resolve().parents[1]
PAIRS = {')': '(', ']': '[', '}': '{'}
OPEN = set(PAIRS.values())
errors = []
for path in sorted((ROOT / 'native').rglob('*.rs')):
    stack = []
    for token_type, value in lex(path.read_text(encoding='utf-8'), RustLexer()):
        if token_type in Comment or token_type in String:
            continue
        for ch in value:
            if ch in OPEN:
                stack.append(ch)
            elif ch in PAIRS:
                if not stack or stack[-1] != PAIRS[ch]:
                    errors.append(f"{path.relative_to(ROOT)}: unexpected {ch}")
                    stack = []
                    break
                stack.pop()
    if stack:
        errors.append(f"{path.relative_to(ROOT)}: unclosed delimiters {''.join(stack[-10:])}")

if errors:
    print('RUST DELIMITER SANITY: FAILED', file=sys.stderr)
    for e in errors:
        print(' - ' + e, file=sys.stderr)
    raise SystemExit(1)
print('RUST DELIMITER SANITY: PASS (not a substitute for rustc)')
