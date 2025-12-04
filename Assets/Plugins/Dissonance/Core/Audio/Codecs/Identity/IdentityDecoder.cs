using System;
using NAudio.Wave;

namespace Dissonance.Audio.Codecs.Identity
{
    internal class IdentityDecoder
        : IVoiceDecoder
    {
        public WaveFormat Format { get; }

        public IdentityDecoder(WaveFormat format)
        {
            Format = format;
        }

        public void Reset()
        {
        }

        public int Decode(EncodedBuffer input, ArraySegment<float> output)
        {
            //Packet lost, clear the output buffer
            if (!input.Encoded.HasValue || input.PacketLost)
            {
                // ReSharper disable once AssignNullToNotNullAttribute (Justification: Array segment cannot be null)
                Array.Clear(output.Array, output.Offset, output.Count);
                return output.Count;
            }

            var inputArray = input.Encoded.Value.Array;
            if (inputArray == null)
                throw new ArgumentNullException(nameof(input));

            var outputArray = output.Array;
            if (outputArray == null)
                throw new ArgumentNullException(nameof(output));

            var bytes = input.Encoded.Value.Count;
            if (bytes > output.Count * sizeof(float))
                throw new ArgumentException("output buffer is too small");

            Buffer.BlockCopy(inputArray, input.Encoded.Value.Offset, outputArray, output.Offset, bytes);
            return input.Encoded.Value.Count / sizeof(float);
        }

        public void Dispose()
        {
        }
    }
}
