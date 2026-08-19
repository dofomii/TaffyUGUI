using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Editor
{
    internal enum TaffyGroupQuickLayout
    {
        Horizontal,
        Vertical,
        CenteredPanel,
        Toolbar,
        Cards,
        Grid,
    }

    internal enum TaffyItemQuickAction
    {
        FillWidth,
        FillParent,
        FitContent,
        FixedSize,
        Flexible,
        Spacer,
        Center,
    }

    internal enum TaffyChildInitialization
    {
        PreserveSizes,
        Stretch,
        FitContent,
    }

    internal static class TaffyLayoutActions
    {
        internal static void ApplyQuickLayout(IEnumerable<TaffyLayoutGroup> groups, TaffyGroupQuickLayout layout)
        {
            if (groups == null)
                return;
            foreach (TaffyLayoutGroup group in groups)
            {
                if (!group)
                    continue;
                ApplyQuickLayout(group, layout);
            }
        }

        internal static void ApplyQuickLayout(TaffyLayoutGroup group, TaffyGroupQuickLayout layout)
        {
            if (!group)
                return;

            Undo.RecordObject(group, "TaffyUGUI " + layout + " layout");
            var serialized = new SerializedObject(group);
            serialized.Update();

            switch (layout)
            {
                case TaffyGroupQuickLayout.Horizontal:
                    SetEnum(serialized, "containerDisplay", (int)TaffyContainerDisplay.Flex);
                    SetEnum(serialized, "direction", (int)TaffyFlexDirection.Row);
                    SetEnum(serialized, "wrap", (int)TaffyFlexWrap.NoWrap);
                    break;
                case TaffyGroupQuickLayout.Vertical:
                    SetEnum(serialized, "containerDisplay", (int)TaffyContainerDisplay.Flex);
                    SetEnum(serialized, "direction", (int)TaffyFlexDirection.Column);
                    SetEnum(serialized, "wrap", (int)TaffyFlexWrap.NoWrap);
                    break;
                case TaffyGroupQuickLayout.CenteredPanel:
                    SetEnum(serialized, "containerDisplay", (int)TaffyContainerDisplay.Flex);
                    SetEnum(serialized, "direction", (int)TaffyFlexDirection.Column);
                    SetEnum(serialized, "justifyContent", (int)TaffyJustify.Center);
                    SetEnum(serialized, "alignItems", (int)TaffyAlign.Center);
                    break;
                case TaffyGroupQuickLayout.Toolbar:
                    SetEnum(serialized, "containerDisplay", (int)TaffyContainerDisplay.Flex);
                    SetEnum(serialized, "direction", (int)TaffyFlexDirection.Row);
                    SetEnum(serialized, "wrap", (int)TaffyFlexWrap.NoWrap);
                    SetEnum(serialized, "justifyContent", (int)TaffyJustify.SpaceBetween);
                    SetEnum(serialized, "alignItems", (int)TaffyAlign.Center);
                    SetFloat(serialized, "horizontalGap", 8f);
                    break;
                case TaffyGroupQuickLayout.Cards:
                    SetEnum(serialized, "containerDisplay", (int)TaffyContainerDisplay.Flex);
                    SetEnum(serialized, "direction", (int)TaffyFlexDirection.Row);
                    SetEnum(serialized, "wrap", (int)TaffyFlexWrap.Wrap);
                    SetEnum(serialized, "alignItems", (int)TaffyAlign.Start);
                    SetFloat(serialized, "horizontalGap", 12f);
                    SetFloat(serialized, "verticalGap", 12f);
                    break;
                case TaffyGroupQuickLayout.Grid:
                    SetEnum(serialized, "containerDisplay", (int)TaffyContainerDisplay.Grid);
                    SetEnum(serialized, "gridAutoFlow", (int)TaffyGridAutoFlow.Row);
                    SetFloat(serialized, "horizontalGap", 12f);
                    SetFloat(serialized, "verticalGap", 12f);
                    SetTwoFractionColumns(serialized.FindProperty("gridColumns"));
                    break;
            }

            serialized.ApplyModifiedProperties();
            Finish(group);
        }

        internal static void InitializeChildren(TaffyLayoutGroup group, TaffyChildInitialization initialization)
        {
            if (!group)
                return;

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("TaffyUGUI initialize children");
            for (int i = 0; i < group.transform.childCount; i++)
            {
                Transform child = group.transform.GetChild(i);
                if (!(child is RectTransform rect))
                    continue;
                TaffyLayoutItem item = child.GetComponent<TaffyLayoutItem>();
                if (!item)
                    item = Undo.AddComponent<TaffyLayoutItem>(child.gameObject);

                Undo.RecordObject(item, "TaffyUGUI initialize child");
                var serialized = new SerializedObject(item);
                serialized.Update();
                switch (initialization)
                {
                    case TaffyChildInitialization.PreserveSizes:
                        SetLength(serialized.FindProperty("width"), TaffyUnit.Points, Mathf.Max(0f, rect.rect.width));
                        SetLength(serialized.FindProperty("height"), TaffyUnit.Points, Mathf.Max(0f, rect.rect.height));
                        break;
                    case TaffyChildInitialization.Stretch:
                        SetLength(serialized.FindProperty("width"), TaffyUnit.Percent, 1f);
                        SetLength(serialized.FindProperty("height"), TaffyUnit.Percent, 1f);
                        break;
                    case TaffyChildInitialization.FitContent:
                        SetLength(serialized.FindProperty("width"), TaffyUnit.Auto, 0f);
                        SetLength(serialized.FindProperty("height"), TaffyUnit.Auto, 0f);
                        break;
                }
                serialized.ApplyModifiedProperties();
                Finish(item);
            }
            Undo.CollapseUndoOperations(undoGroup);
            group.SetLayoutDirty();
        }

        private static void SetTwoFractionColumns(SerializedProperty columns)
        {
            if (columns == null)
                return;
            columns.arraySize = 2;
            for (int i = 0; i < 2; i++)
            {
                SerializedProperty track = columns.GetArrayElementAtIndex(i);
                track.FindPropertyRelative("kind").intValue = (int)TaffyGridTrackKind.Fraction;
                track.FindPropertyRelative("value").floatValue = 1f;
            }
        }

        internal static void SetLength(SerializedProperty property, TaffyUnit unit, float value)
        {
            if (property == null)
                return;
            property.FindPropertyRelative("unit").intValue = (int)unit;
            property.FindPropertyRelative("value").floatValue = value;
        }

        private static void SetEnum(SerializedObject serialized, string name, int value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property != null)
                property.intValue = value;
        }

        private static void SetFloat(SerializedObject serialized, string name, float value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property != null)
                property.floatValue = value;
        }

        internal static void Finish(UnityEngine.Object target)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            EditorUtility.SetDirty(target);
            if (target is TaffyLayoutGroup group)
                group.SetLayoutDirty();
        }
    }

    internal static class TaffyItemActions
    {
        internal static void Apply(IEnumerable<TaffyLayoutItem> items, TaffyItemQuickAction action)
        {
            if (items == null)
                return;
            foreach (TaffyLayoutItem item in items)
            {
                if (item)
                    Apply(item, action);
            }
        }

        internal static void Apply(TaffyLayoutItem item, TaffyItemQuickAction action)
        {
            if (!item)
                return;

            Undo.RecordObject(item, "TaffyUGUI " + action);
            var serialized = new SerializedObject(item);
            serialized.Update();
            switch (action)
            {
                case TaffyItemQuickAction.FillWidth:
                    TaffyLayoutActions.SetLength(serialized.FindProperty("width"), TaffyUnit.Percent, 1f);
                    break;
                case TaffyItemQuickAction.FillParent:
                    TaffyLayoutActions.SetLength(serialized.FindProperty("width"), TaffyUnit.Percent, 1f);
                    TaffyLayoutActions.SetLength(serialized.FindProperty("height"), TaffyUnit.Percent, 1f);
                    break;
                case TaffyItemQuickAction.FitContent:
                    TaffyLayoutActions.SetLength(serialized.FindProperty("width"), TaffyUnit.Auto, 0f);
                    TaffyLayoutActions.SetLength(serialized.FindProperty("height"), TaffyUnit.Auto, 0f);
                    break;
                case TaffyItemQuickAction.FixedSize:
                    TaffyLayoutActions.SetLength(serialized.FindProperty("width"), TaffyUnit.Points, 100f);
                    TaffyLayoutActions.SetLength(serialized.FindProperty("height"), TaffyUnit.Points, 100f);
                    break;
                case TaffyItemQuickAction.Flexible:
                    TaffyLayoutActions.SetLength(serialized.FindProperty("flexBasis"), TaffyUnit.Auto, 0f);
                    serialized.FindProperty("flexGrow").floatValue = 1f;
                    serialized.FindProperty("flexShrink").floatValue = 1f;
                    break;
                case TaffyItemQuickAction.Spacer:
                    TaffyLayoutActions.SetLength(serialized.FindProperty("flexBasis"), TaffyUnit.Points, 0f);
                    serialized.FindProperty("flexGrow").floatValue = 1f;
                    serialized.FindProperty("flexShrink").floatValue = 1f;
                    serialized.FindProperty("measurement").intValue = (int)TaffyMeasurementMode.Disabled;
                    break;
                case TaffyItemQuickAction.Center:
                    ApplyCenter(serialized, item);
                    break;
            }
            serialized.ApplyModifiedProperties();
            TaffyLayoutActions.Finish(item);
        }

        private static void ApplyCenter(SerializedObject serialized, TaffyLayoutItem item)
        {
            TaffyLayoutGroup parent = FindParentGroup(item);
            if (!parent)
                return;
            if (parent.containerDisplay == TaffyContainerDisplay.Flex)
                serialized.FindProperty("alignSelf").intValue = (int)TaffyAlign.Center;
            else if (parent.containerDisplay == TaffyContainerDisplay.Grid)
                serialized.FindProperty("justifySelf").intValue = (int)TaffyAlign.Center;
        }

        internal static TaffyLayoutGroup AddGroupToParent(TaffyLayoutItem item)
        {
            if (!item || !item.transform.parent)
                return null;
            Transform parent = item.transform.parent;
            TaffyLayoutGroup existing = parent.GetComponent<TaffyLayoutGroup>();
            if (existing)
                return existing;
            TaffyLayoutGroup group = Undo.AddComponent<TaffyLayoutGroup>(parent.gameObject);
            TaffyLayoutActions.Finish(group);
            return group;
        }

        internal static TaffyLayoutGroup FindParentGroup(TaffyLayoutItem item)
        {
            if (!item)
                return null;
            Transform current = item.transform.parent;
            while (current)
            {
                TaffyLayoutGroup group = current.GetComponent<TaffyLayoutGroup>();
                if (group)
                    return group;
                current = current.parent;
            }
            return null;
        }
    }

    internal static class TaffyHierarchyActions
    {
        [MenuItem("GameObject/TaffyUGUI/Layout Group", false, 1)]
        private static void CreateLayoutGroupMenu() => CreateLayoutGroup();

        [MenuItem("GameObject/TaffyUGUI/Layout Item", false, 2)]
        private static void CreateLayoutItemMenu() => CreateLayoutItem();

        [MenuItem("GameObject/TaffyUGUI/Horizontal Layout", false, 10)]
        private static void CreateHorizontal() => TaffyCreationRecipeCatalog.Create("horizontal");

        [MenuItem("GameObject/TaffyUGUI/Vertical Layout", false, 11)]
        private static void CreateVertical() => TaffyCreationRecipeCatalog.Create("vertical");

        [MenuItem("GameObject/TaffyUGUI/Centered Panel", false, 12)]
        private static void CreateCenteredPanel() => TaffyCreationRecipeCatalog.Create("centered-panel");

        [MenuItem("GameObject/TaffyUGUI/Toolbar", false, 13)]
        private static void CreateToolbar() => TaffyCreationRecipeCatalog.Create("toolbar");

        [MenuItem("GameObject/TaffyUGUI/Sidebar + Content", false, 14)]
        private static void CreateSidebarContent() => TaffyCreationRecipeCatalog.Create("sidebar-content");

        [MenuItem("GameObject/TaffyUGUI/Scrollable List", false, 15)]
        private static void CreateScrollableList() => TaffyCreationRecipeCatalog.Create("scrollable-list");

        [MenuItem("GameObject/TaffyUGUI/Responsive Cards", false, 16)]
        private static void CreateResponsiveCards() => TaffyCreationRecipeCatalog.Create("responsive-cards");

        [MenuItem("GameObject/TaffyUGUI/Modal", false, 17)]
        private static void CreateModal() => TaffyCreationRecipeCatalog.Create("modal");

        [MenuItem("GameObject/TaffyUGUI/Form", false, 18)]
        private static void CreateForm() => TaffyCreationRecipeCatalog.Create("form");

        [MenuItem("GameObject/TaffyUGUI/Grid Layout", false, 19)]
        private static void CreateGrid() => CreateGroup("Grid Layout", TaffyGroupQuickLayout.Grid);

        [MenuItem("GameObject/TaffyUGUI/Spacer", false, 20)]
        private static void CreateSpacer()
        {
            GameObject go = CreateRect("Spacer");
            TaffyLayoutItem item = Undo.AddComponent<TaffyLayoutItem>(go);
            TaffyItemActions.Apply(item, TaffyItemQuickAction.Spacer);
            Selection.activeGameObject = go;
        }

        internal static GameObject CreateLayoutGroup()
        {
            GameObject go = CreateRect("Taffy Layout Group");
            Undo.AddComponent<TaffyLayoutGroup>(go);
            Selection.activeGameObject = go;
            return go;
        }

        internal static GameObject CreateLayoutItem()
        {
            GameObject go = CreateRect("Taffy Layout Item");
            Undo.AddComponent<TaffyLayoutItem>(go);
            Selection.activeGameObject = go;
            return go;
        }

        internal static GameObject CreateGroup(string name, TaffyGroupQuickLayout layout)
        {
            GameObject go = CreateRect(name);
            TaffyLayoutGroup group = Undo.AddComponent<TaffyLayoutGroup>(go);
            TaffyLayoutActions.ApplyQuickLayout(group, layout);
            Selection.activeGameObject = go;
            return go;
        }

        internal static TaffyLayoutItem CreateChildItem(Transform parent, string name)
        {
            GameObject go = CreateChildRect(parent, name);
            return Undo.AddComponent<TaffyLayoutItem>(go);
        }

        internal static TaffyLayoutGroup CreateChildGroup(Transform parent, string name, TaffyGroupQuickLayout layout)
        {
            GameObject go = CreateChildRect(parent, name);
            TaffyLayoutGroup group = Undo.AddComponent<TaffyLayoutGroup>(go);
            TaffyLayoutActions.ApplyQuickLayout(group, layout);
            return group;
        }

        internal static GameObject CreateChildRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            Undo.SetTransformParent(go.transform, parent, "Parent " + name);
            GameObjectUtility.SetParentAndAlign(go, parent ? parent.gameObject : null);
            return go;
        }

        internal static GameObject CreateRect(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            Transform parent = Selection.activeTransform;
            if (parent)
                Undo.SetTransformParent(go.transform, parent, "Parent " + name);
            GameObjectUtility.SetParentAndAlign(go, parent ? parent.gameObject : null);
            return go;
        }
    }
}
