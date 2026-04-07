using Microsoft.Xna.Framework.Audio;

namespace PSharp8.Audio;

internal class AudioManager : IDisposable
{
    private List<Soundtrack>? _soundtracks;
    private Soundtrack? _activeSoundtrack;
    private readonly FadeController _fade = new();
    private readonly PlaybackController _playback;

    private Dictionary<string, SoundEffect> _sfxDictionary = [];
    private List<SfxPack>? _sfxPacks;
    private SfxPack? _activeSfxPack;
    internal readonly List<SoundEffectInstance> _sfxInstances = [];

    internal AudioManager(string musicDirectory)
    {
        _playback = new PlaybackController(musicDirectory ?? throw new ArgumentNullException(nameof(musicDirectory)));
    }

    internal void SetSfxDictionary(Dictionary<string, SoundEffect> dict)
    {
        _sfxDictionary = dict ?? throw new ArgumentNullException(nameof(dict));
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
            throw new InvalidOperationException("No soundtrack set. Call SetSoundtracks and SetActiveSoundtrack first.");

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

    internal void Sfx(int n)
    {
        if (_activeSfxPack is null)
            throw new InvalidOperationException("No active SFX pack set. Call SetSfxPacks and SetActiveSfxPack first.");

        var key = _activeSfxPack.Prefix + n;
        if (!_sfxDictionary.TryGetValue(key, out var soundEffect))
            throw new KeyNotFoundException($"Sound effect '{key}' not found in SFX dictionary.");

        var newInstance = soundEffect.CreateInstance();
        _sfxInstances.Add(newInstance);
        newInstance.Play();
    }

    #endregion
    #region API Extensions

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

    #endregion
    #region Volume Settings

    internal float MusicBaseVolume { get; private set; } = 1f;
    internal float SfxBaseVolume { get; private set; } = 1f;

    internal void SetMusicVolume(float volume)
    {
        MusicBaseVolume = volume;
        if (_playback.HasCurrentInstance)
            _playback.ApplyVolume(volume);
    }

    internal void SetSfxVolume(float volume)
    {
        SfxBaseVolume = volume;
        foreach (var instance in _sfxInstances)
            instance.Volume = volume;
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

    internal void SetSfxPacks(List<SfxPack> sfxPacks)
    {
        _sfxPacks = sfxPacks ?? throw new ArgumentNullException(nameof(sfxPacks));
    }

    internal void SetActiveSfxPack(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (_sfxPacks is null)
            throw new InvalidOperationException("No SFX packs have been loaded. Call SetSfxPacks first.");

        _activeSfxPack = _sfxPacks.Find(p => p.Name == name)
            ?? throw new KeyNotFoundException($"SFX pack '{name}' not found.");
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

    internal void CleanUpFinishedSfx()
    {
        for (var i = _sfxInstances.Count - 1; i >= 0; i--)
        {
            if (_sfxInstances[i].State == SoundState.Stopped)
            {
                _sfxInstances[i].Dispose();
                _sfxInstances.RemoveAt(i);
            }
        }
    }

    internal void Update(TimeSpan elapsed)
    {
        CleanUpFinishedSfx();

        if (_playback.NeedsPartAdvance)
        {
            if (_playback.AdvancePart())
                _fade.Reset();
        }

        if (!_fade.IsFading)
            return;

        var volume = _fade.Update(elapsed.TotalMilliseconds);
        if (volume.HasValue)
            _playback.ApplyVolume(volume.Value);

        if (_fade.IsComplete)
            CompleteFade();
    }

    public void Dispose()
    {
        _playback.StopAndDispose();

        foreach (var instance in _sfxInstances)
        {
            instance.Stop();
            instance.Dispose();
        }
        _sfxInstances.Clear();
    }

    #endregion
}