using UnrealSharp.CoreUObject;
using UnrealSharp.SlateCore;

using Color = System.Drawing.Color;

namespace UnrealSharp;

public static class ColorExtensions
{
    public static LinearColor ToLinearColor(this Color a)
    {
        var result = new LinearColor
        {
            R = a.R,
            G = a.G,
            B = a.B,
            A = a.A
        };
        return result;
    }
    
    public static SlateColor ToSlateColor(this LinearColor a)
    {
        var result = new SlateColor();
        result.SpecifiedColor.R = a.R;
        result.SpecifiedColor.G = a.G;
        result.SpecifiedColor.B = a.B;
        result.SpecifiedColor.A = a.A;
        result.ColorUseRule = ESlateColorStylingMode.UseColor_Specified;
        return result;
    }
}