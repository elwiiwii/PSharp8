using System.Text.Json;
using FluentAssertions;
using PSharp8.Settings;
using Xunit;

namespace PSharp8.Tests.Settings;

public sealed class HotReloadableSettingsTests : IDisposable
{
    private readonly string _tempDir;

    public HotReloadableSettingsTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"PSharp8.Tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private string TempFile(string name = "settings.json") => Path.Combine(_tempDir, name);

    private sealed class TestSettings
    {
        public int Value { get; set; } = 10;
        public string Label { get; set; } = "default";
    }

    // --------------------------------------------------------------------------
    #region Constructor
    // --------------------------------------------------------------------------

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenFilePathIsNull()
    {
        var act = () => new HotReloadableSettings<TestSettings>(filePath: null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("filePath");
    }

    [Fact]
    public void Constructor_CreatesFile_WhenFileDoesNotExist()
    {
        var path = TempFile();

        using var sut = new HotReloadableSettings<TestSettings>(path);

        File.Exists(path).Should().BeTrue();
    }

    [Fact]
    public void Constructor_WritesDefaultsToFile_WhenFileDoesNotExist()
    {
        var path = TempFile();

        using var _ = new HotReloadableSettings<TestSettings>(path);

        var json = File.ReadAllText(path);
        var loaded = JsonSerializer.Deserialize<TestSettings>(json);
        loaded.Should().NotBeNull();
        loaded!.Value.Should().Be(10);
        loaded.Label.Should().Be("default");
    }

    [Fact]
    public void Constructor_UsesSuppliedDefaultValue_WhenFileDoesNotExist()
    {
        var path = TempFile();
        var custom = new TestSettings { Value = 99, Label = "custom" };

        using var sut = new HotReloadableSettings<TestSettings>(path, defaultValue: custom);

        sut.Current.Value.Should().Be(99);
        sut.Current.Label.Should().Be("custom");
    }

    [Fact]
    public void Constructor_LoadsExistingValue_WhenFileAlreadyExists()
    {
        var path = TempFile();
        File.WriteAllText(path, """{"Value":42,"Label":"loaded"}""");

        using var sut = new HotReloadableSettings<TestSettings>(path);

        sut.Current.Value.Should().Be(42);
        sut.Current.Label.Should().Be("loaded");
    }

    [Fact]
    public void Constructor_DoesNotOverwriteExistingFile_WhenFileAlreadyExists()
    {
        var path = TempFile();
        var original = """{"Value":7,"Label":"original"}""";
        File.WriteAllText(path, original);

        using var _ = new HotReloadableSettings<TestSettings>(path);

        File.ReadAllText(path).Should().Be(original);
    }

    // --------------------------------------------------------------------------
    #endregion
    #region Current
    // --------------------------------------------------------------------------

    [Fact]
    public void Current_ReflectsValueFromFile_AfterConstruction()
    {
        var path = TempFile();
        File.WriteAllText(path, """{"Value":55,"Label":"hello"}""");

        using var sut = new HotReloadableSettings<TestSettings>(path);

        sut.Current.Value.Should().Be(55);
    }

    // ------------------------------------------------------------------------
    #endregion
    #region FlushPending
    // --------------------------------------------------------------------------

    [Fact]
    public void FlushPending_DoesNothing_WhenFileHasNotChanged()
    {
        var path = TempFile();
        using var sut = new HotReloadableSettings<TestSettings>(path);
        var originalCurrent = sut.Current;
        var changedFired = false;
        sut.Changed += _ => changedFired = true;

        sut.FlushPending();

        changedFired.Should().BeFalse();
        sut.Current.Should().BeSameAs(originalCurrent);
    }

    [Fact]
    public async Task FlushPending_UpdatesCurrent_AfterFileIsModified()
    {
        var path = TempFile();
        using var sut = new HotReloadableSettings<TestSettings>(path);

        File.WriteAllText(path, """{"Value":77,"Label":"updated"}""");
        await Task.Delay(500); // wait > 300ms debounce

        sut.FlushPending();

        sut.Current.Value.Should().Be(77);
        sut.Current.Label.Should().Be("updated");
    }

    [Fact]
    public async Task FlushPending_FiresChangedEvent_AfterFileIsModified()
    {
        var path = TempFile();
        using var sut = new HotReloadableSettings<TestSettings>(path);

        TestSettings? received = null;
        sut.Changed += v => received = v;

        File.WriteAllText(path, """{"Value":88,"Label":"fired"}""");
        await Task.Delay(500); // wait > 300ms debounce

        sut.FlushPending();

        received.Should().NotBeNull();
        received!.Value.Should().Be(88);
    }

    [Fact]
    public async Task FlushPending_DoesNotFire_BeforeDebounceElapsed()
    {
        var path = TempFile();
        using var sut = new HotReloadableSettings<TestSettings>(path);
        var changed = false;
        sut.Changed += _ => changed = true;

        File.WriteAllText(path, """{"Value":99,"Label":"too-soon"}""");
        await Task.Delay(50); // well within debounce window

        sut.FlushPending();

        changed.Should().BeFalse();
    }

    // --------------------------------------------------------------------------
    #endregion
    #region Dispose
    // --------------------------------------------------------------------------

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var path = TempFile();
        var sut = new HotReloadableSettings<TestSettings>(path);

        var act = () => sut.Dispose();

        act.Should().NotThrow();
    }

    // --------------------------------------------------------------------------
    #endregion
    // --------------------------------------------------------------------------
}
