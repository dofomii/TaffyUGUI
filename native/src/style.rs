//! Native style regression tests for the complete Taffy 0.13 style surface.

#[cfg(test)]
mod phase1_style_tests {
    use taffy::geometry::{Point, Rect, Size};
    use taffy::prelude::*;
    use taffy::style::{BoxSizing, Clear, Direction, Float, Overflow, Position};
    use taffy::style_helpers::{auto, length, percent};

    #[test]
    fn core_box_model_position_overflow_and_sizing_map_directly_to_taffy() {
        let style = Style {
            display: Display::Block,
            box_sizing: BoxSizing::ContentBox,
            direction: Direction::Rtl,
            overflow: Point { x: Overflow::Hidden, y: Overflow::Scroll },
            scrollbar_width: 12.0,
            position: Position::Absolute,
            inset: Rect {
                left: length(5.0),
                right: auto(),
                top: percent(0.1),
                bottom: auto(),
            },
            size: Size { width: percent(0.5), height: length(80.0) },
            min_size: Size { width: length(20.0), height: auto() },
            max_size: Size { width: length(500.0), height: auto() },
            aspect_ratio: Some(16.0 / 9.0),
            margin: Rect { left: auto(), right: length(2.0), top: length(3.0), bottom: length(4.0) },
            padding: Rect { left: length(1.0), right: percent(0.02), top: length(3.0), bottom: length(4.0) },
            border: Rect { left: length(1.0), right: length(1.0), top: length(1.0), bottom: length(1.0) },
            ..Default::default()
        };
        assert_eq!(style.display, Display::Block);
        assert_eq!(style.box_sizing, BoxSizing::ContentBox);
        assert_eq!(style.direction, Direction::Rtl);
        assert_eq!(style.overflow.y, Overflow::Scroll);
        assert_eq!(style.scrollbar_width, 12.0);
    }

    #[test]
    fn complete_flex_alignment_surface_is_representable() {
        let style = Style {
            display: Display::Flex,
            flex_direction: FlexDirection::ColumnReverse,
            flex_wrap: FlexWrap::WrapReverse,
            flex_basis: percent(0.25),
            flex_grow: 2.0,
            flex_shrink: 0.5,
            align_items: Some(AlignItems::CENTER),
            align_self: Some(AlignSelf::END),
            align_content: Some(AlignContent::SPACE_BETWEEN),
            justify_content: Some(JustifyContent::SPACE_EVENLY),
            gap: Size { width: length(8.0), height: percent(0.05) },
            ..Default::default()
        };
        assert_eq!(style.flex_direction, FlexDirection::ColumnReverse);
        assert_eq!(style.flex_wrap, FlexWrap::WrapReverse);
        assert_eq!(style.flex_grow, 2.0);
        assert_eq!(style.align_self, Some(AlignSelf::END));
    }

    #[test]
    fn block_flow_root_float_and_clear_surface_is_representable() {
        let block = Style { display: Display::Block, float: Float::Left, clear: Clear::Right, ..Default::default() };
        let flow_root = Style { display: Display::FlowRoot, ..Default::default() };
        assert_eq!(block.float, Float::Left);
        assert_eq!(block.clear, Clear::Right);
        assert_eq!(flow_root.display, Display::FlowRoot);
    }

    #[test]
    fn display_none_surface_is_representable() {
        assert_eq!(Style { display: Display::None, ..Default::default() }.display, Display::None);
    }
}
