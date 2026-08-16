#!/usr/bin/env python3
"""Static Phase 1 architecture checks that do not require a Rust toolchain.

This is supplemental: it validates that the full Phase 1 implementation surface is present,
but it never replaces rustfmt, Clippy, Rust tests, or the locked release build.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(relative: str) -> str:
    return (ROOT / relative).read_text(encoding="utf-8")


def require(text: str, needle: str, description: str, errors: list[str]) -> None:
    if needle not in text:
        errors.append(f"missing {description}: {needle!r}")


def forbid(text: str, needle: str, description: str, errors: list[str]) -> None:
    if needle in text:
        errors.append(f"forbidden {description}: {needle!r}")


def require_all(text: str, needles: list[str], group: str, errors: list[str]) -> None:
    for needle in needles:
        require(text, needle, f"{group} support", errors)


def main() -> int:
    errors: list[str] = []

    tracker = read("docs/TASK_TRACKER.md")
    context = read("native/src/context.rs")
    handles = read("native/src/handles.rs")
    style = read("native/src/style.rs")
    grid = read("native/src/grid.rs")
    calc = read("native/src/calc.rs")
    measurement = read("native/src/measurement.rs")
    lib = read("native/src/lib.rs")
    cargo = read("native/Cargo.toml")

    require(tracker, "Phase 1 — Complete Rust/Taffy 0.13 Engine", "active Phase 1 tracker", errors)
    require(cargo, 'taffy = { version = "=0.13.0"', "exact Taffy 0.13.0 pin", errors)
    for feature in ["taffy_tree", "flexbox", "grid", "block_layout", "float_layout", "calc", "content_size", "detailed_layout_info"]:
        require(cargo, f'"{feature}"', f"Taffy feature {feature}", errors)

    require_all(context, [
        "struct NativeTree", "Vec<Option<NodeState>>", "struct ContextRegistry", "thread_local!",
        "static CONTEXT_REGISTRY", "RefCell<ContextRegistry>", "AtomicU32",
        "create_registered_context", "destroy_registered_context", "with_registered_context_mut",
        "try_borrow_mut()",
    ], "P1.1 context arena", errors)
    require_all(handles, ["struct ContextHandle(u64)", "struct NodeHandle(u64)", "struct ResourceHandle(u64)", "encode_parts", "decode_parts"], "P1.2/P1.3 generation handles", errors)
    require_all(context, ["fn add_node", "fn remove_node", "fn clear", "fn set_children", "would_create_cycle"], "P1.4 persistent topology", errors)
    require_all(context, ["Cache", "fn mark_dirty", "fn is_dirty", "mutation_generation", "last_compute"], "P1.5 cached dirty state", errors)

    require_all(style, ["Display::None", "BoxSizing::ContentBox", "Direction::Rtl", "Position::Absolute", "Overflow::Scroll", "scrollbar_width"], "P1.6/P1.7/P1.11/P1.12 core styles", errors)
    require_all(style, ["size:", "min_size:", "max_size:", "aspect_ratio", "margin:", "padding:", "border:"], "P1.9/P1.10 box geometry", errors)
    require_all(calc, ["enum CalcExpr", "Length(f32)", "Percent(f32)", "Add(ResourceHandle", "Clamp", "Dimension::calc", "resolve_ptr"], "P1.8 typed Calc", errors)
    require_all(context, ["content_width", "content_height", "scroll_width", "scroll_height"], "P1.13 content-size results", errors)

    require_all(style, ["FlexDirection::ColumnReverse", "FlexWrap::WrapReverse", "flex_basis", "flex_grow", "flex_shrink", "align_items", "align_self", "align_content", "justify_content", "gap:"], "P1.14-P1.18 Flex", errors)
    require_all(context, ["Display::Block", "Display::FlowRoot", "compute_block_layout"], "P1.19/P1.20 Block/FlowRoot", errors)
    require_all(style, ["Float::Left", "Clear::Right"], "P1.21 Float/Clear", errors)

    require_all(grid, [
        "struct GridTemplateResource", "grid_template_rows", "grid_template_columns", "auto_rows", "auto_columns",
        "fixed_track", "percent_track", "fraction_track", "auto_track", "minmax_track", "repeat_tracks",
        "named_line", "named_span", "template_areas", "row_line_names", "column_line_names",
    ], "P1.22-P1.30 Grid authoring", errors)
    require_all(context, ["DetailedGridInfo", "DetailedLayoutInfo::Grid", "set_detailed_grid_info", "detailed_layout_info"], "P1.31 Grid diagnostics", errors)

    require_all(measurement, ["struct MeasurementRecord", "struct MeasurementSample", "known", "AvailableSpace::MinContent", "AvailableSpace::MaxContent", "is_replaced", "aspect_ratio", "width_samples"], "P1.32/P1.33 measurements", errors)
    require_all(context, ["fn set_measurement", "mark_dirty(node)", "fn set_measurements_bulk"], "P1.34/P1.36 measurement invalidation/bulk upload", errors)
    forbid(context, "extern \"C\"", "managed callback in native engine", errors)
    require(context, "fn set_styles_bulk", "P1.35 bulk style upload", errors)
    require(context, "fn set_children_bulk", "P1.37 bulk topology operation", errors)
    require_all(context, ["if self.last_compute == Some(key)", "compute_count"], "P1.38 one compute per generation", errors)
    require(context, "fn layouts_bulk", "P1.39 bulk result retrieval", errors)

    require(lib, "mod calc;", "Calc module wiring", errors)
    forbid(context, "unsafe impl Send", "unsafe Send override", errors)
    forbid(context, "unsafe impl Sync", "unsafe Sync override", errors)

    # Simple source sanity checks that catch accidental duplicated declarations or unbalanced braces.
    for name, text in {"context.rs": context, "calc.rs": calc, "grid.rs": grid, "measurement.rs": measurement, "style.rs": style}.items():
        if text.count("{") != text.count("}"):
            errors.append(f"unbalanced braces in {name}")
        duplicate_tests = [match.group(1) for match in re.finditer(r"fn\s+(\w+)\s*\([^)]*\)\s*\{", text)]
        if len(duplicate_tests) != len(set(duplicate_tests)):
            # Methods with the same name in different impl blocks are legitimate; only flag exact test duplicates.
            tests = re.findall(r"#\[test\]\s*fn\s+(\w+)", text)
            if len(tests) != len(set(tests)):
                errors.append(f"duplicate test function name in {name}")

    if errors:
        print("Phase 1 static preflight FAILED:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print("Phase 1 static preflight passed: P1.1-P1.39 implementation surface is present.")
    print("Note: rustfmt/Clippy/Rust tests/locked release build are still mandatory verification gates.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
