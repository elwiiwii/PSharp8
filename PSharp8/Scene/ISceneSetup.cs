namespace PSharp8.Scene;

public interface ISceneSetup
{
    (int Width, int Height) Resolution { get; set; }
    IFunctionHandle RegisterUpdate(Action callback, double fps,
        PauseBehavior pauseBehavior = PauseBehavior.Pause);
    IFunctionHandle RegisterDraw(Action callback, double fps,
        PauseBehavior pauseBehavior = PauseBehavior.Pause);
}
