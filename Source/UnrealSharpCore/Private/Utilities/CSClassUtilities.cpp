#include "Utilities/CSClassUtilities.h"
#include "CSManager.h"

bool FCSClassUtilities::IsManagedType(const UClass* Class)
{
	return UCSManager::Get().IsManagedType(Class);
}
