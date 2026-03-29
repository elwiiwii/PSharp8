using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PSharp8.Graphics;

public class GraphicsManager
{
    private readonly SpriteBatch _batch;
    private (int x, int y) _camera = (0, 0);
    private readonly Func<(int W, int H)> _getSceneResolution;
    private readonly GraphicsDeviceManager _graphics;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly PaletteManager _paletteManager;
    private readonly Texture2D _pixel;
    private readonly SpriteTextureManager _spriteTextureManager;
    private readonly Dictionary<string, Texture2D> _textureDictionary;
    private readonly GameWindow _window;

    public GraphicsManager(
        SpriteBatch batch,
        Func<(int W, int H)> getSceneResolution,
        GraphicsDeviceManager graphics,
        GraphicsDevice graphicsDevice,
        PaletteManager paletteManager,
        Texture2D pixel,
        SpriteTextureManager spriteTextureManager,
        Dictionary<string, Texture2D> textureDictionary,
        GameWindow window)
    {
        _batch = batch ?? throw new ArgumentNullException(nameof(batch));
        _getSceneResolution = getSceneResolution ?? throw new ArgumentNullException(nameof(getSceneResolution));
        _graphics = graphics ?? throw new ArgumentNullException(nameof(graphics));
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _paletteManager = paletteManager ?? throw new ArgumentNullException(nameof(paletteManager));
        _pixel = pixel ?? throw new ArgumentNullException(nameof(pixel));
        _spriteTextureManager = spriteTextureManager ?? throw new ArgumentNullException(nameof(spriteTextureManager));
        _textureDictionary = textureDictionary ?? throw new ArgumentNullException(nameof(textureDictionary));
        _window = window ?? throw new ArgumentNullException(nameof(window));
    }

    #region DRAWING PRIMITIVES

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Circ
    /// </summary>
    public void Circ(int centerX, int centerY, int radius, Color color)
    {
        if (radius < 0) return;

        centerX -= _camera.x;
        centerY -= _camera.y;

        IterateMidpointArc(radius, (dx, dy) =>
        {
            DrawScaledPixel(centerX + dx, centerY + dy, color, 1, 1);
            DrawScaledPixel(centerX + dy, centerY + dx, color, 1, 1);
            DrawScaledPixel(centerX - dy, centerY + dx, color, 1, 1);
            DrawScaledPixel(centerX - dx, centerY + dy, color, 1, 1);
            DrawScaledPixel(centerX - dx, centerY - dy, color, 1, 1);
            DrawScaledPixel(centerX - dy, centerY - dx, color, 1, 1);
            DrawScaledPixel(centerX + dy, centerY - dx, color, 1, 1);
            DrawScaledPixel(centerX + dx, centerY - dy, color, 1, 1);
        });
    }

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Circfill
    /// </summary>
    public void Circfill(int centerX, int centerY, int radius, Color color)
    {
        if (radius < 0) return;

        centerX -= _camera.x;
        centerY -= _camera.y;

        IterateMidpointArc(radius, (dx, dy) =>
        {
            DrawScaledPixel(centerX - dx, centerY + dy, color, 2 * dx + 1, 1);
            DrawScaledPixel(centerX - dx, centerY - dy, color, 2 * dx + 1, 1);
            DrawScaledPixel(centerX - dy, centerY + dx, color, 2 * dy + 1, 1);
            DrawScaledPixel(centerX - dy, centerY - dx, color, 2 * dy + 1, 1);
        });
    }

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Cls
    /// </summary>
    public void Cls(Color color)
    {
        _graphicsDevice.Clear(color);
    }

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Line
    /// </summary>
    public void Line(int startX, int startY, int endX, int endY, Color color)
    {
        startX -= _camera.x;
        startY -= _camera.y;
        endX -= _camera.x;
        endY -= _camera.y;

        bool horiz = Math.Abs(endX - startX) >= Math.Abs(endY - startY);
        int dx = startX <= endX ? 1 : -1;
        int dy = startY <= endY ? 1 : -1;
        int x = startX;
        int y = startY;

        for (;;)
        {
            DrawScaledPixel(x, y, color, 1, 1);

            if (horiz)
            {
                if (x == endX) break;
                x += dx;
                y = (int)Math.Round(startY + (double)(endY - startY) * (x - startX) / (endX - startX),
                    MidpointRounding.AwayFromZero);
            }
            else
            {
                if (y == endY) break;
                y += dy;
                x = (int)Math.Round(startX + (double)(endX - startX) * (y - startY) / (endY - startY),
                    MidpointRounding.AwayFromZero);
            }
        }
    }

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Pset
    /// </summary>
    public void Pset(int x, int y, Color color)
    {
        x -= _camera.x;
        y -= _camera.y;

        DrawScaledPixel(x, y, color, 1, 1);
    }

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Rect
    /// </summary>
    public void Rect(int xLeft, int yTop, int xRight, int yBottom, Color color)
    {
        int width = xRight - xLeft + 1;
        int height = yBottom - yTop + 1;
        xLeft -= _camera.x;
        yTop -= _camera.y;

        DrawScaledPixel(xLeft, yTop, color, width, 1);
        DrawScaledPixel(xLeft, yTop + height - 1, color, width, 1);
        DrawScaledPixel(xLeft, yTop, color, 1, height);
        DrawScaledPixel(xLeft + width - 1, yTop, color, 1, height);
    }

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Rectfill
    /// </summary>
    public void Rectfill(int xLeft, int yTop, int xRight, int yBottom, Color color)
    {
        int width = xRight - xLeft + 1;
        int height = yBottom - yTop + 1;
        xLeft -= _camera.x;
        yTop -= _camera.y;

        DrawScaledPixel(xLeft, yTop, color, width, height);
    }

    /// <summary>
    /// https://www.lexaloffle.com/bbs/?tid=150992
    /// </summary>
    public void Rrect(int x, int y, int width, int height, int radius, Color color)
    {
        if (width <= 0 || height <= 0) return;

        x -= _camera.x;
        y -= _camera.y;

        int cornerRadius = Math.Clamp(radius, 0, Math.Min(width, height) / 2);

        // Straight edges (shortened by cornerRadius on each end)
        int straightEdgeWidth  = width  - 2 * cornerRadius;
        int straightEdgeHeight = height - 2 * cornerRadius;
        if (straightEdgeWidth > 0)
        {
            DrawScaledPixel(x + cornerRadius, y,                color, straightEdgeWidth, 1);  // Top
            DrawScaledPixel(x + cornerRadius, y + height - 1,   color, straightEdgeWidth, 1);  // Bottom
        }
        if (straightEdgeHeight > 0)
        {
            DrawScaledPixel(x,             y + cornerRadius, color, 1, straightEdgeHeight);  // Left
            DrawScaledPixel(x + width - 1, y + cornerRadius, color, 1, straightEdgeHeight);  // Right
        }

        // Corner arc centers (one quadrant per corner via midpoint algorithm)
        int topLeftX     = x + cornerRadius,         topLeftY     = y + cornerRadius;
        int topRightX    = x + width - 1 - cornerRadius, topRightY = y + cornerRadius;
        int bottomLeftX  = x + cornerRadius,         bottomLeftY  = y + height - 1 - cornerRadius;
        int bottomRightX = x + width - 1 - cornerRadius, bottomRightY = y + height - 1 - cornerRadius;

        IterateMidpointArc(cornerRadius, (dx, dy) =>
        {
            DrawScaledPixel(topLeftX     - dx, topLeftY     - dy, color);  // Top-left
            DrawScaledPixel(topLeftX     - dy, topLeftY     - dx, color);
            DrawScaledPixel(topRightX    + dx, topRightY    - dy, color);  // Top-right
            DrawScaledPixel(topRightX    + dy, topRightY    - dx, color);
            DrawScaledPixel(bottomLeftX  - dx, bottomLeftY  + dy, color);  // Bottom-left
            DrawScaledPixel(bottomLeftX  - dy, bottomLeftY  + dx, color);
            DrawScaledPixel(bottomRightX + dx, bottomRightY + dy, color);  // Bottom-right
            DrawScaledPixel(bottomRightX + dy, bottomRightY + dx, color);
        });
    }

    /// <summary>
    /// https://www.lexaloffle.com/bbs/?tid=150992
    /// </summary>
    public void Rrectfill(int x, int y, int width, int height, int radius, Color color)
    {
        if (width <= 0 || height <= 0) return;

        x -= _camera.x;
        y -= _camera.y;

        int cornerRadius = Math.Clamp(radius, 0, Math.Min(width, height) / 2);

        // Middle band — full width, between the two corner bands
        int middleBandHeight = height - 2 * cornerRadius;
        if (middleBandHeight > 0)
            DrawScaledPixel(x, y + cornerRadius, color, width, middleBandHeight);

        // Corner regions filled via horizontal scanlines using midpoint algorithm.
        int leftArcCenterX   = x + cornerRadius;
        int rightArcCenterX  = x + width - 1 - cornerRadius;
        int topArcCenterY    = y + cornerRadius;
        int bottomArcCenterY = y + height - 1 - cornerRadius;

        IterateMidpointArc(cornerRadius, (dx, dy) =>
        {
            // Wide scanline: vertical offset dy, horizontal span covers 2*dx+1 cells
            int wideLeft  = leftArcCenterX - dx;
            int wideWidth = rightArcCenterX + dx - wideLeft + 1; // = width - 2*cornerRadius + 2*dx
            DrawScaledPixel(wideLeft, topArcCenterY    - dy, color, wideWidth, 1);  // Top band
            if (bottomArcCenterY + dy != topArcCenterY - dy)
                DrawScaledPixel(wideLeft, bottomArcCenterY + dy, color, wideWidth, 1);  // Bottom band

            // Narrow scanline: vertical offset dx, horizontal span covers 2*dy+1 cells (other octant)
            if (dx != dy)
            {
                int narrowLeft  = leftArcCenterX - dy;
                int narrowWidth = rightArcCenterX + dy - narrowLeft + 1; // = width - 2*cornerRadius + 2*dy
                DrawScaledPixel(narrowLeft, topArcCenterY    - dx, color, narrowWidth, 1);  // Top band
                if (bottomArcCenterY + dx != topArcCenterY - dx)
                    DrawScaledPixel(narrowLeft, bottomArcCenterY + dx, color, narrowWidth, 1);  // Bottom band
            }
        });
    }

    #endregion

    #region PALETTE OPERATIONS

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Pal
    /// </summary>
    public void Pal()
    {
        _paletteManager.ResetPalette();
    }

    public void Pal(Color key, Color value)
    {
        _paletteManager.SetPalette(key, value);
    }

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Palt
    /// </summary>
    public void Palt()
    {
        _paletteManager.ResetTransparency();
    }

    public void Palt(Color key, int opacity)
    {
        _paletteManager.SetTransparency(key, opacity);
    }

    #endregion

    #region CAMERA STATE

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Camera
    /// </summary>
    public void Camera(int x = 0, int y = 0)
    {
        _camera = (x, y);
    }

    #endregion

    #region RENDERING OPERATIONS

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Map
    /// </summary>
    public void Map(int sourceX, int sourceY, int destX, int destY, int sourceWidth, int sourceHeight, int flags = 0)
    {
        Texture2D texture = _spriteTextureManager.GetMapRegionTexture(sourceX, sourceY, sourceWidth, sourceHeight, flags);
        DrawSpriteTexture(texture, destX, destY, false, false);
    }

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Print
    /// </summary>
    public void Print(string text, int x, int y, Color color, Font font)
    {
        x -= _camera.x;
        y -= _camera.y;

        if (!_textureDictionary.TryGetValue(font.TextureName, out var fontTexture))
            return;

        int cellW = font.Characters.Max(pair => pair.Value.Width);
        int cellH = font.Characters.Max(pair => pair.Value.Height);

        if (cellW == 0 || fontTexture.Width % cellW != 0)
            throw new InvalidOperationException(
                $"Font texture '{font.TextureName}' width {fontTexture.Width} is not a multiple of cell width {cellW}.");

        int cols = fontTexture.Width / cellW;
        int cursorX = x;

        foreach (char c in text)
        {
            int tierYStart = 0;

            foreach (var (chars, size) in font.Characters)
            {
                int charIndex = chars.IndexOf(c);
                if (charIndex >= 0)
                {
                    int srcX = (charIndex % cols) * cellW;
                    int srcY = tierYStart + (charIndex / cols) * cellH;

                    _batch.Draw(
                        fontTexture,
                        new Rectangle(cursorX, y, size.Width, size.Height),
                        new Rectangle(srcX, srcY, size.Width, size.Height),
                        color);

                    cursorX += size.Width;
                    break;
                }
                tierYStart += (int)Math.Ceiling((double)chars.Length / cols) * cellH;
            }
            // Unknown chars: do nothing
        }
    }

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Spr
    /// </summary>
    public void Spr(int index, int x, int y, int width = 1, int height = 1, bool flipX = false, bool flipY = false)
    {
        Texture2D texture = _spriteTextureManager.GetSpriteTexture(index, width, height);
        DrawSpriteTexture(texture, x, y, flipX, flipY);
    }

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Sspr
    /// </summary>
    public void Sspr(int sourceX, int sourceY, int sourceWidth, int sourceHeight, int destX, int destY,
            int destWidth = -1, int destHeight = -1, bool flipX = false, bool flipY = false)
    {
        if (destWidth < 0) destWidth = sourceWidth;
        if (destHeight < 0) destHeight = sourceHeight;

        Texture2D texture = _spriteTextureManager.GetScaledRegionTexture(
            sourceX, sourceY, sourceWidth, sourceHeight, destWidth, destHeight);
        DrawSpriteTexture(texture, destX, destY, flipX, flipY);
    }

    private void DrawSpriteTexture(Texture2D texture, int x, int y, bool flipX, bool flipY)
    {
        x -= _camera.x;
        y -= _camera.y;

        var (scaleX, scaleY) = ComputeViewportScales();

        SpriteEffects effects = SpriteEffects.None;
        if (flipX) effects |= SpriteEffects.FlipHorizontally;
        if (flipY) effects |= SpriteEffects.FlipVertically;

        _batch.Draw(
            texture,
            new Rectangle(x * scaleX, y * scaleY, texture.Width * scaleX, texture.Height * scaleY),
            null,
            Color.White,
            0f,
            Vector2.Zero,
            effects,
            0f);
    }

    #endregion

    #region LOW-LEVEL DRAWING

    public void DrawTexture(string textureName, double x, double y, Color color,
            double scaleX = 1, double scaleY = 1, bool flipX = false, bool flipY = false)
    {
        
    }

    public void DrawScaledPixel(double x, double y, Color color, double scaleX = 1,
            double scaleY = 1, bool flipX = false, bool flipY = false)
    {
        var (pixScaleX, pixScaleY) = ComputeViewportScales();
        _batch.Draw(_pixel,
            new Rectangle(
                (int)x * pixScaleX,
                (int)y * pixScaleY,
                Math.Max(1, (int)scaleX * pixScaleX),
                Math.Max(1, (int)scaleY * pixScaleY)),
            color);
    }

    /// <summary>
    /// Returns the integer pixel scale factors that map one cell unit to pixels
    /// on the X and Y axes independently.
    /// Each axis uses the largest integer multiplier that fits the cell grid
    /// within the viewport on that axis — no letterboxing, no shared scale.
    /// </summary>
    private (int scaleX, int scaleY) ComputeViewportScales()
    {
        var vp = _graphicsDevice.Viewport;
        var (cellW, cellH) = _getSceneResolution();
        return (Math.Max(1, vp.Width / cellW), Math.Max(1, vp.Height / cellH));
    }

    private static void IterateMidpointArc(int radius, Action<int, int> onStep)
    {
        for (int dx = radius, dy = 0, error = 0; dx >= dy; )
        {
            onStep(dx, dy);
            dy++;
            if (error < radius - 1)
            {
                error += 1 + 2 * dy;
            }
            else
            {
                dx--;
                error += 1 + 2 * (dy - dx);
            }
        }
    }

    public void DrawLine(Vector2 start, Vector2 end, Color color, double thickness)
    {
        
    }

    public void DrawCirc(Vector2 center, double radius, Color color, double thickness, int segments)
    {
        
    }

    public void DrawRect(Vector2 topLeft, double width, double height, Color color, double thickness)
    {
        
    }

    #endregion
}
