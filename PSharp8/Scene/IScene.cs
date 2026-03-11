namespace PSharp8.Scene;

public interface IScene
{
    string SceneName { get; }
    double Fps { get; }
    (int w, int h) Resolution { get; }
    void Init();
    void Update();
    void Draw();
    string? SpritesPath { get; }
    string? FlagData { get; }
    string? MapPath { get; }
    List<Soundtrack> Music { get; }
    Dictionary<string, string> Sfx { get; }
    void Dispose();
}

