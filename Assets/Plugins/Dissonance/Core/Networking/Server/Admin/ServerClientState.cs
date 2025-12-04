using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dissonance.Datastructures;
using Dissonance.Networking.Client;
using JetBrains.Annotations;

namespace Dissonance.Networking.Server.Admin
{
    internal class ServerClientState<TServer, TClient, TPeer>
        : IServerClientState<TPeer>
        where TServer : BaseServer<TServer, TClient, TPeer>
        where TClient : BaseClient<TServer, TClient, TPeer>
        where TPeer : struct, IEquatable<TPeer>
    {
        private static readonly Log Log = new Log((int)LogCategory.Network, typeof(ServerClientState<TServer, TClient, TPeer>).Name);

        private readonly TServer _server;

        public ClientInfo<TPeer> Peer { get; }
        public string Name => Peer.PlayerName;
        public bool IsConnected => Peer.IsConnected;

        private byte _currentVoiceSession;
        private uint _previousSequenceNumber;
        private readonly PacketLossCalculator _packetLoss = new PacketLossCalculator(128);
        public float PacketLoss => _packetLoss.PacketLoss;

        public event Action<IServerClientState, string> OnStartedListeningToRoom;
        public event Action<IServerClientState, string> OnStoppedListeningToRoom;

#if !DARK_RIFT_SERVER
        public event Action StartedSpeaking;
        public event Action StoppedSpeaking;
        public event Action<VoicePacket> OnVoicePacket;

        private readonly PeerVoiceReceiver _voiceReceiver;
#endif

        private readonly List<string> _rooms;
        public ReadOnlyCollection<string> Rooms { get; }

        private readonly List<RemoteChannel> _channels;
        public ReadOnlyCollection<RemoteChannel> Channels { get; }

        public DateTime LastChannelUpdateUtc { get; private set; }

        public ServerClientState(TServer server, ClientInfo<TPeer> peer)
        {
            _server = server;
            Peer = peer;

            _rooms = new List<string>();
            Rooms = new ReadOnlyCollection<string>(_rooms);

            _channels = new List<RemoteChannel>();
            Channels = new ReadOnlyCollection<RemoteChannel>(_channels);

#if !DARK_RIFT_SERVER
            // Create a peer voice receiver configured to receive _all_ voice packets, ignoring room/player target
            _voiceReceiver = new PeerVoiceReceiver(
                peer.PlayerName, peer.PlayerId,
                "572a03f5a51c41f8b2a9b8d3b498dc33",
                new VoiceEventHandler(this),
                new Rooms(),
                new ConcurrentPool<List<RemoteChannel>>(0, () => new List<RemoteChannel>())
            ) {
                ReceiveAllVoicePackets = true
            };
#endif
        }

        public void RemoveFromRoom([NotNull] string roomName)
        {
            if (roomName == null)
                throw new ArgumentNullException(nameof(roomName));

            // Send a packet to the server as if this peer asked to be removed from the given room
            var p = new PacketWriter(new byte[10 + roomName.Length * 4]);
            p.WriteDeltaChannelState(_server.SessionId, false, Peer.PlayerId, roomName);
            _server.NetworkReceivedPacket(Peer.Connection, p.Written);
        }

        public void Reset()
        {
            // Send a packet to this client telling them that they're using the wrong session ID.
            // This is a lie (in fact we're using the wrong ID intentionally), but it will get the client to
            // remove itself from the room.
            var writer = new PacketWriter(new byte[7]);
            writer.WriteErrorWrongSession(unchecked(_server.SessionId + 1));
            _server.SendUnreliable(new List<TPeer> { Peer.Connection }, writer.Written);
        }

        public void InvokeOnEnteredRoom(string name)
        {
            if (!_rooms.Contains(name))
                _rooms.Add(name);

            var entered = OnStartedListeningToRoom;
            if (entered != null)
            {
                try
                {
                    entered(this, name);
                }
                catch (Exception e)
                {
                    Log.Error("Exception encountered invoking `PlayerJoined` event handler: {0}", e);
                }
            }
        }

        public void InvokeOnExitedRoom(string name)
        {
            _rooms.Remove(name);

            var exited = OnStoppedListeningToRoom;
            if (exited != null)
            {
                try
                {
                    exited(this, name);
                }
                catch (Exception e)
                {
                    Log.Error("Exception encountered invoking `PlayerJoined` event handler: {0}", e);
                }
            }
        }

        public void UpdateChannels([NotNull] List<RemoteChannel> channels)
        {
            _channels.Clear();
            _channels.AddRange(channels);
            LastChannelUpdateUtc = DateTime.UtcNow;
        }

        public void InvokeOnVoicePacket(PacketReader reader)
        {
#if !DARK_RIFT_SERVER
            // Read first part of the header from voice packet
            // skip this packet if it's from the wrong sender
            reader.ReadVoicePacketHeader1(out var senderId);
            if (senderId != Peer.PlayerId)
                return;

            // Copy the reader. This allows the packet loss monitoring to read some of the packet header, without
            // advancing the position and confusing the `_voiceReceiver.ReceivePacket` method.
            var copy = reader;
            copy.ReadVoicePacketHeader2(out var metadata, out var sequenceNumber, out _);

            // Basic packet loss monitoring. We don't fully handle out of order packets here, which produces a slight
            // overestimate of packet loss. Depending on the size of the jitter buffer an out of order packet may act
            // as a lost packet anyway - this is just estimating loss as if the jitter buffer is zero sized (worst case).
            if (metadata.ChannelSession != _currentVoiceSession)
            {
                _previousSequenceNumber = sequenceNumber;
                _currentVoiceSession = metadata.ChannelSession;
            }
            else
            {
                var lost = sequenceNumber != unchecked(_previousSequenceNumber + 1);
                _packetLoss.Update(!lost);
                _previousSequenceNumber = sequenceNumber;
            }

            // Process the reader through the normal voice receiver logic
            _voiceReceiver.ReceivePacket(ref reader, DateTime.UtcNow);
#endif
        }

#if !DARK_RIFT_SERVER
        private class VoiceEventHandler
            : IVoiceEventQueue
        {
            private readonly ServerClientState<TServer, TClient, TPeer> _parent;
            private readonly ConcurrentPool<byte[]> _bytesPool = new ConcurrentPool<byte[]>(4, () => new byte[1024]);

            public VoiceEventHandler(ServerClientState<TServer, TClient, TPeer> parent)
            {
                _parent = parent;
            }

            public void EnqueueStoppedSpeaking(string name)
            {
                _parent.StoppedSpeaking?.Invoke();
            }

            public void EnqueueStartedSpeaking(string name)
            {
                _parent.StartedSpeaking?.Invoke();
            }

            public void EnqueueVoiceData(VoicePacket voicePacket)
            {
                _parent.OnVoicePacket?.Invoke(voicePacket);

                var arr = voicePacket.EncodedAudioFrame.Array;
                if (arr != null)
                    _bytesPool.Put(arr);
            }

            public byte[] GetEventBuffer()
            {
                return _bytesPool.Get();
            }
        }
#endif
    }
}
