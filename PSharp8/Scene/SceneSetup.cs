namespace PSharp8.Scene;

internal class SceneSetup : ISceneSetup
{
    private readonly List<FunctionRegistration> _updateRegistrations = [];
    private readonly List<FunctionRegistration> _drawRegistrations = [];

    private (int Width, int Height) _activeResolution = (128, 128);
    private (int Width, int Height) _pendingResolution = (128, 128);
    private bool _hasPendingResolution;

    public (int Width, int Height) Resolution
    {
        get => _pendingResolution;
        set
        {
            _pendingResolution = value;
            _hasPendingResolution = true;
        }
    }

    internal (int Width, int Height) ActiveResolution => _activeResolution;
    internal IReadOnlyList<FunctionRegistration> UpdateRegistrations => _updateRegistrations;
    internal IReadOnlyList<FunctionRegistration> DrawRegistrations => _drawRegistrations;

    internal void ApplyPendingResolution()
    {
        if (_hasPendingResolution)
        {
            _activeResolution = _pendingResolution;
            _hasPendingResolution = false;
        }
    }

    internal void ResetAccumulators()
    {
        foreach (var reg in _updateRegistrations)
            reg.Accumulator = TimeSpan.Zero;
        foreach (var reg in _drawRegistrations)
            reg.Accumulator = TimeSpan.Zero;
    }

    public IFunctionHandle RegisterUpdate(Action callback, double fps,
        PauseBehavior pauseBehavior = PauseBehavior.Pause)
    {
        var reg = new FunctionRegistration { Callback = callback, Fps = fps, PauseBehavior = pauseBehavior };
        _updateRegistrations.Add(reg);
        return reg;
    }

    public IFunctionHandle RegisterDraw(Action callback, double fps,
        PauseBehavior pauseBehavior = PauseBehavior.Pause)
    {
        var reg = new FunctionRegistration { Callback = callback, Fps = fps, PauseBehavior = pauseBehavior };
        _drawRegistrations.Add(reg);
        return reg;
    }
}
