using UnrealSharp.CoreUObject;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FRandomStreamExporter
{
    public static delegate* unmanaged<ref FRandomStream, void> GenerateNewSeed;
    public static delegate* unmanaged<ref FRandomStream, float> GetFraction;
    public static delegate* unmanaged<ref FRandomStream, uint> GetUnsignedInt;
    public static delegate* unmanaged<ref FRandomStream, FVector> GetUnitVector;
    public static delegate* unmanaged<ref FRandomStream, int, int, int> RandRange;
    public static delegate* unmanaged<ref FRandomStream, FVector, float, FVector> VRandCone;
    public static delegate* unmanaged<ref FRandomStream, FVector, float, float, FVector> VRandCone2;
}