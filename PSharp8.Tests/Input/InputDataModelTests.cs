using FluentAssertions;
using Microsoft.Xna.Framework.Input;
using PSharp8.Input;
using Xunit;

namespace PSharp8.Tests.Input;

public class InputDataModelTests
{
    // --------------------------------------------------------------------------
    #region PicoButton enum
    // --------------------------------------------------------------------------

    [Theory]
    [InlineData(PicoButton.Left, 0)]
    [InlineData(PicoButton.Right, 1)]
    [InlineData(PicoButton.Up, 2)]
    [InlineData(PicoButton.Down, 3)]
    [InlineData(PicoButton.Primary, 4)]
    [InlineData(PicoButton.Secondary, 5)]
    [InlineData(PicoButton.Pause, 6)]
    public void PicoButton_HasCorrectIntValue(PicoButton button, int expectedValue)
    {
        ((int)button).Should().Be(expectedValue);
    }

    // --------------------------------------------------------------------------
    #endregion
    #region MouseButton enum
    // --------------------------------------------------------------------------

    [Theory]
    [InlineData(MouseButton.Left, 0)]
    [InlineData(MouseButton.Right, 1)]
    [InlineData(MouseButton.Middle, 2)]
    [InlineData(MouseButton.X1, 3)]
    [InlineData(MouseButton.X2, 4)]
    public void MouseButton_HasCorrectIntValue(MouseButton button, int expectedValue)
    {
        ((int)button).Should().Be(expectedValue);
    }

    // --------------------------------------------------------------------------
    #endregion
    #region InputSource hierarchy
    // --------------------------------------------------------------------------

    [Fact]
    public void KeyboardSource_Equal_WhenSameKey()
    {
        var a = new KeyboardSource(Keys.Left);
        var b = new KeyboardSource(Keys.Left);

        a.Should().Be(b);
    }

    [Fact]
    public void KeyboardSource_NotEqual_WhenDifferentKeys()
    {
        var a = new KeyboardSource(Keys.Left);
        var b = new KeyboardSource(Keys.Right);

        a.Should().NotBe(b);
    }

    [Fact]
    public void MouseSource_Equal_WhenSameButton()
    {
        var a = new MouseSource(MouseButton.Left);
        var b = new MouseSource(MouseButton.Left);

        a.Should().Be(b);
    }

    [Fact]
    public void GamePadSource_Equal_WhenSameButtons()
    {
        var a = new GamePadSource(Buttons.A);
        var b = new GamePadSource(Buttons.A);

        a.Should().Be(b);
    }

    [Fact]
    public void KeyboardSource_NotEqualToMouseSource_EvenWithLogicallySimilarValues()
    {
        InputSource keyboard = new KeyboardSource(Keys.Left);
        InputSource mouse = new MouseSource(MouseButton.Left);

        keyboard.Should().NotBe(mouse);
    }

    // --------------------------------------------------------------------------
    #endregion
    #region InputEvent
    // --------------------------------------------------------------------------

    [Fact]
    public void InputEvent_StoresPropertiesCorrectly_WhenConstructed()
    {
        var source = new KeyboardSource(Keys.Space);

        var evt = new InputEvent(source, IsDown: true, TimestampNs: 12345UL);

        evt.Source.Should().BeSameAs(source);
        evt.IsDown.Should().BeTrue();
        evt.TimestampNs.Should().Be(12345UL);
    }

    [Fact]
    public void InputEvent_Equal_WhenSameValues()
    {
        var source = new KeyboardSource(Keys.Space);
        var a = new InputEvent(source, IsDown: true, TimestampNs: 99UL);
        var b = new InputEvent(source, IsDown: true, TimestampNs: 99UL);

        a.Should().Be(b);
    }

    [Fact]
    public void InputEvent_Constructor_ThrowsArgumentNullException_WhenSourceIsNull()
    {
        var act = () => new InputEvent(Source: null!, IsDown: false, TimestampNs: 0UL);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("Source");
    }

    // --------------------------------------------------------------------------
    #endregion
    #region InputBindings
    // --------------------------------------------------------------------------

    [Fact]
    public void InputBindings_Constructor_ThrowsArgumentNullException_WhenBindingsIsNull()
    {
        var act = () => new InputBindings(bindings: null!);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("bindings");
    }

    [Fact]
    public void InputBindings_Indexer_ReturnsListForGivenButton()
    {
        IReadOnlyList<InputSource> sources = new List<InputSource> { new KeyboardSource(Keys.Left) };
        var dict = new Dictionary<PicoButton, IReadOnlyList<InputSource>>
        {
            [PicoButton.Left] = sources
        };
        var sut = new InputBindings(dict);

        sut[PicoButton.Left].Should().BeSameAs(sources);
    }

    [Fact]
    public void InputBindings_Default_IsNotNull()
    {
        InputBindings.Default.Should().NotBeNull();
    }

    [Theory]
    [InlineData(PicoButton.Left)]
    [InlineData(PicoButton.Right)]
    [InlineData(PicoButton.Up)]
    [InlineData(PicoButton.Down)]
    [InlineData(PicoButton.Primary)]
    [InlineData(PicoButton.Secondary)]
    [InlineData(PicoButton.Pause)]
    public void InputBindings_Default_HasEntryForEveryPicoButton(PicoButton button)
    {
        var act = () => InputBindings.Default[button];

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(PicoButton.Left)]
    [InlineData(PicoButton.Right)]
    [InlineData(PicoButton.Up)]
    [InlineData(PicoButton.Down)]
    [InlineData(PicoButton.Primary)]
    [InlineData(PicoButton.Secondary)]
    [InlineData(PicoButton.Pause)]
    public void InputBindings_Default_HasAtLeastOneSourceForEveryPicoButton(PicoButton button)
    {
        InputBindings.Default[button].Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void InputBindings_Default_LeftContainsKeyboardLeft()
    {
        InputBindings.Default[PicoButton.Left].Should().Contain(new KeyboardSource(Keys.Left));
    }

    [Fact]
    public void InputBindings_Default_LeftContainsKeyboardA()
    {
        InputBindings.Default[PicoButton.Left].Should().Contain(new KeyboardSource(Keys.A));
    }

    [Fact]
    public void InputBindings_Default_PrimaryContainsGamePadA()
    {
        InputBindings.Default[PicoButton.Primary].Should().Contain(new GamePadSource(Buttons.A));
    }

    [Fact]
    public void InputBindings_Default_PauseContainsKeyboardEscape()
    {
        InputBindings.Default[PicoButton.Pause].Should().Contain(new KeyboardSource(Keys.Escape));
    }

    // --------------------------------------------------------------------------
    #endregion
    #region BtnpConfig
    // --------------------------------------------------------------------------

    [Fact]
    public void BtnpConfig_Default_InitialRepeatMsIs250()
    {
        var config = new BtnpConfig();

        config.InitialRepeatMs.Should().Be(250.0);
    }

    [Fact]
    public void BtnpConfig_Default_SubsequentRepeatMsIs67()
    {
        var config = new BtnpConfig();

        config.SubsequentRepeatMs.Should().Be(67.0);
    }

    [Fact]
    public void BtnpConfig_Default_DebounceMsIsZero()
    {
        var config = new BtnpConfig();

        config.DebounceMs.Should().Be(0.0);
    }

    [Fact]
    public void BtnpConfig_StoresCustomValues_WhenConstructed()
    {
        var config = new BtnpConfig(InitialRepeatMs: 100.0, SubsequentRepeatMs: 33.0, DebounceMs: 10.0);

        config.InitialRepeatMs.Should().Be(100.0);
        config.SubsequentRepeatMs.Should().Be(33.0);
        config.DebounceMs.Should().Be(10.0);
    }

    [Fact]
    public void BtnpConfig_Constructor_ThrowsArgumentOutOfRangeException_WhenDebounceMsIsNegative()
    {
        var act = () => new BtnpConfig(DebounceMs: -1.0);

        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("DebounceMs");
    }
    
    // --------------------------------------------------------------------------
    #endregion
    // --------------------------------------------------------------------------
}
