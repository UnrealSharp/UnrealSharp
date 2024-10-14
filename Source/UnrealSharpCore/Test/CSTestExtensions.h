#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "CSTestExtensions.generated.h"

// Example of how to create an extension method in C++ for use in C#
// The meta=(ExtensionMethod) attribute is used to mark the function as an extension method
// Extensions methods needs to be:
// - Static
// - Public
// - BlueprintCallable or ScriptMethod
// - Have the meta=(ExtensionMethod) attribute
// - The first parameter must be a reference to the type that the extension method is extending
// - In BlueprintFunctionLibrary classes
UCLASS(meta = (NotGeneratorValid))
class UCSTestExtensions : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()

public:

	// Example of an extension method that takes an int parameter
	// public static void ExtensionMethod1(this Actor actor, int value)
	UFUNCTION(meta=(ExtensionMethod, ScriptMethod))
	static void ExtensionMethod1(AActor* Actor, int32 Value) {};

	// Example of an extension method that takes a float parameter
	// public static void ExtensionMethod2(this Actor actor, float value)
	UFUNCTION(meta=(ExtensionMethod, ScriptMethod))
	static void ExtensionMethod2(AActor* Actor, float Value) {};
	
};
