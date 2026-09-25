#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_FMatrix)
{
	// FMatrix is alignas(16), but managed code cannot align its locals to 16 bytes. Take the destination as void*
	// and copy, so the compiler cannot use aligned SIMD stores on managed memory (SIGSEGV on Linux x64).
	void FromRotator(void* OutMatrix, const FRotator Rotator)
	{
		const FMatrix Matrix = Rotator.Quaternion().ToMatrix();
		FMemory::Memcpy(OutMatrix, &Matrix, sizeof(FMatrix));
	}
	
	BIND_UNREALSHARP_FUNCTION(FromRotator)
}
