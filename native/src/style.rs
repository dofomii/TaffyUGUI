//! C-compatible style types and conversion into Taffy 0.13 styles.

use taffy::prelude::*;

#[repr(C)]
#[derive(Clone, Copy, Default)]
pub struct TaffyUGUIDimension {
    /// 0 = auto, 1 = points, 2 = percent (0..1)
    pub unit: i32,
    pub value: f32,
}

#[repr(C)]
#[derive(Clone, Copy, Default)]
pub struct TaffyUGUIStyle {
    pub flex_direction: i32,
    pub flex_wrap: i32,
    pub width: TaffyUGUIDimension,
    pub height: TaffyUGUIDimension,
    pub min_width: TaffyUGUIDimension,
    pub min_height: TaffyUGUIDimension,
    pub max_width: TaffyUGUIDimension,
    pub max_height: TaffyUGUIDimension,
    pub flex_basis: TaffyUGUIDimension,
    pub flex_grow: f32,
    pub flex_shrink: f32,
    pub gap_x: f32,
    pub gap_y: f32,
    pub padding_left: f32,
    pub padding_right: f32,
    pub padding_top: f32,
    pub padding_bottom: f32,
    /// -1 = inherit/default, 0=start, 1=end, 2=center, 3=stretch
    pub align_items: i32,
    /// -1 = inherit/default, 0=start, 1=end, 2=center, 3=stretch
    pub align_self: i32,
    /// -1 = default, 0=start, 1=end, 2=center, 3=space-between, 4=space-around, 5=space-evenly
    pub justify_content: i32,
    pub aspect_ratio: f32,
}

pub(crate) fn to_taffy_style(style: TaffyUGUIStyle) -> Style {
    Style {
        display: Display::Flex,
        flex_direction: match style.flex_direction {
            1 => FlexDirection::Column,
            2 => FlexDirection::RowReverse,
            3 => FlexDirection::ColumnReverse,
            _ => FlexDirection::Row,
        },
        flex_wrap: match style.flex_wrap {
            1 => FlexWrap::Wrap,
            2 => FlexWrap::WrapReverse,
            _ => FlexWrap::NoWrap,
        },
        size: Size {
            width: dimension(style.width),
            height: dimension(style.height),
        },
        min_size: Size {
            width: dimension(style.min_width),
            height: dimension(style.min_height),
        },
        max_size: Size {
            width: dimension(style.max_width),
            height: dimension(style.max_height),
        },
        flex_basis: dimension(style.flex_basis),
        flex_grow: style.flex_grow.max(0.0),
        flex_shrink: style.flex_shrink.max(0.0),
        gap: Size {
            width: LengthPercentage::length(style.gap_x.max(0.0)),
            height: LengthPercentage::length(style.gap_y.max(0.0)),
        },
        padding: Rect {
            left: LengthPercentage::length(style.padding_left.max(0.0)),
            right: LengthPercentage::length(style.padding_right.max(0.0)),
            top: LengthPercentage::length(style.padding_top.max(0.0)),
            bottom: LengthPercentage::length(style.padding_bottom.max(0.0)),
        },
        align_items: align_items(style.align_items),
        align_self: align_self(style.align_self),
        justify_content: justify_content(style.justify_content),
        aspect_ratio: if style.aspect_ratio > 0.0 {
            Some(style.aspect_ratio)
        } else {
            None
        },
        ..Default::default()
    }
}

fn dimension(value: TaffyUGUIDimension) -> Dimension {
    match value.unit {
        1 => Dimension::length(value.value.max(0.0)),
        2 => Dimension::percent(value.value),
        _ => Dimension::auto(),
    }
}

fn align_items(value: i32) -> Option<AlignItems> {
    match value {
        0 => Some(AlignItems::START),
        1 => Some(AlignItems::END),
        2 => Some(AlignItems::CENTER),
        3 => Some(AlignItems::STRETCH),
        _ => None,
    }
}

fn align_self(value: i32) -> Option<AlignSelf> {
    match value {
        0 => Some(AlignSelf::START),
        1 => Some(AlignSelf::END),
        2 => Some(AlignSelf::CENTER),
        3 => Some(AlignSelf::STRETCH),
        _ => None,
    }
}

fn justify_content(value: i32) -> Option<JustifyContent> {
    match value {
        0 => Some(JustifyContent::START),
        1 => Some(JustifyContent::END),
        2 => Some(JustifyContent::CENTER),
        3 => Some(JustifyContent::SPACE_BETWEEN),
        4 => Some(JustifyContent::SPACE_AROUND),
        5 => Some(JustifyContent::SPACE_EVENLY),
        _ => None,
    }
}
