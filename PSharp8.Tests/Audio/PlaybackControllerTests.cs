using FluentAssertions;
using Microsoft.Xna.Framework.Audio;
using PSharp8.Audio;
using PSharp8.Tests.Infrastructure;
using Xunit;

namespace PSharp8.Tests.Audio;

[Collection("Fna")]
public class PlaybackControllerTests(FnaFixture fixture) : IDisposable
{
    private readonly FnaFixture _fixture = fixture;
    private TempMusicDirectory? _tempDir;

    public void Dispose() => _tempDir?.Dispose();

    private PlaybackController CreateController(params string[] filenames)
    {
        var oggNames = filenames.Select(f => f + ".ogg").ToArray();
        _tempDir = FnaFixture.CreateTempMusicDirectory(oggNames);
        return new PlaybackController(_tempDir.Path);
    }

    private static Track SinglePartTrack(string filename, bool loop, int channel = 0)
        => new([new TrackPart(filename, loop)], channel);

    private static Track MultiPartTrack(int channel, params (string filename, bool loop)[] parts)
        => new(parts.Select(p => new TrackPart(p.filename, p.loop)).ToList(), channel);

    // -------------------------------------------------------------------------
    #region Constructor & Defaults
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenMusicDirectoryIsNull()
    {
        var act = () => new PlaybackController(musicDirectory: null!);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("musicDirectory");
    }

    [Fact]
    public void IsPlaying_ReturnsFalse_AfterConstruction()
    {
        var sut = CreateController("song1");

        sut.IsPlaying.Should().BeFalse();
    }

    [Fact]
    public void CurrentVolume_ReturnsZero_AfterConstruction()
    {
        var sut = CreateController("song1");

        sut.CurrentVolume.Should().Be(0f);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region StartTrack
    // -------------------------------------------------------------------------

    [Fact]
    public void StartTrack_CreatesInstance_ForFirstPart()
    {
        var sut = CreateController("song1");
        var track = SinglePartTrack("song1", loop: true);

        sut.StartTrack(track, trackIndex: 0);

        sut.HasCurrentInstance.Should().BeTrue();
        sut.IsPlaying.Should().BeTrue();
        sut.CurrentTrackIndex.Should().Be(0);
    }

    [Fact]
    public void StartTrack_PreservesPosition_WhenSameChannel()
    {
        var sut = CreateController("song1", "song2");
        var track1 = SinglePartTrack("song1", loop: true, channel: 0);
        var track2 = SinglePartTrack("song2", loop: true, channel: 0);

        sut.StartTrack(track1, 0);
        sut.PlaybackPositionMs = 500.0;

        sut.StartTrack(track2, 1);

        sut.PlaybackPositionMs.Should().Be(500.0);
    }

    [Fact]
    public void StartTrack_ResetsPosition_WhenDifferentChannel()
    {
        var sut = CreateController("song1", "song2");
        var track1 = SinglePartTrack("song1", loop: true, channel: 0);
        var track2 = SinglePartTrack("song2", loop: true, channel: 1);

        sut.StartTrack(track1, 0);
        sut.PlaybackPositionMs = 500.0;

        sut.StartTrack(track2, 1);

        sut.PlaybackPositionMs.Should().Be(0.0);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Play & Volume
    // -------------------------------------------------------------------------

    [Fact]
    public void Play_DoesNotThrow_WhenInstanceExists()
    {
        var sut = CreateController("song1");
        sut.StartTrack(SinglePartTrack("song1", loop: true), 0);

        var act = () => sut.Play();

        act.Should().NotThrow();
    }

    [Fact]
    public void ApplyVolume_SetsVolumeOnInstance()
    {
        var sut = CreateController("song1");
        sut.StartTrack(SinglePartTrack("song1", loop: true), 0);

        sut.ApplyVolume(0.7f);

        sut.CurrentVolume.Should().Be(0.7f);
        sut.CurrentInstance!.Volume.Should().Be(0.7f);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region MoveCurrentToOutgoing
    // -------------------------------------------------------------------------

    [Fact]
    public void MoveCurrentToOutgoing_TransfersInstance()
    {
        var sut = CreateController("song1");
        sut.StartTrack(SinglePartTrack("song1", loop: true), 0);
        sut.Play();
        var instance = sut.CurrentInstance!;

        sut.MoveCurrentToOutgoing();

        sut.HasOutgoingInstance.Should().BeTrue();
        sut.OutgoingInstance.Should().BeSameAs(instance);
        sut.CurrentInstance.Should().BeNull();
    }

    [Fact]
    public void MoveCurrentToOutgoing_SilencesOutgoing()
    {
        var sut = CreateController("song1");
        sut.StartTrack(SinglePartTrack("song1", loop: true), 0);
        sut.ApplyVolume(1f);
        sut.Play();

        sut.MoveCurrentToOutgoing();

        sut.OutgoingInstance!.Volume.Should().Be(0f);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region ReverseCrossfade
    // -------------------------------------------------------------------------

    [Fact]
    public void ReverseCrossfade_RepromotesOutgoing_DisposesNew()
    {
        var sut = CreateController("song1", "song2");
        var track0 = SinglePartTrack("song1", loop: true, channel: 0);
        var track1 = SinglePartTrack("song2", loop: true, channel: 1);

        sut.StartTrack(track0, 0);
        sut.Play();
        var original = sut.CurrentInstance!;

        sut.MoveCurrentToOutgoing();
        sut.StartTrack(track1, 1);
        var replacement = sut.CurrentInstance!;

        sut.ReverseCrossfade(0, track0);

        sut.CurrentInstance.Should().BeSameAs(original);
        replacement.IsDisposed.Should().BeTrue();
        sut.HasOutgoingInstance.Should().BeFalse();
        sut.CurrentTrackIndex.Should().Be(0);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region AdvancePart
    // -------------------------------------------------------------------------

    [Fact]
    public void AdvancePart_MovesToNextPart_ReturnsFalse()
    {
        var sut = CreateController("intro", "main");
        var track = MultiPartTrack(0, ("intro", false), ("main", true));
        sut.StartTrack(track, 0);
        sut.ApplyVolume(1f);
        sut.Play();

        var stopped = sut.AdvancePart();

        stopped.Should().BeFalse();
        sut.CurrentPartIndex.Should().Be(1);
    }

    [Fact]
    public void AdvancePart_StopsPlayback_WhenNoParts_ReturnsTrue()
    {
        var sut = CreateController("only");
        var track = MultiPartTrack(0, ("only", false));
        sut.StartTrack(track, 0);
        sut.Play();

        var stopped = sut.AdvancePart();

        stopped.Should().BeTrue();
        sut.IsPlaying.Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    #endregion
    #region StopAndDispose
    // -------------------------------------------------------------------------

    [Fact]
    public void StopAndDispose_ClearsAllState()
    {
        var sut = CreateController("song1");
        sut.StartTrack(SinglePartTrack("song1", loop: true), 0);
        sut.ApplyVolume(1f);
        sut.Play();
        var instance = sut.CurrentInstance!;

        sut.StopAndDispose();

        sut.IsPlaying.Should().BeFalse();
        sut.CurrentTrackIndex.Should().BeNull();
        sut.CurrentVolume.Should().Be(0f);
        sut.HasCurrentInstance.Should().BeFalse();
        instance.IsDisposed.Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    #endregion
}
