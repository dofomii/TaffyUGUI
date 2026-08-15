//! Native context and persistent Taffy tree ownership.

use std::collections::HashMap;
use std::ffi::c_void;

use taffy::prelude::*;

use crate::error::NativeError;
use crate::handles::{BootstrapNodeHandle, FIRST_BOOTSTRAP_NODE_HANDLE};

pub(crate) struct Context {
    tree: TaffyTree<()>,
    nodes: HashMap<BootstrapNodeHandle, NodeId>,
    next_id: BootstrapNodeHandle,
}

impl Context {
    pub(crate) fn new() -> Self {
        Self {
            tree: TaffyTree::new(),
            nodes: HashMap::new(),
            next_id: FIRST_BOOTSTRAP_NODE_HANDLE,
        }
    }

    pub(crate) fn into_opaque_ptr(self) -> *mut c_void {
        Box::into_raw(Box::new(self)) as *mut c_void
    }

    /// Converts the bootstrap ABI context pointer back into its Rust context.
    ///
    /// # Safety
    ///
    /// `ptr` must be null or a live pointer created by [`Context::into_opaque_ptr`].
    /// The caller must guarantee exclusive access for the returned mutable reference.
    pub(crate) unsafe fn from_opaque_ptr<'a>(ptr: *mut c_void) -> Option<&'a mut Self> {
        unsafe { (ptr as *mut Self).as_mut() }
    }

    /// Destroys a bootstrap ABI context pointer.
    ///
    /// # Safety
    ///
    /// `ptr` must be null or a live pointer created by [`Context::into_opaque_ptr`] that
    /// has not previously been destroyed. It must not be used after this call.
    pub(crate) unsafe fn destroy_opaque_ptr(ptr: *mut c_void) {
        if !ptr.is_null() {
            unsafe {
                drop(Box::from_raw(ptr as *mut Self));
            }
        }
    }

    pub(crate) fn create_node(&mut self, style: Style) -> Result<BootstrapNodeHandle, NativeError> {
        let node = self.tree.new_leaf(style).map_err(|_| NativeError::Engine)?;
        let id = self.next_id;
        self.next_id += 1;
        self.nodes.insert(id, node);
        Ok(id)
    }

    pub(crate) fn remove_node(&mut self, id: BootstrapNodeHandle) -> Result<(), NativeError> {
        let node = self.nodes.remove(&id).ok_or(NativeError::NodeNotFound)?;
        self.tree
            .remove(node)
            .map(|_| ())
            .map_err(|_| NativeError::Engine)
    }

    pub(crate) fn set_style(
        &mut self,
        id: BootstrapNodeHandle,
        style: Style,
    ) -> Result<(), NativeError> {
        let node = *self.nodes.get(&id).ok_or(NativeError::NodeNotFound)?;
        self.tree
            .set_style(node, style)
            .map(|_| ())
            .map_err(|_| NativeError::Engine)
    }

    pub(crate) fn set_children(
        &mut self,
        id: BootstrapNodeHandle,
        child_ids: &[BootstrapNodeHandle],
    ) -> Result<(), NativeError> {
        let node = *self.nodes.get(&id).ok_or(NativeError::NodeNotFound)?;
        let mut children = Vec::with_capacity(child_ids.len());
        for child_id in child_ids {
            let child = *self
                .nodes
                .get(child_id)
                .ok_or(NativeError::NodeNotFound)?;
            children.push(child);
        }
        self.tree
            .set_children(node, &children)
            .map(|_| ())
            .map_err(|_| NativeError::Engine)
    }

    pub(crate) fn mark_dirty(&mut self, id: BootstrapNodeHandle) -> Result<(), NativeError> {
        let node = *self.nodes.get(&id).ok_or(NativeError::NodeNotFound)?;
        self.tree
            .mark_dirty(node)
            .map(|_| ())
            .map_err(|_| NativeError::Engine)
    }

    pub(crate) fn compute_layout(
        &mut self,
        root_id: BootstrapNodeHandle,
        width: f32,
        height: f32,
    ) -> Result<(), NativeError> {
        let root = *self
            .nodes
            .get(&root_id)
            .ok_or(NativeError::NodeNotFound)?;
        let available = Size {
            width: available_space(width),
            height: available_space(height),
        };
        self.tree
            .compute_layout(root, available)
            .map(|_| ())
            .map_err(|_| NativeError::Engine)
    }

    pub(crate) fn layout_rect(
        &self,
        id: BootstrapNodeHandle,
    ) -> Result<(f32, f32, f32, f32), NativeError> {
        let node = *self.nodes.get(&id).ok_or(NativeError::NodeNotFound)?;
        let layout = self.tree.layout(node).map_err(|_| NativeError::Engine)?;
        Ok((
            layout.location.x,
            layout.location.y,
            layout.size.width,
            layout.size.height,
        ))
    }
}

fn available_space(value: f32) -> AvailableSpace {
    if value.is_finite() {
        AvailableSpace::Definite(value.max(0.0))
    } else {
        AvailableSpace::MaxContent
    }
}
