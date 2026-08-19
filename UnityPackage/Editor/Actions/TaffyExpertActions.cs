using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Editor
{
    internal static class TaffySectionResetActions
    {
        internal static bool CanReset(string inspectorKey, string sectionKey)
        {
            return !string.IsNullOrEmpty(inspectorKey) && !string.IsNullOrEmpty(sectionKey)
                && (inspectorKey == "Group" || inspectorKey == "Item")
                && sectionKey != "QuickSetup" && sectionKey != "ParentSummary" && sectionKey != "Essentials"
                && sectionKey != "Diagnostics" && sectionKey != "Post";
        }

        internal static void Reset(IEnumerable<Object> targets, string inspectorKey, string sectionKey)
        {
            if (targets == null || !CanReset(inspectorKey, sectionKey))
                return;
            foreach (Object target in targets)
            {
                if (target is TaffyLayoutGroup group && inspectorKey == "Group")
                    ResetGroup(group, sectionKey);
                else if (target is TaffyLayoutItem item && inspectorKey == "Item")
                    ResetItem(item, sectionKey);
            }
        }

        private static void ResetGroup(TaffyLayoutGroup group, string sectionKey)
        {
            if (!group)
                return;
            Undo.RecordObject(group, "Reset TaffyUGUI " + sectionKey);
            switch (sectionKey)
            {
                case "Formatting":
                    group.containerDisplay = TaffyContainerDisplay.Flex;
                    group.boxSizing = TaffyBoxSizing.BorderBox;
                    group.writingDirection = TaffyWritingDirection.LeftToRight;
                    group.overflowX = TaffyOverflow.Visible;
                    group.overflowY = TaffyOverflow.Visible;
                    group.scrollbarWidth = 0f;
                    group.padding = new RectOffset();
                    group.border = default;
                    group.textAlign = TaffyTextAlign.Auto;
                    break;
                case "Flex":
                    group.direction = TaffyFlexDirection.Row;
                    group.wrap = TaffyFlexWrap.NoWrap;
                    group.horizontalGap = 0f;
                    group.verticalGap = 0f;
                    group.justifyContent = TaffyJustify.Start;
                    group.alignItems = TaffyAlign.Stretch;
                    group.alignContent = TaffyAlignContent.Auto;
                    group.justifyItems = TaffyAlign.Auto;
                    break;
                case "Grid":
                    group.gridAutoFlow = TaffyGridAutoFlow.Row;
                    group.gridRows.Clear();
                    group.gridColumns.Clear();
                    group.gridAutoRows.Clear();
                    group.gridAutoColumns.Clear();
                    group.gridNamedLines.Clear();
                    group.gridAreas.Clear();
                    group.gridAreaRows = 0;
                    group.gridAreaColumns = 0;
                    break;
                case "Responsive":
                    group.responsiveProfiles.Clear();
                    group.safeAreaMode = TaffySafeAreaMode.Disabled;
                    group.scrollRectContentMode = TaffyScrollRectContentMode.AutoExpandContent;
                    group.pixelRounding = TaffyPixelRounding.None;
                    group.maxRebuildRequestsPerFrame = 8;
                    break;
                default:
                    return;
            }
            TaffyLayoutActions.Finish(group);
        }

        private static void ResetItem(TaffyLayoutItem item, string sectionKey)
        {
            if (!item)
                return;
            Undo.RecordObject(item, "Reset TaffyUGUI " + sectionKey);
            switch (sectionKey)
            {
                case "Display":
                    item.display = TaffyDisplay.Flex;
                    item.boxSizing = TaffyBoxSizing.BorderBox;
                    item.writingDirection = TaffyWritingDirection.LeftToRight;
                    item.overflowX = TaffyOverflow.Visible;
                    item.overflowY = TaffyOverflow.Visible;
                    item.scrollbarWidth = 0f;
                    break;
                case "PositionSize":
                    item.position = TaffyPosition.Relative;
                    item.inset = TaffyEdges.Auto;
                    item.width = TaffyLength.Auto;
                    item.height = TaffyLength.Auto;
                    item.minWidth = TaffyLength.Auto;
                    item.minHeight = TaffyLength.Auto;
                    item.maxWidth = TaffyLength.Auto;
                    item.maxHeight = TaffyLength.Auto;
                    item.aspectRatio = 0f;
                    break;
                case "BoxModel":
                    item.margin = TaffyEdges.Zero;
                    item.padding = TaffyEdges.Zero;
                    item.border = TaffyEdges.Zero;
                    break;
                case "Flex":
                    item.flexBasis = TaffyLength.Auto;
                    item.flexGrow = 0f;
                    item.flexShrink = 1f;
                    item.alignSelf = TaffyAlign.Auto;
                    break;
                case "Grid":
                    item.gridRowStart = TaffyGridPlacement.Auto;
                    item.gridRowEnd = TaffyGridPlacement.Auto;
                    item.gridColumnStart = TaffyGridPlacement.Auto;
                    item.gridColumnEnd = TaffyGridPlacement.Auto;
                    item.justifySelf = TaffyAlign.Auto;
                    break;
                case "Block":
                    item.floatMode = TaffyFloat.None;
                    item.clearMode = TaffyClear.None;
                    item.textAlign = TaffyTextAlign.Auto;
                    break;
                case "Measurement":
                    item.measurement = TaffyMeasurementMode.Auto;
                    item.forceReplacedElement = false;
                    item.itemIsTable = false;
                    break;
                default:
                    return;
            }
            TaffyLayoutActions.Finish(item);
        }
    }

    internal static class TaffyExpertClipboard
    {
        private sealed class SizeSnapshot
        {
            internal TaffyLength width, height, minWidth, minHeight, maxWidth, maxHeight;
            internal float aspectRatio;
        }

        private sealed class SpacingSnapshot
        {
            internal TaffyEdges inset, margin, padding, border;
        }

        private sealed class FlexSnapshot
        {
            internal TaffyLength flexBasis;
            internal float flexGrow, flexShrink;
            internal TaffyAlign alignSelf;
        }

        private sealed class GridSnapshot
        {
            internal TaffyGridPlacement rowStart, rowEnd, columnStart, columnEnd;
            internal TaffyAlign justifySelf;
        }

        private static SizeSnapshot _size;
        private static SpacingSnapshot _spacing;
        private static FlexSnapshot _flex;
        private static GridSnapshot _grid;

        internal static bool HasSize => _size != null;
        internal static bool HasSpacing => _spacing != null;
        internal static bool HasFlex => _flex != null;
        internal static bool HasGrid => _grid != null;

        internal static void CopySize(TaffyLayoutItem item)
        {
            if (!item) return;
            _size = new SizeSnapshot
            {
                width = Clone(item.width), height = Clone(item.height), minWidth = Clone(item.minWidth), minHeight = Clone(item.minHeight),
                maxWidth = Clone(item.maxWidth), maxHeight = Clone(item.maxHeight), aspectRatio = item.aspectRatio,
            };
        }

        internal static void PasteSize(IEnumerable<TaffyLayoutItem> items)
        {
            if (_size == null || items == null) return;
            foreach (TaffyLayoutItem item in items)
            {
                if (!item) continue;
                Undo.RecordObject(item, "Paste TaffyUGUI Size");
                item.width = Clone(_size.width); item.height = Clone(_size.height);
                item.minWidth = Clone(_size.minWidth); item.minHeight = Clone(_size.minHeight);
                item.maxWidth = Clone(_size.maxWidth); item.maxHeight = Clone(_size.maxHeight);
                item.aspectRatio = _size.aspectRatio;
                TaffyLayoutActions.Finish(item);
            }
        }

        internal static void CopySpacing(TaffyLayoutItem item)
        {
            if (!item) return;
            _spacing = new SpacingSnapshot { inset = Clone(item.inset), margin = Clone(item.margin), padding = Clone(item.padding), border = Clone(item.border) };
        }

        internal static void PasteSpacing(IEnumerable<TaffyLayoutItem> items)
        {
            if (_spacing == null || items == null) return;
            foreach (TaffyLayoutItem item in items)
            {
                if (!item) continue;
                Undo.RecordObject(item, "Paste TaffyUGUI Spacing");
                item.inset = Clone(_spacing.inset); item.margin = Clone(_spacing.margin);
                item.padding = Clone(_spacing.padding); item.border = Clone(_spacing.border);
                TaffyLayoutActions.Finish(item);
            }
        }

        internal static void CopyFlex(TaffyLayoutItem item)
        {
            if (!item) return;
            _flex = new FlexSnapshot { flexBasis = Clone(item.flexBasis), flexGrow = item.flexGrow, flexShrink = item.flexShrink, alignSelf = item.alignSelf };
        }

        internal static void PasteFlex(IEnumerable<TaffyLayoutItem> items)
        {
            if (_flex == null || items == null) return;
            foreach (TaffyLayoutItem item in items)
            {
                if (!item) continue;
                Undo.RecordObject(item, "Paste TaffyUGUI Flex");
                item.flexBasis = Clone(_flex.flexBasis); item.flexGrow = _flex.flexGrow; item.flexShrink = _flex.flexShrink; item.alignSelf = _flex.alignSelf;
                TaffyLayoutActions.Finish(item);
            }
        }

        internal static void CopyGrid(TaffyLayoutItem item)
        {
            if (!item) return;
            _grid = new GridSnapshot
            {
                rowStart = item.gridRowStart, rowEnd = item.gridRowEnd,
                columnStart = item.gridColumnStart, columnEnd = item.gridColumnEnd, justifySelf = item.justifySelf,
            };
        }

        internal static void PasteGrid(IEnumerable<TaffyLayoutItem> items)
        {
            if (_grid == null || items == null) return;
            foreach (TaffyLayoutItem item in items)
            {
                if (!item) continue;
                Undo.RecordObject(item, "Paste TaffyUGUI Grid Placement");
                item.gridRowStart = _grid.rowStart; item.gridRowEnd = _grid.rowEnd;
                item.gridColumnStart = _grid.columnStart; item.gridColumnEnd = _grid.columnEnd; item.justifySelf = _grid.justifySelf;
                TaffyLayoutActions.Finish(item);
            }
        }

        private static TaffyLength Clone(TaffyLength value)
        {
            value.calc = Clone(value.calc);
            return value;
        }

        private static TaffyEdges Clone(TaffyEdges value)
        {
            value.left = Clone(value.left); value.right = Clone(value.right);
            value.top = Clone(value.top); value.bottom = Clone(value.bottom);
            return value;
        }

        private static TaffyCalcExpression Clone(TaffyCalcExpression source)
        {
            if (source == null) return null;
            var clone = new TaffyCalcExpression { operation = source.operation, value = source.value, operands = new List<TaffyCalcExpression>() };
            if (source.operands != null)
            {
                for (int i = 0; i < source.operands.Count; i++)
                    clone.operands.Add(Clone(source.operands[i]));
            }
            return clone;
        }
    }

    internal static class TaffyExpertClipboardGUI
    {
        internal static void Draw(TaffyInspectorContext context)
        {
            if (context == null || !context.IsAdvancedMode || context.IsMultiEditing && context.SerializedObject.targetObjects.Length == 0)
                return;

            TaffyLayoutItem source = context.Item;
            if (!source && context.SerializedObject.targetObjects.Length > 0)
                source = context.SerializedObject.targetObjects[0] as TaffyLayoutItem;
            if (!source)
                return;

            var items = new List<TaffyLayoutItem>();
            for (int i = 0; i < context.SerializedObject.targetObjects.Length; i++)
            {
                if (context.SerializedObject.targetObjects[i] is TaffyLayoutItem item)
                    items.Add(item);
            }

            TaffyEditorGUI.DrawSectionLabel(new GUIContent("Copy / Paste"));
            DrawRow("Size", () => TaffyExpertClipboard.CopySize(source), () => TaffyExpertClipboard.PasteSize(items), TaffyExpertClipboard.HasSize);
            DrawRow("Spacing", () => TaffyExpertClipboard.CopySpacing(source), () => TaffyExpertClipboard.PasteSpacing(items), TaffyExpertClipboard.HasSpacing);
            DrawRow("Flex", () => TaffyExpertClipboard.CopyFlex(source), () => TaffyExpertClipboard.PasteFlex(items), TaffyExpertClipboard.HasFlex);
            DrawRow("Grid Placement", () => TaffyExpertClipboard.CopyGrid(source), () => TaffyExpertClipboard.PasteGrid(items), TaffyExpertClipboard.HasGrid);
        }

        private static void DrawRow(string label, System.Action copy, System.Action paste, bool canPaste)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);
                if (GUILayout.Button("Copy", EditorStyles.miniButtonLeft)) copy();
                using (new EditorGUI.DisabledScope(!canPaste))
                {
                    if (GUILayout.Button("Paste", EditorStyles.miniButtonRight)) paste();
                }
            }
        }
    }
}
