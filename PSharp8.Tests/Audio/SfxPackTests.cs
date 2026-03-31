using FluentAssertions;
using PSharp8.Audio;
using Xunit;

namespace PSharp8.Tests.Audio;

public class SfxPackTests
{
    // -------------------------------------------------------------------------
    #region Construction & Properties
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenNameIsNull()
    {
        var act = () => new SfxPack(name: null!, prefix: "pcraft_og_");

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("name");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenPrefixIsNull()
    {
        var act = () => new SfxPack(name: "original", prefix: null!);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("prefix");
    }

    [Fact]
    public void Name_ReturnsConstructorValue()
    {
        var sut = new SfxPack("original", "pcraft_og_");

        sut.Name.Should().Be("original");
    }

    [Fact]
    public void Prefix_ReturnsConstructorValue()
    {
        var sut = new SfxPack("original", "pcraft_og_");

        sut.Prefix.Should().Be("pcraft_og_");
    }

    // -------------------------------------------------------------------------
    #endregion
}
