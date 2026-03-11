namespace PSharp8.Audio;
  
public class Soundtrack
{
    private readonly string _name;
    private readonly List<Track> _tracks;

    public Soundtrack(string name, List<Track> tracks)
    {
        _name = name;
        _tracks = tracks;
    }

    public string Name => _name;
    public List<Track> Tracks => _tracks;
}

public class Track
{
    private readonly List<TrackPart> _parts;
    private readonly int _channel;

    public Track(List<TrackPart> parts, int channel)
    {
        _parts = parts;
        _channel = channel;
    }

    public List<TrackPart> Parts => _parts;
    public int Channel => _channel;
}

public class TrackPart
{
    private readonly string _filename;
    private readonly bool _loop;

    public TrackPart(string filename, bool loop)
    {
        _filename = filename;
        _loop = loop;
    }

    public string Filename => _filename;
    public bool Loop => _loop;
}