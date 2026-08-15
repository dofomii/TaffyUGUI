//! Native handle definitions and encoding.
//!
//! Context handles are already production-shaped internally. The bootstrap ABI still wraps
//! them in an opaque pointer token until the fixed-width public C ABI is introduced.

pub(crate) type BootstrapNodeHandle = u64;
pub(crate) const FIRST_BOOTSTRAP_NODE_HANDLE: BootstrapNodeHandle = 1;

const INDEX_MASK: u64 = u32::MAX as u64;

#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub(crate) struct ContextHandle(u64);

impl ContextHandle {
    pub(crate) const INVALID: Self = Self(0);

    pub(crate) fn from_parts(index: u32, generation: u32) -> Self {
        debug_assert!(generation != 0);
        let encoded_index = u64::from(index) + 1;
        Self((u64::from(generation) << 32) | encoded_index)
    }

    pub(crate) fn parts(self) -> Option<(u32, u32)> {
        if self == Self::INVALID {
            return None;
        }

        let encoded_index = (self.0 & INDEX_MASK) as u32;
        let generation = (self.0 >> 32) as u32;
        if encoded_index == 0 || generation == 0 {
            return None;
        }

        Some((encoded_index - 1, generation))
    }

    #[cfg(test)]
    pub(crate) const fn raw(self) -> u64 {
        self.0
    }
}

#[cfg(test)]
mod tests {
    use super::ContextHandle;

    #[test]
    fn context_handle_round_trips_parts() {
        let handle = ContextHandle::from_parts(42, 7);
        assert_eq!(handle.parts(), Some((42, 7)));
        assert_ne!(handle.raw(), 0);
    }

    #[test]
    fn zero_handle_is_invalid() {
        assert_eq!(ContextHandle::INVALID.parts(), None);
    }
}
