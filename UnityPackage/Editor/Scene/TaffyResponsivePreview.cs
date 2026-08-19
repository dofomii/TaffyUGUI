using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Editor
{
    internal enum TaffyResponsivePreviewPreset
    {
        Off = 0,
        Desktop = 1,
        Tablet = 2,
        Mobile = 3,
        Custom = 4,
    }

    internal static class TaffyResponsivePreview
    {
        private const string PresetKey = "TaffyUGUI.ResponsivePreview.Preset";
        private const string CustomWidthKey = "TaffyUGUI.ResponsivePreview.CustomWidth";
        private const string CustomHeightKey = "TaffyUGUI.ResponsivePreview.CustomHeight";
        private static readonly Color PreviewColor = new Color(0.3f, 1f, 0.75f, 0.9f);

        internal static TaffyResponsivePreviewPreset Preset
        {
            get => (TaffyResponsivePreviewPreset)EditorPrefs.GetInt(PresetKey, (int)TaffyResponsivePreviewPreset.Off);
            set
            {
                EditorPrefs.SetInt(PresetKey, (int)value);
                SceneView.RepaintAll();
            }
        }

        internal static Vector2 CustomSize
        {
            get => new Vector2(
                Mathf.Max(1f, EditorPrefs.GetFloat(CustomWidthKey, 1280f)),
                Mathf.Max(1f, EditorPrefs.GetFloat(CustomHeightKey, 720f)));
            set
            {
                EditorPrefs.SetFloat(CustomWidthKey, Mathf.Max(1f, value.x));
                EditorPrefs.SetFloat(CustomHeightKey, Mathf.Max(1f, value.y));
                Preset = TaffyResponsivePreviewPreset.Custom;
            }
        }

        internal static bool TryGetPreviewSize(out Vector2 size)
        {
            switch (Preset)
            {
                case TaffyResponsivePreviewPreset.Desktop:
                    size = new Vector2(1440f, 900f);
                    return true;
                case TaffyResponsivePreviewPreset.Tablet:
                    size = new Vector2(1024f, 768f);
                    return true;
                case TaffyResponsivePreviewPreset.Mobile:
                    size = new Vector2(390f, 844f);
                    return true;
                case TaffyResponsivePreviewPreset.Custom:
                    size = CustomSize;
                    return true;
                default:
                    size = Vector2.zero;
                    return false;
            }
        }

        internal static string ResolveProfileName(TaffyLayoutGroup group, Vector2 previewSize)
        {
            if (!group || group.responsiveProfiles == null)
                return string.Empty;

            TaffyResponsiveProfile selected = null;
            for (int i = 0; i < group.responsiveProfiles.Count; i++)
            {
                TaffyResponsiveProfile candidate = group.responsiveProfiles[i];
                if (candidate == null || !Matches(candidate, previewSize))
                    continue;
                if (selected == null || candidate.priority > selected.priority)
                    selected = candidate;
            }

            return selected != null ? selected.name : string.Empty;
        }

        internal static bool Matches(TaffyResponsiveProfile profile, Vector2 size)
        {
            if (profile == null)
                return false;

            float width = Mathf.Max(0f, size.x);
            float height = Mathf.Max(0f, size.y);
            if (width + 0.0001f < Mathf.Max(0f, profile.minWidth) || height + 0.0001f < Mathf.Max(0f, profile.minHeight))
                return false;
            if (profile.maxWidth > 0f && width - 0.0001f > profile.maxWidth)
                return false;
            if (profile.maxHeight > 0f && height - 0.0001f > profile.maxHeight)
                return false;
            return true;
        }

        internal static void Draw(TaffyLayoutGroup group, RectTransform root)
        {
            if (!group || !root || !TryGetPreviewSize(out Vector2 previewSize))
                return;

            Rect source = root.rect;
            if (source.width <= 0f || source.height <= 0f)
                return;

            float maxDimension = Mathf.Max(source.width, source.height);
            float scale = maxDimension / Mathf.Max(previewSize.x, previewSize.y);
            Vector2 scaledSize = previewSize * scale;
            Rect previewRect = new Rect(
                source.center.x - scaledSize.x * 0.5f,
                source.center.y - scaledSize.y * 0.5f,
                scaledSize.x,
                scaledSize.y);

            using (new Handles.DrawingScope(PreviewColor))
                TaffySceneOverlayDrawing.DrawLocalRect(root, previewRect);

            string profileName = ResolveProfileName(group, previewSize);
            string profileText = string.IsNullOrEmpty(profileName) ? "base" : profileName;
            string label = $"Preview {Preset}  {previewSize.x:0}×{previewSize.y:0}  → {profileText}";
            Handles.Label(root.TransformPoint(new Vector3(previewRect.xMin, previewRect.yMin, 0f)), label);
        }

        [MenuItem("Tools/TaffyUGUI/Responsive Preview/Off")]
        private static void PreviewOff() => Preset = TaffyResponsivePreviewPreset.Off;
        [MenuItem("Tools/TaffyUGUI/Responsive Preview/Off", true)]
        private static bool ValidatePreviewOff() => ValidatePreset("Tools/TaffyUGUI/Responsive Preview/Off", TaffyResponsivePreviewPreset.Off);

        [MenuItem("Tools/TaffyUGUI/Responsive Preview/Desktop (1440×900)")]
        private static void PreviewDesktop() => Preset = TaffyResponsivePreviewPreset.Desktop;
        [MenuItem("Tools/TaffyUGUI/Responsive Preview/Desktop (1440×900)", true)]
        private static bool ValidatePreviewDesktop() => ValidatePreset("Tools/TaffyUGUI/Responsive Preview/Desktop (1440×900)", TaffyResponsivePreviewPreset.Desktop);

        [MenuItem("Tools/TaffyUGUI/Responsive Preview/Tablet (1024×768)")]
        private static void PreviewTablet() => Preset = TaffyResponsivePreviewPreset.Tablet;
        [MenuItem("Tools/TaffyUGUI/Responsive Preview/Tablet (1024×768)", true)]
        private static bool ValidatePreviewTablet() => ValidatePreset("Tools/TaffyUGUI/Responsive Preview/Tablet (1024×768)", TaffyResponsivePreviewPreset.Tablet);

        [MenuItem("Tools/TaffyUGUI/Responsive Preview/Mobile (390×844)")]
        private static void PreviewMobile() => Preset = TaffyResponsivePreviewPreset.Mobile;
        [MenuItem("Tools/TaffyUGUI/Responsive Preview/Mobile (390×844)", true)]
        private static bool ValidatePreviewMobile() => ValidatePreset("Tools/TaffyUGUI/Responsive Preview/Mobile (390×844)", TaffyResponsivePreviewPreset.Mobile);

        [MenuItem("Tools/TaffyUGUI/Responsive Preview/Custom Size...")]
        private static void PreviewCustom() => TaffyResponsivePreviewWindow.Open();

        private static bool ValidatePreset(string menuPath, TaffyResponsivePreviewPreset preset)
        {
            Menu.SetChecked(menuPath, Preset == preset);
            return true;
        }
    }

    internal sealed class TaffyResponsivePreviewWindow : EditorWindow
    {
        private Vector2 _size;

        internal static void Open()
        {
            TaffyResponsivePreviewWindow window = GetWindow<TaffyResponsivePreviewWindow>(true, "Taffy Responsive Preview", true);
            window.minSize = new Vector2(260f, 110f);
            window._size = TaffyResponsivePreview.CustomSize;
            window.Show();
        }

        private void OnEnable()
        {
            _size = TaffyResponsivePreview.CustomSize;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Custom Preview Size", EditorStyles.boldLabel);
            _size.x = Mathf.Max(1f, EditorGUILayout.FloatField("Width", _size.x));
            _size.y = Mathf.Max(1f, EditorGUILayout.FloatField("Height", _size.y));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply Preview"))
                {
                    TaffyResponsivePreview.CustomSize = _size;
                    Close();
                }

                if (GUILayout.Button("Cancel"))
                    Close();
            }
        }
    }
}
