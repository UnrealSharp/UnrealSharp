#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "FTextExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UFTextExporter : public UObject
{
	GENERATED_BODY()
public:
	UNREALSHARP_FUNCTION()
	static const TCHAR* ToString(FText* Text);
	
	UNREALSHARP_FUNCTION()
	static void FromString(FText* Text, const char* String);

	UNREALSHARP_FUNCTION()
	static void FromName(FText* Text, FName Name);
	
	UNREALSHARP_FUNCTION()
	static void CreateEmptyText(FText* Text);
};
