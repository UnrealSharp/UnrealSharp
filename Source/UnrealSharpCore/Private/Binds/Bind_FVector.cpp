#include "CSBindsRegistry.h"
#include "Math/Vector.h"

DECLARE_UNREALSHARP_BINDER(Bind_FVector)
{
	FVector FromRotator(FRotator Rotator)
	{
		return Rotator.Vector();
	}
	
	BIND_UNREALSHARP_FUNCTION(FromRotator)
}
