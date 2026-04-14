namespace PSharp8.Input;

public interface IInputManager
{
    bool Btn(int button, int player);
    bool Btnp(int button, int player);
    bool InputBlocked { get; set; }
    void Update(TimeSpan elapsed);
}
