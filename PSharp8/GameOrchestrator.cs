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
    private readonly string _texturesDirectory;
    private readonly AudioManager _audioManager;
    private readonly GraphicsManager _graphicsManager;
    private readonly IInputManager _inputManager;
    private readonly IInputProvider _provider;
    private readonly MathManager _mathManager;
    private readonly MemoryManager? _memoryManager; // TODO
    private readonly SceneManager _sceneManager;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly PaletteManager _paletteManager;
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly TextureCache _textureCache;
    private RenderTarget2D _gameRenderTarget;
    private readonly Dictionary<IScene, SceneAssets> _sceneAssets = new();
    private SpriteMapData? _currentSmData;
    private bool _isDrawing;

    public GameOrchestrator(
        string musicDirectory,
        string sfxDirectory,
        string texturesDirectory,
        IScene defaultScene,
        GraphicsDevice graphicsDevice,
        GraphicsDeviceManager graphicsDeviceManager,
        GameWindow window,
        IInputProvider? inputProvider = null,
        BtnpConfig? btnpConfig = null,
        IInputManager? inputManager = null)
    {
        _audioManager = new AudioManager(
            musicDirectory ?? throw new ArgumentNullException(nameof(musicDirectory)));
        _sfxDirectory = sfxDirectory ?? throw new ArgumentNullException(nameof(sfxDirectory));
        _texturesDirectory = texturesDirectory ?? throw new ArgumentNullException(nameof(texturesDirectory));
        _provider = inputProvider ?? new NullInputProvider();
        _inputManager = inputManager ?? new InputManager(_provider, btnpConfig);

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

        var (rtW, rtH) = _sceneManager.TopResolution;
        _gameRenderTarget = CreateGameRenderTarget(rtW, rtH);

        _mathManager = new MathManager();
        _memoryManager = null; // TODO

        Initialize();
    }

    internal AudioManager AudioManager => _audioManager;
    internal GraphicsManager GraphicsManager => _graphicsManager;
    internal IInputManager InputManager => _inputManager;
    internal MathManager MathManager => _mathManager;
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
            var spritesFullPath = Path.Combine(_texturesDirectory, scene.SpritesPath + ".png");
            var mapFullPath     = Path.Combine(_texturesDirectory, scene.MapPath + ".png");
            using var spriteStream = File.OpenRead(spritesFullPath);
            spriteTexture = Texture2D.FromStream(_graphicsDevice, spriteStream);
            using var mapStream = File.OpenRead(mapFullPath);
            mapTexture = Texture2D.FromStream(_graphicsDevice, mapStream);
            flagString = scene.FlagData is not null ? File.ReadAllText(scene.FlagData) : "";
        }
        else
        {
            spriteTexture = new Texture2D(_graphicsDevice, 128, 128);
            mapTexture = new Texture2D(_graphicsDevice, 128 * 8, 64 * 8);
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

    public void UpdateInput(TimeSpan elapsed)
        => _inputManager.Update(elapsed);

    /// <inheritdoc cref="UpdateInput(TimeSpan)"/>
    [Obsolete(
        "Pass events via IInputProvider instead. " +
        "Use UpdateInput(TimeSpan elapsed) with a PollingInputProvider or custom IInputProvider. " +
        "If reactivating this overload, ensure base.Update() is called before UpdateInput() — " +
        "see EventInputManager remarks for details.")]
    public void UpdateInput(TimeSpan elapsed, IReadOnlyList<InputEvent> events)
    {
        // Delegate to the polling overload; event list is intentionally ignored.
        // Reactivate EventInputManager to restore full event-path behaviour.
        _inputManager.Update(elapsed);
    }

    public void Update(TimeSpan elapsed)
    {
        _sceneManager.InternalUpdate(elapsed);
        _audioManager.Update(elapsed);
    }

    /// <summary>Start a draw frame (idempotent: no-op if already in a frame).</summary>
    public void BeginFrame()
    {
        if (_isDrawing) return;
        _isDrawing = true;
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
            SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
    }

    /// <summary>End the current draw frame (no-op if not in a frame).</summary>
    public void EndFrame()
    {
        if (!_isDrawing) return;
        _isDrawing = false;
        _spriteBatch.End();
    }

    /// <summary>True while a draw frame is active (between BeginFrame and EndFrame).</summary>
    public bool IsDrawing => _isDrawing;

    public void Draw(TimeSpan elapsed)
    {
        var (resW, resH) = _sceneManager.TopResolution;
        if (_gameRenderTarget.Width != resW || _gameRenderTarget.Height != resH)
        {
            _gameRenderTarget.Dispose();
            _gameRenderTarget = CreateGameRenderTarget(resW, resH);
        }

        _graphicsDevice.SetRenderTarget(_gameRenderTarget);
        BeginFrame();
        _sceneManager.InternalDraw(elapsed);
        EndFrame();
        _graphicsDevice.SetRenderTarget(null);

        PresentGameToScreen();

        _textureCache.Tick();
        foreach (var assets in _sceneAssets.Values)
            assets.TextureManager.Tick();
    }

    private RenderTarget2D CreateGameRenderTarget(int w, int h)
        => new RenderTarget2D(_graphicsDevice, w, h, false,
            SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

    private void PresentGameToScreen()
    {
        var pp = _graphicsDevice.PresentationParameters;
        int windowW = pp.BackBufferWidth;
        int windowH = pp.BackBufferHeight;
        int rtW = _gameRenderTarget.Width;
        int rtH = _gameRenderTarget.Height;

        int scale = Math.Max(1, Math.Min(windowW / rtW, windowH / rtH));
        int scaledW = rtW * scale;
        int scaledH = rtH * scale;
        int offsetX = (windowW - scaledW) / 2;
        int offsetY = (windowH - scaledH) / 2;

        _graphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque,
            SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
        _spriteBatch.Draw(_gameRenderTarget,
            new Rectangle(offsetX, offsetY, scaledW, scaledH),
            Color.White);
        _spriteBatch.End();
    }

    public void ApplyInputSettings(InputBindings bindings, BtnpConfig? btnpConfig = null)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        _provider.SetBindings(bindings);
        if (btnpConfig is not null && _inputManager is InputManager realManager)
            realManager.UpdateConfig(btnpConfig);
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

    public void LoadSoundtracks(IReadOnlyList<Soundtrack> soundtracks, string activeSoundtrack)
    {
        ArgumentNullException.ThrowIfNull(soundtracks);
        ArgumentNullException.ThrowIfNull(activeSoundtrack);

        _audioManager.SetSoundtracks(soundtracks.ToList());
        _audioManager.SetActiveSoundtrack(activeSoundtrack);
    }

    public void LoadSfxPacks(IReadOnlyList<SfxPack> sfxPacks, string activePack)
    {
        ArgumentNullException.ThrowIfNull(sfxPacks);
        ArgumentNullException.ThrowIfNull(activePack);
        _audioManager.SetSfxPacks(sfxPacks.ToList());
        _audioManager.SetActiveSfxPack(activePack);
    }

    public void Dispose()
    {
        foreach (var scene in _sceneAssets.Keys.ToList())
            RemoveSceneAssets(scene);
        _pixel.Dispose();
        _spriteBatch.Dispose();
        _textureCache.Dispose();
        _audioManager.Dispose();
        _gameRenderTarget.Dispose();
    }

    /// <summary>
    /// Default <see cref="IInputProvider"/> used when the caller does not supply one.
    /// Reports all buttons as not held and ignores binding changes.
    /// </summary>
    private sealed class NullInputProvider : IInputProvider
    {
        public bool[] GetHeldButtons() => new bool[7];
        public void SetBindings(InputBindings bindings) { }
    }
}

