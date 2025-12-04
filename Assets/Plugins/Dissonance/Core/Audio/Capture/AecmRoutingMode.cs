namespace Dissonance.Audio.Capture
{
    public enum AecmRoutingMode
    {
        // Implementation note - these specific values are important - the WebRtcPreprocessor uses these exact same
        // int values. Don't change them without also changing them there and recompiling on all platforms!

        Disabled = -1,

        // ReSharper disable UnusedMember.Global (Justification: these values are returned from the native audio preproessor)
        QuietEarpieceOrHeadset = 0,
        Earpiece = 1,
        LoudEarpiece = 2,
        Speakerphone = 3,
        LoudSpeakerphone = 4
        // ReSharper restore UnusedMember.Global
    }
}
