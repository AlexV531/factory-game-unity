using Dissonance.Integrations.Offline;
using UnityEditor;
using UnityEngine;

namespace Assets.Dissonance.Integrations.Offline.Editor
{
    [CustomEditor(typeof(OfflineCommsNetwork))]
    public class OfflineCommsNetworkEditor
        : UnityEditor.Editor
    {
        private Texture2D _logo;

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            if (_logo == null)
                _logo = Resources.Load<Texture2D>("dissonance_logo");

            GUILayout.Label(_logo);

            EditorGUILayout.HelpBox("This comms network pretends to always be connected, which forces Dissonance to run the audio recording pipeline. " +
                                    "Create a VoiceBroadcastTrigger and a VoiceReceiptTrigger for a room named \"Loopback\" to hear yourself talking." +
                                    "\n\n" +
                                    "This can be used in menu scenes for mic configuration etc.", MessageType.Info);

            EditorGUILayout.LabelField("Loopback Packet Count", ((OfflineCommsNetwork)target).LoopbackPacketCount.ToString());
        }
    }
}