//! Exported bootstrap C ABI.
//!
//! ABI version 0 intentionally preserves the original pointer-based bootstrap surface.
//! The production fixed-width ABI is introduced only after the native engine is complete.

use std::ffi::c_void;

use crate::context::Context;
use crate::error::{ERR_NULL, OK};
use crate::handles::BootstrapNodeHandle;
use crate::style::{to_taffy_style, TaffyUGUIStyle};
use crate::version::BOOTSTRAP_API_VERSION;

#[repr(C)]
#[derive(Clone, Copy, Default)]
pub struct TaffyUGUILayout {
    pub x: f32,
    pub y: f32,
    pub width: f32,
    pub height: f32,
}

/// Returns the bootstrap ABI version.
///
/// Version `0` means the ABI is still under development and is not the frozen v1 contract.
#[no_mangle]
pub extern "C" fn taffy_ugui_api_version() -> u32 {
    BOOTSTRAP_API_VERSION
}

#[no_mangle]
pub extern "C" fn taffy_ugui_create_context() -> *mut c_void {
    Context::new().into_opaque_ptr()
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
    unsafe {
        Context::destroy_opaque_ptr(ptr);
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
    out_id: *mut BootstrapNodeHandle,
) -> i32 {
    let Some(ctx) = (unsafe { Context::from_opaque_ptr(ptr) }) else {
        return ERR_NULL;
    };
    let Some(out_id) = (unsafe { out_id.as_mut() }) else {
        return ERR_NULL;
    };

    match ctx.create_node(to_taffy_style(style)) {
        Ok(id) => {
            *out_id = id;
            OK
        }
        Err(error) => error.status_code(),
    }
}

/// Removes a node from a live context.
///
/// # Safety
///
/// `ptr` must be a live context pointer created by [`taffy_ugui_create_context`] and must
/// be exclusively accessible for the duration of the call.
#[no_mangle]
pub unsafe extern "C" fn taffy_ugui_remove_node(
    ptr: *mut c_void,
    id: BootstrapNodeHandle,
) -> i32 {
    let Some(ctx) = (unsafe { Context::from_opaque_ptr(ptr) }) else {
        return ERR_NULL;
    };
    ctx.remove_node(id)
        .map(|_| OK)
        .unwrap_or_else(|error| error.status_code())
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
    id: BootstrapNodeHandle,
    style: TaffyUGUIStyle,
) -> i32 {
    let Some(ctx) = (unsafe { Context::from_opaque_ptr(ptr) }) else {
        return ERR_NULL;
    };
    ctx.set_style(id, to_taffy_style(style))
        .map(|_| OK)
        .unwrap_or_else(|error| error.status_code())
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
    id: BootstrapNodeHandle,
    child_ids: *const BootstrapNodeHandle,
    count: usize,
) -> i32 {
    let Some(ctx) = (unsafe { Context::from_opaque_ptr(ptr) }) else {
        return ERR_NULL;
    };
    if count > 0 && child_ids.is_null() {
        return ERR_NULL;
    }

    let child_ids = if count == 0 {
        &[][..]
    } else {
        unsafe { std::slice::from_raw_parts(child_ids, count) }
    };

    ctx.set_children(id, child_ids)
        .map(|_| OK)
        .unwrap_or_else(|error| error.status_code())
}

/// Marks a node dirty in a live context.
///
/// # Safety
///
/// `ptr` must be a live context pointer created by [`taffy_ugui_create_context`] and must
/// be exclusively accessible for the duration of the call.
#[no_mangle]
pub unsafe extern "C" fn taffy_ugui_mark_dirty(
    ptr: *mut c_void,
    id: BootstrapNodeHandle,
) -> i32 {
    let Some(ctx) = (unsafe { Context::from_opaque_ptr(ptr) }) else {
        return ERR_NULL;
    };
    ctx.mark_dirty(id)
        .map(|_| OK)
        .unwrap_or_else(|error| error.status_code())
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
    root_id: BootstrapNodeHandle,
    width: f32,
    height: f32,
) -> i32 {
    let Some(ctx) = (unsafe { Context::from_opaque_ptr(ptr) }) else {
        return ERR_NULL;
    };
    ctx.compute_layout(root_id, width, height)
        .map(|_| OK)
        .unwrap_or_else(|error| error.status_code())
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
    id: BootstrapNodeHandle,
    out_layout: *mut TaffyUGUILayout,
) -> i32 {
    let Some(ctx) = (unsafe { Context::from_opaque_ptr(ptr) }) else {
        return ERR_NULL;
    };
    let Some(out_layout) = (unsafe { out_layout.as_mut() }) else {
        return ERR_NULL;
    };

    match ctx.layout_rect(id) {
        Ok((x, y, width, height)) => {
            *out_layout = TaffyUGUILayout {
                x,
                y,
                width,
                height,
            };
            OK
        }
        Err(error) => error.status_code(),
    }
}
