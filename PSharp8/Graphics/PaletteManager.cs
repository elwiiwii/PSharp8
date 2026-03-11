using Color = Microsoft.Xna.Framework.Color;

namespace PSharp8.Graphics;

public class PaletteManager
{
    private Dictionary<Color, Color> _paletteMap;

    public int PaletteVersion { get; private set; }

    public PaletteManager()
    {
        ResetPalette();
    }

    public Dictionary<Color, Color> PaletteMap => _paletteMap;

//    public void SetPalette(int index, Color value)
//    {
//        if (index < 0 || index >= _paletteMap.Count)
//            throw new ArgumentOutOfRangeException(nameof(index), "Color index must be in valid palette range");
//
//        Color key = _paletteMap.ElementAt(index).Key;
//        SetPalette(key, value);
//    }

    public void SetPalette(Color key, Color value)
    {
        _paletteMap[key] = value;
        PaletteVersion++;
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

    public void SetTransparency(Color key, int opacity)
    {
        if (!_paletteMap.TryGetValue(key, out var existing))
            existing = key;

        existing.A = (byte)Math.Clamp(opacity, 0, 255);
        _paletteMap[key] = existing;
        PaletteVersion++;
    }

    [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(_paletteMap))]
    public void ResetPalette()
    {
        _paletteMap = new(Pico8.Palette);
        PaletteVersion++;
    }

    public void ResetTransparency()
    {
        for (int i = 0; i < _paletteMap.Count; i++)
        {
            Color key = _paletteMap.ElementAt(i).Key;
            Color value = _paletteMap[key];
            value.A = 255;
            _paletteMap[key] = value;
        }
        PaletteVersion++;
    }
}
