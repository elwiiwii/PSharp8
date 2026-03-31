namespace PSharp8.Scene;

public interface IFunctionHandle
{
    double Fps { get; set; }
    PauseBehavior PauseBehavior { get; set; }
    bool Enabled { get; set; }
}
