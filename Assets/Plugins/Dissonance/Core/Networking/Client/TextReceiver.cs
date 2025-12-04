using System;
using JetBrains.Annotations;

namespace Dissonance.Networking.Client
{
    internal class TextReceiver<TPeer>
        where TPeer : struct
    {
        private static readonly Log Log = Logs.Create(LogCategory.Network, typeof(TextReceiver<TPeer>).Name);

        private readonly EventQueue _events;
        private readonly IRooms _rooms;
        private readonly IClientCollection<TPeer?> _peers;

        public TextReceiver([NotNull] EventQueue events, [NotNull] IRooms rooms, [NotNull] IClientCollection<TPeer?> peers)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
            _peers = peers ?? throw new ArgumentNullException(nameof(peers));
        }

        public void ProcessTextMessage(ref PacketReader reader)
        {
            //Parse packet
            var txt = reader.ReadTextPacket();

            //Discover who sent this message
            if (!_peers.TryGetClientInfoById(txt.Sender, out var info))
            {
                Log.Debug("Received a text message from unknown player '{0}'", txt.Sender);
                return;
            }

            //Discover who it is addressed to
            var recipient = GetTxtMessageRecipient(txt.RecipientType, txt.Recipient);
            if (recipient == null)
            {
                Log.Warn("Received a text message for a null recipient from '{0}'", info.PlayerName);
                return;
            }

            //Raise event to propogate message
            _events.EnqueueTextData(new TextMessage(
                info.PlayerName,
                txt.RecipientType,
                recipient,
                txt.Text
            ));
        }

        [CanBeNull]private string GetTxtMessageRecipient(ChannelType txtRecipientType, ushort txtRecipient)
        {
            if (txtRecipientType == ChannelType.Player)
            {
                if (!_peers.TryGetClientInfoById(txtRecipient, out var info))
                    return null;

                return info.PlayerName;
            }
            else if (txtRecipientType == ChannelType.Room)
                return _rooms.Name(txtRecipient);
            else
                throw Log.CreatePossibleBugException("Received a text message intended for an unknown recipient type", "521CB5B5-A45A-402E-95C8-CA99E8FFE4D9");
        }
    }
}
