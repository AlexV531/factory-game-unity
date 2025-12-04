using System.Collections.Generic;
using Dissonance.Datastructures;
using JetBrains.Annotations;

namespace Dissonance.Networking
{
    internal class RoomClientsCollection<T>
    {
        #region fields and properties
        private static readonly IComparer<ClientInfo<T>> ClientComparer = new ClientIdComparer();

        private readonly Dictionary<string, List<ClientInfo<T>>> _clientByRoomName = new Dictionary<string, List<ClientInfo<T>>>();

        // A cache of hashes mapped to names with that hash
        private readonly Dictionary<ushort, List<string>> _roomNamesByHash = new Dictionary<ushort, List<string>>();
        private readonly Pool<List<string>> _listStringPool = new Pool<List<string>>(16, () => new List<string>());
        #endregion

        #region mutate
        private void AddToHashCache(string name)
        {
            var hash = new RoomName(name, true).ToRoomId();

            if (!_roomNamesByHash.TryGetValue(hash, out var names))
            {
                names = _listStringPool.Get();
                _roomNamesByHash.Add(hash, names);
                names.Clear();

                names.Add(name);
            }
            else
            {
                if (!names.Contains(name))
                    names.Add(name);
            }
        }

        private void RemoveFromHashCache(string name)
        {
            var hash = new RoomName(name, true).ToRoomId();

            if (!_roomNamesByHash.TryGetValue(hash, out var names))
                return;

            names.Remove(name);

            // If nothing is left associated with this hash recycle the list of names
            if (names.Count == 0)
            {
                _roomNamesByHash.Remove(hash);
                _listStringPool.Put(names);
            }
        }

        public void Add(string room, [NotNull] ClientInfo<T> client)
        {
            // Make sure this room is stored in the hash->name lookup
            AddToHashCache(room);

            // Get or create the list of clients.
            if (!_clientByRoomName.TryGetValue(room, out var list))
            {
                list = new List<ClientInfo<T>>();
                _clientByRoomName.Add(room, list);
            }

            // Add the client to the list
            var index = list.BinarySearch(client, ClientComparer);
            if (index < 0)
                list.Insert(~index, client);
        }

        public bool Remove(string room, [NotNull] ClientInfo<T> client)
        {
            // Get the list of clients, if it doesn't exist no further work is needed
            if (!_clientByRoomName.TryGetValue(room, out var list))
                return false;

            // Find client in list
            var index = list.BinarySearch(client, ClientComparer);
            if (index < 0)
                return false;

            // Remove client from list
            list.RemoveAt(index);

            // Once the last client has been removed from this room we no longer need the hash lookup for it
            if (list.Count == 0)
                RemoveFromHashCache(room);

            return true;
        }

        public void Clear()
        {
            _clientByRoomName.Clear();
        }
        #endregion

        #region query
        public bool TryGetClientsInRoom(string room, [CanBeNull] List<ClientInfo<T>> output)
        {
            if (_clientByRoomName.TryGetValue(room, out var clients))
            {
                output?.AddRange(clients);
                return true;
            }

            return false;
        }
        
        public bool TryGetClientsInRoom(ushort roomId, [CanBeNull] List<ClientInfo<T>> output)
        {
            // Get all the names which share this hash
            if (!_roomNamesByHash.TryGetValue(roomId, out var names))
                return false;

            // Add client from all those rooms to list
            for (var i = 0; i < names.Count; i++)
                if (_clientByRoomName.TryGetValue(names[i], out var clients))
                    output?.AddRange(clients);

            return true;
        }

        public int ClientCount()
        {
            var sum = 0;
            foreach (var kvp in _clientByRoomName)
                sum += kvp.Value.Count;
            return sum;
        }
        #endregion

        private class ClientIdComparer
            : IComparer<ClientInfo<T>>
        {
            public int Compare(ClientInfo<T> x, ClientInfo<T> y)
            {
                return (x, y) switch
                {
                    (null, null) => 0,
                    (null, _) => -1,
                    (_, null) => 1,

                    _ => x.PlayerId.CompareTo(y.PlayerId)
                };
            }
        }
    }
}
