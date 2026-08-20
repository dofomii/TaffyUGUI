using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Editor
{
    internal enum TaffyResponsiveOverrideKind
    {
        ContainerDisplay = 0,
        FlexDirection = 1,
        FlexWrap = 2,
        Gaps = 3,
        Alignment = 4,
        GridAutoFlow = 5,
        Padding = 6,
    }

    internal static class TaffyResponsiveAuthoringUtility
    {
        internal static readonly GUIContent[] OverrideLabels =
        {
            new GUIContent("Layout Type", "Override the container display mode for this breakpoint."),
            new GUIContent("Flex Direction", "Override Row / Column direction for this breakpoint."),
            new GUIContent("Flex Wrap", "Override wrapping for this breakpoint."),
            new GUIContent("Gaps", "Override horizontal and vertical gaps for this breakpoint."),
            new GUIContent("Alignment", "Override main-axis and cross-axis alignment for this breakpoint."),
            new GUIContent("Grid Auto Flow", "Override Grid auto-placement flow for this breakpoint."),
            new GUIContent("Padding", "Override container padding for this breakpoint."),
        };

        private static readonly string[] OverrideFlags =
        {
            "overrideContainerDisplay",
            "overrideFlexDirection",
            "overrideFlexWrap",
            "overrideGaps",
            "overrideAlignment",
            "overrideGridAutoFlow",
            "overridePadding",
        };

        internal static SerializedProperty AddProfile(SerializedProperty profiles, string name = null)
        {
            if (profiles == null || !profiles.isArray)
                return null;

            int index = profiles.arraySize;
            profiles.InsertArrayElementAtIndex(index);
            SerializedProperty profile = profiles.GetArrayElementAtIndex(index);
            if (profile == null)
                return null;

            ResetProfile(profile, string.IsNullOrWhiteSpace(name) ? "Breakpoint " + (index + 1) : name);
            return profile;
        }

        internal static void ResetProfile(SerializedProperty profile, string name)
        {
            if (profile == null)
                return;

            SetString(profile, "name", string.IsNullOrWhiteSpace(name) ? "Breakpoint" : name);
            SetInt(profile, "priority", 0);
            SetFloat(profile, "minWidth", 0f);
            SetFloat(profile, "maxWidth", 0f);
            SetFloat(profile, "minHeight", 0f);
            SetFloat(profile, "maxHeight", 0f);

            for (int i = 0; i < OverrideFlags.Length; i++)
                SetBool(profile, OverrideFlags[i], false);
        }

        internal static bool IsOverrideEnabled(SerializedProperty profile, TaffyResponsiveOverrideKind kind)
        {
            SerializedProperty flag = FindOverrideFlag(profile, kind);
            return flag != null && flag.boolValue;
        }

        internal static void SetOverrideEnabled(SerializedProperty profile, TaffyResponsiveOverrideKind kind, bool enabled)
        {
            SerializedProperty flag = FindOverrideFlag(profile, kind);
            if (flag != null)
                flag.boolValue = enabled;
        }

        internal static int EnabledOverrideCount(SerializedProperty profile)
        {
            if (profile == null)
                return 0;
            int count = 0;
            for (int i = 0; i < OverrideFlags.Length; i++)
            {
                SerializedProperty flag = profile.FindPropertyRelative(OverrideFlags[i]);
                if (flag != null && flag.boolValue)
                    count++;
            }
            return count;
        }

        internal static string BreakpointSummary(SerializedProperty profile)
        {
            if (profile == null)
                return string.Empty;

            float minWidth = GetFloat(profile, "minWidth");
            float maxWidth = GetFloat(profile, "maxWidth");
            float minHeight = GetFloat(profile, "minHeight");
            float maxHeight = GetFloat(profile, "maxHeight");
            string width = RangeSummary(minWidth, maxWidth, "W");
            string height = RangeSummary(minHeight, maxHeight, "H");
            return width + " • " + height + " • " + EnabledOverrideCount(profile) + " overrides";
        }

        internal static bool BoundsOverlap(SerializedProperty first, SerializedProperty second)
        {
            if (first == null || second == null)
                return false;

            return RangesOverlap(GetFloat(first, "minWidth"), GetFloat(first, "maxWidth"), GetFloat(second, "minWidth"), GetFloat(second, "maxWidth")) &&
                   RangesOverlap(GetFloat(first, "minHeight"), GetFloat(first, "maxHeight"), GetFloat(second, "minHeight"), GetFloat(second, "maxHeight"));
        }

        internal static List<string> CollectOverlapWarnings(SerializedProperty profiles)
        {
            var warnings = new List<string>();
            if (profiles == null || !profiles.isArray)
                return warnings;

            for (int i = 0; i < profiles.arraySize; i++)
            {
                SerializedProperty first = profiles.GetArrayElementAtIndex(i);
                if (first == null)
                    continue;
                string firstName = ProfileName(first, i);
                int firstPriority = first.FindPropertyRelative("priority")?.intValue ?? 0;

                for (int j = i + 1; j < profiles.arraySize; j++)
                {
                    SerializedProperty second = profiles.GetArrayElementAtIndex(j);
                    if (second == null || !BoundsOverlap(first, second))
                        continue;
                    string secondName = ProfileName(second, j);
                    int secondPriority = second.FindPropertyRelative("priority")?.intValue ?? 0;
                    if (firstPriority == secondPriority)
                    {
                        warnings.Add("Breakpoints '" + firstName + "' and '" + secondName + "' overlap at the same priority. Profile order will decide the winner when both match.");
                    }
                }
            }
            return warnings;
        }

        internal static string ProfileName(SerializedProperty profile, int fallbackIndex)
        {
            string name = profile?.FindPropertyRelative("name")?.stringValue;
            return string.IsNullOrWhiteSpace(name) ? "Breakpoint " + (fallbackIndex + 1) : name;
        }

        internal static SerializedProperty FindOverrideFlag(SerializedProperty profile, TaffyResponsiveOverrideKind kind)
        {
            int index = (int)kind;
            if (profile == null || index < 0 || index >= OverrideFlags.Length)
                return null;
            return profile.FindPropertyRelative(OverrideFlags[index]);
        }

        private static bool RangesOverlap(float minA, float maxA, float minB, float maxB)
        {
            float aMax = maxA <= 0f ? float.PositiveInfinity : maxA;
            float bMax = maxB <= 0f ? float.PositiveInfinity : maxB;
            return Mathf.Max(0f, minA) <= bMax && Mathf.Max(0f, minB) <= aMax;
        }

        private static string RangeSummary(float min, float max, string axis)
        {
            min = Mathf.Max(0f, min);
            max = Mathf.Max(0f, max);
            if (min <= 0f && max <= 0f)
                return axis + " any";
            if (max <= 0f)
                return axis + " ≥ " + min.ToString("0.##");
            if (min <= 0f)
                return axis + " ≤ " + max.ToString("0.##");
            return axis + " " + min.ToString("0.##") + "–" + max.ToString("0.##");
        }

        private static void SetBool(SerializedProperty property, string child, bool value)
        {
            SerializedProperty target = property.FindPropertyRelative(child);
            if (target != null)
                target.boolValue = value;
        }

        private static void SetInt(SerializedProperty property, string child, int value)
        {
            SerializedProperty target = property.FindPropertyRelative(child);
            if (target != null)
                target.intValue = value;
        }

        private static void SetFloat(SerializedProperty property, string child, float value)
        {
            SerializedProperty target = property.FindPropertyRelative(child);
            if (target != null)
                target.floatValue = value;
        }

        private static void SetString(SerializedProperty property, string child, string value)
        {
            SerializedProperty target = property.FindPropertyRelative(child);
            if (target != null)
                target.stringValue = value;
        }

        private static float GetFloat(SerializedProperty property, string child)
        {
            SerializedProperty target = property?.FindPropertyRelative(child);
            return target == null ? 0f : target.floatValue;
        }
    }

    internal static class TaffyGridAuthoringUtility
    {
        internal static void SetEqualFractionTracks(SerializedProperty tracks, int count)
        {
            if (tracks == null || !tracks.isArray)
                return;

            count = Mathf.Clamp(count, 1, 64);
            tracks.arraySize = count;
            for (int i = 0; i < count; i++)
                ConfigureSimpleTrack(tracks.GetArrayElementAtIndex(i), TaffyGridTrackKind.Fraction, 1f);
        }

        internal static SerializedProperty AddTrack(SerializedProperty tracks, TaffyGridTrackKind kind = TaffyGridTrackKind.Fraction, float value = 1f)
        {
            if (tracks == null || !tracks.isArray)
                return null;

            int index = tracks.arraySize;
            tracks.InsertArrayElementAtIndex(index);
            SerializedProperty track = tracks.GetArrayElementAtIndex(index);
            ConfigureSimpleTrack(track, kind, value);
            return track;
        }

        internal static void ConfigureSimpleTrack(SerializedProperty track, TaffyGridTrackKind kind, float value = 1f)
        {
            if (track == null)
                return;
            SerializedProperty kindProperty = track.FindPropertyRelative("kind");
            SerializedProperty valueProperty = track.FindPropertyRelative("value");
            if (kindProperty != null)
                kindProperty.intValue = (int)kind;
            if (valueProperty != null && kind is TaffyGridTrackKind.Points or TaffyGridTrackKind.Percent or TaffyGridTrackKind.Fraction)
                valueProperty.floatValue = value;
        }

        internal static string TrackSummary(SerializedProperty track)
        {
            if (track == null)
                return string.Empty;
            SerializedProperty kindProperty = track.FindPropertyRelative("kind");
            if (kindProperty == null)
                return string.Empty;
            TaffyGridTrackKind kind = (TaffyGridTrackKind)kindProperty.intValue;
            float value = track.FindPropertyRelative("value")?.floatValue ?? 0f;
            switch (kind)
            {
                case TaffyGridTrackKind.Points: return value.ToString("0.##") + " px";
                case TaffyGridTrackKind.Percent: return (value * 100f).ToString("0.##") + "%";
                case TaffyGridTrackKind.Fraction: return value.ToString("0.##") + " fr";
                case TaffyGridTrackKind.MinContent: return "Min Content";
                case TaffyGridTrackKind.MaxContent: return "Max Content";
                case TaffyGridTrackKind.MinMax: return "MinMax";
                case TaffyGridTrackKind.Repeat:
                    SerializedProperty mode = track.FindPropertyRelative("repeatMode");
                    if (mode != null && (TaffyGridRepeatMode)mode.intValue == TaffyGridRepeatMode.Count)
                        return "Repeat ×" + Mathf.Max(1, track.FindPropertyRelative("repeatCount")?.intValue ?? 1);
                    return mode == null ? "Repeat" : "Repeat " + ((TaffyGridRepeatMode)mode.intValue);
                case TaffyGridTrackKind.Calc: return "Calc";
                default: return "Auto";
            }
        }

        internal static void SetPlacementSpan(SerializedProperty placement, int span)
        {
            if (placement == null)
                return;
            SerializedProperty kind = placement.FindPropertyRelative("kind");
            SerializedProperty spanProperty = placement.FindPropertyRelative("span");
            if (kind != null)
                kind.intValue = (int)TaffyGridPlacementKind.Span;
            if (spanProperty != null)
                spanProperty.intValue = Mathf.Max(1, span);
        }

        internal static void SetPlacementAuto(SerializedProperty placement)
        {
            SerializedProperty kind = placement?.FindPropertyRelative("kind");
            if (kind != null)
                kind.intValue = (int)TaffyGridPlacementKind.Auto;
        }
    }

    internal static class TaffyResponsiveAuthoringGUI
    {
        private static readonly Dictionary<string, bool> RawFoldouts = new Dictionary<string, bool>();

        internal static void Draw(TaffyInspectorContext context)
        {
            SerializedObject serializedObject = context.SerializedObject;
            SerializedProperty profiles = serializedObject.FindProperty("responsiveProfiles");

            if (!context.IsMultiEditing && context.Group)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.TextField("Active Breakpoint", string.IsNullOrEmpty(context.Group.ActiveResponsiveProfileName) ? "Base settings" : context.Group.ActiveResponsiveProfileName);
            }

            DrawProfiles(profiles);

            TaffySerializedPropertyUtility.DrawProperty(serializedObject, "safeAreaMode", new GUIContent("Safe Area"));
            TaffySerializedPropertyUtility.DrawProperty(serializedObject, "scrollRectContentMode", new GUIContent("ScrollRect Content"));
            TaffySerializedPropertyUtility.DrawProperty(serializedObject, "pixelRounding", new GUIContent("Pixel Rounding"));
            TaffySerializedPropertyUtility.DrawProperty(serializedObject, "maxRebuildRequestsPerFrame", new GUIContent("Max Rebuild Requests / Frame"));

            if (context.IsMultiEditing)
                return;

            List<string> overlapWarnings = TaffyResponsiveAuthoringUtility.CollectOverlapWarnings(profiles);
            for (int i = 0; i < overlapWarnings.Count; i++)
                EditorGUILayout.HelpBox(overlapWarnings[i], MessageType.Warning);

            if (context.Group && !context.Group.ValidateResponsiveProfiles(out string error))
                EditorGUILayout.HelpBox(error, MessageType.Error);
        }

        private static void DrawProfiles(SerializedProperty profiles)
        {
            if (profiles == null)
                return;

            EditorGUILayout.LabelField("Breakpoints", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Profiles match the container RectTransform size. Zero maximum means unbounded. Higher priority wins before list order.", MessageType.None);

            if (profiles.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("Responsive breakpoint editing is unavailable while selected Groups have different profile lists.", MessageType.Info);
                return;
            }

            for (int i = 0; i < profiles.arraySize; i++)
            {
                SerializedProperty profile = profiles.GetArrayElementAtIndex(i);
                DrawProfileCard(profiles, profile, i);
            }

            if (GUILayout.Button("+ Add Breakpoint"))
                TaffyResponsiveAuthoringUtility.AddProfile(profiles);
        }

        private static void DrawProfileCard(SerializedProperty profiles, SerializedProperty profile, int index)
        {
            if (profile == null)
                return;

            string key = TaffyEditorObjectIdentity.IdentityHash(profile.serializedObject.targetObject) + ":" + profile.propertyPath;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(TaffyResponsiveAuthoringUtility.ProfileName(profile, index), EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Remove", GUILayout.Width(62f)))
                {
                    profiles.DeleteArrayElementAtIndex(index);
                    EditorGUILayout.EndVertical();
                    return;
                }
            }

            EditorGUILayout.LabelField(TaffyResponsiveAuthoringUtility.BreakpointSummary(profile), EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(profile.FindPropertyRelative("name"), new GUIContent("Name"));
            EditorGUILayout.PropertyField(profile.FindPropertyRelative("priority"), new GUIContent("Priority", "Higher priority wins when multiple profiles match."));

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(profile.FindPropertyRelative("minWidth"), new GUIContent("Min Width"));
                EditorGUILayout.PropertyField(profile.FindPropertyRelative("maxWidth"), new GUIContent("Max Width", "0 means unbounded."));
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(profile.FindPropertyRelative("minHeight"), new GUIContent("Min Height"));
                EditorGUILayout.PropertyField(profile.FindPropertyRelative("maxHeight"), new GUIContent("Max Height", "0 means unbounded."));
            }

            DrawEnabledOverrides(profile);
            DrawAddOverrideMenu(profile);

            bool raw = RawFoldouts.TryGetValue(key, out bool saved) && saved;
            raw = EditorGUILayout.Foldout(raw, "Raw profile data (Advanced)", true);
            RawFoldouts[key] = raw;
            if (raw)
                EditorGUILayout.PropertyField(profile, GUIContent.none, true);

            EditorGUILayout.EndVertical();
        }

        private static void DrawEnabledOverrides(SerializedProperty profile)
        {
            DrawOverride(profile, TaffyResponsiveOverrideKind.ContainerDisplay, "containerDisplay", "Layout Type");
            DrawOverride(profile, TaffyResponsiveOverrideKind.FlexDirection, "direction", "Direction");
            DrawOverride(profile, TaffyResponsiveOverrideKind.FlexWrap, "wrap", "Wrap");

            if (TaffyResponsiveAuthoringUtility.IsOverrideEnabled(profile, TaffyResponsiveOverrideKind.Gaps))
            {
                DrawOverrideHeader(profile, TaffyResponsiveOverrideKind.Gaps, "Gaps");
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(profile.FindPropertyRelative("horizontalGap"), new GUIContent("Horizontal"));
                    EditorGUILayout.PropertyField(profile.FindPropertyRelative("verticalGap"), new GUIContent("Vertical"));
                }
            }

            if (TaffyResponsiveAuthoringUtility.IsOverrideEnabled(profile, TaffyResponsiveOverrideKind.Alignment))
            {
                DrawOverrideHeader(profile, TaffyResponsiveOverrideKind.Alignment, "Alignment");
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(profile.FindPropertyRelative("justifyContent"), new GUIContent("Main Axis"));
                    EditorGUILayout.PropertyField(profile.FindPropertyRelative("alignItems"), new GUIContent("Cross Axis"));
                    EditorGUILayout.PropertyField(profile.FindPropertyRelative("alignContent"), new GUIContent("Wrapped Lines"));
                    EditorGUILayout.PropertyField(profile.FindPropertyRelative("justifyItems"), new GUIContent("Grid Items"));
                }
            }

            DrawOverride(profile, TaffyResponsiveOverrideKind.GridAutoFlow, "gridAutoFlow", "Grid Auto Flow");
            DrawOverride(profile, TaffyResponsiveOverrideKind.Padding, "padding", "Padding", true);
        }

        private static void DrawOverride(SerializedProperty profile, TaffyResponsiveOverrideKind kind, string propertyName, string label, bool includeChildren = false)
        {
            if (!TaffyResponsiveAuthoringUtility.IsOverrideEnabled(profile, kind))
                return;
            DrawOverrideHeader(profile, kind, label);
            using (new EditorGUI.IndentLevelScope())
                EditorGUILayout.PropertyField(profile.FindPropertyRelative(propertyName), new GUIContent(label), includeChildren);
        }

        private static void DrawOverrideHeader(SerializedProperty profile, TaffyResponsiveOverrideKind kind, string label)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("×", GUILayout.Width(24f)))
                    TaffyResponsiveAuthoringUtility.SetOverrideEnabled(profile, kind, false);
            }
        }

        private static void DrawAddOverrideMenu(SerializedProperty profile)
        {
            if (!GUILayout.Button("+ Override Property"))
                return;

            var menu = new GenericMenu();
            bool any = false;
            for (int i = 0; i < TaffyResponsiveAuthoringUtility.OverrideLabels.Length; i++)
            {
                TaffyResponsiveOverrideKind kind = (TaffyResponsiveOverrideKind)i;
                if (TaffyResponsiveAuthoringUtility.IsOverrideEnabled(profile, kind))
                    continue;
                any = true;
                GUIContent label = TaffyResponsiveAuthoringUtility.OverrideLabels[i];
                menu.AddItem(label, false, () =>
                {
                    profile.serializedObject.Update();
                    TaffyResponsiveAuthoringUtility.SetOverrideEnabled(profile, kind, true);
                    profile.serializedObject.ApplyModifiedProperties();
                });
            }
            if (!any)
                menu.AddDisabledItem(new GUIContent("All override properties are enabled"));
            menu.ShowAsContext();
        }
    }

    internal static class TaffyGridAuthoringGUI
    {
        private static readonly Dictionary<string, bool> AdvancedFoldouts = new Dictionary<string, bool>();

        internal static void DrawGroup(TaffyInspectorContext context)
        {
            SerializedObject serializedObject = context.SerializedObject;
            TaffySerializedPropertyUtility.DrawProperty(serializedObject, "gridAutoFlow", new GUIContent("Auto Flow"));

            DrawStarterButtons(serializedObject.FindProperty("gridColumns"));
            DrawTrackList(serializedObject.FindProperty("gridColumns"), "Columns");
            DrawTrackList(serializedObject.FindProperty("gridRows"), "Rows");

            EditorGUILayout.LabelField("Gap", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                TaffySerializedPropertyUtility.DrawProperty(serializedObject, "horizontalGap", new GUIContent("Horizontal"));
                TaffySerializedPropertyUtility.DrawProperty(serializedObject, "verticalGap", new GUIContent("Vertical"));
            }

            DrawTrackList(serializedObject.FindProperty("gridAutoColumns"), "Implicit Columns", true);
            DrawTrackList(serializedObject.FindProperty("gridAutoRows"), "Implicit Rows", true);

            string key = TaffyEditorObjectIdentity.IdentityHash(serializedObject.targetObject) + ":GridAdvanced";
            bool expanded = AdvancedFoldouts.TryGetValue(key, out bool saved) && saved;
            expanded = EditorGUILayout.Foldout(expanded, "Named lines / areas (Advanced)", true);
            AdvancedFoldouts[key] = expanded;
            if (expanded)
            {
                TaffySerializedPropertyUtility.DrawProperty(serializedObject, "gridNamedLines", new GUIContent("Named Lines"));
                TaffySerializedPropertyUtility.DrawProperty(serializedObject, "gridAreas", new GUIContent("Areas"));
                TaffySerializedPropertyUtility.DrawProperty(serializedObject, "gridAreaRows", new GUIContent("Area Rows"));
                TaffySerializedPropertyUtility.DrawProperty(serializedObject, "gridAreaColumns", new GUIContent("Area Columns"));
            }
        }

        internal static void DrawItem(TaffyInspectorContext context)
        {
            SerializedObject serializedObject = context.SerializedObject;
            EditorGUILayout.LabelField("Grid Placement", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Auto"))
                {
                    TaffyGridAuthoringUtility.SetPlacementAuto(serializedObject.FindProperty("gridRowStart"));
                    TaffyGridAuthoringUtility.SetPlacementAuto(serializedObject.FindProperty("gridRowEnd"));
                    TaffyGridAuthoringUtility.SetPlacementAuto(serializedObject.FindProperty("gridColumnStart"));
                    TaffyGridAuthoringUtility.SetPlacementAuto(serializedObject.FindProperty("gridColumnEnd"));
                }
                if (GUILayout.Button("Span 2 Cols"))
                    TaffyGridAuthoringUtility.SetPlacementSpan(serializedObject.FindProperty("gridColumnEnd"), 2);
                if (GUILayout.Button("Span 2 Rows"))
                    TaffyGridAuthoringUtility.SetPlacementSpan(serializedObject.FindProperty("gridRowEnd"), 2);
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("gridColumnStart"), new GUIContent("Column Start"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("gridColumnEnd"), new GUIContent("Column End / Span"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("gridRowStart"), new GUIContent("Row Start"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("gridRowEnd"), new GUIContent("Row End / Span"), true);
            TaffySerializedPropertyUtility.DrawProperty(serializedObject, "justifySelf", new GUIContent("Self Alignment"));
        }

        private static void DrawStarterButtons(SerializedProperty columns)
        {
            EditorGUILayout.LabelField("Grid Starter", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("2 Columns"))
                    TaffyGridAuthoringUtility.SetEqualFractionTracks(columns, 2);
                if (GUILayout.Button("3 Columns"))
                    TaffyGridAuthoringUtility.SetEqualFractionTracks(columns, 3);
                if (GUILayout.Button("4 Columns"))
                    TaffyGridAuthoringUtility.SetEqualFractionTracks(columns, 4);
            }
        }

        private static void DrawTrackList(SerializedProperty tracks, string label, bool collapsedByDefault = false)
        {
            if (tracks == null)
                return;

            string key = TaffyEditorObjectIdentity.IdentityHash(tracks.serializedObject.targetObject) + ":" + tracks.propertyPath;
            bool expanded;
            if (!AdvancedFoldouts.TryGetValue(key, out expanded))
                expanded = !collapsedByDefault;
            expanded = EditorGUILayout.Foldout(expanded, label + " (" + tracks.arraySize + ")", true);
            AdvancedFoldouts[key] = expanded;
            if (!expanded)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                for (int i = 0; i < tracks.arraySize; i++)
                {
                    SerializedProperty track = tracks.GetArrayElementAtIndex(i);
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField((i + 1) + ". " + TaffyGridAuthoringUtility.TrackSummary(track), EditorStyles.miniBoldLabel);
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Remove", GUILayout.Width(62f)))
                            {
                                tracks.DeleteArrayElementAtIndex(i);
                                break;
                            }
                        }
                        EditorGUILayout.PropertyField(track, new GUIContent("Track"), true);
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("+ Fraction"))
                        TaffyGridAuthoringUtility.AddTrack(tracks, TaffyGridTrackKind.Fraction, 1f);
                    if (GUILayout.Button("+ Auto"))
                        TaffyGridAuthoringUtility.AddTrack(tracks, TaffyGridTrackKind.Auto, 0f);
                }
            }
        }
    }
}
