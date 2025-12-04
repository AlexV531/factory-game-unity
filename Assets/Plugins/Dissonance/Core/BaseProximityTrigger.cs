using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dissonance.Extensions;
using JetBrains.Annotations;
using UnityEngine;

namespace Dissonance
{
    public abstract class BaseProximityTrigger<THandle>
        : BaseCommsTrigger
    {
        private float Size => _range * 2;

        [SerializeField, Range(1, 100), Tooltip("Radius of proximity chat.")]
        private int _range = 10;
        public int Range
        {
            get => _range;
            set
            {
                if (_range != value)
                {
                    _range = value;
                    CloseChannels();
                }
            }
        }

        [SerializeField] private string _roomName = "GridProximityChat";
        public string RoomName
        {
            get => _roomName;
            set
            {
                _roomName = value;
                CloseChannels();
            }
        }

        /// <inheritdoc />
        public override bool CanTrigger
        {
            get
            {
                if (!Comms || !Comms.IsStarted)
                    return false;

                if (_roomName == null)
                    return false;

                if (_self == null || !_self.IsConnected)
                    return false;

                var tracker = _self?.Tracker;
                if (tracker == null || !tracker.IsTracking)
                    return false;

                if (UseColliderTrigger && !IsColliderTriggered)
                    return false;

                return true;
            }
        }

        [SerializeField] private bool _useTrigger;
        /// <summary>
        /// Get or set if this broadcast trigger should use a unity trigger volume
        /// </summary>
        public override bool UseColliderTrigger
        {
            get => _useTrigger;
            set => _useTrigger = value;
        }

        protected int ActiveChannelCount => _grid?.ChannelCount ?? 0;

        private VoicePlayerState _self;
        private Grid _grid;

        protected abstract Grid CreateGrid();

        private void OnValidate()
        {
            CloseChannels();

            var broadcasters = FindObjectsByType<VoiceProximityBroadcastTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID).Where(a => a.RoomName == RoomName).ToList();
            var receivers = FindObjectsByType<VoiceProximityReceiptTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID).Where(a => a.RoomName == RoomName).ToList();
            foreach (var broadcaster in broadcasters)
                broadcaster.Range = Range;
            foreach (var receiver in receivers)
                receiver.Range = Range;
        }

        protected override void Start()
        {
            _grid = CreateGrid();

            base.Start();
        }

        protected override void OnDisable()
        {
            CloseChannels();
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            CloseChannels();
            base.OnDestroy();
        }

        protected void CloseChannels()
        {
            _grid?.CloseAll();
        }

        internal bool AllowJoin(Vector3Int id)
        {
            return AllowJoin(_self, id);
        }

        /// <summary>
        /// Override this to filter which rooms may be joined. Call `GetCellBounds` to get the world space position of this cell.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        protected virtual bool AllowJoin(VoicePlayerState player, Vector3Int id)
        {
            return true;
        }

        /// <summary>
        /// Get the world space bounds of the cell with the given ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        protected Bounds GetCellBounds(Vector3Int id)
        {
            var min = (Vector3)id * Size;
            var max = min + new Vector3(Size, Size, Size);

            var bounds = new Bounds();
            bounds.SetMinMax(min, max);

            return bounds;
        }

        protected override void Update()
        {
            // Sanity check that we have a reference to DissonanceComms
            if (!CheckVoiceComm())
            {
                CloseChannels();
                return;
            }

            // Get a reference to the local player if we don't already have one
            if (_self == null || _self.Name != Comms.LocalPlayerName)
            {
                _self = Comms.FindPlayer(Comms.LocalPlayerName);
            }
            
            // Check if triggering is currently allowed
            if (!CanTrigger)
            {
                CloseChannels();
                return;
            }

            // Check if tokens are preventing activation
            if (!TokenActivationState)
            {
                CloseChannels();
                return;
            }

            // Check that the local player reference is valid
            var tracker = _self?.Tracker;
            if (tracker == null || !tracker.IsTracking)
            {
                CloseChannels();
                return;
            }

            // Check if the user is blocking activation
            if (!IsUserActivated())
            {
                CloseChannels();
                return;
            }

            // Update the two grids
            _grid.Update(tracker);

            base.Update();
        }

        /// <summary>
        /// Get a value indicating if the user wants this trigger to activate
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsUserActivated()
        {
            return true;
        }

        public void OnDrawGizmosSelected()
        {
            _grid?.DrawGizmos();
        }

        protected abstract class Grid
        {
            public BaseProximityTrigger<THandle> Parent { get; }
            public int ChannelCount => _handles.Count;

            private readonly StringBuilder _nameBuilder = new StringBuilder();

            private readonly List<(Vector3Int, THandle)> _handles = new List<(Vector3Int, THandle)>();
            private readonly HashSet<Vector3Int> _keys = new HashSet<Vector3Int>();
            private IDissonancePlayer _player;

            private const int CacheSize = 128;
            private readonly Dictionary<Vector3Int, string> _roomNameCache = new Dictionary<Vector3Int, string>(CacheSize);
            [CanBeNull] private string _cacheRoomName;

            protected Grid(BaseProximityTrigger<THandle> parent)
            {
                Parent = parent;
            }

            public void Update(IDissonancePlayer player)
            {
                _player = player;
                
                var aabb = new Bounds(player.Position, new Vector3(Parent.Range, Parent.Range, Parent.Range) * 2);
                var min = CellPos(aabb.min);
                var max = CellPos(aabb.max);// + Vector3Int.one;
                var bounds = new BoundsInt();
                bounds.SetMinMax(min, max);

                // Close any handles which are not in the AABB
                for (var i = _handles.Count - 1; i >= 0; i--)
                {
                    var item = _handles[i];
                    var (key, handle) = (item.Item1, item.Item2);

                    if (!InBound(key, bounds))
                    {
                        CloseHandle(handle);
                        _handles.RemoveAt(i);
                        _keys.Remove(key);
                    }
                }

                // Join all cells which intersect the AABB
                for (var x = min.x; x <= max.x; x++)
                for (var y = min.y; y <= max.y; y++)
                for (var z = min.z; z <= max.z; z++)
                {
                    var id = new Vector3Int(x, y, z);
                    if (!Parent.AllowJoin(id))
                        continue;

                    if (_keys.Add(id))
                    {
                        var handle = CreateHandle(id, GenerateName(id));
                        _handles.Add((id, handle));
                    }
                }
            }

            private static bool InBound(Vector3Int point, BoundsInt bounds)
            {
                return point.x >= bounds.xMin && point.x <= bounds.xMax
                    && point.y >= bounds.yMin && point.y <= bounds.yMax
                    && point.z >= bounds.zMin && point.z <= bounds.zMax;
            }

            public void CloseAll()
            {
                foreach (var handle in _handles)
                    CloseHandle(handle.Item2);
                _handles.Clear();
                _keys.Clear();
            }

            protected abstract THandle CreateHandle(Vector3Int id, string name);

            protected abstract void CloseHandle(THandle handle);

            private Vector3Int CellPos(Vector3 pos)
            {
                return pos.Quantise(Parent.Size);
            }

            private string GenerateName(Vector3Int pos)
            {
                // Clear the name cache if the room name has changed.
                if (_cacheRoomName != Parent.RoomName)
                {
                    _cacheRoomName = Parent.RoomName;
                    _roomNameCache.Clear();
                }

                // Try to get the item from the cache
                if (!_roomNameCache.TryGetValue(pos, out var value))
                {
                    // Once the cache is full dump everything and start again
                    if (_roomNameCache.Count >= CacheSize)
                        _roomNameCache.Clear();

                    // Generate the name for this room
                    _nameBuilder.Clear();
                    _nameBuilder.EnsureCapacity(Parent.RoomName.Length + 50);
                    _nameBuilder.Append(Parent.RoomName);
                    _nameBuilder.Append(" {X:");
                    _nameBuilder.Append(pos.x);
                    _nameBuilder.Append(",Y:");
                    _nameBuilder.Append(pos.y);
                    _nameBuilder.Append(",Z:");
                    _nameBuilder.Append(pos.z);
                    _nameBuilder.Append(",R:");
                    _nameBuilder.Append(Parent.Range);
                    _nameBuilder.Append("}");
                    value = _nameBuilder.ToString();

                    // Store it in the cache
                    _roomNameCache[pos] = value;
                }

                return value;
            }

            public void DrawGizmos()
            {
                if (_player == null)
                {
                    return;
                }

                var lineCol = new Color(0.3f, 0.3f, 0.95f);
                var bulkCol = new Color(lineCol.r, lineCol.g, lineCol.b, 0.05f);

                Gizmos.color = lineCol;
                Gizmos.DrawWireSphere(_player.Position, Parent.Range);

                foreach (var key in _keys)
                {
                    var min = (Vector3)key * Parent.Size;
                    var max = min + new Vector3(Parent.Size, Parent.Size, Parent.Size);
                    DrawCube(min, max, lineCol, bulkCol);
                }
            }

            private static void DrawCube(Vector3 min, Vector3 max, Color lines, Color fill)
            {
                var mid = (min + max) / 2 + new Vector3(0, 0.001f, 0);
                var size = max - min;

                Gizmos.color = lines;
                Gizmos.DrawWireCube(mid, size);
                Gizmos.color = fill;
                Gizmos.DrawCube(mid, size);
            }
        }
    }
}
