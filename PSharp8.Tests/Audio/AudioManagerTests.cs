using FluentAssertions;
using Microsoft.Xna.Framework.Audio;
using PSharp8.Audio;
using Xunit;

namespace PSharp8.Tests.Audio;

public class AudioManagerTests
{
    // -------------------------------------------------------------------------
    #region Construction & Defaults
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenMusicDictionaryIsNull()
    {
        var act = () => new AudioManager(musicDirectory: null!, sfxDictionary: new Dictionary<string, SoundEffect>());

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("musicDirectory");
    }

    [Fact]
    public void IsPlaying_ReturnsFalse_AfterConstruction()
    {
        var sut = new AudioManager("", new Dictionary<string, SoundEffect>());

        sut.IsPlaying.Should().BeFalse();
    }

    [Fact]
    public void CurrentTrackIndex_ReturnsNull_AfterConstruction()
    {
        var sut = new AudioManager("", new Dictionary<string, SoundEffect>());

        sut.CurrentTrackIndex.Should().BeNull();
    }

    [Fact]
    public void CurrentVolume_ReturnsZero_AfterConstruction()
    {
        var sut = new AudioManager("", new Dictionary<string, SoundEffect>());

        sut.CurrentVolume.Should().Be(0f);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region SetSoundtracks / SetActiveSoundtrack
    // -------------------------------------------------------------------------

    [Fact]
    public void SetSoundtracks_ThrowsArgumentNullException_WhenNull()
    {
        var sut = new AudioManager("", new Dictionary<string, SoundEffect>());

        var act = () => sut.SetSoundtracks(null!);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("soundtracks");
    }

    [Fact]
    public void SetActiveSoundtrack_SelectsSoundtrack_WhenNameMatches()
    {
        var sut = new AudioManager("", new Dictionary<string, SoundEffect>());
        var soundtracks = new List<Soundtrack>
        {
            new("original", [new Track([new TrackPart("file1", true)], 0)]),
            new("new!", [new Track([new TrackPart("file2", true)], 0)])
        };
        sut.SetSoundtracks(soundtracks);

        // Should not throw — soundtrack "new!" exists
        var act = () => sut.SetActiveSoundtrack("new!");

        act.Should().NotThrow();
    }

    [Fact]
    public void SetActiveSoundtrack_ThrowsKeyNotFound_WhenNameNotFound()
    {
        var sut = new AudioManager("", new Dictionary<string, SoundEffect>());
        var soundtracks = new List<Soundtrack>
        {
            new("original", [new Track([new TrackPart("file1", true)], 0)])
        };
        sut.SetSoundtracks(soundtracks);

        var act = () => sut.SetActiveSoundtrack("nonexistent");

        act.Should().Throw<KeyNotFoundException>();
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Edge Cases & Guards
    // -------------------------------------------------------------------------

    [Fact]
    public void Music_ThrowsArgumentOutOfRange_WhenIndexExceedsTrackCount()
    {
        var sut = new AudioManager("", new Dictionary<string, SoundEffect>());
        var soundtracks = new List<Soundtrack>
        {
            new("original", [new Track([new TrackPart("file1", true)], 0)])
        };
        sut.SetSoundtracks(soundtracks);
        sut.SetActiveSoundtrack("original");

        // Soundtrack "original" has 1 track (index 0 only), so index 5 is out of range
        var act = () => sut.Music(5, 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Music_ThrowsInvalidOperationException_WhenNoSoundtrackSet()
    {
        var sut = new AudioManager("", new Dictionary<string, SoundEffect>());

        var act = () => sut.Music(0, 0);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Music_ThrowsInvalidOperationException_WhenNoSoundtrackSet_WithZeroFade()
    {
        var sut = new AudioManager("", new Dictionary<string, SoundEffect>());

        var act = () => sut.Music(0, 0);

        act.Should().Throw<InvalidOperationException>();
    }

    // -------------------------------------------------------------------------
    #endregion
}

