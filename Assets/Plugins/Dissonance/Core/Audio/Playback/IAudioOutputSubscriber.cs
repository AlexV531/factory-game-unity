using System;

namespace Dissonance.Audio.Playback
{
    /// <summary>
    ///     Subscriber to received audio from the end of the audio output system. To use this
    ///     create a MonoBehaviour which implements this interface and attach it to the playback prefab.
    /// </summary>
    /// <remarks>
    ///     Not currently compatible with any alternative (non-Unity) playback system, for example FMOD.
    /// </remarks>
    public interface IAudioOutputSubscriber
    {
        /// <summary>
        ///     <para>
        ///         Called by Dissonance with a frame of audio data, immediately before it is played through the Unity audio system
        ///         using OnAudioFilterRead (<see href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnAudioFilterRead.html">OnAudioFilterRead</see>).
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     This method will be called on the audio thread, performance is ABSOLUTELY CRITICAL! Any delay here will cause crackling, static and audio desync.<br />
        ///     <list type="bullet">
        ///         <item>
        ///             <term>Blocking</term>
        ///             <description>Do not block this thread for any length of time.</description>
        ///         </item>
        ///         <item>
        ///             <term>Locking</term>
        ///             <description>Locks block while contended, avoid using locks.</description>
        ///         </item>
        ///         <item>
        ///             <term>Allocating</term>
        ///             <description>Allocating memory may cause the garbage collector to run, which will block the thread.</description>
        ///         </item>
        ///         <item>
        ///             <term>Unity calls</term>
        ///             <description>Most Unity calls cannot be used when not on the "main thread".</description>
        ///         </item>
        ///     </list>
        /// </remarks>
        /// <param name="data">
        ///     All of the audio data which is about to be sent to Unity. Modifying this data will modify what is played back.<br />
        ///     Do not store a reference to this array!
        /// </param>
        /// <param name="complete">
        ///     Indicates if the audio playback session is complete (i.e. on uninterrupted stream of voice). This will be set for the last frame of audio.
        /// </param>
        void OnAudioPlayback(ArraySegment<float> data, bool complete);
    }
}
