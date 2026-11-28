#include "Extensions/Libraries/CSPrimitiveComponentExtensions.h"
#include "Components/PrimitiveComponent.h"

TArray<float> UCSPrimitiveComponentExtensions::GetCustomPrimitiveData(const UPrimitiveComponent* PrimitiveComponent)
{
	return PrimitiveComponent->GetCustomPrimitiveData().Data;
}
