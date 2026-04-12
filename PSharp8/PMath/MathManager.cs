using FixMath;

namespace PSharp8.PMath;

internal class MathManager
{
    private readonly SinDict _sinDict = new();
    private readonly CosDict _cosDict = new();
    private Random _random = new();

    internal F32 Abs(F32 a) => F32.Abs(a);

    internal F32 Ceil(F32 a) => F32.Ceil(a);

    internal F32 Cos(F32 angle)
    {
        angle = Mod(angle, F32.One);
        return F32.FromRaw((int)(_cosDict.LookupTable[angle.Raw / 10.0] * 10));
    }

    internal F32 Flr(F32 a) => F32.Floor(a);

    internal F32 Max(F32 a, F32 b) => F32.Max(a, b);

    internal F32 Mid(F32 a, F32 b, F32 c)
    {
        if (a > b) (a, b) = (b, a);
        if (b > c) (b, c) = (c, b);
        if (a > b) (a, b) = (b, a);
        return b;
    }

    internal F32 Min(F32 a, F32 b) => F32.Min(a, b);

    internal F32 Mod(F32 a, F32 b)
    {
        F32 r = a % b;
        return r < F32.Zero ? r + b : r;
    }

    internal F32 Rnd(F32 max, Random? r = null)
    {
        r ??= _random;
        return F32.FromDouble(r.NextDouble() * max.Double);
    }

    internal F32 Sgn(F32 a)
    {
        if (a > F32.Zero) return F32.One;
        if (a < F32.Zero) return F32.Neg1;
        return F32.Zero;
    }

    internal F32 Sin(F32 angle)
    {
        angle = Mod(angle, F32.One);
        return F32.FromRaw((int)(_sinDict.LookupTable[angle.Raw / 10.0] * 10));
    }

    internal void Srand(F32 seed)
    {
        int s = seed.Raw;
        _random = new Random(s);
    }
}