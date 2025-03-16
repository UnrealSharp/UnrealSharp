#include "FStringExporter.h"

void UFStringExporter::MarshalToNativeString(FString* String, TCHAR* ManagedString)
{
	*String = ManagedString;
}
