using System;
using System.Collections.ObjectModel;
using JetBrains.Annotations;

namespace Dissonance.Networking.Server.Admin
{
    /// <summary>
    /// Server side representation of a client in a voice session
    /// </summary>
    public interface IServerClientState
    {
        /// <summary>
        /// Name of this player (as set in `DissonanceComms.LocalPlayerName`)
        /// </summary>
        [NotNull] string Name { get; }

        /// <summary>
        /// Get if this player is connected to the voice session
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Event fires when player starts listening to a room channel
        /// </summary>
        event Action<IServerClientState, string> OnStartedListeningToRoom;

        /// <summary>
        /// Event fires when player stops listening to a room channel
        /// </summary>
        event Action<IServerClientState, string> OnStoppedListeningToRoom;

        /// <summary>
        /// List of rooms that this player is currently listening to
        /// </summary>
        ReadOnlyCollection<string> Rooms { get; }

#if !DARK_RIFT_SERVER
        /// <summary>
        /// Event fires when player starts speaking to any channel. Requires `EnableChannelMonitoring`!
        /// </summary>
        event Action StartedSpeaking;

        /// <summary>
        /// Event fires when player stops speaking to all channels. Requires `EnableChannelMonitoring`!
        /// </summary>
        event Action StoppedSpeaking;

        /// <summary>
        /// Event fires for every voice packet from this player. Requires `EnableChannelMonitoring`!
        /// </summary>
        event Action<VoicePacket> OnVoicePacket;

        /// <summary>
        /// List of channels that this player is currently speaking through.
        /// This only returns useful results if `EnableChannelMonitoring` is enabled.
        /// </summary>
        ReadOnlyCollection<RemoteChannel> Channels { get; }
#endif

        /// <summary>
        /// Last time the `Channels` collection was updated
        /// </summary>
        DateTime LastChannelUpdateUtc { get; }

        /// <summary>
        /// Get the estimated packet loss for this client (0 to 1). Requires `EnableChannelMonitoring`!
        /// </summary>
        float PacketLoss { get; }

        /// <summary>
        /// Remove this player from a room.
        /// This prevents them from listening to the room until they re-enter the room. It does _not_ prevent them from re-entering the room!
        /// </summary>
        /// <param name="roomName"></param>
        void RemoveFromRoom(string roomName);

        /// <summary>
        /// Kick this user from the voice session, forcing them to immediately reconnect.
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// Server side representation of a client in a voice session.
    /// </summary>
    /// <typeparam name="TPeer">Type of network-backend-specific information</typeparam>
    public interface IServerClientState<TPeer>
        : IServerClientState
    {
        ClientInfo<TPeer> Peer { get; }
    }
}
