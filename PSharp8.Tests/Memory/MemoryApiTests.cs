using FluentAssertions;
using PSharp8.Audio;
using PSharp8.Input;
using PSharp8.Scene;
using PSharp8.Tests.Infrastructure;
using Xunit;

namespace PSharp8.Tests.Memory;

[Collection("Fna")]
public class MemoryApiTests(FnaFixture fixture) : GraphicsTestBase(fixture)
{
    // -------------------------------------------------------------------------
    #region Helpers
    // -------------------------------------------------------------------------

    private GameOrchestrator BuildOrchestrator()
        => new(
            ".",
            ".",
            ".",
            new NullScene(),
            _gd,
            _fixture.GraphicsDeviceManager,
            _fixture.Window);

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

    // -------------------------------------------------------------------------
    #endregion
    // -------------------------------------------------------------------------
    #region Reload
    // -------------------------------------------------------------------------

    [Fact]
    public void Reload_RestoresMapTile_ViaPico8Api()
    {
        using var orch = BuildOrchestrator();
        Pico8.Initialize(orch);

        Pico8.Mset(0, 0, 1);
        Pico8.Reload();

        Pico8.Mget(0, 0).Should().Be(0);
    }

    [Fact]
    public void Reload_RestoresFlags_ViaPico8Api()
    {
        using var orch = BuildOrchestrator();
        Pico8.Initialize(orch);

        orch.SmManager.SetFlag(0, 0xFF);
        Pico8.Reload();

        Pico8.Fget(0).Should().Be(0);
    }

    [Fact]
    public void Reload_IncrementsVersions_ViaPico8Api()
    {
        using var orch = BuildOrchestrator();
        Pico8.Initialize(orch);
        int sprV = orch.SmManager.SpritesheetVersion;
        int mapV = orch.SmManager.MapVersion;

        Pico8.Reload();

        orch.SmManager.SpritesheetVersion.Should().Be(sprV + 1);
        orch.SmManager.MapVersion.Should().Be(mapV + 1);
    }

    // -------------------------------------------------------------------------
    #endregion
    // -------------------------------------------------------------------------
    #region MapToSpritesheet1D
    // -------------------------------------------------------------------------

    [Fact]
    public void MapToSpritesheet1D_IncrementsSpritesheetVersion_ViaPico8Api()
    {
        using var orch = BuildOrchestrator();
        Pico8.Initialize(orch);
        int vBefore = orch.SmManager.SpritesheetVersion;

        Pico8.MapToSpritesheet1D(cellX: 0, cellY: 0, destX: 0, destY: 0, length: 2);

        orch.SmManager.SpritesheetVersion.Should().BeGreaterThan(vBefore);
    }

    [Fact]
    public void MapToSpritesheet1D_IsNoOp_WhenLengthIsZero_ViaPico8Api()
    {
        using var orch = BuildOrchestrator();
        Pico8.Initialize(orch);
        int vBefore = orch.SmManager.SpritesheetVersion;

        Pico8.MapToSpritesheet1D(length: 0);

        orch.SmManager.SpritesheetVersion.Should().Be(vBefore);
    }

    // -------------------------------------------------------------------------
    #endregion
    // -------------------------------------------------------------------------
    #region MapToSpritesheet2D
    // -------------------------------------------------------------------------

    [Fact]
    public void MapToSpritesheet2D_IncrementsSpritesheetVersion_ViaPico8Api()
    {
        using var orch = BuildOrchestrator();
        Pico8.Initialize(orch);
        int vBefore = orch.SmManager.SpritesheetVersion;

        Pico8.MapToSpritesheet2D(cellX: 0, cellY: 0, cellW: 1, cellH: 1, destX: 0, destY: 0, destW: 2, destH: 1);

        orch.SmManager.SpritesheetVersion.Should().BeGreaterThan(vBefore);
    }

    [Fact]
    public void MapToSpritesheet2D_IsNoOp_WhenCellWidthIsZero_ViaPico8Api()
    {
        using var orch = BuildOrchestrator();
        Pico8.Initialize(orch);
        int vBefore = orch.SmManager.SpritesheetVersion;

        Pico8.MapToSpritesheet2D(cellW: 0);

        orch.SmManager.SpritesheetVersion.Should().Be(vBefore);
    }

    // -------------------------------------------------------------------------
    #endregion
}
