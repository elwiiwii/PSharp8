namespace PSharp8.Graphics;

public static class Fonts
{
    public static Font P8SCII => new(
        characters: new(){
            { (6, 4), "▮■□⁙⁘‖◀▶「」¥•、。゛゜ !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~○" },
            { (6, 8), "█▒?⬇️░✽●♥☉웃⌂⬅️?♪🅾️◆…➡️★⧗⬆️ˇ∧❎▤▥あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをんっゃゅょアイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲンッャュョ◜◝" },
        },
        textureName: "P8SCII");

    public static Font BigFont => new(
        characters: new(){{ (12, 8), "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_- " }},
        textureName: "BigFont");
}

public class Font
{
    private readonly Dictionary<(int Width, int Height), string> _characters;
    private readonly string _textureName;

    public Font(Dictionary<(int Width, int Height), string> characters, string textureName)
    {
        _characters = characters;
        _textureName = textureName;
    }

    public Dictionary<(int Width, int Height), string> Characters => _characters;
    public string TextureName => _textureName;
}