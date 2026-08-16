//! Typed Calc resources used by Taffy compact-length values.
//!
//! Taffy 0.13 treats Calc payloads as opaque pointers and asks the layout tree to resolve
//! them against a percentage basis. We keep the public/native identity generation-safe while
//! retaining stable aligned tokens for Taffy's internal pointer-shaped representation.

use std::sync::atomic::{AtomicU32, Ordering};

use taffy::style::{Dimension, LengthPercentage, LengthPercentageAuto};

use crate::error::NativeError;
use crate::handles::ResourceHandle;

#[derive(Debug, Clone, PartialEq)]
pub(crate) enum CalcExpr {
    Length(f32),
    Percent(f32),
    Add(ResourceHandle, ResourceHandle),
    Sub(ResourceHandle, ResourceHandle),
    Scale(ResourceHandle, f32),
    Min(Vec<ResourceHandle>),
    Max(Vec<ResourceHandle>),
    Clamp {
        min: ResourceHandle,
        preferred: ResourceHandle,
        max: ResourceHandle,
    },
}

#[repr(C, align(8))]
#[derive(Debug)]
struct CalcToken {
    handle: ResourceHandle,
}

struct CalcSlot {
    generation: u32,
    expr: Option<CalcExpr>,
    token: Option<Box<CalcToken>>,
}

#[derive(Default)]
pub(crate) struct CalcRegistry {
    slots: Vec<CalcSlot>,
    free: Vec<u32>,
    // Box indirection is intentional: retired Calc token addresses must remain stable for any
    // Taffy CompactLength values that still carry their opaque pointer.
    #[allow(clippy::vec_box)]
    retired_tokens: Vec<Box<CalcToken>>,
}

impl CalcRegistry {
    pub(crate) fn clear(&mut self) {
        for slot in &mut self.slots {
            if let Some(token) = slot.token.take() {
                self.retired_tokens.push(token);
            }
            slot.expr = None;
        }
        self.slots.clear();
        self.free.clear();
        self.retired_tokens.clear();
    }

    pub(crate) fn insert(&mut self, expr: CalcExpr) -> Result<ResourceHandle, NativeError> {
        validate_expr(&expr)?;
        self.validate_dependencies(&expr)?;
        let generation = next_resource_generation();
        if let Some(index) = self.free.pop() {
            let slot = self.slots.get_mut(index as usize).ok_or(NativeError::ResourceNotFound)?;
            let handle = ResourceHandle::from_parts(index, generation);
            slot.generation = generation;
            slot.expr = Some(expr);
            slot.token = Some(Box::new(CalcToken { handle }));
            return Ok(handle);
        }

        let index = u32::try_from(self.slots.len()).map_err(|_| NativeError::Capacity)?;
        let handle = ResourceHandle::from_parts(index, generation);
        self.slots.push(CalcSlot {
            generation,
            expr: Some(expr),
            token: Some(Box::new(CalcToken { handle })),
        });
        Ok(handle)
    }

    pub(crate) fn remove(&mut self, handle: ResourceHandle) -> Result<(), NativeError> {
        self.get(handle)?;
        if self.active_expressions_reference(handle) {
            return Err(NativeError::InvalidValue);
        }
        let (index, generation) = handle.parts().ok_or(NativeError::ResourceNotFound)?;
        let slot = self.slots.get_mut(index as usize).ok_or(NativeError::ResourceNotFound)?;
        if slot.generation != generation || slot.expr.is_none() {
            return Err(NativeError::ResourceNotFound);
        }
        slot.expr = None;
        if let Some(token) = slot.token.take() {
            self.retired_tokens.push(token);
        }
        self.free.push(index);
        Ok(())
    }

    pub(crate) fn dimension(&self, handle: ResourceHandle) -> Result<Dimension, NativeError> {
        Ok(Dimension::calc(self.token_ptr(handle)?))
    }

    pub(crate) fn length_percentage(&self, handle: ResourceHandle) -> Result<LengthPercentage, NativeError> {
        Ok(LengthPercentage::calc(self.token_ptr(handle)?))
    }

    pub(crate) fn length_percentage_auto(
        &self,
        handle: ResourceHandle,
    ) -> Result<LengthPercentageAuto, NativeError> {
        Ok(LengthPercentageAuto::calc(self.token_ptr(handle)?))
    }

    pub(crate) fn resolve_ptr(&self, ptr: *const (), basis: f32) -> f32 {
        if ptr.is_null() || !basis.is_finite() {
            return 0.0;
        }
        let address = ptr as usize;
        let handle = self.slots.iter().find_map(|slot| {
            slot.token
                .as_ref()
                .filter(|token| (&***token as *const CalcToken as usize) == address)
                .map(|token| token.handle)
        });
        handle.and_then(|handle| self.evaluate(handle, basis).ok()).unwrap_or(0.0)
    }

    pub(crate) fn evaluate(&self, handle: ResourceHandle, basis: f32) -> Result<f32, NativeError> {
        self.evaluate_inner(handle, basis, 0)
    }

    fn evaluate_inner(&self, handle: ResourceHandle, basis: f32, depth: u8) -> Result<f32, NativeError> {
        if depth >= 64 {
            return Err(NativeError::InvalidValue);
        }
        let expr = self.get(handle)?;
        let next = depth + 1;
        let value = match expr {
            CalcExpr::Length(value) => *value,
            CalcExpr::Percent(value) => basis * *value,
            CalcExpr::Add(a, b) => self.evaluate_inner(*a, basis, next)? + self.evaluate_inner(*b, basis, next)?,
            CalcExpr::Sub(a, b) => self.evaluate_inner(*a, basis, next)? - self.evaluate_inner(*b, basis, next)?,
            CalcExpr::Scale(value, factor) => self.evaluate_inner(*value, basis, next)? * *factor,
            CalcExpr::Min(values) => values
                .iter()
                .map(|value| self.evaluate_inner(*value, basis, next))
                .collect::<Result<Vec<_>, _>>()?
                .into_iter()
                .fold(f32::INFINITY, f32::min),
            CalcExpr::Max(values) => values
                .iter()
                .map(|value| self.evaluate_inner(*value, basis, next))
                .collect::<Result<Vec<_>, _>>()?
                .into_iter()
                .fold(f32::NEG_INFINITY, f32::max),
            CalcExpr::Clamp { min, preferred, max } => {
                let min = self.evaluate_inner(*min, basis, next)?;
                let preferred = self.evaluate_inner(*preferred, basis, next)?;
                let max = self.evaluate_inner(*max, basis, next)?;
                preferred.clamp(min.min(max), max.max(min))
            }
        };
        if value.is_finite() { Ok(value) } else { Err(NativeError::InvalidValue) }
    }


    fn validate_dependencies(&self, expr: &CalcExpr) -> Result<(), NativeError> {
        for handle in expression_dependencies(expr) {
            self.get(handle)?;
        }
        Ok(())
    }

    fn active_expressions_reference(&self, target: ResourceHandle) -> bool {
        self.slots
            .iter()
            .filter_map(|slot| slot.expr.as_ref())
            .flat_map(expression_dependencies)
            .any(|handle| handle == target)
    }

    fn get(&self, handle: ResourceHandle) -> Result<&CalcExpr, NativeError> {
        let (index, generation) = handle.parts().ok_or(NativeError::ResourceNotFound)?;
        let slot = self.slots.get(index as usize).ok_or(NativeError::ResourceNotFound)?;
        if slot.generation != generation {
            return Err(NativeError::ResourceNotFound);
        }
        slot.expr.as_ref().ok_or(NativeError::ResourceNotFound)
    }

    pub(crate) fn token_ptr_for_taffy(&self, handle: ResourceHandle) -> Result<*const (), NativeError> {
        self.token_ptr(handle)
    }

    fn token_ptr(&self, handle: ResourceHandle) -> Result<*const (), NativeError> {
        let (index, generation) = handle.parts().ok_or(NativeError::ResourceNotFound)?;
        let slot = self.slots.get(index as usize).ok_or(NativeError::ResourceNotFound)?;
        if slot.generation != generation || slot.expr.is_none() {
            return Err(NativeError::ResourceNotFound);
        }
        let token = slot.token.as_ref().ok_or(NativeError::ResourceNotFound)?;
        Ok((&**token as *const CalcToken).cast())
    }
}

fn expression_dependencies(expr: &CalcExpr) -> Vec<ResourceHandle> {
    match expr {
        CalcExpr::Length(_) | CalcExpr::Percent(_) => Vec::new(),
        CalcExpr::Add(a, b) | CalcExpr::Sub(a, b) => vec![*a, *b],
        CalcExpr::Scale(value, _) => vec![*value],
        CalcExpr::Min(values) | CalcExpr::Max(values) => values.clone(),
        CalcExpr::Clamp { min, preferred, max } => vec![*min, *preferred, *max],
    }
}

fn validate_expr(expr: &CalcExpr) -> Result<(), NativeError> {
    let valid = match expr {
        CalcExpr::Length(v) | CalcExpr::Percent(v) => v.is_finite(),
        CalcExpr::Scale(_, factor) => factor.is_finite(),
        CalcExpr::Min(values) | CalcExpr::Max(values) => !values.is_empty(),
        _ => true,
    };
    if valid { Ok(()) } else { Err(NativeError::InvalidValue) }
}

static NEXT_RESOURCE_GENERATION: AtomicU32 = AtomicU32::new(1);

fn next_resource_generation() -> u32 {
    loop {
        let generation = NEXT_RESOURCE_GENERATION.fetch_add(1, Ordering::Relaxed);
        if generation != 0 {
            return generation;
        }
    }
}

#[cfg(test)]
mod tests {
    use super::{CalcExpr, CalcRegistry};

    #[test]
    fn calc_expression_resolves_against_basis() {
        let mut registry = CalcRegistry::default();
        let px = registry.insert(CalcExpr::Length(20.0)).unwrap();
        let pct = registry.insert(CalcExpr::Percent(0.5)).unwrap();
        let sum = registry.insert(CalcExpr::Add(px, pct)).unwrap();
        assert_eq!(registry.evaluate(sum, 200.0).unwrap(), 120.0);
        let value = registry.dimension(sum).unwrap();
        let ptr = value.into_raw().calc_value();
        assert_eq!(registry.resolve_ptr(ptr, 300.0), 170.0);
    }

    #[test]
    fn removed_calc_handle_stays_stale() {
        let mut registry = CalcRegistry::default();
        let first = registry.insert(CalcExpr::Length(1.0)).unwrap();
        registry.remove(first).unwrap();
        let second = registry.insert(CalcExpr::Length(2.0)).unwrap();
        assert_ne!(first, second);
        assert!(registry.evaluate(first, 1.0).is_err());
        assert_eq!(registry.evaluate(second, 1.0).unwrap(), 2.0);
    }

    #[test]
    fn composite_resources_require_live_operands() {
        let mut registry = CalcRegistry::default();
        let first = registry.insert(CalcExpr::Length(1.0)).unwrap();
        registry.remove(first).unwrap();
        assert!(registry.insert(CalcExpr::Scale(first, 2.0)).is_err());
    }

    #[test]
    fn referenced_calc_resource_cannot_be_removed() {
        let mut registry = CalcRegistry::default();
        let first = registry.insert(CalcExpr::Length(1.0)).unwrap();
        let second = registry.insert(CalcExpr::Length(2.0)).unwrap();
        let sum = registry.insert(CalcExpr::Add(first, second)).unwrap();
        assert!(registry.remove(first).is_err());
        registry.remove(sum).unwrap();
        registry.remove(first).unwrap();
    }
}
