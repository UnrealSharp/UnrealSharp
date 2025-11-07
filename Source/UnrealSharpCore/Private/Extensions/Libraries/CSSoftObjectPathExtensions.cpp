#include "Extensions/Libraries/CSSoftObjectPathExtensions.h"

UObject* UCSSoftObjectPathExtensions::ResolveObject(const FSoftObjectPath& SoftObjectPath)
{
	return SoftObjectPath.ResolveObject();
}
