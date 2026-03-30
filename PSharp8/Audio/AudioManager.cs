using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace PSharp8.Audio;

internal class AudioManager
{
    private List<Soundtrack>? _soundtracks;
    private Soundtrack? _activeSoundtrack;
    private readonly FadeController _fade = new();
    private readonly PlaybackController _playback;

    internal AudioManager(Dictionary<string, SoundEffect> musicDictionary)
    {
        _playback = new PlaybackController(musicDictionary ?? throw new ArgumentNullException(nameof(musicDictionary)));
    }

    #region State Properties

    internal bool IsPlaying => _playback.IsPlaying;
    internal bool IsFading => _fade.IsFading;
    internal int? CurrentTrackIndex => _playback.CurrentTrackIndex;
    internal float CurrentVolume => _playback.CurrentVolume;
    internal SoundEffectInstance? CurrentInstance => _playback.CurrentInstance;
    internal SoundEffectInstance? OutgoingInstance => _playback.OutgoingInstance;
    internal int CurrentPartIndex => _playback.CurrentPartIndex;

    #endregion
    #region PICO-8 API

    internal void Music(int n, int fadeMs)
    {
        if (_activeSoundtrack is null)
            return;

        if (n >= 0 && n >= _activeSoundtrack.Tracks.Count)
            throw new ArgumentOutOfRangeException(nameof(n));

        if (n < 0)
        {
            if (!_playback.HasCurrentInstance)
                return;

            if (fadeMs > 0)
            {
                _fade.BeginFade(fadeMs, _playback.CurrentVolume, 0f, fadingOut: true);
            }
            else
            {
                _playback.StopAndDispose();
                _fade.Reset();
            }
            return;
        }

        // Fade-out reversal: same track replayed during fade-out
        if (_fade.IsFading && _fade.IsFadingOut && n == _playback.CurrentTrackIndex)
        {
            var reversalMs = (int)Math.Max(_fade.FadeElapsedMs, 1);
            _fade.BeginFade(reversalMs, _playback.CurrentVolume, 1f, fadingOut: false);
            return;
        }

        // Crossfade reversal: old track replayed during active crossfade
        if (_fade.IsFading && !_fade.IsFadingOut && _playback.HasOutgoingInstance && n == _playback.OutgoingTrackIndex)
        {
            _playback.ReverseCrossfade(n, _activeSoundtrack.Tracks[n]);
            var reversalMs = (int)Math.Max(_fade.FadeElapsedMs, 1);
            _fade.BeginFade(reversalMs, _playback.CurrentVolume, 1f, fadingOut: false);
            return;
        }

        if (_playback.HasCurrentInstance)
        {
            if (fadeMs > 0)
                _playback.MoveCurrentToOutgoing();
            else
                _playback.DisposeCurrent();
        }

        var track = _activeSoundtrack.Tracks[n];
        _playback.StartTrack(track, n);

        if (fadeMs > 0)
        {
            _playback.ApplyVolume(0f);
            _fade.BeginFade(fadeMs, 0f, 1f, fadingOut: false);
        }
        else
        {
            _playback.ApplyVolume(1f);
            _fade.Reset();
        }

        _playback.Play();
    }

    internal void FadeVolume(float targetPercent, int fadeMs)
    {
        targetPercent = Math.Clamp(targetPercent, 0f, 1f);

        if (fadeMs <= 0)
        {
            _playback.ApplyVolume(targetPercent);
            return;
        }

        _fade.BeginFade(fadeMs, _playback.CurrentVolume, targetPercent, fadingOut: false);
    }

    internal void RestoreVolume(int fadeMs)
    {
        if (_playback.CurrentVolume >= 1f)
            return;

        FadeVolume(1f, fadeMs);
    }

    internal void Sfx(int n)
    {
        throw new NotImplementedException();
    }

    internal void Update(GameTime gameTime)
    {
        if (_playback.NeedsPartAdvance)
        {
            if (_playback.AdvancePart())
                _fade.Reset();
        }

        if (!_fade.IsFading)
            return;

        var volume = _fade.Update(gameTime.ElapsedGameTime.TotalMilliseconds);
        if (volume.HasValue)
            _playback.ApplyVolume(volume.Value);

        if (_fade.IsComplete)
            CompleteFade();
    }

    #endregion
    #region State Control

    internal void SetSoundtracks(List<Soundtrack> soundtracks)
    {
        _soundtracks = soundtracks ?? throw new ArgumentNullException(nameof(soundtracks));
    }

    internal void SetActiveSoundtrack(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        _activeSoundtrack = _soundtracks?.Find(s => s.Name == name)
            ?? throw new KeyNotFoundException($"Soundtrack '{name}' not found.");
    }

    internal void CompleteFade()
    {
        if (!_fade.IsFading)
            return;

        var wasFadingOut = _fade.IsFadingOut;
        var target = _fade.Complete();

        if (wasFadingOut)
        {
            _playback.StopAndDispose();
        }
        else
        {
            _playback.DisposeOutgoing();
            if (target.HasValue)
                _playback.ApplyVolume(target.Value);
        }
    }

    #endregion
}