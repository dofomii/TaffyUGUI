using System;
using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Editor
{
    internal sealed class TaffyInspectorContext
    {
        internal TaffyInspectorContext(UnityEditor.Editor editor)
        {
            Editor = editor ?? throw new ArgumentNullException(nameof(editor));
            SerializedObject = editor.serializedObject;
            Targets = editor.targets ?? Array.Empty<UnityEngine.Object>();
            PrimaryTarget = editor.target;
        }

        internal UnityEditor.Editor Editor { get; }
        internal SerializedObject SerializedObject { get; }
        internal UnityEngine.Object[] Targets { get; }
        internal UnityEngine.Object PrimaryTarget { get; }
        internal bool IsMultiEditing => Targets.Length > 1;
        internal TaffyLayoutGroup Group => PrimaryTarget as TaffyLayoutGroup;
        internal TaffyLayoutItem Item => PrimaryTarget as TaffyLayoutItem;

        internal TaffyLayoutGroup ParentGroup
        {
            get
            {
                TaffyLayoutItem item = Item;
                return item ? item.GetComponentInParent<TaffyLayoutGroup>() : null;

        }

   }
        internal TaffyContainerDisplay ResolvedAuthoringDisplay
        {
            get
            {
                SerializedProperty property = SerializedObject.FindProperty("containerDisplay");
                if (property != null && property.propertyType == SerializedPropertyType.Enum)
                    return (TaffyContainerDisplay)property.intValue;

                TaffyLayoutGroup parent = ParentGroup;
                return parent ? parent.containerDisplay : TaffyContainerDisplay.Flex;
            }
        }

        internal SerializedProperty Find(string propertyName)
        {
            return SerializedObject.FindProperty(propertyName);
        }
    }
}
