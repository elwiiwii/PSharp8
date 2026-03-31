namespace PSharp8.Input;

internal interface IInputManager
{
    bool Btn(int button, int player);
    bool Btnp(int button, int player);
    bool InputBlocked { get; set; }
}
