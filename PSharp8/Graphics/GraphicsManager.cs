using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PSharp8.Graphics;

public class GraphicsManager
{
    private readonly SpriteBatch _batch;
    private (int X, int Y) _cameraOffset;
    private readonly GraphicsDeviceManager _graphics;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly PaletteManager _paletteManager;
    private readonly Texture2D _pixel;
    private readonly Dictionary<string, Texture2D> _textureDictionary;
    private readonly GameWindow _window;
    private readonly SpriteTextureManager _spriteTextureManager;

    public GraphicsManager(
        SpriteBatch batch,
        GraphicsDeviceManager graphics,
        GraphicsDevice graphicsDevice,
        PaletteManager paletteManager,
        Texture2D pixel,
        Dictionary<string, Texture2D> textureDictionary,
        GameWindow window,
        SpriteTextureManager spriteTextureManager)
    {
        _cameraOffset = (0, 0);
        _graphics = graphics ?? throw new ArgumentNullException(nameof(graphics));
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _paletteManager = paletteManager ?? throw new ArgumentNullException(nameof(paletteManager));
        _pixel = pixel ?? throw new ArgumentNullException(nameof(pixel));
        _textureDictionary = textureDictionary ?? throw new ArgumentNullException(nameof(textureDictionary));
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _batch = batch ?? throw new ArgumentNullException(nameof(batch));
        _spriteTextureManager = spriteTextureManager ?? throw new ArgumentNullException(nameof(spriteTextureManager));
    }

    #region DRAWING PRIMITIVES

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Circ
    /// </summary>
    public void Circ(int centerX, int centerY, int radius, Color color)
    {
        if (radius <= 0) return;

        centerX -= _cameraOffset.X;
        centerY -= _cameraOffset.Y;

        for (int dx = radius, dy = 0, error = 0; dx >= dy; )
        {
            DrawScaledPixel(centerX + dx, centerY + dy, color, 1, 1);
            DrawScaledPixel(centerX + dy, centerY + dx, color, 1, 1);
            DrawScaledPixel(centerX - dy, centerY + dx, color, 1, 1);
            DrawScaledPixel(centerX - dx, centerY + dy, color, 1, 1);
            DrawScaledPixel(centerX - dx, centerY - dy, color, 1, 1);
            DrawScaledPixel(centerX - dy, centerY - dx, color, 1, 1);
            DrawScaledPixel(centerX + dy, centerY - dx, color, 1, 1);
            DrawScaledPixel(centerX + dx, centerY - dy, color, 1, 1);

            dy += 1;
            if (error < radius - 1)
                error += 1 + 2 * dy;
            else
            {
                dx -= 1;
                error += 1 + 2 * (dy - dx);
            }
        }
    }

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Circfill
    /// </summary>
    public void Circfill(int centerX, int centerY, int radius, Color color)
    {
        if (radius <= 0) return;

        centerX -= _cameraOffset.X;
        centerY -= _cameraOffset.Y;

        for (int dx = radius, dy = 0, error = 0; dx >= dy; )
        {
            DrawScaledPixel(centerX - dx, centerY + dy, color, 2 * dx + 1, 1);
            DrawScaledPixel(centerX - dx, centerY - dy, color, 2 * dx + 1, 1);
            DrawScaledPixel(centerX - dy, centerY + dx, color, 2 * dy + 1, 1);
            DrawScaledPixel(centerX - dy, centerY - dx, color, 2 * dy + 1, 1);

            dy += 1;
            if (error < radius - 1)
                error += 1 + 2 * dy;
            else
            {
                dx -= 1;
                error += 1 + 2 * (dy - dx);
            }
        }
    }

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Cls
    /// </summary>
    public void Cls(Color color)
    {
        _graphicsDevice.Clear(color);
    }

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Pset
    /// </summary>
    public void Pset(int x, int y, Color color)
    {
        DrawScaledPixel(x, y, color, 1, 1);
    }

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Rect
    /// </summary>
    public void Rect(int xLeft, int yTop, int xRight, int yBottom, Color color)
    {
        xLeft -= _cameraOffset.X;
        yTop -= _cameraOffset.Y;
        int width = xRight - xLeft + 1;
        int height = yBottom - yTop + 1;

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
        xLeft -= _cameraOffset.X;
        yTop -= _cameraOffset.Y;
        int width = xRight - xLeft + 1;
        int height = yBottom - yTop + 1;

        DrawScaledPixel(xLeft, yTop, color, width, height);
    }

    /// <summary>
    /// https://www.lexaloffle.com/bbs/?tid=150992
    /// </summary>
    public void Rrect(int x, int y, int width, int height, int radius, Color color)
    {
        if (width <= 0 || height <= 0) return;

        x -= _cameraOffset.X;
        y -= _cameraOffset.Y;

        int r = Math.Clamp(radius, 0, Math.Min(width, height) / 2);

        //if (r == 0)
        //{
        //    Rect(x, y, x + width - 1, y + height - 1, color);
        //    return;
        //}

        // Straight edges (shortened by r on each end)
        int edgeW = width - 2 * r;
        int edgeH = height - 2 * r;
        if (edgeW > 0)
        {
            DrawScaledPixel(x + r, y, color, edgeW, 1);               // Top
            DrawScaledPixel(x + r, y + height - 1, color, edgeW, 1);  // Bottom
        }
        if (edgeH > 0)
        {
            DrawScaledPixel(x, y + r, color, 1, edgeH);               // Left
            DrawScaledPixel(x + width - 1, y + r, color, 1, edgeH);   // Right
        }

        // Corner arc centers (one quadrant per corner via midpoint algorithm)
        int tlx = x + r, tly = y + r;
        int trx = x + width - 1 - r, try_ = y + r;
        int blx = x + r, bly = y + height - 1 - r;
        int brx = x + width - 1 - r, bry = y + height - 1 - r;

        for (int dx = r, dy = 0, error = 0; dx >= dy; )
        {
            DrawScaledPixel(tlx - dx, tly - dy, color);   // Top-left
            DrawScaledPixel(tlx - dy, tly - dx, color);
            DrawScaledPixel(trx + dx, try_ - dy, color);  // Top-right
            DrawScaledPixel(trx + dy, try_ - dx, color);
            DrawScaledPixel(blx - dx, bly + dy, color);   // Bottom-left
            DrawScaledPixel(blx - dy, bly + dx, color);
            DrawScaledPixel(brx + dx, bry + dy, color);   // Bottom-right
            DrawScaledPixel(brx + dy, bry + dx, color);

            dy++;
            if (error < r - 1) error += 1 + 2 * dy;
            else { dx--; error += 1 + 2 * (dy - dx); }
        }
    }

    /// <summary>
    /// https://www.lexaloffle.com/bbs/?tid=150992
    /// </summary>
    public void Rrectfill(int x, int y, int width, int height, int radius, Color color)
    {
        if (width <= 0 || height <= 0) return;

        x -= _cameraOffset.X;
        y -= _cameraOffset.Y;

        int r = Math.Clamp(radius, 0, Math.Min(width, height) / 2);

        //if (r == 0)
        //{
        //    Rectfill(x, y, x + width - 1, y + height - 1, color);
        //    return;
        //}

        // Middle band — full width, between the two corner bands
        int edgeH = height - 2 * r;
        if (edgeH > 0)
            DrawScaledPixel(x, y + r, color, width, edgeH);

        // Corner regions filled via horizontal scanlines using midpoint algorithm.
        // tlx/trx: x centers of left/right corner columns
        // tly/bly: y centers of top/bottom corner rows
        int tlx = x + r;
        int trx = x + width - 1 - r;
        int tly = y + r;
        int bly = y + height - 1 - r;

        for (int dx = r, dy = 0, error = 0; dx >= dy; )
        {
            // Scanline at vertical offset dy from corner center
            // Spans from (tlx - dx) to (trx + dx)
            int left1 = tlx - dx;
            int w1 = trx + dx - left1 + 1; // = width - 2*r + 2*dx
            DrawScaledPixel(left1, tly - dy, color, w1, 1);  // Top band
            if (bly + dy != tly - dy)
                DrawScaledPixel(left1, bly + dy, color, w1, 1);  // Bottom band

            // Scanline at vertical offset dx from corner center (other octant)
            if (dx != dy)
            {
                int left2 = tlx - dy;
                int w2 = trx + dy - left2 + 1; // = width - 2*r + 2*dy
                DrawScaledPixel(left2, tly - dx, color, w2, 1);  // Top band
                if (bly + dx != tly - dx)
                    DrawScaledPixel(left2, bly + dx, color, w2, 1);  // Bottom band
            }

            dy++;
            if (error < r - 1) error += 1 + 2 * dy;
            else { dx--; error += 1 + 2 * (dy - dx); }
        }
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
        _cameraOffset = (x, y);
    }

    #endregion

    #region RENDERING OPERATIONS

    public void Print(string text, int x, int y, Color color, Font font)
    {
        x -= _cameraOffset.X;
        y -= _cameraOffset.Y;

        if (!_textureDictionary.TryGetValue(font.TextureName, out Texture2D? fontTexture))
            return;

        int cursorX = x;
        foreach (char c in text)
        {
            int charIndex = -1;
            int charWidth = 0;
            int charHeight = 0;
            int srcY = 0;

            foreach (var (chars, size) in font.Characters)
            {
                int idx = chars.IndexOf(c);
                if (idx >= 0)
                {
                    charIndex = idx;
                    charWidth = size.Width;
                    charHeight = size.Height;
                    break;
                }
                srcY += size.Height;
            }

            if (charIndex < 0)
            {
                cursorX += font.Characters.First().Value.Width;
                continue;
            }

            int charsPerRow = fontTexture.Width / charWidth;
            int srcX = (charIndex % charsPerRow) * charWidth;
            srcY += (charIndex / charsPerRow) * charHeight;

            _batch.Draw(fontTexture,
                new Rectangle(cursorX, y, charWidth, charHeight),
                new Rectangle(srcX, srcY, charWidth, charHeight),
                color);

            cursorX += charWidth;
        }
    }

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Spr
    /// </summary>
    public void Spr(int index, int x, int y, int width = 1, int height = 1, bool flipX = false, bool flipY = false)
    {
        if (_spriteTextureManager is null) return;
        x -= _cameraOffset.X;
        y -= _cameraOffset.Y;
        Texture2D tex = _spriteTextureManager.GetSpriteTexture(index, width, height);
        Rectangle dst = new(x, y, width * 8, height * 8);
        SpriteEffects effects = (flipX ? SpriteEffects.FlipHorizontally : SpriteEffects.None)
                              | (flipY ? SpriteEffects.FlipVertically : SpriteEffects.None);
        _batch.Draw(tex, dst, null, Color.White, 0f, Vector2.Zero, effects, 0f);
    }

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Sspr
    /// </summary>
    public void Sspr(int sourceX, int sourceY, int sourceWidth, int sourceHeight, int destX, int destY,
            int destWidth, int destHeight, bool flipX = false, bool flipY = false)
    {
        if (_spriteTextureManager is null) return;
        destX -= _cameraOffset.X;
        destY -= _cameraOffset.Y;
        Texture2D tex = _spriteTextureManager.GetSpritesheetTexture();
        Rectangle src = new(sourceX, sourceY, sourceWidth, sourceHeight);
        Rectangle dst = new(destX, destY, destWidth, destHeight);
        SpriteEffects effects = (flipX ? SpriteEffects.FlipHorizontally : SpriteEffects.None)
                              | (flipY ? SpriteEffects.FlipVertically : SpriteEffects.None);
        _batch.Draw(tex, dst, src, Color.White, 0f, Vector2.Zero, effects, 0f);
    }

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Map
    /// </summary>
    public void Map(int sourceX, int sourceY, int destX, int destY, int sourceWidth, int sourceHeight, int flags = 0)
    {
        if (_spriteTextureManager is null) return;
        destX -= _cameraOffset.X;
        destY -= _cameraOffset.Y;
        Texture2D tex = _spriteTextureManager.GetMapRegionTexture(sourceX, sourceY, sourceWidth, sourceHeight, flags);
        Rectangle dst = new(destX, destY, sourceWidth * 8, sourceHeight * 8);
        _batch.Draw(tex, dst, null, Color.White);
    }

    public void Tick()
    {
        _spriteTextureManager?.Tick();
    }

    /// <summary>
    /// https://pico-8.fandom.com/wiki/Line
    /// </summary>
    public void Line(int x0, int y0, int x1, int y1, Color c)
    {
        x0 -= _cameraOffset.X;
        y0 -= _cameraOffset.Y;
        x1 -= _cameraOffset.X;
        y1 -= _cameraOffset.Y;
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
        _batch.Draw(_pixel,
            new Rectangle((int)x, (int)y, Math.Max(1, (int)scaleX), Math.Max(1, (int)scaleY)),
            color);
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
