using System.Runtime.InteropServices;
using UnrealSharp.Attributes;
using UnrealSharp.CoreUObject;

namespace UnrealSharp.EnhancedInput;

[UStruct, BlittableType, StructLayout(LayoutKind.Sequential)]
public partial struct InputActionValue
{
    private Vector AxisValue;
    private EInputActionValueType ValueType;
    
    public float GetAxis1D()
    {
        return (float) AxisValue.X;
    }
    
    public Vector2D GetAxis2D()
    {
        return new Vector2D(AxisValue.X, AxisValue.Y);
    }
    
    public Vector GetAxis3D()
    {
        return AxisValue;
    }
}