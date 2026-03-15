using FixMath;
using Microsoft.Xna.Framework;

namespace PSharp8.Graphics;

public class DrawState
{
    private (double x, double y) _cursor = (0, 0);
    private (int x, int y) _camera = (0, 0);
    private (Color foreground, Color background) _pen = (Pico8.Palette.ElementAt(6).Key, Pico8.Palette.ElementAt(1).Key);
    private PrintState _printState = new();
}

public class PrintState
{
    private bool _isTerminated = false;
    private int _repeatCount = 0;
    private int _skipFrames = 0;
    private int _delayFrames = 0;
    private (int x, int y) _homePos = (0, 0);
    private int? _wrapBoundary = null;
    private int _tabWidth = 4;
    private bool _isUnderlined = false;
    private bool _isWide = false;
    private bool _isTall = false;
    private bool _isStripy = false;
    private bool _isInverted = false;
    private bool _hasBorder = true;
    private bool _drawSolidBackground = false;
    private byte _outline = 0;
}