using UnityEditor;

namespace TaffyUGUI.Editor
{
    internal static class TaffySceneOverlayPreferences
    {
        private const string Prefix = "TaffyUGUI.SceneOverlay.";

        internal static bool ContainerBounds
        {
            get => Get(nameof(ContainerBounds), true);
            set => Set(nameof(ContainerBounds), value);
        }

        internal static bool ChildBounds
        {
            get => Get(nameof(ChildBounds), true);
            set => Set(nameof(ChildBounds), value);
        }

        internal static bool PaddingBounds
        {
            get => Get(nameof(PaddingBounds), false);
            set => Set(nameof(PaddingBounds), value);
        }

        internal static bool ItemMargins
        {
            get => Get(nameof(ItemMargins), false);
            set => Set(nameof(ItemMargins), value);
        }

        internal static bool FlexAxes
        {
            get => Get(nameof(FlexAxes), false);
            set => Set(nameof(FlexAxes), value);
        }

        internal static bool GapMarkers
        {
            get => Get(nameof(GapMarkers), false);
            set => Set(nameof(GapMarkers), value);
        }

        internal static bool GridTracks
        {
            get => Get(nameof(GridTracks), true);
            set => Set(nameof(GridTracks), value);
        }

        internal static bool ResponsiveProfileLabel
        {
            get => Get(nameof(ResponsiveProfileLabel), true);
            set => Set(nameof(ResponsiveProfileLabel), value);
        }

        internal static bool ComputedSizeLabels
        {
            get => Get(nameof(ComputedSizeLabels), false);
            set => Set(nameof(ComputedSizeLabels), value);
        }

        internal static void ResetForTests()
        {
            Delete(nameof(ContainerBounds));
            Delete(nameof(ChildBounds));
            Delete(nameof(PaddingBounds));
            Delete(nameof(ItemMargins));
            Delete(nameof(FlexAxes));
            Delete(nameof(GapMarkers));
            Delete(nameof(GridTracks));
            Delete(nameof(ResponsiveProfileLabel));
            Delete(nameof(ComputedSizeLabels));
        }

        [MenuItem("Tools/TaffyUGUI/Scene Overlays/Container Bounds")]
        private static void ToggleContainerBounds() => ContainerBounds = !ContainerBounds;
        [MenuItem("Tools/TaffyUGUI/Scene Overlays/Container Bounds", true)]
        private static bool ValidateContainerBounds() => Validate("Tools/TaffyUGUI/Scene Overlays/Container Bounds", ContainerBounds);

        [MenuItem("Tools/TaffyUGUI/Scene Overlays/Child Bounds")]
        private static void ToggleChildBounds() => ChildBounds = !ChildBounds;
        [MenuItem("Tools/TaffyUGUI/Scene Overlays/Child Bounds", true)]
        private static bool ValidateChildBounds() => Validate("Tools/TaffyUGUI/Scene Overlays/Child Bounds", ChildBounds);

        [MenuItem("Tools/TaffyUGUI/Scene Overlays/Padding Bounds")]
        private static void TogglePaddingBounds() => PaddingBounds = !PaddingBounds;
        [MenuItem("Tools/TaffyUGUI/Scene Overlays/Padding Bounds", true)]
        private static bool ValidatePaddingBounds() => Validate("Tools/TaffyUGUI/Scene Overlays/Padding Bounds", PaddingBounds);

        [MenuItem("Tools/TaffyUGUI/Scene Overlays/Selected Item Margins")]
        private static void ToggleItemMargins() => ItemMargins = !ItemMargins;
        [MenuItem("Tools/TaffyUGUI/Scene Overlays/Selected Item Margins", true)]
        private static bool ValidateItemMargins() => Validate("Tools/TaffyUGUI/Scene Overlays/Selected Item Margins", ItemMargins);

        [MenuItem("Tools/TaffyUGUI/Scene Overlays/Flex Axes")]
        private static void ToggleFlexAxes() => FlexAxes = !FlexAxes;
        [MenuItem("Tools/TaffyUGUI/Scene Overlays/Flex Axes", true)]
        private static bool ValidateFlexAxes() => Validate("Tools/TaffyUGUI/Scene Overlays/Flex Axes", FlexAxes);

        [MenuItem("Tools/TaffyUGUI/Scene Overlays/Gap Markers")]
        private static void ToggleGapMarkers() => GapMarkers = !GapMarkers;
        [MenuItem("Tools/TaffyUGUI/Scene Overlays/Gap Markers", true)]
        private static bool ValidateGapMarkers() => Validate("Tools/TaffyUGUI/Scene Overlays/Gap Markers", GapMarkers);

        [MenuItem("Tools/TaffyUGUI/Scene Overlays/Grid Tracks")]
        private static void ToggleGridTracks() => GridTracks = !GridTracks;
        [MenuItem("Tools/TaffyUGUI/Scene Overlays/Grid Tracks", true)]
        private static bool ValidateGridTracks() => Validate("Tools/TaffyUGUI/Scene Overlays/Grid Tracks", GridTracks);

        [MenuItem("Tools/TaffyUGUI/Scene Overlays/Responsive Profile Label")]
        private static void ToggleResponsiveProfileLabel() => ResponsiveProfileLabel = !ResponsiveProfileLabel;
        [MenuItem("Tools/TaffyUGUI/Scene Overlays/Responsive Profile Label", true)]
        private static bool ValidateResponsiveProfileLabel() => Validate("Tools/TaffyUGUI/Scene Overlays/Responsive Profile Label", ResponsiveProfileLabel);

        [MenuItem("Tools/TaffyUGUI/Scene Overlays/Computed Size Labels")]
        private static void ToggleComputedSizeLabels() => ComputedSizeLabels = !ComputedSizeLabels;
        [MenuItem("Tools/TaffyUGUI/Scene Overlays/Computed Size Labels", true)]
        private static bool ValidateComputedSizeLabels() => Validate("Tools/TaffyUGUI/Scene Overlays/Computed Size Labels", ComputedSizeLabels);

        private static bool Get(string key, bool defaultValue)
        {
            return EditorPrefs.GetBool(Prefix + key, defaultValue);
        }

        private static void Set(string key, bool value)
        {
            EditorPrefs.SetBool(Prefix + key, value);
            SceneView.RepaintAll();
        }

        private static void Delete(string key)
        {
            EditorPrefs.DeleteKey(Prefix + key);
        }

        private static bool Validate(string menuPath, bool value)
        {
            Menu.SetChecked(menuPath, value);
            return true;
        }
    }
}
