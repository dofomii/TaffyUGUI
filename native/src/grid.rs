//! Grid-specific native authoring and conversion for Taffy 0.13.

#![allow(dead_code)] // Phase 1 authoring helpers are retained for native regression tests; Phase 2 converts ABI data directly.
use taffy::geometry::Line;
use taffy::prelude::*;
use taffy::style::{
    GridPlacement, GridTemplateArea, GridTemplateAreas, GridTemplateComponent,
    MaxTrackSizingFunction, MinTrackSizingFunction, TrackSizingFunction,
};
use taffy::style_helpers::{auto, flex, length, minmax, percent, repeat};

#[derive(Debug, Clone, Default, PartialEq)]
pub(crate) struct GridTemplateResource {
    pub rows: Vec<GridTemplateComponent<String>>,
    pub columns: Vec<GridTemplateComponent<String>>,
    pub auto_rows: Vec<TrackSizingFunction>,
    pub auto_columns: Vec<TrackSizingFunction>,
    pub row_line_names: Vec<Vec<String>>,
    pub column_line_names: Vec<Vec<String>>,
    pub areas: Option<GridTemplateAreas<String>>,
}

impl GridTemplateResource {
    pub(crate) fn apply_to(&self, style: &mut Style) {
        style.grid_template_rows = self.rows.clone();
        style.grid_template_columns = self.columns.clone();
        style.grid_auto_rows = self.auto_rows.clone();
        style.grid_auto_columns = self.auto_columns.clone();
        style.grid_template_row_names = self.row_line_names.clone();
        style.grid_template_column_names = self.column_line_names.clone();
        style.grid_template_areas = self.areas.clone();
    }
}

pub(crate) fn fixed_track(value: f32) -> GridTemplateComponent<String> { length(value.max(0.0)) }
pub(crate) fn percent_track(value: f32) -> GridTemplateComponent<String> { percent(value) }
pub(crate) fn auto_track() -> GridTemplateComponent<String> { auto() }
pub(crate) fn fraction_track(value: f32) -> GridTemplateComponent<String> { flex(value.max(0.0)) }
pub(crate) fn minmax_track(min: MinTrackSizingFunction, max: MaxTrackSizingFunction) -> GridTemplateComponent<String> { minmax(min, max) }
pub(crate) fn repeat_tracks(count: u16, tracks: Vec<TrackSizingFunction>) -> GridTemplateComponent<String> { repeat(count, tracks) }
pub(crate) fn fixed_auto_track(value: f32) -> TrackSizingFunction { length(value.max(0.0)) }
pub(crate) fn percent_auto_track(value: f32) -> TrackSizingFunction { percent(value) }
pub(crate) fn fraction_auto_track(value: f32) -> TrackSizingFunction { flex(value.max(0.0)) }
pub(crate) fn automatic_auto_track() -> TrackSizingFunction { auto() }
pub(crate) fn named_line(name: impl Into<String>, occurrence: i16) -> GridPlacement<String> { GridPlacement::NamedLine(name.into(), occurrence) }
pub(crate) fn named_span(name: impl Into<String>, count: u16) -> GridPlacement<String> { GridPlacement::NamedSpan(name.into(), count) }
pub(crate) fn placement(start: GridPlacement<String>, end: GridPlacement<String>) -> Line<GridPlacement<String>> { Line { start, end } }

pub(crate) fn template_areas(
    row_count: u16,
    column_count: u16,
    areas: impl IntoIterator<Item = (String, u16, u16, u16, u16)>,
) -> GridTemplateAreas<String> {
    GridTemplateAreas {
        row_count,
        column_count,
        areas: areas.into_iter().map(|(name, row_start, row_end, column_start, column_end)| GridTemplateArea {
            name, row_start, row_end, column_start, column_end,
        }).collect(),
    }
}

#[cfg(test)]
mod tests {
    use taffy::prelude::*;
    use taffy::style::GridPlacement;
    use taffy::style_helpers::{auto, line, span};
    use super::{auto_track, automatic_auto_track, fixed_auto_track, fixed_track, fraction_auto_track,
        fraction_track, minmax_track, named_line, named_span, percent_auto_track, percent_track,
        placement, repeat_tracks, template_areas, GridTemplateResource};

    #[test]
    fn grid_resource_applies_named_lines_areas_and_tracks() {
        let resource = GridTemplateResource {
            columns: vec![fraction_track(1.0), fraction_track(2.0)],
            rows: vec![auto()],
            column_line_names: vec![vec!["left".into()], vec!["middle".into()], vec!["right".into()]],
            row_line_names: vec![vec!["top".into()], vec!["bottom".into()]],
            areas: Some(template_areas(1, 2, [("main".into(), 1, 2, 1, 3)])),
            ..Default::default()
        };
        let mut style = Style { display: Display::Grid, ..Default::default() };
        resource.apply_to(&mut style);
        assert_eq!(style.grid_template_columns.len(), 2);
        assert_eq!(style.grid_template_column_names[0][0], "left");
        assert_eq!(style.grid_template_areas.as_ref().unwrap().areas[0].name, "main");
    }

    #[test]
    fn grid_placement_supports_numeric_spans_and_named_lines() {
        let numeric = placement(line(2), span(3));
        assert!(matches!(numeric.start, GridPlacement::Line(_)));
        assert!(matches!(numeric.end, GridPlacement::Span(3)));
        let named = placement(named_line("content", 1), named_span("content", 1));
        assert!(matches!(named.start, GridPlacement::NamedLine(_, 1)));
        assert!(matches!(named.end, GridPlacement::NamedSpan(_, 1)));
    }

    #[test]
    fn all_grid_track_helpers_construct_supported_track_forms() {
        let fixed = fixed_track(10.0);
        let pct = percent_track(0.5);
        let automatic = auto_track();
        let fractional = fraction_track(2.0);
        let minmaxed = minmax_track(auto(), taffy::style_helpers::fr(1.0));
        let repeated = repeat_tracks(2, vec![fixed_auto_track(8.0), percent_auto_track(0.25)]);
        let implicit = [automatic_auto_track(), fraction_auto_track(1.0)];
        assert!(matches!(fixed, GridTemplateComponent::Single(_)));
        assert!(matches!(pct, GridTemplateComponent::Single(_)));
        assert!(matches!(automatic, GridTemplateComponent::Single(_)));
        assert!(matches!(fractional, GridTemplateComponent::Single(_)));
        assert!(matches!(minmaxed, GridTemplateComponent::Single(_)));
        assert!(matches!(repeated, GridTemplateComponent::Repeat(_)));
        assert_eq!(implicit.len(), 2);
    }
}
