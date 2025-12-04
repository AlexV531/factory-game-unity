using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor
{
    [CustomEditor(typeof(VoiceProximityReceiptTrigger), editorForChildClasses: true)]
    public class VoiceProximityReceiptTriggerEditor
        : UnityEditor.Editor
    {
        private Texture2D _logo;

        private readonly TokenControl _tokenEditor = new TokenControl("This receipt trigger will only receive voice if the local player has at least one of these access tokens");

        private SerializedProperty _roomExpanded;
        private SerializedProperty _tokensExpanded;
        private SerializedProperty _colliderExpanded;

        private SerializedProperty _range;

        public void Awake()
        {
            _logo = Resources.Load<Texture2D>("dissonance_logo");
        }

        public void OnEnable()
        {
            _roomExpanded = serializedObject.FindProperty("_roomExpanded");
            _tokensExpanded = serializedObject.FindProperty("_tokensExpanded");
            _colliderExpanded = serializedObject.FindProperty("_colliderExpanded");

            _range = serializedObject.FindProperty("_range");
        }

        public override void OnInspectorGUI()
        {
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                GUILayout.Label(_logo);

                var receiver = (VoiceProximityReceiptTrigger)target;
                GuiHelpers.FoldoutBoxGroup(_roomExpanded, "Room", RoomsGui, receiver);
                GuiHelpers.FoldoutBoxGroup(_tokensExpanded, "Access Tokens", _tokenEditor.DrawInspectorGui, receiver);
                GuiHelpers.FoldoutBoxGroup(_colliderExpanded, "Collider Activation", VoiceReceiptTriggerEditor.TriggerActivationGui, receiver);

                Undo.FlushUndoRecordObjects();
                if (changed.changed)
                    EditorUtility.SetDirty(target);
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private void RoomsGui(VoiceProximityReceiptTrigger trigger)
        {
            VoiceReceiptTriggerEditor.RoomsGui(trigger);
            EditorGUILayout.PropertyField(_range);
            EditorGUILayout.HelpBox("'Range' must be the same for all VoiceProximityBroadcast and VoiceProximityReceipt triggers using the same room name", MessageType.Info);
        }
    }
}
