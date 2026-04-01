using FluentAssertions;
using Microsoft.Xna.Framework.Input;
using PSharp8.Input;
using Xunit;

namespace PSharp8.Tests.Input;

public class InputManagerTests
{
    // --------------------------------------------------------------------------
    #region Helpers
    // --------------------------------------------------------------------------

    private static InputManager CreateSut(InputBindings? bindings = null, BtnpConfig? config = null)
        => new(bindings ?? InputBindings.Default, config);

    private static List<InputEvent> NoEvents() => [];

    private static InputEvent KeyDown(Keys key, ulong timestampNs = 0UL) =>
        new(new KeyboardSource(key), IsDown: true, TimestampNs: timestampNs);

    private static InputEvent KeyUp(Keys key, ulong timestampNs = 0UL) =>
        new(new KeyboardSource(key), IsDown: false, TimestampNs: timestampNs);

    private static InputBindings BindOnly(PicoButton button, InputSource source) =>
        new(new Dictionary<PicoButton, IReadOnlyList<InputSource>>
        {
            [button] = new List<InputSource> { source },
        });

    // --------------------------------------------------------------------------
    #endregion
    #region Constructor
    // --------------------------------------------------------------------------

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenBindingsIsNull()
    {
        var act = () => new InputManager(bindings: null!);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("bindings");
    }

    [Fact]
    public void Constructor_UsesDefaultBtnpConfig_WhenConfigIsNull()
    {
        var act = () => CreateSut(config: null);

        act.Should().NotThrow();
    }

    // --------------------------------------------------------------------------
    #endregion
    #region SetBindings
    // --------------------------------------------------------------------------

    [Fact]
    public void SetBindings_ThrowsArgumentNullException_WhenBindingsIsNull()
    {
        var sut = CreateSut();

        var act = () => sut.SetBindings(null!);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("bindings");
    }

    [Fact]
    public void SetBindings_DoesNotThrow_WhenBindingsIsValid()
    {
        var sut = CreateSut();

        var act = () => sut.SetBindings(InputBindings.Default);

        act.Should().NotThrow();
    }

    // --------------------------------------------------------------------------
    #endregion
    #region Update
    // --------------------------------------------------------------------------

    [Fact]
    public void Update_DoesNotThrow_WhenCalledWithEmptyEventList()
    {
        var sut = CreateSut();

        var act = () => sut.Update(TimeSpan.FromMilliseconds(16), NoEvents());

        act.Should().NotThrow();
    }

    [Fact]
    public void Update_DoesNotThrow_WhenCalledWithEvents()
    {
        var sut = CreateSut();
        var events = new List<InputEvent>
        {
            new(new KeyboardSource(Keys.Left), IsDown: true, TimestampNs: 1000UL),
        };

        var act = () => sut.Update(TimeSpan.FromMilliseconds(16), events);

        act.Should().NotThrow();
    }

    // --------------------------------------------------------------------------
    #endregion
    #region Btn
    // --------------------------------------------------------------------------

    [Fact]
    public void Btn_ReturnsFalse_WhenInputBlocked()
    {
        var sut = CreateSut();
        sut.Update(TimeSpan.FromMilliseconds(16), [KeyDown(Keys.Left)]);
        sut.InputBlocked = true;

        sut.Btn((int)PicoButton.Left, 0).Should().BeFalse();
    }

    [Fact]
    public void Btn_ReturnsFalse_WhenButtonNotHeld()
    {
        var sut = CreateSut();
        sut.Update(TimeSpan.FromMilliseconds(16), NoEvents());

        sut.Btn((int)PicoButton.Left, 0).Should().BeFalse();
    }

    [Fact]
    public void Btn_ReturnsTrue_WhenButtonIsHeld()
    {
        var sut = CreateSut();
        sut.Update(TimeSpan.FromMilliseconds(16), [KeyDown(Keys.Left)]);

        sut.Btn((int)PicoButton.Left, 0).Should().BeTrue();
    }

    [Fact]
    public void Btn_ReturnsFalse_AfterButtonReleased()
    {
        var sut = CreateSut();
        sut.Update(TimeSpan.FromMilliseconds(16), [KeyDown(Keys.Left)]);
        sut.Update(TimeSpan.FromMilliseconds(16), [KeyUp(Keys.Left)]);

        sut.Btn((int)PicoButton.Left, 0).Should().BeFalse();
    }

    [Fact]
    public void Btn_ReturnsFalse_ForSubFrameTap()
    {
        // KeyDown then KeyUp in same Update call — _heldNow ends as false
        var sut = CreateSut();
        sut.Update(TimeSpan.FromMilliseconds(16), [KeyDown(Keys.Left, 0UL), KeyUp(Keys.Left, 1UL)]);

        sut.Btn((int)PicoButton.Left, 0).Should().BeFalse();
    }

    // --------------------------------------------------------------------------
    #endregion
    #region Btnp
    // --------------------------------------------------------------------------

    [Fact]
    public void Btnp_ReturnsFalse_WhenInputBlocked()
    {
        var sut = CreateSut();
        sut.Update(TimeSpan.FromMilliseconds(16), [KeyDown(Keys.Left)]);
        sut.InputBlocked = true;

        sut.Btnp((int)PicoButton.Left, 0).Should().BeFalse();
    }

    [Fact]
    public void Btnp_ReturnsTrue_OnFreshPress()
    {
        var sut = CreateSut();
        sut.Update(TimeSpan.FromMilliseconds(16), [KeyDown(Keys.Left)]);

        sut.Btnp((int)PicoButton.Left, 0).Should().BeTrue();
    }

    [Fact]
    public void Btnp_ReturnsFalse_WhileHeld_BeforeInitialRepeat()
    {
        var config = new BtnpConfig(InitialRepeatMs: 100.0, SubsequentRepeatMs: 50.0);
        var sut = CreateSut(config: config);
        sut.Update(TimeSpan.FromMilliseconds(16), [KeyDown(Keys.Left)]); // fresh press fires
        sut.Update(TimeSpan.FromMilliseconds(50), NoEvents());            // 50ms held < 100ms initial repeat

        sut.Btnp((int)PicoButton.Left, 0).Should().BeFalse();
    }

    [Fact]
    public void Btnp_ReturnsTrue_AfterInitialRepeat()
    {
        var config = new BtnpConfig(InitialRepeatMs: 100.0, SubsequentRepeatMs: 50.0);
        var sut = CreateSut(config: config);
        sut.Update(TimeSpan.FromMilliseconds(16), [KeyDown(Keys.Left)]); // fresh press
        sut.Update(TimeSpan.FromMilliseconds(200), NoEvents());           // 200ms >= 100ms → repeat fires

        sut.Btnp((int)PicoButton.Left, 0).Should().BeTrue();
    }

    [Fact]
    public void Btnp_ReturnsTrue_AtSubsequentRepeatIntervals()
    {
        var config = new BtnpConfig(InitialRepeatMs: 100.0, SubsequentRepeatMs: 50.0);
        var sut = CreateSut(config: config);
        sut.Update(TimeSpan.FromMilliseconds(16), [KeyDown(Keys.Left)]); // fresh press
        sut.Update(TimeSpan.FromMilliseconds(200), NoEvents());           // initial repeat fires
        sut.Update(TimeSpan.FromMilliseconds(100), NoEvents());           // 100ms >= 50ms subsequent repeat

        sut.Btnp((int)PicoButton.Left, 0).Should().BeTrue();
    }

    [Fact]
    public void Btnp_ReturnsTrue_ForSubFrameTap()
    {
        // KeyDown + KeyUp in same frame — _pressedThisFrame is true even though _heldNow is false
        var sut = CreateSut();
        sut.Update(TimeSpan.FromMilliseconds(16), [KeyDown(Keys.Left, 0UL), KeyUp(Keys.Left, 1UL)]);

        sut.Btnp((int)PicoButton.Left, 0).Should().BeTrue();
    }

    [Fact]
    public void Btnp_ReturnsFalse_OnSubsequentFrame_ForSubFrameTap()
    {
        var sut = CreateSut();
        sut.Update(TimeSpan.FromMilliseconds(16), [KeyDown(Keys.Left, 0UL), KeyUp(Keys.Left, 1UL)]);
        sut.Update(TimeSpan.FromMilliseconds(16), NoEvents()); // _pressedThisFrame cleared

        sut.Btnp((int)PicoButton.Left, 0).Should().BeFalse();
    }

    [Fact]
    public void Btnp_Pause_ReturnsFalse_WhenHeld()
    {
        // Pause has no autorepeat — only the first-press frame fires Btnp
        var config = new BtnpConfig(InitialRepeatMs: 100.0, SubsequentRepeatMs: 50.0);
        var sut = CreateSut(config: config);
        sut.Update(TimeSpan.FromMilliseconds(16), [KeyDown(Keys.Escape)]); // first frame fires
        sut.Update(TimeSpan.FromMilliseconds(200), NoEvents());             // held for 200ms > InitialRepeatMs

        sut.Btnp((int)PicoButton.Pause, 0).Should().BeFalse();
    }

    [Fact]
    public void Btnp_Pause_ReturnsTrue_ForSubFrameTap()
    {
        var sut = CreateSut();
        sut.Update(TimeSpan.FromMilliseconds(16), [KeyDown(Keys.Escape, 0UL), KeyUp(Keys.Escape, 1UL)]);

        sut.Btnp((int)PicoButton.Pause, 0).Should().BeTrue();
    }

    // --------------------------------------------------------------------------
    #endregion
    #region Debounce
    // --------------------------------------------------------------------------

    [Fact]
    public void Btn_ReturnsTrue_WhenKeyUpWithinDebounceWindowIsDiscarded()
    {
        // KeyDown at t=1s accepted; KeyUp at t=1s+10ms is within 50ms window → discarded → button stays held
        var config = new BtnpConfig(DebounceMs: 50.0);
        var sut = CreateSut(config: config);
        var events = new List<InputEvent>
        {
            KeyDown(Keys.Left, 1_000_000_000UL),  // t=1s — accepted (1s-0 >> 50ms window)
            KeyUp(Keys.Left,   1_010_000_000UL),  // t=1s+10ms — discarded (10ms < 50ms window)
        };
        sut.Update(TimeSpan.FromMilliseconds(16), events);

        sut.Btn((int)PicoButton.Left, 0).Should().BeTrue();
    }

    [Fact]
    public void Btn_ReturnsFalse_WhenKeyUpIsOutsideDebounceWindow()
    {
        // KeyDown at t=1s accepted; KeyUp at t=1s+60ms is outside 50ms window → accepted → button released
        var config = new BtnpConfig(DebounceMs: 50.0);
        var sut = CreateSut(config: config);
        var events = new List<InputEvent>
        {
            KeyDown(Keys.Left, 1_000_000_000UL),  // t=1s — accepted
            KeyUp(Keys.Left,   1_060_000_000UL),  // t=1s+60ms — accepted (60ms > 50ms window)
        };
        sut.Update(TimeSpan.FromMilliseconds(16), events);

        sut.Btn((int)PicoButton.Left, 0).Should().BeFalse();
    }

    [Fact]
    public void Btnp_ReturnsTrue_WhenDebounceIsZero_AndEventsHaveSameTimestamp()
    {
        // DebounceMs=0 means no debounce; duplicate KeyDown at same timestamp is harmless
        var config = new BtnpConfig(DebounceMs: 0.0);
        var sut = CreateSut(config: config);
        var events = new List<InputEvent>
        {
            KeyDown(Keys.Left, 1_000_000_000UL),
            KeyDown(Keys.Left, 1_000_000_000UL), // duplicate — redundant but must not crash
        };

        var act = () => sut.Update(TimeSpan.FromMilliseconds(16), events);
        act.Should().NotThrow();

        sut.Btnp((int)PicoButton.Left, 0).Should().BeTrue();
    }

    // --------------------------------------------------------------------------
    #endregion
    #region SetBindings state clearing
    // --------------------------------------------------------------------------

    [Fact]
    public void SetBindings_ClearsHeldState()
    {
        var sut = CreateSut();
        sut.Update(TimeSpan.FromMilliseconds(16), [KeyDown(Keys.Left)]);
        sut.SetBindings(InputBindings.Default);

        sut.Btn((int)PicoButton.Left, 0).Should().BeFalse();
    }

    [Fact]
    public void SetBindings_NewBindingsAreUsed()
    {
        var initialBindings = BindOnly(PicoButton.Left, new KeyboardSource(Keys.A));
        var sut = CreateSut(bindings: initialBindings);
        sut.Update(TimeSpan.FromMilliseconds(16), [KeyDown(Keys.A)]);
        sut.Btn((int)PicoButton.Left, 0).Should().BeTrue(); // press registered under old binding

        var newBindings = BindOnly(PicoButton.Left, new KeyboardSource(Keys.Z));
        sut.SetBindings(newBindings);
        sut.Update(TimeSpan.FromMilliseconds(16), [KeyDown(Keys.A)]); // old key no longer bound

        sut.Btn((int)PicoButton.Left, 0).Should().BeFalse();
    }

    // --------------------------------------------------------------------------
    #endregion
}
