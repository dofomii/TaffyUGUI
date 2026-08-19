using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Editor
{
    [InitializeOnLoad]
    public static class TaffySceneVisualization
    {
        private const string PreferenceKey = "TaffyUGUI.SceneVisualization.Enabled";

        static TaffySceneVisualization()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(PreferenceKey, true);
            set
            {
                EditorPrefs.SetBool(PreferenceKey, value);
                SceneView.RepaintAll();
            }
        }

        [MenuItem("Tools/TaffyUGUI/Toggle Scene Visualization")]
        private static void Toggle()
        {
            Enabled = !Enabled;
        }

        [MenuItem("Tools/TaffyUGUI/Toggle Scene Visualization", true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked("Tools/TaffyUGUI/Toggle Scene Visualization", Enabled);
            return true;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!Enabled)
                return;

            Object[] selection = Selection.objects;
            for (int i = 0; i < selection.Length; i++)
            {
                GameObject go = selection[i] as GameObject;
                if (!go && selection[i] is Component component)
                    go = component.gameObject;
                if (!go)
                    continue;

                TaffyLayoutGroup group = go.GetComponent<TaffyLayoutGroup>();
                if (group && group.isActiveAndEnabled)
                    TaffySceneOverlayDrawing.DrawGroup(group);

                TaffyLayoutItem item = go.GetComponent<TaffyLayoutItem>();
                if (TaffySceneOverlayPreferences.ItemMargins && item && item.isActiveAndEnabled)
                    TaffySceneOverlayDrawing.DrawSelectedItemMargin(item);
            }
        }
    }
}
