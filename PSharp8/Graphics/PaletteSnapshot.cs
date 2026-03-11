using Microsoft.Xna.Framework;

namespace PSharp8.Graphics;

internal readonly struct PaletteSnapshot : IEquatable<PaletteSnapshot>
{
    private readonly (uint key, uint value)[] _entries;

    public PaletteSnapshot(IEnumerable<KeyValuePair<Color, Color>> relevantEntries)
    {
        _entries = relevantEntries
            .Select(kvp => (kvp.Key.PackedValue, kvp.Value.PackedValue))
            .OrderBy(e => e.Item1)
            .ToArray();
    }

    public bool Equals(PaletteSnapshot other)
        => (_entries ?? []).SequenceEqual(other._entries ?? []);

    public override bool Equals(object? obj)
        => obj is PaletteSnapshot other && Equals(other);

    public override int GetHashCode()
        => (_entries ?? []).Aggregate(0, (hash, e) => HashCode.Combine(hash, e.Item1, e.Item2));

    public static bool operator ==(PaletteSnapshot left, PaletteSnapshot right) => left.Equals(right);
    public static bool operator !=(PaletteSnapshot left, PaletteSnapshot right) => !left.Equals(right);
}
