using System.Runtime.InteropServices;
using UnrealSharp.Attributes;
using UnrealSharp.CoreUObject;

namespace UnrealSharp.EnhancedInput;

[UStruct, BlittableType, StructLayout(LayoutKind.Sequential)]
public partial struct FInputActionValue
{
    private FVector AxisValue;
    private EInputActionValueType ValueType;
    
    public float GetAxis1D()
    {
        return (float) AxisValue.X;
    }
    
    public FVector2D GetAxis2D()
    {
        return new FVector2D(AxisValue.X, AxisValue.Y);
    }
    
    public FVector GetAxis3D()
    {
        return AxisValue;
    }
}