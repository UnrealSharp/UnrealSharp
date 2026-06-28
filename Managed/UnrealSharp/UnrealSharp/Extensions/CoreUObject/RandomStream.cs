using System.Runtime.InteropServices;
using UnrealSharp.Interop;

namespace UnrealSharp.CoreUObject;

public partial record struct FRandomStream
{
	public int InitialSeed { get; private set; }
	public uint Seed { get; private set; }
	
	public FRandomStream(int initialSeed)
	{
		InitialSeed = initialSeed;
		Seed = (uint) initialSeed;
	}
	
	public void Initialize(int initialSeed)
	{
		InitialSeed = initialSeed;
		Seed = (uint) initialSeed;
	}
	
	public void Reset()
	{
		Seed = (uint) InitialSeed;
	}
	
	public bool Equals(FRandomStream other)
	{
		return InitialSeed == other.InitialSeed && Seed == other.Seed;
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
		Bind_FRandomStream.CallGenerateNewSeed(ref this);
	}
	
	public float GetFraction( )
	{
		return Bind_FRandomStream.CallGetFraction(ref this);
	}

	public uint GetUnsignedInt( )
	{
		return Bind_FRandomStream.CallGetUnsignedInt(ref this);
	}
	
	public FVector GetUnitVector( )
	{
		return Bind_FRandomStream.CallGetUnitVector(ref this);
	}
	
	public int RandRange(int min, int max) 
	{
		return Bind_FRandomStream.CallRandRange(ref this, min, max);
	}
	
	public FVector GetUnitVectorInCone(FVector dir, float coneHalfAngleRad)
	{
		return Bind_FRandomStream.CallVRandCone(ref this, dir, coneHalfAngleRad);
	}

	public FVector GetUnitVectorInCone(FVector dir, float horizontalConeHalfAngleRad, float verticalConeHalfAngleRad)
	{
		return Bind_FRandomStream.CallVRandCone2(ref this, dir, horizontalConeHalfAngleRad, verticalConeHalfAngleRad);
	}
}