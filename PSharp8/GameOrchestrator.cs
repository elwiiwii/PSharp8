namespace PSharp8;

public class GameOrchestrator
{
    private readonly AudioManager? _audioManager;
    private readonly GraphicsManager? _graphicsManager;
    private readonly InputManager _inputManager;
    private readonly MathManager? _mathManager;
    private readonly MemoryManager? _memoryManager;
    private readonly SceneManager? _sceneManager;
    private readonly SpriteMapData? _smManager;

    public GameOrchestrator(
        InputBindings? bindings = null,
        BtnpConfig? btnpConfig = null)
    {
        _inputManager = new InputManager(bindings ?? InputBindings.Default, btnpConfig);

        // TODO construct remaining managers
        _audioManager = null;
        _graphicsManager = null;
        _mathManager = null;
        _memoryManager = null;
        _sceneManager = null;
        _smManager = null;

        Initialize();
    }

    internal AudioManager AudioManager => _audioManager!;
    internal GraphicsManager GraphicsManager => _graphicsManager!;
    internal InputManager InputManager => _inputManager;
    internal MathManager MathManager => _mathManager!;
    internal MemoryManager MemoryManager => _memoryManager!;
    internal SceneManager SceneManager => _sceneManager!;
    internal SpriteMapData SmManager => _smManager!;

    private void Initialize()
    {
        Pico8.Initialize(this);
    }

    public void UpdateInput(TimeSpan elapsed, IReadOnlyList<InputEvent> events)
        => _inputManager.Update(elapsed, events);
}
