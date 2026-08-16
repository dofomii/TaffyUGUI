//! Native handle definitions and fixed-width slot/generation encoding.
//!
//! Context, node, and resource handles are production-shaped fixed-width values. The Phase 2
//! C ABI exposes only their opaque `u64` representation; slot/generation details stay private.

const INDEX_MASK: u64 = u32::MAX as u64;

fn encode_parts(index: u32, generation: u32) -> u64 {
    debug_assert!(generation != 0);
    let encoded_index = u64::from(index) + 1;
    (u64::from(generation) << 32) | encoded_index
}

fn decode_parts(raw: u64) -> Option<(u32, u32)> {
    if raw == 0 {
        return None;
    }

    let encoded_index = (raw & INDEX_MASK) as u32;
    let generation = (raw >> 32) as u32;
    if encoded_index == 0 || generation == 0 {
        return None;
    }

    Some((encoded_index - 1, generation))
}

#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub(crate) struct ContextHandle(u64);

impl ContextHandle {
    pub(crate) const fn from_raw(raw: u64) -> Self {
        Self(raw)
    }

    pub(crate) fn from_parts(index: u32, generation: u32) -> Self {
        Self(encode_parts(index, generation))
    }

    pub(crate) fn parts(self) -> Option<(u32, u32)> {
        decode_parts(self.0)
    }

    pub(crate) const fn raw(self) -> u64 {
        self.0
    }
}

#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub(crate) struct NodeHandle(u64);

impl NodeHandle {
    pub(crate) const fn from_raw(raw: u64) -> Self {
        Self(raw)
    }

    pub(crate) const fn raw(self) -> u64 {
        self.0
    }

    pub(crate) fn from_parts(index: u32, generation: u32) -> Self {
        Self(encode_parts(index, generation))
    }

    pub(crate) fn parts(self) -> Option<(u32, u32)> {
        decode_parts(self.0)
    }
}

#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub(crate) struct ResourceHandle(u64);

impl ResourceHandle {
    pub(crate) const fn from_raw(raw: u64) -> Self {
        Self(raw)
    }

    pub(crate) const fn raw(self) -> u64 {
        self.0
    }

    pub(crate) fn from_parts(index: u32, generation: u32) -> Self {
        Self(encode_parts(index, generation))
    }

    pub(crate) fn parts(self) -> Option<(u32, u32)> {
        decode_parts(self.0)
    }
}

#[cfg(test)]
mod tests {
    use super::{ContextHandle, NodeHandle, ResourceHandle};

    #[test]
    fn context_handle_round_trips_parts() {
        let handle = ContextHandle::from_parts(42, 7);
        assert_eq!(handle.parts(), Some((42, 7)));
        assert_ne!(handle.raw(), 0);
    }

    #[test]
    fn node_handle_round_trips_parts_and_raw_value() {
        let handle = NodeHandle::from_parts(12, 99);
        let raw = handle.raw();
        assert_eq!(NodeHandle::from_raw(raw), handle);
        assert_eq!(handle.parts(), Some((12, 99)));
    }

    #[test]
    fn resource_handle_round_trips_parts_and_raw_value() {
        let handle = ResourceHandle::from_parts(3, 11);
        assert_eq!(ResourceHandle::from_raw(handle.raw()), handle);
        assert_eq!(handle.parts(), Some((3, 11)));
    }

    #[test]
    fn zero_handles_are_invalid() {
        assert_eq!(ContextHandle(0).parts(), None);
        assert_eq!(NodeHandle::from_raw(0).parts(), None);
        assert_eq!(ResourceHandle::from_raw(0).parts(), None);
    }
}
