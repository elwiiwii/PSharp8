using FixMath;
using Microsoft.Xna.Framework;

namespace PSharp8;

public static class Pico8
{
    private static readonly AsyncLocal<GameOrchestrator?> _orchestrator = new();

    private static GameOrchestrator Orch =>
        _orchestrator.Value ?? throw new InvalidOperationException(
            "Pico8 static API has not been initialized. Call Pico8.Initialize(orchestrator) first.");

    public static void Initialize(GameOrchestrator orchestrator)
    {
        _orchestrator.Value = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    }

    public static Dictionary<Color, Color> BasePalette => new() {
        { new(0x00, 0x00, 0x00, 255), new(0x00, 0x00, 0x00, 0) }, // 00 black
        { new(0x1D, 0x2B, 0x53, 255), new(0x1D, 0x2B, 0x53, 255) }, // 01 dark-blue
        { new(0x7E, 0x25, 0x53, 255), new(0x7E, 0x25, 0x53, 255) }, // 02 dark-purple
        { new(0x00, 0x87, 0x51, 255), new(0x00, 0x87, 0x51, 255) }, // 03 dark-green
        { new(0xAB, 0x52, 0x36, 255), new(0xAB, 0x52, 0x36, 255) }, // 04 brown
        { new(0x5F, 0x57, 0x4F, 255), new(0x5F, 0x57, 0x4F, 255) }, // 05 dark-grey
        { new(0xC2, 0xC3, 0xC7, 255), new(0xC2, 0xC3, 0xC7, 255) }, // 06 light-grey
        { new(0xFF, 0xF1, 0xE8, 255), new(0xFF, 0xF1, 0xE8, 255) }, // 07 white
        { new(0xFF, 0x00, 0x4D, 255), new(0xFF, 0x00, 0x4D, 255) }, // 08 red
        { new(0xFF, 0xA3, 0x00, 255), new(0xFF, 0xA3, 0x00, 255) }, // 09 orange
        { new(0xFF, 0xEC, 0x27, 255), new(0xFF, 0xEC, 0x27, 255) }, // 10 yellow
        { new(0x00, 0xE4, 0x36, 255), new(0x00, 0xE4, 0x36, 255) }, // 11 green
        { new(0x29, 0xAD, 0xFF, 255), new(0x29, 0xAD, 0xFF, 255) }, // 12 blue
        { new(0x83, 0x76, 0x9C, 255), new(0x83, 0x76, 0x9C, 255) }, // 13 lavender
        { new(0xFF, 0x77, 0xA8, 255), new(0xFF, 0x77, 0xA8, 255) }, // 14 pink
        { new(0xFF, 0xCC, 0xAA, 255), new(0xFF, 0xCC, 0xAA, 255) }, // 15 light-peach

        { new(0x29, 0x18, 0x14, 255), new(0x29, 0x18, 0x14, 255) }, // 16 brownish-black
        { new(0x11, 0x1D, 0x35, 255), new(0x11, 0x1D, 0x35, 255) }, // 17 darker-blue
        { new(0x42, 0x21, 0x36, 255), new(0x42, 0x21, 0x36, 255) }, // 18 darker-purple
        { new(0x12, 0x53, 0x59, 255), new(0x12, 0x53, 0x59, 255) }, // 19 blue-green
        { new(0x74, 0x2F, 0x29, 255), new(0x74, 0x2F, 0x29, 255) }, // 20 dark-brown
        { new(0x49, 0x33, 0x3B, 255), new(0x49, 0x33, 0x3B, 255) }, // 21 darker-grey
        { new(0xA2, 0x88, 0x79, 255), new(0xA2, 0x88, 0x79, 255) }, // 22 medium-grey
        { new(0xF3, 0xEF, 0x7D, 255), new(0xF3, 0xEF, 0x7D, 255) }, // 23 light-yellow
        { new(0xBE, 0x12, 0x50, 255), new(0xBE, 0x12, 0x50, 255) }, // 24 dark-red
        { new(0xFF, 0x6C, 0x24, 255), new(0xFF, 0x6C, 0x24, 255) }, // 25 dark-orange
        { new(0xA8, 0xE7, 0x2E, 255), new(0xA8, 0xE7, 0x2E, 255) }, // 26 lime-green
        { new(0x00, 0xB5, 0x43, 255), new(0x00, 0xB5, 0x43, 255) }, // 27 medium-green
        { new(0x06, 0x5A, 0xB5, 255), new(0x06, 0x5A, 0xB5, 255) }, // 28 true-blue
        { new(0x75, 0x46, 0x65, 255), new(0x75, 0x46, 0x65, 255) }, // 29 mauve
        { new(0xFF, 0x6E, 0x59, 255), new(0xFF, 0x6E, 0x59, 255) }, // 30 dark-peach
        { new(0xFF, 0x9D, 0x81, 255), new(0xFF, 0x9D, 0x81, 255) }, // 31 peach
    };

    #region INPUT API

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Btn
    /// </summary>
    public static bool Btn(int button, int player = 0)
        => Orch.InputManager.Btn(button, player);

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Btnp
    /// </summary>
    public static bool Btnp(int button, int player = 0)
        => Orch.InputManager.Btnp(button, player);

    #endregion

    #region GRAPHICS API

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Camera
    /// </summary>
    public static void Camera()
        => Orch.GraphicsManager.Camera(0, 0);

    public static void Camera(double x, double y)
        => Orch.GraphicsManager.Camera((int)x, (int)y);

    public static void Camera(F32 x, F32 y)
        => Orch.GraphicsManager.Camera(F32.FloorToInt(x), F32.FloorToInt(y));

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Circ
    /// </summary>
    public static void Circ(double x, double y, double radius, double color)
        => Orch.GraphicsManager.Circ((int)x, (int)y, (int)radius, BasePalette.ElementAt((int)color).Key);

    public static void Circ(double x, double y, double radius, Color color)
        => Orch.GraphicsManager.Circ((int)x, (int)y, (int)radius, color);

    public static void Circ(F32 x, F32 y, F32 radius, F32 color)
        => Orch.GraphicsManager.Circ(F32.FloorToInt(x), F32.FloorToInt(y),
                F32.FloorToInt(radius), BasePalette.ElementAt(F32.FloorToInt(color)).Key);

    public static void Circ(F32 x, F32 y, F32 radius, Color color)
        => Orch.GraphicsManager.Circ(F32.FloorToInt(x), F32.FloorToInt(y),
                F32.FloorToInt(radius), color);

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Circfill
    /// </summary>
    public static void Circfill(double x, double y, double radius, double color)
        => Orch.GraphicsManager.Circfill((int)x, (int)y, (int)radius, BasePalette.ElementAt((int)color).Key);

    public static void Circfill(double x, double y, double radius, Color color)
        => Orch.GraphicsManager.Circfill((int)x, (int)y, (int)radius, color);

    public static void Circfill(F32 x, F32 y, F32 radius, F32 color)
        => Orch.GraphicsManager.Circfill(F32.FloorToInt(x), F32.FloorToInt(y),
                F32.FloorToInt(radius), BasePalette.ElementAt(F32.FloorToInt(color)).Key);

    public static void Circfill(F32 x, F32 y, F32 radius, Color color)
        => Orch.GraphicsManager.Circfill(F32.FloorToInt(x), F32.FloorToInt(y),
                F32.FloorToInt(radius), color);

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Cls
    /// </summary>
    public static void Cls(double color = 0)
        => Orch.GraphicsManager.Cls(BasePalette.ElementAt((int)color).Key);

    public static void Cls(Color color)
        => Orch.GraphicsManager.Cls(color);

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Line
    /// </summary>
    public static void Line(double xStart, double yStart, double xEnd, double yEnd, double color)
        => Orch.GraphicsManager.Line((int)xStart, (int)yStart, (int)xEnd, (int)yEnd,
                BasePalette.ElementAt((int)color).Key);

    public static void Line(double xStart, double yStart, double xEnd, double yEnd, Color color)
        => Orch.GraphicsManager.Line((int)xStart, (int)yStart, (int)xEnd, (int)yEnd, color);

    public static void Line(F32 xStart, F32 yStart, F32 xEnd, F32 yEnd, F32 color)
        => Orch.GraphicsManager.Line(F32.FloorToInt(xStart), F32.FloorToInt(yStart),
                F32.FloorToInt(xEnd), F32.FloorToInt(yEnd), BasePalette.ElementAt(F32.FloorToInt(color)).Key);

    public static void Line(F32 xStart, F32 yStart, F32 xEnd, F32 yEnd, Color color)
        => Orch.GraphicsManager.Line(F32.FloorToInt(xStart), F32.FloorToInt(yStart),
                F32.FloorToInt(xEnd), F32.FloorToInt(yEnd), color);

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Map
    /// </summary>
    public static void Map(double cellX, double cellY, double screenX, double screenY,
            double cellWidth, double cellHeight, int flags = 0)
        => Orch.GraphicsManager.Map((int)cellX, (int)cellY, (int)screenX, (int)screenY,
                (int)cellWidth, (int)cellHeight, flags);
    
    public static void Map(F32 cellX, F32 cellY, F32 screenX, F32 screenY,
            F32 cellWidth, F32 cellHeight, int flags = 0)
        => Orch.GraphicsManager.Map(F32.FloorToInt(cellX), F32.FloorToInt(cellY), F32.FloorToInt(screenX),
                F32.FloorToInt(screenY), F32.FloorToInt(cellWidth), F32.FloorToInt(cellHeight), flags);
    
    /// <summary>
    /// https://pico-8.fandom.com/wiki/Pal
    /// </summary>
    public static void Pal()
        => Orch.GraphicsManager.Pal();

    public static void Pal(double index, double value)
        => Orch.GraphicsManager.Pal(BasePalette.ElementAt((int)index).Key,
                BasePalette.ElementAt((int)value).Key);

    public static void Pal(double index, Color color)
        => Orch.GraphicsManager.Pal(BasePalette.ElementAt((int)index).Key, color);

    public static void Pal(F32 index, F32 color)
        => Orch.GraphicsManager.Pal(BasePalette.ElementAt(F32.FloorToInt(index)).Key,
                BasePalette.ElementAt(F32.FloorToInt(color)).Key);

    public static void Pal(F32 index, Color color)
        => Orch.GraphicsManager.Pal(BasePalette.ElementAt(F32.FloorToInt(index)).Key, color);

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Palt
    /// </summary>
    public static void Palt()
        => Orch.GraphicsManager.Palt();

    public static void Palt(double index, bool isTransparent)
        => Orch.GraphicsManager.Palt(BasePalette.ElementAt((int)index).Key, isTransparent ? 255 : 0);

    public static void Palt(double index, double opacity)
        => Orch.GraphicsManager.Palt(BasePalette.ElementAt((int)index).Key, (int)opacity);

    public static void Palt(F32 index, bool isTransparent)
        => Orch.GraphicsManager.Palt(BasePalette.ElementAt(F32.FloorToInt(index)).Key, isTransparent ? 255 : 0);

    public static void Palt(F32 index, F32 opacity)
        => Orch.GraphicsManager.Palt(BasePalette.ElementAt(F32.FloorToInt(index)).Key, F32.FloorToInt(opacity));

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Print
    /// </summary>
    public static void Print(string text, double x, double y, double color, Font? font = null)
        => Orch.GraphicsManager.Print(text, (int)x, (int)y, BasePalette.ElementAt((int)color).Key, font ?? Fonts.P8SCII);

    public static void Print(string text, double x, double y, Color color, Font? font = null)
        => Orch.GraphicsManager.Print(text, (int)x, (int)y, color, font ?? Fonts.P8SCII);

    public static void Print(string text, F32 x, F32 y, F32 color, Font? font = null)
        => Orch.GraphicsManager.Print(text, F32.FloorToInt(x), F32.FloorToInt(y),
                BasePalette.ElementAt(F32.FloorToInt(color)).Key, font ?? Fonts.P8SCII);

    public static void Print(string text, F32 x, F32 y, Color color, Font? font = null)
        => Orch.GraphicsManager.Print(text, F32.FloorToInt(x), F32.FloorToInt(y), color, font ?? Fonts.P8SCII);

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Pset
    /// </summary>
    public static void Pset(double x, double y, double color)
        => Orch.GraphicsManager.Pset((int)x, (int)y, BasePalette.ElementAt((int)color).Key);

    public static void Pset(double x, double y, Color color)
        => Orch.GraphicsManager.Pset((int)x, (int)y, color);

    public static void Pset(F32 x, F32 y, F32 color)
        => Orch.GraphicsManager.Pset(F32.FloorToInt(x), F32.FloorToInt(y),
                BasePalette.ElementAt(F32.FloorToInt(color)).Key);

    public static void Pset(F32 x, F32 y, Color color)
        => Orch.GraphicsManager.Pset(F32.FloorToInt(x), F32.FloorToInt(y), color);

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Rect
    /// </summary>
    public static void Rect(double xLeft, double yTop, double xRight, double yBottom, double color)
        => Orch.GraphicsManager.Rect((int)xLeft, (int)yTop, (int)xRight, (int)yBottom,
                BasePalette.ElementAt((int)color).Key);

    public static void Rect(double xLeft, double yTop, double xRight, double yBottom, Color color)
        => Orch.GraphicsManager.Rect((int)xLeft, (int)yTop, (int)xRight, (int)yBottom, color);

    public static void Rect(F32 xLeft, F32 yTop, F32 xRight, F32 yBottom, F32 color)
        => Orch.GraphicsManager.Rect(F32.FloorToInt(xLeft), F32.FloorToInt(yTop),
                F32.FloorToInt(xRight), F32.FloorToInt(yBottom), BasePalette.ElementAt(F32.FloorToInt(color)).Key);

    public static void Rect(F32 xLeft, F32 yTop, F32 xRight, F32 yBottom, Color color)
        => Orch.GraphicsManager.Rect(F32.FloorToInt(xLeft), F32.FloorToInt(yTop),
                F32.FloorToInt(xRight), F32.FloorToInt(yBottom), color);

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Rectfill
    /// </summary>
    public static void Rectfill(double xLeft, double yTop, double xRight, double yBottom, double color)
        => Orch.GraphicsManager.Rectfill((int)xLeft, (int)yTop, (int)xRight, (int)yBottom,
                BasePalette.ElementAt((int)color).Key);

    public static void Rectfill(double xLeft, double yTop, double xRight, double yBottom, Color color)
        => Orch.GraphicsManager.Rectfill((int)xLeft, (int)yTop, (int)xRight, (int)yBottom, color);

    public static void Rectfill(F32 xLeft, F32 yTop, F32 xRight, F32 yBottom, F32 color)
        => Orch.GraphicsManager.Rectfill(F32.FloorToInt(xLeft), F32.FloorToInt(yTop),
                F32.FloorToInt(xRight), F32.FloorToInt(yBottom), BasePalette.ElementAt(F32.FloorToInt(color)).Key);

    public static void Rectfill(F32 xLeft, F32 yTop, F32 xRight, F32 yBottom, Color color)
        => Orch.GraphicsManager.Rectfill(F32.FloorToInt(xLeft), F32.FloorToInt(yTop),
                F32.FloorToInt(xRight), F32.FloorToInt(yBottom), color);
    
    /// <summary>
    /// https://www.lexaloffle.com/bbs/?tid=150992
    /// </summary>
    public static void Rrect(double xLeft, double yTop, double xRight, double yBottom, double radius, double color)
        => Orch.GraphicsManager.Rrect((int)xLeft, (int)yTop, (int)xRight, (int)yBottom,
                (int)radius, BasePalette.ElementAt((int)color).Key);

    public static void Rrect(double xLeft, double yTop, double xRight, double yBottom, double radius, Color color)
        => Orch.GraphicsManager.Rrect((int)xLeft, (int)yTop, (int)xRight, (int)yBottom, (int)radius, color);

    public static void Rrect(F32 xLeft, F32 yTop, F32 xRight, F32 yBottom, F32 radius, F32 color)
        => Orch.GraphicsManager.Rrect(F32.FloorToInt(xLeft), F32.FloorToInt(yTop),
                F32.FloorToInt(xRight), F32.FloorToInt(yBottom), F32.FloorToInt(radius),
                BasePalette.ElementAt(F32.FloorToInt(color)).Key);

    public static void Rrect(F32 xLeft, F32 yTop, F32 xRight, F32 yBottom, F32 radius, Color color)
        => Orch.GraphicsManager.Rrect(F32.FloorToInt(xLeft), F32.FloorToInt(yTop),
                F32.FloorToInt(xRight), F32.FloorToInt(yBottom), F32.FloorToInt(radius), color);

    /// <summary>
    /// https://www.lexaloffle.com/bbs/?tid=150992
    /// </summary>
    public static void Rrectfill(double xLeft, double yTop, double xRight, double yBottom, double radius, double color)
        => Orch.GraphicsManager.Rrectfill((int)xLeft, (int)yTop, (int)xRight, (int)yBottom,
                (int)radius, BasePalette.ElementAt((int)color).Key);

    public static void Rrectfill(double xLeft, double yTop, double xRight, double yBottom, double radius, Color color)
        => Orch.GraphicsManager.Rrectfill((int)xLeft, (int)yTop, (int)xRight, (int)yBottom, (int)radius, color);

    public static void Rrectfill(F32 xLeft, F32 yTop, F32 xRight, F32 yBottom, F32 radius, F32 color)
        => Orch.GraphicsManager.Rrectfill(F32.FloorToInt(xLeft), F32.FloorToInt(yTop),
                F32.FloorToInt(xRight), F32.FloorToInt(yBottom), F32.FloorToInt(radius),
                BasePalette.ElementAt(F32.FloorToInt(color)).Key);

    public static void Rrectfill(F32 xLeft, F32 yTop, F32 xRight, F32 yBottom, F32 radius, Color color)
        => Orch.GraphicsManager.Rrectfill(F32.FloorToInt(xLeft), F32.FloorToInt(yTop),
                F32.FloorToInt(xRight), F32.FloorToInt(yBottom), F32.FloorToInt(radius), color);
    
    /// <summary>
    /// https://pico-8.fandom.com/wiki/Spr
    /// </summary>
    public static void Spr(double index, double x = 0, double y = 0, double width = 1.0, double height = 1.0,
            bool flip_x = false, bool flip_y = false)
        => Orch.GraphicsManager.Spr((int)index, (int)x, (int)y, (int)width, (int)height, flip_x, flip_y);

    public static void Spr(F32 index, F32 x, F32 y, F32 width, F32 height, bool flipX = false, bool flipY = false)
        => Orch.GraphicsManager.Spr(F32.FloorToInt(index), F32.FloorToInt(x), F32.FloorToInt(y),
                F32.FloorToInt(width), F32.FloorToInt(height), flipX, flipY);
    
    /// <summary>
    /// https://pico-8.fandom.com/wiki/Sspr
    /// </summary>
    public static void Sspr(double sourceX, double sourceY, double sourceWidth, double sourceHeight,
            double destX, double destY, double destWidth, double destHeight, bool flipX = false, bool flipY = false)
        => Orch.GraphicsManager.Sspr((int)sourceX, (int)sourceY, (int)sourceWidth, (int)sourceHeight,
                (int)destX, (int)destY, (int)destWidth, (int)destHeight, flipX, flipY);

    public static void Sspr(F32 sourceX, F32 sourceY, F32 sourceWidth, F32 sourceHeight, F32 destX, F32 destY,
            F32 destWidth, F32 destHeight, bool flipX = false, bool flipY = false)
        => Orch.GraphicsManager.Sspr(F32.FloorToInt(sourceX), F32.FloorToInt(sourceY), F32.FloorToInt(sourceWidth),
                F32.FloorToInt(sourceHeight), F32.FloorToInt(destX), F32.FloorToInt(destY),
                F32.FloorToInt(destWidth), F32.FloorToInt(destHeight), flipX, flipY);

    #endregion

    #region SCENE MANAGEMENT API

    /// <summary>
    /// Schedule a scene transition (PICO-8 load equivalent)
    /// </summary>
    public static void ScheduleScene(Func<IScene> sceneFactory)
        => Orch.SceneManager.ScheduleScene(sceneFactory);

    #endregion

    #region MEMORY API

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Cartdata
    /// </summary>
    public static void CartData(string id)
        => Orch.MemoryManager.CartData(id);
    

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Cstore
    /// </summary>
    public static void Cstore()
        => Orch.MemoryManager.Cstore();

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Dget
    /// </summary>
    public static F32 Dget(int index)
        => Orch.MemoryManager.Dget(index);

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Dset
    /// </summary>
    public static void Dset(int index, double value)
        => Orch.MemoryManager.Dset(index, value);

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Fget
    /// </summary>
    public static int Fget(int n)
        => Orch.SfmManager.GetFlag(n);

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Load
    /// </summary>
    public static void Load(string fileName)
        => Orch.MemoryManager.Load(fileName);

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Menuitem
    /// </summary>
    public static void Menuitem(int index, Func<string> getName, Action? callback = null)
        => Orch.MemoryManager.Menuitem(index, getName, callback);

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Mget
    /// </summary>
    public static int Mget(int celx, int cely)
        => Orch.SfmManager.GetMapTile(celx, cely);

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Mset
    /// </summary>
    public static void Mset(int celx, int cely, int snum = 0)
        => Orch.SfmManager.SetMapTile(celx, cely, snum);

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Reload
    /// </summary>
    public static void Reload()
        => Orch.SfmManager.Reload();

    #endregion

    #region AUDIO API

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Music
    /// </summary>
    public static void Music(double n, double fadeMs = 0)
        => Orch.AudioManager.Music((int)n, (int)fadeMs);

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Sfx
    /// </summary>
    public static void Sfx(double n)
        => Orch.AudioManager.Sfx((int)n);

    #endregion

    #region MATH API

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Abs
    /// </summary>
    public static F32 Abs(double value)
        => Orch.MathManager.Abs(F32.FromDouble(value));

    public static F32 Abs(F32 value)
    {
        return Orch.MathManager.Abs(value);
    }

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Ceil
    /// </summary>
    public static F32 Ceil(double value)
        => Orch.MathManager.Ceil(F32.FromDouble(value));

    public static F32 Ceil(F32 value)
    {
        return Orch.MathManager.Ceil(value);
    }

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Cos
    /// </summary>
    public static F32 Cos(double angle)
        => Orch.MathManager.Cos(F32.FromDouble(angle));

    public static F32 Cos(F32 angle)
        => Orch.MathManager.Cos(angle);

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Flr
    /// </summary>
    public static F32 Flr(double value)
        => Orch.MathManager.Flr(F32.FromDouble(value));

    public static F32 Flr(F32 value)
        => Orch.MathManager.Flr(value);

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Max
    /// </summary>
    public static F32 Max(double first, double second = 0)
        => Orch.MathManager.Max(F32.FromDouble(first), F32.FromDouble(second));

    public static F32 Max(F32 first, F32 second)
    {
        return Orch.MathManager.Max(first, second);
    }

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Mid
    /// </summary>
    public static F32 Mid(double first, double second, double third)
        => Orch.MathManager.Mid(F32.FromDouble(first), F32.FromDouble(second), F32.FromDouble(third));

    public static F32 Mid(F32 first, F32 second, F32 third)
        => Orch.MathManager.Mid(first, second, third);

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Min
    /// </summary>
    public static F32 Min(double first, double second = 0)
        => Orch.MathManager.Min(F32.FromDouble(first), F32.FromDouble(second));

    public static F32 Min(F32 first, F32 second)
        => Orch.MathManager.Min(first, second);

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Lua
    /// </summary>
    public static F32 Mod(double first, double second)
        => Orch.MathManager.Mod(F32.FromDouble(first), F32.FromDouble(second));

    public static F32 Mod(F32 first, F32 second)
        => Orch.MathManager.Mod(first, second);

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Rnd
    /// </summary>
    public static F32 Rnd()
        => Orch.MathManager.Rnd(F32.One, null);

    public static F32 Rnd(double max, Random? random = null)
        => Orch.MathManager.Rnd(F32.FromDouble(max), random);

    public static F32 Rnd(F32 max, Random? random = null)
        => Orch.MathManager.Rnd(max, random);

    public static F32 Rnd(double max, object reference)
        => Orch.MathManager.Rnd(F32.FromDouble(max), reference);

    public static F32 Rnd(F32 max, object reference)
        => Orch.MathManager.Rnd(max, reference);

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Sgn
    /// </summary>
    public static F32 Sgn(double value)
        => Orch.MathManager.Sgn(F32.FromDouble(value));

    public static F32 Sgn(F32 value)
        => Orch.MathManager.Sgn(value);

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Sin
    /// </summary>
    public static F32 Sin(double angle)
        => Orch.MathManager.Sin(F32.FromDouble(angle));

    public static F32 Sin(F32 angle)
        => Orch.MathManager.Sin(angle);

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Srand
    /// </summary>
    public static void Srand(double seed, Random? random = null)
        => Orch.MathManager.Srand(F32.FromDouble(seed), random);

    public static void Srand(F32 seed, Random? random = null)
        => Orch.MathManager.Srand(seed, random);

    public static void Srand(double seed, object reference)
        => Orch.MathManager.Srand(F32.FromDouble(seed), reference);

    public static void Srand(F32 seed, object reference)
        => Orch.MathManager.Srand(seed, reference);

    #endregion

    #region API EXTENSIONS

    /// <summary>
    /// DrawTexture
    /// </summary>
    public static void DrawTexture(string textureName, double x, double y, double color,
            double scaleX = 1, double scaleY = 1, bool flipX = false, bool flipY = false)
        => Orch.GraphicsManager.DrawTexture(textureName, x, y, BasePalette.ElementAt((int)color).Key, scaleX, scaleY, flipX, flipY);
    
    public static void DrawTexture(string textureName, double x, double y, Color color,
            double scaleX = 1, double scaleY = 1, bool flipX = false, bool flipY = false)
        => Orch.GraphicsManager.DrawTexture(textureName, x, y, color, scaleX, scaleY, flipX, flipY);
    
    public static void DrawTexture(string textureName, double x, double y, Rectangle sourceRect,
            double color, double scaleX = 1, double scaleY = 1, bool flipX = false, bool flipY = false)
        => Orch.GraphicsManager.DrawTexture(textureName, x, y, BasePalette.ElementAt((int)color).Key, scaleX, scaleY, flipX, flipY);
    
    public static void DrawTexture(string textureName, double x, double y, Rectangle sourceRect,
            Color color, double scaleX = 1, double scaleY = 1, bool flipX = false, bool flipY = false)
        => Orch.GraphicsManager.DrawTexture(textureName, x, y, color, scaleX, scaleY, flipX, flipY);
    
    /// <summary>
    /// DrawLine
    /// </summary>
    public static void DrawLine(Vector2 start, Vector2 end, double color, double thickness)
        => Orch.GraphicsManager.DrawLine(start, end, BasePalette.ElementAt((int)color).Key, thickness);
    
    public static void DrawLine(Vector2 start, Vector2 end, Color color, double thickness)
        => Orch.GraphicsManager.DrawLine(start, end, color, thickness);
    
    /// <summary>
    /// DrawCirc
    /// </summary>
    public static void DrawCirc(Vector2 center, double radius, double color, double thickness, int segments = 32)
        => Orch.GraphicsManager.DrawCirc(center, radius, BasePalette.ElementAt((int)color).Key, thickness, segments);
    
    public static void DrawCirc(Vector2 center, double radius, Color color, double thickness, int segments = 32)
        => Orch.GraphicsManager.DrawCirc(center, radius, color, thickness, segments);
    
    /// <summary>
    /// DrawRect
    /// </summary>
    public static void DrawRect(Vector2 topLeft, double width, double height, double color, double thickness)
        => Orch.GraphicsManager.DrawRect(topLeft, width, height, BasePalette.ElementAt((int)color).Key, thickness);
    
    public static void DrawRect(Vector2 topLeft, double width, double height, Color color, double thickness)
        => Orch.GraphicsManager.DrawRect(topLeft, width, height, color, thickness);
        
    public static void DrawRect(Rectangle rect, double color, double thickness)
        => Orch.GraphicsManager.DrawRect(new(rect.X, rect.Y), rect.Width, rect.Height,
                BasePalette.ElementAt((int)color).Key, thickness);
    
    public static void DrawRect(Rectangle rect, Color color, double thickness)
        => Orch.GraphicsManager.DrawRect(new(rect.X, rect.Y), rect.Width, rect.Height, color, thickness);
        
    #endregion
}
