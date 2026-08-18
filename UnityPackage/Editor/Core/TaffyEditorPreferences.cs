using UnityEditor;

namespace TaffyUGUI.Editor
{
    internal enum TaffyInspectorMode
    {
        Simple = 0,
        Advanced = 1,
    }

    internal enum TaffyInspectorDensity
    {
        Comfortable = 0,
        Compact = 1,
    }

    internal static class TaffyEditorPreferences
    {
        private const string Prefix = "TaffyUGUI.Editor.";

        internal static bool GetFoldout(string inspectorKey, string sectionKey, bool defaultValue)
        {
            return EditorPrefs.GetBool(FoldoutKey(inspectorKey, sectionKey), defaultValue);
        }

        internal static void SetFoldout(string inspectorKey, string sectionKey, bool value)
        {
            EditorPrefs.SetBool(FoldoutKey(inspectorKey, sectionKey), value);
        }

        internal static TaffyInspectorMode InspectorMode
        {
            get => (TaffyInspectorMode)EditorPrefs.GetInt(Prefix + "InspectorMode", (int)TaffyInspectorMode.Simple);
            set => EditorPrefs.SetInt(Prefix + "InspectorMode", (int)value);
        }

        internal static TaffyInspectorDensity InspectorDensity
        {
            get => (TaffyInspectorDensity)EditorPrefs.GetInt(Prefix + "InspectorDensity", (int)TaffyInspectorDensity.Comfortable);
            set => EditorPrefs.SetInt(Prefix + "InspectorDensity", (int)value);
        }

        private static string FoldoutKey(string inspectorKey, string sectionKey)
        {
            return Prefix + "Foldout." + inspectorKey + "." + sectionKey;
        }
    }
}
