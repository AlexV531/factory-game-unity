using System;
using Dissonance.Audio.Playback;
using JetBrains.Annotations;

namespace Dissonance
{
    /// <summary>
    /// Represents an open channel the local client is receiving voice with
    /// </summary>
    public readonly struct RemoteChannel
    {
        /// <summary>
        /// Get the type of this channel
        /// </summary>
        public ChannelType Type { get; }

        /// <summary>
        /// Get the playback options set for this channel
        /// </summary>
        public PlaybackOptions Options { get; }

        /// <summary>
        /// Get the name of the target of this channel. Either room name or player name depending upon the type of this channel
        /// </summary>
        public string TargetName { get; }

        public RemoteChannel([NotNull] string targetName, ChannelType type, PlaybackOptions options)
        {
            TargetName = targetName ?? throw new ArgumentNullException(nameof(targetName));
            Type = type;
            Options = options;
        }
    }
}
