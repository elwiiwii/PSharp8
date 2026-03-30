namespace PSharp8.Audio;

internal class FadeController
{
    private bool _isFading;
    private bool _fadingOut;
    private int _fadeTotalMs;
    private double _fadeElapsedMs;
    private float _fadeStartVolume;
    private float _fadeTargetVolume;

    internal bool IsFading => _isFading;
    internal bool IsFadingOut => _fadingOut;
    internal double FadeElapsedMs => _fadeElapsedMs;
    internal bool IsComplete => _isFading && _fadeElapsedMs >= _fadeTotalMs;

    internal void BeginFade(int fadeMs, float startVolume, float targetVolume, bool fadingOut)
    {
        _isFading = true;
        _fadingOut = fadingOut;
        _fadeTotalMs = fadeMs;
        _fadeElapsedMs = 0;
        _fadeStartVolume = startVolume;
        _fadeTargetVolume = targetVolume;
    }

    internal float? Update(double elapsedMs)
    {
        if (!_isFading)
            return null;

        _fadeElapsedMs += elapsedMs;
        var progress = (float)Math.Min(_fadeElapsedMs / _fadeTotalMs, 1.0);
        var volume = _fadeStartVolume + (_fadeTargetVolume - _fadeStartVolume) * progress;

        if (progress >= 1f)
            return null; // caller should call Complete()

        return volume;
    }

    internal float? Complete()
    {
        if (!_isFading)
            return null;

        var target = _fadeTargetVolume;
        var wasFadingOut = _fadingOut;
        Reset();
        return target;
    }

    internal void Reset()
    {
        _isFading = false;
        _fadingOut = false;
        _fadeTotalMs = 0;
        _fadeElapsedMs = 0;
        _fadeStartVolume = 0f;
        _fadeTargetVolume = 0f;
    }
}
