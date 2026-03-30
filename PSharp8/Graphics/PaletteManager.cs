using Color = Microsoft.Xna.Framework.Color;

namespace PSharp8.Graphics;

internal class PaletteManager
{
    private Dictionary<Color, Color> _paletteMap;
    private int _paletteVersion;

    internal PaletteManager()
    {
        ResetPalette();
    }

    internal Dictionary<Color, Color> PaletteMap => _paletteMap;
    internal int PaletteVersion => _paletteVersion;

//    public void SetPalette(int index, Color value)
//    {
//        if (index < 0 || index >= _paletteMap.Count)
//            throw new ArgumentOutOfRangeException(nameof(index), "Color index must be in valid palette range");
//
//        Color key = _paletteMap.ElementAt(index).Key;
//        SetPalette(key, value);
//    }

    internal void SetPalette(Color key, Color value)
    {
        _paletteMap[key] = value;
        _paletteVersion++;
    }

//    public void SetTransparency(int index, int opacity)
//    {
//        if (index < 0 || index >= _paletteMap.Count)
//            throw new ArgumentOutOfRangeException(nameof(index), "Color index must be in valid palette range");
//
//        Color key = _paletteMap.ElementAt(index).Key;
//
//        SetTransparency(key, opacity);
//    }

    internal void SetTransparency(Color key, int opacity)
    {
        if (!_paletteMap.TryGetValue(key, out var existing))
            existing = key;

        existing.A = (byte)Math.Clamp(opacity, 0, 255);
        _paletteMap[key] = existing;
        _paletteVersion++;
    }

    [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(_paletteMap))]
    internal void ResetPalette()
    {
        _paletteMap = new(Pico8.BasePalette);
        _paletteVersion++;
    }

    internal void ResetTransparency()
    {
        for (int i = 0; i < _paletteMap.Count; i++)
        {
            Color key = _paletteMap.ElementAt(i).Key;
            Color value = _paletteMap[key];
            value.A = 255;
            _paletteMap[key] = value;
        }
        _paletteVersion++;
    }
}
