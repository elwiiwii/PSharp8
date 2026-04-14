namespace PSharp8.Input;

/// <summary>
/// Event-driven input manager. Processes timestamped <see cref="InputEvent"/> records each frame.
/// </summary>
/// <remarks>
/// <para><b>Status:</b> Deprecated in favour of <see cref="InputManager"/> with an
/// <see cref="IInputProvider"/> (polling path). Use <see cref="EventInputManager"/> only when
/// you need sub-frame tap detection or per-source debounce.</para>
///
/// <para><b>Call-order bug (root cause of input latency):</b><br/>
/// When wiring this class into an FNA/XNA game loop, you <em>must</em> call
/// <c>base.Update(gameTime)</c> <b>before</b> collecting events and calling
/// <c>UpdateInput()</c>. If you drain the SDL event buffer and call <c>UpdateInput</c> first,
/// FNA has not yet processed SDL events for the current frame — every event arrives
/// exactly one frame late. The correct order is:</para>
/// <code>
/// protected override void Update(GameTime gameTime)
/// {
///     base.Update(gameTime);            // FNA fires SDL_AddEventWatch here
///     // NOW drain event buffer
///     _orchestrator.UpdateInput(elapsed, events);
///     _orchestrator.Update(elapsed);    // game logic runs with current-frame input
/// }
/// </code>
/// </remarks>
[Obsolete(
    "Use InputManager with IInputProvider (polling path). " +
    "If reactivating event-based input, ensure base.Update() is called before UpdateInput() " +
    "in the host game loop — see remarks for details.")]
internal class EventInputManager
{
    private const int ButtonCount = 7; // PicoButton values 0–6

    private InputBindings _bindings;
    private BtnpConfig _config;

    internal InputBindings Bindings => _bindings;

    private readonly bool[] _heldNow = new bool[ButtonCount];
    private readonly bool[] _wasHeld = new bool[ButtonCount];
    private readonly bool[] _pressedThisFrame = new bool[ButtonCount];
    private readonly double[] _heldMs = new double[ButtonCount];
    private readonly double[] _lastRepeatMs = new double[ButtonCount];
    private readonly Dictionary<InputSource, ulong> _lastStateChangeNs = new();
    private Dictionary<InputSource, List<PicoButton>> _sourceToButtons = new();

    public EventInputManager(InputBindings bindings, BtnpConfig? config = null)
    {
        _bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
        _config = config ?? new BtnpConfig();
        BuildReverseMap();
    }

    public bool InputBlocked { get; set; }

    private void BuildReverseMap()
    {
        _sourceToButtons = new Dictionary<InputSource, List<PicoButton>>();
        foreach (PicoButton button in Enum.GetValues<PicoButton>())
        {
            foreach (InputSource source in _bindings[button])
            {
                if (!_sourceToButtons.TryGetValue(source, out var list))
                    _sourceToButtons[source] = list = new List<PicoButton>();
                list.Add(button);
            }
        }
    }

    private void ClearState()
    {
        Array.Clear(_heldNow, 0, ButtonCount);
        Array.Clear(_wasHeld, 0, ButtonCount);
        Array.Clear(_pressedThisFrame, 0, ButtonCount);
        Array.Clear(_heldMs, 0, ButtonCount);
        Array.Clear(_lastRepeatMs, 0, ButtonCount);
        _lastStateChangeNs.Clear();
    }

    public void Update(TimeSpan elapsed, IReadOnlyList<InputEvent> events)
    {
        // Snapshot held state for edge detection
        Array.Copy(_heldNow, _wasHeld, ButtonCount);

        // Clear per-frame pressed flag
        Array.Clear(_pressedThisFrame, 0, ButtonCount);

        // Process events in order
        foreach (InputEvent evt in events)
        {
            if (IsDebounced(evt))
                continue;

            _lastStateChangeNs[evt.Source] = evt.TimestampNs;

            if (!_sourceToButtons.TryGetValue(evt.Source, out var buttons))
                continue;

            foreach (PicoButton button in buttons)
            {
                int i = (int)button;
                if (evt.IsDown)
                {
                    _heldNow[i] = true;
                    _pressedThisFrame[i] = true;
                }
                else
                {
                    _heldNow[i] = false;
                }
            }
        }

        // Update hold timing
        double elapsedMs = elapsed.TotalMilliseconds;
        for (int i = 0; i < ButtonCount; i++)
            UpdateButtonTiming(i, elapsedMs);
    }

    private bool IsDebounced(InputEvent evt)
    {
        if (_config.DebounceMs <= 0.0) return false;
        if (!_lastStateChangeNs.TryGetValue(evt.Source, out ulong lastNs)) return false;
        return evt.TimestampNs - lastNs < (ulong)(_config.DebounceMs * 1_000_000.0);
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

    public void SetBindings(InputBindings bindings)
    {
        _bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
        BuildReverseMap();
        ClearState();
    }

    internal void UpdateConfig(BtnpConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
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

        // Fresh press or sub-frame tap
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
