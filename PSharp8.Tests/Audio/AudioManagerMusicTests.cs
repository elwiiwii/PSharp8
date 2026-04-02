using FluentAssertions;
using PSharp8.Audio;
using PSharp8.Tests.Infrastructure;
using Xunit;

namespace PSharp8.Tests.Audio;

[Collection("Fna")]
public class AudioManagerMusicTests(FnaFixture fixture) : IDisposable
{
    private readonly FnaFixture _fixture = fixture;
    private TempMusicDirectory? _tempDir;

    public void Dispose() => _tempDir?.Dispose();

    private AudioManager CreateManager(params string[] filenames)
    {
        var oggNames = filenames.Select(f => f + ".ogg").ToArray();
        _tempDir = FnaFixture.CreateTempMusicDirectory(oggNames);
        return new AudioManager(_tempDir.Path);
    }

    private static Soundtrack SingleTrackSoundtrack(string filename, bool loop, int channel = 0)
    {
        return new Soundtrack("test", [new Track([new TrackPart(filename + ".ogg", loop)], channel)]);
    }

    private static Soundtrack TwoTrackSoundtrack(string file1, int ch1, string file2, int ch2)
    {
        return new Soundtrack("test", [
            new Track([new TrackPart(file1 + ".ogg", true)], ch1),
            new Track([new TrackPart(file2 + ".ogg", true)], ch2)
        ]);
    }

    // -------------------------------------------------------------------------
    #region Fade In From Nothing
    // -------------------------------------------------------------------------

    [Fact]
    public void Music_SetsIsPlayingTrue_WhenCalledWithValidIndexAndNoCurrentMusic()
    {
        var sut = CreateManager("song1");
        sut.SetSoundtracks([SingleTrackSoundtrack("song1", loop: true)]);
        sut.SetActiveSoundtrack("test");

        sut.Music(0, 0);

        sut.IsPlaying.Should().BeTrue();
    }

    [Fact]
    public void Music_SetsCurrentTrackIndex_WhenCalledWithValidIndex()
    {
        var sut = CreateManager("song1");
        sut.SetSoundtracks([SingleTrackSoundtrack("song1", loop: true)]);
        sut.SetActiveSoundtrack("test");

        sut.Music(0, 0);

        sut.CurrentTrackIndex.Should().Be(0);
    }

    [Fact]
    public void Music_PlaysAtFullVolume_WhenFadeMsIsZero()
    {
        var sut = CreateManager("song1");
        sut.SetSoundtracks([SingleTrackSoundtrack("song1", loop: true)]);
        sut.SetActiveSoundtrack("test");

        sut.Music(0, 0);

        sut.CurrentVolume.Should().Be(1f);
    }

    [Fact]
    public void Music_StartsAtZeroVolume_WhenFadeMsGreaterThanZero()
    {
        var sut = CreateManager("song1");
        sut.SetSoundtracks([SingleTrackSoundtrack("song1", loop: true)]);
        sut.SetActiveSoundtrack("test");

        sut.Music(0, fadeMs: 500);

        sut.IsPlaying.Should().BeTrue();
        sut.CurrentVolume.Should().Be(0f);
    }

    [Fact]
    public void Music_IsFadingReturnsTrue_WhenFadeInProgress()
    {
        var sut = CreateManager("song1");
        sut.SetSoundtracks([SingleTrackSoundtrack("song1", loop: true)]);
        sut.SetActiveSoundtrack("test");

        sut.Music(0, fadeMs: 500);

        sut.IsFading.Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Fade Out To Nothing
    // -------------------------------------------------------------------------

    [Fact]
    public void Music_BeginsFadeOut_WhenCalledWithNegativeOne()
    {
        var sut = CreateManager("song1");
        sut.SetSoundtracks([SingleTrackSoundtrack("song1", loop: true)]);
        sut.SetActiveSoundtrack("test");
        sut.Music(0, 0);

        sut.Music(-1, fadeMs: 500);

        sut.IsFading.Should().BeTrue();
        sut.IsPlaying.Should().BeTrue("still playing while fading out");
    }

    [Fact]
    public void Music_StopsImmediately_WhenCalledWithNegativeOneAndZeroFadeMs()
    {
        var sut = CreateManager("song1");
        sut.SetSoundtracks([SingleTrackSoundtrack("song1", loop: true)]);
        sut.SetActiveSoundtrack("test");
        sut.Music(0, 0);
        sut.IsPlaying.Should().BeTrue("precondition: music is playing");

        sut.Music(-1, fadeMs: 0);

        sut.IsPlaying.Should().BeFalse();
        sut.CurrentTrackIndex.Should().BeNull();
        sut.CurrentVolume.Should().Be(0f);
        sut.IsFading.Should().BeFalse();
    }

    [Fact]
    public void Music_DisposesInstance_WhenStoppedImmediately()
    {
        var sut = CreateManager("song1");
        sut.SetSoundtracks([SingleTrackSoundtrack("song1", loop: true)]);
        sut.SetActiveSoundtrack("test");
        sut.Music(0, 0);

        sut.Music(-1, fadeMs: 0);

        // After immediate stop, creating a new instance from the same SoundEffect
        // should succeed (proves the old one was cleaned up, not leaked)
        var act = () => sut.Music(0, 0);
        act.Should().NotThrow();
        sut.IsPlaying.Should().BeTrue();
    }

    [Fact]
    public void Music_IsPlayingReturnsFalse_AfterFadeOutCompletes()
    {
        var sut = CreateManager("song1");
        sut.SetSoundtracks([SingleTrackSoundtrack("song1", loop: true)]);
        sut.SetActiveSoundtrack("test");
        sut.Music(0, 0);

        // Begin fade out, then simulate completion
        sut.Music(-1, fadeMs: 100);
        sut.IsFading.Should().BeTrue("precondition: fade-out in progress");

        sut.CompleteFade();

        sut.IsPlaying.Should().BeFalse();
        sut.CurrentTrackIndex.Should().BeNull();
        sut.IsFading.Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Crossfade Different Channel
    // -------------------------------------------------------------------------

    [Fact]
    public void Music_StartsCrossfade_WhenNewTrackOnDifferentChannel()
    {
        var sut = CreateManager("song1", "song2");
        sut.SetSoundtracks([TwoTrackSoundtrack("song1", 0, "song2", 1)]);
        sut.SetActiveSoundtrack("test");

        sut.Music(0, 0);
        var oldInstance = sut.CurrentInstance!;

        sut.Music(1, 500); // crossfade to track 1 on different channel

        // Stop all — both old and new tracks should be fully cleaned up
        sut.Music(-1, 0);

        oldInstance.IsDisposed.Should().BeTrue(
            "stopping all music during crossfade should dispose the old track instance");
    }

    [Fact]
    public void Music_FadesOutOldTrack_DuringCrossfade()
    {
        var sut = CreateManager("song1", "song2");
        sut.SetSoundtracks([TwoTrackSoundtrack("song1", 0, "song2", 1)]);
        sut.SetActiveSoundtrack("test");

        sut.Music(0, 0);
        var oldInstance = sut.CurrentInstance!;
        oldInstance.Volume.Should().Be(1.0f, "precondition: old track at full volume");

        sut.Music(1, 500); // crossfade to track 1

        oldInstance.Volume.Should().BeLessThan(1.0f,
            "old track should begin fading out when crossfade starts");
    }

    [Fact]
    public void Music_FadesInNewTrack_DuringCrossfade()
    {
        var sut = CreateManager("song1", "song2");
        sut.SetSoundtracks([TwoTrackSoundtrack("song1", 0, "song2", 1)]);
        sut.SetActiveSoundtrack("test");

        sut.Music(0, 0);
        var oldInstance = sut.CurrentInstance!;

        sut.Music(1, 500); // crossfade to track 1
        var newInstance = sut.CurrentInstance!;

        // New track starts at zero volume during crossfade
        newInstance.Volume.Should().Be(0f, "new track starts at zero during crossfade");

        // Old track should NOT be disposed yet (still fading out)
        oldInstance.IsDisposed.Should().BeFalse("old track should still exist during crossfade");

        // But the old track's volume should be reducing (beginning of fade-out)
        oldInstance.Volume.Should().BeLessThan(1.0f,
            "old track should begin fading out during crossfade");
    }

    [Fact]
    public void Music_DisposesOldTrack_WhenCrossfadeCompletes()
    {
        var sut = CreateManager("song1", "song2");
        sut.SetSoundtracks([TwoTrackSoundtrack("song1", 0, "song2", 1)]);
        sut.SetActiveSoundtrack("test");

        sut.Music(0, 0);
        var oldInstance = sut.CurrentInstance!;

        sut.Music(1, 500); // crossfade to track 1
        sut.CompleteFade();

        oldInstance.IsDisposed.Should().BeTrue(
            "old track instance should be disposed when crossfade completes");
    }

    [Fact]
    public void Music_NewTrackStartsFromBeginning_WhenDifferentChannel()
    {
        var sut = CreateManager("song1", "song2");
        sut.SetSoundtracks([TwoTrackSoundtrack("song1", 0, "song2", 1)]);
        sut.SetActiveSoundtrack("test");

        sut.Music(0, 0);
        var oldInstance = sut.CurrentInstance!;

        // Immediate switch (fadeMs = 0) to a track on a different channel
        sut.Music(1, 0);

        // Old track should be cleanly replaced (disposed, not leaked)
        oldInstance.IsDisposed.Should().BeTrue(
            "old track should be disposed when immediately switching to different channel");

        // New track should be playing
        sut.IsPlaying.Should().BeTrue();
        sut.CurrentTrackIndex.Should().Be(1);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Fade Reversal
    // -------------------------------------------------------------------------

    [Fact]
    public void Music_ReversesFadeOut_WhenSameTrackReplayedDuringFadeOut()
    {
        var sut = CreateManager("song1");
        sut.SetSoundtracks([SingleTrackSoundtrack("song1", loop: true)]);
        sut.SetActiveSoundtrack("test");
        sut.Music(0, 0); // playing at full volume
        var instance = sut.CurrentInstance!;

        sut.Music(-1, fadeMs: 1000); // begin fade out
        sut.Update(TimeSpan.FromMilliseconds(400)); // 40% through fade-out → volume ~0.6

        sut.Music(0, fadeMs: 1000); // replay same track — should reverse

        // The original instance should be kept (not replaced)
        sut.CurrentInstance.Should().BeSameAs(instance,
            "reversing a fade-out should keep the same instance, not create a new one");
        sut.IsFading.Should().BeTrue("should now be fading back in");
    }

    [Fact]
    public void Music_ReversesFadeIn_WhenFadeOutTriggeredDuringFadeIn()
    {
        var sut = CreateManager("song1");
        sut.SetSoundtracks([SingleTrackSoundtrack("song1", loop: true)]);
        sut.SetActiveSoundtrack("test");

        sut.Music(0, fadeMs: 1000); // fade in from nothing
        sut.Update(TimeSpan.FromMilliseconds(600)); // 60% through fade-in → volume ~0.6
        var volumeBeforeReversal = sut.CurrentVolume;

        sut.Music(-1, fadeMs: 1000); // trigger fade-out — should reverse

        sut.IsFading.Should().BeTrue("should now be fading out");
        // Volume should start from where it was, not jump
        sut.CurrentVolume.Should().BeApproximately(volumeBeforeReversal, 0.01f,
            "volume should not jump when reversing a fade-in");
    }

    [Fact]
    public void Music_ReversalUsesElapsedTime_AsFadeDuration()
    {
        var sut = CreateManager("song1");
        sut.SetSoundtracks([SingleTrackSoundtrack("song1", loop: true)]);
        sut.SetActiveSoundtrack("test");
        sut.Music(0, 0); // playing at full volume

        sut.Music(-1, fadeMs: 1000); // begin fade out
        sut.Update(TimeSpan.FromMilliseconds(400)); // 40% through → volume ~0.6

        sut.Music(0, fadeMs: 1000); // reverse — should use 400ms as new fade duration

        // After 400ms the reversal should complete (back to full volume)
        sut.Update(TimeSpan.FromMilliseconds(400));

        sut.CurrentVolume.Should().Be(1f,
            "reversal should complete in the elapsed time of the original fade (400ms)");
        sut.IsFading.Should().BeFalse("fade should be complete after reversal duration");
    }

    [Fact]
    public void Music_ReversesCrossfade_WhenOldTrackReplayedDuringCrossfade()
    {
        var sut = CreateManager("song1", "song2");
        sut.SetSoundtracks([TwoTrackSoundtrack("song1", 0, "song2", 1)]);
        sut.SetActiveSoundtrack("test");

        sut.Music(0, 0); // play track 0
        var originalInstance = sut.CurrentInstance!;

        sut.Music(1, fadeMs: 1000); // crossfade to track 1
        var newInstance = sut.CurrentInstance!;
        sut.Update(TimeSpan.FromMilliseconds(300)); // 30% through crossfade

        sut.Music(0, fadeMs: 1000); // replay track 0 — should reverse crossfade

        // The outgoing (original) instance should be re-promoted to current
        sut.CurrentInstance.Should().BeSameAs(originalInstance,
            "reversing a crossfade should re-promote the outgoing instance");
        sut.CurrentTrackIndex.Should().Be(0, "should be back on track 0");
    }

    [Fact]
    public void Music_DisposesNewInstance_WhenCrossfadeReversed()
    {
        var sut = CreateManager("song1", "song2");
        sut.SetSoundtracks([TwoTrackSoundtrack("song1", 0, "song2", 1)]);
        sut.SetActiveSoundtrack("test");

        sut.Music(0, 0); // play track 0
        sut.Music(1, fadeMs: 1000); // crossfade to track 1
        var newInstance = sut.CurrentInstance!;
        sut.Update(TimeSpan.FromMilliseconds(300)); // 30% through crossfade

        sut.Music(0, fadeMs: 1000); // reverse the crossfade

        newInstance.IsDisposed.Should().BeTrue(
            "the new instance (from the interrupted crossfade) should be disposed");
        sut.OutgoingInstance.Should().BeNull(
            "there should be no outgoing instance after reversal — only the restored track");
    }

    // -------------------------------------------------------------------------
    #endregion
}
