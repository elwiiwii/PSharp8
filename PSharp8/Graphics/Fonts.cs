namespace PSharp8.Graphics;

public static class Fonts
{
    public static Font P8SCII => new(
        characters: new(){
            { "▮■□⁙⁘‖◀▶「」¥•、。゛゜ !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~○", (4, 6) },
            { "█▒?⬇️░✽●♥☉웃⌂⬅️?♪🅾️◆…➡️★⧗⬆️ˇ∧❎▤▥あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをんっゃゅょアイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲンッャュョ◜◝", (8, 6) },
        },
        textureName: "P8SCII");

    public static Font BigFont => new(
        characters: new(){{ "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_- ", (8, 12) }},
        textureName: "BigFont");
}

public class Font(Dictionary<string, (int Width, int Height)> characters, string textureName)
{
    private readonly Dictionary<string, (int Width, int Height)> _characters = characters;
    private readonly string _textureName = textureName;

    public Dictionary<string, (int Width, int Height)> Characters => _characters;
    public string TextureName => _textureName;
}