using System;

namespace Dissonance.Audio.Codecs.Opus
{
    internal class OpusEncoder
        : IVoiceEncoder
    {
        private static readonly Log Log = Logs.Create(LogCategory.Playback, nameof(OpusEncoder));

        private readonly OpusNative.OpusEncoder _encoder;

        /// <summary>
        /// Permitted frame sizes in samples per second
        /// </summary>
        private static readonly int[] PermittedFrameSizesSamples = {
            (int)(2.5f * FixedSampleRate / 1000),
            (int)(5 * FixedSampleRate / 1000),
            (int)(10 * FixedSampleRate / 1000),
            (int)(20 * FixedSampleRate / 1000),
            (int)(40 * FixedSampleRate / 1000),
            (int)(60 * FixedSampleRate / 1000)
        };

        public const int FixedSampleRate = 48000;
        public int SampleRate => FixedSampleRate;

        public float PacketLoss
        {
            set => _encoder.PacketLoss = value;
        }

        public int FrameSize { get; }

        public OpusEncoder(AudioQuality quality, FrameSize frameSize, bool fec = true)
        {
            _encoder = new OpusNative.OpusEncoder(SampleRate, 1)
            {
                EnableForwardErrorCorrection = fec,
                Bitrate = GetTargetBitrate(quality)
            };

            FrameSize = GetFrameSize(frameSize);
        }

        private static int GetTargetBitrate(AudioQuality quality)
        {
            // https://wiki.xiph.org/Opus_Recommended_Settings#Recommended_Bitrates
            switch (quality)
            {
                case AudioQuality.Low:
                    return 10000;
                case AudioQuality.Medium:
                    return 17000;
                case AudioQuality.High:
                    return 24000;
                default:
                    throw new ArgumentOutOfRangeException(nameof(quality), quality, null);
            }
        }

        /// <summary>
        /// Get the number of samples in a single frame
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static int GetFrameSize(FrameSize size)
        {
            switch (size)
            {
                case Dissonance.FrameSize.Tiny:
                    return PermittedFrameSizesSamples[2]; // 10ms
                case Dissonance.FrameSize.Small:
                    return PermittedFrameSizesSamples[3]; // 20ms
                case Dissonance.FrameSize.Medium:
                    return PermittedFrameSizesSamples[4]; // 40ms
                case Dissonance.FrameSize.Large:
                    return PermittedFrameSizesSamples[5]; // 60ms
                default:
                    throw new ArgumentOutOfRangeException(nameof(size), size, null);
            }
        }

        public ArraySegment<byte> Encode(ArraySegment<float> samples, ArraySegment<byte> encodedBuffer)
        {
            if (Array.IndexOf(PermittedFrameSizesSamples, samples.Count) == -1)
                throw new ArgumentException(Log.PossibleBugMessage($"Incorrect frame size '{samples.Count}'", "6AFD9ADF-1D15-4197-99E9-5A19ECB8CD20"), nameof(samples));

            var encodedByteCount = _encoder.EncodeFloats(samples, encodedBuffer);

            // ReSharper disable once AssignNullToNotNullAttribute (Justification: Array segment cannot be null)
            return new ArraySegment<byte>(encodedBuffer.Array, encodedBuffer.Offset, encodedByteCount);
        }

        public void Reset()
        {
            _encoder.Reset();
        }

        public void Dispose()
        {
            _encoder.Dispose();
        }
    }
}
