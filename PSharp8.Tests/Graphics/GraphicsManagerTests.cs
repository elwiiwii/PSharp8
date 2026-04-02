using FluentAssertions;
using Microsoft.Xna.Framework.Graphics;
using PSharp8.Graphics;
using PSharp8.Tests.Infrastructure;
using Xunit;

namespace PSharp8.Tests.Graphics;

[Collection("Fna")]
public class GraphicsManagerTests(FnaFixture fixture) : GraphicsTestBase(fixture)
{
    // -------------------------------------------------------------------------
    #region Constructor — argument validation
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenBatchIsNull()
    {
        var act = () => new GraphicsManager(
            null!,
            () => (128, 128),
            _fixture.GraphicsDeviceManager,
            _gd,
            new PaletteManager(),
            MakeSolid(1, 1, White),
            BuildSpriteTextureManager(),
            new TextureCache(_gd, "."),
            _fixture.Window);

        act.Should().Throw<ArgumentNullException>().WithParameterName("batch");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenGetSceneResolutionIsNull()
    {
        using var spriteBatch = new SpriteBatch(_gd);
        var act = () => new GraphicsManager(
            spriteBatch,
            null!,
            _fixture.GraphicsDeviceManager,
            _gd,
            new PaletteManager(),
            MakeSolid(1, 1, White),
            BuildSpriteTextureManager(),
            new TextureCache(_gd, "."),
            _fixture.Window);

        act.Should().Throw<ArgumentNullException>().WithParameterName("getSceneResolution");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenGraphicsIsNull()
    {
        using var spriteBatch = new SpriteBatch(_gd);
        var act = () => new GraphicsManager(
            spriteBatch,
            () => (128, 128),
            null!,
            _gd,
            new PaletteManager(),
            MakeSolid(1, 1, White),
            BuildSpriteTextureManager(),
            new TextureCache(_gd, "."),
            _fixture.Window);

        act.Should().Throw<ArgumentNullException>().WithParameterName("graphics");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenGraphicsDeviceIsNull()
    {
        using var spriteBatch = new SpriteBatch(_gd);
        var act = () => new GraphicsManager(
            spriteBatch,
            () => (128, 128),
            _fixture.GraphicsDeviceManager,
            null!,
            new PaletteManager(),
            MakeSolid(1, 1, White),
            BuildSpriteTextureManager(),
            new TextureCache(_gd, "."),
            _fixture.Window);

        act.Should().Throw<ArgumentNullException>().WithParameterName("graphicsDevice");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenPaletteManagerIsNull()
    {
        using var spriteBatch = new SpriteBatch(_gd);
        var act = () => new GraphicsManager(
            spriteBatch,
            () => (128, 128),
            _fixture.GraphicsDeviceManager,
            _gd,
            null!,
            MakeSolid(1, 1, White),
            BuildSpriteTextureManager(),
            new TextureCache(_gd, "."),
            _fixture.Window);

        act.Should().Throw<ArgumentNullException>().WithParameterName("paletteManager");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenPixelIsNull()
    {
        using var spriteBatch = new SpriteBatch(_gd);
        var act = () => new GraphicsManager(
            spriteBatch,
            () => (128, 128),
            _fixture.GraphicsDeviceManager,
            _gd,
            new PaletteManager(),
            null!,
            BuildSpriteTextureManager(),
            new TextureCache(_gd, "."),
            _fixture.Window);

        act.Should().Throw<ArgumentNullException>().WithParameterName("pixel");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenSpriteTextureManagerIsNull()
    {
        using var spriteBatch = new SpriteBatch(_gd);
        var act = () => new GraphicsManager(
            spriteBatch,
            () => (128, 128),
            _fixture.GraphicsDeviceManager,
            _gd,
            new PaletteManager(),
            MakeSolid(1, 1, White),
            null!,
            new TextureCache(_gd, "."),
            _fixture.Window);

        act.Should().Throw<ArgumentNullException>().WithParameterName("spriteTextureManager");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenTextureCacheIsNull()
    {
        using var spriteBatch = new SpriteBatch(_gd);
        var act = () => new GraphicsManager(
            spriteBatch,
            () => (128, 128),
            _fixture.GraphicsDeviceManager,
            _gd,
            new PaletteManager(),
            MakeSolid(1, 1, White),
            BuildSpriteTextureManager(),
            null!,
            _fixture.Window);

        act.Should().Throw<ArgumentNullException>().WithParameterName("textureCache");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenWindowIsNull()
    {
        using var spriteBatch = new SpriteBatch(_gd);
        var act = () => new GraphicsManager(
            spriteBatch,
            () => (128, 128),
            _fixture.GraphicsDeviceManager,
            _gd,
            new PaletteManager(),
            MakeSolid(1, 1, White),
            BuildSpriteTextureManager(),
            new TextureCache(_gd, "."),
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("window");
    }

    // -------------------------------------------------------------------------
    #endregion
    #region SetSpriteTextureManager
    // -------------------------------------------------------------------------

    [Fact]
    public void SetSpriteTextureManager_ThrowsArgumentNullException_WhenNull()
    {
        using var batch = new SpriteBatch(_gd);
        var gm = new GraphicsManager(
            batch,
            () => (128, 128),
            _fixture.GraphicsDeviceManager,
            _gd,
            new PaletteManager(),
            MakeSolid(1, 1, White),
            BuildSpriteTextureManager(),
            new TextureCache(_gd, "."),
            _fixture.Window);

        var act = () => gm.SetSpriteTextureManager(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("spriteTextureManager");
    }

    [Fact]
    public void SetSpriteTextureManager_DoesNotThrow_WhenValidInstance()
    {
        using var batch = new SpriteBatch(_gd);
        var gm = new GraphicsManager(
            batch,
            () => (128, 128),
            _fixture.GraphicsDeviceManager,
            _gd,
            new PaletteManager(),
            MakeSolid(1, 1, White),
            BuildSpriteTextureManager(),
            new TextureCache(_gd, "."),
            _fixture.Window);

        var act = () => gm.SetSpriteTextureManager(BuildSpriteTextureManager());

        act.Should().NotThrow();
    }

    // -------------------------------------------------------------------------
    #endregion
}