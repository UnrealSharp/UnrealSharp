#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_FRotator)
{
	void FromMatrix(FRotator* Rotator, const FMatrix& Matrix)
	{
		*Rotator = Matrix.Rotator();
	}
	
	BIND_UNREALSHARP_FUNCTION(FromMatrix)
}




