using Microsoft.Xna.Framework.Audio;
using NVorbis;

namespace PSharp8.Audio;

internal sealed class OggStreamPlayer : IDisposable
{
    private const int BufferFloatCount = 16384; // ~185ms at 44100Hz stereo

    private readonly VorbisReader _reader;
    private readonly bool _loop;
    private readonly float[] _floatBuffer;
    private readonly byte[] _pcmBuffer;
    private bool _disposed;

    internal DynamicSoundEffectInstance Instance { get; }

    internal OggStreamPlayer(string filePath, bool loop)
    {
        _loop = loop;
        _floatBuffer = new float[BufferFloatCount];
        _pcmBuffer = new byte[BufferFloatCount * 2]; // 16-bit = 2 bytes per sample
        _reader = new VorbisReader(filePath);
        Instance = new DynamicSoundEffectInstance(
            _reader.SampleRate,
            _reader.Channels == 1 ? AudioChannels.Mono : AudioChannels.Stereo);
        Instance.BufferNeeded += OnBufferNeeded;
    }

    /// <summary>
    /// Unsubscribes from <see cref="DynamicSoundEffectInstance.BufferNeeded"/> so the stream
    /// stops submitting new audio data. The instance drains its existing queue and stops
    /// naturally. Call this when the track is being faded out and audio data is no longer needed.
    /// </summary>
    internal void StopStreaming()
    {
        Instance.BufferNeeded -= OnBufferNeeded;
    }

    private void OnBufferNeeded(object? sender, EventArgs e)
    {
        if (_disposed)
            return;

        int samplesRead = _reader.ReadSamples(_floatBuffer, 0, _floatBuffer.Length);

        if (samplesRead == 0)
        {
            if (!_loop)
                return; // let the instance exhaust its queue and stop naturally

            _reader.SamplePosition = 0;
            samplesRead = _reader.ReadSamples(_floatBuffer, 0, _floatBuffer.Length);
            if (samplesRead == 0)
                return;
        }

        int byteCount = samplesRead * 2;
        for (int i = 0; i < samplesRead; i++)
        {
            short s = (short)Math.Clamp(_floatBuffer[i] * 32767f, short.MinValue, short.MaxValue);
            _pcmBuffer[i * 2] = (byte)(s & 0xFF);
            _pcmBuffer[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
        }

        Instance.SubmitBuffer(_pcmBuffer, 0, byteCount);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        // Unsubscribe first so no new callbacks can be enqueued after this point.
        Instance.BufferNeeded -= OnBufferNeeded;
        // Set flag so any callback already in-flight returns early before SubmitBuffer.
        _disposed = true;
        Instance.Stop();
        Instance.Dispose();
        _reader.Dispose();
    }
}
