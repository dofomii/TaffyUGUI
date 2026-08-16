//! Native context, persistent layout arena, resource ownership, and Taffy dispatch.
//!
//! Taffy 0.13's compact style representation is intentionally non-`Send`. Context state stays
//! in a thread-local registry and uses process-wide generations so stale/cross-owner handles
//! cannot resolve to unrelated local slots.

use std::cell::RefCell;
use std::collections::HashMap;
use std::sync::atomic::{AtomicU32, Ordering};
use std::sync::{Mutex, OnceLock};
use std::thread::ThreadId;

use taffy::prelude::*;
use taffy::tree::{DetailedLayoutInfo, LayoutInput, LayoutOutput, RunMode};
use taffy::DetailedGridInfo;
use taffy::{
    compute_block_layout, compute_cached_layout, compute_flexbox_layout, compute_grid_layout,
    compute_hidden_layout, compute_leaf_layout, compute_root_layout, round_layout, BlockContext,
    Cache, CacheTree, LayoutBlockContainer, LayoutFlexboxContainer, LayoutGridContainer,
    LayoutPartialTree, RoundTree, TraversePartialTree, TraverseTree,
};

use crate::calc::{CalcExpr, CalcRegistry};
use crate::error::NativeError;
use crate::grid::GridTemplateResource;
use crate::handles::{ContextHandle, NodeHandle, ResourceHandle};
use crate::measurement::MeasurementRecord;

#[derive(Debug, Clone, Copy, PartialEq)]
struct ComputeKey {
    root: NodeId,
    mutation_generation: u64,
    available: Size<AvailableSpace>,
}

#[derive(Debug, Clone, Copy, PartialEq)]
pub(crate) struct LayoutResult {
    pub node: NodeHandle,
    pub order: u32,
    pub x: f32,
    pub y: f32,
    pub width: f32,
    pub height: f32,
    pub content_width: f32,
    pub content_height: f32,
    pub scroll_width: f32,
    pub scroll_height: f32,
}

#[derive(Debug, Clone)]
struct NodeState {
    style: Style,
    measurement: Option<MeasurementRecord>,
    cache: Cache,
    unrounded_layout: Layout,
    final_layout: Layout,
    detailed_layout_info: DetailedLayoutInfo,
    parent: Option<NodeId>,
    children: Vec<NodeId>,
}

impl NodeState {
    fn new(style: Style) -> Self {
        Self {
            style,
            measurement: None,
            cache: Cache::new(),
            unrounded_layout: Layout::new(),
            final_layout: Layout::new(),
            detailed_layout_info: DetailedLayoutInfo::None,
            parent: None,
            children: Vec::new(),
        }
    }
}

#[derive(Default)]
struct NativeTree {
    nodes: Vec<Option<NodeState>>,
    free: Vec<usize>,
    calc: CalcRegistry,
}

impl NativeTree {
    fn add_node(&mut self, style: Style) -> Result<NodeId, NativeError> {
        if let Some(index) = self.free.pop() {
            let slot = self.nodes.get_mut(index).ok_or(NativeError::NodeNotFound)?;
            debug_assert!(slot.is_none());
            *slot = Some(NodeState::new(style));
            return Ok(NodeId::from(index));
        }
        let index = self.nodes.len();
        self.nodes.push(Some(NodeState::new(style)));
        Ok(NodeId::from(index))
    }

    fn remove_node(&mut self, node: NodeId) -> Result<(), NativeError> {
        let index = usize::from(node);
        let state = self
            .nodes
            .get(index)
            .and_then(Option::as_ref)
            .ok_or(NativeError::NodeNotFound)?;
        let parent = state.parent;
        let children = state.children.clone();

        if let Some(parent) = parent {
            if let Ok(parent_state) = self.node_mut(parent) {
                parent_state.children.retain(|child| *child != node);
            }
            self.mark_dirty(parent)?;
        }
        for child in children {
            self.node_mut(child)?.parent = None;
            self.mark_dirty(child)?;
        }

        self.nodes[index] = None;
        self.free.push(index);
        Ok(())
    }

    fn clear(&mut self) {
        self.nodes.clear();
        self.free.clear();
        self.calc.clear();
    }

    fn node(&self, node: NodeId) -> Result<&NodeState, NativeError> {
        self.nodes
            .get(usize::from(node))
            .and_then(Option::as_ref)
            .ok_or(NativeError::NodeNotFound)
    }

    fn node_mut(&mut self, node: NodeId) -> Result<&mut NodeState, NativeError> {
        self.nodes
            .get_mut(usize::from(node))
            .and_then(Option::as_mut)
            .ok_or(NativeError::NodeNotFound)
    }

    fn set_style(&mut self, node: NodeId, mut style: Style) -> Result<(), NativeError> {
        if self
            .node(node)?
            .measurement
            .as_ref()
            .is_some_and(|record| record.is_replaced)
        {
            style.item_is_replaced = true;
        }
        self.node_mut(node)?.style = style;
        self.mark_dirty(node)
    }

    fn set_measurement(
        &mut self,
        node: NodeId,
        measurement: Option<MeasurementRecord>,
    ) -> Result<(), NativeError> {
        if let Some(record) = &measurement {
            record.validate()?;
        }
        let state = self.node_mut(node)?;
        state.style.item_is_replaced = measurement
            .as_ref()
            .is_some_and(|record| record.is_replaced);
        state.measurement = measurement;
        self.mark_dirty(node)
    }

    fn set_children(&mut self, parent: NodeId, children: &[NodeId]) -> Result<(), NativeError> {
        self.node(parent)?;
        for (index, child) in children.iter().enumerate() {
            self.node(*child)?;
            if *child == parent
                || children[..index].contains(child)
                || self.would_create_cycle(parent, *child)?
            {
                return Err(NativeError::InvalidValue);
            }
        }

        let old_children = self.node(parent)?.children.clone();
        for child in &old_children {
            if self.node(*child)?.parent == Some(parent) {
                self.node_mut(*child)?.parent = None;
            }
        }

        for child in children {
            if let Some(old_parent) = self.node(*child)?.parent {
                if old_parent != parent {
                    self.node_mut(old_parent)?
                        .children
                        .retain(|candidate| candidate != child);
                    self.mark_dirty(old_parent)?;
                }
            }
            self.node_mut(*child)?.parent = Some(parent);
        }
        self.node_mut(parent)?.children = children.to_vec();
        self.mark_dirty(parent)
    }

    fn would_create_cycle(&self, parent: NodeId, child: NodeId) -> Result<bool, NativeError> {
        let mut cursor = Some(parent);
        while let Some(node) = cursor {
            if node == child {
                return Ok(true);
            }
            cursor = self.node(node)?.parent;
        }
        Ok(false)
    }

    fn mark_dirty(&mut self, node: NodeId) -> Result<(), NativeError> {
        let mut cursor = Some(node);
        while let Some(current) = cursor {
            let state = self.node_mut(current)?;
            state.cache.clear();
            state.detailed_layout_info = DetailedLayoutInfo::None;
            cursor = state.parent;
        }
        Ok(())
    }

    fn is_dirty(&self, node: NodeId) -> Result<bool, NativeError> {
        Ok(self.node(node)?.cache.is_empty())
    }

    fn mark_all_dirty(&mut self) {
        for state in self.nodes.iter_mut().flatten() {
            state.cache.clear();
            state.detailed_layout_info = DetailedLayoutInfo::None;
        }
    }

    fn compute(&mut self, root: NodeId, available: Size<AvailableSpace>) {
        compute_root_layout(self, root, available);
        round_layout(self, root);
    }

    fn final_layout(&self, node: NodeId) -> Result<Layout, NativeError> {
        Ok(self.node(node)?.final_layout)
    }

    fn detailed_layout_info(&self, node: NodeId) -> Result<&DetailedLayoutInfo, NativeError> {
        Ok(&self.node(node)?.detailed_layout_info)
    }

    fn compute_child_layout_inner(
        &mut self,
        node_id: NodeId,
        inputs: LayoutInput,
        block_ctx: Option<&mut BlockContext<'_>>,
    ) -> LayoutOutput {
        if inputs.run_mode == RunMode::PerformHiddenLayout {
            return compute_hidden_layout(self, node_id);
        }

        compute_cached_layout(self, node_id, inputs, |tree, node_id, inputs| {
            let state = tree.node(node_id).expect("Taffy only requests live nodes");
            let display = state.style.display;
            let has_children = !state.children.is_empty();

            match (display, has_children) {
                (Display::None, _) => compute_hidden_layout(tree, node_id),
                (Display::Block, true) => compute_block_layout(tree, node_id, inputs, block_ctx),
                (Display::FlowRoot, true) => compute_block_layout(tree, node_id, inputs, None),
                (Display::Flex, true) => compute_flexbox_layout(tree, node_id, inputs),
                (Display::Grid, true) => compute_grid_layout(tree, node_id, inputs),
                (_, false) => {
                    let state = tree.node(node_id).expect("Taffy only requests live nodes");
                    let style = state.style.clone();
                    let measurement = state.measurement.clone();
                    compute_leaf_layout(
                        inputs,
                        &style,
                        |value, basis| tree.calc.resolve_ptr(value, basis),
                        |known_dimensions, available_space| {
                            measurement
                                .as_ref()
                                .map(|record| record.measure(known_dimensions, available_space))
                                .unwrap_or(Size::ZERO)
                        },
                    )
                }
            }
        })
    }
}

struct ChildIter<'a>(std::slice::Iter<'a, NodeId>);
impl Iterator for ChildIter<'_> {
    type Item = NodeId;
    fn next(&mut self) -> Option<Self::Item> {
        self.0.next().copied()
    }
}

impl TraversePartialTree for NativeTree {
    type ChildIter<'a> = ChildIter<'a>;

    fn child_ids(&self, parent_node_id: NodeId) -> Self::ChildIter<'_> {
        ChildIter(
            self.node(parent_node_id)
                .expect("live parent")
                .children
                .iter(),
        )
    }

    fn child_count(&self, parent_node_id: NodeId) -> usize {
        self.node(parent_node_id)
            .expect("live parent")
            .children
            .len()
    }

    fn get_child_id(&self, parent_node_id: NodeId, child_index: usize) -> NodeId {
        self.node(parent_node_id).expect("live parent").children[child_index]
    }
}
impl TraverseTree for NativeTree {}

impl LayoutPartialTree for NativeTree {
    type CoreContainerStyle<'a>
        = &'a Style
    where
        Self: 'a;
    type CustomIdent = String;

    fn get_core_container_style(&self, node_id: NodeId) -> Self::CoreContainerStyle<'_> {
        &self.node(node_id).expect("live node").style
    }

    fn resolve_calc_value(&self, value: *const (), basis: f32) -> f32 {
        self.calc.resolve_ptr(value, basis)
    }

    fn set_unrounded_layout(&mut self, node_id: NodeId, layout: &Layout) {
        self.node_mut(node_id).expect("live node").unrounded_layout = *layout;
    }

    fn compute_child_layout(&mut self, node_id: NodeId, inputs: LayoutInput) -> LayoutOutput {
        self.compute_child_layout_inner(node_id, inputs, None)
    }
}

impl CacheTree for NativeTree {
    fn cache_get(&self, node_id: NodeId, input: &LayoutInput) -> Option<LayoutOutput> {
        self.node(node_id).expect("live node").cache.get(input)
    }

    fn cache_store(&mut self, node_id: NodeId, input: &LayoutInput, output: LayoutOutput) {
        self.node_mut(node_id)
            .expect("live node")
            .cache
            .store(input, output);
    }

    fn cache_clear(&mut self, node_id: NodeId) {
        self.node_mut(node_id).expect("live node").cache.clear();
    }
}

impl LayoutFlexboxContainer for NativeTree {
    type FlexboxContainerStyle<'a>
        = &'a Style
    where
        Self: 'a;
    type FlexboxItemStyle<'a>
        = &'a Style
    where
        Self: 'a;

    fn get_flexbox_container_style(&self, node_id: NodeId) -> Self::FlexboxContainerStyle<'_> {
        &self.node(node_id).expect("live node").style
    }

    fn get_flexbox_child_style(&self, child_node_id: NodeId) -> Self::FlexboxItemStyle<'_> {
        &self.node(child_node_id).expect("live child").style
    }
}

impl LayoutGridContainer for NativeTree {
    type GridContainerStyle<'a>
        = &'a Style
    where
        Self: 'a;
    type GridItemStyle<'a>
        = &'a Style
    where
        Self: 'a;

    fn get_grid_container_style(&self, node_id: NodeId) -> Self::GridContainerStyle<'_> {
        &self.node(node_id).expect("live node").style
    }

    fn get_grid_child_style(&self, child_node_id: NodeId) -> Self::GridItemStyle<'_> {
        &self.node(child_node_id).expect("live child").style
    }

    fn set_detailed_grid_info(&mut self, node_id: NodeId, detailed_grid_info: DetailedGridInfo) {
        self.node_mut(node_id)
            .expect("live node")
            .detailed_layout_info = DetailedLayoutInfo::Grid(Box::new(detailed_grid_info));
    }
}

impl LayoutBlockContainer for NativeTree {
    type BlockContainerStyle<'a>
        = &'a Style
    where
        Self: 'a;
    type BlockItemStyle<'a>
        = &'a Style
    where
        Self: 'a;

    fn get_block_container_style(&self, node_id: NodeId) -> Self::BlockContainerStyle<'_> {
        &self.node(node_id).expect("live node").style
    }

    fn get_block_child_style(&self, child_node_id: NodeId) -> Self::BlockItemStyle<'_> {
        &self.node(child_node_id).expect("live child").style
    }

    fn compute_block_child_layout(
        &mut self,
        node_id: NodeId,
        inputs: LayoutInput,
        block_ctx: Option<&mut BlockContext<'_>>,
    ) -> LayoutOutput {
        self.compute_child_layout_inner(node_id, inputs, block_ctx)
    }
}

impl RoundTree for NativeTree {
    fn get_unrounded_layout(&self, node_id: NodeId) -> Layout {
        self.node(node_id).expect("live node").unrounded_layout
    }

    fn set_final_layout(&mut self, node_id: NodeId, layout: &Layout) {
        self.node_mut(node_id).expect("live node").final_layout = *layout;
    }
}

pub(crate) struct Context {
    tree: NativeTree,
    nodes: NodeRegistry,
    mutation_generation: u64,
    last_compute: Option<ComputeKey>,
    compute_count: u64,
}

impl Context {
    pub(crate) fn new() -> Self {
        Self {
            tree: NativeTree::default(),
            nodes: NodeRegistry::default(),
            mutation_generation: 1,
            last_compute: None,
            compute_count: 0,
        }
    }

    pub(crate) fn create_node(&mut self, style: Style) -> Result<NodeHandle, NativeError> {
        let node = self.tree.add_node(style)?;
        match self.nodes.insert(node) {
            Ok(handle) => {
                self.bump_generation();
                Ok(handle)
            }
            Err(error) => {
                let _ = self.tree.remove_node(node);
                Err(error)
            }
        }
    }

    pub(crate) fn remove_node(&mut self, handle: NodeHandle) -> Result<(), NativeError> {
        let node = self.nodes.resolve(handle)?;
        self.tree.remove_node(node)?;
        self.nodes.remove(handle)?;
        self.bump_generation();
        Ok(())
    }

    pub(crate) fn clear(&mut self) {
        self.tree.clear();
        self.nodes.clear();
        self.bump_generation();
    }

    pub(crate) fn set_style(
        &mut self,
        handle: NodeHandle,
        style: Style,
    ) -> Result<(), NativeError> {
        let node = self.nodes.resolve(handle)?;
        self.tree.set_style(node, style)?;
        self.bump_generation();
        Ok(())
    }

    pub(crate) fn apply_grid_template(
        &mut self,
        handle: NodeHandle,
        resource: &GridTemplateResource,
    ) -> Result<(), NativeError> {
        let node = self.nodes.resolve(handle)?;
        let mut style = self.tree.node(node)?.style.clone();
        resource.apply_to(&mut style);
        self.tree.set_style(node, style)?;
        self.bump_generation();
        Ok(())
    }

    pub(crate) fn set_styles_bulk(
        &mut self,
        updates: &[(NodeHandle, Style)],
    ) -> Result<(), NativeError> {
        let resolved = updates
            .iter()
            .map(|(handle, style)| Ok((self.nodes.resolve(*handle)?, style.clone())))
            .collect::<Result<Vec<_>, NativeError>>()?;
        for (node, style) in resolved {
            self.tree.set_style(node, style)?;
        }
        if !updates.is_empty() {
            self.bump_generation();
        }
        Ok(())
    }

    pub(crate) fn set_measurement(
        &mut self,
        handle: NodeHandle,
        measurement: Option<MeasurementRecord>,
    ) -> Result<(), NativeError> {
        let node = self.nodes.resolve(handle)?;
        self.tree.set_measurement(node, measurement)?;
        self.bump_generation();
        Ok(())
    }

    pub(crate) fn set_measurements_bulk(
        &mut self,
        updates: &[(NodeHandle, Option<MeasurementRecord>)],
    ) -> Result<(), NativeError> {
        let resolved = updates
            .iter()
            .map(|(handle, record)| {
                if let Some(record) = record {
                    record.validate()?;
                }
                Ok((self.nodes.resolve(*handle)?, record.clone()))
            })
            .collect::<Result<Vec<_>, NativeError>>()?;
        for (node, record) in resolved {
            self.tree.set_measurement(node, record)?;
        }
        if !updates.is_empty() {
            self.bump_generation();
        }
        Ok(())
    }

    pub(crate) fn set_children(
        &mut self,
        handle: NodeHandle,
        child_handles: &[NodeHandle],
    ) -> Result<(), NativeError> {
        let node = self.nodes.resolve(handle)?;
        let children = child_handles
            .iter()
            .map(|handle| self.nodes.resolve(*handle))
            .collect::<Result<Vec<_>, _>>()?;
        self.tree.set_children(node, &children)?;
        self.bump_generation();
        Ok(())
    }

    pub(crate) fn set_children_bulk(
        &mut self,
        updates: &[(NodeHandle, Vec<NodeHandle>)],
    ) -> Result<(), NativeError> {
        let resolved = updates
            .iter()
            .map(|(parent, children)| {
                Ok((
                    self.nodes.resolve(*parent)?,
                    children
                        .iter()
                        .map(|child| self.nodes.resolve(*child))
                        .collect::<Result<Vec<_>, _>>()?,
                ))
            })
            .collect::<Result<Vec<_>, NativeError>>()?;
        for (parent, children) in resolved {
            self.tree.set_children(parent, &children)?;
        }
        if !updates.is_empty() {
            self.bump_generation();
        }
        Ok(())
    }

    pub(crate) fn mark_dirty(&mut self, handle: NodeHandle) -> Result<(), NativeError> {
        let node = self.nodes.resolve(handle)?;
        self.tree.mark_dirty(node)?;
        self.bump_generation();
        Ok(())
    }

    pub(crate) fn is_dirty(&self, handle: NodeHandle) -> Result<bool, NativeError> {
        self.tree.is_dirty(self.nodes.resolve(handle)?)
    }

    pub(crate) fn create_calc(&mut self, expr: CalcExpr) -> Result<ResourceHandle, NativeError> {
        let handle = self.tree.calc.insert(expr)?;
        self.bump_generation();
        Ok(handle)
    }

    pub(crate) fn remove_calc(&mut self, handle: ResourceHandle) -> Result<(), NativeError> {
        self.tree.calc.remove(handle)?;
        self.tree.mark_all_dirty();
        self.bump_generation();
        Ok(())
    }

    pub(crate) fn calc_dimension(&self, handle: ResourceHandle) -> Result<Dimension, NativeError> {
        self.tree.calc.dimension(handle)
    }
    pub(crate) fn calc_length_percentage(
        &self,
        handle: ResourceHandle,
    ) -> Result<LengthPercentage, NativeError> {
        self.tree.calc.length_percentage(handle)
    }
    pub(crate) fn calc_length_percentage_auto(
        &self,
        handle: ResourceHandle,
    ) -> Result<LengthPercentageAuto, NativeError> {
        self.tree.calc.length_percentage_auto(handle)
    }
    pub(crate) fn calc_ptr(&self, handle: ResourceHandle) -> Result<*const (), NativeError> {
        self.tree.calc.token_ptr_for_taffy(handle)
    }

    pub(crate) fn compute_layout(
        &mut self,
        root_handle: NodeHandle,
        width: f32,
        height: f32,
    ) -> Result<(), NativeError> {
        let root = self.nodes.resolve(root_handle)?;
        let available = Size {
            width: available_space(width),
            height: available_space(height),
        };
        let key = ComputeKey {
            root,
            mutation_generation: self.mutation_generation,
            available,
        };
        if self.last_compute == Some(key) {
            return Ok(());
        }
        self.tree.compute(root, available);
        self.last_compute = Some(key);
        self.compute_count = self.compute_count.wrapping_add(1);
        Ok(())
    }

    pub(crate) fn layouts_bulk(
        &self,
        handles: &[NodeHandle],
    ) -> Result<Vec<LayoutResult>, NativeError> {
        handles
            .iter()
            .map(|handle| {
                let layout = self.tree.final_layout(self.nodes.resolve(*handle)?)?;
                Ok(LayoutResult {
                    node: *handle,
                    order: layout.order,
                    x: layout.location.x,
                    y: layout.location.y,
                    width: layout.size.width,
                    height: layout.size.height,
                    content_width: layout.content_size.width,
                    content_height: layout.content_size.height,
                    scroll_width: layout.scroll_width(),
                    scroll_height: layout.scroll_height(),
                })
            })
            .collect()
    }

    pub(crate) fn detailed_layout_info(
        &self,
        handle: NodeHandle,
    ) -> Result<&DetailedLayoutInfo, NativeError> {
        self.tree.detailed_layout_info(self.nodes.resolve(handle)?)
    }

    #[cfg(test)]
    fn compute_count(&self) -> u64 {
        self.compute_count
    }

    fn bump_generation(&mut self) {
        self.mutation_generation = self.mutation_generation.wrapping_add(1).max(1);
        self.last_compute = None;
    }
}

struct NodeSlot {
    generation: u32,
    node: Option<NodeId>,
}

#[derive(Default)]
struct NodeRegistry {
    slots: Vec<NodeSlot>,
    free: Vec<u32>,
}

impl NodeRegistry {
    fn insert(&mut self, node: NodeId) -> Result<NodeHandle, NativeError> {
        let generation = next_node_generation();
        if let Some(index) = self.free.pop() {
            let slot = self
                .slots
                .get_mut(index as usize)
                .ok_or(NativeError::NodeNotFound)?;
            debug_assert!(slot.node.is_none());
            slot.generation = generation;
            slot.node = Some(node);
            return Ok(NodeHandle::from_parts(index, generation));
        }
        let index = u32::try_from(self.slots.len()).map_err(|_| NativeError::Capacity)?;
        self.slots.push(NodeSlot {
            generation,
            node: Some(node),
        });
        Ok(NodeHandle::from_parts(index, generation))
    }

    fn resolve(&self, handle: NodeHandle) -> Result<NodeId, NativeError> {
        let (index, generation) = handle.parts().ok_or(NativeError::NodeNotFound)?;
        let slot = self
            .slots
            .get(index as usize)
            .ok_or(NativeError::NodeNotFound)?;
        if slot.generation != generation {
            return Err(NativeError::NodeNotFound);
        }
        slot.node.ok_or(NativeError::NodeNotFound)
    }

    fn remove(&mut self, handle: NodeHandle) -> Result<(), NativeError> {
        let (index, generation) = handle.parts().ok_or(NativeError::NodeNotFound)?;
        let slot = self
            .slots
            .get_mut(index as usize)
            .ok_or(NativeError::NodeNotFound)?;
        if slot.generation != generation || slot.node.is_none() {
            return Err(NativeError::NodeNotFound);
        }
        slot.node = None;
        self.free.push(index);
        Ok(())
    }

    fn clear(&mut self) {
        self.slots.clear();
        self.free.clear();
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
        let generation = next_context_generation();
        if let Some(index) = self.free.pop() {
            let slot = self
                .slots
                .get_mut(index as usize)
                .ok_or(NativeError::ContextNotFound)?;
            debug_assert!(slot.context.is_none());
            slot.generation = generation;
            slot.context = Some(context);
            return Ok(ContextHandle::from_parts(index, generation));
        }
        let index = u32::try_from(self.slots.len()).map_err(|_| NativeError::Capacity)?;
        self.slots.push(ContextSlot {
            generation,
            context: Some(context),
        });
        Ok(ContextHandle::from_parts(index, generation))
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

static NEXT_CONTEXT_GENERATION: AtomicU32 = AtomicU32::new(1);
static NEXT_NODE_GENERATION: AtomicU32 = AtomicU32::new(1);

thread_local! {
    static CONTEXT_REGISTRY: RefCell<ContextRegistry> = RefCell::new(ContextRegistry::default());
}

fn context_owners() -> &'static Mutex<HashMap<u64, ThreadId>> {
    static OWNERS: OnceLock<Mutex<HashMap<u64, ThreadId>>> = OnceLock::new();
    OWNERS.get_or_init(|| Mutex::new(HashMap::new()))
}

fn validate_context_owner(handle: ContextHandle) -> Result<(), NativeError> {
    let owners = context_owners()
        .lock()
        .map_err(|_| NativeError::RegistryBusy)?;
    match owners.get(&handle.raw()) {
        Some(owner) if *owner == std::thread::current().id() => Ok(()),
        Some(_) => Err(NativeError::WrongThread),
        None => Err(NativeError::ContextNotFound),
    }
}

pub(crate) fn create_registered_context() -> Result<ContextHandle, NativeError> {
    let handle = with_registry_mut(|registry| registry.insert(Context::new()))?;
    context_owners()
        .lock()
        .map_err(|_| NativeError::RegistryBusy)?
        .insert(handle.raw(), std::thread::current().id());
    Ok(handle)
}

pub(crate) fn destroy_registered_context(handle: ContextHandle) -> Result<(), NativeError> {
    validate_context_owner(handle)?;
    with_registry_mut(|registry| registry.remove(handle))?;
    context_owners()
        .lock()
        .map_err(|_| NativeError::RegistryBusy)?
        .remove(&handle.raw());
    Ok(())
}

pub(crate) fn with_registered_context_mut<T>(
    handle: ContextHandle,
    operation: impl FnOnce(&mut Context) -> Result<T, NativeError>,
) -> Result<T, NativeError> {
    validate_context_owner(handle)?;
    with_registry_mut(|registry| operation(registry.get_mut(handle)?))
}

fn with_registry_mut<T>(
    operation: impl FnOnce(&mut ContextRegistry) -> Result<T, NativeError>,
) -> Result<T, NativeError> {
    CONTEXT_REGISTRY.with(|registry| {
        let mut registry = registry
            .try_borrow_mut()
            .map_err(|_| NativeError::RegistryBusy)?;
        operation(&mut registry)
    })
}

fn next_context_generation() -> u32 {
    next_generation(&NEXT_CONTEXT_GENERATION)
}
fn next_node_generation() -> u32 {
    next_generation(&NEXT_NODE_GENERATION)
}
fn next_generation(counter: &AtomicU32) -> u32 {
    loop {
        let generation = counter.fetch_add(1, Ordering::Relaxed);
        if generation != 0 {
            return generation;
        }
    }
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
    use std::thread;

    use taffy::prelude::*;
    use taffy::style::{Clear, Float};
    use taffy::style_helpers::{auto, length, line, span};

    use super::{
        create_registered_context, destroy_registered_context, with_registered_context_mut,
        Context, ContextRegistry,
    };
    use crate::calc::CalcExpr;
    use crate::error::NativeError;
    use crate::grid::{fraction_track, template_areas, GridTemplateResource};
    use crate::measurement::MeasurementRecord;

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
    fn reused_context_slot_changes_generation() {
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
    }

    #[test]
    fn registered_context_does_not_resolve_on_another_thread() {
        let handle = create_registered_context().unwrap();
        let other_thread =
            thread::spawn(move || with_registered_context_mut(handle, |_| Ok(())).unwrap_err());
        assert_eq!(other_thread.join().unwrap(), NativeError::WrongThread);
        assert_eq!(destroy_registered_context(handle), Ok(()));
    }

    #[test]
    fn stale_and_cross_context_node_handles_are_rejected() {
        let mut first_context = Context::new();
        let mut second_context = Context::new();
        let first = first_context.create_node(Style::default()).unwrap();
        first_context.remove_node(first).unwrap();
        let replacement = first_context.create_node(Style::default()).unwrap();
        assert_ne!(first, replacement);
        assert!(matches!(
            first_context.set_style(first, Style::default()),
            Err(NativeError::NodeNotFound)
        ));
        assert!(matches!(
            second_context.set_style(replacement, Style::default()),
            Err(NativeError::NodeNotFound)
        ));
    }

    #[test]
    fn persistent_topology_and_dirty_state_work() {
        let mut context = Context::new();
        let root = context.create_node(Style::default()).unwrap();
        let child = context.create_node(Style::default()).unwrap();
        context.set_children(root, &[child]).unwrap();
        assert!(context.is_dirty(root).unwrap());
        context.compute_layout(root, 100.0, 100.0).unwrap();
        assert!(!context.is_dirty(root).unwrap());
        context.mark_dirty(child).unwrap();
        assert!(context.is_dirty(root).unwrap());
    }

    #[test]
    fn flex_and_content_size_layout_execute() {
        let mut context = Context::new();
        let root = context
            .create_node(Style {
                display: Display::Flex,
                size: Size {
                    width: length(100.0),
                    height: length(50.0),
                },
                gap: Size {
                    width: length(10.0),
                    height: length(0.0),
                },
                ..Default::default()
            })
            .unwrap();
        let a = context
            .create_node(Style {
                size: Size {
                    width: length(20.0),
                    height: length(10.0),
                },
                ..Default::default()
            })
            .unwrap();
        let b = context
            .create_node(Style {
                size: Size {
                    width: length(30.0),
                    height: length(10.0),
                },
                ..Default::default()
            })
            .unwrap();
        context.set_children(root, &[a, b]).unwrap();
        context.compute_layout(root, 100.0, 50.0).unwrap();
        let results = context.layouts_bulk(&[root, a, b]).unwrap();
        assert_eq!(results[1].x, 0.0);
        assert_eq!(results[2].x, 30.0);
        assert!(results[0].content_width >= 60.0);
    }

    #[test]
    fn block_flow_root_float_and_clear_execute() {
        let mut context = Context::new();
        let root = context
            .create_node(Style {
                display: Display::FlowRoot,
                size: Size {
                    width: length(200.0),
                    height: auto(),
                },
                ..Default::default()
            })
            .unwrap();
        let floated = context
            .create_node(Style {
                display: Display::Block,
                float: Float::Left,
                size: Size {
                    width: length(60.0),
                    height: length(40.0),
                },
                ..Default::default()
            })
            .unwrap();
        let cleared = context
            .create_node(Style {
                display: Display::Block,
                clear: Clear::Both,
                size: Size {
                    width: length(100.0),
                    height: length(20.0),
                },
                ..Default::default()
            })
            .unwrap();
        context.set_children(root, &[floated, cleared]).unwrap();
        context.compute_layout(root, 200.0, f32::INFINITY).unwrap();
        let layouts = context.layouts_bulk(&[floated, cleared]).unwrap();
        assert!(layouts[1].y >= layouts[0].height);
    }

    #[test]
    fn calc_resources_resolve_during_layout() {
        let mut context = Context::new();
        let px = context.create_calc(CalcExpr::Length(10.0)).unwrap();
        let pct = context.create_calc(CalcExpr::Percent(0.5)).unwrap();
        let sum = context.create_calc(CalcExpr::Add(px, pct)).unwrap();
        let width = context.calc_dimension(sum).unwrap();
        let node = context
            .create_node(Style {
                size: Size {
                    width,
                    height: length(10.0),
                },
                ..Default::default()
            })
            .unwrap();
        context.compute_layout(node, 200.0, 100.0).unwrap();
        let layout = context.layouts_bulk(&[node]).unwrap().remove(0);
        assert_eq!(layout.width, 110.0);
    }

    #[test]
    fn cached_measurements_are_used_without_managed_callbacks() {
        let mut context = Context::new();
        let node = context.create_node(Style::default()).unwrap();
        context
            .set_measurement(
                node,
                Some(MeasurementRecord {
                    min_content: Size {
                        width: 20.0,
                        height: 10.0,
                    },
                    max_content: Size {
                        width: 80.0,
                        height: 10.0,
                    },
                    preferred: Size {
                        width: 50.0,
                        height: 10.0,
                    },
                    ..Default::default()
                }),
            )
            .unwrap();
        context
            .compute_layout(node, f32::INFINITY, f32::INFINITY)
            .unwrap();
        assert_eq!(context.layouts_bulk(&[node]).unwrap()[0].width, 80.0);
    }

    #[test]
    fn grid_tracks_placement_named_areas_and_diagnostics_execute() {
        let mut context = Context::new();
        let mut root_style = Style {
            display: Display::Grid,
            size: Size {
                width: length(200.0),
                height: length(100.0),
            },
            grid_auto_flow: GridAutoFlow::Row,
            align_content: Some(AlignContent::STRETCH),
            justify_content: Some(JustifyContent::STRETCH),
            align_items: Some(AlignItems::STRETCH),
            justify_items: Some(JustifyItems::STRETCH),
            ..Default::default()
        };
        GridTemplateResource {
            columns: vec![fraction_track(1.0), fraction_track(1.0)],
            rows: vec![auto()],
            column_line_names: vec![
                vec!["left".into()],
                vec!["middle".into()],
                vec!["right".into()],
            ],
            row_line_names: vec![vec!["top".into()], vec!["bottom".into()]],
            areas: Some(template_areas(1, 2, [("main".into(), 1, 2, 1, 3)])),
            ..Default::default()
        }
        .apply_to(&mut root_style);
        let root = context.create_node(root_style).unwrap();
        let child = context
            .create_node(Style {
                grid_column: Line {
                    start: line(1),
                    end: span(2),
                },
                grid_row: Line {
                    start: line(1),
                    end: line(2),
                },
                ..Default::default()
            })
            .unwrap();
        context.set_children(root, &[child]).unwrap();
        context.compute_layout(root, 200.0, 100.0).unwrap();
        assert!(matches!(
            context.detailed_layout_info(root).unwrap(),
            taffy::tree::DetailedLayoutInfo::Grid(_)
        ));
    }

    #[test]
    fn bulk_updates_and_one_compute_per_generation() {
        let mut context = Context::new();
        let root = context.create_node(Style::default()).unwrap();
        let child = context.create_node(Style::default()).unwrap();
        context.set_children_bulk(&[(root, vec![child])]).unwrap();
        context
            .set_styles_bulk(&[(
                child,
                Style {
                    size: Size {
                        width: length(10.0),
                        height: length(10.0),
                    },
                    ..Default::default()
                },
            )])
            .unwrap();
        context.set_measurements_bulk(&[(child, None)]).unwrap();
        context.compute_layout(root, 100.0, 100.0).unwrap();
        context.compute_layout(root, 100.0, 100.0).unwrap();
        assert_eq!(context.compute_count(), 1);
        context.mark_dirty(child).unwrap();
        context.compute_layout(root, 100.0, 100.0).unwrap();
        assert_eq!(context.compute_count(), 2);
    }

    #[test]
    fn display_none_hides_descendants() {
        let mut context = Context::new();
        let root = context
            .create_node(Style {
                display: Display::None,
                ..Default::default()
            })
            .unwrap();
        let child = context
            .create_node(Style {
                size: Size {
                    width: length(50.0),
                    height: length(20.0),
                },
                ..Default::default()
            })
            .unwrap();
        context.set_children(root, &[child]).unwrap();
        context.compute_layout(root, 100.0, 100.0).unwrap();
        let layout = context.layouts_bulk(&[child]).unwrap().remove(0);
        assert_eq!((layout.width, layout.height), (0.0, 0.0));
    }
}
