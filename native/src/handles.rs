//! Transitional handle definitions used by bootstrap ABI version 0.
//!
//! Production generation-safe context and node handles are implemented in Phase 1.

pub(crate) type BootstrapNodeHandle = u64;
pub(crate) const FIRST_BOOTSTRAP_NODE_HANDLE: BootstrapNodeHandle = 1;
