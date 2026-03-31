using FluentAssertions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using PSharp8.Audio;
using PSharp8.Tests.Infrastructure;
using Xunit;

namespace PSharp8.Tests.Audio;

[Collection("Fna")]
public class AudioManagerVolumeTests(FnaFixture fixture)
{
    private readonly FnaFixture _fixture = fixture;

    private AudioManager CreateManager(params string[] filenames)
    {
        var dict = new Dictionary<string, SoundEffect>();
        foreach (var name in filenames)
            dict[name] = FnaFixture.CreateSilentSoundEffect();
        return new AudioManager(dict, new Dictionary<string, SoundEffect>());
    }

    private static Soundtrack SingleTrackSoundtrack(string filename, bool loop, int channel = 0)
    {
        return new Soundtrack("test", [new Track([new TrackPart(filename, loop)], channel)]);
    }

    private static GameTime Elapsed(double ms) =>
        new(TimeSpan.Zero, TimeSpan.FromMilliseconds(ms));

    // -------------------------------------------------------------------------
    #region FadeVolume / RestoreVolume
    // -------------------------------------------------------------------------

    [Fact]
    public void FadeVolume_ReducesVolumeToTarget_OverSpecifiedMs()
    {
        var sut = CreateManager("song1");
        sut.SetSoundtracks([SingleTrackSoundtrack("song1", loop: true)]);
        sut.SetActiveSoundtrack("test");
        sut.Music(0, 0);
        sut.CurrentVolume.Should().Be(1f, "precondition: playing at full volume");

        sut.FadeVolume(0.5f, fadeMs: 1000);
        sut.Update(Elapsed(1000));

        sut.CurrentVolume.Should().BeApproximately(0.5f, 0.01f,
            "volume should reach target after fade duration elapses");
        sut.IsFading.Should().BeFalse("fade should be complete");
    }

    [Fact]
    public void FadeVolume_SetsVolumeImmediately_WhenFadeMsIsZero()
    {
        var sut = CreateManager("song1");
        sut.SetSoundtracks([SingleTrackSoundtrack("song1", loop: true)]);
        sut.SetActiveSoundtrack("test");
        sut.Music(0, 0);

        sut.FadeVolume(0.3f, fadeMs: 0);

        sut.CurrentVolume.Should().BeApproximately(0.3f, 0.01f,
            "volume should change immediately when fadeMs is 0");
        sut.IsFading.Should().BeFalse("no fade needed for immediate change");
    }

    [Theory]
    [InlineData(-0.5f, 0f)]  // negative clamped to 0
    [InlineData(1.5f, 1f)]   // above 1 clamped to 1
    [InlineData(0f, 0f)]     // exactly 0 is valid
    [InlineData(1f, 1f)]     // exactly 1 is valid
    public void FadeVolume_ClampsTarget_WhenOutsideZeroOneRange(float input, float expected)
    {
        var sut = CreateManager("song1");
        sut.SetSoundtracks([SingleTrackSoundtrack("song1", loop: true)]);
        sut.SetActiveSoundtrack("test");
        sut.Music(0, 0);

        sut.FadeVolume(input, fadeMs: 0);

        sut.CurrentVolume.Should().BeApproximately(expected, 0.01f);
    }

    [Fact]
    public void FadeVolume_DoesNotStopPlayback_WhenTargetIsZero()
    {
        var sut = CreateManager("song1");
        sut.SetSoundtracks([SingleTrackSoundtrack("song1", loop: true)]);
        sut.SetActiveSoundtrack("test");
        sut.Music(0, 0);

        sut.FadeVolume(0f, fadeMs: 500);
        sut.Update(Elapsed(500));

        sut.IsPlaying.Should().BeTrue(
            "FadeVolume to 0% should not stop playback, unlike Music(-1, ...)");
        sut.CurrentVolume.Should().Be(0f);
    }

    [Fact]
    public void Update_ProgressesFadeVolume_OverElapsedMs()
    {
        var sut = CreateManager("song1");
        sut.SetSoundtracks([SingleTrackSoundtrack("song1", loop: true)]);
        sut.SetActiveSoundtrack("test");
        sut.Music(0, 0);

        sut.FadeVolume(0f, fadeMs: 1000); // fade from 1.0 to 0.0

        sut.Update(Elapsed(500)); // halfway

        sut.CurrentVolume.Should().BeApproximately(0.5f, 0.01f,
            "volume should be ~50% after half the fade duration (lerping from 1.0 to 0.0)");
        sut.IsFading.Should().BeTrue("fade should still be in progress");
    }

    [Fact]
    public void RestoreVolume_ReturnsToFullVolume_OverSpecifiedMs()
    {
        var sut = CreateManager("song1");
        sut.SetSoundtracks([SingleTrackSoundtrack("song1", loop: true)]);
        sut.SetActiveSoundtrack("test");
        sut.Music(0, 0);

        sut.FadeVolume(0.3f, fadeMs: 0); // immediately reduce
        sut.CurrentVolume.Should().BeApproximately(0.3f, 0.01f, "precondition");

        sut.RestoreVolume(fadeMs: 500);
        sut.Update(Elapsed(500));

        sut.CurrentVolume.Should().Be(1f,
            "volume should return to full after RestoreVolume completes");
        sut.IsFading.Should().BeFalse("fade should be complete");
    }

    [Fact]
    public void RestoreVolume_HasNoEffect_WhenAlreadyAtFullVolume()
    {
        var sut = CreateManager("song1");
        sut.SetSoundtracks([SingleTrackSoundtrack("song1", loop: true)]);
        sut.SetActiveSoundtrack("test");
        sut.Music(0, 0); // playing at full volume

        sut.RestoreVolume(fadeMs: 500);

        sut.CurrentVolume.Should().Be(1f, "volume unchanged — already at full");
        sut.IsFading.Should().BeFalse("no fade needed when already at full volume");
    }

    // -------------------------------------------------------------------------
    #endregion
}
