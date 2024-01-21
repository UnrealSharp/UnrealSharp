using System.DoubleNumerics;
using System.Runtime.InteropServices;
using UnrealSharp.Attributes;
using UnrealSharp.Interop;

namespace UnrealSharp;

[UStruct(IsBlittable = true), StructLayout(LayoutKind.Sequential)]
public struct RandomStream : IEquatable<RandomStream>
{
	private int _initialSeed;
	private int _seed;
	public int CurrentSeed => _seed;
	
	public RandomStream(int initialSeed)
	{
		_initialSeed = initialSeed;
		_seed = initialSeed;
	}
	
	public void Initialize(int initialSeed)
	{
		_initialSeed = initialSeed;
		_seed = initialSeed;
	}
	
	public void Reset()
	{
		_seed = _initialSeed;
	}
	
	public static bool operator ==(RandomStream a, RandomStream b)
	{
		return a._seed == b._seed;
	}

	public static bool operator !=(RandomStream a, RandomStream b)
	{
		return !(a == b);
	}
	
	public bool Equals(RandomStream other)
	{
		return _initialSeed == other._initialSeed && _seed == other._seed;
	}

	public override bool Equals(object obj)
	{
		return obj is RandomStream other && Equals(other);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(_initialSeed, _seed);
	}

	public override string ToString()
	{
		return CurrentSeed.ToString();
	}

	public void GenerateNewSeed()
	{
		FRandomStreamExporter.CallGenerateNewSeed(ref this);
	}
	
	public float GetFraction( )
	{
		return FRandomStreamExporter.CallGetFraction(ref this);
	}

	public uint GetUnsignedInt( )
	{
		return FRandomStreamExporter.CallGetUnsignedInt(ref this);
	}
	
	public Vector3 GetUnitVector( )
	{
		return FRandomStreamExporter.CallGetUnitVector(ref this);
	}
	
	public int RandRange(int min, int max) 
	{
		return FRandomStreamExporter.CallRandRange(ref this, min, max);
	}
	
	public Vector3 GetUnitVectorInCone(Vector3 dir, float coneHalfAngleRad)
	{
		return FRandomStreamExporter.CallVRandCone(ref this, dir, coneHalfAngleRad);
	}

	public Vector3 GetUnitVectorInCone(Vector3 dir, float horizontalConeHalfAngleRad, float verticalConeHalfAngleRad)
	{
		return FRandomStreamExporter.CallVRandCone2(ref this, dir, horizontalConeHalfAngleRad, verticalConeHalfAngleRad);
	}
}