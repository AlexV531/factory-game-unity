using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor
{
    public class TokenControl
    {
        private readonly string _hint;

        private string _proposedToken = "New Token";

        public TokenControl(string hint)
        {
            _hint = hint;
        }

        public void DrawInspectorGui<T>(T target)
            where T : Object, IAccessTokenCollection
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.HelpBox(_hint, MessageType.Info);

                var tokensToRemove = new List<string>();
                foreach (var token in target.Tokens)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(token);
                        if (GUILayout.Button("Delete", GUILayout.MaxWidth(50)))
                        {
                            tokensToRemove.Add(token);
                            _proposedToken = "New Token";
                        }
                    }
                }

                foreach (var token in tokensToRemove)
                {
                    Undo.RecordObject(target, "Removed Dissonance Access Token");
                    target.RemoveToken(token);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _proposedToken = EditorGUILayout.TextField(_proposedToken);
                    if (GUILayout.Button("Add Token"))
                    {
                        Undo.RecordObject(target, "Added Dissonance Access Token");

                        target.AddToken(_proposedToken);
                        _proposedToken = "New Token";
                    }
                }
            }
        }
    }
}
