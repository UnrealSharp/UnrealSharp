using UnrealSharp.CoreUObject;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FRandomStreamExporter
{
    public static delegate* unmanaged<ref RandomStream, void> GenerateNewSeed;
    public static delegate* unmanaged<ref RandomStream, float> GetFraction;
    public static delegate* unmanaged<ref RandomStream, uint> GetUnsignedInt;
    public static delegate* unmanaged<ref RandomStream, Vector> GetUnitVector;
    public static delegate* unmanaged<ref RandomStream, int, int, int> RandRange;
    public static delegate* unmanaged<ref RandomStream, Vector, float, Vector> VRandCone;
    public static delegate* unmanaged<ref RandomStream, Vector, float, float, Vector> VRandCone2;
}