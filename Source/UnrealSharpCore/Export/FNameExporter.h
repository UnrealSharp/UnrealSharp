#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "FNameExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UFNameExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static void NameToString(FName Name, FString* OutString);

	UNREALSHARP_FUNCTION()
	static void StringToName(FName* Name, const UTF16CHAR* String);
	
	UNREALSHARP_FUNCTION()
	static bool IsValid(FName Name);
	
};
