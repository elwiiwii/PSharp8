using FluentAssertions;
using Microsoft.Xna.Framework.Input;
using PSharp8.Audio;
using PSharp8.Input;
using PSharp8.Scene;
using PSharp8.Tests.Infrastructure;
using Xunit;

namespace PSharp8.Tests;

[Collection("Fna")]
public class GameOrchestratorGraphicsTests : GraphicsTestBase
{
    public GameOrchestratorGraphicsTests(FnaFixture fixture) : base(fixture)
    {
    }

    // --------------------------------------------------
    #region Helpers
    // --------------------------------------------------

    private GameOrchestrator BuildOrchestrator(IScene? scene = null)
    {
        scene ??= new NullScene();
        return new GameOrchestrator(
            ".",
            ".",
            ".",
            scene,
            _gd,
            _fixture.GraphicsDeviceManager,
            _fixture.Window);
    }

    private sealed class NullScene : IScene
    {
        public string? Name => null;
        public void Init(ISceneSetup setup) { }
        public string? SpritesPath => null;
        public string? MapPath => null;
        public string? FlagData => null;
        public IReadOnlyList<Soundtrack> Music => [];
        public IReadOnlyList<SfxPack> Sfx => [];
    }

    private sealed class TestScene(Action<ISceneSetup> configure) : IScene
    {
        public string? Name => null;
        public void Init(ISceneSetup setup) => configure(setup);
        public string? SpritesPath => null;
        public string? MapPath => null;
        public string? FlagData => null;
        public IReadOnlyList<Soundtrack> Music => [];
        public IReadOnlyList<SfxPack> Sfx => [];
    }

    // --------------------------------------------------
    #endregion
    #region Constructor — null guards
    // --------------------------------------------------

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenMusicDirectoryIsNull()
    {
        var act = () => new GameOrchestrator(
            null!,
            ".",
            ".",
            new NullScene(),
            _gd,
            _fixture.GraphicsDeviceManager,
            _fixture.Window);

        act.Should().Throw<ArgumentNullException>().WithParameterName("musicDirectory");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenSfxDirectoryIsNull()
    {
        var act = () => new GameOrchestrator(
            ".",
            null!,
            ".",
            new NullScene(),
            _gd,
            _fixture.GraphicsDeviceManager,
            _fixture.Window);

        act.Should().Throw<ArgumentNullException>().WithParameterName("sfxDirectory");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenTexturesDirectoryIsNull()
    {
        var act = () => new GameOrchestrator(
            ".",
            ".",
            null!,
            new NullScene(),
            _gd,
            _fixture.GraphicsDeviceManager,
            _fixture.Window);

        act.Should().Throw<ArgumentNullException>().WithParameterName("texturesDirectory");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenDefaultSceneIsNull()
    {
        var act = () => new GameOrchestrator(
            ".",
            ".",
            ".",
            null!,
            _gd,
            _fixture.GraphicsDeviceManager,
            _fixture.Window);

        act.Should().Throw<ArgumentNullException>().WithParameterName("defaultScene");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenGraphicsDeviceManagerIsNull()
    {
        var act = () => new GameOrchestrator(
            ".",
            ".",
            ".",
            new NullScene(),
            _gd,
            null!,
            _fixture.Window);

        act.Should().Throw<ArgumentNullException>().WithParameterName("graphicsDeviceManager");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenWindowIsNull()
    {
        var act = () => new GameOrchestrator(
            ".",
            ".",
            ".",
            new NullScene(),
            _gd,
            _fixture.GraphicsDeviceManager,
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("window");
    }

    // --------------------------------------------------
    #endregion
    #region Construction with graphics
    // --------------------------------------------------

    [Fact]
    public void Constructor_BuildsGraphicsManager_WhenAllGraphicsParamsProvided()
    {
        using var sut = BuildOrchestrator();

        sut.GraphicsManager.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_BuildsSceneManager_WhenAllGraphicsParamsProvided()
    {
        using var sut = BuildOrchestrator();

        sut.SceneManager.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_LoadsSpriteAssetsForDefaultScene_ImmediatelyWithoutUpdate()
    {
        using var sut = BuildOrchestrator();

        sut.SmManager.Should().NotBeNull();
    }

    // --------------------------------------------------
    #endregion
    #region Update
    // --------------------------------------------------

    [Fact]
    public void Update_DoesNotThrow_WhenGraphicsInitialized()
    {
        using var sut = BuildOrchestrator();

        var act = () => sut.Update(TimeSpan.FromMilliseconds(16));

        act.Should().NotThrow();
    }

    [Fact]
    public void Update_FiresSceneCallbacks_WhenElapsedReachesInterval()
    {
        var callCount = 0;
        var scene = new TestScene(setup => setup.RegisterUpdate(() => callCount++, fps: 10));
        using var sut = BuildOrchestrator(scene);

        sut.Update(TimeSpan.FromMilliseconds(100));

        callCount.Should().Be(1);
    }

    [Fact]
    public void Update_DoesNotThrow_AfterConstruction()
    {
        using var sut = BuildOrchestrator();

        var act = () => sut.Update(TimeSpan.FromMilliseconds(16));

        act.Should().NotThrow();
    }

    // --------------------------------------------------
    #endregion
    #region InternalUpdate integration
    // --------------------------------------------------

    [Fact]
    public void InternalUpdate_DoesNotThrow_WithInitialSceneActive()
    {
        using var sut = BuildOrchestrator();

        var act = () => sut.SceneManager.InternalUpdate(TimeSpan.FromMilliseconds(16));

        act.Should().NotThrow();
    }

    [Fact]
    public void InternalUpdate_DoesNotThrow_AfterSchedulingNewScene()
    {
        using var sut = BuildOrchestrator();
        sut.SceneManager.ScheduleScene(() => new NullScene());

        var act = () => sut.SceneManager.InternalUpdate(TimeSpan.Zero);

        act.Should().NotThrow();
    }

    [Fact]
    public void InternalUpdate_DoesNotThrow_WithTwoScenesOnStack()
    {
        using var sut = BuildOrchestrator();
        sut.SceneManager.PushScene(() => new NullScene());

        // apply the push, then tick both scenes
        sut.SceneManager.InternalUpdate(TimeSpan.Zero);

        var act = () => sut.SceneManager.InternalUpdate(TimeSpan.FromMilliseconds(16));

        act.Should().NotThrow();
    }

    [Fact]
    public void InternalUpdate_DoesNotThrow_AfterPoppingScene()
    {
        using var sut = BuildOrchestrator();
        sut.SceneManager.PushScene(() => new NullScene());
        sut.SceneManager.InternalUpdate(TimeSpan.Zero); // apply push

        sut.SceneManager.PopScene();

        var act = () => sut.SceneManager.InternalUpdate(TimeSpan.Zero);

        act.Should().NotThrow();
    }

    // --------------------------------------------------
    #endregion
    #region Dispose
    // --------------------------------------------------

    [Fact]
    public void Dispose_DoesNotThrow_WhenGraphicsInitialized()
    {
        var sut = BuildOrchestrator();

        var act = () => sut.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_DoesNotThrow_WhenCalledTwice()
    {
        var sut = BuildOrchestrator();
        sut.Dispose();

        var act = () => sut.Dispose();

        act.Should().NotThrow();
    }

    // --------------------------------------------------
    #endregion
    #region ApplyInputSettings
    // --------------------------------------------------

    [Fact]
    public void ApplyInputSettings_ThrowsArgumentNullException_WhenBindingsIsNull()
    {
        using var sut = BuildOrchestrator();
        var act = () => sut.ApplyInputSettings(bindings: null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("bindings");
    }

    [Fact]
    public void ApplyInputSettings_UpdatesActiveBindings()
    {
        using var sut = BuildOrchestrator();
        var newBindings = new InputBindings(new Dictionary<PicoButton, IReadOnlyList<InputSource>>
        {
            [PicoButton.Left] = [new KeyboardSource(Keys.Q)],
        });

        sut.ApplyInputSettings(newBindings);

        ((InputManager)sut.InputManager).Bindings[PicoButton.Left].Should().ContainSingle()
            .Which.Should().Be(new KeyboardSource(Keys.Q));
    }

    [Fact]
    public void ApplyInputSettings_AcceptsNullBtnpConfig()
    {
        using var sut = BuildOrchestrator();
        var act = () => sut.ApplyInputSettings(InputBindings.Default, btnpConfig: null);
        act.Should().NotThrow();
    }

    // --------------------------------------------------
    #endregion
    #region ApplyAudioSettings
    // --------------------------------------------------

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void ApplyAudioSettings_ThrowsArgumentOutOfRangeException_WhenMusicVolumeOutOfRange(int volume)
    {
        using var sut = BuildOrchestrator();
        var act = () => sut.ApplyAudioSettings(musicVolume: volume, sfxVolume: 100);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("musicVolume");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void ApplyAudioSettings_ThrowsArgumentOutOfRangeException_WhenSfxVolumeOutOfRange(int volume)
    {
        using var sut = BuildOrchestrator();
        var act = () => sut.ApplyAudioSettings(musicVolume: 100, sfxVolume: volume);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("sfxVolume");
    }

    [Fact]
    public void ApplyAudioSettings_SetsMusicBaseVolume()
    {
        using var sut = BuildOrchestrator();

        sut.ApplyAudioSettings(musicVolume: 50, sfxVolume: 100);

        sut.AudioManager.MusicBaseVolume.Should().BeApproximately(0.5f, precision: 0.001f);
    }

    [Fact]
    public void ApplyAudioSettings_SetsSfxBaseVolume()
    {
        using var sut = BuildOrchestrator();

        sut.ApplyAudioSettings(musicVolume: 100, sfxVolume: 75);

        sut.AudioManager.SfxBaseVolume.Should().BeApproximately(0.75f, precision: 0.001f);
    }

    [Fact]
    public void ApplyAudioSettings_AcceptsZeroVolumes()
    {
        using var sut = BuildOrchestrator();
        var act = () => sut.ApplyAudioSettings(musicVolume: 0, sfxVolume: 0);
        act.Should().NotThrow();
    }

    [Fact]
    public void ApplyAudioSettings_AcceptsMaxVolumes()
    {
        using var sut = BuildOrchestrator();
        var act = () => sut.ApplyAudioSettings(musicVolume: 100, sfxVolume: 100);
        act.Should().NotThrow();
    }

    // --------------------------------------------------
    #endregion
    // --------------------------------------------------
}
