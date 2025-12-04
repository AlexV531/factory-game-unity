using System;

namespace Dissonance.Audio.Codecs.Identity
{
    internal class IdentityEncoder
        : IVoiceEncoder
    {
        public float PacketLoss
        {
            set { }
        }

        public int FrameSize { get; }

        public int SampleRate { get; }

        public IdentityEncoder(int sampleRate, int frameSize)
        {
            SampleRate = sampleRate;
            FrameSize = frameSize;
        }

        public ArraySegment<byte> Encode(ArraySegment<float> samples, ArraySegment<byte> array)
        {
            var inputArray = samples.Array ?? throw new ArgumentNullException(nameof(samples));
            var outputArray = array.Array ?? throw new ArgumentNullException(nameof(array));

            var bytes = samples.Count * sizeof(float);
            if (bytes > array.Count)
                throw new ArgumentException("output buffer is too small");

            Buffer.BlockCopy(inputArray, samples.Offset, outputArray, array.Offset, bytes);

            // ReSharper disable once AssignNullToNotNullAttribute
            return new ArraySegment<byte>(array.Array, array.Offset, bytes);
        }

        public void Reset()
        {
        }

        public void Dispose()
        {
        }
    }
}
