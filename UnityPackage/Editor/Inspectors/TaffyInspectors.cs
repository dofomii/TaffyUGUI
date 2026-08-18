using UnityEditor;

namespace TaffyUGUI.Editor
{
    [CustomEditor(typeof(TaffyLayoutGroup)), CanEditMultipleObjects]
    public sealed class TaffyLayoutGroupEditor : UnityEditor.Editor
    {
        internal static readonly string[] PropertyCoverage =
        {
            "containerDisplay", "boxSizing", "writingDirection", "overflowX", "overflowY", "scrollbarWidth", "m_Padding", "border", "textAlign",
            "direction", "wrap", "horizontalGap", "verticalGap", "justifyContent", "alignItems", "alignContent", "justifyItems",
            "gridAutoFlow", "gridRows", "gridColumns", "gridAutoRows", "gridAutoColumns", "gridNamedLines", "gridAreas", "gridAreaRows", "gridAreaColumns",
            "responsiveProfiles", "safeAreaMode", "scrollRectContentMode", "pixelRounding", "maxRebuildRequestsPerFrame",
        };

        private readonly TaffyGroupQuickSetupSection _quickSetupSection = new TaffyGroupQuickSetupSection();
        private readonly TaffyInspectorSection[] _authoringSections =
        {
            new TaffyGroupFormattingSection(),
            new TaffyGroupFlexSection(),
            new TaffyGroupGridSection(),
            new TaffyGroupResponsiveSection(),
        };

        private readonly TaffyGroupDiagnosticsSection _diagnosticsSection = new TaffyGroupDiagnosticsSection();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var context = new TaffyInspectorContext(this);
            TaffyEditorGUI.DrawScript(serializedObject);
            TaffyEditorGUI.DrawInspectorMode();
            _quickSetupSection.Draw(context);

            if (context.IsSimpleMode)
            {
                TaffyEditorGUI.DrawSimpleModeHint();
            }
            else
            {
                for (int i = 0; i < _authoringSections.Length; i++)
                    _authoringSections[i].Draw(context);
            }

            serializedObject.ApplyModifiedProperties();

            if (!context.IsMultiEditing && context.Group)
                TaffyEditorGUI.DrawValidation(context.Group);
            _diagnosticsSection.Draw(context);
        }
    }

    [CustomEditor(typeof(TaffyLayoutItem)), CanEditMultipleObjects]
    public sealed class TaffyLayoutItemEditor : UnityEditor.Editor
    {
        internal static readonly string[] PropertyCoverage =
        {
            "display", "boxSizing", "writingDirection", "overflowX", "overflowY", "scrollbarWidth",
            "position", "inset", "width", "height", "minWidth", "minHeight", "maxWidth", "maxHeight", "aspectRatio",
            "margin", "padding", "border",
            "flexBasis", "flexGrow", "flexShrink", "alignSelf",
            "gridRowStart", "gridRowEnd", "gridColumnStart", "gridColumnEnd", "justifySelf",
            "floatMode", "clearMode", "textAlign",
            "measurement", "forceReplacedElement", "itemIsTable",
        };

        private readonly TaffyItemParentSummarySection _parentSummarySection = new TaffyItemParentSummarySection();
        private readonly TaffyItemEssentialsSection _essentialsSection = new TaffyItemEssentialsSection();
        private readonly TaffyInspectorSection[] _authoringSections =
        {
            new TaffyItemDisplaySection(),
            new TaffyItemPositionSizeSection(),
            new TaffyItemBoxModelSection(),
            new TaffyItemFlexSection(),
            new TaffyItemGridSection(),
            new TaffyItemBlockSection(),
            new TaffyItemMeasurementSection(),
        };

        private readonly TaffyItemPostSection _postSection = new TaffyItemPostSection();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var context = new TaffyInspectorContext(this);
            TaffyEditorGUI.DrawScript(serializedObject);
            TaffyEditorGUI.DrawInspectorMode();
            _parentSummarySection.Draw(context);
            _essentialsSection.Draw(context);

            if (context.IsSimpleMode)
            {
                TaffyEditorGUI.DrawSimpleModeHint();
            }
            else
            {
                for (int i = 0; i < _authoringSections.Length; i++)
                    _authoringSections[i].Draw(context);
            }

            serializedObject.ApplyModifiedProperties();
            _postSection.Draw(context);
        }
    }
}
