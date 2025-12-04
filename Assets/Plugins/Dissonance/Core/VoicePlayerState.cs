using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dissonance.Audio.Capture;
using Dissonance.Audio.Playback;
using Dissonance.Networking;
using JetBrains.Annotations;
using UnityEngine;

namespace Dissonance
{
    /// <summary>
    /// The state of a player in a Dissonance session
    /// </summary>
    public abstract class VoicePlayerState
    {
        private static readonly Log Log = Logs.Create(LogCategory.Core, nameof(VoicePlayerState));

        /// <summary>
        /// Event which will be invoked whenever this player starts speaking
        /// </summary>
        // ReSharper disable once EventNeverSubscribedTo.Global (Justificiation: Public API)
        public event Action<VoicePlayerState> OnStartedSpeaking;

        /// <summary>
        /// Event which will be invoked whenever this player stops speaking
        /// </summary>
        // ReSharper disable once EventNeverSubscribedTo.Global (Justificiation: Public API)
        public event Action<VoicePlayerState> OnStoppedSpeaking;

        /// <summary>
        /// Event which will be invoked when this player enters a new room
        /// </summary>
        // ReSharper disable once EventNeverSubscribedTo.Global (Justificiation: Public API)
        public event Action<VoicePlayerState, string> OnEnteredRoom;

        /// <summary>
        /// Event which will be invoked when this player exits a room
        /// </summary>
        // ReSharper disable once EventNeverSubscribedTo.Global (Justificiation: Public API)
        public event Action<VoicePlayerState, string> OnExitedRoom;

        /// <summary>
        /// Event which will be invoked when this player leaves the session
        /// </summary>
        // ReSharper disable once EventNeverSubscribedTo.Global (Justificiation: Public API)
        public event Action<VoicePlayerState> OnLeftSession;

        #region constructor
        internal VoicePlayerState(string name)
        {
            Name = name;
        }
        #endregion

        #region properties
        /// <summary>
        /// Get the name of the player this object represents
        /// </summary>
        [NotNull] public string Name { get; }

        /// <summary>
        /// Get a value indicating if this player is connected to the session
        /// </summary>
        public abstract bool IsConnected { get; }

        /// <summary>
        /// Get a value indicating if this player is currently speaking
        /// </summary>
        public abstract bool IsSpeaking { get; }

        /// <summary>
        /// The current amplitude of the voice signal from this player
        /// </summary>
        public abstract float Amplitude { get; }

        /// <summary>
        /// Current priority of speech from this speaker (null if the speaker is not currently speaking)
        /// </summary>
        public abstract ChannelPriority? SpeakerPriority { get; }

        /// <summary>
        /// Get or set the volume which voice from this player should be played at (must be between 0 and 1)
        /// </summary>
        public abstract float Volume { get; set; }

        /// <summary>
        /// Get or set whether audio from this player is muted for the local player
        /// </summary>
        public abstract bool IsLocallyMuted { get; set; }

        /// <summary>
        /// Get the set of rooms this player is listening to
        /// </summary>
        [NotNull] public abstract ReadOnlyCollection<string> Rooms { get; }

        /// <summary>
        /// Get the voice playback instance for this player (may be null if this player has disconnected)
        /// </summary>
        [CanBeNull] public IVoicePlayback Playback => PlaybackInternal;

        /// <summary>
        /// Get the voice playback instance for this player (may be null if this player has disconnected)
        /// </summary>
        [CanBeNull] internal abstract IVoicePlaybackInternal PlaybackInternal { get; }

        /// <summary>
        /// Get the dissonance tracker associated with this player (may be null if no position tracking has been initialised for this player)
        /// </summary>
        [CanBeNull] public abstract IDissonancePlayer Tracker { get; internal set; }

        /// <summary>
        /// Get the estimated packet loss (0 to 1) to/from this player (may be null if the player has disconnected)
        /// </summary>
        [CanBeNull] public abstract float? PacketLoss { get; }

        /// <summary>
        /// Get a value indicating if this object represents the local player
        /// </summary>
        public abstract bool IsLocalPlayer { get; }
        #endregion

        #region event invokers
        internal void InvokeOnStoppedSpeaking()
        {
            PlaybackInternal?.StopPlayback();

            var evt = OnStoppedSpeaking;
            evt?.Invoke(this);
        }

        internal void InvokeOnStartedSpeaking()
        {
            PlaybackInternal?.StartPlayback();

            OnStartedSpeaking?.Invoke(this);
        }

        internal void InvokeOnLeftSession()
        {
            OnLeftSession?.Invoke(this);
        }

        internal virtual void InvokeOnEnteredRoom(RoomEvent evtData)
        {
            Log.AssertAndThrowPossibleBug(evtData.Joined, "FC760FE7-10D6-4572-B7D6-D33799D93FFD", "Passed leave event to join event handler");

            OnEnteredRoom?.Invoke(this, evtData.Room);
        }

        internal virtual void InvokeOnExitedRoom(RoomEvent evtData)
        {
            Log.AssertAndThrowPossibleBug(!evtData.Joined, "359A67D1-DE96-4181-B5FF-D4ED3B8C0DF0", "Passed join event to leave event handler");

            OnExitedRoom?.Invoke(this, evtData.Room);
        }
        #endregion

        /// <summary>
        /// Get the list of channels that this player is currently speaking to
        /// </summary>
        /// <param name="output"></param>
        public abstract void GetSpeakingChannels([NotNull] List<RemoteChannel> output);

        internal abstract void Update();
    }

    /// <inheritdoc />
    internal class LocalVoicePlayerState
        : VoicePlayerState
    {
        #region fields
        private static readonly Log Log = Logs.Create(LogCategory.Core, nameof(LocalVoicePlayerState));

        [NotNull] private readonly IAmplitudeProvider _micAmplitude;
        [NotNull] private readonly Rooms _rooms;
        [NotNull] private readonly RoomChannels _roomChannels;
        [NotNull] private readonly PlayerChannels _playerChannels;
        [NotNull] private readonly ILossEstimator _loss;
        [NotNull] private readonly ICommsNetwork _network;
        #endregion

        #region constructor
        public LocalVoicePlayerState(string name, [NotNull] IAmplitudeProvider micAmplitude, [NotNull] Rooms rooms, [NotNull] RoomChannels roomChannels, [NotNull] PlayerChannels playerChannels, [NotNull] ILossEstimator loss, [NotNull] ICommsNetwork network)
            : base(name)
        {
            _rooms = rooms;
            _micAmplitude = micAmplitude;
            _roomChannels = roomChannels;
            _playerChannels = playerChannels;
            _loss = loss;
            _network = network;

            rooms.JoinedRoom += OnLocallyEnteredRoom;
            rooms.LeftRoom += OnLocallyExitedRoom;
            roomChannels.OpenedChannel += OnChannelOpened;
            roomChannels.ClosedChannel += OnChannelClosed;
            playerChannels.OpenedChannel += OnChannelOpened;
            playerChannels.ClosedChannel += OnChannelClosed;
        }
        #endregion

        #region event invokers
        private void OnChannelOpened<TId>([NotNull] TId channel, ChannelProperties properties)
        {
            var count = _playerChannels.Count + _roomChannels.Count;
            if (count == 1)
            {
                Log.Debug("Local player started speaking");
                InvokeOnStartedSpeaking();
            }
        }

        private void OnChannelClosed<TId>([NotNull] TId channel, ChannelProperties properties)
        {
            var count = _playerChannels.Count + _roomChannels.Count;
            if (count == 0)
            {
                Log.Debug("Local player stopped speaking");
                InvokeOnStoppedSpeaking();
            }
        }

        private void OnLocallyEnteredRoom([NotNull] string room)
        {
            InvokeOnEnteredRoom(new RoomEvent(Name, room, true, Rooms));
        }

        private void OnLocallyExitedRoom([NotNull] string room)
        {
            InvokeOnExitedRoom(new RoomEvent(Name, room, false, Rooms));
        }
        #endregion

        #region properties
        public override bool IsConnected => _network.Status == ConnectionStatus.Connected;

        internal override IVoicePlaybackInternal PlaybackInternal => null;

        public override bool IsLocallyMuted
        {
            //Local microphone audio is never played through the local speakers - i.e. the local player is always locally muted
            get => true;

            set
            {
                if (!value)
                {
                    Log.Error(Log.UserErrorMessage(
                        "Attempted to Locally UnMute the local player",
                        "Setting `IsLocallyMuted = false` on the local player",
                        "https://placeholder-software.co.uk/dissonance/docs/Reference/Other/VoicePlayerState.html",
                        "BEF78918-1805-4D59-A071-74E7B38D13C8"
                    ));
                }
            }
        }

        public override ReadOnlyCollection<string> Rooms => _rooms.Memberships;

        public override IDissonancePlayer Tracker { get; internal set; }

        public override float Amplitude => _micAmplitude.Amplitude;

        public override ChannelPriority? SpeakerPriority => null;

        public override float Volume
        {
            get => 1;

            // ReSharper disable once ValueParameterNotUsed (Justification this property isn't supported)
            set
            {
                Log.Error(Log.UserErrorMessage(
                    "Attempted to set playback volume of local player",
                    "Setting `Volume = value` on the local player",
                    "https://placeholder-software.co.uk/dissonance/docs/Reference/Other/VoicePlayerState.html",
                    "9822EFB8-1A4A-4F54-9A32-5F183AE8D4DE"
                ));
            }
        }

        public override bool IsSpeaking => _roomChannels.Count > 0 || _playerChannels.Count > 0;

        public override float? PacketLoss => _loss.PacketLoss;

        public override bool IsLocalPlayer => true;

        #endregion

        public override void GetSpeakingChannels(List<RemoteChannel> channels)
        {
            // Check that the `channels` parameter is not null
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            Log.AssertAndThrowUserError(
                channels != null,
                "`channels` parameter is null",
                "Passing a null object when calling `GetSpeakingChannels`",
                "https://placeholder-software.co.uk/dissonance/docs/Reference/Other/VoicePlayerState.html#getspeakingchannelschannels-listremotechannel",
                "6F103E91-E8BD-40B0-8690-CFAE2FB801A5"
            );

            //In both these the enumerator is a struct, so this does not allocate

            using (var enumerator = _roomChannels.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var item = enumerator.Current.Value;
                    channels.Add(CreateRemoteChannel<RoomChannel, RoomName>(enumerator.Current.Value, item.TargetId, ChannelType.Room));
                }
            }

            using (var enumerator = _playerChannels.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var item = enumerator.Current.Value;
                    channels.Add(CreateRemoteChannel<PlayerChannel, string>(enumerator.Current.Value, item.TargetId, ChannelType.Player));
                }
            }
        }

        private static RemoteChannel CreateRemoteChannel<TChannel, TId>([NotNull] TChannel item, string name, ChannelType type)
            where TChannel : IChannel<TId>
        {
            return new RemoteChannel(
                name,
                type,
                new PlaybackOptions(item.Properties.Positional, item.Properties.AmplitudeMultiplier, item.Properties.TransmitPriority)
            );
        }

        internal override void Update()
        {
        }
    }

    /// <inheritdoc />
    internal class RemoteVoicePlayerState
        : VoicePlayerState
    {
        #region fields
        private static readonly Log Log = Logs.Create(LogCategory.Core, nameof(RemoteVoicePlayerState));

        private readonly IVoicePlaybackInternal _playback;
        private IDissonancePlayer _player;

        private static readonly ReadOnlyCollection<string> EmptyRoomsList = new ReadOnlyCollection<string>(new List<string>(0));
        private ReadOnlyCollection<string> _rooms;
        #endregion

        #region constructor
        internal RemoteVoicePlayerState([NotNull] IVoicePlaybackInternal playback)
            : base(playback.PlayerName)
        {
            _playback = playback;
            _playback.Reset();
        }
        #endregion

        #region properties
        public override bool IsConnected
        {
            get
            {
                //We're checking three things here:
                // 1. We know playback is not null (it's readonly and assigned non-null in the constructor), but Unity may be pretending it's null!
                // 1. If playback is inactive then this player has disconnected
                // 2. If playback has a different name it's been reassigned to another player (and this one must have disconnected)
                return ((UnityEngine.Object)_playback) != null && _playback.IsActive && _playback.PlayerName == Name;
            }
        }

        public override bool IsSpeaking => IsConnected && _playback.IsSpeaking;

        public override float Amplitude => IsConnected ? _playback.Amplitude : 0;

        public override float Volume
        {
            get
            {
                var p = PlaybackInternal;
                return p?.PlaybackVolume ?? 0;
            }
            set
            {
                if (value < 0 || value > 1)
                    throw new ArgumentOutOfRangeException(nameof(value), "Volume must be between 0 and 1");

                var p = PlaybackInternal;
                if (p != null)
                    p.PlaybackVolume = value;
            }
        }

        public override ChannelPriority? SpeakerPriority
        {
            get
            {
                var playback = PlaybackInternal;
                if (playback != null && playback.IsSpeaking && !playback.IsMuted)
                    return playback.Priority;

                return null;
            }
        }

        internal override IVoicePlaybackInternal PlaybackInternal => IsConnected ? _playback : null;

        public override bool IsLocallyMuted
        {
            get => IsConnected && _playback.IsMuted;
            set
            {
                var p = PlaybackInternal;

                if (!IsConnected || p == null)
                    Log.Warn("Attempted to (un)mute player {0}, but they are not connected", Name);
                else
                    p.IsMuted = value;
            }
        }

        public override ReadOnlyCollection<string> Rooms => _rooms ?? EmptyRoomsList;

        public override IDissonancePlayer Tracker
        {
            get => _player;
            internal set
            {
                _player = value;

                if (_playback.PlayerName == Name)
                {
                    _playback.AllowPositionalPlayback = value != null;

                    if (!_playback.AllowPositionalPlayback)
                        _playback.SetTransform(Vector3.zero, Quaternion.identity);
                }
            }
        }

        public override float? PacketLoss
        {
            get
            {
                var p = Playback;
                return p?.PacketLoss;
            }
        }

        public override bool IsLocalPlayer => false;

        internal float? Jitter
        {
            get
            {
                var p = Playback;
                return p?.Jitter;
            }
        }
        #endregion

        internal override void Update()
        {
            var p = PlaybackInternal;
            if (Tracker != null && p != null && Tracker.IsTracking)
                p.SetTransform(Tracker.Position, Tracker.Rotation);
        }

        public override void GetSpeakingChannels(List<RemoteChannel> channels)
        {
            // Check that the `channels` parameter is not null
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            Log.AssertAndThrowUserError(
                channels != null,
                "`channels` parameter is null",
                "Passing a null object when calling `GetSpeakingChannels`",
                "https://placeholder-software.co.uk/dissonance/docs/Reference/Other/VoicePlayerState.html#getspeakingchannelschannels-listremotechannel",
                "6F103E91-E8BD-40B0-8690-CFAE2FB801A5"
            );

            channels.Clear();

            ((IRemoteChannelProvider)Playback)?.GetRemoteChannels(channels);
        }

        #region event invokers
        internal override void InvokeOnEnteredRoom(RoomEvent evtData)
        {
            _rooms = evtData.Rooms;
            base.InvokeOnEnteredRoom(evtData);
        }

        internal override void InvokeOnExitedRoom(RoomEvent evtData)
        {
            _rooms = evtData.Rooms;
            base.InvokeOnExitedRoom(evtData);
        }
        #endregion
    }
}
