namespace PSharp8.Input;

internal class InputManager : IInputManager
{
    private const int ButtonCount = 7; // PicoButton values 0–6

    private readonly IInputProvider _provider;
    private BtnpConfig _config;

    private readonly bool[] _heldNow = new bool[ButtonCount];
    private readonly bool[] _wasHeld = new bool[ButtonCount];
    private readonly bool[] _pressedThisFrame = new bool[ButtonCount];
    private readonly double[] _heldMs = new double[ButtonCount];
    private readonly double[] _lastRepeatMs = new double[ButtonCount];

    public InputManager(IInputProvider provider, BtnpConfig? config = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _config = config ?? new BtnpConfig();
    }

    public bool InputBlocked { get; set; }

    internal void UpdateConfig(BtnpConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public void Update(TimeSpan elapsed)
    {
        // Snapshot held state for edge detection
        Array.Copy(_heldNow, _wasHeld, ButtonCount);

        // Clear per-frame pressed flag
        Array.Clear(_pressedThisFrame, 0, ButtonCount);

        // Poll the provider for current held state
        var held = _provider.GetHeldButtons();
        for (int i = 0; i < ButtonCount; i++)
        {
            _heldNow[i] = held[i];
            if (held[i] && !_wasHeld[i])
                _pressedThisFrame[i] = true;
        }

        // Update hold timing
        double elapsedMs = elapsed.TotalMilliseconds;
        for (int i = 0; i < ButtonCount; i++)
            UpdateButtonTiming(i, elapsedMs);
    }

    private void UpdateButtonTiming(int i, double elapsedMs)
    {
        if (_heldNow[i])
        {
            _heldMs[i] += elapsedMs;
        }
        else
        {
            _heldMs[i] = 0.0;
            _lastRepeatMs[i] = 0.0;
        }

        // Reset repeat baseline on fresh press
        if (_pressedThisFrame[i] && !_wasHeld[i])
            _lastRepeatMs[i] = _heldMs[i];
    }

    public bool Btn(int button, int player)
    {
        if (InputBlocked) return false;
        return _heldNow[button];
    }

    public bool Btnp(int button, int player)
    {
        if (InputBlocked) return false;

        // Pause never auto-repeats
        if (button == (int)PicoButton.Pause)
            return _pressedThisFrame[button] && !_wasHeld[button];

        // Fresh press
        if (_pressedThisFrame[button] && !_wasHeld[button])
            return true;

        // Auto-repeat
        if (_heldNow[button] && _heldMs[button] >= _config.InitialRepeatMs)
        {
            if (_heldMs[button] - _lastRepeatMs[button] >= _config.SubsequentRepeatMs)
            {
                _lastRepeatMs[button] = _heldMs[button];
                return true;
            }
        }

        return false;
    }
}
