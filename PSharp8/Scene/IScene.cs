namespace PSharp8.Scene;

public interface IScene
{
    string? Name { get; }
    void Init(ISceneSetup setup);
    string? SpritesPath { get; }
    string? MapPath { get; }
    string? FlagDataPath { get; }
    IReadOnlyList<Soundtrack> Music { get; }
    IReadOnlyList<SfxPack> Sfx { get; }
}

