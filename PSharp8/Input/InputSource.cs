using Microsoft.Xna.Framework.Input;

namespace PSharp8.Input;

public abstract record InputSource;

public sealed record KeyboardSource(Keys Key) : InputSource;

public sealed record MouseSource(MouseButton Button) : InputSource;

public sealed record GamePadSource(Buttons Button) : InputSource;
