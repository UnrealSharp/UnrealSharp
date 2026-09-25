#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_FRotator)
{
	// FMatrix is alignas(16), but a managed FMatrix is only 8-byte aligned. Copy it into an aligned local
	// before use, so the compiler cannot use aligned SIMD loads on managed memory.
	void FromMatrix(FRotator* Rotator, const void* InMatrix)
	{
		FMatrix Matrix;
		FMemory::Memcpy(&Matrix, InMatrix, sizeof(FMatrix));
		*Rotator = Matrix.Rotator();
	}
	
	BIND_UNREALSHARP_FUNCTION(FromMatrix)
}
