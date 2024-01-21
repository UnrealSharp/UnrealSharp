using System.DoubleNumerics;

namespace UnrealSharp;

public static class NumericsExtensions
{
	const float SmallNumber = 1e-8f;
	const float KindaSmallNumber = 1e-4f;

	public static bool IsNearlyZero(this Vector3 vec, float tolerance)
	{
		return
			Math.Abs(vec.X) < tolerance
			&&	Math.Abs(vec.Y) < tolerance
			&&	Math.Abs(vec.Z) < tolerance;
	}

	// Matrix4
	public static Vector3 GetOrigin(this Matrix4x4 matrix)
	{
		return new Vector3(matrix.M41, matrix.M42, matrix.M43);
	}

	public static double GetScaledAxis(this Matrix4x4 matrix, Axis axis)
	{
		Vector3 axisVector;

		switch(axis)
		{
			case Axis.X:
				axisVector = new Vector3(matrix.M11, matrix.M21, matrix.M31);
				break;
			case Axis.Y:
				axisVector = new Vector3(matrix.M12, matrix.M22, matrix.M32);
				break;
			case Axis.Z:
				axisVector = new Vector3(matrix.M13, matrix.M23, matrix.M33);
				break;
			default:
				throw new ArgumentException("Invalid axis");
		}

		return axisVector.Length();
	}
        
	public static bool IsNearlyZero(this double value, double tolerance)
	{
		return Math.Abs(value) < tolerance;
	}
        
	public static Matrix4x4 InvertSafe(this Matrix4x4 matrix)
	{
		// Check for zero scale matrix to invert
		if(	matrix.GetScaledAxis( Axis.X ).IsNearlyZero(SmallNumber) && 
		    matrix.GetScaledAxis( Axis.Y ).IsNearlyZero(SmallNumber) && 
		    matrix.GetScaledAxis( Axis.Z ).IsNearlyZero(SmallNumber) ) 
		{
			// just set to zero - avoids unsafe inverse of zero and duplicates what QNANs were resulting in before (scaling away all children)
			return new Matrix4x4();
		}
		else
		{
			Matrix4x4 invertedMatrix;
			Matrix4x4.Invert(matrix, out invertedMatrix);
			return invertedMatrix;
		}
	}


	public static Matrix4x4 CreateRotationTranslationMatrix(Rotator rot, Vector3 origin)
	{
		const float piOver180 = (float)Math.PI / 180.0f;
		float SR = (float)Math.Sin(rot.Roll * piOver180);
		float SP = (float)Math.Sin(rot.Pitch * piOver180);
		float SY = (float)Math.Sin(rot.Yaw * piOver180);
		float CR = (float)Math.Cos(rot.Roll * piOver180);
		float CP = (float)Math.Cos(rot.Pitch * piOver180);
		float CY = (float)Math.Cos(rot.Yaw * piOver180);

		return new Matrix4x4(
			CP * CY,
			CP * SY,
			SP,
			0.0f,

			SR * SP * CY - CR * SY,
			SR * SP * SY + CR * CY,
			- SR * CP,
			0.0f,

			-( CR * SP * CY + SR * SY ),
			CY * SR - CR * SP * SY,
			CR * CP,
			0.0f,

			origin.X,
			origin.Y,
			origin.Z,
			1.0f);
	}
}