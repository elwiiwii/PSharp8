namespace PSharp8.Input;

/// <summary>
/// Supplies raw button-held state each frame to <see cref="InputManager"/>.
/// </summary>
/// <remarks>
/// Implementations are responsible for translating physical hardware state (keyboard,
/// gamepad, mouse) into a 7-element boolean array indexed by <see cref="PicoButton"/>.
/// This separation means <see cref="InputManager"/> has no knowledge of SDL, XNA
/// input classes, or binding resolution — it only sees an abstract held-state snapshot.
/// </remarks>
public interface IInputProvider
{
    /// <summary>
    /// Returns the current held state of all 7 Pico-8 buttons.
    /// Index <c>i</c> corresponds to <c>(PicoButton)i</c>.
    /// </summary>
    bool[] GetHeldButtons();

    /// <summary>
    /// Replaces the active input bindings used to resolve physical inputs to
    /// <see cref="PicoButton"/>s.
    /// </summary>
    void SetBindings(InputBindings bindings);
}
