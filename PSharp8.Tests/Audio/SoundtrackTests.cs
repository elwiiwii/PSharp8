using FluentAssertions;
using PSharp8.Audio;
using Xunit;

namespace PSharp8.Tests.Audio;

public class SoundtrackTests
{
    // -------------------------------------------------------------------------
    #region Construction
    // -------------------------------------------------------------------------

    [Fact]
    public void Soundtrack_StoresNameAndTracks_WhenConstructed()
    {
        var tracks = new List<Track>
        {
            new(parts: [new TrackPart("file1", loop: true)], channel: 0)
        };

        var sut = new Soundtrack("original", tracks);

        sut.Name.Should().Be("original");
        sut.Tracks.Should().BeSameAs(tracks);
    }

    [Fact]
    public void Track_StoresPartsAndChannel_WhenConstructed()
    {
        var parts = new List<TrackPart> { new("cave_0", loop: false), new("cave_1", loop: true) };

        var sut = new Track(parts, channel: 2);

        sut.Parts.Should().BeSameAs(parts);
        sut.Channel.Should().Be(2);
    }

    [Fact]
    public void TrackPart_StoresFilenameAndLoop_WhenConstructed()
    {
        var sut = new TrackPart("pcraft_og_cave_1", loop: true);

        sut.Filename.Should().Be("pcraft_og_cave_1");
        sut.Loop.Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    #endregion
}
