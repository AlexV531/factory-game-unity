using System;
using JetBrains.Annotations;
using NAudio.Wave;

namespace Dissonance.Audio.Codecs.Opus
{
    internal class OpusDecoder
        : IVoiceDecoder
    {
        public WaveFormat Format { get; }

        private OpusNative.OpusDecoder _decoder;

        public OpusDecoder([NotNull] WaveFormat format, bool fec = true)
        {
            Format = format ?? throw new ArgumentNullException(nameof(format));

            _decoder = new OpusNative.OpusDecoder(format.SampleRate, format.Channels) {
                EnableForwardErrorCorrection = fec,
            };
        }

        public void Dispose()
        {
            _decoder?.Dispose();
            _decoder = null;
        }

        public void Reset()
        {
            _decoder.Reset();
        }

        public int Decode(EncodedBuffer input, ArraySegment<float> output)
        {
            return _decoder.DecodeFloats(input, output);
        }
    }
}
