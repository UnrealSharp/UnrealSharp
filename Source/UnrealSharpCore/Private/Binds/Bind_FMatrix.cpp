#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_FMatrix)
{
	void FromRotator(FMatrix* Matrix, const FRotator Rotator)
	{
		*Matrix = Rotator.Quaternion().ToMatrix();
	}
	
	BIND_UNREALSHARP_FUNCTION(FromRotator)
}

