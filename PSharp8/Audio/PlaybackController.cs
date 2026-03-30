using Microsoft.Xna.Framework.Audio;

namespace PSharp8.Audio;

internal class PlaybackController
{
    private readonly Dictionary<string, SoundEffect> _musicDictionary;

    private SoundEffectInstance? _currentInstance;
    private SoundEffectInstance? _outgoingInstance;
    private int? _currentTrackIndex;
    private int? _currentChannel;
    private float _currentVolume;
    private int _currentPartIndex;
    private Track? _currentTrack;
    private int? _outgoingTrackIndex;
    private double _playbackPositionMs;

    internal PlaybackController(Dictionary<string, SoundEffect> musicDictionary)
    {
        _musicDictionary = musicDictionary ?? throw new ArgumentNullException(nameof(musicDictionary));
    }

    #region State Properties

    internal SoundEffectInstance? CurrentInstance => _currentInstance;
    internal SoundEffectInstance? OutgoingInstance => _outgoingInstance;
    internal int CurrentPartIndex => _currentPartIndex;
    internal bool IsPlaying => _currentTrackIndex is not null;
    internal int? CurrentTrackIndex => _currentTrackIndex;
    internal float CurrentVolume => _currentVolume;
    internal bool HasCurrentInstance => _currentInstance is not null;
    internal bool HasOutgoingInstance => _outgoingInstance is not null;
    internal int? OutgoingTrackIndex => _outgoingTrackIndex;
    internal double PlaybackPositionMs
    {
        get => _playbackPositionMs;
        set => _playbackPositionMs = value;
    }

    internal bool NeedsPartAdvance =>
        _currentInstance is not null && _currentTrack is not null
        && _currentInstance.State == SoundState.Stopped && !_currentInstance.IsLooped;

    #endregion
    #region Instance Lifecycle

    internal void StartTrack(Track track, int trackIndex)
    {
        var sameChannel = _currentChannel.HasValue && _currentChannel.Value == track.Channel;
        if (!sameChannel)
            _playbackPositionMs = 0.0;

        _currentInstance = CreateInstanceFromPart(track.Parts[0]);
        _currentTrackIndex = trackIndex;
        _currentChannel = track.Channel;
        _currentPartIndex = 0;
        _currentTrack = track;
    }

    internal void Play() => _currentInstance?.Play();

    internal void MoveCurrentToOutgoing()
    {
        _outgoingInstance = _currentInstance;
        _outgoingInstance!.Volume = 0f;
        _outgoingTrackIndex = _currentTrackIndex;
        _currentInstance = null;
    }

    internal void ReverseCrossfade(int trackIndex, Track track)
    {
        DisposeInstance(ref _currentInstance);
        _currentInstance = _outgoingInstance;
        _outgoingInstance = null;
        _outgoingTrackIndex = null;
        _currentTrackIndex = trackIndex;
        _currentTrack = track;
        _currentChannel = track.Channel;
    }

    internal void DisposeOutgoing() => DisposeInstance(ref _outgoingInstance);

    internal void DisposeCurrent() => DisposeInstance(ref _currentInstance);

    internal void ApplyVolume(float volume)
    {
        _currentVolume = volume;
        if (_currentInstance is not null)
            _currentInstance.Volume = volume;
    }

    /// <summary>
    /// Advances to the next part. Returns <c>true</c> if playback ended (no more parts).
    /// </summary>
    internal bool AdvancePart()
    {
        var nextIndex = _currentPartIndex + 1;
        if (nextIndex >= _currentTrack!.Parts.Count)
        {
            StopAndDispose();
            return true;
        }

        DisposeInstance(ref _currentInstance);
        _currentPartIndex = nextIndex;
        _currentInstance = CreateInstanceFromPart(_currentTrack.Parts[nextIndex]);
        _currentInstance.Volume = _currentVolume;
        _currentInstance.Play();
        return false;
    }

    internal void StopAndDispose()
    {
        DisposeInstance(ref _outgoingInstance);
        DisposeInstance(ref _currentInstance);
        _currentTrackIndex = null;
        _currentChannel = null;
        _currentVolume = 0f;
        _playbackPositionMs = 0.0;
        _currentPartIndex = 0;
        _currentTrack = null;
        _outgoingTrackIndex = null;
    }

    #endregion
    #region Helpers

    private SoundEffectInstance CreateInstanceFromPart(TrackPart part)
    {
        var soundEffect = _musicDictionary[part.Filename];
        var instance = soundEffect.CreateInstance();
        instance.IsLooped = part.Loop;
        return instance;
    }

    internal static void DisposeInstance(ref SoundEffectInstance? instance)
    {
        instance?.Stop();
        instance?.Dispose();
        instance = null;
    }

    #endregion
}
