using System;
using Dissonance.Audio.Playback;
using Dissonance.Extensions;
using NAudio.Wave;

namespace Dissonance.Audio.Codecs.Silence
{
    internal class SilenceDecoder
        : IVoiceDecoder
    {
        private readonly int _frameSize;

        public WaveFormat Format { get; }

        public SilenceDecoder(FrameFormat frameFormat)
        {
            _frameSize = (int)frameFormat.FrameSize;
            Format = frameFormat.WaveFormat;
        }

        public void Dispose()
        {
        }

        
        public void Reset()
        {
        }

        public int Decode(EncodedBuffer input, ArraySegment<float> output)
        {
            //Clear output buffer so that it just contains silence
            output.Clear();

            //Output the length of a frame
            return _frameSize;
        }
    }
}
