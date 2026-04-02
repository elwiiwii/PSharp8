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
        var act = () => new AudioManager(musicDirectory: null!);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("musicDirectory");
    }

    [Fact]
    public void SetSfxDictionary_ThrowsArgumentNullException_WhenDictIsNull()
    {
        var sut = new AudioManager("");
        var act = () => sut.SetSfxDictionary(null!);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("dict");
    }

    [Fact]
    public void IsPlaying_ReturnsFalse_AfterConstruction()
    {
        var sut = new AudioManager("");

        sut.IsPlaying.Should().BeFalse();
    }

    [Fact]
    public void CurrentTrackIndex_ReturnsNull_AfterConstruction()
    {
        var sut = new AudioManager("");

        sut.CurrentTrackIndex.Should().BeNull();
    }

    [Fact]
    public void CurrentVolume_ReturnsZero_AfterConstruction()
    {
        var sut = new AudioManager("");

        sut.CurrentVolume.Should().Be(0f);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region SetSoundtracks / SetActiveSoundtrack
    // -------------------------------------------------------------------------

    [Fact]
    public void SetSoundtracks_ThrowsArgumentNullException_WhenNull()
    {
        var sut = new AudioManager("");

        var act = () => sut.SetSoundtracks(null!);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("soundtracks");
    }

    [Fact]
    public void SetActiveSoundtrack_SelectsSoundtrack_WhenNameMatches()
    {
        var sut = new AudioManager("");
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
        var sut = new AudioManager("");
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
        var sut = new AudioManager("");
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
        var sut = new AudioManager("");

        var act = () => sut.Music(0, 0);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Music_ThrowsInvalidOperationException_WhenNoSoundtrackSet_WithZeroFade()
    {
        var sut = new AudioManager("");

        var act = () => sut.Music(0, 0);

        act.Should().Throw<InvalidOperationException>();
    }

    // -------------------------------------------------------------------------
    #endregion
    #region SetMusicVolume / SetSfxVolume
    // -------------------------------------------------------------------------

    [Fact]
    public void MusicBaseVolume_DefaultsTo1()
    {
        new AudioManager("").MusicBaseVolume.Should().Be(1f);
    }

    [Fact]
    public void SfxBaseVolume_DefaultsTo1()
    {
        new AudioManager("").SfxBaseVolume.Should().Be(1f);
    }

    [Fact]
    public void SetMusicVolume_StoresMusicBaseVolume()
    {
        var sut = new AudioManager("");

        sut.SetMusicVolume(0.6f);

        sut.MusicBaseVolume.Should().BeApproximately(0.6f, precision: 0.001f);
    }

    [Fact]
    public void SetSfxVolume_StoresSfxBaseVolume()
    {
        var sut = new AudioManager("");

        sut.SetSfxVolume(0.3f);

        sut.SfxBaseVolume.Should().BeApproximately(0.3f, precision: 0.001f);
    }

    // -------------------------------------------------------------------------
    #endregion
}

