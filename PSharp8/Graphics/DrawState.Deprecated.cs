/*using Microsoft.Xna.Framework;

namespace PSharp8.Graphics;

public class DrawState
{
    private (int x, int y) _cursor = (0, 0);
    private (int x, int y) _camera = (0, 0);
    private (Color foreground, Color background) _pen = (Pico8.BasePalette.ElementAt(6).Key, Pico8.BasePalette.ElementAt(1).Key);
    private Font _font = Fonts.P8SCII;
//    private PrintSession? _printSession = null;
//    
//    private sealed class PrintSession(
//        string text,
//        int startX,
//        int startY)
//    {
//        public readonly string Text = text;
//        public int CurrentIndex = 0;
//
//        //public int RepeatCount = 0;
//        //public int SkipFrames = 0;
//        //public int DelayFrames = 0;
//        public (int x, int y) HomePos = (startX, startY);
//        //public int? WrapBoundary = null;
//        //public int TabWidth = 4;
//        //public bool IsUnderlined = false;
//        public int HorScale = 1;
//        public int VertScale = 1;
//        //public bool IsStripy = false;
//        //public bool IsInverted = false;
//        //public bool HasBorder = true;
//        //public bool DrawSolidBackground = false;
//        //public byte Outline = 0;
//    }

    public (int x, int y) Cursor => _cursor;
    public (int x, int y) Camera => _camera;
    public (Color foreground, Color background) Pen => _pen;

    public void SetCamera(int x, int y)
    {
        _camera = (x, y);
    }
}*/