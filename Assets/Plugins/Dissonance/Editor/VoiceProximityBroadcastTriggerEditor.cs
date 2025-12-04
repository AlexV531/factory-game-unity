#if !NCRUNCH
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor
{
    [CustomEditor(typeof(VoiceProximityBroadcastTrigger), editorForChildClasses: true)]
    public class VoiceProximityBroadcastTriggerEditor
        : UnityEditor.Editor
    {
        private Texture2D _logo;

        private readonly TokenControl _tokenEditor = new TokenControl("This broadcast trigger will only send voice if the local player has at least one of these access tokens");

        private SerializedProperty _roomExpanded;
        private SerializedProperty _metadataExpanded;
        private SerializedProperty _activationModeExpanded;
        private SerializedProperty _tokensExpanded;

        private SerializedProperty _range;

        public void Awake()
        {
            _logo = Resources.Load<Texture2D>("dissonance_logo");
        }

        private void OnEnable()
        {
            _roomExpanded = serializedObject.FindProperty("_roomExpanded");
            _metadataExpanded = serializedObject.FindProperty("_metadataExpanded");
            _activationModeExpanded = serializedObject.FindProperty("_activationModeExpanded");
            _tokensExpanded = serializedObject.FindProperty("_tokensExpanded");

            _range = serializedObject.FindProperty("_range");
        }

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                GUILayout.Label(_logo);

                var transmitter = (VoiceProximityBroadcastTrigger)target;

                GuiHelpers.FoldoutBoxGroup(_roomExpanded, "Room", RoomsGui, transmitter);
                GuiHelpers.FoldoutBoxGroup(_metadataExpanded, "Channel Metadata", MetadataGui, transmitter);
                GuiHelpers.FoldoutBoxGroup(_activationModeExpanded, "Activation Mode", ActivationModeGui, transmitter);
                GuiHelpers.FoldoutBoxGroup(_tokensExpanded, "Access Tokens", _tokenEditor.DrawInspectorGui, transmitter);

                Undo.FlushUndoRecordObjects();

                if (changed.changed)
                    EditorUtility.SetDirty(target);
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private void RoomsGui(VoiceProximityBroadcastTrigger trigger)
        {
            VoiceBroadcastTriggerEditor.RoomTypeGui(trigger);
            EditorGUILayout.PropertyField(_range);
            EditorGUILayout.HelpBox("'Range' must be the same for all VoiceProximityBroadcast and VoiceProximityReceipt triggers using the same room name", MessageType.Info);
        }

        private static void MetadataGui([NotNull] VoiceProximityBroadcastTrigger transmitter)
        {
            transmitter.ChangeWithUndo(
                "Changed Dissonance Channel Priority",
                (ChannelPriority)EditorGUILayout.EnumPopup(new GUIContent("Priority", "Priority for speech sent through this trigger"), transmitter.Priority),
                transmitter.Priority,
                a => transmitter.Priority = a
            );

            if (transmitter.Priority == ChannelPriority.None)
            {
                EditorGUILayout.HelpBox(
                    "Priority for the voice sent from this room. Voices will mute all lower priority voices on the receiver while they are speaking.\n\n" +
                    "'None' means that this room specifies no particular priority and the priority of this player will be used instead",
                    MessageType.Info);
            }
        }

        private static void ActivationModeGui([NotNull] VoiceProximityBroadcastTrigger transmitter)
        {
            transmitter.ChangeWithUndo(
                "Changed Dissonance Broadcast Trigger Mute",
                EditorGUILayout.Toggle(new GUIContent("Mute", "If this trigger is prevented from sending any audio"), transmitter.IsMuted),
                transmitter.IsMuted,
                a => transmitter.IsMuted = a
            );

            VoiceBroadcastTriggerEditor.ActivationModeGui(transmitter);
            VoiceBroadcastTriggerEditor.VolumeTriggerActivationGui(transmitter);
        }
    }
}
#endif
