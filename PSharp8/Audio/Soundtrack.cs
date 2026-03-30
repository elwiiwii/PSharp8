namespace PSharp8.Audio;
  
public class Soundtrack(string name, List<Track> tracks)
{
    private readonly string _name = name;
    private readonly List<Track> _tracks = tracks;

    internal string Name => _name;
    internal List<Track> Tracks => _tracks;
}

public class Track(List<TrackPart> parts, int channel)
{
    private readonly List<TrackPart> _parts = parts;
    private readonly int _channel = channel;

    internal List<TrackPart> Parts => _parts;
    internal int Channel => _channel;
}

public class TrackPart(string filename, bool loop)
{
    private readonly string _filename = filename;
    private readonly bool _loop = loop;

    internal string Filename => _filename;
    internal bool Loop => _loop;
}