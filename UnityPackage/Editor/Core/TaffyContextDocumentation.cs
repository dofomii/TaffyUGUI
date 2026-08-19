using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Editor
{
    internal static class TaffyContextDocumentationGUI
    {
        internal static void Draw(TaffyInspectorContext context)
        {
            if (context == null || context.IsMultiEditing)
                return;

            string primaryPath;
            string primaryLabel;
            string secondaryPath;
            string secondaryLabel;

            if (context.Group)
            {
                bool grid = context.ResolvedAuthoringDisplay == TaffyContainerDisplay.Grid;
                primaryPath = grid ? "Documentation~/grid-and-calc.md" : "Documentation~/flexbox.md";
                primaryLabel = grid ? "Grid Docs" : "Flex Docs";
                secondaryPath = "Documentation~/responsive-and-scrollrect.md";
                secondaryLabel = "Responsive Docs";
            }
            else if (context.Item)
            {
                bool grid = TaffyInspectorVisibility.ParentIsGrid(context);
                primaryPath = grid ? "Documentation~/grid-and-calc.md" : "Documentation~/flexbox.md";
                primaryLabel = grid ? "Grid Item Docs" : "Flex Item Docs";
                secondaryPath = "Documentation~/measurement.md";
                secondaryLabel = "Measurement Docs";
            }
            else
            {
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Documentation");
                if (GUILayout.Button(primaryLabel, EditorStyles.miniButtonLeft))
                    TaffyFirstUseGuideWindow.OpenPackagePath(primaryPath);
                if (GUILayout.Button(secondaryLabel, EditorStyles.miniButtonMid))
                    TaffyFirstUseGuideWindow.OpenPackagePath(secondaryPath);
                if (GUILayout.Button("Troubleshooting", EditorStyles.miniButtonRight))
                    TaffyFirstUseGuideWindow.OpenPackagePath("Documentation~/troubleshooting.md");
            }
        }
    }
}
