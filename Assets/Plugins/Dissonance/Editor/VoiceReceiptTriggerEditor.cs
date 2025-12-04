#if !NCRUNCH
using System.Collections.Generic;
using System.Linq;
using Dissonance.Config;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor
{
    [CustomEditor(typeof(VoiceReceiptTrigger), editorForChildClasses: true)]
    public class VoiceReceiptTriggerEditor
        : UnityEditor.Editor
    {
        private Texture2D _logo;

        private readonly TokenControl _tokenEditor = new TokenControl("This receipt trigger will only receive voice if the local player has at least one of these access tokens");

        private SerializedProperty _roomExpanded;
        private SerializedProperty _tokensExpanded;
        private SerializedProperty _colliderExpanded;

        public void Awake()
        {
            _logo = Resources.Load<Texture2D>("dissonance_logo");
        }

        public void OnEnable()
        {
            _roomExpanded = serializedObject.FindProperty("_roomExpanded");
            _tokensExpanded = serializedObject.FindProperty("_tokensExpanded");
            _colliderExpanded = serializedObject.FindProperty("_colliderExpanded");
        }

        public override void OnInspectorGUI()
        {
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                GUILayout.Label(_logo);

                var receiver = (VoiceReceiptTrigger)target;
                GuiHelpers.FoldoutBoxGroup(_roomExpanded, "Room", RoomsGui, receiver);
                GuiHelpers.FoldoutBoxGroup(_tokensExpanded, "Access Tokens", _tokenEditor.DrawInspectorGui, receiver);
                GuiHelpers.FoldoutBoxGroup(_colliderExpanded, "Collider Activation", TriggerActivationGui, receiver);

                Undo.FlushUndoRecordObjects();
                if (changed.changed)
                    EditorUtility.SetDirty(target);
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        internal static void RoomsGui<T>([NotNull] T trigger)
            where T : MonoBehaviour, IVoiceReceiptTrigger
        {
            var roomNames = ChatRoomSettings.Load().Names;

            var haveRooms = roomNames.Count > 0;
            if (haveRooms)
            {
                var roomList = new List<string>(roomNames);
                var roomIndex = roomList.IndexOf(trigger.RoomName);
                var deadRoom = false;

                using (new EditorGUILayout.HorizontalScope())
                {
                    //Detect if the room name is not null, and is also not in the list. This implies the room has been deleted from the room list. In this case 
                    if (roomIndex == -1 && !string.IsNullOrEmpty(trigger.RoomName))
                    {
                        roomList.Insert(0, trigger.RoomName);
                        roomIndex = 0;
                        deadRoom = true;
                    }

                    trigger.ChangeWithUndo(
                        "Changed Dissonance Receiver Room",
                        EditorGUILayout.Popup(new GUIContent("Chat Room", "The room to receive voice from"), roomIndex, roomList.Select(a => new GUIContent(a)).ToArray()),
                        roomIndex,
                        a => trigger.RoomName = roomList[a]
                    );

                    if (GUILayout.Button("Config Rooms"))
                        ChatRoomSettingsEditor.GoToSettings();
                }

                if (deadRoom)
                    EditorGUILayout.HelpBox(string.Format("Room '{0}' is no longer defined in the chat room configuration! \nRe-create the '{0}' room, or select a different room.", trigger.RoomName), MessageType.Warning);
                else if (string.IsNullOrEmpty(trigger.RoomName))
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

        internal static void TriggerActivationGui([NotNull] BaseCommsTrigger trigger)
        {
            trigger.ChangeWithUndo(
                "Changed Dissonance Collider Activation",
                EditorGUILayout.Toggle(new GUIContent("Collider Activation", "Only allows speech when the user is inside a collider"), trigger.UseColliderTrigger),
                trigger.UseColliderTrigger,
                u => trigger.UseColliderTrigger = u
            );

            EditorGUILayout.HelpBox(
                "Use collider activation to only receive when the player is inside a collider.",
                MessageType.Info
            );

            if (trigger.UseColliderTrigger)
            {
                var triggers2D = trigger.gameObject.GetComponents<Collider2D>().Any(c => c.isTrigger);
                var triggers3D = trigger.gameObject.GetComponents<Collider>().Any(c => c.isTrigger);
                if (!triggers2D && !triggers3D)
                    EditorGUILayout.HelpBox("Cannot find any collider components with 'isTrigger = true' attached to this GameObject.", MessageType.Warning);
            }
        }
    }
}
#endif