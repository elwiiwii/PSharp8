using FluentAssertions;
using Microsoft.Xna.Framework.Input;
using PSharp8.Input;
using Xunit;

namespace PSharp8.Tests;

public class GameOrchestratorTests
{
    private static GameOrchestrator CreateSut()
        => new(musicDirectory: ".", sfxDictionary: []);

    // --- ApplyInputSettings ---

    [Fact]
    public void ApplyInputSettings_ThrowsArgumentNullException_WhenBindingsIsNull()
    {
        var sut = CreateSut();
        var act = () => sut.ApplyInputSettings(bindings: null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("bindings");
    }

    [Fact]
    public void ApplyInputSettings_UpdatesActiveBindings()
    {
        var sut = CreateSut();
        var newBindings = new InputBindings(new Dictionary<PicoButton, IReadOnlyList<InputSource>>
        {
            [PicoButton.Left] = [new KeyboardSource(Keys.Q)],
        });

        sut.ApplyInputSettings(newBindings);

        sut.InputManager.Bindings[PicoButton.Left].Should().ContainSingle()
            .Which.Should().Be(new KeyboardSource(Keys.Q));
    }

    [Fact]
    public void ApplyInputSettings_AcceptsNullBtnpConfig()
    {
        var sut = CreateSut();
        var act = () => sut.ApplyInputSettings(InputBindings.Default, btnpConfig: null);
        act.Should().NotThrow();
    }

    // --- ApplyAudioSettings ---

    [Theory]
    [InlineData(-1)]   // below minimum
    [InlineData(101)]  // above maximum
    public void ApplyAudioSettings_ThrowsArgumentOutOfRangeException_WhenMusicVolumeOutOfRange(int volume)
    {
        var sut = CreateSut();
        var act = () => sut.ApplyAudioSettings(musicVolume: volume, sfxVolume: 100);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("musicVolume");
    }

    [Theory]
    [InlineData(-1)]   // below minimum
    [InlineData(101)]  // above maximum
    public void ApplyAudioSettings_ThrowsArgumentOutOfRangeException_WhenSfxVolumeOutOfRange(int volume)
    {
        var sut = CreateSut();
        var act = () => sut.ApplyAudioSettings(musicVolume: 100, sfxVolume: volume);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("sfxVolume");
    }

    [Fact]
    public void ApplyAudioSettings_SetsMusicBaseVolume()
    {
        var sut = CreateSut();

        sut.ApplyAudioSettings(musicVolume: 50, sfxVolume: 100);

        sut.AudioManager.MusicBaseVolume.Should().BeApproximately(0.5f, precision: 0.001f);
    }

    [Fact]
    public void ApplyAudioSettings_SetsSfxBaseVolume()
    {
        var sut = CreateSut();

        sut.ApplyAudioSettings(musicVolume: 100, sfxVolume: 75);

        sut.AudioManager.SfxBaseVolume.Should().BeApproximately(0.75f, precision: 0.001f);
    }

    [Fact]
    public void ApplyAudioSettings_AcceptsZeroVolumes()
    {
        var sut = CreateSut();
        var act = () => sut.ApplyAudioSettings(musicVolume: 0, sfxVolume: 0);
        act.Should().NotThrow();
    }

    [Fact]
    public void ApplyAudioSettings_AcceptsMaxVolumes()
    {
        var sut = CreateSut();
        var act = () => sut.ApplyAudioSettings(musicVolume: 100, sfxVolume: 100);
        act.Should().NotThrow();
    }
}
