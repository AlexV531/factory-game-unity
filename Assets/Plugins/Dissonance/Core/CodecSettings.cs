using Dissonance.Audio.Codecs;

namespace Dissonance
{
    public readonly struct CodecSettings
    {
        public Codec Codec { get; }
        public uint FrameSize { get; }
        public int SampleRate { get; }

        public CodecSettings(Codec codec, uint frameSize, int sampleRate)
        {
            Codec = codec;
            FrameSize = frameSize;
            SampleRate = sampleRate;
        }

        public override string ToString()
        {
            return $"Codec: {Codec}, FrameSize: {FrameSize}, SampleRate: {SampleRate / 1000f:##.##}kHz";
        }
    }
}
