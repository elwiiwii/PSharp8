namespace PSharp8.Input;

public record InputEvent(InputSource Source, bool IsDown, ulong TimestampNs)
{
    public InputSource Source { get; init; } = Source ?? throw new ArgumentNullException(nameof(Source));
}
