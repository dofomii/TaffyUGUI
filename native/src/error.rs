//! Native status codes and internal error mapping.

pub(crate) const OK: i32 = 0;
pub(crate) const ERR_NULL: i32 = -1;
pub(crate) const ERR_NODE: i32 = -2;
pub(crate) const ERR_TAFFY: i32 = -3;

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub(crate) enum NativeError {
    ContextNotFound,
    NodeNotFound,
    Capacity,
    Engine,
}

impl NativeError {
    pub(crate) const fn status_code(self) -> i32 {
        match self {
            // ABI 0 did not have a distinct invalid-context status, so preserve its
            // existing behavior until the production status enum is introduced.
            Self::ContextNotFound => ERR_NULL,
            Self::NodeNotFound => ERR_NODE,
            Self::Capacity | Self::Engine => ERR_TAFFY,
        }
    }
}
