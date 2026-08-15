//! TaffyUGUI native layout library.
//!
//! The crate is intentionally split by ownership before Phase 1 feature work begins. The
//! current exported surface remains bootstrap ABI version 0 and is not the frozen v1 ABI.

mod context;
mod error;
mod grid;
mod handles;
mod measurement;
mod version;

pub mod ffi;
pub mod style;

pub use ffi::*;
pub use style::*;
