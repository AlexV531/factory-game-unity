#if !NCRUNCH
using System;
using System.Collections.Generic;
using System.Linq;
using Dissonance.Config;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor
{
    [CustomEditor(typeof(VoiceBroadcastTrigger), editorForChildClasses: true)]
    public class VoiceBroadcastTriggerEditor
        : UnityEditor.Editor
    {
        private Texture2D _logo;

        private readonly TokenControl _tokenEditor = new TokenControl("This broadcast trigger will only send voice if the local player has at least one of these access tokens");

        private SerializedProperty _channelTypeExpanded;
        private SerializedProperty _metadataExpanded;
        private SerializedProperty _activationModeExpanded;
        private SerializedProperty _tokensExpanded;
        private SerializedProperty _ampExpanded;

        public void Awake()
        {
            _logo = Resources.Load<Texture2D>("dissonance_logo");
        }

        private void OnEnable()
        {
            _channelTypeExpanded = serializedObject.FindProperty("_channelTypeExpanded");
            _metadataExpanded = serializedObject.FindProperty("_metadataExpanded");
            _activationModeExpanded = serializedObject.FindProperty("_activationModeExpanded");
            _tokensExpanded = serializedObject.FindProperty("_tokensExpanded");
            _ampExpanded = serializedObject.FindProperty("_ampExpanded");
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

                var transmitter = (VoiceBroadcastTrigger)target;

                GuiHelpers.FoldoutBoxGroup(_channelTypeExpanded, "Channel Type", ChannelTypeGui, transmitter);
                GuiHelpers.FoldoutBoxGroup(_metadataExpanded, "Channel Metadata", MetadataGui, transmitter);
                GuiHelpers.FoldoutBoxGroup(_activationModeExpanded, "Activation Mode", ActivationGui, transmitter);
                GuiHelpers.FoldoutBoxGroup(_tokensExpanded, "Access Tokens", _tokenEditor.DrawInspectorGui, transmitter);
                GuiHelpers.FoldoutBoxGroup(_ampExpanded, "Amplitude Faders", VolumeGui, transmitter);

                Undo.FlushUndoRecordObjects();

                if (changed.changed)
                    EditorUtility.SetDirty(target);
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void ChannelTypeGui([NotNull] VoiceBroadcastTrigger transmitter)
        {
            transmitter.ChangeWithUndo(
                "Changed Dissonance Channel Type",
                (CommTriggerTarget)EditorGUILayout.EnumPopup(new GUIContent("Channel Type", "Where this trigger sends voice to"), transmitter.ChannelType),
                transmitter.ChannelType,
                a => transmitter.ChannelType = a
            );

            if (transmitter.ChannelType == CommTriggerTarget.Player)
            {
                transmitter.ChangeWithUndo(
                    "Changed Dissonance Channel Transmitter Player Name",
                    EditorGUILayout.TextField(new GUIContent("Recipient Player Name", "The name of the player receiving voice from this trigger"), transmitter.PlayerId),
                    transmitter.PlayerId,
                    a => transmitter.PlayerId = a
                );
            }

            if (transmitter.ChannelType == CommTriggerTarget.Room)
            {
                RoomTypeGui(transmitter);
            }

            if (transmitter.ChannelType == CommTriggerTarget.Self)
            {
                EditorGUILayout.HelpBox(
                    "Self mode sends voice data to the DissonancePlayer attached to this game object.",
                    MessageType.None
                );

                var player = transmitter.GetComponent<IDissonancePlayer>() ?? transmitter.GetComponentInParent<IDissonancePlayer>();
                if (player == null)
                {
                    EditorGUILayout.HelpBox(
                        "This GameObject (and it's parent) does not have a Dissonance player component!",
                        MessageType.Error
                    );
                }
                else
                {
                    if (EditorApplication.isPlaying)
                    {
                        if (!player.IsTracking)
                        {
                            EditorGUILayout.HelpBox(
                                "The trigger is disabled because the player tracker script is not yet tracking the player",
                                MessageType.Warning
                            );
                        }

                        if (player.Type == NetworkPlayerType.Local)
                        {
                            EditorGUILayout.HelpBox(
                                "This trigger is disabled because the player tracker script represents the local player (cannot send voice to yourself).",
                                MessageType.Info
                            );
                        }

                        if (player.IsTracking && player.Type == NetworkPlayerType.Unknown)
                        {
                            EditorGUILayout.HelpBox(
                                "This trigger is disabled because the player tracker script is tracking an 'Unknown' player type. This is probably a bug in your player tracker script.",
                                MessageType.Error
                            );
                        }
                    }
                }
            }
        }

        internal static void RoomTypeGui<T>(T transmitter)
            where T : MonoBehaviour, IVoiceBroadcastTrigger
        {
            var roomNames = ChatRoomSettings.Load().Names;

            var haveRooms = roomNames.Count > 0;
            if (haveRooms)
            {
                var roomList = new List<string>(roomNames);
                var roomIndex = roomList.IndexOf(transmitter.RoomName);

                using (new EditorGUILayout.HorizontalScope())
                {
                    // Detect if the room name is not null, and is also not in the list. This implies the room has been deleted from the room list.
                    // If this is the case insert it into our temporary copy of the room names list
                    if (roomIndex == -1 && !string.IsNullOrEmpty(transmitter.RoomName))
                    {
                        roomList.Insert(0, transmitter.RoomName);
                        roomIndex = 0;
                    }

                    transmitter.ChangeWithUndo(
                        "Changed Dissonance Transmitter Room",
                        EditorGUILayout.Popup(new GUIContent("Chat Room", "The room to send voice to"), roomIndex, roomList.Select(a => new GUIContent(a)).ToArray()),
                        roomIndex,
                        a => transmitter.RoomName = roomList[a]
                    );

                    if (GUILayout.Button("Config Rooms"))
                        ChatRoomSettingsEditor.GoToSettings();
                }

                if (string.IsNullOrEmpty(transmitter.RoomName))
                    EditorGUILayout.HelpBox("No chat room selected", MessageType.Error);
            }
            else
            {
                if (GUILayout.Button("Create New Rooms"))
                    ChatRoomSettingsEditor.GoToSettings();
            }

            if (!haveRooms)
                EditorGUILayout.HelpBox("No rooms are defined. Click 'Create New Rooms' to configure chat rooms.", MessageType.Warning);
        }

        private static void MetadataGui([NotNull] VoiceBroadcastTrigger transmitter)
        {
            transmitter.ChangeWithUndo(
                "Changed Dissonance Positional Audio",
                EditorGUILayout.Toggle(new GUIContent("Use Positional Data", "If voices sent with this trigger should be played with 3D playback"), transmitter.BroadcastPosition),
                transmitter.BroadcastPosition,
                a => transmitter.BroadcastPosition = a
            );

            if (!transmitter.BroadcastPosition)
            {
                EditorGUILayout.HelpBox(
                    "Send audio on this channel with positional data to allow 3D playback if set up on the receiving end. There is no performance cost to enabling this.\n\n" +
                    "Please see the Dissonance documentation for instructions on how to set your project up for playback of 3D voice comms.",
                    MessageType.Info);
            }

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

        private static void ActivationGui([NotNull] VoiceBroadcastTrigger transmitter)
        {
            transmitter.ChangeWithUndo(
                "Changed Dissonance Broadcast Trigger Mute",
                EditorGUILayout.Toggle(new GUIContent("Mute", "If this trigger is prevented from sending any audio"), transmitter.IsMuted),
                transmitter.IsMuted,
                a => transmitter.IsMuted = a
            );

            ActivationModeGui(transmitter);
            VolumeTriggerActivationGui(transmitter);
        }

        internal static void ActivationModeGui<T>(T transmitter)
            where T : MonoBehaviour, IVoiceBroadcastTrigger
        {
            transmitter.ChangeWithUndo(
                "Changed Dissonance Activation Mode",
                (CommActivationMode)EditorGUILayout.EnumPopup(new GUIContent("Activation Mode", "How the user should indicate an intention to speak"), transmitter.Mode),
                transmitter.Mode,
                m => transmitter.Mode = m
            );

            if (transmitter.Mode == CommActivationMode.None)
            {
                EditorGUILayout.HelpBox(
                    "While in this mode no voice will ever be transmitted",
                    MessageType.Info
                );
            }

            if (transmitter.Mode == CommActivationMode.PushToTalk)
            {
                transmitter.ChangeWithUndo(
                    "Changed Dissonance Push To Talk Axis",
                    EditorGUILayout.TextField(new GUIContent("Input Axis Name", "Which input axis indicates the user is speaking"), transmitter.InputName),
                    transmitter.InputName,
                    a => transmitter.InputName = a
                );

                // For some reason `Input.GetAxis` breaks the UI layout in Unity 6, even when it's wrapped in the try/catch!
                // Workaround that by always displaying a tip, instead of contextually displaying an error.
#if UNITY_6000_0_OR_NEWER
                EditorGUILayout.HelpBox($"Ensure axis '{transmitter.InputName}' exists in the Input Manager (Edit > Project Settings > Input Manager)", MessageType.Info);
#else
                try
                {
                    Input.GetAxis(transmitter.InputName);
                }
                catch
                {
                    EditorGUILayout.HelpBox($"Input axis '{transmitter.InputName}' does not exist. Create it in the Input Manager (Edit > Project Settings > Input Manager)", MessageType.Error);
                }
#endif
            }
        }

        internal static void VolumeTriggerActivationGui(BaseCommsTrigger trigger)
        {
            trigger.ChangeWithUndo(
                "Changed Dissonance Collider Activation",
                EditorGUILayout.Toggle(new GUIContent("Collider Activation", "Only allows speech when the user is inside a collider"), trigger.UseColliderTrigger),
                trigger.UseColliderTrigger,
                u => trigger.UseColliderTrigger = u
            );

            if (trigger.UseColliderTrigger)
            {
                var triggers2D = trigger.gameObject.GetComponents<Collider2D>().Any(c => c.isTrigger);
                var triggers3D = trigger.gameObject.GetComponents<Collider>().Any(c => c.isTrigger);
                if (!triggers2D && !triggers3D)
                    EditorGUILayout.HelpBox("Cannot find any collider components with 'isTrigger = true' attached to this GameObject.", MessageType.Warning);
            }
        }

        private static void VolumeGui([NotNull] VoiceBroadcastTrigger transmitter)
        {
            if (EditorApplication.isPlaying)
            {
                var currentDb = Helpers.ToDecibels(transmitter.CurrentFaderVolume);
                EditorGUILayout.Slider("Current Gain (dB)", currentDb, Helpers.MinDecibels, Math.Max(10, currentDb));
                EditorGUILayout.Space();
            }

            EditorGUILayout.LabelField(new GUIContent($"{transmitter.Mode} Fade", $"Fade when {transmitter.Mode} mode changes"));
            SingleFaderGui(transmitter, transmitter.ActivationFader);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledGroupScope(!transmitter.UseColliderTrigger))
            {
                EditorGUILayout.LabelField(new GUIContent("Volume Trigger Fade", "Fade when when entering/exiting collider volume trigger"));
                SingleFaderGui(transmitter, transmitter.ColliderTriggerFader);
            }
        }

        private static void SingleFaderGui([NotNull] VoiceBroadcastTrigger transmitter, [NotNull] VolumeFaderSettings settings)
        {
            transmitter.ChangeWithUndo(
                "Changed Dissonance Collider Trigger Volume",
                Helpers.FromDecibels(EditorGUILayout.Slider(new GUIContent("Channel Volume (dB)", "Amplification for voice sent from this trigger"), Helpers.ToDecibels(settings.Volume), Helpers.MinDecibels, 10)),
                //EditorGUILayout.Slider(new GUIContent("Channel Volume (VMUL)", "Volume multiplier for voice sent from this trigger"), settings.Volume, 0, 4),
                settings.Volume,
                a => settings.Volume = a
            );

            transmitter.ChangeWithUndo(
                "Changed Dissonance Trigger Fade In Time",
                EditorGUILayout.Slider(new GUIContent("Fade In Time", "Duration (seconds) for voice take to reach full volume"), (float)settings.FadeIn.TotalSeconds, 0, 3),
                settings.FadeIn.TotalSeconds,
                a => settings.FadeIn = TimeSpan.FromSeconds(a)
            );

            transmitter.ChangeWithUndo(
                "Changed Dissonance Trigger Fade Out Time",
                EditorGUILayout.Slider(new GUIContent("Fade Out Time", "Duration (seconds) for voice to fade to silent and stop transmitting"), (float)settings.FadeOut.TotalSeconds, 0, 3),
                settings.FadeOut.TotalSeconds,
                a => settings.FadeOut = TimeSpan.FromSeconds(a)
            );
        }
    }
}
#endif
