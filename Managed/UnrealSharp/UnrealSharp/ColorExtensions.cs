using UnrealSharp.CoreUObject;
using UnrealSharp.SlateCore;

using Color = System.Drawing.Color;

namespace UnrealSharp;

public static class ColorExtensions
{
    public static FLinearColor ToLinearColor(this Color a)
    {
        var result = new FLinearColor
        {
            R = a.R,
            G = a.G,
            B = a.B,
            A = a.A
        };
        return result;
    }
    
    public static FSlateColor ToSlateColor(this FLinearColor a)
    {
        var result = new FSlateColor();
        result.SpecifiedColor.R = a.R;
        result.SpecifiedColor.G = a.G;
        result.SpecifiedColor.B = a.B;
        result.SpecifiedColor.A = a.A;
        result.ColorUseRule = ESlateColorStylingMode.UseColor_Specified;
        return result;
    }
}