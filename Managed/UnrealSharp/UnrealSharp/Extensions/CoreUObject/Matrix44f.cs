namespace UnrealSharp.CoreUObject;

public partial record struct FMatrix44f
{
    public FPlane4f XPlane;
    public FPlane4f YPlane;
    public FPlane4f ZPlane;
    public FPlane4f WPlane;

    public FMatrix44f(FPlane4f xPlane, FPlane4f yPlane, FPlane4f zPlane, FPlane4f wPlane)
    {
        XPlane = xPlane;
        YPlane = yPlane;
        ZPlane = zPlane;
        WPlane = wPlane;
    }
}
