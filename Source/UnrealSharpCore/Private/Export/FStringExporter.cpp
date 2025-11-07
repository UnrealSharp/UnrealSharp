#include "Export/FStringExporter.h"

void UFStringExporter::MarshalToNativeString(FString* String, TCHAR* ManagedString)
{
	*String = ManagedString;
}
