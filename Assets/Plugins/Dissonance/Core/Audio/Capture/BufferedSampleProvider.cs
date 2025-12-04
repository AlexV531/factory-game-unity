using System;
using Dissonance.Datastructures;
using NAudio.Wave;

namespace Dissonance.Audio.Capture
{
    /// <summary>
    /// A sample provider which reads from an internal buffer of samples
    /// </summary>
    internal class BufferedSampleProvider
        : ISampleProvider
    {
        public int Count => _samples.EstimatedUnreadCount;

        public int Capacity => _samples.Capacity;

        /// <inheritdoc />
        public WaveFormat WaveFormat { get; }

        private readonly TransferBuffer<float> _samples;

        public BufferedSampleProvider(WaveFormat format, int bufferSize)
        {
            WaveFormat = format;
            _samples = new TransferBuffer<float>(bufferSize);
        }

        /// <inheritdoc />
        public int Read(float[] buffer, int offset, int count)
        {
            if (!_samples.Read(new ArraySegment<float>(buffer, offset, count)))
                return 0;
            return count;
        }

        /// <summary>
        /// Write data into the buffer
        /// </summary>
        /// <param name="data"></param>
        /// <returns>The amount of data written into the buffer</returns>
        public int Write(ArraySegment<float> data)
        {
            if (data.Array == null)
                throw new ArgumentNullException(nameof(data));

            return _samples.WriteSome(data);
        }

        public void Reset()
        {
            _samples.Clear();
        }
    }
}
