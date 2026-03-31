namespace PSharp8.Scene;

internal class FunctionRegistration : IFunctionHandle
{
    public double Fps { get; set; }
    public PauseBehavior PauseBehavior { get; set; }
    public bool Enabled { get; set; } = true;
    public required Action Callback { get; init; }
    public TimeSpan Accumulator { get; set; }
}
