using Microsoft.Xna.Framework.Audio;

namespace PSharp8.Audio;

internal class PlaybackController
{
    private readonly string _musicDirectory;

    private OggStreamPlayer? _currentPlayer;
    private OggStreamPlayer? _outgoingPlayer;
    private int? _currentTrackIndex;
    private int? _currentChannel;
    private float _currentVolume;
    private int _currentPartIndex;
    private Track? _currentTrack;
    private int? _outgoingTrackIndex;
    private double _playbackPositionMs;

    internal PlaybackController(string musicDirectory)
    {
        _musicDirectory = musicDirectory ?? throw new ArgumentNullException(nameof(musicDirectory));
    }

    #region State Properties

    internal SoundEffectInstance? CurrentInstance => _currentPlayer?.Instance;
    internal SoundEffectInstance? OutgoingInstance => _outgoingPlayer?.Instance;
    internal int CurrentPartIndex => _currentPartIndex;
    internal bool IsPlaying => _currentTrackIndex is not null;
    internal int? CurrentTrackIndex => _currentTrackIndex;
    internal float CurrentVolume => _currentVolume;
    internal bool HasCurrentInstance => _currentPlayer is not null;
    internal bool HasOutgoingInstance => _outgoingPlayer is not null;
    internal int? OutgoingTrackIndex => _outgoingTrackIndex;
    internal double PlaybackPositionMs
    {
        get => _playbackPositionMs;
        set => _playbackPositionMs = value;
    }

    internal bool NeedsPartAdvance =>
        _currentPlayer is not null && _currentTrack is not null
        && _currentPlayer.Instance.State == SoundState.Stopped;

    #endregion
    #region Instance Lifecycle

    internal void StartTrack(Track track, int trackIndex)
    {
        var sameChannel = _currentChannel.HasValue && _currentChannel.Value == track.Channel;
        if (!sameChannel)
            _playbackPositionMs = 0.0;

        _currentPlayer = CreatePlayerFromPart(track.Parts[0]);
        _currentTrackIndex = trackIndex;
        _currentChannel = track.Channel;
        _currentPartIndex = 0;
        _currentTrack = track;
    }

    internal void Play() => _currentPlayer?.Instance.Play();

    internal void MoveCurrentToOutgoing()
    {
        _outgoingPlayer = _currentPlayer;
        _outgoingPlayer!.StopStreaming(); // no longer need audio data for a track being faded out
        _outgoingPlayer.Instance.Volume = 0f;
        _outgoingTrackIndex = _currentTrackIndex;
        _currentPlayer = null;
    }

    internal void ReverseCrossfade(int trackIndex, Track track)
    {
        DisposePlayer(ref _currentPlayer);
        _currentPlayer = _outgoingPlayer;
        _outgoingPlayer = null;
        _outgoingTrackIndex = null;
        _currentTrackIndex = trackIndex;
        _currentTrack = track;
        _currentChannel = track.Channel;
    }

    internal void DisposeOutgoing() => DisposePlayer(ref _outgoingPlayer);

    internal void DisposeCurrent() => DisposePlayer(ref _currentPlayer);

    internal void ApplyVolume(float volume)
    {
        _currentVolume = volume;
        if (_currentPlayer is not null)
            _currentPlayer.Instance.Volume = volume;
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

        DisposePlayer(ref _currentPlayer);
        _currentPartIndex = nextIndex;
        _currentPlayer = CreatePlayerFromPart(_currentTrack.Parts[nextIndex]);
        _currentPlayer.Instance.Volume = _currentVolume;
        _currentPlayer.Instance.Play();
        return false;
    }

    internal void StopAndDispose()
    {
        DisposePlayer(ref _outgoingPlayer);
        DisposePlayer(ref _currentPlayer);
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

    private OggStreamPlayer CreatePlayerFromPart(TrackPart part)
    {
        var path = Path.Combine(_musicDirectory, part.Filename);
        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"Music file '{part.Filename}' not found in directory '{_musicDirectory}'.", path);
        return new OggStreamPlayer(path, part.Loop);
    }

    private static void DisposePlayer(ref OggStreamPlayer? player)
    {
        player?.Dispose();
        player = null;
    }

    #endregion
}
