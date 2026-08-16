//! Stable native status codes and per-thread diagnostics.

use std::cell::RefCell;

#[repr(i32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum TuStatus {
    Ok = 0,
    NullPointer = -1,
    InvalidContext = -2,
    InvalidNode = -3,
    InvalidResource = -4,
    InvalidEnum = -5,
    InvalidCount = -6,
    InvalidNumber = -7,
    InvalidValue = -8,
    Capacity = -9,
    WrongThread = -10,
    RegistryBusy = -11,
    Engine = -12,
    InternalPanic = -13,
}


#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub(crate) enum NativeError {
    NullPointer,
    ContextNotFound,
    NodeNotFound,
    ResourceNotFound,
    InvalidEnum,
    InvalidCount,
    InvalidNumber,
    InvalidValue,
    Capacity,
    WrongThread,
    RegistryBusy,
}

impl NativeError {
    pub(crate) const fn status(self) -> TuStatus {
        match self {
            Self::NullPointer => TuStatus::NullPointer,
            Self::ContextNotFound => TuStatus::InvalidContext,
            Self::NodeNotFound => TuStatus::InvalidNode,
            Self::ResourceNotFound => TuStatus::InvalidResource,
            Self::InvalidEnum => TuStatus::InvalidEnum,
            Self::InvalidCount => TuStatus::InvalidCount,
            Self::InvalidNumber => TuStatus::InvalidNumber,
            Self::InvalidValue => TuStatus::InvalidValue,
            Self::Capacity => TuStatus::Capacity,
            Self::WrongThread => TuStatus::WrongThread,
            Self::RegistryBusy => TuStatus::RegistryBusy,
        }
    }
    pub(crate) const fn status_code(self) -> i32 { self.status() as i32 }
}


impl core::fmt::Display for NativeError {
    fn fmt(&self, formatter: &mut core::fmt::Formatter<'_>) -> core::fmt::Result {
        formatter.write_str(match self {
            Self::NullPointer => "required pointer was null",
            Self::ContextNotFound => "context handle is invalid or stale",
            Self::NodeNotFound => "node handle is invalid, stale, or belongs to another context",
            Self::ResourceNotFound => "resource handle is invalid, stale, or belongs to another context",
            Self::InvalidEnum => "enum value is outside the supported numeric range",
            Self::InvalidCount => "buffer count or capacity is invalid",
            Self::InvalidNumber => "numeric input is not finite or is outside the allowed range",
            Self::InvalidValue => "input value combination is invalid",
            Self::Capacity => "native fixed-width handle/count capacity was exceeded",
            Self::WrongThread => "context was used from a thread other than its owner",
            Self::RegistryBusy => "thread-local context registry is already mutably borrowed",
        })
    }
}

thread_local! {
    static LAST_ERROR: RefCell<String> = RefCell::new(String::new());
}

pub(crate) fn clear_last_error() { LAST_ERROR.with(|v| v.borrow_mut().clear()); }
pub(crate) fn set_last_error(message: impl Into<String>) { LAST_ERROR.with(|v| *v.borrow_mut() = message.into()); }
pub(crate) fn last_error_bytes() -> Vec<u8> { LAST_ERROR.with(|v| v.borrow().as_bytes().to_vec()) }
