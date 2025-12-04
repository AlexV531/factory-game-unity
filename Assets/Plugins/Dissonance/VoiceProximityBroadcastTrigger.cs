using Dissonance.VAD;
using UnityEngine;

namespace Dissonance
{
    /// <summary>
    /// Sends voice to nearby rooms in an infinite grid of rooms
    /// </summary>
    [HelpURL("https://placeholder-software.co.uk/dissonance/docs/Reference/Components/Voice-Proximity-Broadcast-Trigger/")]
    public class VoiceProximityBroadcastTrigger
        : BaseProximityTrigger<RoomChannel>, IVoiceActivationListener, IVoiceBroadcastTrigger
    {
        #region field and properties
#pragma warning disable CS0414
        [SerializeField] private bool _roomExpanded = true;
        [SerializeField] private bool _metadataExpanded; // < These properties contain state used by the inspector. It needs
        [SerializeField] private bool _activationModeExpanded; // < to be stored here because the inspector is sometimes recreated
        [SerializeField] private bool _tokensExpanded; // < by the editor, discarding all state (for example, making things inside foldouts inaccessible!
#pragma warning restore CS0414

        private bool _isVadSpeaking;
        private CommActivationMode? _previousMode;

        [SerializeField] private string _inputName;

        /// <summary>
        /// Get or set the input axis name (only applicable if this trigger is using Push-To-Talk)
        /// </summary>
        public string InputName
        {
            get => _inputName;
            set => _inputName = value;
        }

        [SerializeField] private CommActivationMode _mode = CommActivationMode.VoiceActivation;

        /// <summary>
        /// Get or set how the player indicates speaking intent to this trigger
        /// </summary>
        public CommActivationMode Mode
        {
            get => _mode;
            set => _mode = value;
        }

        [SerializeField] private bool _muted;

        /// <summary>
        /// Get or set if this voice broadcast trigger is muted
        /// </summary>
        public bool IsMuted
        {
            get => _muted;
            set
            {
                if (value)
                    CloseChannels();
                Log.Debug("Mute Proximity Broadcast Trigger '{0}' = {1}", RoomName, value);
                _muted = value;
            }
        }

        /// <summary>
        /// Get if this voice broadcast trigger is currently transmitting voice
        /// </summary>
        public bool IsTransmitting => ActiveChannelCount != 0;

        [SerializeField] private ChannelPriority _prority = ChannelPriority.Default;

        public ChannelPriority Priority
        {
            get => _prority;
            set
            {
                _prority = value;
                CloseChannels();
            }
        }

        public override bool CanTrigger => !IsMuted && base.CanTrigger;
        #endregion

        /// <summary>
        /// Invert the `IsMuted` property. This is convenient for integration with UI elements.
        /// e.g. a UI button can directly call this while in `Voice Activated` mode to enable/disable voice on click.
        /// </summary>
        public void ToggleMute()
        {
            IsMuted = !IsMuted;
        }

        /// <summary>
        /// Get a value indicating if the user wants to speak
        /// </summary>
        /// <returns></returns>
        protected override bool IsUserActivated()
        {
            switch (Mode)
            {
                case CommActivationMode.VoiceActivation:
                    return _isVadSpeaking;

                case CommActivationMode.PushToTalk:
                    return Input.GetAxis(InputName) > 0.5f;

                case CommActivationMode.Open:
                    return true;

                case CommActivationMode.None:
                    return false;

                default:
                    Log.Error("Unknown Activation Mode '{0}'", Mode);
                    return false;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (Comms != null)
                Comms.SubscribeToVoiceActivation(this);
        }

        protected override void OnDisable()
        {
            if (Comms != null)
                Comms.UnsubscribeFromVoiceActivation(this);

            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            if (Comms != null)
                Comms.UnsubscribeFromVoiceActivation(this);

            base.OnDestroy();
        }

        protected override void Update()
        {
            // When the mode changes close all channels, they will be re-opened if necessary
            if (_mode != _previousMode)
                CloseChannels();
            _previousMode = _mode;

            base.Update();
        }

        private class BroadcastGrid
            : Grid
        {
            private readonly VoiceProximityBroadcastTrigger _parent;

            public BroadcastGrid(VoiceProximityBroadcastTrigger parent)
                : base(parent)
            {
                _parent = parent;
            }

            protected override RoomChannel CreateHandle(Vector3Int id, string name)
            {
                return Parent.Comms.RoomChannels.Open(new RoomName(name, true), true, _parent.Priority);
            }

            protected override void CloseHandle(RoomChannel handle)
            {
                handle.Dispose();
            }
        }

        protected override Grid CreateGrid()
        {
            return new BroadcastGrid(this);
        }

        #region IVoiceActivationListener impl

        void IVoiceActivationListener.VoiceActivationStart()
        {
            _isVadSpeaking = true;
        }

        void IVoiceActivationListener.VoiceActivationStop()
        {
            _isVadSpeaking = false;
        }

        #endregion
    }
}