using Microsoft.Xna.Framework.Audio;

namespace PSharp8;

public class GameOrchestrator : IDisposable
{
    // TODO make not nullable once all managers are implemented and initialized in the constructor
    private readonly AudioManager _audioManager;
    private readonly GraphicsManager? _graphicsManager;
    private readonly InputManager _inputManager;
    private readonly MathManager? _mathManager;
    private readonly MemoryManager? _memoryManager;
    private readonly SceneManager? _sceneManager;
    private readonly SpriteMapData? _smManager;

    public GameOrchestrator(
        string musicDirectory,
        Dictionary<string, SoundEffect> sfxDictionary,
        InputBindings? bindings = null,
        BtnpConfig? btnpConfig = null)
    {
        _audioManager = new AudioManager(
            musicDirectory ?? throw new ArgumentNullException(nameof(musicDirectory)),
            sfxDictionary ?? throw new ArgumentNullException(nameof(sfxDictionary)));

        _inputManager = new InputManager(bindings ?? InputBindings.Default, btnpConfig);

        // TODO construct remaining managers
        _graphicsManager = null;
        _mathManager = null;
        _memoryManager = null;
        _sceneManager = null;
        _smManager = null;

        Initialize();
    }

    internal AudioManager AudioManager => _audioManager;
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

    public void ApplyInputSettings(InputBindings bindings, BtnpConfig? btnpConfig = null)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        _inputManager.SetBindings(bindings);
        if (btnpConfig is not null)
            _inputManager.UpdateConfig(btnpConfig);
    }

    public void ApplyAudioSettings(int musicVolume, int sfxVolume)
    {
        if (musicVolume < 0 || musicVolume > 100)
            throw new ArgumentOutOfRangeException(nameof(musicVolume), musicVolume, "Must be 0–100.");
        if (sfxVolume < 0 || sfxVolume > 100)
            throw new ArgumentOutOfRangeException(nameof(sfxVolume), sfxVolume, "Must be 0–100.");

        _audioManager.SetMusicVolume(musicVolume / 100f);
        _audioManager.SetSfxVolume(sfxVolume / 100f);
    }

    public void Dispose()
    {
        _audioManager?.Dispose();
    }
}
