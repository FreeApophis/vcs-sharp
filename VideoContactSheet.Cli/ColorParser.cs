using SkiaSharp;

namespace VideoContactSheet.Cli;

internal static class ColorParser
{
    // X11 / ImageMagick / CSS named colors likely to appear in vcs config files.
    private static readonly Dictionary<string, SKColor> Named = new(StringComparer.OrdinalIgnoreCase)
    {
        ["black"] = new SKColor(0x00, 0x00, 0x00),
        ["white"] = new SKColor(0xFF, 0xFF, 0xFF),
        ["red"] = new SKColor(0xFF, 0x00, 0x00),
        ["lime"] = new SKColor(0x00, 0xFF, 0x00),
        ["green"] = new SKColor(0x00, 0x80, 0x00),
        ["blue"] = new SKColor(0x00, 0x00, 0xFF),
        ["yellow"] = new SKColor(0xFF, 0xFF, 0x00),
        ["cyan"] = new SKColor(0x00, 0xFF, 0xFF),
        ["aqua"] = new SKColor(0x00, 0xFF, 0xFF),
        ["magenta"] = new SKColor(0xFF, 0x00, 0xFF),
        ["fuchsia"] = new SKColor(0xFF, 0x00, 0xFF),
        ["silver"] = new SKColor(0xC0, 0xC0, 0xC0),
        ["gray"] = new SKColor(0x80, 0x80, 0x80),
        ["grey"] = new SKColor(0x80, 0x80, 0x80),
        ["maroon"] = new SKColor(0x80, 0x00, 0x00),
        ["olive"] = new SKColor(0x80, 0x80, 0x00),
        ["navy"] = new SKColor(0x00, 0x00, 0x80),
        ["purple"] = new SKColor(0x80, 0x00, 0x80),
        ["teal"] = new SKColor(0x00, 0x80, 0x80),
        ["orange"] = new SKColor(0xFF, 0xA5, 0x00),
        ["orangered"] = new SKColor(0xFF, 0x45, 0x00),
        ["pink"] = new SKColor(0xFF, 0xC0, 0xCB),
        ["hotpink"] = new SKColor(0xFF, 0x69, 0xB4),
        ["deeppink"] = new SKColor(0xFF, 0x14, 0x93),
        ["brown"] = new SKColor(0xA5, 0x2A, 0x2A),
        ["tan"] = new SKColor(0xD2, 0xB4, 0x8C),
        ["khaki"] = new SKColor(0xF0, 0xE6, 0x8C),
        ["gold"] = new SKColor(0xFF, 0xD7, 0x00),
        ["goldenrod"] = new SKColor(0xDA, 0xA5, 0x20),
        ["darkgoldenrod"] = new SKColor(0xB8, 0x86, 0x0B),
        ["lightgoldenrod"] = new SKColor(0xEE, 0xDD, 0x82),
        ["lightgoldenrodyellow"] = new SKColor(0xFA, 0xFA, 0xD2),
        ["palegoldenrod"] = new SKColor(0xEE, 0xE8, 0xAA),
        ["linen"] = new SKColor(0xFA, 0xF0, 0xE6),
        ["beige"] = new SKColor(0xF5, 0xF5, 0xDC),
        ["ivory"] = new SKColor(0xFF, 0xFF, 0xF0),
        ["snow"] = new SKColor(0xFF, 0xFA, 0xFA),
        ["wheat"] = new SKColor(0xF5, 0xDE, 0xB3),
        ["lightgray"] = new SKColor(0xD3, 0xD3, 0xD3),
        ["lightgrey"] = new SKColor(0xD3, 0xD3, 0xD3),
        ["darkgray"] = new SKColor(0xA9, 0xA9, 0xA9),
        ["darkgrey"] = new SKColor(0xA9, 0xA9, 0xA9),
        ["dimgray"] = new SKColor(0x69, 0x69, 0x69),
        ["dimgrey"] = new SKColor(0x69, 0x69, 0x69),
        ["slategray"] = new SKColor(0x70, 0x80, 0x90),
        ["slategrey"] = new SKColor(0x70, 0x80, 0x90),
        ["darkslategray"] = new SKColor(0x2F, 0x4F, 0x4F),
        ["lightslategray"] = new SKColor(0x77, 0x88, 0x99),
        ["lightblue"] = new SKColor(0xAD, 0xD8, 0xE6),
        ["skyblue"] = new SKColor(0x87, 0xCE, 0xEB),
        ["steelblue"] = new SKColor(0x46, 0x82, 0xB4),
        ["cornflowerblue"] = new SKColor(0x64, 0x95, 0xED),
        ["royalblue"] = new SKColor(0x41, 0x69, 0xE1),
        ["mediumblue"] = new SKColor(0x00, 0x00, 0xCD),
        ["darkblue"] = new SKColor(0x00, 0x00, 0x8B),
        ["indigo"] = new SKColor(0x4B, 0x00, 0x82),
        ["violet"] = new SKColor(0xEE, 0x82, 0xEE),
        ["plum"] = new SKColor(0xDD, 0xA0, 0xDD),
        ["orchid"] = new SKColor(0xDA, 0x70, 0xD6),
        ["thistle"] = new SKColor(0xD8, 0xBF, 0xD8),
        ["lavender"] = new SKColor(0xE6, 0xE6, 0xFA),
        ["lightgreen"] = new SKColor(0x90, 0xEE, 0x90),
        ["palegreen"] = new SKColor(0x98, 0xFB, 0x98),
        ["darkgreen"] = new SKColor(0x00, 0x64, 0x00),
        ["forestgreen"] = new SKColor(0x22, 0x8B, 0x22),
        ["seagreen"] = new SKColor(0x2E, 0x8B, 0x57),
        ["mediumseagreen"] = new SKColor(0x3C, 0xB3, 0x71),
        ["lightseagreen"] = new SKColor(0x20, 0xB2, 0xAA),
        ["mediumaquamarine"] = new SKColor(0x66, 0xCD, 0xAA),
        ["turquoise"] = new SKColor(0x40, 0xE0, 0xD0),
        ["coral"] = new SKColor(0xFF, 0x7F, 0x50),
        ["tomato"] = new SKColor(0xFF, 0x63, 0x47),
        ["salmon"] = new SKColor(0xFA, 0x80, 0x72),
        ["lightsalmon"] = new SKColor(0xFF, 0xA0, 0x7A),
        ["darksalmon"] = new SKColor(0xE9, 0x96, 0x7A),
        ["lightyellow"] = new SKColor(0xFF, 0xFF, 0xE0),
        ["lightyellowgreen"] = new SKColor(0x9A, 0xCD, 0x32),
        ["chartreuse"] = new SKColor(0x7F, 0xFF, 0x00),
        ["yellowgreen"] = new SKColor(0x9A, 0xCD, 0x32),
        ["greenyellow"] = new SKColor(0xAD, 0xFF, 0x2F),
        ["transparent"] = new SKColor(0x00, 0x00, 0x00, 0x00),
    };

    public static bool TryParse(string? value, out SKColor color)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            color = default;
            return false;
        }

        var trimmed = value.Trim();

        if (Named.TryGetValue(trimmed, out color))
        {
            return true;
        }

        if (trimmed.StartsWith('#'))
        {
            return TryParseHex(trimmed, out color);
        }

        color = default;
        return false;
    }

    private static bool TryParseHex(string hex, out SKColor color)
    {
        // Strip leading '#'
        var s = hex[1..];

        color = default;
        return s.Length switch
        {
            3 => TryExpand3(s, out color),
            4 => TryExpand4(s, out color),
            6 => TryParse6(s, out color),
            8 => TryParse8(s, out color),
            _ => false,
        };
    }

    private static bool TryExpand3(string s, out SKColor color)
    {
        // #rgb → #rrggbb
        if (!TryHex1(s[0], out byte r) || !TryHex1(s[1], out byte g) || !TryHex1(s[2], out byte b))
        {
            color = default;
            return false;
        }

        color = new SKColor((byte)(r | (r << 4)), (byte)(g | (g << 4)), (byte)(b | (b << 4)));
        return true;
    }

    private static bool TryExpand4(string s, out SKColor color)
    {
        // #rgba → #rrggbbaa
        if (!TryHex1(s[0], out byte r) || !TryHex1(s[1], out byte g) || !TryHex1(s[2], out byte b) || !TryHex1(s[3], out byte a))
        {
            color = default;
            return false;
        }

        color = new SKColor((byte)(r | (r << 4)), (byte)(g | (g << 4)), (byte)(b | (b << 4)), (byte)(a | (a << 4)));
        return true;
    }

    private static bool TryParse6(string s, out SKColor color)
    {
        if (!TryHex2(s, 0, out byte r) || !TryHex2(s, 2, out byte g) || !TryHex2(s, 4, out byte b))
        {
            color = default;
            return false;
        }

        color = new SKColor(r, g, b);
        return true;
    }

    private static bool TryParse8(string s, out SKColor color)
    {
        if (!TryHex2(s, 0, out byte r) || !TryHex2(s, 2, out byte g) || !TryHex2(s, 4, out byte b) || !TryHex2(s, 6, out byte a))
        {
            color = default;
            return false;
        }

        color = new SKColor(r, g, b, a);
        return true;
    }

    private static bool TryHex2(string s, int offset, out byte value)
    {
        if (!TryHex1(s[offset], out byte hi) || !TryHex1(s[offset + 1], out byte lo))
        {
            value = 0;
            return false;
        }

        value = (byte)((hi << 4) | lo);
        return true;
    }

    private static bool TryHex1(char c, out byte nibble)
    {
        nibble = c switch
        {
            >= '0' and <= '9' => (byte)(c - '0'),
            >= 'a' and <= 'f' => (byte)(c - 'a' + 10),
            >= 'A' and <= 'F' => (byte)(c - 'A' + 10),
            _ => 0xFF,
        };
        return nibble != 0xFF;
    }
}
