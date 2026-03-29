namespace PSharp8.Scene;

public interface IScene
{
    string SceneName { get; }
    (int w, int h) Resolution { get; }
    void Init();
    void Update(int fps);
    void UpdateWhilePaused(int fps);
    void Draw(int fps);
    void DrawWhilePaused(int fps);
    string? SpritesPath { get; }
    string? FlagData { get; }
    string? MapPath { get; }
    List<Soundtrack> Music { get; }
    Dictionary<string, string> Sfx { get; }
    void Dispose();
}

