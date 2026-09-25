#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_FRotator)
{
	// FMatrix is 16-byte aligned, managed memory isn't.
	void FromMatrix(FRotator* Rotator, const void* InMatrix)
	{
		FMatrix Matrix;
		FMemory::Memcpy(&Matrix, InMatrix, sizeof(FMatrix));
		*Rotator = Matrix.Rotator();
	}
	
	BIND_UNREALSHARP_FUNCTION(FromMatrix)
}
