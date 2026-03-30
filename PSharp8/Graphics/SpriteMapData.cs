using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PSharp8.Graphics;

internal class SpriteMapData
{
    private const int SPRITE_WIDTH = 8;
    private const int SPRITE_HEIGHT = 8;
    private readonly Texture2D _originalSpritesheetTexture;
    private readonly Color[] _spritesheetData;
    private readonly int _spriteSheetWidth;
    private readonly int _spriteSheetHeight;
    private readonly int _spriteCount;
    private int _spritesheetVersion;
    private readonly Texture2D _originalMapTexture;
    private readonly int[] _mapData;
    private readonly int _mapWidth;
    private readonly int _mapHeight;
    private int _mapVersion;
    private readonly int[] _originalFlags;
    private readonly int[] _flagData;

    internal int SpriteSheetWidth => _spriteSheetWidth;
    internal int SpriteSheetHeight => _spriteSheetHeight;
    internal int SpritesPerRow => _spriteSheetWidth / SPRITE_WIDTH;
    internal int SpriteCount => _spriteCount;
    internal int SpritesheetVersion => _spritesheetVersion;
    internal int MapWidth => _mapWidth;
    internal int MapHeight => _mapHeight;
    internal int MapVersion => _mapVersion;

    internal Color[] SpritesheetData => _spritesheetData;

    internal SpriteMapData(
        Texture2D spriteTexture,
        Texture2D mapTexture,
        string flagString)
    {
        if (spriteTexture.Width % SPRITE_WIDTH != 0 || spriteTexture.Height % SPRITE_HEIGHT != 0)
            throw new ArgumentException(
                $"Sprite texture dimensions ({spriteTexture.Width}x{spriteTexture.Height}) must be multiples of {SPRITE_WIDTH}.",
                nameof(spriteTexture));

        if (mapTexture.Width % SPRITE_WIDTH != 0 || mapTexture.Height % SPRITE_HEIGHT != 0)
            throw new ArgumentException(
                $"Map texture dimensions ({mapTexture.Width}x{mapTexture.Height}) must be multiples of {SPRITE_WIDTH}.",
                nameof(mapTexture));

        _originalSpritesheetTexture = spriteTexture ?? throw new ArgumentNullException(nameof(spriteTexture));
        _spriteSheetWidth = spriteTexture.Width;
        _spriteSheetHeight = spriteTexture.Height;
        _spriteCount = (_spriteSheetWidth / SPRITE_WIDTH) * (_spriteSheetHeight / SPRITE_HEIGHT);
        _originalMapTexture = mapTexture ?? throw new ArgumentNullException(nameof(mapTexture));
        _mapWidth = mapTexture.Width / SPRITE_WIDTH;
        _mapHeight = mapTexture.Height / SPRITE_HEIGHT;
        _originalFlags = (flagString ?? "").Chunk(2)
            .Select(c => Convert.ToInt32(new string(c), 16))
            .Concat(Enumerable.Repeat(0, _spriteCount))
            .Take(_spriteCount)
            .ToArray();
        _flagData = (int[])_originalFlags.Clone();

        _spritesheetData = new Color[_spriteSheetWidth * _spriteSheetHeight];
        _mapData = new int[_mapWidth * _mapHeight];

        Reload();
    }

    internal void Reload()
    {
        _originalSpritesheetTexture.GetData(_spritesheetData);

        int mapTexWidth = _originalMapTexture.Width;
        Color[] mapPixels = new Color[_originalMapTexture.Width * _originalMapTexture.Height];
        _originalMapTexture.GetData(mapPixels);

        int spritesPerRow = _spriteSheetWidth / SPRITE_WIDTH;

        for (int cy = 0; cy < _mapHeight; cy++)
        {
            for (int cx = 0; cx < _mapWidth; cx++)
            {
                _mapData[cx + cy * _mapWidth] = FindMatchingSpriteIndex(
                    mapPixels, mapTexWidth,
                    cx * SPRITE_WIDTH, cy * SPRITE_HEIGHT,
                    spritesPerRow);
            }
        }

        Array.Copy(_originalFlags, _flagData, _spriteCount);
        _spritesheetVersion++;
        _mapVersion++;
    }

    private int FindMatchingSpriteIndex(
        Color[] mapPixels, int mapTexWidth,
        int tileOriginX, int tileOriginY,
        int spritesPerRow)
    {
        for (int si = 0; si < _spriteCount; si++)
        {
            int spriteOriginX = (si % spritesPerRow) * SPRITE_WIDTH;
            int spriteOriginY = (si / spritesPerRow) * SPRITE_HEIGHT;

            bool match = true;
            for (int py = 0; py < SPRITE_HEIGHT && match; py++)
            {
                for (int px = 0; px < SPRITE_WIDTH && match; px++)
                {
                    Color mapColor    = mapPixels[(tileOriginX + px) + (tileOriginY + py) * mapTexWidth];
                    Color spriteColor = _spritesheetData[(spriteOriginX + px) + (spriteOriginY + py) * _spriteSheetWidth];
                    if (mapColor != spriteColor)
                    {
                        match = false;
                        break;
                    }
                }
                if (!match) break;
            }

            if (match) return si;
        }

        return 0;
    }

    internal Color GetSpritePixel(int x, int y)
    {
        if (x < 0 || x >= _spriteSheetWidth || y < 0 || y >= _spriteSheetHeight)
            return Color.Black;

        return _spritesheetData[x + y * _spriteSheetWidth];
    }

    internal void SetSpritePixel(int x, int y, Color color)
    {
        if (x < 0 || x >= _spriteSheetWidth || y < 0 || y >= _spriteSheetHeight)
            return;

        _spritesheetData[x + y * _spriteSheetWidth] = color;
        _spritesheetVersion++;
    }

    internal int GetFlag(int n)
    {
        if (n < 0 || n >= _spriteCount) return 0;
        return _flagData[n];
    }

    internal bool GetFlag(int n, int bit)
    {
        if (n < 0 || n >= _spriteCount) return false;
        if (bit < 0 || bit > 7) return false;
        return (_flagData[n] >> bit & 1) == 1;
    }

    internal void SetFlag(int n, int value)
    {
        if (n < 0 || n >= _spriteCount) return;
        _flagData[n] = value & 0xFF;
    }

    internal void SetFlag(int n, int bit, bool v)
    {
        if (n < 0 || n >= _spriteCount) return;
        if (bit < 0 || bit > 7) return;
        if (v)
            _flagData[n] |= 1 << bit;
        else
            _flagData[n] &= ~(1 << bit);
    }

    internal int GetMapTile(int x, int y)
    {
        if (x < 0 || x >= _mapWidth || y < 0 || y >= _mapHeight)
            return 0;

        return _mapData[x + y * _mapWidth];
    }

    internal void SetMapTile(int x, int y, int spriteNumber)
    {
        if (x < 0 || x >= _mapWidth || y < 0 || y >= _mapHeight)
            return;

        if (spriteNumber < 0 || spriteNumber >= _spriteCount)
            return;

        _mapData[x + y * _mapWidth] = spriteNumber;
        _mapVersion++;
    }

    internal void MapToSpritesheet1D(
        int cellX = 0,
        int cellY = 32,
        int destX = 0,
        int destY = 64,
        int length = 8192,
        int @base = 16)
    {
        if (length <= 0 || @base <= 0)
            return;

        int mapStartIndex = cellX + (cellY * _mapWidth);
        int spriteStartIndex = destX + (destY * _spriteSheetWidth);
        int pixelsWritten = 0;

        for (int pixelOffset = 0; pixelOffset < length; pixelOffset++)
        {
            int mapIndex = mapStartIndex + (pixelOffset / 2);
            int spriteIndex = spriteStartIndex + pixelOffset;

            if (mapIndex < 0 || mapIndex >= _mapData.Length)
                break;

            if (spriteIndex < 0 || spriteIndex >= _spritesheetData.Length)
                break;

            int tile = _mapData[mapIndex];
            int splitValue = (pixelOffset % 2 == 0) ? (tile / @base) : (tile % @base);
            Color color = Pico8.BasePalette.ElementAt(splitValue % 16).Key;
            _spritesheetData[spriteIndex] = color;
            pixelsWritten++;
        }

        if (pixelsWritten > 0)
            _spritesheetVersion++;
    }

    internal void MapToSpritesheet2D(
        int cellX = 0,
        int cellY = 32,
        int cellW = 128,
        int cellH = 32,
        int destX = 0,
        int destY = 64,
        int destW = 128,
        int destH = 64,
        int @base = 16)
    {
        if (cellW <= 0 || cellH <= 0 || destW <= 0 || destH <= 0 || @base <= 0)
            return;

        int sourcePixels = cellW * cellH * 2;
        int destinationPixels = destW * destH;
        int copyPixels = Math.Min(sourcePixels, destinationPixels);
        int pixelsWritten = 0;

        for (int i = 0; i < copyPixels; i++)
        {
            int sourceCellOffset = i / 2;
            int sourceCellX = cellX + (sourceCellOffset % cellW);
            int sourceCellY = cellY + (sourceCellOffset / cellW);

            if (sourceCellX < 0 || sourceCellX >= _mapWidth || sourceCellY < 0 || sourceCellY >= _mapHeight)
                break;

            int destinationX = destX + (i % destW);
            int destinationY = destY + (i / destW);

            if (destinationX < 0 || destinationX >= _spriteSheetWidth || destinationY < 0 || destinationY >= _spriteSheetHeight)
                break;

            int tile = _mapData[sourceCellX + (sourceCellY * _mapWidth)];
            int splitValue = (i % 2 == 0) ? (tile / @base) : (tile % @base);
            Color color = Pico8.BasePalette.ElementAt(splitValue % 16).Key;
            _spritesheetData[destinationX + (destinationY * _spriteSheetWidth)] = color;
            pixelsWritten++;
        }

        if (pixelsWritten > 0)
            _spritesheetVersion++;
    }

    internal Rectangle GetSpriteSourceRect(int spriteIndex, int widthSprites = 1, int heightSprites = 1)
    {
        int spritesPerRow = _spriteSheetWidth / SPRITE_WIDTH;
        return new Rectangle(
            (spriteIndex % spritesPerRow) * SPRITE_WIDTH,
            (spriteIndex / spritesPerRow) * SPRITE_HEIGHT,
            widthSprites * SPRITE_WIDTH,
            heightSprites * SPRITE_HEIGHT);
    }
}
