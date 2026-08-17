using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TaffyUGUI.Editor
{
    public enum TaffyMigrationKind
    {
        Unsupported = 0,
        Horizontal = 1,
        Vertical = 2,
        Grid = 3,
    }

    public sealed class TaffyMigrationAnalysis
    {
        public LayoutGroup source;
        public TaffyMigrationKind kind;
        public bool canMigrate;
        public string message;
    }

    public sealed class TaffyMigrationResult
    {
        public LayoutGroup source;
        public TaffyLayoutGroup migrated;
        public bool success;
        public string message;
    }

    public static class TaffyMigrationService
    {
        private sealed class MigrationSnapshot
        {
            internal GameObject gameObject;
            internal TaffyMigrationKind kind;
            internal bool enabled;
            internal RectOffset padding;
            internal bool horizontal;
            internal float spacing;
            internal TextAnchor childAlignment;
            internal bool childControlWidth;
            internal bool childControlHeight;
            internal bool childForceExpandWidth;
            internal bool childForceExpandHeight;
            internal bool reverseArrangement;
            internal GridLayoutGroup.Constraint gridConstraint;
            internal int constraintCount;
            internal Vector2 cellSize;
            internal Vector2 gridSpacing;
            internal readonly List<RectTransform> children = new List<RectTransform>();
        }

        public static TaffyMigrationAnalysis Analyze(LayoutGroup source)
        {
            var analysis = new TaffyMigrationAnalysis { source = source, kind = TaffyMigrationKind.Unsupported };
            if (!source)
            {
                analysis.message = "Source layout group is null.";
                return analysis;
            }
            if (source.GetComponent<TaffyLayoutGroup>())
            {
                analysis.message = "A TaffyLayoutGroup already exists on this GameObject.";
                return analysis;
            }

            if (source is HorizontalLayoutGroup horizontal)
                return AnalyzeHorizontalOrVertical(horizontal, true);
            if (source is VerticalLayoutGroup vertical)
                return AnalyzeHorizontalOrVertical(vertical, false);
            if (source is GridLayoutGroup grid)
                return AnalyzeGrid(grid);

            analysis.message = $"{source.GetType().Name} is not a supported migration source.";
            return analysis;
        }

        public static TaffyMigrationResult Migrate(LayoutGroup source)
        {
            TaffyMigrationAnalysis analysis = Analyze(source);
            if (!analysis.canMigrate)
                return new TaffyMigrationResult { source = source, success = false, message = analysis.message };

            MigrationSnapshot snapshot = Capture(source, analysis.kind);
            LayoutGroup original = source;
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName($"Migrate {source.name} to TaffyUGUI");
            try
            {
                Undo.DestroyObjectImmediate(source);
                TaffyLayoutGroup target = Undo.AddComponent<TaffyLayoutGroup>(snapshot.gameObject);
                if (!target)
                    throw new InvalidOperationException("Unity refused to add TaffyLayoutGroup after removing the legacy LayoutGroup.");

                Undo.RecordObject(target, "Configure TaffyLayoutGroup");
                target.enabled = snapshot.enabled;
                target.padding = ClonePadding(snapshot.padding);

                if (snapshot.kind == TaffyMigrationKind.Horizontal || snapshot.kind == TaffyMigrationKind.Vertical)
                    ConfigureHorizontalOrVertical(target, snapshot);
                else if (snapshot.kind == TaffyMigrationKind.Grid)
                    ConfigureGrid(target, snapshot);

                RecordPrefab(target);
                EditorUtility.SetDirty(target);
                Undo.CollapseUndoOperations(undoGroup);
                return new TaffyMigrationResult
                {
                    source = original,
                    migrated = target,
                    success = true,
                    message = $"Migrated to {nameof(TaffyLayoutGroup)}.",
                };
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                return new TaffyMigrationResult { source = original, success = false, message = exception.ToString() };
            }
        }

        private static MigrationSnapshot Capture(LayoutGroup source, TaffyMigrationKind kind)
        {
            var snapshot = new MigrationSnapshot
            {
                gameObject = source.gameObject,
                kind = kind,
                enabled = source.enabled,
                padding = ClonePadding(source.padding),
            };

            for (int i = 0; i < source.transform.childCount; i++)
            {
                RectTransform child = source.transform.GetChild(i) as RectTransform;
                if (child) snapshot.children.Add(child);
            }

            if (source is HorizontalOrVerticalLayoutGroup flex)
            {
                snapshot.horizontal = source is HorizontalLayoutGroup;
                snapshot.spacing = flex.spacing;
                snapshot.childAlignment = flex.childAlignment;
                snapshot.childControlWidth = flex.childControlWidth;
                snapshot.childControlHeight = flex.childControlHeight;
                snapshot.childForceExpandWidth = flex.childForceExpandWidth;
                snapshot.childForceExpandHeight = flex.childForceExpandHeight;
                snapshot.reverseArrangement = ReadReverseArrangement(flex);
            }
            else if (source is GridLayoutGroup grid)
            {
                snapshot.gridConstraint = grid.constraint;
                snapshot.constraintCount = grid.constraintCount;
                snapshot.cellSize = grid.cellSize;
                snapshot.gridSpacing = grid.spacing;
                snapshot.childAlignment = grid.childAlignment;
            }

            return snapshot;
        }

        public static List<TaffyMigrationResult> MigrateAll(IEnumerable<LayoutGroup> sources)
        {
            var results = new List<TaffyMigrationResult>();
            if (sources == null)
                return results;

            var seen = new HashSet<int>();
            foreach (LayoutGroup source in sources)
            {
                if (!source || !seen.Add(source.GetInstanceID()))
                    continue;
                results.Add(Migrate(source));
            }
            return results;
        }

        public static List<LayoutGroup> FindLoadedLegacyGroups(bool selectionOnly)
        {
            var result = new List<LayoutGroup>();
            var seen = new HashSet<int>();
            if (selectionOnly)
            {
                GameObject[] selected = Selection.gameObjects;
                for (int i = 0; i < selected.Length; i++)
                {
                    LayoutGroup[] groups = selected[i].GetComponentsInChildren<LayoutGroup>(true);
                    for (int j = 0; j < groups.Length; j++)
                        AddLegacy(groups[j], result, seen);
                }
                return result;
            }

            LayoutGroup[] all = Resources.FindObjectsOfTypeAll<LayoutGroup>();
            for (int i = 0; i < all.Length; i++)
                AddLegacy(all[i], result, seen);
            return result;
        }

        private static TaffyMigrationAnalysis AnalyzeHorizontalOrVertical(HorizontalOrVerticalLayoutGroup source, bool horizontal)
        {
            var analysis = new TaffyMigrationAnalysis
            {
                source = source,
                kind = horizontal ? TaffyMigrationKind.Horizontal : TaffyMigrationKind.Vertical,
                canMigrate = true,
                message = "Compatible with Taffy Flex migration.",
            };

            SerializedObject serialized = new SerializedObject(source);
            SerializedProperty scaleWidth = serialized.FindProperty("m_ChildScaleWidth");
            SerializedProperty scaleHeight = serialized.FindProperty("m_ChildScaleHeight");
            if ((scaleWidth != null && scaleWidth.boolValue) || (scaleHeight != null && scaleHeight.boolValue))
            {
                analysis.canMigrate = false;
                analysis.message = "Legacy child scale control is enabled. TaffyUGUI does not mutate child localScale during layout, so this layout cannot be migrated without changing semantics.";
            }
            return analysis;
        }

        private static TaffyMigrationAnalysis AnalyzeGrid(GridLayoutGroup source)
        {
            var analysis = new TaffyMigrationAnalysis
            {
                source = source,
                kind = TaffyMigrationKind.Grid,
                canMigrate = false,
            };

            if (source.constraint == GridLayoutGroup.Constraint.Flexible)
            {
                analysis.message = "Flexible GridLayoutGroup chooses its row/column count with uGUI-specific heuristics. Use Fixed Column Count or Fixed Row Count before migration.";
                return analysis;
            }
            if (source.startCorner != GridLayoutGroup.Corner.UpperLeft)
            {
                analysis.message = "Only Upper Left GridLayoutGroup start corner is migrated automatically. Other corners require explicit Grid placement authoring.";
                return analysis;
            }
            if (source.constraint == GridLayoutGroup.Constraint.FixedColumnCount && source.startAxis != GridLayoutGroup.Axis.Horizontal)
            {
                analysis.message = "Fixed Column Count migration requires Horizontal start axis for deterministic row-major placement.";
                return analysis;
            }
            if (source.constraint == GridLayoutGroup.Constraint.FixedRowCount && source.startAxis != GridLayoutGroup.Axis.Vertical)
            {
                analysis.message = "Fixed Row Count migration requires Vertical start axis for deterministic column-major placement.";
                return analysis;
            }
            if (source.constraintCount <= 0)
            {
                analysis.message = "Grid constraint count must be greater than zero.";
                return analysis;
            }
            if (source.cellSize.x < 0f || source.cellSize.y < 0f || source.spacing.x < 0f || source.spacing.y < 0f)
            {
                analysis.message = "Negative Grid cell size or spacing cannot be represented by Taffy Grid authoring.";
                return analysis;
            }

            analysis.canMigrate = true;
            analysis.message = "Compatible with deterministic Taffy Grid migration.";
            return analysis;
        }

        private static void ConfigureHorizontalOrVertical(TaffyLayoutGroup target, MigrationSnapshot source)
        {
            bool horizontal = source.horizontal;
            target.containerDisplay = TaffyContainerDisplay.Flex;
            target.direction = horizontal ? TaffyFlexDirection.Row : TaffyFlexDirection.Column;
            target.wrap = TaffyFlexWrap.NoWrap;
            target.horizontalGap = horizontal ? Mathf.Max(0f, source.spacing) : 0f;
            target.verticalGap = horizontal ? 0f : Mathf.Max(0f, source.spacing);
            target.justifyContent = horizontal ? HorizontalJustify(source.childAlignment) : VerticalJustify(source.childAlignment);
            target.alignItems = horizontal
                ? (source.childControlHeight && source.childForceExpandHeight ? TaffyAlign.Stretch : VerticalAlign(source.childAlignment))
                : (source.childControlWidth && source.childForceExpandWidth ? TaffyAlign.Stretch : HorizontalAlign(source.childAlignment));
            target.alignContent = TaffyAlignContent.Start;

            if (source.reverseArrangement)
                target.direction = horizontal ? TaffyFlexDirection.RowReverse : TaffyFlexDirection.ColumnReverse;

            for (int i = 0; i < source.children.Count; i++)
            {
                RectTransform child = source.children[i];
                if (!child) continue;
                ConfigureLegacyChild(child,
                    source.childControlWidth,
                    source.childControlHeight,
                    horizontal ? source.childForceExpandWidth : source.childForceExpandHeight,
                    horizontal);
            }
        }

        private static void ConfigureGrid(TaffyLayoutGroup target, MigrationSnapshot source)
        {
            target.containerDisplay = TaffyContainerDisplay.Grid;
            target.horizontalGap = Mathf.Max(0f, source.gridSpacing.x);
            target.verticalGap = Mathf.Max(0f, source.gridSpacing.y);
            target.justifyContent = HorizontalJustify(source.childAlignment);
            target.alignContent = ToAlignContent(VerticalJustify(source.childAlignment));
            target.alignItems = TaffyAlign.Start;
            target.justifyItems = TaffyAlign.Start;
            target.gridRows.Clear();
            target.gridColumns.Clear();
            target.gridAutoRows.Clear();
            target.gridAutoColumns.Clear();
            target.gridNamedLines.Clear();
            target.gridAreas.Clear();
            target.gridAreaRows = 0;
            target.gridAreaColumns = 0;

            if (source.gridConstraint == GridLayoutGroup.Constraint.FixedColumnCount)
            {
                target.gridAutoFlow = TaffyGridAutoFlow.Row;
                for (int i = 0; i < source.constraintCount; i++)
                    target.gridColumns.Add(TaffyGridTrack.Points(source.cellSize.x));
                target.gridAutoRows.Add(TaffyGridTrack.Points(source.cellSize.y));
            }
            else
            {
                target.gridAutoFlow = TaffyGridAutoFlow.Column;
                for (int i = 0; i < source.constraintCount; i++)
                    target.gridRows.Add(TaffyGridTrack.Points(source.cellSize.y));
                target.gridAutoColumns.Add(TaffyGridTrack.Points(source.cellSize.x));
            }

            for (int i = 0; i < source.children.Count; i++)
            {
                RectTransform child = source.children[i];
                if (!child) continue;
                TaffyLayoutItem item = GetOrAddItem(child);
                item.width = TaffyLength.Points(Mathf.Max(0f, source.cellSize.x));
                item.height = TaffyLength.Points(Mathf.Max(0f, source.cellSize.y));
                item.justifySelf = TaffyAlign.Start;
                item.alignSelf = TaffyAlign.Start;
                RecordPrefab(item);
                EditorUtility.SetDirty(item);
            }
        }

        private static void ConfigureLegacyChild(RectTransform child, bool controlWidth, bool controlHeight, bool forceExpandMain, bool horizontal)
        {
            bool needsItem = !controlWidth || !controlHeight || forceExpandMain;
            TaffyLayoutItem existing = child.GetComponent<TaffyLayoutItem>();
            if (!needsItem && !existing)
                return;

            TaffyLayoutItem item = existing ? existing : Undo.AddComponent<TaffyLayoutItem>(child.gameObject);
            Undo.RecordObject(item, "Configure Taffy child migration");
            if (!controlWidth)
                item.width = TaffyLength.Points(Mathf.Max(0f, child.rect.width));
            if (!controlHeight)
                item.height = TaffyLength.Points(Mathf.Max(0f, child.rect.height));
            if (forceExpandMain)
                item.flexGrow = Mathf.Max(1f, item.flexGrow);
            RecordPrefab(item);
            EditorUtility.SetDirty(item);
        }

        private static TaffyLayoutItem GetOrAddItem(RectTransform child)
        {
            TaffyLayoutItem item = child.GetComponent<TaffyLayoutItem>();
            if (!item)
                item = Undo.AddComponent<TaffyLayoutItem>(child.gameObject);
            Undo.RecordObject(item, "Configure Taffy Grid item");
            return item;
        }

        private static bool ReadReverseArrangement(HorizontalOrVerticalLayoutGroup source)
        {
            SerializedObject serialized = new SerializedObject(source);
            SerializedProperty property = serialized.FindProperty("m_ReverseArrangement");
            return property != null && property.boolValue;
        }

        private static TaffyJustify HorizontalJustify(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperCenter:
                case TextAnchor.MiddleCenter:
                case TextAnchor.LowerCenter: return TaffyJustify.Center;
                case TextAnchor.UpperRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.LowerRight: return TaffyJustify.End;
                default: return TaffyJustify.Start;
            }
        }

        private static TaffyJustify VerticalJustify(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.MiddleLeft:
                case TextAnchor.MiddleCenter:
                case TextAnchor.MiddleRight: return TaffyJustify.Center;
                case TextAnchor.LowerLeft:
                case TextAnchor.LowerCenter:
                case TextAnchor.LowerRight: return TaffyJustify.End;
                default: return TaffyJustify.Start;
            }
        }

        private static TaffyAlign HorizontalAlign(TextAnchor anchor)
        {
            switch (HorizontalJustify(anchor))
            {
                case TaffyJustify.Center: return TaffyAlign.Center;
                case TaffyJustify.End: return TaffyAlign.End;
                default: return TaffyAlign.Start;
            }
        }

        private static TaffyAlign VerticalAlign(TextAnchor anchor)
        {
            switch (VerticalJustify(anchor))
            {
                case TaffyJustify.Center: return TaffyAlign.Center;
                case TaffyJustify.End: return TaffyAlign.End;
                default: return TaffyAlign.Start;
            }
        }

        private static TaffyAlignContent ToAlignContent(TaffyJustify justify)
        {
            switch (justify)
            {
                case TaffyJustify.Center: return TaffyAlignContent.Center;
                case TaffyJustify.End: return TaffyAlignContent.End;
                default: return TaffyAlignContent.Start;
            }
        }

        private static RectOffset ClonePadding(RectOffset padding)
        {
            return padding == null ? new RectOffset() : new RectOffset(padding.left, padding.right, padding.top, padding.bottom);
        }

        private static void RecordPrefab(UnityEngine.Object obj)
        {
            if (obj && PrefabUtility.IsPartOfPrefabInstance(obj))
                PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
        }

        private static void AddLegacy(LayoutGroup group, List<LayoutGroup> result, HashSet<int> seen)
        {
            if (!group || group is TaffyLayoutGroup || !group.gameObject.scene.IsValid() || !group.gameObject.scene.isLoaded)
                return;
            if (!(group is HorizontalLayoutGroup) && !(group is VerticalLayoutGroup) && !(group is GridLayoutGroup))
                return;
            if (seen.Add(group.GetInstanceID()))
                result.Add(group);
        }
    }

    public sealed class TaffyMigrationWindow : EditorWindow
    {
        private bool _selectionOnly = true;
        private Vector2 _scroll;
        private List<LayoutGroup> _groups = new List<LayoutGroup>();

        [MenuItem("Tools/TaffyUGUI/Migration Window")]
        public static void Open()
        {
            TaffyMigrationWindow window = GetWindow<TaffyMigrationWindow>("Taffy Migration");
            window.Refresh();
            window.Show();
        }

        private void OnEnable() => Refresh();

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Migration is conservative. Unsupported layouts are never modified. Use Undo immediately after migration if you want to inspect the generated authoring and revert it.", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                bool selection = EditorGUILayout.ToggleLeft("Selection only", _selectionOnly, GUILayout.Width(110f));
                if (selection != _selectionOnly)
                {
                    _selectionOnly = selection;
                    Refresh();
                }
                if (GUILayout.Button("Refresh", GUILayout.Width(80f)))
                    Refresh();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Migrate All Safe", GUILayout.Width(120f)))
                    MigrateAllSafe();
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _groups.Count; i++)
            {
                LayoutGroup group = _groups[i];
                if (!group)
                    continue;
                TaffyMigrationAnalysis analysis = TaffyMigrationService.Analyze(group);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.ObjectField(group, typeof(LayoutGroup), true);
                    using (new EditorGUI.DisabledScope(!analysis.canMigrate))
                    {
                        if (GUILayout.Button("Migrate", GUILayout.Width(70f)))
                        {
                            TaffyMigrationResult result = TaffyMigrationService.Migrate(group);
                            if (!result.success)
                                EditorUtility.DisplayDialog("Taffy Migration", result.message, "OK");
                            Refresh();
                            GUIUtility.ExitGUI();
                        }
                    }
                }
                EditorGUILayout.HelpBox(analysis.message, analysis.canMigrate ? MessageType.Info : MessageType.Warning);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }

        private void Refresh()
        {
            _groups = TaffyMigrationService.FindLoadedLegacyGroups(_selectionOnly);
            Repaint();
        }

        private void MigrateAllSafe()
        {
            var safe = new List<LayoutGroup>();
            for (int i = 0; i < _groups.Count; i++)
            {
                TaffyMigrationAnalysis analysis = TaffyMigrationService.Analyze(_groups[i]);
                if (analysis.canMigrate)
                    safe.Add(_groups[i]);
            }

            if (safe.Count == 0)
            {
                EditorUtility.DisplayDialog("Taffy Migration", "No safely migratable legacy layout groups were found in the current scope.", "OK");
                return;
            }
            if (!EditorUtility.DisplayDialog("Taffy Migration", $"Migrate {safe.Count} compatible layout group(s)? This operation is Undoable.", "Migrate", "Cancel"))
                return;

            List<TaffyMigrationResult> results = TaffyMigrationService.MigrateAll(safe);
            int success = 0;
            for (int i = 0; i < results.Count; i++)
                if (results[i].success) success++;
            Refresh();
            EditorUtility.DisplayDialog("Taffy Migration", $"Migrated {success} of {results.Count} compatible layout group(s).", "OK");
        }
    }
}
