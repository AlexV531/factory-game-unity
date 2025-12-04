using JetBrains.Annotations;
using System;
using UnityEditor;

namespace Dissonance.Editor
{
    internal static class GuiHelpers
    {
        internal static void FoldoutBoxGroup<T>([NotNull] SerializedProperty expanded, string title, Action<T> gui, T trigger)
        {
            expanded.boolValue = EditorGUILayout.Foldout(expanded.boolValue, title);
            if (expanded.boolValue)
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    gui(trigger);
        }
    }
}
