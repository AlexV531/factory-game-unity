namespace Dissonance.Audio.Playback
{
    public readonly struct PlaybackOptions
    {
        public PlaybackOptions(bool isPositional, float amplitudeMultiplier, ChannelPriority priority)
        {
            IsPositional = isPositional;
            AmplitudeMultiplier = amplitudeMultiplier;
            Priority = priority;
        }

        /// <summary>
        /// Get if audio on this channel is positional
        /// </summary>
        public bool IsPositional { get; }

        /// <summary>
        /// Get the amplitude multiplier applied to audio played through this channel
        /// </summary>
        public float AmplitudeMultiplier { get; }

        /// <summary>
        /// Get the priority of audio on this channel
        /// </summary>
        public ChannelPriority Priority { get; }
    }
}
