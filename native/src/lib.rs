use std::collections::HashMap;
use std::ffi::c_void;
use taffy::prelude::*;

const OK: i32 = 0;
const ERR_NULL: i32 = -1;
const ERR_NODE: i32 = -2;
const ERR_TAFFY: i32 = -3;

#[repr(C)]
#[derive(Clone, Copy, Default)]
pub struct TaffyUGUIDimension {
    /// 0 = auto, 1 = points, 2 = percent (0..1)
    pub unit: i32,
    pub value: f32,
}

#[repr(C)]
#[derive(Clone, Copy, Default)]
pub struct TaffyUGUIStyle {
    pub flex_direction: i32,
    pub flex_wrap: i32,
    pub width: TaffyUGUIDimension,
    pub height: TaffyUGUIDimension,
    pub min_width: TaffyUGUIDimension,
    pub min_height: TaffyUGUIDimension,
    pub max_width: TaffyUGUIDimension,
    pub max_height: TaffyUGUIDimension,
    pub flex_basis: TaffyUGUIDimension,
    pub flex_grow: f32,
    pub flex_shrink: f32,
    pub gap_x: f32,
    pub gap_y: f32,
    pub padding_left: f32,
    pub padding_right: f32,
    pub padding_top: f32,
    pub padding_bottom: f32,
    /// -1 = inherit/default, 0=start, 1=end, 2=center, 3=stretch
    pub align_items: i32,
    /// -1 = inherit/default, 0=start, 1=end, 2=center, 3=stretch
    pub align_self: i32,
    /// -1 = default, 0=start, 1=end, 2=center, 3=space-between, 4=space-around, 5=space-evenly
    pub justify_content: i32,
    pub aspect_ratio: f32,
}

#[repr(C)]
#[derive(Clone, Copy, Default)]
pub struct TaffyUGUILayout {
    pub x: f32,
    pub y: f32,
    pub width: f32,
    pub height: f32,
}

struct Context {
    tree: TaffyTree<()>,
    nodes: HashMap<u64, NodeId>,
    next_id: u64,
}

fn dim(v: TaffyUGUIDimension) -> Dimension {
    match v.unit {
        1 => Dimension::length(v.value.max(0.0)),
        2 => Dimension::percent(v.value),
        _ => Dimension::auto(),
    }
}

fn align_items(v: i32) -> Option<AlignItems> {
    match v {
        0 => Some(AlignItems::START),
        1 => Some(AlignItems::END),
        2 => Some(AlignItems::CENTER),
        3 => Some(AlignItems::STRETCH),
        _ => None,
    }
}

fn align_self(v: i32) -> Option<AlignSelf> {
    match v {
        0 => Some(AlignSelf::START),
        1 => Some(AlignSelf::END),
        2 => Some(AlignSelf::CENTER),
        3 => Some(AlignSelf::STRETCH),
        _ => None,
    }
}

fn justify(v: i32) -> Option<JustifyContent> {
    match v {
        0 => Some(JustifyContent::START),
        1 => Some(JustifyContent::END),
        2 => Some(JustifyContent::CENTER),
        3 => Some(JustifyContent::SPACE_BETWEEN),
        4 => Some(JustifyContent::SPACE_AROUND),
        5 => Some(JustifyContent::SPACE_EVENLY),
        _ => None,
    }
}

fn to_style(s: TaffyUGUIStyle) -> Style {
    Style {
        display: Display::Flex,
        flex_direction: match s.flex_direction {
            1 => FlexDirection::Column,
            2 => FlexDirection::RowReverse,
            3 => FlexDirection::ColumnReverse,
            _ => FlexDirection::Row,
        },
        flex_wrap: match s.flex_wrap {
            1 => FlexWrap::Wrap,
            2 => FlexWrap::WrapReverse,
            _ => FlexWrap::NoWrap,
        },
        size: Size {
            width: dim(s.width),
            height: dim(s.height),
        },
        min_size: Size {
            width: dim(s.min_width),
            height: dim(s.min_height),
        },
        max_size: Size {
            width: dim(s.max_width),
            height: dim(s.max_height),
        },
        flex_basis: dim(s.flex_basis),
        flex_grow: s.flex_grow.max(0.0),
        flex_shrink: s.flex_shrink.max(0.0),
        gap: Size {
            width: LengthPercentage::length(s.gap_x.max(0.0)),
            height: LengthPercentage::length(s.gap_y.max(0.0)),
        },
        padding: Rect {
            left: LengthPercentage::length(s.padding_left.max(0.0)),
            right: LengthPercentage::length(s.padding_right.max(0.0)),
            top: LengthPercentage::length(s.padding_top.max(0.0)),
            bottom: LengthPercentage::length(s.padding_bottom.max(0.0)),
        },
        align_items: align_items(s.align_items),
        align_self: align_self(s.align_self),
        justify_content: justify(s.justify_content),
        aspect_ratio: if s.aspect_ratio > 0.0 {
            Some(s.aspect_ratio)
        } else {
            None
        },
        ..Default::default()
    }
}

unsafe fn context<'a>(ptr: *mut c_void) -> Option<&'a mut Context> {
    (ptr as *mut Context).as_mut()
}

/// Returns the bootstrap ABI version.
///
/// Version `0` means the ABI is still under development and is not the frozen v1 contract.
#[no_mangle]
pub extern "C" fn taffy_ugui_api_version() -> u32 {
    0
}

#[no_mangle]
pub extern "C" fn taffy_ugui_create_context() -> *mut c_void {
    let ctx = Context {
        tree: TaffyTree::new(),
        nodes: HashMap::new(),
        next_id: 1,
    };
    Box::into_raw(Box::new(ctx)) as *mut c_void
}

/// Destroys a context previously created by [`taffy_ugui_create_context`].
///
/// # Safety
///
/// `ptr` must either be null or be a live pointer returned by
/// [`taffy_ugui_create_context`] that has not already been destroyed. No other thread or
/// caller may access the context while this function executes, and the pointer must not be
/// used again after this call.
#[no_mangle]
pub unsafe extern "C" fn taffy_ugui_destroy_context(ptr: *mut c_void) {
    if !ptr.is_null() {
        drop(Box::from_raw(ptr as *mut Context));
    }
}

/// Creates a leaf node in a live context.
///
/// # Safety
///
/// `ptr` must be a live context pointer created by [`taffy_ugui_create_context`] and must
/// be exclusively accessible for the duration of the call. `out_id` must be null or point
/// to writable memory for one `u64`; when non-null, this function writes the new node id to
/// it on success.
#[no_mangle]
pub unsafe extern "C" fn taffy_ugui_create_node(
    ptr: *mut c_void,
    style: TaffyUGUIStyle,
    out_id: *mut u64,
) -> i32 {
    let Some(ctx) = context(ptr) else {
        return ERR_NULL;
    };
    let Some(out_id) = out_id.as_mut() else {
        return ERR_NULL;
    };
    match ctx.tree.new_leaf(to_style(style)) {
        Ok(node) => {
            let id = ctx.next_id;
            ctx.next_id += 1;
            ctx.nodes.insert(id, node);
            *out_id = id;
            OK
        }
        Err(_) => ERR_TAFFY,
    }
}

/// Removes a node from a live context.
///
/// # Safety
///
/// `ptr` must be a live context pointer created by [`taffy_ugui_create_context`] and must
/// be exclusively accessible for the duration of the call.
#[no_mangle]
pub unsafe extern "C" fn taffy_ugui_remove_node(ptr: *mut c_void, id: u64) -> i32 {
    let Some(ctx) = context(ptr) else {
        return ERR_NULL;
    };
    let Some(node) = ctx.nodes.remove(&id) else {
        return ERR_NODE;
    };
    ctx.tree.remove(node).map(|_| OK).unwrap_or(ERR_TAFFY)
}

/// Updates the style of a node in a live context.
///
/// # Safety
///
/// `ptr` must be a live context pointer created by [`taffy_ugui_create_context`] and must
/// be exclusively accessible for the duration of the call.
#[no_mangle]
pub unsafe extern "C" fn taffy_ugui_set_style(
    ptr: *mut c_void,
    id: u64,
    style: TaffyUGUIStyle,
) -> i32 {
    let Some(ctx) = context(ptr) else {
        return ERR_NULL;
    };
    let Some(&node) = ctx.nodes.get(&id) else {
        return ERR_NODE;
    };
    ctx.tree
        .set_style(node, to_style(style))
        .map(|_| OK)
        .unwrap_or(ERR_TAFFY)
}

/// Replaces the children of a node in a live context.
///
/// # Safety
///
/// `ptr` must be a live context pointer created by [`taffy_ugui_create_context`] and must
/// be exclusively accessible for the duration of the call. If `count` is non-zero,
/// `child_ids` must point to at least `count` contiguous, initialized `u64` values that
/// remain readable for the duration of the call. If `count` is zero, `child_ids` may be
/// null.
#[no_mangle]
pub unsafe extern "C" fn taffy_ugui_set_children(
    ptr: *mut c_void,
    id: u64,
    child_ids: *const u64,
    count: usize,
) -> i32 {
    let Some(ctx) = context(ptr) else {
        return ERR_NULL;
    };
    let Some(&node) = ctx.nodes.get(&id) else {
        return ERR_NODE;
    };
    if count > 0 && child_ids.is_null() {
        return ERR_NULL;
    }
    let ids = if count == 0 {
        &[][..]
    } else {
        std::slice::from_raw_parts(child_ids, count)
    };
    let mut children = Vec::with_capacity(count);
    for child_id in ids {
        let Some(&child) = ctx.nodes.get(child_id) else {
            return ERR_NODE;
        };
        children.push(child);
    }
    ctx.tree
        .set_children(node, &children)
        .map(|_| OK)
        .unwrap_or(ERR_TAFFY)
}

/// Marks a node dirty in a live context.
///
/// # Safety
///
/// `ptr` must be a live context pointer created by [`taffy_ugui_create_context`] and must
/// be exclusively accessible for the duration of the call.
#[no_mangle]
pub unsafe extern "C" fn taffy_ugui_mark_dirty(ptr: *mut c_void, id: u64) -> i32 {
    let Some(ctx) = context(ptr) else {
        return ERR_NULL;
    };
    let Some(&node) = ctx.nodes.get(&id) else {
        return ERR_NODE;
    };
    ctx.tree.mark_dirty(node).map(|_| OK).unwrap_or(ERR_TAFFY)
}

/// Computes layout for a root node in a live context.
///
/// # Safety
///
/// `ptr` must be a live context pointer created by [`taffy_ugui_create_context`] and must
/// be exclusively accessible for the duration of the call.
#[no_mangle]
pub unsafe extern "C" fn taffy_ugui_compute_layout(
    ptr: *mut c_void,
    root_id: u64,
    width: f32,
    height: f32,
) -> i32 {
    let Some(ctx) = context(ptr) else {
        return ERR_NULL;
    };
    let Some(&root) = ctx.nodes.get(&root_id) else {
        return ERR_NODE;
    };
    let available = Size {
        width: if width.is_finite() {
            AvailableSpace::Definite(width.max(0.0))
        } else {
            AvailableSpace::MaxContent
        },
        height: if height.is_finite() {
            AvailableSpace::Definite(height.max(0.0))
        } else {
            AvailableSpace::MaxContent
        },
    };
    ctx.tree
        .compute_layout(root, available)
        .map(|_| OK)
        .unwrap_or(ERR_TAFFY)
}

/// Reads the computed layout of a node from a live context.
///
/// # Safety
///
/// `ptr` must be a live context pointer created by [`taffy_ugui_create_context`] and must
/// be exclusively accessible for the duration of the call. `out_layout` must be null or
/// point to writable memory for one [`TaffyUGUILayout`]; when non-null, this function writes
/// the node's layout to it on success.
#[no_mangle]
pub unsafe extern "C" fn taffy_ugui_get_layout(
    ptr: *mut c_void,
    id: u64,
    out_layout: *mut TaffyUGUILayout,
) -> i32 {
    let Some(ctx) = context(ptr) else {
        return ERR_NULL;
    };
    let Some(&node) = ctx.nodes.get(&id) else {
        return ERR_NODE;
    };
    let Some(out_layout) = out_layout.as_mut() else {
        return ERR_NULL;
    };
    match ctx.tree.layout(node) {
        Ok(layout) => {
            *out_layout = TaffyUGUILayout {
                x: layout.location.x,
                y: layout.location.y,
                width: layout.size.width,
                height: layout.size.height,
            };
            OK
        }
        Err(_) => ERR_TAFFY,
    }
}
