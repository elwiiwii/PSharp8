using FluentAssertions;
using Microsoft.Xna.Framework.Audio;
using PSharp8.Audio;
using PSharp8.Tests.Infrastructure;
using Xunit;

namespace PSharp8.Tests.Audio;

public class AudioManagerSfxTests
{
    private static AudioManager CreateSut(Dictionary<string, SoundEffect>? sfxDictionary = null)
    {
        return new AudioManager(
            new Dictionary<string, SoundEffect>(),
            sfxDictionary ?? new Dictionary<string, SoundEffect>());
    }

    // -------------------------------------------------------------------------
    #region Constructor & SFX Dictionary Guards
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenSfxDictionaryIsNull()
    {
        var act = () => new AudioManager(
            new Dictionary<string, SoundEffect>(),
            sfxDictionary: null!);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("sfxDictionary");
    }

    // -------------------------------------------------------------------------
    #endregion
    #region SetSfxPacks / SetActiveSfxPack
    // -------------------------------------------------------------------------

    [Fact]
    public void SetSfxPacks_ThrowsArgumentNullException_WhenNull()
    {
        var sut = CreateSut();

        var act = () => sut.SetSfxPacks(null!);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("sfxPacks");
    }

    [Fact]
    public void SetActiveSfxPack_ThrowsArgumentNullException_WhenNull()
    {
        var sut = CreateSut();

        var act = () => sut.SetActiveSfxPack(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SetActiveSfxPack_ThrowsInvalidOperationException_WhenNoPacksSet()
    {
        var sut = CreateSut();

        var act = () => sut.SetActiveSfxPack("original");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SetActiveSfxPack_ThrowsKeyNotFoundException_WhenPackNotFound()
    {
        var sut = CreateSut();
        sut.SetSfxPacks([new SfxPack("original", "pcraft_og_")]);

        var act = () => sut.SetActiveSfxPack("nonexistent");

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void SetActiveSfxPack_SelectsPack_WhenNameMatches()
    {
        var sut = CreateSut();
        sut.SetSfxPacks([
            new SfxPack("original", "pcraft_og_"),
            new SfxPack("soft", "pcraft_soft_")
        ]);

        var act = () => sut.SetActiveSfxPack("soft");

        act.Should().NotThrow();
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Sfx() Edge Cases & Guards
    // -------------------------------------------------------------------------

    [Fact]
    public void Sfx_ThrowsInvalidOperationException_WhenNoActiveSfxPack()
    {
        var sut = CreateSut();

        var act = () => sut.Sfx(11);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Sfx_ThrowsKeyNotFoundException_WhenSfxNotInDictionary()
    {
        var sut = CreateSut();
        sut.SetSfxPacks([new SfxPack("original", "pcraft_og_")]);
        sut.SetActiveSfxPack("original");

        // "pcraft_og_99" does not exist in the empty sfxDictionary
        var act = () => sut.Sfx(99);

        act.Should().Throw<KeyNotFoundException>();
    }

    // -------------------------------------------------------------------------
    #endregion
}

[Collection("Fna")]
public class AudioManagerSfxPlaybackTests(FnaFixture fixture) : GraphicsTestBase(fixture)
{
    private AudioManager CreateSut(Dictionary<string, SoundEffect> sfxDictionary)
    {
        return new AudioManager(
            new Dictionary<string, SoundEffect>(),
            sfxDictionary);
    }

    // -------------------------------------------------------------------------
    #region Sfx() Playback
    // -------------------------------------------------------------------------

    [Fact]
    public void Sfx_PlaysSoundEffect_ForValidNumber()
    {
        var sfx = FnaFixture.CreateSilentSoundEffect();
        _ownedDisposables.Add(sfx);
        var sfxDict = new Dictionary<string, SoundEffect> { { "pcraft_og_11", sfx } };
        var sut = CreateSut(sfxDict);
        sut.SetSfxPacks([new SfxPack("original", "pcraft_og_")]);
        sut.SetActiveSfxPack("original");

        sut.Sfx(11);

        sut._sfxInstances.Should().ContainSingle()
            .Which.State.Should().Be(SoundState.Playing);
    }

    [Fact]
    public void Sfx_KeepsOldInstances_WhenNewSfxPlayed()
    {
        var sfx11 = FnaFixture.CreateSilentSoundEffect();
        var sfx12 = FnaFixture.CreateSilentSoundEffect();
        _ownedDisposables.Add(sfx11);
        _ownedDisposables.Add(sfx12);
        var sfxDict = new Dictionary<string, SoundEffect>
        {
            { "pcraft_og_11", sfx11 },
            { "pcraft_og_12", sfx12 }
        };
        var sut = CreateSut(sfxDict);
        sut.SetSfxPacks([new SfxPack("original", "pcraft_og_")]);
        sut.SetActiveSfxPack("original");

        sut.Sfx(11);
        sut.Sfx(12);

        sut._sfxInstances.Should().HaveCount(2);
        sut._sfxInstances.Should().OnlyContain(i => i.State == SoundState.Playing);
    }

    [Fact]
    public void Sfx_PlaysFromCorrectPack_AfterSwitchingPack()
    {
        var sfxOg = FnaFixture.CreateSilentSoundEffect();
        var sfxSoft = FnaFixture.CreateSilentSoundEffect();
        _ownedDisposables.Add(sfxOg);
        _ownedDisposables.Add(sfxSoft);
        var sfxDict = new Dictionary<string, SoundEffect>
        {
            { "pcraft_og_11", sfxOg },
            { "pcraft_soft_11", sfxSoft }
        };
        var sut = CreateSut(sfxDict);
        sut.SetSfxPacks([
            new SfxPack("original", "pcraft_og_"),
            new SfxPack("soft", "pcraft_soft_")
        ]);

        sut.SetActiveSfxPack("original");
        sut.Sfx(11);

        sut.SetActiveSfxPack("soft");
        sut.Sfx(11);

        // Both instances should still be tracked and playing
        sut._sfxInstances.Should().HaveCount(2);
        sut._sfxInstances.Should().OnlyContain(i => i.State == SoundState.Playing);
    }

    [Fact]
    public void Sfx_PlaysDifferentSounds_ForDifferentNumbers()
    {
        var sfx11 = FnaFixture.CreateSilentSoundEffect();
        var sfx12 = FnaFixture.CreateSilentSoundEffect();
        _ownedDisposables.Add(sfx11);
        _ownedDisposables.Add(sfx12);
        var sfxDict = new Dictionary<string, SoundEffect>
        {
            { "pcraft_og_11", sfx11 },
            { "pcraft_og_12", sfx12 }
        };
        var sut = CreateSut(sfxDict);
        sut.SetSfxPacks([new SfxPack("original", "pcraft_og_")]);
        sut.SetActiveSfxPack("original");

        sut.Sfx(11);
        sut.Sfx(12);

        // Both SFX should be tracked concurrently
        sut._sfxInstances.Should().HaveCount(2);
    }

    [Fact]
    public void CleanUpFinishedSfx_DisposesStoppedInstances()
    {
        var sfx = FnaFixture.CreateSilentSoundEffect();
        _ownedDisposables.Add(sfx);
        var sfxDict = new Dictionary<string, SoundEffect> { { "pcraft_og_11", sfx } };
        var sut = CreateSut(sfxDict);
        sut.SetSfxPacks([new SfxPack("original", "pcraft_og_")]);
        sut.SetActiveSfxPack("original");

        sut.Sfx(11);
        var instance = sut._sfxInstances[0];
        instance.Stop();

        sut.CleanUpFinishedSfx();

        instance.IsDisposed.Should().BeTrue();
        sut._sfxInstances.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------
    #endregion
}
