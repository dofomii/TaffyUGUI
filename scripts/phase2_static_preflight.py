#!/usr/bin/env python3
from pathlib import Path
import re, shutil, subprocess, sys, tempfile
ROOT=Path(__file__).resolve().parents[1]
ffi=(ROOT/'native/src/ffi.rs').read_text(); err=(ROOT/'native/src/error.rs').read_text(); ctx=(ROOT/'native/src/context.rs').read_text(); hdr=(ROOT/'include/taffy_ugui.h').read_text(); cfg=(ROOT/'cbindgen.toml').read_text()
errors=[]
public_exports=sorted(set(re.findall(r'extern\s+"C"\s+fn\s+(\w+)\s*\(',ffi)))
noncanonical=[x for x in public_exports if not x.startswith('tu_')]
if noncanonical: errors.append('non-tu exports: '+', '.join(noncanonical))
for name in public_exports:
    if name not in hdr: errors.append(f'export missing from header: {name}')
    if name not in cfg: errors.append(f'export missing from cbindgen allowlist: {name}')

public_types = sorted(set(re.findall(r'pub\s+(?:enum|struct|type)\s+(Tu\w+)', ffi)))
for name in public_types:
    if name not in hdr: errors.append(f'public ABI type missing from header: {name}')
    if name not in cfg: errors.append(f'public ABI type missing from cbindgen allowlist: {name}')
required=['tu_get_abi_version','tu_get_build_version_length','tu_copy_build_version','tu_get_capabilities','tu_context_create','tu_context_destroy','tu_context_clear','tu_node_create','tu_node_remove','tu_node_set_style','tu_nodes_set_styles','tu_node_set_children','tu_nodes_set_children','tu_node_set_measurement','tu_nodes_set_measurements','tu_calc_create','tu_calc_remove','tu_node_set_grid_template','tu_get_grid_info','tu_get_grid_track_sizes','tu_get_grid_items','tu_compute_layout','tu_get_layout','tu_get_layouts_bulk','tu_get_last_error_length','tu_copy_last_error']
for name in required:
    if name not in public_exports: errors.append(f'missing required ABI symbol {name}')
if 'catch_unwind' not in ffi or 'InternalPanic' not in ffi: errors.append('panic boundary missing')
for tok in ['NullPointer','InvalidContext','InvalidNode','InvalidResource','InvalidEnum','InvalidCount','InvalidNumber','WrongThread','InternalPanic']:
    if tok not in err: errors.append('status missing '+tok)
for tok in ['context_owners','validate_context_owner','WrongThread']:
    if tok not in ctx: errors.append('thread ownership missing '+tok)
if 'uint32_t' not in hdr or 'uint64_t' not in hdr: errors.append('fixed-width header types missing')
if 'size_t' in hdr or 'usize' in hdr: errors.append('pointer-width ABI type leaked into header')
if re.search(r'\*const|\*mut', ffi) and '#[derive(Clone, Copy, Default)]' in ffi: errors.append('raw-pointer FFI structs must not derive Default at the Rust 1.82 MSRV')
if 'include = [' not in cfg: errors.append('cbindgen public allowlist missing')
unsafe_exports=len(re.findall(r'pub\s+unsafe\s+extern\s+"C"\s+fn',ffi)); safety_docs=len(re.findall(r'^/// # Safety$', ffi, flags=re.M))
if safety_docs < unsafe_exports: errors.append(f'unsafe exports={unsafe_exports}, proper safety sections={safety_docs}')
# Ensure no legacy exported names remain anywhere.
for path in (ROOT/'native/src').glob('*.rs'):
    text=path.read_text()
    if '#[no_mangle]' in text and path.name != 'ffi.rs': errors.append(f'no_mangle export outside ffi.rs: {path.name}')
# Compile the committed header as both C and C++ when clang is available.
clang=shutil.which('clang'); clangxx=shutil.which('clang++')
if clang:
    with tempfile.TemporaryDirectory() as d:
        c=Path(d)/'smoke.c'; c.write_text('#include "taffy_ugui.h"\nint main(void){TuStyle s={0};TuLayout l={0};(void)s;(void)l;return 0;}\n')
        r=subprocess.run([clang,'-std=c11','-Wall','-Wextra','-Werror','-I',str(ROOT/'include'),'-fsyntax-only',str(c)],capture_output=True,text=True)
        if r.returncode: errors.append('C header compile failed: '+r.stderr.strip())
if clangxx:
    with tempfile.TemporaryDirectory() as d:
        c=Path(d)/'smoke.cpp'; c.write_text('#include "taffy_ugui.h"\nint main(){TuStyle s{};TuLayout l{};(void)s;(void)l;return 0;}\n')
        r=subprocess.run([clangxx,'-std=c++17','-Wall','-Wextra','-Werror','-I',str(ROOT/'include'),'-fsyntax-only',str(c)],capture_output=True,text=True)
        if r.returncode: errors.append('C++ header compile failed: '+r.stderr.strip())
if errors:
    print('Phase 2 static preflight FAILED:'); [print(' -',e) for e in errors]; sys.exit(1)
print(f'Phase 2 static preflight passed: {len(public_exports)} canonical tu_* exports; header compiles as C/C++; fixed-width ABI, panic/error/thread checks present.')
print('Note: cbindgen regeneration, rustfmt, Clippy, Rust tests, and locked release build remain mandatory.')
