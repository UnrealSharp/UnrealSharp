#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_FMatrix)
{
	// FMatrix is 16-byte aligned, managed memory isn't.
	void FromRotator(void* OutMatrix, const FRotator Rotator)
	{
		const FMatrix Matrix = Rotator.Quaternion().ToMatrix();
		FMemory::Memcpy(OutMatrix, &Matrix, sizeof(FMatrix));
	}
	
	BIND_UNREALSHARP_FUNCTION(FromRotator)
}
