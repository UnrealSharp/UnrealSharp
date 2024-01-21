using System.DoubleNumerics;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public unsafe partial class FRandomStreamExporter
{
    public static delegate* unmanaged<ref RandomStream, void> GenerateNewSeed;
    public static delegate* unmanaged<ref RandomStream, float> GetFraction;
    public static delegate* unmanaged<ref RandomStream, uint> GetUnsignedInt;
    public static delegate* unmanaged<ref RandomStream, Vector3> GetUnitVector;
    public static delegate* unmanaged<ref RandomStream, int, int, int> RandRange;
    public static delegate* unmanaged<ref RandomStream, Vector3, float, Vector3> VRandCone;
    public static delegate* unmanaged<ref RandomStream, Vector3, float, float, Vector3> VRandCone2;
}