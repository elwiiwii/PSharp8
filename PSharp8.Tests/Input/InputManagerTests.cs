using FluentAssertions;
using Moq;
using PSharp8.Input;
using Xunit;

namespace PSharp8.Tests.Input;

public class InputManagerTests
{
    // --------------------------------------------------------------------------
    #region Helpers
    // --------------------------------------------------------------------------

    /// <summary>Creates a mock provider reporting <paramref name="held"/> buttons as held.</summary>
    private static Mock<IInputProvider> ProviderWith(params PicoButton[] held)
    {
        var state = new bool[7];
        foreach (var b in held) state[(int)b] = true;
        var mock = new Mock<IInputProvider>();
        mock.Setup(p => p.GetHeldButtons()).Returns(state);
        return mock;
    }

    private static Mock<IInputProvider> NoButtonsHeld()
    {
        var mock = new Mock<IInputProvider>();
        mock.Setup(p => p.GetHeldButtons()).Returns(new bool[7]);
        return mock;
    }

    private static InputManager CreateSut(IInputProvider provider, BtnpConfig? config = null)
        => new(provider, config);

    // --------------------------------------------------------------------------
    #endregion
    #region Constructor
    // --------------------------------------------------------------------------

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenProviderIsNull()
    {
        var act = () => new InputManager(provider: null!);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("provider");
    }

    [Fact]
    public void Constructor_DoesNotThrow_WhenConfigIsNull()
    {
        var act = () => CreateSut(NoButtonsHeld().Object, config: null);

        act.Should().NotThrow();
    }

    // --------------------------------------------------------------------------
    #endregion
    #region Update
    // --------------------------------------------------------------------------

    [Fact]
    public void Update_DoesNotThrow_WhenNoButtonsHeld()
    {
        var sut = CreateSut(NoButtonsHeld().Object);

        var act = () => sut.Update(TimeSpan.FromMilliseconds(16));

        act.Should().NotThrow();
    }

    [Fact]
    public void Update_CallsGetHeldButtons_EachFrame()
    {
        var mock = NoButtonsHeld();
        var sut = CreateSut(mock.Object);

        sut.Update(TimeSpan.FromMilliseconds(16));
        sut.Update(TimeSpan.FromMilliseconds(16));

        mock.Verify(p => p.GetHeldButtons(), Times.Exactly(2));
    }

    // --------------------------------------------------------------------------
    #endregion
    #region Btn
    // --------------------------------------------------------------------------

    [Fact]
    public void Btn_ReturnsFalse_WhenInputBlocked()
    {
        var sut = CreateSut(ProviderWith(PicoButton.Left).Object);
        sut.Update(TimeSpan.FromMilliseconds(16));
        sut.InputBlocked = true;

        sut.Btn((int)PicoButton.Left, 0).Should().BeFalse();
    }

    [Fact]
    public void Btn_ReturnsFalse_WhenButtonNotHeld()
    {
        var sut = CreateSut(NoButtonsHeld().Object);
        sut.Update(TimeSpan.FromMilliseconds(16));

        sut.Btn((int)PicoButton.Left, 0).Should().BeFalse();
    }

    [Fact]
    public void Btn_ReturnsTrue_WhenButtonIsHeld()
    {
        var sut = CreateSut(ProviderWith(PicoButton.Left).Object);
        sut.Update(TimeSpan.FromMilliseconds(16));

        sut.Btn((int)PicoButton.Left, 0).Should().BeTrue();
    }

    [Fact]
    public void Btn_ReturnsFalse_AfterButtonReleased()
    {
        var mock = ProviderWith(PicoButton.Left);
        var sut = CreateSut(mock.Object);
        sut.Update(TimeSpan.FromMilliseconds(16));

        // Release the button
        mock.Setup(p => p.GetHeldButtons()).Returns(new bool[7]);
        sut.Update(TimeSpan.FromMilliseconds(16));

        sut.Btn((int)PicoButton.Left, 0).Should().BeFalse();
    }

    // --------------------------------------------------------------------------
    #endregion
    #region Btnp
    // --------------------------------------------------------------------------

    [Fact]
    public void Btnp_ReturnsFalse_WhenInputBlocked()
    {
        var sut = CreateSut(ProviderWith(PicoButton.Left).Object);
        sut.Update(TimeSpan.FromMilliseconds(16));
        sut.InputBlocked = true;

        sut.Btnp((int)PicoButton.Left, 0).Should().BeFalse();
    }

    [Fact]
    public void Btnp_ReturnsTrue_OnFreshPress()
    {
        var sut = CreateSut(ProviderWith(PicoButton.Left).Object);
        sut.Update(TimeSpan.FromMilliseconds(16));

        sut.Btnp((int)PicoButton.Left, 0).Should().BeTrue();
    }

    [Fact]
    public void Btnp_ReturnsFalse_OnSecondFrame_WhileHeld_BeforeInitialRepeat()
    {
        var config = new BtnpConfig(InitialRepeatMs: 100.0, SubsequentRepeatMs: 50.0);
        var mock = ProviderWith(PicoButton.Left);
        var sut = CreateSut(mock.Object, config);
        sut.Update(TimeSpan.FromMilliseconds(16));  // fresh press fires
        sut.Update(TimeSpan.FromMilliseconds(50));  // 50ms held < 100ms initial repeat

        sut.Btnp((int)PicoButton.Left, 0).Should().BeFalse();
    }

    [Fact]
    public void Btnp_ReturnsTrue_AfterInitialRepeat()
    {
        var config = new BtnpConfig(InitialRepeatMs: 100.0, SubsequentRepeatMs: 50.0);
        var mock = ProviderWith(PicoButton.Left);
        var sut = CreateSut(mock.Object, config);
        sut.Update(TimeSpan.FromMilliseconds(16));   // fresh press
        sut.Update(TimeSpan.FromMilliseconds(200));  // 200ms >= 100ms → repeat fires

        sut.Btnp((int)PicoButton.Left, 0).Should().BeTrue();
    }

    [Fact]
    public void Btnp_ReturnsTrue_AtSubsequentRepeatIntervals()
    {
        var config = new BtnpConfig(InitialRepeatMs: 100.0, SubsequentRepeatMs: 50.0);
        var mock = ProviderWith(PicoButton.Left);
        var sut = CreateSut(mock.Object, config);
        sut.Update(TimeSpan.FromMilliseconds(16));   // fresh press
        sut.Update(TimeSpan.FromMilliseconds(200));  // initial repeat fires
        sut.Update(TimeSpan.FromMilliseconds(100));  // 100ms >= 50ms → subsequent repeat

        sut.Btnp((int)PicoButton.Left, 0).Should().BeTrue();
    }

    [Fact]
    public void Btnp_Pause_ReturnsFalse_WhenHeld()
    {
        // Pause has no auto-repeat — only the first-press frame fires Btnp
        var config = new BtnpConfig(InitialRepeatMs: 100.0, SubsequentRepeatMs: 50.0);
        var mock = ProviderWith(PicoButton.Pause);
        var sut = CreateSut(mock.Object, config);
        sut.Update(TimeSpan.FromMilliseconds(16));   // first frame fires
        sut.Update(TimeSpan.FromMilliseconds(200));  // held for 200ms > InitialRepeatMs

        sut.Btnp((int)PicoButton.Pause, 0).Should().BeFalse();
    }

    [Fact]
    public void Btnp_ReturnsFalse_OnSecondFrame_WhenAlreadyHeld()
    {
        // Frame 1: not-held → held = fresh press (Btnp true)
        // Frame 2: held → held = no fresh press, no repeat yet (Btnp false)
        var config = new BtnpConfig(InitialRepeatMs: 500.0, SubsequentRepeatMs: 50.0);
        var mock = ProviderWith(PicoButton.Left);
        var sut = CreateSut(mock.Object, config);
        sut.Update(TimeSpan.FromMilliseconds(16)); // fresh press
        sut.Update(TimeSpan.FromMilliseconds(16)); // still held, repeat not reached

        sut.Btnp((int)PicoButton.Left, 0).Should().BeFalse();
    }

    // --------------------------------------------------------------------------
    #endregion
}
