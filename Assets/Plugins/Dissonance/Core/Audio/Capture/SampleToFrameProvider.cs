using System;
using Dissonance.Extensions;
using NAudio.Wave;

namespace Dissonance.Audio.Capture
{
    internal class SampleToFrameProvider
        : IFrameProvider
    {
        private readonly ISampleProvider _source;
        public WaveFormat WaveFormat => _source.WaveFormat;

        public uint FrameSize { get; }

        private int _samplesInFrame;
        private readonly float[] _frame;

        public SampleToFrameProvider(ISampleProvider source, uint frameSize)
        {
            _source = source;
            FrameSize = frameSize;

            _frame = new float[frameSize];
        }

        public bool Read(ArraySegment<float> outBuffer)
        {
            if (outBuffer.Count < FrameSize)
                throw new ArgumentException($"Supplied buffer is smaller than frame size. {outBuffer.Count} < {FrameSize}", nameof(outBuffer));

            //Try to read enough samples to fill up the internal frame buffer
            _samplesInFrame += _source.Read(_frame, _samplesInFrame, checked((int)(FrameSize - _samplesInFrame)));

            //If we have filled the buffer copy it to the output
            if (_samplesInFrame == FrameSize)
            {
                outBuffer.CopyFrom(_frame);
                _samplesInFrame = 0;

                return true;
            }
            else
                return false;
        }

        public void Reset()
        {
            _samplesInFrame = 0;
        }
    }
}
