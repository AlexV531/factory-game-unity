using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dissonance.Extensions;
using Dissonance.Networking.Client;
using JetBrains.Annotations;

namespace Dissonance.Networking
{
    internal readonly struct ClientInfo
    {
        public string PlayerName { get; }
        public ushort PlayerId { get; }
        public CodecSettings CodecSettings { get; }

        public ClientInfo(string playerName, ushort playerId, CodecSettings codecSettings)
        {
            PlayerName = playerName;
            PlayerId = playerId;
            CodecSettings = codecSettings;
        }
    }

    /// <summary>
    /// Information about a client in a network session
    /// </summary>
    public class ClientInfo<TPeer>
        : IEquatable<ClientInfo<TPeer>>
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Network, typeof(ClientInfo<TPeer>).Name);

        private readonly List<string> _rooms = new List<string>();

        /// <summary>
        /// Name of this client (as specified by the DissonanceComms component for the client)
        /// </summary>
        [NotNull] public string PlayerName { get; }

        /// <summary>
        /// Unique ID of this client
        /// </summary>
        public ushort PlayerId { get; }

        /// <summary>
        /// The codec settings being used by the client
        /// </summary>
        public CodecSettings CodecSettings { get; }

        /// <summary>
        /// Ordered list of rooms this client is listening to
        /// </summary>
        [NotNull] internal ReadOnlyCollection<string> Rooms { get; }

        [CanBeNull] public TPeer Connection { get; internal set; }

        public bool IsConnected { get; internal set; }

        internal PeerVoiceReceiver VoiceReceiver { get; set; }
        #endregion

        public ClientInfo(string playerName, ushort playerId, CodecSettings codecSettings, [CanBeNull] TPeer connection)
        {
            Rooms = new ReadOnlyCollection<string>(_rooms);

            PlayerName = playerName;
            PlayerId = playerId;
            CodecSettings = codecSettings;
            Connection = connection;

            IsConnected = true;
        }

        public override string ToString()
        {
            return $"Client '{PlayerName}/{PlayerId}/{Connection}'";
        }

        #region equality
        public bool Equals(ClientInfo<TPeer> other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return string.Equals(PlayerName, other.PlayerName) && PlayerId == other.PlayerId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((ClientInfo<TPeer>)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (PlayerName.GetFnvHashCode() * 397) ^ PlayerId.GetHashCode();
            }
        }
        #endregion

        #region room management
        public bool AddRoom([NotNull] string roomName)
        {
            if (roomName == null) throw new ArgumentNullException(nameof(roomName));

            var index = _rooms.BinarySearch(roomName);
            if (index < 0)
            {
                _rooms.Insert(~index, roomName);
                Log.Trace("Added room {0} to client {1}", roomName, this);

                return true;
            }

            return false;
        }

        public bool RemoveRoom([NotNull] string roomName)
        {
            if (roomName == null) throw new ArgumentNullException(nameof(roomName));

            var index = _rooms.BinarySearch(roomName);
            if (index >= 0)
            {
                _rooms.RemoveAt(index);
                Log.Trace("Removed room {0} from client {1}", roomName, this);

                return true;
            }

            return false;
        }
        #endregion
    }
}
