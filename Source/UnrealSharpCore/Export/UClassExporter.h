#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "UClassExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UUClassExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static UFunction* GetNativeFunctionFromClassAndName(const UClass* Class, const char* FunctionName);

	UNREALSHARP_FUNCTION()
	static UFunction* GetNativeFunctionFromInstanceAndName(const UObject* NativeObject, const char* FunctionName);

	UNREALSHARP_FUNCTION()
	static void* GetDefaultFromName(const char* AssemblyName, const char* Namespace, const char* ClassName);

	UNREALSHARP_FUNCTION()
	static void* GetDefaultFromInstance(UObject* Object);
	
};
