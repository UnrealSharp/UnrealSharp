#pragma once

#include "CoreMinimal.h"
#include "UnrealSharpBinds.h"
#include "FNameExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class UNREALSHARPCORE_API UFNameExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static void NameToString(FName Name, FString& OutString);

	UNREALSHARP_FUNCTION()
	static void StringToName(FName& Name, const UTF16CHAR* String);
	
	UNREALSHARP_FUNCTION()
	static bool IsValid(FName Name);
	
};
