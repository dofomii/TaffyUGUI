//! Native context and persistent Taffy tree ownership.

use std::collections::HashMap;
use std::sync::{Mutex, MutexGuard, OnceLock};

use taffy::prelude::*;

use crate::error::NativeError;
use crate::handles::{BootstrapNodeHandle, ContextHandle, FIRST_BOOTSTRAP_NODE_HANDLE};

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
            let child = *self.nodes.get(child_id).ok_or(NativeError::NodeNotFound)?;
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
        let root = *self.nodes.get(&root_id).ok_or(NativeError::NodeNotFound)?;
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

struct ContextSlot {
    generation: u32,
    context: Option<Context>,
}

#[derive(Default)]
struct ContextRegistry {
    slots: Vec<ContextSlot>,
    free: Vec<u32>,
}

impl ContextRegistry {
    fn insert(&mut self, context: Context) -> Result<ContextHandle, NativeError> {
        if let Some(index) = self.free.pop() {
            let slot = self
                .slots
                .get_mut(index as usize)
                .ok_or(NativeError::ContextNotFound)?;
            debug_assert!(slot.context.is_none());
            slot.context = Some(context);
            return Ok(ContextHandle::from_parts(index, slot.generation));
        }

        let index = u32::try_from(self.slots.len()).map_err(|_| NativeError::Capacity)?;
        const INITIAL_GENERATION: u32 = 1;
        self.slots.push(ContextSlot {
            generation: INITIAL_GENERATION,
            context: Some(context),
        });
        Ok(ContextHandle::from_parts(index, INITIAL_GENERATION))
    }

    fn remove(&mut self, handle: ContextHandle) -> Result<(), NativeError> {
        let (index, generation) = handle.parts().ok_or(NativeError::ContextNotFound)?;
        let slot = self
            .slots
            .get_mut(index as usize)
            .ok_or(NativeError::ContextNotFound)?;
        if slot.generation != generation || slot.context.is_none() {
            return Err(NativeError::ContextNotFound);
        }

        slot.context = None;
        slot.generation = next_generation(slot.generation);
        self.free.push(index);
        Ok(())
    }

    fn get_mut(&mut self, handle: ContextHandle) -> Result<&mut Context, NativeError> {
        let (index, generation) = handle.parts().ok_or(NativeError::ContextNotFound)?;
        let slot = self
            .slots
            .get_mut(index as usize)
            .ok_or(NativeError::ContextNotFound)?;
        if slot.generation != generation {
            return Err(NativeError::ContextNotFound);
        }
        slot.context.as_mut().ok_or(NativeError::ContextNotFound)
    }
}

static CONTEXT_REGISTRY: OnceLock<Mutex<ContextRegistry>> = OnceLock::new();

fn registry() -> &'static Mutex<ContextRegistry> {
    CONTEXT_REGISTRY.get_or_init(|| Mutex::new(ContextRegistry::default()))
}

fn lock_registry() -> MutexGuard<'static, ContextRegistry> {
    registry()
        .lock()
        .unwrap_or_else(|poisoned| poisoned.into_inner())
}

pub(crate) fn create_registered_context() -> Result<ContextHandle, NativeError> {
    lock_registry().insert(Context::new())
}

pub(crate) fn destroy_registered_context(handle: ContextHandle) -> Result<(), NativeError> {
    lock_registry().remove(handle)
}

pub(crate) fn with_registered_context_mut<T>(
    handle: ContextHandle,
    operation: impl FnOnce(&mut Context) -> Result<T, NativeError>,
) -> Result<T, NativeError> {
    let mut registry = lock_registry();
    operation(registry.get_mut(handle)?)
}

fn next_generation(current: u32) -> u32 {
    let next = current.wrapping_add(1);
    if next == 0 { 1 } else { next }
}

fn available_space(value: f32) -> AvailableSpace {
    if value.is_finite() {
        AvailableSpace::Definite(value.max(0.0))
    } else {
        AvailableSpace::MaxContent
    }
}

#[cfg(test)]
mod tests {
    use super::{Context, ContextRegistry};
    use crate::error::NativeError;

    #[test]
    fn registry_insert_and_remove_context() {
        let mut registry = ContextRegistry::default();
        let handle = registry.insert(Context::new()).unwrap();
        assert!(registry.get_mut(handle).is_ok());
        assert_eq!(registry.remove(handle), Ok(()));
        assert!(matches!(
            registry.get_mut(handle),
            Err(NativeError::ContextNotFound)
        ));
    }

    #[test]
    fn reused_slot_changes_generation() {
        let mut registry = ContextRegistry::default();
        let first = registry.insert(Context::new()).unwrap();
        let (first_index, first_generation) = first.parts().unwrap();
        registry.remove(first).unwrap();

        let second = registry.insert(Context::new()).unwrap();
        let (second_index, second_generation) = second.parts().unwrap();

        assert_eq!(first_index, second_index);
        assert_ne!(first_generation, second_generation);
        assert!(matches!(
            registry.get_mut(first),
            Err(NativeError::ContextNotFound)
        ));
        assert!(registry.get_mut(second).is_ok());
    }

    #[test]
    fn contexts_are_isolated() {
        let mut registry = ContextRegistry::default();
        let first = registry.insert(Context::new()).unwrap();
        let second = registry.insert(Context::new()).unwrap();

        let first_ptr = registry.get_mut(first).unwrap() as *mut Context;
        let second_ptr = registry.get_mut(second).unwrap() as *mut Context;
        assert_ne!(first_ptr, second_ptr);
    }
}
