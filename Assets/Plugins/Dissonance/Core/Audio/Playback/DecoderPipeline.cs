using System;
using System.Collections.Generic;
using Dissonance.Audio.Codecs;
using Dissonance.Datastructures;
using Dissonance.Extensions;
using Dissonance.Networking;
using JetBrains.Annotations;
using NAudio.Wave;

namespace Dissonance.Audio.Playback
{
    internal class DecoderPipeline
        : IDecoderPipeline, IVolumeProvider, IRemoteChannelProvider, IRateProvider
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Playback, nameof(DecoderPipeline));

        private readonly Action<DecoderPipeline> _completionHandler;
        private readonly TransferBuffer<VoicePacket> _inputBuffer;
        private readonly ConcurrentPool<byte[]> _bytePool;
        private readonly ConcurrentPool<List<RemoteChannel>> _channelListPool;
        private readonly BufferedDecoder _source;
        private readonly SynchronizerSampleSource _synchronizer;
        private readonly Resampler _resampler;
        private readonly ISampleSource _output;

        private volatile bool _prepared;
        private volatile bool _complete;
        private bool _sourceClosed;

        private readonly TimeSpan _frameDuration;
        private DateTime? _firstFrameArrival;
        private uint _firstFrameSeq;

        public int BufferCount => _source.BufferCount + _inputBuffer.EstimatedUnreadCount;

        public TimeSpan BufferTime => TimeSpan.FromTicks(BufferCount * _frameDuration.Ticks);

        public float PacketLoss => _source.PacketLoss;

        public PlaybackOptions PlaybackOptions => _source.LatestPlaybackOptions;

        public WaveFormat OutputFormat => _output.WaveFormat;

        public TimeSpan InputFrameTime => _frameDuration;

        float IRateProvider.PlaybackRate => _synchronizer.PlaybackRate;

        public SyncState SyncState => _synchronizer.State;

        public string ID { get; }

        #endregion

        #region constructor
        public DecoderPipeline([NotNull] IVoiceDecoder decoder, uint inputFrameSize, [NotNull] Action<DecoderPipeline> completionHandler, string id, bool softClip = true)
        {
            if (decoder == null) throw new ArgumentNullException(nameof(decoder));
            if (completionHandler == null) throw new ArgumentNullException(nameof(completionHandler));

            ID = id;

            _completionHandler = completionHandler;
            _inputBuffer = new TransferBuffer<VoicePacket>(128);

            // we need buffers to hold the encoded frames, but we have no idea how large an encoded frame is! These buffers are large enough...
            // ...to hold a frame with no compression whatsoever, so they should be large enough to hold the frame when it's opus compressed.
            _bytePool = new ConcurrentPool<byte[]>(12, () => new byte[inputFrameSize * decoder.Format.Channels * 4]);
            _channelListPool = new ConcurrentPool<List<RemoteChannel>>(12, () => new List<RemoteChannel>());

            _frameDuration = TimeSpan.FromSeconds((double)inputFrameSize / decoder.Format.SampleRate);
            _firstFrameArrival = null;
            _firstFrameSeq = 0;

            // Buffer decoded packets and decode them on demand
            var source = new BufferedDecoder(decoder, inputFrameSize, decoder.Format, RecycleFrame);
            _source = source;

            // Every time the volume changes, change is smoothly over the course of a single frame
            var ramped = new VolumeRampedFrameSource(source, this);

            // Convert stream of fixed size frames into a stream of samples taken in arbitrary chunks
            var samples = new FrameToSampleConverter(ramped);

            // Monitor stream sync and adjust playback rate to stay close in sync
            _synchronizer = new SynchronizerSampleSource(samples, TimeSpan.FromSeconds(1));

            // Resample the data to the output rate
            _resampler = new Resampler(_synchronizer, this);

            // Chain a soft clip stage if necessary
            _output = softClip ? (ISampleSource)new SoftClipSampleSource(_resampler) : _resampler;
        }
        #endregion

        private void RecycleFrame(VoicePacket packet)
        {
            // ReSharper disable once AssignNullToNotNullAttribute (Justification: Arr segment internal array is not null)
            _bytePool.Put(packet.EncodedAudioFrame.Array);

            if (packet.Channels != null)
            {
                packet.Channels.Clear();
                _channelListPool.Put(packet.Channels);
            }
        }

        /// <summary>
        /// Prepare the pipeline to begin playing a new stream of audio
        /// </summary>
        /// <param name="context"></param>
        public void Prepare(SessionContext context)
        {
            _output.Prepare(context);

            _prepared = true;
        }

        public void EnableDynamicSync()
        {
            _synchronizer.Enable();
        }

        public void SetOutputSampleRate(int? rate)
        {
            _resampler.SetOutputRate(rate);
        }

        public bool Read(ArraySegment<float> samples)
        {
            FlushTransferBuffer();
            var complete = _output.Read(samples);

            if (complete)
                _completionHandler(this);

            return complete;
        }

        /// <summary>
        /// Deliver a new encoded audio packet
        /// </summary>
        /// <remarks>It will be placed into a transfer buffer and will be moved into the actual buffer when FlushTransferBuffer is called on the audio playback thread</remarks>
        /// <param name="packet"></param>
        /// <param name="now"></param>
        /// <returns>How delayed this packet is from when it should arrive</returns>
        public float Push(VoicePacket packet, DateTime now)
        {
            Log.Trace("Received frame {0} from network", packet.SequenceNumber);

            // copy the data out of the frame, as the network thread will re-use the heap allocated things
            List<RemoteChannel> channelsCopy = null;
            if (packet.Channels != null)
            {
                channelsCopy = _channelListPool.Get();
                channelsCopy.Clear();
                channelsCopy.AddRange(packet.Channels);
            }

            var frameCopy = packet.EncodedAudioFrame.CopyToSegment(_bytePool.Get());

            var packetCopy = new VoicePacket(
                packet.SenderPlayerId,
                packet.PlaybackOptions.Priority,
                packet.PlaybackOptions.AmplitudeMultiplier,
                packet.PlaybackOptions.IsPositional,
                frameCopy,
                packet.SequenceNumber,
                channelsCopy
            );

            // queue the frame onto the transfer buffer
            if (!_inputBuffer.TryWrite(packetCopy))
                Log.Warn("Failed to write an encoded audio packet into the input transfer buffer");

            // We've received a packet but not been prepared yet, which means audio playback hasn't started yet - immediately flush the transfer buffer
            // It's safe to do this because we know playback isn't going, so the audio thread won't be accessing the buffer at the same time as we're flushing
            if (!_prepared)
                FlushTransferBuffer();

            // calculate how late the packet is
            if (!_firstFrameArrival.HasValue)
            {
                _firstFrameArrival = now;
                _firstFrameSeq = packet.SequenceNumber;

                return 0;
            }
            else
            {
                var expectedTime = _firstFrameArrival.Value + TimeSpan.FromTicks(_frameDuration.Ticks * (packet.SequenceNumber - _firstFrameSeq));
                var delay = now - expectedTime;

                return (float)delay.TotalSeconds;
            }
        }

        public void Stop()
        {
            _complete = true;
        }

        public void Reset()
        {
            _output.Reset();

            _firstFrameArrival = null;
            _prepared = false;
            _complete = false;
            _sourceClosed = false;

            VolumeProvider = null;
        }

        /// <summary>
        /// Flush the transfer buffer to the encoded audio buffer.
        ///
        /// THIS IS NOT THREAD SAFE! Don't call this once playback has started.
        /// </summary>
        public void FlushTransferBuffer()
        {
            // empty the transfer buffer into the decoder buffer
            while (_inputBuffer.Read(out var frame))
                _source.Push(frame);

            // set the complete flag after flushing the transfer buffer
            if (_complete && !_sourceClosed)
            {
                _sourceClosed = true;
                _source.Stop();
            }
        }

        #region IVolumeProvider implementation
        public IVolumeProvider VolumeProvider { get; set; }

        float IVolumeProvider.TargetVolume => (VolumeProvider?.TargetVolume ?? 1) * PlaybackOptions.AmplitudeMultiplier;

        #endregion

        public void GetRemoteChannels(List<RemoteChannel> output)
        {
            output.Clear();

            _source.GetRemoteChannels(output);
        }
    }
}
