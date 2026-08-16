use core::mem::{align_of, offset_of, size_of};
use std::ptr;
use taffy_ugui::*;
const OK: i32 = TuStatus::Ok as i32;
fn typed_value(kind: TuValueKind, scalar: f32) -> TuValue {
    TuValue {
        kind: kind as i32,
        value: scalar,
        resource: 0,
    }
}
fn px(value: f32) -> TuValue {
    typed_value(TuValueKind::Length, value)
}
fn calc_value(resource: TuResourceHandle) -> TuValue {
    TuValue {
        kind: TuValueKind::Calc as i32,
        value: 0.0,
        resource,
    }
}
fn base_style(display: TuDisplay) -> TuStyle {
    let mut style: TuStyle = unsafe { core::mem::zeroed() };
    style.display = display as i32;
    style.box_sizing = TuBoxSizing::BorderBox as i32;
    style.direction = TuDirection::Ltr as i32;
    style.overflow_x = TuOverflow::Visible as i32;
    style.overflow_y = TuOverflow::Visible as i32;
    style.position = TuPosition::Relative as i32;
    style.margin_left = px(0.0);
    style.margin_right = px(0.0);
    style.margin_top = px(0.0);
    style.margin_bottom = px(0.0);
    style.flex_direction = TuFlexDirection::Row as i32;
    style.flex_wrap = TuFlexWrap::NoWrap as i32;
    style.flex_shrink = 1.0;
    style.align_items = TuAlign::Unset as i32;
    style.align_self = TuAlign::Unset as i32;
    style.align_content = TuAlignContent::Unset as i32;
    style.justify_content = TuAlignContent::Unset as i32;
    style.justify_items = TuAlign::Unset as i32;
    style.justify_self = TuAlign::Unset as i32;
    style.float_mode = TuFloatMode::None as i32;
    style.clear_mode = TuClearMode::None as i32;
    style.text_align = TuTextAlign::Auto as i32;
    style.grid_auto_flow = TuGridAutoFlow::Row as i32;
    style
}
fn fixed_style(display: TuDisplay, width: f32, height: f32) -> TuStyle {
    let mut style = base_style(display);
    style.width = px(width);
    style.height = px(height);
    style
}
struct Context(TuContextHandle);
impl Context {
    fn new() -> Self {
        let mut handle = 0;
        assert_eq!(unsafe { tu_context_create(&mut handle) }, OK);
        assert_ne!(handle, 0);
        Self(handle)
    }
    fn node(&self, style: &TuStyle) -> TuNodeHandle {
        let mut handle = 0;
        assert_eq!(unsafe { tu_node_create(self.0, style, &mut handle) }, OK);
        assert_ne!(handle, 0);
        handle
    }
    fn children(&self, parent: TuNodeHandle, children: &[TuNodeHandle]) {
        assert_eq!(
            unsafe {
                tu_node_set_children(self.0, parent, children.as_ptr(), children.len() as u32)
            },
            OK
        )
    }
    fn compute(&self, root: TuNodeHandle, width: f32, height: f32) {
        assert_eq!(tu_compute_layout(self.0, root, width, height), OK)
    }
    fn layout(&self, node: TuNodeHandle) -> TuLayout {
        let mut layout: TuLayout = unsafe { core::mem::zeroed() };
        assert_eq!(unsafe { tu_get_layout(self.0, node, &mut layout) }, OK);
        layout
    }
}
impl Drop for Context {
    fn drop(&mut self) {
        let _ = tu_context_destroy(self.0);
    }
}
fn approx(actual: f32, expected: f32) {
    assert!(
        (actual - expected).abs() <= 0.01,
        "actual={actual} expected={expected}"
    )
}
fn line(index: i32) -> TuGridPlacement {
    TuGridPlacement {
        kind: TuGridPlacementKind::Line as i32,
        line: index,
        span: 0,
        occurrence: 0,
        name: TuStringView {
            data: ptr::null(),
            len: 0,
        },
    }
}
fn named_line(name: &[u8], occurrence: i32) -> TuGridPlacement {
    TuGridPlacement {
        kind: TuGridPlacementKind::NamedLine as i32,
        line: 0,
        span: 0,
        occurrence,
        name: TuStringView {
            data: name.as_ptr(),
            len: name.len() as u32,
        },
    }
}
fn fixed_track(value: f32) -> TuGridTrack {
    let mut track: TuGridTrack = unsafe { core::mem::zeroed() };
    track.kind = TuGridTrackKind::Length as i32;
    track.value = value;
    track
}

#[test]
fn p3_1_context_handle_error_and_version_units() {
    assert_eq!(
        tu_get_abi_version(),
        1,
        "ABI-v1-RC must remain locked after the verified Phase 3 gate"
    );
    assert_eq!(tu_get_abi_stage(), 1);
    assert_eq!(tu_get_taffy_version_packed(), 13 << 12);
    assert_ne!(tu_get_capabilities(), 0);
    let context = Context::new();
    let node = context.node(&fixed_style(TuDisplay::Flex, 10.0, 10.0));
    assert_eq!(tu_node_remove(context.0, node), OK);
    assert_eq!(
        tu_node_mark_dirty(context.0, node),
        TuStatus::InvalidNode as i32
    );
    assert!(tu_get_last_error_length() > 0);
}

#[test]
fn p3_2_flex_golden_geometry() {
    let context = Context::new();
    let mut root_style = fixed_style(TuDisplay::Flex, 300.0, 100.0);
    root_style.gap_x = px(10.0);
    let root = context.node(&root_style);
    let a = context.node(&fixed_style(TuDisplay::Flex, 50.0, 20.0));
    let b = context.node(&fixed_style(TuDisplay::Flex, 70.0, 30.0));
    context.children(root, &[a, b]);
    context.compute(root, 300.0, 100.0);
    let la = context.layout(a);
    let lb = context.layout(b);
    approx(la.x, 0.0);
    approx(la.y, 0.0);
    approx(la.width, 50.0);
    approx(la.height, 20.0);
    approx(lb.x, 60.0);
    approx(lb.y, 0.0);
    approx(lb.width, 70.0);
    approx(lb.height, 30.0);
}

#[test]
fn p3_3_block_flowroot_float_golden_geometry() {
    let context = Context::new();
    let root = context.node(&fixed_style(TuDisplay::FlowRoot, 200.0, 100.0));
    let mut fs = fixed_style(TuDisplay::Block, 50.0, 20.0);
    fs.float_mode = TuFloatMode::Left as i32;
    let floated = context.node(&fs);
    let mut cs = fixed_style(TuDisplay::Block, 80.0, 15.0);
    cs.clear_mode = TuClearMode::Both as i32;
    let cleared = context.node(&cs);
    context.children(root, &[floated, cleared]);
    context.compute(root, 200.0, 100.0);
    let f = context.layout(floated);
    let c = context.layout(cleared);
    approx(f.x, 0.0);
    approx(f.y, 0.0);
    approx(f.width, 50.0);
    approx(f.height, 20.0);
    assert!(c.y >= 20.0);
    approx(c.width, 80.0);
    approx(c.height, 15.0);
}

#[test]
fn p3_4_grid_named_area_and_placement_golden_geometry() {
    let context = Context::new();
    let root = context.node(&fixed_style(TuDisplay::Grid, 200.0, 100.0));
    let rows = [fixed_track(40.0), fixed_track(60.0)];
    let columns = [fixed_track(100.0), fixed_track(100.0)];
    let start = b"content-start";
    let end = b"content-end";
    let named_lines = [
        TuNamedGridLine {
            axis: TuGridAxis::Column as i32,
            line_index: 0,
            name: TuStringView {
                data: start.as_ptr(),
                len: start.len() as u32,
            },
        },
        TuNamedGridLine {
            axis: TuGridAxis::Column as i32,
            line_index: 1,
            name: TuStringView {
                data: end.as_ptr(),
                len: end.len() as u32,
            },
        },
    ];
    let area_name = b"content";
    let areas = [TuGridArea {
        name: TuStringView {
            data: area_name.as_ptr(),
            len: area_name.len() as u32,
        },
        row_start: 1,
        row_end: 2,
        column_start: 1,
        column_end: 2,
    }];
    let template = TuGridTemplate {
        rows: rows.as_ptr(),
        row_count: 2,
        columns: columns.as_ptr(),
        column_count: 2,
        auto_rows: ptr::null(),
        auto_row_count: 0,
        auto_columns: ptr::null(),
        auto_column_count: 0,
        named_lines: named_lines.as_ptr(),
        named_line_count: 2,
        areas: areas.as_ptr(),
        area_count: 1,
        area_rows: 2,
        area_columns: 2,
    };
    assert_eq!(
        unsafe { tu_node_set_grid_template(context.0, root, &template) },
        OK
    );
    let mut item_style = fixed_style(TuDisplay::Flex, 25.0, 20.0);
    item_style.grid_row_start = line(2);
    item_style.grid_row_end = line(3);
    item_style.grid_column_start = named_line(start, 1);
    item_style.grid_column_end = named_line(end, 1);
    let item = context.node(&item_style);
    context.children(root, &[item]);
    context.compute(root, 200.0, 100.0);
    let l = context.layout(item);
    approx(l.x, 0.0);
    approx(l.y, 40.0);
    approx(l.width, 25.0);
    approx(l.height, 20.0);
    let mut info: TuGridInfo = unsafe { core::mem::zeroed() };
    assert_eq!(unsafe { tu_get_grid_info(context.0, root, &mut info) }, OK);
    assert_eq!(info.explicit_rows, 2);
    assert_eq!(info.explicit_columns, 2);
    assert_eq!(info.item_count, 1);
}

#[test]
fn p3_5_calc_and_measurement_golden_geometry() {
    let context = Context::new();
    let ps = TuCalcSpec {
        op: TuCalcOp::Percent as i32,
        value: 0.5,
        operands: ptr::null(),
        operand_count: 0,
    };
    let ls = TuCalcSpec {
        op: TuCalcOp::Length as i32,
        value: 10.0,
        operands: ptr::null(),
        operand_count: 0,
    };
    let mut ph = 0;
    let mut lh = 0;
    assert_eq!(unsafe { tu_calc_create(context.0, &ps, &mut ph) }, OK);
    assert_eq!(unsafe { tu_calc_create(context.0, &ls, &mut lh) }, OK);
    let ops = [ph, lh];
    let add = TuCalcSpec {
        op: TuCalcOp::Add as i32,
        value: 0.0,
        operands: ops.as_ptr(),
        operand_count: 2,
    };
    let mut ch = 0;
    assert_eq!(unsafe { tu_calc_create(context.0, &add, &mut ch) }, OK);
    let mut rs = base_style(TuDisplay::Flex);
    rs.width = calc_value(ch);
    rs.height = px(40.0);
    let root = context.node(&rs);
    context.compute(root, 200.0, 40.0);
    approx(context.layout(root).width, 110.0);
    let measured = context.node(&base_style(TuDisplay::Flex));
    let m = TuMeasurement {
        min_width: 20.0,
        min_height: 10.0,
        max_width: 80.0,
        max_height: 30.0,
        preferred_width: 60.0,
        preferred_height: 24.0,
        aspect_ratio: 0.0,
        is_replaced: 0,
        samples: ptr::null(),
        sample_count: 0,
    };
    assert_eq!(
        unsafe { tu_node_set_measurement(context.0, measured, &m) },
        OK
    );
    context.compute(measured, f32::INFINITY, f32::INFINITY);
    let l = context.layout(measured);
    approx(l.width, 80.0);
    approx(l.height, 30.0);
}

#[test]
fn p3_6_struct_size_alignment_and_enum_numeric_contract() {
    assert_eq!(size_of::<TuContextHandle>(), 8);
    assert_eq!(size_of::<TuNodeHandle>(), 8);
    assert_eq!(size_of::<TuResourceHandle>(), 8);
    assert_eq!(size_of::<TuStatus>(), 4);
    assert_eq!(size_of::<TuDisplay>(), 4);
    assert_eq!(TuStatus::Ok as i32, 0);
    assert_eq!(TuStatus::InternalPanic as i32, -13);
    assert_eq!(TuDisplay::FlowRoot as i32, 4);
    assert_eq!(TuGridTrackKind::Repeat as i32, 8);
    assert_eq!(TuAlign::Unset as i32, -1);
    assert_eq!(offset_of!(TuValue, kind), 0);
    assert_eq!(offset_of!(TuValue, value), 4);
    assert_eq!(offset_of!(TuValue, resource), 8);
    assert_eq!(offset_of!(TuLayout, node), 0);
    assert_eq!(offset_of!(TuLayout, order), 8);
    #[cfg(target_pointer_width = "64")]
    {
        assert_eq!(
            (size_of::<TuStringView>(), align_of::<TuStringView>()),
            (16, 8)
        );
        assert_eq!((size_of::<TuValue>(), align_of::<TuValue>()), (16, 8));
        assert_eq!(
            (size_of::<TuGridPlacement>(), align_of::<TuGridPlacement>()),
            (32, 8)
        );
        assert_eq!((size_of::<TuStyle>(), align_of::<TuStyle>()), (632, 8));
        assert_eq!(
            (size_of::<TuStyleUpdate>(), align_of::<TuStyleUpdate>()),
            (640, 8)
        );
        assert_eq!(
            (size_of::<TuMeasurement>(), align_of::<TuMeasurement>()),
            (48, 8)
        );
        assert_eq!(
            (
                size_of::<TuMeasurementUpdate>(),
                align_of::<TuMeasurementUpdate>()
            ),
            (64, 8)
        );
        assert_eq!(
            (
                size_of::<TuChildrenUpdate>(),
                align_of::<TuChildrenUpdate>()
            ),
            (24, 8)
        );
        assert_eq!((size_of::<TuLayout>(), align_of::<TuLayout>()), (48, 8));
        assert_eq!((size_of::<TuCalcSpec>(), align_of::<TuCalcSpec>()), (24, 8));
        assert_eq!(
            (size_of::<TuGridTrack>(), align_of::<TuGridTrack>()),
            (72, 8)
        );
        assert_eq!(
            (size_of::<TuGridTemplate>(), align_of::<TuGridTemplate>()),
            (104, 8)
        );
    }
}

#[test]
fn p3_7_invalid_stale_cross_context_and_malformed_input_contract() {
    let a = Context::new();
    let b = Context::new();
    let node_a = a.node(&fixed_style(TuDisplay::Flex, 10.0, 10.0));
    assert_eq!(
        tu_node_mark_dirty(b.0, node_a),
        TuStatus::InvalidNode as i32
    );
    assert_eq!(
        unsafe { tu_node_create(a.0, ptr::null(), ptr::null_mut()) },
        TuStatus::NullPointer as i32
    );
    assert_eq!(
        tu_compute_layout(a.0, node_a, f32::NAN, 10.0),
        TuStatus::InvalidNumber as i32
    );
    let mut invalid = base_style(TuDisplay::Flex);
    invalid.display = i32::MAX;
    let mut out = 0;
    assert_eq!(
        unsafe { tu_node_create(a.0, &invalid, &mut out) },
        TuStatus::InvalidEnum as i32
    );
    let children = [node_a];
    assert_eq!(
        unsafe { tu_node_set_children(a.0, node_a, children.as_ptr(), 1) },
        TuStatus::InvalidValue as i32
    );
    assert_eq!(tu_node_remove(a.0, node_a), OK);
    assert_eq!(tu_node_remove(a.0, node_a), TuStatus::InvalidNode as i32);
}

#[test]
fn p3_8_repeated_lifecycle_and_topology_stress() {
    let context = Context::new();
    for iteration in 0..500_u32 {
        let root = context.node(&fixed_style(TuDisplay::Flex, 100.0, 30.0));
        let a = context.node(&fixed_style(TuDisplay::Flex, 10.0, 10.0));
        let b = context.node(&fixed_style(TuDisplay::Flex, 20.0, 10.0));
        context.children(root, &[a, b]);
        context.compute(root, 100.0, 30.0);
        assert_eq!(context.layout(a).node, a);
        assert_eq!(tu_node_remove(context.0, a), OK);
        context.children(root, &[b]);
        context.compute(root, 100.0, 30.0);
        assert_eq!(tu_node_remove(context.0, b), OK);
        assert_eq!(tu_node_remove(context.0, root), OK);
        if iteration % 50 == 0 {
            assert_eq!(tu_context_clear(context.0), OK);
        }
    }
}

#[test]
fn p3_7_wrong_thread_use_is_rejected() {
    let context = Context::new();
    let raw = context.0;
    let status = std::thread::spawn(move || tu_context_clear(raw))
        .join()
        .unwrap();
    assert_eq!(status, TuStatus::WrongThread as i32);
}
