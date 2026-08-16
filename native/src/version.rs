//! Native ABI/build version constants.

pub const TU_ABI_VERSION: u32 = 0;
pub const TU_ABI_STAGE: u32 = 0; // 0 = candidate, 1 = RC, 2 = final
pub const TU_TAFFY_VERSION_MAJOR: u32 = 0;
pub const TU_TAFFY_VERSION_MINOR: u32 = 13;
pub const TU_TAFFY_VERSION_PATCH: u32 = 0;
pub const TU_CAP_FLEX: u64 = 1 << 0;
pub const TU_CAP_GRID: u64 = 1 << 1;
pub const TU_CAP_BLOCK: u64 = 1 << 2;
pub const TU_CAP_FLOAT: u64 = 1 << 3;
pub const TU_CAP_CALC: u64 = 1 << 4;
pub const TU_CAP_CONTENT_SIZE: u64 = 1 << 5;
pub const TU_CAP_DETAILED_GRID: u64 = 1 << 6;
pub const TU_CAP_CACHED_MEASUREMENT: u64 = 1 << 7;
pub const TU_CAP_THREAD_LOCAL_CONTEXTS: u64 = 1 << 8;
pub const TU_CAP_PANIC_UNWIND_GUARD: u64 = if cfg!(panic = "unwind") { 1 << 9 } else { 0 };
pub const TU_CAPABILITIES: u64 = TU_CAP_FLEX | TU_CAP_GRID | TU_CAP_BLOCK | TU_CAP_FLOAT | TU_CAP_CALC |
    TU_CAP_CONTENT_SIZE | TU_CAP_DETAILED_GRID | TU_CAP_CACHED_MEASUREMENT | TU_CAP_THREAD_LOCAL_CONTEXTS |
    TU_CAP_PANIC_UNWIND_GUARD;

