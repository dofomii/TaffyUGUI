//! ABI-v1-RC production C ABI surface.
//!
//! All exported functions use fixed-width values and opaque generation-safe handles. Raw
//! pointers are caller-owned temporary buffers only. Expected invalid input is reported through
//! [`TuStatus`] and the per-thread last-error buffer. Every export is protected by a panic guard.

use std::panic::{catch_unwind, AssertUnwindSafe};
use std::ptr;

use taffy::geometry::{Line, Point, Rect, Size};
use taffy::prelude::*;
use taffy::style::{
    BoxSizing, Clear, Direction, Float, GridAutoFlow, GridPlacement, Overflow, Position, TextAlign,
};
use taffy::style_helpers::{auto, flex, length, max_content, min_content, minmax, percent, repeat};

use crate::calc::CalcExpr;
use crate::context::{
    create_registered_context, destroy_registered_context, with_registered_context_mut,
    LayoutResult,
};
pub use crate::error::TuStatus;
use crate::error::{clear_last_error, last_error_bytes, set_last_error, NativeError};
use crate::grid::{template_areas, GridTemplateResource};
use crate::handles::{ContextHandle, NodeHandle, ResourceHandle};
use crate::measurement::{MeasurementRecord, MeasurementSample};
use crate::version::*;

pub type TuContextHandle = u64;
pub type TuNodeHandle = u64;
pub type TuResourceHandle = u64;

#[repr(i32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum TuValueKind {
    Auto = 0,
    Length = 1,
    Percent = 2,
    Calc = 3,
}
#[repr(i32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum TuDisplay {
    None = 0,
    Flex = 1,
    Grid = 2,
    Block = 3,
    FlowRoot = 4,
}
#[repr(i32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum TuBoxSizing {
    BorderBox = 0,
    ContentBox = 1,
}
#[repr(i32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum TuDirection {
    Ltr = 0,
    Rtl = 1,
}
#[repr(i32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum TuOverflow {
    Visible = 0,
    Clip = 1,
    Hidden = 2,
    Scroll = 3,
}
#[repr(i32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum TuPosition {
    Relative = 0,
    Absolute = 1,
}
#[repr(i32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum TuFlexDirection {
    Row = 0,
    Column = 1,
    RowReverse = 2,
    ColumnReverse = 3,
}
#[repr(i32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum TuFlexWrap {
    NoWrap = 0,
    Wrap = 1,
    WrapReverse = 2,
}
#[repr(i32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum TuAlign {
    Unset = -1,
    Start = 0,
    End = 1,
    Center = 2,
    Stretch = 3,
    Baseline = 4,
    FlexStart = 5,
    FlexEnd = 6,
    SelfStart = 7,
    SelfEnd = 8,
    SafeStart = 9,
    SafeEnd = 10,
    SafeCenter = 11,
    SafeFlexStart = 12,
    SafeFlexEnd = 13,
    SafeSelfStart = 14,
    SafeSelfEnd = 15,
}
#[repr(i32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum TuAlignContent {
    Unset = -1,
    Start = 0,
    End = 1,
    Center = 2,
    Stretch = 3,
    SpaceBetween = 4,
    SpaceAround = 5,
    SpaceEvenly = 6,
    FlexStart = 7,
    FlexEnd = 8,
    SafeStart = 9,
    SafeEnd = 10,
    SafeCenter = 11,
    SafeFlexStart = 12,
    SafeFlexEnd = 13,
}
#[repr(i32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum TuTextAlign {
    Auto = 0,
    LegacyLeft = 1,
    LegacyRight = 2,
    LegacyCenter = 3,
}
#[repr(i32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum TuFloatMode {
    None = 0,
    Left = 1,
    Right = 2,
}
#[repr(i32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum TuClearMode {
    None = 0,
    Left = 1,
    Right = 2,
    Both = 3,
}
#[repr(i32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum TuGridAutoFlow {
    Row = 0,
    Column = 1,
    RowDense = 2,
    ColumnDense = 3,
}
#[repr(i32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum TuGridPlacementKind {
    Auto = 0,
    Line = 1,
    Span = 2,
    NamedLine = 3,
    NamedSpan = 4,
}
#[repr(i32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum TuCalcOp {
    Length = 0,
    Percent = 1,
    Add = 2,
    Sub = 3,
    Scale = 4,
    Min = 5,
    Max = 6,
    Clamp = 7,
}
#[repr(i32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum TuGridTrackKind {
    Auto = 0,
    Length = 1,
    Percent = 2,
    Fraction = 3,
    MinMax = 4,
    MinContent = 5,
    MaxContent = 6,
    Calc = 7,
    Repeat = 8,
}
#[repr(i32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum TuGridRepeatMode {
    Count = 0,
    AutoFill = 1,
    AutoFit = 2,
}
#[repr(i32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum TuGridAxis {
    Row = 0,
    Column = 1,
}

#[repr(C)]
#[derive(Clone, Copy)]
pub struct TuStringView {
    pub data: *const u8,
    pub len: u32,
}
#[repr(C)]
#[derive(Clone, Copy)]
pub struct TuValue {
    pub kind: i32,
    pub value: f32,
    pub resource: TuResourceHandle,
}
#[repr(C)]
#[derive(Clone, Copy)]
pub struct TuGridPlacement {
    pub kind: i32,
    pub line: i32,
    pub span: u32,
    pub occurrence: i32,
    pub name: TuStringView,
}

#[repr(C)]
#[derive(Clone, Copy)]
pub struct TuStyle {
    pub display: i32,
    pub box_sizing: i32,
    pub direction: i32,
    pub overflow_x: i32,
    pub overflow_y: i32,
    pub scrollbar_width: f32,
    pub position: i32,
    pub inset_left: TuValue,
    pub inset_right: TuValue,
    pub inset_top: TuValue,
    pub inset_bottom: TuValue,
    pub width: TuValue,
    pub height: TuValue,
    pub min_width: TuValue,
    pub min_height: TuValue,
    pub max_width: TuValue,
    pub max_height: TuValue,
    pub aspect_ratio: f32,
    pub margin_left: TuValue,
    pub margin_right: TuValue,
    pub margin_top: TuValue,
    pub margin_bottom: TuValue,
    pub padding_left: TuValue,
    pub padding_right: TuValue,
    pub padding_top: TuValue,
    pub padding_bottom: TuValue,
    pub border_left: TuValue,
    pub border_right: TuValue,
    pub border_top: TuValue,
    pub border_bottom: TuValue,
    pub flex_direction: i32,
    pub flex_wrap: i32,
    pub flex_basis: TuValue,
    pub flex_grow: f32,
    pub flex_shrink: f32,
    pub align_items: i32,
    pub align_self: i32,
    pub align_content: i32,
    pub justify_content: i32,
    pub justify_items: i32,
    pub justify_self: i32,
    pub gap_x: TuValue,
    pub gap_y: TuValue,
    pub item_is_table: u8,
    pub item_is_replaced: u8,
    pub float_mode: i32,
    pub clear_mode: i32,
    pub text_align: i32,
    pub grid_auto_flow: i32,
    pub grid_row_start: TuGridPlacement,
    pub grid_row_end: TuGridPlacement,
    pub grid_column_start: TuGridPlacement,
    pub grid_column_end: TuGridPlacement,
}
#[repr(C)]
#[derive(Clone, Copy)]
pub struct TuStyleUpdate {
    pub node: TuNodeHandle,
    pub style: TuStyle,
}
#[repr(C)]
#[derive(Clone, Copy)]
pub struct TuMeasurementSample {
    pub available_width: f32,
    pub width: f32,
    pub height: f32,
}
#[repr(C)]
#[derive(Clone, Copy)]
pub struct TuMeasurement {
    pub min_width: f32,
    pub min_height: f32,
    pub max_width: f32,
    pub max_height: f32,
    pub preferred_width: f32,
    pub preferred_height: f32,
    pub aspect_ratio: f32,
    pub is_replaced: u8,
    pub samples: *const TuMeasurementSample,
    pub sample_count: u32,
}
#[repr(C)]
#[derive(Clone, Copy)]
pub struct TuMeasurementUpdate {
    pub node: TuNodeHandle,
    pub measurement: TuMeasurement,
    pub has_measurement: u8,
}
#[repr(C)]
#[derive(Clone, Copy)]
pub struct TuChildrenUpdate {
    pub parent: TuNodeHandle,
    pub children: *const TuNodeHandle,
    pub child_count: u32,
}
#[repr(C)]
#[derive(Clone, Copy)]
pub struct TuLayout {
    pub node: TuNodeHandle,
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

#[repr(C)]
#[derive(Clone, Copy)]
pub struct TuCalcSpec {
    pub op: i32,
    pub value: f32,
    pub operands: *const TuResourceHandle,
    pub operand_count: u32,
}
#[repr(C)]
#[derive(Clone, Copy)]
pub struct TuGridTrack {
    pub kind: i32,
    pub value: f32,
    pub resource: TuResourceHandle,
    pub min_kind: i32,
    pub min_value: f32,
    pub min_resource: TuResourceHandle,
    pub max_kind: i32,
    pub max_value: f32,
    pub max_resource: TuResourceHandle,
    pub repeat_mode: i32,
    pub repeat_count: u32,
    pub repeat_tracks: *const TuGridTrack,
    pub repeat_track_count: u32,
}
#[repr(C)]
#[derive(Clone, Copy)]
pub struct TuNamedGridLine {
    pub axis: i32,
    pub line_index: u32,
    pub name: TuStringView,
}
#[repr(C)]
#[derive(Clone, Copy)]
pub struct TuGridArea {
    pub name: TuStringView,
    pub row_start: u32,
    pub row_end: u32,
    pub column_start: u32,
    pub column_end: u32,
}
#[repr(C)]
#[derive(Clone, Copy)]
pub struct TuGridInfo {
    pub negative_implicit_rows: u32,
    pub explicit_rows: u32,
    pub positive_implicit_rows: u32,
    pub negative_implicit_columns: u32,
    pub explicit_columns: u32,
    pub positive_implicit_columns: u32,
    pub row_track_count: u32,
    pub column_track_count: u32,
    pub item_count: u32,
}
#[repr(C)]
#[derive(Clone, Copy)]
pub struct TuGridItemInfo {
    pub row_start: u32,
    pub row_end: u32,
    pub column_start: u32,
    pub column_end: u32,
}
#[repr(C)]
#[derive(Clone, Copy)]
pub struct TuGridTemplate {
    pub rows: *const TuGridTrack,
    pub row_count: u32,
    pub columns: *const TuGridTrack,
    pub column_count: u32,
    pub auto_rows: *const TuGridTrack,
    pub auto_row_count: u32,
    pub auto_columns: *const TuGridTrack,
    pub auto_column_count: u32,
    pub named_lines: *const TuNamedGridLine,
    pub named_line_count: u32,
    pub areas: *const TuGridArea,
    pub area_count: u32,
    pub area_rows: u32,
    pub area_columns: u32,
}

fn guard(f: impl FnOnce() -> Result<(), NativeError>) -> i32 {
    clear_last_error();
    match catch_unwind(AssertUnwindSafe(f)) {
        Ok(Ok(())) => TuStatus::Ok as i32,
        Ok(Err(e)) => {
            set_last_error(e.to_string());
            e.status_code()
        }
        Err(_) => {
            set_last_error("unexpected native panic");
            TuStatus::InternalPanic as i32
        }
    }
}
fn ctx(raw: u64) -> Result<ContextHandle, NativeError> {
    let h = ContextHandle::from_raw(raw);
    h.parts().ok_or(NativeError::ContextNotFound)?;
    Ok(h)
}
fn node(raw: u64) -> Result<NodeHandle, NativeError> {
    let h = NodeHandle::from_raw(raw);
    h.parts().ok_or(NativeError::NodeNotFound)?;
    Ok(h)
}
fn resource(raw: u64) -> Result<ResourceHandle, NativeError> {
    let h = ResourceHandle::from_raw(raw);
    h.parts().ok_or(NativeError::ResourceNotFound)?;
    Ok(h)
}
fn finite(v: f32) -> Result<f32, NativeError> {
    if v.is_finite() {
        Ok(v)
    } else {
        Err(NativeError::InvalidNumber)
    }
}
fn nonneg(v: f32) -> Result<f32, NativeError> {
    let v = finite(v)?;
    if v >= 0.0 {
        Ok(v)
    } else {
        Err(NativeError::InvalidNumber)
    }
}
fn bool8(v: u8) -> Result<bool, NativeError> {
    match v {
        0 => Ok(false),
        1 => Ok(true),
        _ => Err(NativeError::InvalidValue),
    }
}
unsafe fn slice<'a, T>(ptr: *const T, count: u32) -> Result<&'a [T], NativeError> {
    if count == 0 {
        return Ok(&[]);
    };
    if ptr.is_null() {
        return Err(NativeError::NullPointer);
    };
    Ok(unsafe { std::slice::from_raw_parts(ptr, count as usize) })
}
unsafe fn string(view: TuStringView) -> Result<String, NativeError> {
    let bytes = unsafe { slice(view.data, view.len)? };
    std::str::from_utf8(bytes)
        .map(str::to_owned)
        .map_err(|_| NativeError::InvalidValue)
}

fn align_item(v: i32) -> Result<Option<AlignItems>, NativeError> {
    Ok(match v {
        -1 => None,
        0 => Some(AlignItems::START),
        1 => Some(AlignItems::END),
        2 => Some(AlignItems::CENTER),
        3 => Some(AlignItems::STRETCH),
        4 => Some(AlignItems::BASELINE),
        5 => Some(AlignItems::FLEX_START),
        6 => Some(AlignItems::FLEX_END),
        7 => Some(AlignItems::SELF_START),
        8 => Some(AlignItems::SELF_END),
        9 => Some(AlignItems::SAFE_START),
        10 => Some(AlignItems::SAFE_END),
        11 => Some(AlignItems::SAFE_CENTER),
        12 => Some(AlignItems::SAFE_FLEX_START),
        13 => Some(AlignItems::SAFE_FLEX_END),
        14 => Some(AlignItems::SAFE_SELF_START),
        15 => Some(AlignItems::SAFE_SELF_END),
        _ => return Err(NativeError::InvalidEnum),
    })
}
fn align_content(v: i32) -> Result<Option<AlignContent>, NativeError> {
    Ok(match v {
        -1 => None,
        0 => Some(AlignContent::START),
        1 => Some(AlignContent::END),
        2 => Some(AlignContent::CENTER),
        3 => Some(AlignContent::STRETCH),
        4 => Some(AlignContent::SPACE_BETWEEN),
        5 => Some(AlignContent::SPACE_AROUND),
        6 => Some(AlignContent::SPACE_EVENLY),
        7 => Some(AlignContent::FLEX_START),
        8 => Some(AlignContent::FLEX_END),
        9 => Some(AlignContent::SAFE_START),
        10 => Some(AlignContent::SAFE_END),
        11 => Some(AlignContent::SAFE_CENTER),
        12 => Some(AlignContent::SAFE_FLEX_START),
        13 => Some(AlignContent::SAFE_FLEX_END),
        _ => return Err(NativeError::InvalidEnum),
    })
}
fn dim(c: &crate::context::Context, v: TuValue) -> Result<Dimension, NativeError> {
    match v.kind {
        0 => Ok(auto()),
        1 => Ok(length(nonneg(v.value)?)),
        2 => Ok(percent(nonneg(v.value)?)),
        3 => c.calc_dimension(resource(v.resource)?),
        _ => Err(NativeError::InvalidEnum),
    }
}
fn lpa(c: &crate::context::Context, v: TuValue) -> Result<LengthPercentageAuto, NativeError> {
    match v.kind {
        0 => Ok(auto()),
        1 => Ok(length(finite(v.value)?)),
        2 => Ok(percent(finite(v.value)?)),
        3 => c.calc_length_percentage_auto(resource(v.resource)?),
        _ => Err(NativeError::InvalidEnum),
    }
}
fn lp(c: &crate::context::Context, v: TuValue) -> Result<LengthPercentage, NativeError> {
    match v.kind {
        0 => Ok(length(0.0)),
        1 => Ok(length(nonneg(v.value)?)),
        2 => Ok(percent(nonneg(v.value)?)),
        3 => c.calc_length_percentage(resource(v.resource)?),
        _ => Err(NativeError::InvalidEnum),
    }
}
unsafe fn placement(v: TuGridPlacement) -> Result<GridPlacement<String>, NativeError> {
    Ok(match v.kind {
        0 => GridPlacement::Auto,
        1 => taffy::style_helpers::line(v.line.clamp(i16::MIN as i32, i16::MAX as i32) as i16),
        2 => GridPlacement::Span(u16::try_from(v.span).map_err(|_| NativeError::InvalidValue)?),
        3 => GridPlacement::NamedLine(
            unsafe { string(v.name)? },
            v.occurrence.clamp(i16::MIN as i32, i16::MAX as i32) as i16,
        ),
        4 => GridPlacement::NamedSpan(
            unsafe { string(v.name)? },
            u16::try_from(v.span).map_err(|_| NativeError::InvalidValue)?,
        ),
        _ => return Err(NativeError::InvalidEnum),
    })
}

unsafe fn style(c: &crate::context::Context, s: TuStyle) -> Result<Style, NativeError> {
    let display = match s.display {
        0 => Display::None,
        1 => Display::Flex,
        2 => Display::Grid,
        3 => Display::Block,
        4 => Display::FlowRoot,
        _ => return Err(NativeError::InvalidEnum),
    };
    let overflow = |v| {
        Ok(match v {
            0 => Overflow::Visible,
            1 => Overflow::Clip,
            2 => Overflow::Hidden,
            3 => Overflow::Scroll,
            _ => return Err(NativeError::InvalidEnum),
        })
    };
    let mut out = Style {
        display,
        box_sizing: match s.box_sizing {
            0 => BoxSizing::BorderBox,
            1 => BoxSizing::ContentBox,
            _ => return Err(NativeError::InvalidEnum),
        },
        direction: match s.direction {
            0 => Direction::Ltr,
            1 => Direction::Rtl,
            _ => return Err(NativeError::InvalidEnum),
        },
        overflow: Point {
            x: overflow(s.overflow_x)?,
            y: overflow(s.overflow_y)?,
        },
        scrollbar_width: nonneg(s.scrollbar_width)?,
        position: match s.position {
            0 => Position::Relative,
            1 => Position::Absolute,
            _ => return Err(NativeError::InvalidEnum),
        },
        inset: Rect {
            left: lpa(c, s.inset_left)?,
            right: lpa(c, s.inset_right)?,
            top: lpa(c, s.inset_top)?,
            bottom: lpa(c, s.inset_bottom)?,
        },
        size: Size {
            width: dim(c, s.width)?,
            height: dim(c, s.height)?,
        },
        min_size: Size {
            width: dim(c, s.min_width)?,
            height: dim(c, s.min_height)?,
        },
        max_size: Size {
            width: dim(c, s.max_width)?,
            height: dim(c, s.max_height)?,
        },
        aspect_ratio: if s.aspect_ratio == 0.0 {
            None
        } else {
            Some(nonneg(s.aspect_ratio)?)
        },
        margin: Rect {
            left: lpa(c, s.margin_left)?,
            right: lpa(c, s.margin_right)?,
            top: lpa(c, s.margin_top)?,
            bottom: lpa(c, s.margin_bottom)?,
        },
        padding: Rect {
            left: lp(c, s.padding_left)?,
            right: lp(c, s.padding_right)?,
            top: lp(c, s.padding_top)?,
            bottom: lp(c, s.padding_bottom)?,
        },
        border: Rect {
            left: lp(c, s.border_left)?,
            right: lp(c, s.border_right)?,
            top: lp(c, s.border_top)?,
            bottom: lp(c, s.border_bottom)?,
        },
        flex_direction: match s.flex_direction {
            0 => FlexDirection::Row,
            1 => FlexDirection::Column,
            2 => FlexDirection::RowReverse,
            3 => FlexDirection::ColumnReverse,
            _ => return Err(NativeError::InvalidEnum),
        },
        flex_wrap: match s.flex_wrap {
            0 => FlexWrap::NoWrap,
            1 => FlexWrap::Wrap,
            2 => FlexWrap::WrapReverse,
            _ => return Err(NativeError::InvalidEnum),
        },
        flex_basis: dim(c, s.flex_basis)?,
        flex_grow: nonneg(s.flex_grow)?,
        flex_shrink: nonneg(s.flex_shrink)?,
        align_items: align_item(s.align_items)?,
        align_self: align_item(s.align_self)?,
        align_content: align_content(s.align_content)?,
        justify_content: align_content(s.justify_content)?,
        justify_items: align_item(s.justify_items)?,
        justify_self: align_item(s.justify_self)?,
        gap: Size {
            width: lp(c, s.gap_x)?,
            height: lp(c, s.gap_y)?,
        },
        item_is_table: bool8(s.item_is_table)?,
        item_is_replaced: bool8(s.item_is_replaced)?,
        float: match s.float_mode {
            0 => Float::None,
            1 => Float::Left,
            2 => Float::Right,
            _ => return Err(NativeError::InvalidEnum),
        },
        clear: match s.clear_mode {
            0 => Clear::None,
            1 => Clear::Left,
            2 => Clear::Right,
            3 => Clear::Both,
            _ => return Err(NativeError::InvalidEnum),
        },
        text_align: match s.text_align {
            0 => TextAlign::Auto,
            1 => TextAlign::LegacyLeft,
            2 => TextAlign::LegacyRight,
            3 => TextAlign::LegacyCenter,
            _ => return Err(NativeError::InvalidEnum),
        },
        grid_auto_flow: match s.grid_auto_flow {
            0 => GridAutoFlow::Row,
            1 => GridAutoFlow::Column,
            2 => GridAutoFlow::RowDense,
            3 => GridAutoFlow::ColumnDense,
            _ => return Err(NativeError::InvalidEnum),
        },
        grid_row: Line {
            start: unsafe { placement(s.grid_row_start)? },
            end: unsafe { placement(s.grid_row_end)? },
        },
        grid_column: Line {
            start: unsafe { placement(s.grid_column_start)? },
            end: unsafe { placement(s.grid_column_end)? },
        },
        ..Default::default()
    };
    if out.aspect_ratio == Some(0.0) {
        out.aspect_ratio = None
    };
    Ok(out)
}
unsafe fn measurement(m: TuMeasurement) -> Result<MeasurementRecord, NativeError> {
    let samples = unsafe { slice(m.samples, m.sample_count)? }
        .iter()
        .map(|s| {
            Ok(MeasurementSample {
                available_width: nonneg(s.available_width)?,
                size: Size {
                    width: nonneg(s.width)?,
                    height: nonneg(s.height)?,
                },
            })
        })
        .collect::<Result<Vec<_>, NativeError>>()?;
    let r = MeasurementRecord {
        min_content: Size {
            width: nonneg(m.min_width)?,
            height: nonneg(m.min_height)?,
        },
        max_content: Size {
            width: nonneg(m.max_width)?,
            height: nonneg(m.max_height)?,
        },
        preferred: Size {
            width: nonneg(m.preferred_width)?,
            height: nonneg(m.preferred_height)?,
        },
        aspect_ratio: if m.aspect_ratio == 0.0 {
            None
        } else {
            Some(nonneg(m.aspect_ratio)?)
        },
        is_replaced: bool8(m.is_replaced)?,
        width_samples: samples,
    };
    r.validate()?;
    Ok(r)
}

#[no_mangle]
pub extern "C" fn tu_get_abi_version() -> u32 {
    TU_ABI_VERSION
}
#[no_mangle]
pub extern "C" fn tu_get_abi_stage() -> u32 {
    TU_ABI_STAGE
}
#[no_mangle]
pub extern "C" fn tu_get_capabilities() -> u64 {
    TU_CAPABILITIES
}
#[no_mangle]
pub extern "C" fn tu_get_taffy_version_packed() -> u32 {
    (TU_TAFFY_VERSION_MAJOR << 24) | (TU_TAFFY_VERSION_MINOR << 12) | TU_TAFFY_VERSION_PATCH
}
#[no_mangle]
pub extern "C" fn tu_get_build_version_length() -> u32 {
    u32::try_from(env!("CARGO_PKG_VERSION").len()).unwrap_or(u32::MAX)
}
/// Copies the native package/build version.
/// # Safety
/// `buffer` must reference `capacity` writable bytes and `out_written` must be writable.
#[no_mangle]
pub unsafe extern "C" fn tu_copy_build_version(
    buffer: *mut u8,
    capacity: u32,
    out_written: *mut u32,
) -> i32 {
    guard(|| {
        let bytes = env!("CARGO_PKG_VERSION").as_bytes();
        let w = unsafe { out_written.as_mut() }.ok_or(NativeError::NullPointer)?;
        if capacity > 0 && buffer.is_null() {
            return Err(NativeError::NullPointer);
        };
        let n = bytes.len().min(capacity as usize);
        if n > 0 {
            unsafe { ptr::copy_nonoverlapping(bytes.as_ptr(), buffer, n) }
        };
        *w = n as u32;
        Ok(())
    })
}
#[no_mangle]
pub extern "C" fn tu_get_last_error_length() -> u32 {
    u32::try_from(last_error_bytes().len()).unwrap_or(u32::MAX)
}
/// Copies the thread-local diagnostic string.
/// # Safety
/// `buffer` must reference `capacity` writable bytes and `out_written` must reference one writable `u32`.
#[no_mangle]
pub unsafe extern "C" fn tu_copy_last_error(
    buffer: *mut u8,
    capacity: u32,
    out_written: *mut u32,
) -> i32 {
    guard(|| {
        let bytes = last_error_bytes();
        if out_written.is_null() {
            return Err(NativeError::NullPointer);
        };
        if capacity > 0 && buffer.is_null() {
            return Err(NativeError::NullPointer);
        };
        let n = bytes.len().min(capacity as usize);
        if n > 0 {
            unsafe { ptr::copy_nonoverlapping(bytes.as_ptr(), buffer, n) }
        };
        unsafe { *out_written = n as u32 };
        Ok(())
    })
}
/// Creates a context.
/// # Safety
/// `out_context` must point to writable storage for one context handle.
#[no_mangle]
pub unsafe extern "C" fn tu_context_create(out_context: *mut TuContextHandle) -> i32 {
    guard(|| {
        let out = unsafe { out_context.as_mut() }.ok_or(NativeError::NullPointer)?;
        *out = create_registered_context()?.raw();
        Ok(())
    })
}
#[no_mangle]
pub extern "C" fn tu_context_destroy(context: TuContextHandle) -> i32 {
    guard(|| destroy_registered_context(ctx(context)?))
}
#[no_mangle]
pub extern "C" fn tu_context_clear(context: TuContextHandle) -> i32 {
    guard(|| {
        with_registered_context_mut(ctx(context)?, |c| {
            c.clear();
            Ok(())
        })
    })
}
/// Creates a node.
/// # Safety
/// `style_ptr` must point to one initialized `TuStyle`, and `out_node` must be writable.
#[no_mangle]
pub unsafe extern "C" fn tu_node_create(
    context: TuContextHandle,
    style_ptr: *const TuStyle,
    out_node: *mut TuNodeHandle,
) -> i32 {
    guard(|| {
        with_registered_context_mut(ctx(context)?, |c| {
            let s = unsafe { style_ptr.as_ref() }.ok_or(NativeError::NullPointer)?;
            let style = unsafe { style(c, *s)? };
            let h = c.create_node(style)?;
            let out = unsafe { out_node.as_mut() }.ok_or(NativeError::NullPointer)?;
            *out = h.raw();
            Ok(())
        })
    })
}
#[no_mangle]
pub extern "C" fn tu_node_remove(context: TuContextHandle, node_handle: TuNodeHandle) -> i32 {
    guard(|| with_registered_context_mut(ctx(context)?, |c| c.remove_node(node(node_handle)?)))
}
/// Sets one style.
/// # Safety
/// `style_ptr` must point to one initialized `TuStyle` for the duration of the call.
#[no_mangle]
pub unsafe extern "C" fn tu_node_set_style(
    context: TuContextHandle,
    node_handle: TuNodeHandle,
    style_ptr: *const TuStyle,
) -> i32 {
    guard(|| {
        with_registered_context_mut(ctx(context)?, |c| {
            let s = unsafe { style_ptr.as_ref() }.ok_or(NativeError::NullPointer)?;
            let st = unsafe { style(c, *s)? };
            c.set_style(node(node_handle)?, st)
        })
    })
}
/// Bulk style upload.
/// # Safety
/// `updates` must reference `count` initialized entries when `count` is non-zero.
#[no_mangle]
pub unsafe extern "C" fn tu_nodes_set_styles(
    context: TuContextHandle,
    updates: *const TuStyleUpdate,
    count: u32,
) -> i32 {
    guard(|| {
        with_registered_context_mut(ctx(context)?, |c| {
            let input = unsafe { slice(updates, count)? };
            let mut v = Vec::with_capacity(input.len());
            for u in input {
                v.push((node(u.node)?, unsafe { style(c, u.style)? }))
            }
            c.set_styles_bulk(&v)
        })
    })
}
/// Replaces children.
/// # Safety
/// `children` must reference `count` node handles when `count` is non-zero.
#[no_mangle]
pub unsafe extern "C" fn tu_node_set_children(
    context: TuContextHandle,
    parent: TuNodeHandle,
    children: *const TuNodeHandle,
    count: u32,
) -> i32 {
    guard(|| {
        with_registered_context_mut(ctx(context)?, |c| {
            let ids = unsafe { slice(children, count)? }
                .iter()
                .map(|v| node(*v))
                .collect::<Result<Vec<_>, _>>()?;
            c.set_children(node(parent)?, &ids)
        })
    })
}
/// Bulk topology upload.
/// # Safety
/// `updates` and every nested child buffer must remain valid for the duration of the call.
#[no_mangle]
pub unsafe extern "C" fn tu_nodes_set_children(
    context: TuContextHandle,
    updates: *const TuChildrenUpdate,
    count: u32,
) -> i32 {
    guard(|| {
        with_registered_context_mut(ctx(context)?, |c| {
            let input = unsafe { slice(updates, count)? };
            let mut v = Vec::new();
            for u in input {
                v.push((
                    node(u.parent)?,
                    unsafe { slice(u.children, u.child_count)? }
                        .iter()
                        .map(|x| node(*x))
                        .collect::<Result<Vec<_>, _>>()?,
                ))
            }
            c.set_children_bulk(&v)
        })
    })
}
#[no_mangle]
pub extern "C" fn tu_node_mark_dirty(context: TuContextHandle, node_handle: TuNodeHandle) -> i32 {
    guard(|| with_registered_context_mut(ctx(context)?, |c| c.mark_dirty(node(node_handle)?)))
}
/// Returns dirty state as `0` or `1`.
/// # Safety
/// `out_dirty` must point to one writable byte.
#[no_mangle]
pub unsafe extern "C" fn tu_node_is_dirty(
    context: TuContextHandle,
    node_handle: TuNodeHandle,
    out_dirty: *mut u8,
) -> i32 {
    guard(|| {
        with_registered_context_mut(ctx(context)?, |c| {
            let o = unsafe { out_dirty.as_mut() }.ok_or(NativeError::NullPointer)?;
            *o = u8::from(c.is_dirty(node(node_handle)?)?);
            Ok(())
        })
    })
}
/// Sets or clears a cached measurement record.
/// # Safety
/// When non-null, `measurement_ptr` and its nested sample buffer must remain valid for the call.
#[no_mangle]
pub unsafe extern "C" fn tu_node_set_measurement(
    context: TuContextHandle,
    node_handle: TuNodeHandle,
    measurement_ptr: *const TuMeasurement,
) -> i32 {
    guard(|| {
        with_registered_context_mut(ctx(context)?, |c| {
            let m = if measurement_ptr.is_null() {
                None
            } else {
                Some(unsafe { measurement(*measurement_ptr)? })
            };
            c.set_measurement(node(node_handle)?, m)
        })
    })
}
/// Bulk measurement upload.
/// # Safety
/// `updates` and each nested sample buffer must remain valid for the duration of the call.
#[no_mangle]
pub unsafe extern "C" fn tu_nodes_set_measurements(
    context: TuContextHandle,
    updates: *const TuMeasurementUpdate,
    count: u32,
) -> i32 {
    guard(|| {
        with_registered_context_mut(ctx(context)?, |c| {
            let input = unsafe { slice(updates, count)? };
            let mut v = Vec::new();
            for u in input {
                v.push((
                    node(u.node)?,
                    if !bool8(u.has_measurement)? {
                        None
                    } else {
                        Some(unsafe { measurement(u.measurement)? })
                    },
                ))
            }
            c.set_measurements_bulk(&v)
        })
    })
}
/// Creates a typed Calc resource.
/// # Safety
/// `spec` and its operand buffer must remain valid for the call, and `out_resource` must be writable.
#[no_mangle]
pub unsafe extern "C" fn tu_calc_create(
    context: TuContextHandle,
    spec: *const TuCalcSpec,
    out_resource: *mut TuResourceHandle,
) -> i32 {
    guard(|| {
        with_registered_context_mut(ctx(context)?, |c| {
            let s = unsafe { spec.as_ref() }.ok_or(NativeError::NullPointer)?;
            let ops = unsafe { slice(s.operands, s.operand_count)? }
                .iter()
                .map(|h| resource(*h))
                .collect::<Result<Vec<_>, _>>()?;
            let expr = match s.op {
                0 => CalcExpr::Length(finite(s.value)?),
                1 => CalcExpr::Percent(finite(s.value)?),
                2 if ops.len() == 2 => CalcExpr::Add(ops[0], ops[1]),
                3 if ops.len() == 2 => CalcExpr::Sub(ops[0], ops[1]),
                4 if ops.len() == 1 => CalcExpr::Scale(ops[0], finite(s.value)?),
                5 => CalcExpr::Min(ops),
                6 => CalcExpr::Max(ops),
                7 if ops.len() == 3 => CalcExpr::Clamp {
                    min: ops[0],
                    preferred: ops[1],
                    max: ops[2],
                },
                _ => return Err(NativeError::InvalidValue),
            };
            let h = c.create_calc(expr)?;
            let o = unsafe { out_resource.as_mut() }.ok_or(NativeError::NullPointer)?;
            *o = h.raw();
            Ok(())
        })
    })
}
#[no_mangle]
pub extern "C" fn tu_calc_remove(context: TuContextHandle, res: TuResourceHandle) -> i32 {
    guard(|| with_registered_context_mut(ctx(context)?, |c| c.remove_calc(resource(res)?)))
}

fn min_track(
    context: &crate::context::Context,
    kind: i32,
    value: f32,
    resource_handle: TuResourceHandle,
) -> Result<MinTrackSizingFunction, NativeError> {
    Ok(match kind {
        0 => auto(),
        1 => length(nonneg(value)?),
        2 => percent(nonneg(value)?),
        5 => min_content(),
        6 => max_content(),
        7 => MinTrackSizingFunction::calc(context.calc_ptr(resource(resource_handle)?)?),
        _ => return Err(NativeError::InvalidEnum),
    })
}
fn max_track(
    context: &crate::context::Context,
    kind: i32,
    value: f32,
    resource_handle: TuResourceHandle,
) -> Result<MaxTrackSizingFunction, NativeError> {
    Ok(match kind {
        0 => auto(),
        1 => length(nonneg(value)?),
        2 => percent(nonneg(value)?),
        3 => taffy::style_helpers::fr(nonneg(value)?),
        5 => min_content(),
        6 => max_content(),
        7 => MaxTrackSizingFunction::calc(context.calc_ptr(resource(resource_handle)?)?),
        _ => return Err(NativeError::InvalidEnum),
    })
}
fn sizing_track(
    context: &crate::context::Context,
    track: &TuGridTrack,
) -> Result<TrackSizingFunction, NativeError> {
    Ok(match track.kind {
        0 => auto(),
        1 => length(nonneg(track.value)?),
        2 => percent(nonneg(track.value)?),
        3 => flex(nonneg(track.value)?),
        4 => minmax(
            min_track(context, track.min_kind, track.min_value, track.min_resource)?,
            max_track(context, track.max_kind, track.max_value, track.max_resource)?,
        ),
        5 => min_content(),
        6 => max_content(),
        7 => minmax(
            MinTrackSizingFunction::calc(context.calc_ptr(resource(track.resource)?)?),
            MaxTrackSizingFunction::calc(context.calc_ptr(resource(track.resource)?)?),
        ),
        _ => return Err(NativeError::InvalidEnum),
    })
}
unsafe fn grid_component(
    context: &crate::context::Context,
    track: &TuGridTrack,
) -> Result<GridTemplateComponent<String>, NativeError> {
    Ok(match track.kind {
        0..=7 => GridTemplateComponent::Single(sizing_track(context, track)?),
        8 => {
            let tracks = unsafe { slice(track.repeat_tracks, track.repeat_track_count)? }
                .iter()
                .map(|nested| sizing_track(context, nested))
                .collect::<Result<Vec<_>, _>>()?;
            if tracks.is_empty() {
                return Err(NativeError::InvalidCount);
            };
            match track.repeat_mode {
                0 => {
                    let count =
                        u16::try_from(track.repeat_count).map_err(|_| NativeError::InvalidValue)?;
                    if count == 0 {
                        return Err(NativeError::InvalidValue);
                    };
                    repeat(count, tracks)
                }
                1 => repeat(RepetitionCount::AutoFill, tracks),
                2 => repeat(RepetitionCount::AutoFit, tracks),
                _ => return Err(NativeError::InvalidEnum),
            }
        }
        _ => return Err(NativeError::InvalidEnum),
    })
}
/// Applies Grid track, named-line, and area data to a node style.
/// # Safety
/// `template` and all nested buffers/string views must remain valid for the duration of the call.
#[no_mangle]
pub unsafe extern "C" fn tu_node_set_grid_template(
    context: TuContextHandle,
    node_handle: TuNodeHandle,
    template: *const TuGridTemplate,
) -> i32 {
    guard(|| {
        with_registered_context_mut(ctx(context)?, |c| {
            let t = unsafe { template.as_ref() }.ok_or(NativeError::NullPointer)?;
            let rows = unsafe { slice(t.rows, t.row_count)? }
                .iter()
                .map(|x| unsafe { grid_component(c, x) })
                .collect::<Result<Vec<_>, _>>()?;
            let columns = unsafe { slice(t.columns, t.column_count)? }
                .iter()
                .map(|x| unsafe { grid_component(c, x) })
                .collect::<Result<Vec<_>, _>>()?;
            let auto_rows = unsafe { slice(t.auto_rows, t.auto_row_count)? }
                .iter()
                .map(|x| sizing_track(c, x))
                .collect::<Result<Vec<_>, _>>()?;
            let auto_columns = unsafe { slice(t.auto_columns, t.auto_column_count)? }
                .iter()
                .map(|x| sizing_track(c, x))
                .collect::<Result<Vec<_>, _>>()?;
            let mut r = GridTemplateResource {
                rows,
                columns,
                auto_rows,
                auto_columns,
                ..Default::default()
            };
            for n in unsafe { slice(t.named_lines, t.named_line_count)? } {
                let name = unsafe { string(n.name)? };
                if n.line_index > u16::MAX as u32 || n.name.len == 0 {
                    return Err(NativeError::InvalidValue);
                };
                let idx = n.line_index as usize;
                let target = if n.axis == 0 {
                    &mut r.row_line_names
                } else if n.axis == 1 {
                    &mut r.column_line_names
                } else {
                    return Err(NativeError::InvalidEnum);
                };
                if target.len() <= idx {
                    target.resize_with(idx + 1, Vec::new)
                };
                target[idx].push(name)
            }
            let areas = unsafe { slice(t.areas, t.area_count)? };
            if !areas.is_empty() {
                let mut converted = Vec::new();
                for a in areas {
                    if a.name.len == 0
                        || a.row_start == 0
                        || a.column_start == 0
                        || a.row_end <= a.row_start
                        || a.column_end <= a.column_start
                        || a.row_end > t.area_rows + 1
                        || a.column_end > t.area_columns + 1
                    {
                        return Err(NativeError::InvalidValue);
                    };
                    converted.push((
                        unsafe { string(a.name)? },
                        u16::try_from(a.row_start).map_err(|_| NativeError::InvalidValue)?,
                        u16::try_from(a.row_end).map_err(|_| NativeError::InvalidValue)?,
                        u16::try_from(a.column_start).map_err(|_| NativeError::InvalidValue)?,
                        u16::try_from(a.column_end).map_err(|_| NativeError::InvalidValue)?,
                    ));
                }
                r.areas = Some(template_areas(
                    u16::try_from(t.area_rows).map_err(|_| NativeError::InvalidValue)?,
                    u16::try_from(t.area_columns).map_err(|_| NativeError::InvalidValue)?,
                    converted,
                ));
            }
            c.apply_grid_template(node(node_handle)?, &r)
        })
    })
}
/// Reads detailed Grid summary metadata.
/// # Safety
/// `out_info` must point to writable storage for one `TuGridInfo`.
#[no_mangle]
pub unsafe extern "C" fn tu_get_grid_info(
    context: TuContextHandle,
    node_handle: TuNodeHandle,
    out_info: *mut TuGridInfo,
) -> i32 {
    guard(|| {
        with_registered_context_mut(ctx(context)?, |c| {
            let o = unsafe { out_info.as_mut() }.ok_or(NativeError::NullPointer)?;
            match c.detailed_layout_info(node(node_handle)?)? {
                taffy::tree::DetailedLayoutInfo::Grid(info) => {
                    *o = TuGridInfo {
                        negative_implicit_rows: info.rows.negative_implicit_tracks as u32,
                        explicit_rows: info.rows.explicit_tracks as u32,
                        positive_implicit_rows: info.rows.positive_implicit_tracks as u32,
                        negative_implicit_columns: info.columns.negative_implicit_tracks as u32,
                        explicit_columns: info.columns.explicit_tracks as u32,
                        positive_implicit_columns: info.columns.positive_implicit_tracks as u32,
                        row_track_count: info.rows.sizes.len() as u32,
                        column_track_count: info.columns.sizes.len() as u32,
                        item_count: info.items.len() as u32,
                    };
                    Ok(())
                }
                _ => Err(NativeError::InvalidValue),
            }
        })
    })
}
/// Copies detailed Grid track sizes for row (`0`) or column (`1`).
/// # Safety
/// `sizes` must reference `capacity` writable floats when required, and `out_written` must be writable.
#[no_mangle]
pub unsafe extern "C" fn tu_get_grid_track_sizes(
    context: TuContextHandle,
    node_handle: TuNodeHandle,
    axis: i32,
    sizes: *mut f32,
    capacity: u32,
    out_written: *mut u32,
) -> i32 {
    guard(|| {
        with_registered_context_mut(ctx(context)?, |c| {
            let w = unsafe { out_written.as_mut() }.ok_or(NativeError::NullPointer)?;
            let tracks = match c.detailed_layout_info(node(node_handle)?)? {
                taffy::tree::DetailedLayoutInfo::Grid(info) => match axis {
                    0 => &info.rows.sizes,
                    1 => &info.columns.sizes,
                    _ => return Err(NativeError::InvalidEnum),
                },
                _ => return Err(NativeError::InvalidValue),
            };
            if capacity < tracks.len() as u32 {
                return Err(NativeError::InvalidCount);
            };
            if !tracks.is_empty() && sizes.is_null() {
                return Err(NativeError::NullPointer);
            };
            for (i, v) in tracks.iter().enumerate() {
                unsafe { *sizes.add(i) = *v }
            }
            *w = tracks.len() as u32;
            Ok(())
        })
    })
}
/// Copies detailed Grid item placements.
/// # Safety
/// `items` must reference `capacity` writable entries when required, and `out_written` must be writable.
#[no_mangle]
pub unsafe extern "C" fn tu_get_grid_items(
    context: TuContextHandle,
    node_handle: TuNodeHandle,
    items: *mut TuGridItemInfo,
    capacity: u32,
    out_written: *mut u32,
) -> i32 {
    guard(|| {
        with_registered_context_mut(ctx(context)?, |c| {
            let w = unsafe { out_written.as_mut() }.ok_or(NativeError::NullPointer)?;
            let src = match c.detailed_layout_info(node(node_handle)?)? {
                taffy::tree::DetailedLayoutInfo::Grid(info) => &info.items,
                _ => return Err(NativeError::InvalidValue),
            };
            if capacity < src.len() as u32 {
                return Err(NativeError::InvalidCount);
            };
            if !src.is_empty() && items.is_null() {
                return Err(NativeError::NullPointer);
            };
            for (i, v) in src.iter().enumerate() {
                unsafe {
                    *items.add(i) = TuGridItemInfo {
                        row_start: v.row_start as u32,
                        row_end: v.row_end as u32,
                        column_start: v.column_start as u32,
                        column_end: v.column_end as u32,
                    }
                }
            }
            *w = src.len() as u32;
            Ok(())
        })
    })
}
/// Copies detailed Grid gutter sizes for row (`0`) or column (`1`).
/// # Safety
/// `gutters` must reference `capacity` writable floats when required, and `out_written` must be writable.
#[no_mangle]
pub unsafe extern "C" fn tu_get_grid_gutters(
    context: TuContextHandle,
    node_handle: TuNodeHandle,
    axis: i32,
    gutters: *mut f32,
    capacity: u32,
    out_written: *mut u32,
) -> i32 {
    guard(|| {
        with_registered_context_mut(ctx(context)?, |c| {
            let w = unsafe { out_written.as_mut() }.ok_or(NativeError::NullPointer)?;
            let values = match c.detailed_layout_info(node(node_handle)?)? {
                taffy::tree::DetailedLayoutInfo::Grid(info) => match axis {
                    0 => &info.rows.gutters,
                    1 => &info.columns.gutters,
                    _ => return Err(NativeError::InvalidEnum),
                },
                _ => return Err(NativeError::InvalidValue),
            };
            if capacity < values.len() as u32 {
                return Err(NativeError::InvalidCount);
            };
            if !values.is_empty() && gutters.is_null() {
                return Err(NativeError::NullPointer);
            };
            for (i, v) in values.iter().enumerate() {
                unsafe { *gutters.add(i) = *v }
            }
            *w = values.len() as u32;
            Ok(())
        })
    })
}

#[no_mangle]
pub extern "C" fn tu_compute_layout(
    context: TuContextHandle,
    root: TuNodeHandle,
    width: f32,
    height: f32,
) -> i32 {
    guard(|| {
        if width.is_nan()
            || height.is_nan()
            || width < 0.0
            || height < 0.0
            || width == f32::NEG_INFINITY
            || height == f32::NEG_INFINITY
        {
            return Err(NativeError::InvalidNumber);
        };
        with_registered_context_mut(ctx(context)?, |c| {
            c.compute_layout(node(root)?, width, height)
        })
    })
}
/// Reads one computed layout.
/// # Safety
/// `out_layout` must point to writable storage for one `TuLayout`.
#[no_mangle]
pub unsafe extern "C" fn tu_get_layout(
    context: TuContextHandle,
    node_handle: TuNodeHandle,
    out_layout: *mut TuLayout,
) -> i32 {
    guard(|| {
        with_registered_context_mut(ctx(context)?, |c| {
            let v = c
                .layouts_bulk(&[node(node_handle)?])?
                .pop()
                .ok_or(NativeError::NodeNotFound)?;
            let o = unsafe { out_layout.as_mut() }.ok_or(NativeError::NullPointer)?;
            *o = layout(v);
            Ok(())
        })
    })
}
fn layout(v: LayoutResult) -> TuLayout {
    TuLayout {
        node: v.node.raw(),
        order: v.order,
        x: v.x,
        y: v.y,
        width: v.width,
        height: v.height,
        content_width: v.content_width,
        content_height: v.content_height,
        scroll_width: v.scroll_width,
        scroll_height: v.scroll_height,
    }
}
/// Bulk layout retrieval.
/// # Safety
/// `handles` must reference `count` entries, `output` must reference `capacity` writable entries, and `out_written` must be writable.
#[no_mangle]
pub unsafe extern "C" fn tu_get_layouts_bulk(
    context: TuContextHandle,
    handles: *const TuNodeHandle,
    count: u32,
    output: *mut TuLayout,
    capacity: u32,
    out_written: *mut u32,
) -> i32 {
    guard(|| {
        with_registered_context_mut(ctx(context)?, |c| {
            let hs = unsafe { slice(handles, count)? }
                .iter()
                .map(|x| node(*x))
                .collect::<Result<Vec<_>, _>>()?;
            if capacity < count {
                return Err(NativeError::InvalidCount);
            };
            if count > 0 && output.is_null() {
                return Err(NativeError::NullPointer);
            };
            let values = c.layouts_bulk(&hs)?;
            for (i, v) in values.into_iter().enumerate() {
                unsafe { *output.add(i) = layout(v) }
            }
            let w = unsafe { out_written.as_mut() }.ok_or(NativeError::NullPointer)?;
            *w = count;
            Ok(())
        })
    })
}

#[cfg(test)]
mod tests {
    use super::*;
    const AUTO_VALUE: TuValue = TuValue {
        kind: TuValueKind::Auto as i32,
        value: 0.0,
        resource: 0,
    };
    const AUTO_PLACEMENT: TuGridPlacement = TuGridPlacement {
        kind: TuGridPlacementKind::Auto as i32,
        line: 0,
        span: 0,
        occurrence: 0,
        name: TuStringView {
            data: std::ptr::null(),
            len: 0,
        },
    };
    fn test_style() -> TuStyle {
        TuStyle {
            display: TuDisplay::Flex as i32,
            box_sizing: TuBoxSizing::BorderBox as i32,
            direction: TuDirection::Ltr as i32,
            overflow_x: TuOverflow::Visible as i32,
            overflow_y: TuOverflow::Visible as i32,
            scrollbar_width: 0.0,
            position: TuPosition::Relative as i32,
            inset_left: AUTO_VALUE,
            inset_right: AUTO_VALUE,
            inset_top: AUTO_VALUE,
            inset_bottom: AUTO_VALUE,
            width: AUTO_VALUE,
            height: AUTO_VALUE,
            min_width: AUTO_VALUE,
            min_height: AUTO_VALUE,
            max_width: AUTO_VALUE,
            max_height: AUTO_VALUE,
            aspect_ratio: 0.0,
            margin_left: AUTO_VALUE,
            margin_right: AUTO_VALUE,
            margin_top: AUTO_VALUE,
            margin_bottom: AUTO_VALUE,
            padding_left: AUTO_VALUE,
            padding_right: AUTO_VALUE,
            padding_top: AUTO_VALUE,
            padding_bottom: AUTO_VALUE,
            border_left: AUTO_VALUE,
            border_right: AUTO_VALUE,
            border_top: AUTO_VALUE,
            border_bottom: AUTO_VALUE,
            flex_direction: TuFlexDirection::Row as i32,
            flex_wrap: TuFlexWrap::NoWrap as i32,
            flex_basis: AUTO_VALUE,
            flex_grow: 0.0,
            flex_shrink: 1.0,
            align_items: TuAlign::Unset as i32,
            align_self: TuAlign::Unset as i32,
            align_content: TuAlignContent::Unset as i32,
            justify_content: TuAlignContent::Unset as i32,
            justify_items: TuAlign::Unset as i32,
            justify_self: TuAlign::Unset as i32,
            gap_x: AUTO_VALUE,
            gap_y: AUTO_VALUE,
            item_is_table: 0,
            item_is_replaced: 0,
            float_mode: TuFloatMode::None as i32,
            clear_mode: TuClearMode::None as i32,
            text_align: TuTextAlign::Auto as i32,
            grid_auto_flow: TuGridAutoFlow::Row as i32,
            grid_row_start: AUTO_PLACEMENT,
            grid_row_end: AUTO_PLACEMENT,
            grid_column_start: AUTO_PLACEMENT,
            grid_column_end: AUTO_PLACEMENT,
        }
    }
    #[test]
    fn version_and_capability_queries_are_stable() {
        assert_eq!(tu_get_abi_version(), TU_ABI_VERSION);
        assert_eq!(tu_get_abi_stage(), TU_ABI_STAGE);
        assert_eq!(tu_get_capabilities(), TU_CAPABILITIES);
        assert_ne!(tu_get_taffy_version_packed(), 0);
    }
    #[test]
    fn context_node_lifecycle_rejects_stale_node_handles() {
        let mut context = 0;
        assert_eq!(
            unsafe { tu_context_create(&mut context) },
            TuStatus::Ok as i32
        );
        let style = test_style();
        let mut node = 0;
        assert_eq!(
            unsafe { tu_node_create(context, &style, &mut node) },
            TuStatus::Ok as i32
        );
        assert_eq!(tu_node_remove(context, node), TuStatus::Ok as i32);
        assert_eq!(
            tu_node_mark_dirty(context, node),
            TuStatus::InvalidNode as i32
        );
        assert_eq!(tu_context_destroy(context), TuStatus::Ok as i32);
    }
    #[test]
    fn invalid_style_enum_is_reported_without_panicking() {
        let mut context = 0;
        assert_eq!(
            unsafe { tu_context_create(&mut context) },
            TuStatus::Ok as i32
        );
        let mut style = test_style();
        style.display = i32::MAX;
        let mut node = 0;
        assert_eq!(
            unsafe { tu_node_create(context, &style, &mut node) },
            TuStatus::InvalidEnum as i32
        );
        assert!(tu_get_last_error_length() > 0);
        assert_eq!(tu_context_destroy(context), TuStatus::Ok as i32);
    }
    #[test]
    fn wrong_thread_context_use_has_explicit_status() {
        let mut context = 0;
        assert_eq!(
            unsafe { tu_context_create(&mut context) },
            TuStatus::Ok as i32
        );
        let status = std::thread::spawn(move || tu_context_clear(context))
            .join()
            .unwrap();
        assert_eq!(status, TuStatus::WrongThread as i32);
        assert_eq!(tu_context_destroy(context), TuStatus::Ok as i32);
    }
    #[test]
    fn calc_operands_must_be_live_in_the_owning_context() {
        let mut context = 0;
        assert_eq!(
            unsafe { tu_context_create(&mut context) },
            TuStatus::Ok as i32
        );
        let length_spec = TuCalcSpec {
            op: TuCalcOp::Length as i32,
            value: 10.0,
            operands: std::ptr::null(),
            operand_count: 0,
        };
        let mut first = 0;
        assert_eq!(
            unsafe { tu_calc_create(context, &length_spec, &mut first) },
            TuStatus::Ok as i32
        );
        assert_eq!(tu_calc_remove(context, first), TuStatus::Ok as i32);
        let operands = [first];
        let scale_spec = TuCalcSpec {
            op: TuCalcOp::Scale as i32,
            value: 2.0,
            operands: operands.as_ptr(),
            operand_count: 1,
        };
        let mut output = 0;
        assert_eq!(
            unsafe { tu_calc_create(context, &scale_spec, &mut output) },
            TuStatus::InvalidResource as i32
        );
        assert_eq!(tu_context_destroy(context), TuStatus::Ok as i32);
    }
    #[test]
    fn bulk_layout_result_path_round_trips_fixed_width_handles() {
        let mut context = 0;
        assert_eq!(
            unsafe { tu_context_create(&mut context) },
            TuStatus::Ok as i32
        );
        let mut root_style = test_style();
        root_style.width = TuValue {
            kind: TuValueKind::Length as i32,
            value: 100.0,
            resource: 0,
        };
        root_style.height = TuValue {
            kind: TuValueKind::Length as i32,
            value: 40.0,
            resource: 0,
        };
        let mut root = 0;
        assert_eq!(
            unsafe { tu_node_create(context, &root_style, &mut root) },
            TuStatus::Ok as i32
        );
        assert_eq!(
            tu_compute_layout(context, root, 100.0, 40.0),
            TuStatus::Ok as i32
        );
        let handles = [root];
        let mut output = [TuLayout {
            node: 0,
            order: 0,
            x: 0.0,
            y: 0.0,
            width: 0.0,
            height: 0.0,
            content_width: 0.0,
            content_height: 0.0,
            scroll_width: 0.0,
            scroll_height: 0.0,
        }];
        let mut written = 0;
        assert_eq!(
            unsafe {
                tu_get_layouts_bulk(
                    context,
                    handles.as_ptr(),
                    1,
                    output.as_mut_ptr(),
                    1,
                    &mut written,
                )
            },
            TuStatus::Ok as i32
        );
        assert_eq!(written, 1);
        assert_eq!(output[0].node, root);
        assert_eq!(output[0].width, 100.0);
        assert_eq!(output[0].height, 40.0);
        assert_eq!(tu_context_destroy(context), TuStatus::Ok as i32);
    }
}
