using FluentAssertions;
using Microsoft.Xna.Framework.Graphics;
using PSharp8.Graphics;
using PSharp8.Tests.Infrastructure;
using Xunit;

namespace PSharp8.Tests.Graphics;

[Collection("Fna")]
public class TextureCacheTests(FnaFixture fixture) : GraphicsTestBase(fixture)
{
    // -------------------------------------------------------------------------
    #region Constructor — argument validation
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenGraphicsDeviceIsNull()
    {
        var act = () => new TextureCache(null!, ".");
        act.Should().Throw<ArgumentNullException>().WithParameterName("graphicsDevice");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenTexturesDirectoryIsNull()
    {
        var act = () => new TextureCache(_gd, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("texturesDirectory");
    }

    [Fact]
    public void Constructor_ThrowsArgumentOutOfRange_WhenStaleTtlFramesIsNegative()
    {
        var act = () => new TextureCache(_gd, ".", staleTtlFrames: -1);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("staleTtlFrames");
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Get
    // -------------------------------------------------------------------------

    [Fact]
    public void Get_ThrowsFileNotFoundException_WhenTextureNotFound()
    {
        using var cache = new TextureCache(_gd, Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

        var act = () => cache.Get("missing");

        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void Get_LoadsTextureFromDisk_OnFirstAccess()
    {
        var tempDir = CreateTempTextureDir("test_tex");
        using var cache = new TextureCache(_gd, tempDir);

        var texture = cache.Get("test_tex");

        texture.Should().NotBeNull();
    }

    [Fact]
    public void Get_ReturnsCachedInstance_OnSubsequentAccess()
    {
        var tempDir = CreateTempTextureDir("tex");
        using var cache = new TextureCache(_gd, tempDir);

        var first = cache.Get("tex");
        var second = cache.Get("tex");

        second.Should().BeSameAs(first);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Tick
    // -------------------------------------------------------------------------

    [Fact]
    public void Tick_EvictsStaleEntry_AfterTtlExceeded()
    {
        var tempDir = CreateTempTextureDir("tex");
        using var cache = new TextureCache(_gd, tempDir, staleTtlFrames: 2);

        var first = cache.Get("tex"); // loads and caches at frame 0
        cache.Tick(); // frame 1, not evicted
        cache.Tick(); // frame 2, not evicted
        cache.Tick(); // frame 3, evicted (3-0 > 2)

        var second = cache.Get("tex"); // reloads from disk

        second.Should().NotBeSameAs(first); // fresh instance — proves eviction occurred
        first.IsDisposed.Should().BeTrue();  // evicted entry was disposed
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Dispose
    // -------------------------------------------------------------------------

    [Fact]
    public void Dispose_DisposesAllCachedTextures()
    {
        var tempDir = CreateTempTextureDir("tex");
        var cache = new TextureCache(_gd, tempDir);

        var tex = cache.Get("tex");
        cache.Dispose();

        tex.IsDisposed.Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Helpers
    // -------------------------------------------------------------------------

    private string CreateTempTextureDir(string textureName)
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        using var tex = new Texture2D(_gd, 4, 4);
        using var stream = File.OpenWrite(Path.Combine(dir, textureName + ".png"));
        tex.SaveAsPng(stream, tex.Width, tex.Height);
        return dir;
    }

    // -------------------------------------------------------------------------
    #endregion
}
