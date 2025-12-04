using System;
using System.Collections.Generic;
using Dissonance.Audio.Playback;
using Dissonance.Extensions;
using Dissonance.Networking;
using JetBrains.Annotations;
using UnityEngine;

namespace Dissonance.Integrations.Offline
{
    /// <summary>
    /// This is a Dissonance network integration that pretends to always be connected to a server.
    /// This can be used to activate the voice processing pipeline in an offline scene (e.g. menus).
    ///
    /// Create a VoiceReceiptTrigger for the "Loopback" room to hear yourself.
    /// </summary>
    public class OfflineCommsNetwork
        : MonoBehaviour, ICommsNetwork
    {
        #region fields and properties
        private bool _loopbackActive;
        private bool _sentStartedSpeakingEvent;
        private uint _loopbackSequenceNumber;
        private readonly List<RemoteChannel> _loopbackChannels = new List<RemoteChannel>();
        private readonly Queue<byte[]> _bufferPool = new Queue<byte[]>();
        private readonly Queue<VoicePacket> _loopbackQueue = new Queue<VoicePacket>(128);

        private bool _playerJoined;
        private CodecSettings? _codecSettings;

        public int LoopbackPacketCount { get; private set; }

        public ConnectionStatus Status => ConnectionStatus.Connected;
        #endregion

        public void Initialize(string playerName, Rooms rooms, PlayerChannels playerChannels, RoomChannels roomChannels, CodecSettings codecSettings)
        {
            _codecSettings = codecSettings;
            _loopbackChannels.Add(new RemoteChannel("Loopback", ChannelType.Room, new PlaybackOptions(false, 1, ChannelPriority.Default)));

            roomChannels.OpenedChannel += BeginLoopback;
            roomChannels.ClosedChannel += EndLoopback;
        }

        private void BeginLoopback(RoomName channel, ChannelProperties props)
        {
            _loopbackActive = true;
        }

        private void EndLoopback(RoomName channel, ChannelProperties props)
        {
            if (_sentStartedSpeakingEvent)
                PlayerStoppedSpeaking?.Invoke("Loopback");

            _loopbackQueue.Clear();
            _sentStartedSpeakingEvent = false;
            _loopbackActive = false;
            _loopbackSequenceNumber = 0;
        }

        public NetworkMode Mode => NetworkMode.Client;

        public event Action<string, CodecSettings> PlayerJoined;
        public event Action<VoicePacket> VoicePacketReceived;
        public event Action<string> PlayerStartedSpeaking;
        public event Action<string> PlayerStoppedSpeaking;

        #region unused events
#pragma warning disable CS0067
        public event Action<NetworkMode> ModeChanged;           // We're always connected
        public event Action<string> PlayerLeft;                 // Loopback player never leaves
        public event Action<TextMessage> TextPacketReceived;    // We don't care about text
        public event Action<RoomEvent> PlayerEnteredRoom;       // Remote players never join rooms...
        public event Action<RoomEvent> PlayerExitedRoom;        // ...and therefore never leave them either
#pragma warning restore CS0067
        #endregion

        public void SendVoice(ArraySegment<byte> data)
        {
            if (!_loopbackActive)
                return;

            // Copy the data into a buffer we're allowed to store for a few frames
            var buffer = data.CopyToSegment(_bufferPool.Count > 0 ? _bufferPool.Dequeue() : new byte[1024]);

            // Store the packet in a buffer to be sent later
            LoopbackPacketCount++;
            _loopbackQueue.Enqueue(new VoicePacket(
                "Loopback",
                ChannelPriority.Default,
                1,
                false,
                buffer,
                _loopbackSequenceNumber++,
                _loopbackChannels
            ));
        }

        public void SendText([CanBeNull] string data, ChannelType recipientType, string recipientId)
        {
            // Do nothing! Text messages are completely ignored by this network system.
        }

        private void Update()
        {
            JoinFakePlayer();

            if (_playerJoined)
                PumpLoopback();
        }

        private void JoinFakePlayer()
        {
            // Don't join the player again if they've already joined
            if (_playerJoined)
                return;

            // Can't join the player until we know the encoding settings
            if (!_codecSettings.HasValue)
                return;

            PlayerJoined?.Invoke("Loopback", _codecSettings.Value);
            _playerJoined = true;
        }

        private void PumpLoopback()
        {
            if (!_loopbackActive)
                return;

            // If we haven't started streaming audio yet wait for the buffer to fill up a bit
            if (!_sentStartedSpeakingEvent && _loopbackQueue.Count < 5)
                return;

            // Now that the buffer has some audio, send an event to inform the playback system
            if (!_sentStartedSpeakingEvent)
            {
                PlayerStartedSpeaking?.Invoke("Loopback");
                _sentStartedSpeakingEvent = true;
            }

            // Pump all waiting packets to playback system
            while (_loopbackQueue.Count > 0)
            {
                var pkt = _loopbackQueue.Dequeue();
                VoicePacketReceived?.Invoke(pkt);
                _bufferPool.Enqueue(pkt.EncodedAudioFrame.Array);
            }
        }
    }
}