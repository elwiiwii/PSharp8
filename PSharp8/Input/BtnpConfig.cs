namespace PSharp8.Input;

public record BtnpConfig(double InitialRepeatMs = 250.0, double SubsequentRepeatMs = 67.0, double DebounceMs = 0.0)
{
    public double DebounceMs { get; init; } = DebounceMs >= 0.0
        ? DebounceMs
        : throw new ArgumentOutOfRangeException(nameof(DebounceMs));
}
