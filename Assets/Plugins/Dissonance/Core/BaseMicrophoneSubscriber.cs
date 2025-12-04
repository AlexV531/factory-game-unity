using System;
using Dissonance.Audio.Capture;
using Dissonance.Datastructures;
using NAudio.Wave;
using UnityEngine;

namespace Dissonance
{
    /// <summary>
    /// This class is a convenient helper for using `IMicrophoneSubscriber` - it copies data across to the main thread for processing.
    /// </summary>
    public abstract class BaseMicrophoneSubscriber
        : MonoBehaviour, IMicrophoneSubscriber
    {
        private WaveFormat _format;
        private readonly TransferBuffer<float> _transfer = new TransferBuffer<float>(capacity: 4096);
        private bool _resetPending;
        private int _lostSamples;

        private readonly float[] _temporary = new float[800];

        void IMicrophoneSubscriber.ReceiveMicrophoneData(ArraySegment<float> buffer, WaveFormat format)
        {
            // If we don't know the format save it now
            if (_format == null)
            {
                _format = format;
                _resetPending = true;
            }

            // If the format has changed, clear the buffer
            if (!_format.Equals(format))
            {
                _format = format;
                _resetPending = true;
                _transfer.Clear();
                _lostSamples = 0;
                return;
            }

            // Write as much data as possible to the buffer
            var written = _transfer.WriteSome(buffer);
            _lostSamples += buffer.Count - written;
        }

        void IMicrophoneSubscriber.Reset()
        {
            _transfer.Clear();
            _resetPending = true;
        }

        public virtual void Update()
        {
            if (_resetPending)
            {
                if (_format == null)
                    return;
                _resetPending = false;
                ResetAudioStream(_format);
            }

            // Keep reading as much data as possible
            var loop = true;
            while (loop)
            {
                // Clear the temporary array, ready to write more data into it
                Array.Clear(_temporary, 0, _temporary.Length);

                // If there are any lost samples shrink the read array by that amount to inject silence (no more than 50% of the data may be silence)
                var silence = Math.Min(_temporary.Length / 2, _lostSamples);
                var read = new ArraySegment<float>(_temporary, 0, _temporary.Length - silence);

                // Read the reduced size array segment from the buffer, but then submit the entire array to the user.
                // This means the unread section is filled with silence
                if (_transfer.Read(read))
                {
                    _lostSamples -= silence;
                    ProcessAudio(new ArraySegment<float>(_temporary));
                }
                else
                    loop = false;
            }
        }

        /// <summary>
        /// Process the given array of data. **Do not** save a reference to this array, it will be re-used by the audio system!
        /// </summary>
        /// <param name="data"></param>
        protected abstract void ProcessAudio(ArraySegment<float> data);

        /// <summary>
        /// This is called whenever the audio stream is interrupted (e.g. microphone stopped recording for some reason) or the audio format is changed.
        /// Reset your consuming system here.
        /// </summary>
        /// <param name="waveFormat"></param>
        protected abstract void ResetAudioStream(WaveFormat waveFormat);
    }
}

