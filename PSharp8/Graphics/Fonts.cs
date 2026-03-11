namespace PSharp8.Graphics;

public static class Fonts
{
    public static Font P8SCII => new(
        characters: new(){
            { "▮■□⁙⁘‖◀▶「」¥•、。゛゜ !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~○", (6, 4) },
            { "█▒?⬇️░✽●♥☉웃⌂⬅️?♪🅾️◆…➡️★⧗⬆️ˇ∧❎▤▥あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをんっゃゅょアイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲンッャュョ◜◝", (6, 8) }},
        textureName: "P8SCII");

    public static Font BigFont => new(
        characters: new(){{ "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_- ", (12, 8) }},
        textureName: "BigFont");
}

public class Font
{
    private readonly Dictionary<string, (int Width, int Height)> _characters;
    private readonly string _textureName;

    public Font(Dictionary<string, (int Width, int Height)> characters, string textureName)
    {
        _characters = characters;
        _textureName = textureName;
    }

    public Dictionary<string, (int Width, int Height)> Characters => _characters;
    public string TextureName => _textureName;
}