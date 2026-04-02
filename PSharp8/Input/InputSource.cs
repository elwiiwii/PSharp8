using System.Text.Json.Serialization;
using Microsoft.Xna.Framework.Input;

namespace PSharp8.Input;

[JsonDerivedType(typeof(KeyboardSource), typeDiscriminator: "keyboard")]
[JsonDerivedType(typeof(MouseSource), typeDiscriminator: "mouse")]
[JsonDerivedType(typeof(GamePadSource), typeDiscriminator: "gamepad")]
public abstract record InputSource;

public sealed record KeyboardSource(Keys Key) : InputSource;

public sealed record MouseSource(MouseButton Button) : InputSource;

public sealed record GamePadSource(Buttons Button) : InputSource;
