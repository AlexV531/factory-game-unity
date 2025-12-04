namespace Dissonance
{
    public interface IVoiceBroadcastTrigger
    {
        string InputName { get; set; }

        CommActivationMode Mode { get; set; }

        bool IsMuted { get; set; }

        bool UseColliderTrigger { get; set; }

        string RoomName { get; set; }

        ChannelPriority Priority { get; set; }

        bool IsTransmitting { get; }

        void ToggleMute();
    }
}
