namespace UnrealSharp.CoreUObject;

public partial struct FLinearColor : IEquatable<FLinearColor>
{
    public FLinearColor(float r, float g, float b, float a = 1)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }
    
    public static FLinearColor operator *(FLinearColor a, FLinearColor b)
    {
        return new FLinearColor(a.R * b.R, a.G * b.G, a.B * b.B, a.A * b.A);
    }
    
    public static FLinearColor operator +(FLinearColor a, FLinearColor b)
    {
        return new FLinearColor(a.R + b.R, a.G + b.G, a.B + b.B, a.A + b.A);
    }
    
    public static FLinearColor operator -(FLinearColor a, FLinearColor b)
    {
        return new FLinearColor(a.R - b.R, a.G - b.G, a.B - b.B, a.A - b.A);
    }
    
    public static FLinearColor operator *(FLinearColor a, float b)
    {
        return new FLinearColor(a.R * b, a.G * b, a.B * b, a.A * b);
    }
    
    public static FLinearColor operator /(FLinearColor a, float b)
    {
        return new FLinearColor(a.R / b, a.G / b, a.B / b, a.A / b);
    }
    
    public static FLinearColor operator +(FLinearColor a, float b)
    {
        return new FLinearColor(a.R + b, a.G + b, a.B + b, a.A + b);
    }
    
    public static FLinearColor operator -(FLinearColor a, float b)
    {
        return new FLinearColor(a.R - b, a.G - b, a.B - b, a.A - b);
    }
    
    public static bool operator ==(FLinearColor a, FLinearColor b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(FLinearColor a, FLinearColor b)
    {
        return !(a == b);
    }

    public bool IsZero()
    {
        return R == 0 && G == 0 && B == 0 && A == 0;
    }
    
    public bool Equals(FLinearColor other)
    {
        return R.Equals(other.R) && G.Equals(other.G) && B.Equals(other.B) && A.Equals(other.A);
    }
    
    public override bool Equals(object obj)
    {
        return obj is FLinearColor other && Equals(other);
    }

    public override string ToString()
    {
        return $"R={R}, G={G}, B={B}, A={A}";
    }
    
    public static FLinearColor Red => new(1, 0, 0);
    public static FLinearColor Green => new(0, 1, 0);
    public static FLinearColor Blue => new(0, 0, 1);
    public static FLinearColor White => new(1, 1, 1);
    public static FLinearColor Black => new(0, 0, 0);
    public static FLinearColor Transparent => new(0, 0, 0, 0);
    public static FLinearColor Yellow => new(1, 1, 0);
    public static FLinearColor Cyan => new(0, 1, 1);
    public static FLinearColor Magenta => new(1, 0, 1);
    public static FLinearColor Orange => new(1, 0.5f, 0);
    public static FLinearColor Purple => new(0.5f, 0, 0.5f);
    public static FLinearColor Turquoise => new(0, 1, 1);
    public static FLinearColor Silver => new(0.75f, 0.75f, 0.75f);
    public static FLinearColor Emerald => new(0.25f, 1, 0.5f);
    public static FLinearColor Gold => new(1, 0.75f, 0);
    public static FLinearColor Bronze => new(0.75f, 0.5f, 0.25f);
    public static FLinearColor SlateBlue => new(0.25f, 0.25f, 1);
    public static FLinearColor Foreground => new(0.894f, 0.894f, 0.894f);
    public static FLinearColor Background => new(0.125f, 0.125f, 0.125f);
    public static FLinearColor TransparentRed => new(1, 0, 0, 0.5f);
    public static FLinearColor TransparentGreen => new(0, 1, 0, 0.5f);
    public static FLinearColor TransparentBlue => new(0, 0, 1, 0.5f);
    public static FLinearColor TransparentWhite => new(1, 1, 1, 0.5f);
    public static FLinearColor TransparentBlack => new(0, 0, 0, 0.5f);
    public static FLinearColor Gray => new(0.5f, 0.5f, 0.5f);
    public static FLinearColor YellowGreen => new(0.75f, 1, 0);
    public static FLinearColor CyanBlue => new(0, 0.75f, 1);
    public static FLinearColor MagentaPink => new(1, 0, 0.75f);
    public static FLinearColor OrangeYellow => new(1, 0.75f, 0);
    public static FLinearColor PurplePink => new(0.75f, 0, 1);
    public static FLinearColor GrassGreen => new(0.25f, 1, 0);
    public static FLinearColor PastelBlue => new(0, 0.25f, 1);
    public static FLinearColor PastelOrange => new(1, 0.5f, 0);
    public static FLinearColor PastelPurple => new(0.5f, 0, 1);
    public static FLinearColor PastelYellow => new(1, 0.75f, 0);
    public static FLinearColor LightOrange => new(1, 0.5f, 0.25f);
    public static FLinearColor LightPurple => new(0.5f, 0.25f, 1);
    public static FLinearColor LightBlue => new(0.25f, 0.5f, 1);
    public static FLinearColor LightYellow => new(1, 0.75f, 0.25f);
    public static FLinearColor LightGreen => new(0.25f, 1, 0.5f);
    public static FLinearColor LightPink => new(1, 0.25f, 0.5f);
    public static FLinearColor LightTurquoise => new(0.25f, 1, 0.75f);
    public static FLinearColor LightGray => new(0.75f, 0.75f, 0.75f);
    public static FLinearColor DarkGray => new(0.25f, 0.25f, 0.25f);
    public static FLinearColor SpringGreen => new(0, 1, 0.25f);
    public static FLinearColor Aqua => new(0, 0.25f, 1);
    public static FLinearColor Pink => new(1, 0, 0.25f);
    public static FLinearColor Rose => new(1, 0.25f, 0.25f);
    public static FLinearColor Lavender => new(0.25f, 0, 1);
    public static FLinearColor TurquoiseBlue => new(0, 1, 0.75f);
    public static FLinearColor Violet => new(0.75f, 0, 1);
    public static FLinearColor LightBrown => new(0.75f, 0.5f, 0.25f);
    public static FLinearColor Brown => new(0.5f, 0.25f, 0);
    public static FLinearColor Olive => new(0.5f, 0.5f, 0);
    public static FLinearColor DarkGreen => new(0, 0.5f, 0);
    public static FLinearColor DarkBlue => new(0, 0, 0.5f);
    public static FLinearColor DarkPurple => new(0.5f, 0, 0.5f);
    public static FLinearColor DarkRed => new(0.5f, 0, 0);
    public static FLinearColor DarkBrown => new(0.5f, 0.25f, 0.25f);
    public static FLinearColor CyanWhite => new(0.75f, 1, 1);
    public static FLinearColor YellowWhite => new(1, 1, 0.75f);
    public static FLinearColor GreenWhite => new(0.75f, 1, 0.75f);
    public static FLinearColor MagentaWhite => new(1, 0.75f, 1);
    public static FLinearColor BlueWhite => new(0.75f, 0.75f, 1);
    public static FLinearColor OrangeWhite => new(1, 0.75f, 0.75f);
    public static FLinearColor PurpleWhite => new(0.75f, 0.75f, 1);
    public static FLinearColor TurquoiseWhite => new(0.75f, 1, 0.75f);
    public static FLinearColor GrayWhite => new(0.75f, 0.75f, 0.75f);
}