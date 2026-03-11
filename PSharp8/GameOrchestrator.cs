namespace PSharp8;

public class GameOrchestrator : IDisposable
{
    // Private
    private readonly AudioManager _audioManager;
    private readonly GraphicsManager _graphicsManager;
    private readonly InputManager _inputManager;
    private readonly MathManager _mathManager;
    private readonly MemoryManager _memoryManager;
    private readonly SceneManager _sceneManager;
    private readonly SpriteMapData _sfmManager;

    //private IScene _currentScene;
    private readonly IScene _defaultScene;

    public GameOrchestrator(
        AudioManager audioManager,
        GraphicsManager graphicsManager,
        InputManager inputManager,
        MathManager mathManager,
        MemoryManager memoryManager,
        SceneManager sceneManager,
        SpriteMapData sfmManager,
        IScene defaultScene)
    {
        _audioManager = audioManager ?? throw new ArgumentNullException(nameof(audioManager));
        _graphicsManager = graphicsManager ?? throw new ArgumentNullException(nameof(graphicsManager));
        _inputManager = inputManager ?? throw new ArgumentNullException(nameof(inputManager));
        _mathManager = mathManager ?? throw new ArgumentNullException(nameof(mathManager));
        _memoryManager = memoryManager ?? throw new ArgumentNullException(nameof(memoryManager));
        _sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
        _sfmManager = sfmManager ?? throw new ArgumentNullException(nameof(sfmManager));
        _defaultScene = defaultScene ?? throw new ArgumentNullException(nameof(defaultScene));
    }

    // Public
    public AudioManager AudioManager => _audioManager;
    public GraphicsManager GraphicsManager => _graphicsManager;
    public InputManager InputManager => _inputManager;
    public MathManager MathManager => _mathManager;
    public MemoryManager MemoryManager => _memoryManager;
    public SceneManager SceneManager => _sceneManager;
    public SpriteMapData SfmManager => _sfmManager;

//    public GameOrchestrator(
//        IInputStateManager inputManager,
//        IGraphicsAPI graphicsAPI,
//        IAudioAPI audioAPI,
//        ISceneManager sceneManager,
//        ICartDataLoader? cartDataLoader = null,
//        GameHostContext? host = null,
//        IScene? cart = null,
//        Func<IScene>? titleSceneFactory = null,
//        IPaletteManager? paletteManager = null,
//        IMapManager? mapManager = null,
//        IDisplayManager? displayManager = null)
//    {
//        _inputManager = inputManager ?? throw new ArgumentNullException(nameof(inputManager));
//        _ = graphicsAPI ?? throw new ArgumentNullException(nameof(graphicsAPI));
//        _ = audioAPI ?? throw new ArgumentNullException(nameof(audioAPI));
//        _sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
//
//        // Scene & host defaults (Null Object pattern — no null! anywhere)
//        _currentCart = cart ?? NullScene.Instance;
//        _titleSceneFactory = titleSceneFactory ?? (() => _currentCart);
//        _scenes = host?.Scenes ?? [];
//        _textureDictionary = host?.TextureDictionary ?? [];
//        _audioSettings = host?.AudioSettings ?? InMemorySettings.Default;
//        _displaySettings = host?.DisplaySettings ?? InMemorySettings.Default;
//        _inputBindings = host?.InputBindings ?? DefaultInputBindings.Instance;
//
//        _displayManager = displayManager
//            ?? (host != null
//                ? new DisplayManager(host.Graphics, host.GraphicsDevice, host.Window, host.DisplaySettings)
//                : new DisplayManager(null, null, null, null));
//
//        _cartDataLoader = cartDataLoader ?? new CartDataLoader();
//        _graphicsOrch = new GraphicsManager(graphicsAPI, paletteManager);
//        _audioOrch = new AudioOrchestrator(audioAPI);
//        _mapManager = mapManager;
//        _colors = Pico8Utils.DefaultColors;
//        _pauseMenuState = new PauseMenuState(this);
//    }

    public void Initialize()
    {
//        Pico8.Initialize(this);
//        _initialized = true;
//        if (_currentCart != null)
//        {
//            LoadCart(_currentCart);
//        }
    }

    public void LoadCart(IScene cart)
    {
//        _ = cart ?? throw new ArgumentNullException(nameof(cart));
//
//        _currentCart?.Dispose();
//        _cartData = CartData.Empty;
//        _currentCart = cart;
//
//        _inputManager.Reset();
//        SoundDispose();
//        UpdateViewport();
//
//        _pauseMenuState?.Reset();
//        _pauseMenuState?.InitializeMenuStructure();
//
//        _sceneManager.TransitionToScene(cart);
//
//        Reload();
//        _currentCart.Init();
    }

//    public void ReloadCart() => LoadCart(_currentCart);

    public void ScheduleScene(Func<IScene> sceneFactory)
    {
//        _sceneManager.ScheduleScene(sceneFactory);
    }

    private void Reload()
    {
//        DisposeManagers();
//
//        _cartData = _cartDataLoader.Load(_currentCart, _colors, _textureDictionary);
//
//        _trackManager = new TrackManager(
//            () => _cartData.Music,
//            () => _cartData.Sfx,
//            _audioSettings);
    }

    public void Update()
    {
//        if (!_initialized)
//            throw new InvalidOperationException("GameOrchestrator must be initialized before Update().");
//
//        _displayManager.RecalculateCell(_currentCart.Resolution);
//
//        // System hotkeys (Ctrl+Q/R/M/F)
//        HandleHotkeys();
//
//        if (!(_currentCart.SceneName == "TitleScreen") && _inputManager.Btnp(6))
//        {
//            _pauseMenuState?.TogglePause();
//        }
//        _inputManager.UpdatePauseButton();
//
//        if (_pauseMenuState?.IsPaused ?? false)
//        {
//            _inputManager.SetPauseMode(true);
//            _inputManager.UpdateLockout();
//
//            _pauseMenuState?.HandleMenuInput(
//                upPressed: _inputManager.Btnp(2),
//                downPressed: _inputManager.Btnp(3),
//                selectPressed: _inputManager.Btnp(0) || _inputManager.Btnp(1) ||
//                    _inputManager.Btnp(4) || _inputManager.Btnp(5),
//                leftPressed: _inputManager.Btnp(0),
//                rightPressed: _inputManager.Btnp(1),
//                actionAPressed: _inputManager.Btnp(4),
//                actionBPressed: _inputManager.Btnp(5));
//
//            _audioOrch.PlaySound(false);
//        }
//        else
//        {
//            _inputManager.SetPauseMode(false);
//            _inputManager.UpdateLockout();
//
//            _audioOrch.PlaySound(true);
//
//            try
//            {
//                _currentCart.Update();
//            }
//            catch (Exception ex)
//            {
//                HandleSceneException(ex, "Update");
//            }
//
//            var scheduledScene = _sceneManager.GetAndClearScheduledScene();
//            if (scheduledScene is not null)
//            {
//                LoadCart(scheduledScene());
//            }
//        }
//
//        _inputManager.Update();
//        _audioOrch.Update();
//        _popupService?.Update();
    }

    private void HandleHotkeys()
    {
//        bool isCtrlDown = _inputManager.IsKeyDown(Keys.LeftControl) || _inputManager.IsKeyDown(Keys.RightControl);
//        if (!isCtrlDown) return;
//
//        if (_inputManager.IsKeyJustPressed(Keys.Q))
//        {
//            QuitToTitle();
//        }
//        else if (_inputManager.IsKeyJustPressed(Keys.R))
//        {
//            ReloadCart();
//        }
//        else if (_inputManager.IsKeyJustPressed(Keys.M))
//        {
//            ToggleSound();
//        }
//        else if (_inputManager.IsKeyJustPressed(Keys.F))
//        {
//            ToggleFullscreen();
//        }
    }

    private void HandleSceneException(Exception ex, string phase)
    {
//        Console.WriteLine($"Scene error in {phase}: {ex.Message}");
//        Console.WriteLine(ex.StackTrace);
//
//        // Only show a new error popup if one isn't already active (debounce)
//        if (_popupService is not null && !_popupService.HasActivePopup)
//        {
//            Notifications.ShowError(ex.Message);
//        }
//    }
//
//    public void Draw()
//    {
//        if (!_initialized)
//            throw new InvalidOperationException("GameOrchestrator must be initialized before Draw().");
//
//        _graphicsOrch.Pal();
//        _graphicsOrch.Palt();
//
//        try
//        {
//            _currentCart.Draw();
//        }
//        catch (Exception ex)
//        {
//            HandleSceneException(ex, "Draw");
//        }
//
//        if (_pauseMenuState?.IsPaused ?? false)
//        {
//            _pauseMenuRenderer?.DrawPauseMenu(
//                _pauseMenuState.CurrentMenuItems,
//                _pauseMenuState.SelectedIndex,
//                _displayManager.Cell);
//        }
//
//        _popupService?.Draw(_displayManager.Resolution);
    }

    public void UpdateViewport()
    {
//        _displayManager.UpdateViewport(_currentCart);
    }

    public void SoundDispose()
    {
//        _audioOrch.SoundDispose();
    }

    /// <summary>
    /// Set display configuration (virtual resolution and cell size).
    /// In production, these are computed from viewport + scene resolution.
    /// This method provides test access to set them directly.
    /// </summary>
    public void SetDisplayConfig((int w, int h) resolution, (int Width, int Height) cell)
    {
//        _displayManager.SetDisplayConfig(resolution, cell);
    }

    /// <summary>
    /// Toggle fullscreen mode, applying graphics and viewport changes.
    /// Persists settings and shows a notification popup.
    /// </summary>
    public void ToggleFullscreen()
    {
//        _displaySettings.IsFullscreen = !_displaySettings.IsFullscreen;
//        _displaySettings.Save();
//        _displayManager.ToggleFullscreen(_currentCart);
//        Notifications.Show($"fullscreen {(_displaySettings.IsFullscreen ? "on" : "off")} (ctrl-f)");
    }

    /// <summary>
    /// Toggle sound on/off. Persists settings, mutes audio if disabled,
    /// and shows a notification popup.
    /// </summary>
    public void ToggleSound()
    {
//        _audioSettings.SoundEnabled = !_audioSettings.SoundEnabled;
//        _audioSettings.Save();
//        if (!_audioSettings.SoundEnabled)
//        {
//            _audioOrch.Mute();
//        }
//        Notifications.Show($"sound {(_audioSettings.SoundEnabled ? "on" : "off")} (ctrl-m)");
    }

    /// <summary>
    /// Quit to the title screen (first scene in Scenes list).
    /// Shows a notification popup.
    /// </summary>
    public void QuitToTitle()
    {
//        ScheduleScene(_titleSceneFactory);
//        Notifications.Show("quit (ctrl-q)");
    }

    /// <summary>
    /// Play background music by index. Delegates to AudioOrchestrator.
    /// </summary>
    public void PlayMusic(int n)
    {
//        _audioOrch.Music(n);
    }

    private void DisposeManagers()
    {
//        _audioOrch.Dispose();
    }

    public void Dispose()
    {
        DisposeManagers();
    }
}
