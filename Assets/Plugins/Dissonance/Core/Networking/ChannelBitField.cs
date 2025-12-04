using System;

namespace Dissonance.Networking
{
    internal readonly struct ChannelBitField
    {
        #region mask constants
        private const ushort TypeMask = 0x0001;         //00000000 00000001
        private const ushort PositionalMask = 0x0002;   //00000000 00000010
        private const ushort ClosureMask = 0x0004;      //00000000 00000100

        private const ushort PriorityOffset = 3;
        private const ushort PriorityMask = 0x0018;     //00000000 00011000

        private const ushort SessionIdOffset = 5;
        private const ushort SessionIdMask = 0x0061;    //00000000 01100000

        private const ushort AmplitudeOffset = 8;
        private const ushort AmplitudeMask = 0xFF00;    //11111111 00000000
        #endregion

        #region fields and properties
        public ushort Bitfield { get; }

        public ChannelType Type
        {
            get
            {
                if ((Bitfield & TypeMask) == TypeMask)
                    return ChannelType.Room;
                return ChannelType.Player;
            }
        }

        public bool IsClosing => (Bitfield & ClosureMask) == ClosureMask;

        public bool IsPositional => (Bitfield & PositionalMask) == PositionalMask;

        public ChannelPriority Priority
        {
            get
            {
                var val = (Bitfield & PriorityMask) >> PriorityOffset;
                switch (val)
                {
                    default: return ChannelPriority.Default;
                    case 1: return ChannelPriority.Low;
                    case 2: return ChannelPriority.Medium;
                    case 3: return ChannelPriority.High;
                }
            }
        }

        public float AmplitudeMultiplier
        {
            get
            {
                //Get a byte value for the amplitude (0-255)
                var v = (Bitfield & AmplitudeMask) >> AmplitudeOffset;

                //move into floating point 0-2 range
                return v / 255f * 2;
            }
        }

        public int SessionId => (Bitfield & SessionIdMask) >> SessionIdOffset;

        #endregion

        public ChannelBitField(ushort bitfield)
        {
            Bitfield = bitfield;
        }

        public ChannelBitField(ChannelType type, int sessionId, ChannelPriority priority, float amplitudeMult, bool positional, bool closing)
            : this()
        {
            Bitfield = 0;

            //Pack the single bit values by setting their flags
            if (type == ChannelType.Room)
                Bitfield |= TypeMask;
            if (positional)
                Bitfield |= PositionalMask;
            if (closing)
                Bitfield |= ClosureMask;

            //Pack 2 bits of priority
            Bitfield |= PackPriority(priority);
            
            //Pack 2 bits of session ID by wrapping it as a 2 bit number and then shifting bits into position
            Bitfield |= (ushort)((sessionId % 4) << SessionIdOffset);

            //Pack amplitude multiplier by converting range limited float (0 to 2) to byte and shifting byte into position
            var ampByte = (byte)Math.Round(Math.Min(2, Math.Max(0, amplitudeMult)) / 2 * byte.MaxValue);
            Bitfield |= (ushort)(ampByte << AmplitudeOffset);
        }

        private static ushort PackPriority(ChannelPriority priority)
        {
            switch (priority)
            {
                case ChannelPriority.Low:
                    return 1 << PriorityOffset;
                case ChannelPriority.Medium:
                    return 2 << PriorityOffset;
                case ChannelPriority.High:
                    return 3 << PriorityOffset;

                // ReSharper disable RedundantCaseLabel, RedundantEmptyDefaultSwitchBranch (justification: I like to be explicit about these things)
                case ChannelPriority.None:
                case ChannelPriority.Default:
                default:
                    return 0;
                // ReSharper restore RedundantCaseLabel, RedundantEmptyDefaultSwitchBranch
            }
        }
    }
}
