using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace TaffyUGUI.Editor
{
    internal static class TaffyLayoutExplanation
    {
        internal static string Build(Component component, TaffyComputedLayoutSnapshot snapshot)
        {
            if (!component || !snapshot.Available)
                return "Current layout data is unavailable.";

            var lines = new List<string>
            {
                "Current result: " + Format(snapshot.Size.x) + " × " + Format(snapshot.Size.y) +
                " at (" + Format(snapshot.Position.x) + ", " + Format(snapshot.Position.y) + ")."
            };

            TaffyLayoutGroup group = component as TaffyLayoutGroup;
            TaffyLayoutItem item = component as TaffyLayoutItem;
            if (item)
                ExplainItem(item, snapshot, lines);
            else if (group)
                ExplainGroup(group, snapshot, lines);

            if (!string.IsNullOrEmpty(snapshot.GridDiagnostics))
                lines.Add("Grid diagnostics: " + snapshot.GridDiagnostics + ".");

            return string.Join("\n\n", lines);
        }

        private static void ExplainItem(TaffyLayoutItem item, TaffyComputedLayoutSnapshot snapshot, List<string> lines)
        {
            lines.Add("Width: " + DescribeLength(item.width, "the containing width") + ". Height: " +
                      DescribeLength(item.height, "the containing height") + ".");

            if (item.width.unit == TaffyUnit.Auto || item.height.unit == TaffyUnit.Auto)
            {
                if (snapshot.MeasurementAvailable)
                {
                    lines.Add("Content measurement is available. Preferred intrinsic content is " +
                              Format(snapshot.Measurement.preferred.x) + " × " + Format(snapshot.Measurement.preferred.y) +
                              " when measured with " + Format(snapshot.MeasurementWidth) + " units of available width. Auto sizing may use this intrinsic contribution together with parent layout constraints.");
                }
                else
                {
                    lines.Add("At least one size axis is Auto. The exact content/intrinsic contribution is not exposed for this selection, so the current size is shown without claiming a specific internal sizing reason.");
                }
            }

            string padding = DescribeEdges(item.padding);
            if (!string.IsNullOrEmpty(padding))
                lines.Add("Item padding contributes inside the box: " + padding + ".");

            TaffyLayoutGroup parent = FindParentGroup(item.transform);
            if (parent)
            {
                ExplainResponsive(parent, lines);
                if (snapshot.EffectiveDisplay == TaffyContainerDisplay.Flex && item.flexGrow > 0f)
                {
                    lines.Add("Flex Grow is " + Format(item.flexGrow) +
                              ". This item participates in sharing remaining main-axis space relative to sibling grow factors. The snapshot does not expose the native solver's exact per-item allocation, so no exact grow contribution is claimed.");
                }

                if (snapshot.EffectiveDisplay == TaffyContainerDisplay.Grid)
                    ExplainGridItem(item, parent, lines);
            }
        }

        private static void ExplainGroup(TaffyLayoutGroup group, TaffyComputedLayoutSnapshot snapshot, List<string> lines)
        {
            RectOffset padding = group.padding;
            if (padding != null && (padding.left != 0 || padding.right != 0 || padding.top != 0 || padding.bottom != 0))
            {
                lines.Add("Container padding reduces the space available to children: L " + padding.left +
                          " • R " + padding.right + " • T " + padding.top + " • B " + padding.bottom + ".");
            }

            ExplainResponsive(group, lines);

            if (snapshot.EffectiveDisplay == TaffyContainerDisplay.Grid)
            {
                lines.Add("Grid configuration has " + group.gridColumns.Count + " explicit column track(s) and " +
                          group.gridRows.Count + " explicit row track(s). Exact resolved track boundaries are reported only when deterministic data is available; this explanation does not infer hidden native solver decisions.");
            }
        }

        private static void ExplainResponsive(TaffyLayoutGroup group, List<string> lines)
        {
            if (!group || string.IsNullOrEmpty(group.ActiveResponsiveProfileName))
            {
                lines.Add("Responsive profile: Base settings are active.");
                return;
            }

            TaffyResponsiveProfile profile = FindActiveProfile(group);
            if (profile == null)
            {
                lines.Add("Responsive profile: " + group.ActiveResponsiveProfileName + " is active, but its serialized override record could not be resolved safely.");
                return;
            }

            var overrides = new List<string>();
            if (profile.overrideContainerDisplay) overrides.Add("display");
            if (profile.overrideFlexDirection) overrides.Add("direction");
            if (profile.overrideFlexWrap) overrides.Add("wrap");
            if (profile.overrideGaps) overrides.Add("gaps");
            if (profile.overrideAlignment) overrides.Add("alignment");
            if (profile.overrideGridAutoFlow) overrides.Add("Grid flow");
            if (profile.overridePadding) overrides.Add("padding");

            lines.Add("Responsive profile: " + profile.name + " is active" +
                      (overrides.Count == 0 ? " and does not override tracked container fields." :
                       "; active overrides: " + string.Join(", ", overrides) + "."));
        }

        private static void ExplainGridItem(TaffyLayoutItem item, TaffyLayoutGroup parent, List<string> lines)
        {
            string row = DescribePlacement(item.gridRowStart, item.gridRowEnd);
            string column = DescribePlacement(item.gridColumnStart, item.gridColumnEnd);
            lines.Add("Grid placement request: rows " + row + "; columns " + column + ". Parent has " +
                      parent.gridRows.Count + " explicit row track(s) and " + parent.gridColumns.Count +
                      " explicit column track(s). Auto-placement and final track geometry may depend on siblings and native layout state, so unresolved details are not guessed.");
        }

        private static string DescribeLength(TaffyLength length, string percentBasis)
        {
            switch (length.unit)
            {
                case TaffyUnit.Points:
                    return "fixed at " + Format(length.value) + " layout units before Canvas scaling";
                case TaffyUnit.Percent:
                    return "requests " + Format(length.value * 100f) + "% of " + percentBasis;
                case TaffyUnit.Calc:
                    return "uses a Calc expression; the current result may combine multiple terms";
                default:
                    return "Auto; parent layout and intrinsic/content information can contribute";
            }
        }

        private static string DescribeEdges(TaffyEdges edges)
        {
            string left = DescribeEdge(edges.left);
            string right = DescribeEdge(edges.right);
            string top = DescribeEdge(edges.top);
            string bottom = DescribeEdge(edges.bottom);
            if (left == "0" && right == "0" && top == "0" && bottom == "0")
                return string.Empty;
            if (left == right && left == top && left == bottom)
                return left + " on all sides";
            return "L " + left + " • R " + right + " • T " + top + " • B " + bottom;
        }

        private static string DescribeEdge(TaffyLength length)
        {
            switch (length.unit)
            {
                case TaffyUnit.Points: return Format(length.value);
                case TaffyUnit.Percent: return Format(length.value * 100f) + "%";
                case TaffyUnit.Calc: return "Calc";
                default: return "Auto";
            }
        }

        private static string DescribePlacement(TaffyGridPlacement start, TaffyGridPlacement end)
        {
            return DescribePlacementValue(start) + " → " + DescribePlacementValue(end);
        }

        private static string DescribePlacementValue(TaffyGridPlacement placement)
        {
            switch (placement.kind)
            {
                case TaffyGridPlacementKind.Line: return "line " + placement.line;
                case TaffyGridPlacementKind.Span: return "span " + placement.span;
                case TaffyGridPlacementKind.NamedLine: return "named line " + placement.name;
                case TaffyGridPlacementKind.NamedSpan: return "named span " + placement.name + " × " + placement.span;
                default: return "Auto";
            }
        }

        private static TaffyResponsiveProfile FindActiveProfile(TaffyLayoutGroup group)
        {
            if (!group || group.responsiveProfiles == null || string.IsNullOrEmpty(group.ActiveResponsiveProfileName))
                return null;
            for (int i = 0; i < group.responsiveProfiles.Count; i++)
            {
                TaffyResponsiveProfile profile = group.responsiveProfiles[i];
                if (profile != null && string.Equals(profile.name, group.ActiveResponsiveProfileName, StringComparison.Ordinal))
                    return profile;
            }
            return null;
        }

        private static TaffyLayoutGroup FindParentGroup(Transform transform)
        {
            Transform current = transform ? transform.parent : null;
            while (current)
            {
                TaffyLayoutGroup group = current.GetComponent<TaffyLayoutGroup>();
                if (group)
                    return group;
                current = current.parent;
            }
            return null;
        }

        private static string Format(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
