#include "CSQuatExtensions.h"

void UCSQuatExtensions::ToQuaternion(FQuat& Quaternion, const FRotator& Rotator)
{
	Quaternion = Rotator.Quaternion();
}

void UCSQuatExtensions::ToRotator(FRotator& Rotator, const FQuat& Quaternion)
{
	Rotator = Quaternion.Rotator();
}
