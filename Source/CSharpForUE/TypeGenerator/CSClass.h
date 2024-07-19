#pragma once

#include "CoreMinimal.h"
#include "UObject/Package.h"
#include "Engine/BlueprintGeneratedClass.h"
#include "CSClass.generated.h"

class FCSGeneratedClassBuilder;
class UCSFunction;
struct FCSharpClassInfo;

UCLASS()
class CSHARPFORUE_API UCSClass : public UBlueprintGeneratedClass
{
	GENERATED_BODY()
public:
	friend FCSGeneratedClassBuilder;

	static void InvokeManagedMethod(UObject* ObjectToInvokeOn, FFrame& Stack, RESULT_DECL);
	static void ProcessOutParameters(FOutParmRec* OutParameters, uint8* ArgumentBuffer);
	static bool InvokeManagedEvent(UObject* ObjectToInvokeOn, FFrame& Stack, const UCSFunction* Function, uint8* ArgumentBuffer, RESULT_DECL);
	bool bCanTick = true;

	TSharedRef<FCSharpClassInfo> GetClassInfo() const;

private:

	TSharedPtr<FCSharpClassInfo> ClassMetaData;
	
};