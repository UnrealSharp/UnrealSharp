namespace UnrealSharp.CoreUObject;

public partial struct LinearColor : IEquatable<LinearColor>
{
    public LinearColor(float r, float g, float b, float a = 1)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }
    
    public static LinearColor operator *(LinearColor a, LinearColor b)
    {
        return new LinearColor(a.R * b.R, a.G * b.G, a.B * b.B, a.A * b.A);
    }
    
    public static LinearColor operator +(LinearColor a, LinearColor b)
    {
        return new LinearColor(a.R + b.R, a.G + b.G, a.B + b.B, a.A + b.A);
    }
    
    public static LinearColor operator -(LinearColor a, LinearColor b)
    {
        return new LinearColor(a.R - b.R, a.G - b.G, a.B - b.B, a.A - b.A);
    }
    
    public static LinearColor operator *(LinearColor a, float b)
    {
        return new LinearColor(a.R * b, a.G * b, a.B * b, a.A * b);
    }
    
    public static LinearColor operator /(LinearColor a, float b)
    {
        return new LinearColor(a.R / b, a.G / b, a.B / b, a.A / b);
    }
    
    public static LinearColor operator +(LinearColor a, float b)
    {
        return new LinearColor(a.R + b, a.G + b, a.B + b, a.A + b);
    }
    
    public static LinearColor operator -(LinearColor a, float b)
    {
        return new LinearColor(a.R - b, a.G - b, a.B - b, a.A - b);
    }
    
    public static bool operator ==(LinearColor a, LinearColor b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(LinearColor a, LinearColor b)
    {
        return !(a == b);
    }

    public bool IsZero()
    {
        return R == 0 && G == 0 && B == 0 && A == 0;
    }
    
    public bool Equals(LinearColor other)
    {
        return R.Equals(other.R) && G.Equals(other.G) && B.Equals(other.B) && A.Equals(other.A);
    }
    
    public override bool Equals(object obj)
    {
        return obj is LinearColor other && Equals(other);
    }

    public override string ToString()
    {
        return $"R={R}, G={G}, B={B}, A={A}";
    }
    
    public static LinearColor Red => new(1, 0, 0);
    public static LinearColor Green => new(0, 1, 0);
    public static LinearColor Blue => new(0, 0, 1);
    public static LinearColor White => new(1, 1, 1);
    public static LinearColor Black => new(0, 0, 0);
    public static LinearColor Transparent => new(0, 0, 0, 0);
    public static LinearColor Yellow => new(1, 1, 0);
    public static LinearColor Cyan => new(0, 1, 1);
    public static LinearColor Magenta => new(1, 0, 1);
    public static LinearColor Orange => new(1, 0.5f, 0);
    public static LinearColor Purple => new(0.5f, 0, 0.5f);
    public static LinearColor Turquoise => new(0, 1, 1);
    public static LinearColor Silver => new(0.75f, 0.75f, 0.75f);
    public static LinearColor Emerald => new(0.25f, 1, 0.5f);
    public static LinearColor Gold => new(1, 0.75f, 0);
    public static LinearColor Bronze => new(0.75f, 0.5f, 0.25f);
    public static LinearColor SlateBlue => new(0.25f, 0.25f, 1);
    public static LinearColor Foreground => new(0.894f, 0.894f, 0.894f);
    public static LinearColor Background => new(0.125f, 0.125f, 0.125f);
    public static LinearColor TransparentRed => new(1, 0, 0, 0.5f);
    public static LinearColor TransparentGreen => new(0, 1, 0, 0.5f);
    public static LinearColor TransparentBlue => new(0, 0, 1, 0.5f);
    public static LinearColor TransparentWhite => new(1, 1, 1, 0.5f);
    public static LinearColor TransparentBlack => new(0, 0, 0, 0.5f);
    public static LinearColor Gray => new(0.5f, 0.5f, 0.5f);
    public static LinearColor YellowGreen => new(0.75f, 1, 0);
    public static LinearColor CyanBlue => new(0, 0.75f, 1);
    public static LinearColor MagentaPink => new(1, 0, 0.75f);
    public static LinearColor OrangeYellow => new(1, 0.75f, 0);
    public static LinearColor PurplePink => new(0.75f, 0, 1);
    public static LinearColor GrassGreen => new(0.25f, 1, 0);
    public static LinearColor PastelBlue => new(0, 0.25f, 1);
    public static LinearColor PastelOrange => new(1, 0.5f, 0);
    public static LinearColor PastelPurple => new(0.5f, 0, 1);
    public static LinearColor PastelYellow => new(1, 0.75f, 0);
    public static LinearColor LightOrange => new(1, 0.5f, 0.25f);
    public static LinearColor LightPurple => new(0.5f, 0.25f, 1);
    public static LinearColor LightBlue => new(0.25f, 0.5f, 1);
    public static LinearColor LightYellow => new(1, 0.75f, 0.25f);
    public static LinearColor LightGreen => new(0.25f, 1, 0.5f);
    public static LinearColor LightPink => new(1, 0.25f, 0.5f);
    public static LinearColor LightTurquoise => new(0.25f, 1, 0.75f);
    public static LinearColor LightGray => new(0.75f, 0.75f, 0.75f);
    public static LinearColor DarkGray => new(0.25f, 0.25f, 0.25f);
    public static LinearColor SpringGreen => new(0, 1, 0.25f);
    public static LinearColor Aqua => new(0, 0.25f, 1);
    public static LinearColor Pink => new(1, 0, 0.25f);
    public static LinearColor Rose => new(1, 0.25f, 0.25f);
    public static LinearColor Lavender => new(0.25f, 0, 1);
    public static LinearColor TurquoiseBlue => new(0, 1, 0.75f);
    public static LinearColor Violet => new(0.75f, 0, 1);
    public static LinearColor LightBrown => new(0.75f, 0.5f, 0.25f);
    public static LinearColor Brown => new(0.5f, 0.25f, 0);
    public static LinearColor Olive => new(0.5f, 0.5f, 0);
    public static LinearColor DarkGreen => new(0, 0.5f, 0);
    public static LinearColor DarkBlue => new(0, 0, 0.5f);
    public static LinearColor DarkPurple => new(0.5f, 0, 0.5f);
    public static LinearColor DarkRed => new(0.5f, 0, 0);
    public static LinearColor DarkBrown => new(0.5f, 0.25f, 0.25f);
    public static LinearColor CyanWhite => new(0.75f, 1, 1);
    public static LinearColor YellowWhite => new(1, 1, 0.75f);
    public static LinearColor GreenWhite => new(0.75f, 1, 0.75f);
    public static LinearColor MagentaWhite => new(1, 0.75f, 1);
    public static LinearColor BlueWhite => new(0.75f, 0.75f, 1);
    public static LinearColor OrangeWhite => new(1, 0.75f, 0.75f);
    public static LinearColor PurpleWhite => new(0.75f, 0.75f, 1);
    public static LinearColor TurquoiseWhite => new(0.75f, 1, 0.75f);
    public static LinearColor GrayWhite => new(0.75f, 0.75f, 0.75f);
}