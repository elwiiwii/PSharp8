using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace PSharp8.Input;

[JsonConverter(typeof(InputBindingsJsonConverter))]
public class InputBindings
{
    private readonly IReadOnlyDictionary<PicoButton, IReadOnlyList<InputSource>> _bindings;

    public InputBindings(IReadOnlyDictionary<PicoButton, IReadOnlyList<InputSource>> bindings)
    {
        _bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
    }

    public IReadOnlyList<InputSource> this[PicoButton button] =>
        _bindings.TryGetValue(button, out var list) ? list : [];

    public static InputBindings Default { get; } = CreateDefault();

    private static InputBindings CreateDefault()
    {
        var dict = new Dictionary<PicoButton, IReadOnlyList<InputSource>>
        {
            [PicoButton.Left] = new List<InputSource>
            {
                new KeyboardSource(Keys.Left),
                new KeyboardSource(Keys.A),
                new GamePadSource(Buttons.DPadLeft),
                new GamePadSource(Buttons.LeftThumbstickLeft),
            },
            [PicoButton.Right] = new List<InputSource>
            {
                new KeyboardSource(Keys.Right),
                new KeyboardSource(Keys.D),
                new GamePadSource(Buttons.DPadRight),
                new GamePadSource(Buttons.LeftThumbstickRight),
            },
            [PicoButton.Up] = new List<InputSource>
            {
                new KeyboardSource(Keys.Up),
                new KeyboardSource(Keys.W),
                new GamePadSource(Buttons.DPadUp),
                new GamePadSource(Buttons.LeftThumbstickUp),
            },
            [PicoButton.Down] = new List<InputSource>
            {
                new KeyboardSource(Keys.Down),
                new KeyboardSource(Keys.S),
                new GamePadSource(Buttons.DPadDown),
                new GamePadSource(Buttons.LeftThumbstickDown),
            },
            [PicoButton.Primary] = new List<InputSource>
            {
                new KeyboardSource(Keys.Z),
                new KeyboardSource(Keys.C),
                new KeyboardSource(Keys.J),
                new KeyboardSource(Keys.NumPad1),
                new GamePadSource(Buttons.A),
                new GamePadSource(Buttons.Y),
                new GamePadSource(Buttons.RightShoulder),
            },
            [PicoButton.Secondary] = new List<InputSource>
            {
                new KeyboardSource(Keys.X),
                new KeyboardSource(Keys.V),
                new KeyboardSource(Keys.K),
                new KeyboardSource(Keys.NumPad2),
                new GamePadSource(Buttons.B),
                new GamePadSource(Buttons.X),
                new GamePadSource(Buttons.LeftShoulder),
            },
            [PicoButton.Pause] = new List<InputSource>
            {
                new KeyboardSource(Keys.Escape),
                new KeyboardSource(Keys.Enter),
                new KeyboardSource(Keys.P),
                new GamePadSource(Buttons.Start),
                new GamePadSource(Buttons.Back),
            },
        };

        return new InputBindings(dict);
    }
}
