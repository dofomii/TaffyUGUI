using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Editor
{
    internal abstract class TaffyInspectorSection
    {
        private readonly string _inspectorKey;
        private readonly string _sectionKey;
        private readonly GUIContent _title;
        private readonly bool _useFoldout;
        private readonly bool _showHeader;
        private readonly bool _defaultExpanded;

        protected TaffyInspectorSection(
            string inspectorKey,
            string sectionKey,
            GUIContent title,
            bool useFoldout,
            bool showHeader = true,
            bool defaultExpanded = true)
        {
            _inspectorKey = inspectorKey;
            _sectionKey = sectionKey;
            _title = title ?? GUIContent.none;
            _useFoldout = useFoldout;
            _showHeader = showHeader;
            _defaultExpanded = defaultExpanded;
        }

        internal virtual bool IsRelevant(TaffyInspectorContext context)
        {
            return context != null;
        }

        internal virtual string GetSummary(TaffyInspectorContext context)
        {
            return null;
        }

        internal void Draw(TaffyInspectorContext context)
        {
            if (!IsRelevant(context))
                return;

            if (!_showHeader)
            {
                DrawContent(context);
                return;
            }

            if (!_useFoldout)
            {
                TaffyEditorGUI.DrawSectionLabel(_title);
                DrawContent(context);
                return;
            }

            string summary = GetSummary(context);
            var header = new GUIContent(TaffyEditorGUI.WithSummary(_title.text, summary), _title.tooltip);
            bool expanded = TaffyEditorPreferences.GetFoldout(_inspectorKey, _sectionKey, _defaultExpanded);
            bool next = EditorGUILayout.BeginFoldoutHeaderGroup(expanded, header);
            if (next != expanded)
                TaffyEditorPreferences.SetFoldout(_inspectorKey, _sectionKey, next);
            if (next)
                DrawContent(context);
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        protected abstract void DrawContent(TaffyInspectorContext context);
    }
}
