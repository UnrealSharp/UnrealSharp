using System.Runtime.InteropServices;
using UnrealSharp.Interop;

namespace UnrealSharp.CoreUObject;

[StructLayout(LayoutKind.Sequential)]
public partial struct RandomStream
{
	public RandomStream(int initialSeed)
	{
		InitialSeed = initialSeed;
		Seed = initialSeed;
	}
	
	public void Initialize(int initialSeed)
	{
		InitialSeed = initialSeed;
		Seed = initialSeed;
	}
	
	public void Reset()
	{
		Seed = InitialSeed;
	}
	
	public static bool operator ==(RandomStream a, RandomStream b)
	{
		return a.Seed == b.Seed;
	}

	public static bool operator !=(RandomStream a, RandomStream b)
	{
		return !(a == b);
	}
	
	public bool Equals(RandomStream other)
	{
		return InitialSeed == other.InitialSeed && Seed == other.Seed;
	}

	public override bool Equals(object obj)
	{
		return obj is RandomStream other && Equals(other);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(InitialSeed, Seed);
	}

	public override string ToString()
	{
		return Seed.ToString();
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
	
	public Vector GetUnitVector( )
	{
		return FRandomStreamExporter.CallGetUnitVector(ref this);
	}
	
	public int RandRange(int min, int max) 
	{
		return FRandomStreamExporter.CallRandRange(ref this, min, max);
	}
	
	public Vector GetUnitVectorInCone(Vector dir, float coneHalfAngleRad)
	{
		return FRandomStreamExporter.CallVRandCone(ref this, dir, coneHalfAngleRad);
	}

	public Vector GetUnitVectorInCone(Vector dir, float horizontalConeHalfAngleRad, float verticalConeHalfAngleRad)
	{
		return FRandomStreamExporter.CallVRandCone2(ref this, dir, horizontalConeHalfAngleRad, verticalConeHalfAngleRad);
	}
}