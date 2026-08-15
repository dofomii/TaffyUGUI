//! Exported bootstrap C ABI.
//!
//! ABI version 0 intentionally preserves the original pointer-shaped bootstrap surface.
//! The pointer now identifies only a tiny token; real native state lives in the production
//! context registry and is addressed by a generation-safe internal `ContextHandle`.
//! Bootstrap node ids remain raw `u64` values, but internally they are generation-safe
//! `NodeHandle` values.

use std::ffi::c_void;
use std::ptr;

use crate::context::{
    create_registered_context, destroy_registered_context, with_registered_context_mut,
};
use crate::error::{ERR_NULL, OK};
use crate::handles::{BootstrapNodeHandle, ContextHandle, NodeHandle};
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

struct BootstrapContextToken {
    handle: ContextHandle,
}

impl BootstrapContextToken {
    fn into_opaque_ptr(self) -> *mut c_void {
        Box::into_raw(Box::new(self)) as *mut c_void
    }

    /// Reads a live bootstrap token from an opaque ABI-0 pointer.
    ///
    /// # Safety
    ///
    /// `ptr` must be null or a live pointer returned by [`taffy_ugui_create_context`]. The
    /// token must not be concurrently destroyed for the duration of the returned borrow.
    unsafe fn from_opaque_ptr<'a>(ptr: *mut c_void) -> Option<&'a Self> {
        unsafe { (ptr as *const Self).as_ref() }
    }

    /// Takes ownership of a bootstrap token so it can be destroyed.
    ///
    /// # Safety
    ///
    /// `ptr` must be a non-null live pointer returned by [`taffy_ugui_create_context`] that
    /// has not previously been destroyed. It must not be used again after this call.
    unsafe fn take_from_opaque_ptr(ptr: *mut c_void) -> Box<Self> {
        unsafe { Box::from_raw(ptr as *mut Self) }
    }
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
    match create_registered_context() {
        Ok(handle) => BootstrapContextToken { handle }.into_opaque_ptr(),
        Err(_) => ptr::null_mut(),
    }
}

/// Destroys a context previously created by [`taffy_ugui_create_context`].
///
/// # Safety
///
/// `ptr` must either be null or be a live pointer returned by
/// [`taffy_ugui_create_context`] that has not already been destroyed. No other thread or
/// caller may access the token while this function executes, and the pointer must not be
/// used again after this call.
#[no_mangle]
pub unsafe extern "C" fn taffy_ugui_destroy_context(ptr: *mut c_void) {
    if ptr.is_null() {
        return;
    }

    let token = unsafe { BootstrapContextToken::take_from_opaque_ptr(ptr) };
    let _ = destroy_registered_context(token.handle);
}

/// Creates a leaf node in a live context.
///
/// # Safety
///
/// `ptr` must be a live context token created by [`taffy_ugui_create_context`] and must
/// remain readable for the duration of the call. `out_id` must be null or point to writable
/// memory for one `u64`; when non-null, this function writes the new node id to it on
/// success.
#[no_mangle]
pub unsafe extern "C" fn taffy_ugui_create_node(
    ptr: *mut c_void,
    style: TaffyUGUIStyle,
    out_id: *mut BootstrapNodeHandle,
) -> i32 {
    let Some(handle) = (unsafe { context_handle(ptr) }) else {
        return ERR_NULL;
    };
    let Some(out_id) = (unsafe { out_id.as_mut() }) else {
        return ERR_NULL;
    };

    match with_registered_context_mut(handle, |ctx| ctx.create_node(to_taffy_style(style))) {
        Ok(node_handle) => {
            *out_id = node_handle.raw();
            OK
        }
        Err(error) => error.status_code(),
    }
}

/// Removes a node from a live context.
///
/// # Safety
///
/// `ptr` must be a live context token created by [`taffy_ugui_create_context`] and must
/// remain readable for the duration of the call.
#[no_mangle]
pub unsafe extern "C" fn taffy_ugui_remove_node(ptr: *mut c_void, id: BootstrapNodeHandle) -> i32 {
    with_context_status(ptr, |handle| {
        with_registered_context_mut(handle, |ctx| ctx.remove_node(NodeHandle::from_raw(id)))
    })
}

/// Updates the style of a node in a live context.
///
/// # Safety
///
/// `ptr` must be a live context token created by [`taffy_ugui_create_context`] and must
/// remain readable for the duration of the call.
#[no_mangle]
pub unsafe extern "C" fn taffy_ugui_set_style(
    ptr: *mut c_void,
    id: BootstrapNodeHandle,
    style: TaffyUGUIStyle,
) -> i32 {
    with_context_status(ptr, |handle| {
        with_registered_context_mut(handle, |ctx| {
            ctx.set_style(NodeHandle::from_raw(id), to_taffy_style(style))
        })
    })
}

/// Replaces the children of a node in a live context.
///
/// # Safety
///
/// `ptr` must be a live context token created by [`taffy_ugui_create_context`] and must
/// remain readable for the duration of the call. If `count` is non-zero, `child_ids` must
/// point to at least `count` contiguous, initialized `u64` values that remain readable for
/// the duration of the call. If `count` is zero, `child_ids` may be null.
#[no_mangle]
pub unsafe extern "C" fn taffy_ugui_set_children(
    ptr: *mut c_void,
    id: BootstrapNodeHandle,
    child_ids: *const BootstrapNodeHandle,
    count: usize,
) -> i32 {
    if count > 0 && child_ids.is_null() {
        return ERR_NULL;
    }
    let child_ids = if count == 0 {
        &[][..]
    } else {
        unsafe { std::slice::from_raw_parts(child_ids, count) }
    };
    let child_handles: Vec<NodeHandle> = child_ids
        .iter()
        .copied()
        .map(NodeHandle::from_raw)
        .collect();

    with_context_status(ptr, |handle| {
        with_registered_context_mut(handle, |ctx| {
            ctx.set_children(NodeHandle::from_raw(id), &child_handles)
        })
    })
}

/// Marks a node dirty in a live context.
///
/// # Safety
///
/// `ptr` must be a live context token created by [`taffy_ugui_create_context`] and must
/// remain readable for the duration of the call.
#[no_mangle]
pub unsafe extern "C" fn taffy_ugui_mark_dirty(ptr: *mut c_void, id: BootstrapNodeHandle) -> i32 {
    with_context_status(ptr, |handle| {
        with_registered_context_mut(handle, |ctx| ctx.mark_dirty(NodeHandle::from_raw(id)))
    })
}

/// Computes layout for a root node in a live context.
///
/// # Safety
///
/// `ptr` must be a live context token created by [`taffy_ugui_create_context`] and must
/// remain readable for the duration of the call.
#[no_mangle]
pub unsafe extern "C" fn taffy_ugui_compute_layout(
    ptr: *mut c_void,
    root_id: BootstrapNodeHandle,
    width: f32,
    height: f32,
) -> i32 {
    with_context_status(ptr, |handle| {
        with_registered_context_mut(handle, |ctx| {
            ctx.compute_layout(NodeHandle::from_raw(root_id), width, height)
        })
    })
}

/// Reads the computed layout of a node from a live context.
///
/// # Safety
///
/// `ptr` must be a live context token created by [`taffy_ugui_create_context`] and must
/// remain readable for the duration of the call. `out_layout` must be null or point to
/// writable memory for one [`TaffyUGUILayout`]; when non-null, this function writes the
/// node's layout to it on success.
#[no_mangle]
pub unsafe extern "C" fn taffy_ugui_get_layout(
    ptr: *mut c_void,
    id: BootstrapNodeHandle,
    out_layout: *mut TaffyUGUILayout,
) -> i32 {
    let Some(handle) = (unsafe { context_handle(ptr) }) else {
        return ERR_NULL;
    };
    let Some(out_layout) = (unsafe { out_layout.as_mut() }) else {
        return ERR_NULL;
    };

    match with_registered_context_mut(handle, |ctx| {
        ctx.layout_rect(NodeHandle::from_raw(id))
    }) {
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

/// Resolves a bootstrap token pointer to its generation-safe internal context handle.
///
/// # Safety
///
/// `ptr` must be null or a live bootstrap token pointer returned by
/// [`taffy_ugui_create_context`], and the token must not be concurrently destroyed.
unsafe fn context_handle(ptr: *mut c_void) -> Option<ContextHandle> {
    unsafe { BootstrapContextToken::from_opaque_ptr(ptr) }.map(|token| token.handle)
}

fn with_context_status(
    ptr: *mut c_void,
    operation: impl FnOnce(ContextHandle) -> Result<(), crate::error::NativeError>,
) -> i32 {
    let Some(handle) = (unsafe { context_handle(ptr) }) else {
        return ERR_NULL;
    };
    operation(handle)
        .map(|_| OK)
        .unwrap_or_else(|error| error.status_code())
}
