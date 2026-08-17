// The FFI module intentionally keeps several generated-style one-line pointer copies;
// formatting them as `= *value` is unambiguous but Clippy flags the compact form.
#![allow(clippy::suspicious_assignment_formatting)]

//! TaffyUGUI native layout library.
//!
//! The native engine is persistent and Unity-independent. The exported `tu_*` surface is locked
//! as final ABI v1 (version 1, stage 2).

mod calc;
mod context;
mod error;
mod grid;
mod handles;
mod measurement;
mod version;

pub mod ffi;
mod style;

pub use ffi::*;
pub use version::*;
