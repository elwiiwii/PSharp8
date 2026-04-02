using FluentAssertions;
using PSharp8.Audio;
using PSharp8.Tests.Infrastructure;
using Xunit;

namespace PSharp8.Tests.Audio;

[Collection("Fna")]
public class AudioManagerUpdateTests(FnaFixture fixture) : IDisposable
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

    private static Soundtrack MultiPartSoundtrack(params (string filename, bool loop)[] parts)
    {
        var trackParts = parts.Select(p => new TrackPart(p.filename + ".ogg", p.loop)).ToList();
        return new Soundtrack("test", [new Track(trackParts, channel: 0)]);
    }

    // -------------------------------------------------------------------------
    #region Time-Based Fade Progression
    // -------------------------------------------------------------------------

    [Fact]
    public void Update_ProgressesFadeIn_OverElapsedMs()
    {
        var sut = CreateManager("song1");
        sut.SetSoundtracks([SingleTrackSoundtrack("song1", loop: true)]);
        sut.SetActiveSoundtrack("test");

        sut.Music(0, fadeMs: 1000); // fade in over 1 second
        sut.CurrentVolume.Should().Be(0f, "precondition: starts at zero");

        sut.Update(TimeSpan.FromMilliseconds(500)); // half the fade duration

        sut.CurrentVolume.Should().BeApproximately(0.5f, 0.01f,
            "volume should be ~50% after half the fade duration");
        sut.IsFading.Should().BeTrue("fade should still be in progress");
    }

    [Fact]
    public void Update_ReachesFullVolume_AfterFadeMsElapsed()
    {
        var sut = CreateManager("song1");
        sut.SetSoundtracks([SingleTrackSoundtrack("song1", loop: true)]);
        sut.SetActiveSoundtrack("test");

        sut.Music(0, fadeMs: 500);

        sut.Update(TimeSpan.FromMilliseconds(500)); // exactly the full fade duration

        sut.CurrentVolume.Should().Be(1f, "volume should reach full after fade completes");
        sut.IsFading.Should().BeFalse("fade should be finished");
    }

    [Fact]
    public void Update_ProgressesFadeOut_OverElapsedMs()
    {
        var sut = CreateManager("song1");
        sut.SetSoundtracks([SingleTrackSoundtrack("song1", loop: true)]);
        sut.SetActiveSoundtrack("test");

        sut.Music(0, 0); // play at full volume
        sut.Music(-1, fadeMs: 1000); // begin fade out

        sut.Update(TimeSpan.FromMilliseconds(500)); // half the fade duration

        sut.CurrentVolume.Should().BeApproximately(0.5f, 0.01f,
            "volume should be ~50% after half the fade-out duration");
        sut.IsPlaying.Should().BeTrue("still playing while fading out");
    }

    [Fact]
    public void Update_StopsAndDisposes_WhenFadeOutCompletes()
    {
        var sut = CreateManager("song1");
        sut.SetSoundtracks([SingleTrackSoundtrack("song1", loop: true)]);
        sut.SetActiveSoundtrack("test");

        sut.Music(0, 0);
        var instance = sut.CurrentInstance!;
        sut.Music(-1, fadeMs: 200);

        sut.Update(TimeSpan.FromMilliseconds(200)); // complete the fade out

        sut.IsPlaying.Should().BeFalse("music should stop after fade-out completes");
        sut.IsFading.Should().BeFalse();
        sut.CurrentVolume.Should().Be(0f);
        instance.IsDisposed.Should().BeTrue("instance should be disposed after fade-out completes");
    }

    [Fact]
    public void Update_CompletesOvershoot_WhenElapsedExceedsFadeMs()
    {
        var sut = CreateManager("song1");
        sut.SetSoundtracks([SingleTrackSoundtrack("song1", loop: true)]);
        sut.SetActiveSoundtrack("test");

        sut.Music(0, fadeMs: 100);

        sut.Update(TimeSpan.FromMilliseconds(999)); // far exceeds fade duration

        sut.CurrentVolume.Should().Be(1f, "volume should clamp to full, not exceed it");
        sut.IsFading.Should().BeFalse("fade should be finished");
    }

    [Fact]
    public void Update_DisposesOutgoingInstance_WhenCrossfadeCompletes()
    {
        var sut = CreateManager("song1", "song2");
        sut.SetSoundtracks([TwoTrackSoundtrack("song1", 0, "song2", 1)]);
        sut.SetActiveSoundtrack("test");

        sut.Music(0, 0);
        var oldInstance = sut.CurrentInstance!;

        sut.Music(1, fadeMs: 300); // crossfade

        sut.Update(TimeSpan.FromMilliseconds(300)); // complete the crossfade

        oldInstance.IsDisposed.Should().BeTrue("outgoing instance should be disposed after crossfade");
        sut.CurrentVolume.Should().Be(1f, "new track should be at full volume");
        sut.IsFading.Should().BeFalse();
    }

    [Fact]
    public void Update_DoesNothing_WhenNotFading()
    {
        var sut = CreateManager("song1");
        sut.SetSoundtracks([SingleTrackSoundtrack("song1", loop: true)]);
        sut.SetActiveSoundtrack("test");

        sut.Music(0, 0); // play at full volume, no fade
        sut.CurrentVolume.Should().Be(1f);

        sut.Update(TimeSpan.FromMilliseconds(1000)); // large elapsed, but no fade in progress

        sut.CurrentVolume.Should().Be(1f, "volume should be unchanged when not fading");
        sut.IsFading.Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    #endregion
    #region TrackPart Sequencing
    // -------------------------------------------------------------------------

    [Fact]
    public void Update_AdvancesToNextPart_WhenNonLoopingPartStops()
    {
        var sut = CreateManager("intro", "main");
        sut.SetSoundtracks([MultiPartSoundtrack(("intro", false), ("main", true))]);
        sut.SetActiveSoundtrack("test");

        sut.Music(0, 0);
        sut.CurrentPartIndex.Should().Be(0, "precondition: on first part");

        // Simulate the first (non-looping) part finishing by stopping it
        sut.CurrentInstance!.Stop();

        sut.Update(TimeSpan.FromMilliseconds(16)); // one frame tick

        sut.CurrentPartIndex.Should().Be(1, "should have advanced to the second part");
        sut.IsPlaying.Should().BeTrue("should still be playing the next part");
    }

    [Fact]
    public void Update_StopsPlayback_WhenLastNonLoopingPartEnds()
    {
        var sut = CreateManager("part1", "part2");
        sut.SetSoundtracks([MultiPartSoundtrack(("part1", false), ("part2", false))]);
        sut.SetActiveSoundtrack("test");

        sut.Music(0, 0);
        sut.CurrentInstance!.Stop(); // first part finishes
        sut.Update(TimeSpan.FromMilliseconds(16)); // advance to second part
        sut.CurrentPartIndex.Should().Be(1, "precondition: on last part");

        sut.CurrentInstance!.Stop(); // second (last, non-looping) part finishes
        sut.Update(TimeSpan.FromMilliseconds(16));

        sut.IsPlaying.Should().BeFalse("playback should stop when all non-looping parts finish");
        sut.CurrentTrackIndex.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    #endregion
}
