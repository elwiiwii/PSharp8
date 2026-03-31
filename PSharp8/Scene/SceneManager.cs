namespace PSharp8.Scene;

internal class SceneManager
{
    private readonly IInputManager _inputManager;
    private Func<IScene>? _pendingSchedule;
    private Func<IScene>? _pendingPush;
    private bool _pendingPop;
    private readonly List<(IScene Scene, SceneSetup Setup)> _stack = [];

    internal SceneManager(IInputManager inputManager)
    {
        _inputManager = inputManager ?? throw new ArgumentNullException(nameof(inputManager));
    }

    internal void ScheduleScene(Func<IScene> factory)
    {
        _pendingSchedule = factory;
        _pendingPush = null;
        _pendingPop = false;
    }

    internal void PushScene(Func<IScene> factory)
    {
        _pendingPush = factory;
    }

    internal void PopScene()
    {
        _pendingPop = true;
    }

    internal void InternalUpdate(TimeSpan elapsed)
    {
        if (_pendingSchedule is not null)
        {
            var factory = _pendingSchedule;
            _pendingSchedule = null;
            _stack.Clear();
            _stack.Add(CreateAndInitScene(factory));
        }
        else if (_pendingPush is not null)
        {
            var factory = _pendingPush;
            _pendingPush = null;
            _stack.Add(CreateAndInitScene(factory));
        }
        else if (_pendingPop)
        {
            _pendingPop = false;
            if (_stack.Count > 0)
                _stack.RemoveAt(_stack.Count - 1);
            if (_stack.Count > 0)
                _stack[^1].Setup.ResetAccumulators();
        }

        if (_stack.Count == 0) return;

        foreach (var entry in _stack)
            entry.Setup.ApplyPendingResolution();

        for (int i = 0; i < _stack.Count; i++)
        {
            bool isTop = i == _stack.Count - 1;
            var setup = _stack[i].Setup;
            foreach (var reg in setup.UpdateRegistrations)
            {
                if (!reg.Enabled) continue;
                if (!isTop && reg.PauseBehavior == PauseBehavior.Pause) continue;
                reg.Accumulator += elapsed;
                var interval = TimeSpan.FromSeconds(1.0 / reg.Fps);
                while (reg.Accumulator >= interval)
                {
                    reg.Accumulator -= interval;
                    if (!isTop && reg.PauseBehavior == PauseBehavior.ContinueWithoutInputs)
                    {
                        _inputManager.InputBlocked = true;
                        reg.Callback();
                        _inputManager.InputBlocked = false;
                    }
                    else
                    {
                        reg.Callback();
                    }
                }
            }
        }
    }

    private static (IScene Scene, SceneSetup Setup) CreateAndInitScene(Func<IScene> factory)
    {
        var setup = new SceneSetup();
        var scene = factory();
        scene.Init(setup);
        setup.ApplyPendingResolution();
        return (scene, setup);
    }
}