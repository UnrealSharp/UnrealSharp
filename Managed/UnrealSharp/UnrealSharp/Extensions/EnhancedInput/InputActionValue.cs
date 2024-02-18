using System.DoubleNumerics;
using System.Runtime.InteropServices;

namespace UnrealSharp.EnhancedInput;

[StructLayout(LayoutKind.Sequential)]
public partial struct InputActionValue
{
    private Vector3 AxisValue;
    private EInputActionValueType ValueType;
    
    public float GetAxis1D()
    {
        return (float) AxisValue.X;
    }
    
    public Vector2 GetAxis2D()
    {
        return new Vector2(AxisValue.X, AxisValue.Y);
    }
    
    public Vector3 GetAxis3D()
    {
        return AxisValue;
    }
}