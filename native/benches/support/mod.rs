use std::time::Instant;

use taffy_ugui::*;

pub const OK: i32 = TuStatus::Ok as i32;

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
    style.flex_shrink = 0.0;
    style.align_items = TuAlign::Start as i32;
    style.align_self = TuAlign::Unset as i32;
    style.align_content = TuAlignContent::Start as i32;
    style.justify_content = TuAlignContent::Start as i32;
    style.justify_items = TuAlign::Unset as i32;
    style.justify_self = TuAlign::Unset as i32;
    style.float_mode = TuFloatMode::None as i32;
    style.clear_mode = TuClearMode::None as i32;
    style.text_align = TuTextAlign::Auto as i32;
    style.grid_auto_flow = TuGridAutoFlow::Row as i32;
    style
}

pub fn fixed_style(display: TuDisplay, width: f32, height: f32) -> TuStyle {
    let mut style = base_style(display);
    style.width = px(width);
    style.height = px(height);
    style
}

pub struct BenchTree {
    context: TuContextHandle,
    root: TuNodeHandle,
    nodes: Vec<TuNodeHandle>,
}

// This module is compiled separately into multiple custom benchmark binaries; each binary uses a subset of these helpers.
#[allow(dead_code)]
impl BenchTree {
    pub fn new(node_count: usize) -> Self {
        assert!(node_count >= 2, "benchmark requires at least 2 nodes");

        let mut context = 0;
        assert_eq!(unsafe { tu_context_create(&mut context) }, OK);

        let mut root_style = fixed_style(TuDisplay::Flex, 1024.0, 768.0);
        root_style.flex_wrap = TuFlexWrap::Wrap as i32;
        root_style.gap_x = px(4.0);
        root_style.gap_y = px(4.0);

        let root = create_node(context, &root_style);
        let mut nodes = Vec::with_capacity(node_count);
        nodes.push(root);
        for index in 1..node_count {
            let width = 18.0 + ((index % 11) as f32 * 3.0);
            let height = 14.0 + ((index % 7) as f32 * 2.0);
            nodes.push(create_node(
                context,
                &fixed_style(TuDisplay::Flex, width, height),
            ));
        }
        assert_eq!(
            unsafe {
                tu_node_set_children(context, root, nodes[1..].as_ptr(), (nodes.len() - 1) as u32)
            },
            OK
        );

        Self {
            context,
            root,
            nodes,
        }
    }

    pub fn compute(&self) -> i32 {
        tu_compute_layout(self.context, self.root, 1024.0, 768.0)
    }

    pub fn compute_ns(&self) -> u128 {
        let start = Instant::now();
        let status = self.compute();
        let elapsed = start.elapsed();
        assert_eq!(status, OK);
        self.validate();
        elapsed.as_nanos()
    }

    pub fn mark_leaf_dirty(&self) {
        assert_eq!(tu_node_mark_dirty(self.context, self.checksum_node()), OK);
    }

    pub fn validate(&self) {
        let mut layout: TuLayout = unsafe { core::mem::zeroed() };
        assert_eq!(
            unsafe { tu_get_layout(self.context, self.checksum_node(), &mut layout) },
            OK
        );
        assert!(layout.width > 0.0 && layout.height > 0.0);
    }

    pub fn context(&self) -> TuContextHandle {
        self.context
    }

    pub fn nodes(&self) -> &[TuNodeHandle] {
        &self.nodes
    }

    fn checksum_node(&self) -> TuNodeHandle {
        *self.nodes.last().expect("benchmark child")
    }
}

impl Drop for BenchTree {
    fn drop(&mut self) {
        let _ = tu_context_destroy(self.context);
    }
}

fn create_node(context: TuContextHandle, style: &TuStyle) -> TuNodeHandle {
    let mut node = 0;
    assert_eq!(unsafe { tu_node_create(context, style, &mut node) }, OK);
    node
}

pub fn percentile(sorted: &[u128], numerator: usize, denominator: usize) -> u128 {
    let rank = (sorted.len() * numerator).div_ceil(denominator);
    sorted[rank.saturating_sub(1).min(sorted.len() - 1)]
}
