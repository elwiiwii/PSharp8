using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace PSharp8;

public class GameOrchestrator : IDisposable
{
    private record SceneAssets(
        SpriteMapData Data,
        SpriteTextureManager TextureManager,
        Texture2D SpriteTexture,
        Texture2D MapTexture,
        Dictionary<string, SoundEffect> Sfx);

    private readonly string _sfxDirectory;
    private readonly AudioManager _audioManager;
    private readonly GraphicsManager _graphicsManager;
    private readonly InputManager _inputManager;
    private readonly MathManager? _mathManager;    // TODO
    private readonly MemoryManager? _memoryManager; // TODO
    private readonly SceneManager _sceneManager;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly PaletteManager _paletteManager;
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly TextureCache _textureCache;
    private readonly Dictionary<IScene, SceneAssets> _sceneAssets = new();
    private SpriteMapData? _currentSmData;

    public GameOrchestrator(
        string musicDirectory,
        string sfxDirectory,
        string texturesDirectory,
        IScene defaultScene,
        GraphicsDevice graphicsDevice,
        GraphicsDeviceManager graphicsDeviceManager,
        GameWindow window,
        InputBindings? bindings = null,
        BtnpConfig? btnpConfig = null)
    {
        _audioManager = new AudioManager(
            musicDirectory ?? throw new ArgumentNullException(nameof(musicDirectory)));
        _sfxDirectory = sfxDirectory ?? throw new ArgumentNullException(nameof(sfxDirectory));
        ArgumentNullException.ThrowIfNull(texturesDirectory, nameof(texturesDirectory));
        _inputManager = new InputManager(bindings ?? InputBindings.Default, btnpConfig);

        ArgumentNullException.ThrowIfNull(defaultScene);
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentNullException.ThrowIfNull(graphicsDeviceManager);
        ArgumentNullException.ThrowIfNull(window);

        _graphicsDevice = graphicsDevice;
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _paletteManager = new PaletteManager();
        _textureCache = new TextureCache(graphicsDevice, texturesDirectory);

        var pixel = new Texture2D(graphicsDevice, 1, 1);
        pixel.SetData(new[] { Color.White });
        _pixel = pixel;

        var stm = LoadSceneAssets(defaultScene);
        _currentSmData = _sceneAssets[defaultScene].Data;

        _graphicsManager = new GraphicsManager(
            _spriteBatch,
            () => (128, 128),
            graphicsDeviceManager,
            graphicsDevice,
            _paletteManager,
            pixel,
            stm,
            _textureCache,
            window);

        _sceneManager = new SceneManager(
            _inputManager,
            initialScene: defaultScene,
            onSceneCreated: s => LoadSceneAssets(s),
            onBeforeSceneCallbacks: s =>
            {
                if (_sceneAssets.TryGetValue(s, out var assets))
                {
                    _graphicsManager.SetSpriteTextureManager(assets.TextureManager);
                    _audioManager.SetSfxDictionary(assets.Sfx);
                    _currentSmData = assets.Data;
                }
            },
            onSceneRemoved: RemoveSceneAssets);

        // TODO construct remaining managers
        _mathManager = null;
        _memoryManager = null;

        Initialize();
    }

    internal AudioManager AudioManager => _audioManager;
    internal GraphicsManager GraphicsManager => _graphicsManager;
    internal InputManager InputManager => _inputManager;
    internal MathManager MathManager => _mathManager!;
    internal MemoryManager MemoryManager => _memoryManager!;
    internal SceneManager SceneManager => _sceneManager;
    internal SpriteMapData SmManager => _currentSmData!;

    private void Initialize()
    {
        Pico8.Initialize(this);
    }

    private SpriteTextureManager LoadSceneAssets(IScene scene)
    {
        Texture2D spriteTexture;
        Texture2D mapTexture;
        string flagString;

        if (scene.SpritesPath is not null && scene.MapPath is not null)
        {
            using var spriteStream = File.OpenRead(scene.SpritesPath);
            spriteTexture = Texture2D.FromStream(_graphicsDevice, spriteStream);
            using var mapStream = File.OpenRead(scene.MapPath);
            mapTexture = Texture2D.FromStream(_graphicsDevice, mapStream);
            flagString = scene.FlagData is not null ? File.ReadAllText(scene.FlagData) : "";
        }
        else
        {
            spriteTexture = new Texture2D(_graphicsDevice, 8, 8);
            mapTexture = new Texture2D(_graphicsDevice, 8, 8);
            flagString = "";
        }

        var smData = new SpriteMapData(spriteTexture, mapTexture, flagString);
        var lru = new LruCache<SpriteSnapshot, Texture2D>();
        var stm = new SpriteTextureManager(_graphicsDevice, _paletteManager, smData, lru);

        var sfx = new Dictionary<string, SoundEffect>();
        if (Directory.Exists(_sfxDirectory))
        {
            foreach (var sfxPack in scene.Sfx)
            {
                foreach (var wavPath in Directory.EnumerateFiles(_sfxDirectory, "*.wav"))
                {
                    var filename = Path.GetFileNameWithoutExtension(wavPath);
                    if (filename.StartsWith(sfxPack.Prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        using var stream = File.OpenRead(wavPath);
                        sfx[filename] = SoundEffect.FromStream(stream);
                    }
                }
            }
        }

        _sceneAssets[scene] = new SceneAssets(smData, stm, spriteTexture, mapTexture, sfx);
        return stm;
    }

    private void RemoveSceneAssets(IScene scene)
    {
        if (_sceneAssets.TryGetValue(scene, out var assets))
        {
            assets.TextureManager.Dispose();
            assets.SpriteTexture.Dispose();
            assets.MapTexture.Dispose();
            foreach (var sfx in assets.Sfx.Values)
                sfx.Dispose();
            _sceneAssets.Remove(scene);
        }
    }

    public void UpdateInput(TimeSpan elapsed, IReadOnlyList<InputEvent> events)
        => _inputManager.Update(elapsed, events);

    public void Update(TimeSpan elapsed)
    {
        _sceneManager.InternalUpdate(elapsed);
        _audioManager.Update(elapsed);
    }

    public void Draw(TimeSpan elapsed)
    {
        _graphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
            SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
        _sceneManager.InternalDraw(elapsed);
        _spriteBatch.End();
        _textureCache.Tick();
        foreach (var assets in _sceneAssets.Values)
            assets.TextureManager.Tick();
    }

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
        foreach (var scene in _sceneAssets.Keys.ToList())
            RemoveSceneAssets(scene);
        _pixel.Dispose();
        _spriteBatch.Dispose();
        _textureCache.Dispose();
        _audioManager.Dispose();
    }
}

