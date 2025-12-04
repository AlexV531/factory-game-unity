using System;
using System.Collections.Generic;
using Dissonance.Extensions;
using JetBrains.Annotations;

namespace Dissonance
{
    public struct RoomName
        : IEquatable<RoomName>
    {
        [NotNull] public string Name { get; set; }
        internal bool SuppressDuplicateCheck { get; set; }

        internal RoomName([NotNull] string name, bool suppress = false)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            SuppressDuplicateCheck = suppress;
        }

        public RoomName([NotNull] string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            SuppressDuplicateCheck = false;
        }

        public bool Equals(RoomName other)
        {
            return string.Equals(Name, other.Name);
        }

        public static implicit operator RoomName(string name)
        {
            return new RoomName(name);
        }

        public static implicit operator string(RoomName name)
        {
            return name.Name;
        }

        public readonly override string ToString()
        {
            return Name;
        }
    }

    public static class RoomIdConversion
    {
#if DEBUG
        private static readonly Log Log = Logs.Create(LogCategory.Core, "Rooms");
        private static readonly Dictionary<ushort, string> RoomIdMappings = new Dictionary<ushort, string>();
#endif

        public static ushort ToRoomId(this string name)
        {
            return new RoomName(name).ToRoomId();
        }

        public static ushort ToRoomId(this RoomName name)
        {
            if (name.Name == null)
                throw new ArgumentNullException(nameof(name));

            var id = Hash16(name.Name);

#if DEBUG
            if (!name.SuppressDuplicateCheck)
            {
                if (RoomIdMappings.TryGetValue(id, out var existing))
                {
                    Log.AssertAndLogError(
                        existing == name.Name,
                        "b3ccbf8e-6a6c-4533-8684-5a299c413937",
                        "Hash collision between room names '{0}' and '{1}'. Please choose a different room name.",
                        existing,
                        name
                    );
                }
                else
                    RoomIdMappings[id] = name.Name;
            }
#endif

            return id;
        }

        private static ushort Hash16([NotNull] string str)
        {
            var hash = str.GetFnvHashCode();

            unchecked
            {
                //We now have a good 32 bit hash, but we want to mix this down into a 16 bit hash
                var upper = (ushort)(hash >> 16);
                var lower = (ushort)hash;
                return (ushort)(upper * 5791 + lower * 7639);
            }
        }
    }
}
