using Microsoft.Xna.Framework;
using System.IO.Hashing;
using System.Runtime.InteropServices;

namespace PSharp8.Graphics;

internal readonly struct SpriteSnapshot : IEquatable<SpriteSnapshot>
{
    private readonly int _width;
    private readonly int _height;
    private readonly ulong _pixelHash;
    private readonly PaletteSnapshot _palette;

    internal SpriteSnapshot(Color[] pixels, int width, int height, Dictionary<Color, Color> paletteMap)
    {
        _width = width;
        _height = height;
        _pixelHash = XxHash64.HashToUInt64(MemoryMarshal.Cast<Color, byte>(pixels.AsSpan()));
        var pixelColors = new HashSet<Color>(pixels);
        _palette = new PaletteSnapshot(paletteMap.Where(kvp => pixelColors.Contains(kvp.Key)));
    }

    public bool Equals(SpriteSnapshot other)
        => _width == other._width && _height == other._height && _pixelHash == other._pixelHash && _palette.Equals(other._palette);

    public override bool Equals(object? obj)
        => obj is SpriteSnapshot other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(_width, _height, _pixelHash, _palette);

    public static bool operator ==(SpriteSnapshot left, SpriteSnapshot right) => left.Equals(right);
    public static bool operator !=(SpriteSnapshot left, SpriteSnapshot right) => !left.Equals(right);
}
