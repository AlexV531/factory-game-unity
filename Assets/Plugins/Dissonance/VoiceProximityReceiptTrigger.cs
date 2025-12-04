using UnityEngine;

namespace Dissonance
{
    /// <summary>
    /// Joins nearby rooms in an infinite grid of rooms
    /// </summary>
    [HelpURL("https://placeholder-software.co.uk/dissonance/docs/Reference/Components/Voice-Proximity-Receipt-Trigger/")]
    public class VoiceProximityReceiptTrigger
        : BaseProximityTrigger<RoomMembership>, IVoiceReceiptTrigger
    {
#pragma warning disable CS0414
        [SerializeField] private bool _roomExpanded = true;
        [SerializeField] private bool _tokensExpanded;
        [SerializeField] private bool _colliderExpanded;
#pragma warning restore CS0414

        private class ReceiptGrid
            : Grid
        {
            private readonly VoiceProximityReceiptTrigger _parent;

            public ReceiptGrid(VoiceProximityReceiptTrigger parent)
                : base(parent)
            {
                _parent = parent;
            }

            protected override RoomMembership CreateHandle(Vector3Int id, string name)
            {
                return Parent.Comms.Rooms.Join(new RoomName(name, true));
            }

            protected override void CloseHandle(RoomMembership handle)
            {
                _parent.Comms.Rooms.Leave(handle);
            }
        }

        protected override Grid CreateGrid()
        {
            return new ReceiptGrid(this);
        }
    }
}
