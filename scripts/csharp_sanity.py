#!/usr/bin/env python3
"""Lightweight managed-source integrity check when a C# compiler/Unity is unavailable."""
from pathlib import Path
import re
import sys
from pygments import lex
from pygments.lexers.dotnet import CSharpLexer
from pygments.token import Comment, String

ROOT = Path(__file__).resolve().parents[1]
errors=[]
for path in sorted((ROOT/'UnityPackage').rglob('*.cs')):
    stack=[]
    pairs={')':'(',']':'[','}':'{'}
    for typ,val in lex(path.read_text(encoding='utf-8'), CSharpLexer()):
        if typ in Comment or typ in String:
            continue
        for ch in val:
            if ch in '([{': stack.append(ch)
            elif ch in pairs:
                if not stack or stack[-1] != pairs[ch]:
                    errors.append(f'{path.relative_to(ROOT)}: unexpected {ch}')
                    stack=[]
                    break
                stack.pop()
    if stack: errors.append(f'{path.relative_to(ROOT)}: unclosed delimiters {"".join(stack[-10:])}')
    text=path.read_text(encoding='utf-8')
    if len(re.findall(r'^\s*#if\b', text, flags=re.M)) != len(re.findall(r'^\s*#endif\b', text, flags=re.M)):
        errors.append(f'{path.relative_to(ROOT)}: unbalanced #if/#endif')
if errors:
    print('C# SANITY: FAILED', file=sys.stderr)
    for e in errors: print(' - '+e, file=sys.stderr)
    raise SystemExit(1)
print('C# SANITY: PASS (Unity compilation is still the authoritative managed check)')
