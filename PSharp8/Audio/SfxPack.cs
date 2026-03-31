namespace PSharp8.Audio;

public class SfxPack(string name, string prefix)
{
    private readonly string _name = name ?? throw new ArgumentNullException(nameof(name));
    private readonly string _prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));

    internal string Name => _name;
    internal string Prefix => _prefix;
}
