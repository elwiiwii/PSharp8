namespace PSharp8.Scene;

internal class FunctionRegistration : IFunctionHandle
{
    public double Fps { get; set; }
    public PauseBehavior PauseBehavior { get; set; }
    public bool Enabled { get; set; } = true;
    public required Action Callback { get; init; }
    public TimeSpan Accumulator { get; set; }
}

internal class SceneSetup : ISceneSetup
{
    private readonly List<FunctionRegistration> _updateRegistrations = [];
    private readonly List<FunctionRegistration> _drawRegistrations = [];

    public (int Width, int Height) Resolution { get; set; } = (128, 128);

    internal IReadOnlyList<FunctionRegistration> UpdateRegistrations => _updateRegistrations;

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

internal class SceneManager
{
    private readonly IInputManager _inputManager;
    private Func<IScene>? _pendingScene;
    private SceneSetup? _activeSetup;

    internal SceneManager(IInputManager inputManager)
    {
        _inputManager = inputManager ?? throw new ArgumentNullException(nameof(inputManager));
    }

    internal void ScheduleScene(Func<IScene> factory)
    {
        _pendingScene = factory;
    }

    internal void InternalUpdate(TimeSpan elapsed)
    {
        if (_pendingScene is not null)
        {
            var scene = _pendingScene();
            _pendingScene = null;
            _activeSetup = new SceneSetup();
            scene.Init(_activeSetup);
        }

        if (_activeSetup is null) return;

        foreach (var reg in _activeSetup.UpdateRegistrations)
        {
            if (!reg.Enabled) continue;
            reg.Accumulator += elapsed;
            var interval = TimeSpan.FromSeconds(1.0 / reg.Fps);
            while (reg.Accumulator >= interval)
            {
                reg.Accumulator -= interval;
                reg.Callback();
            }
        }
    }
}