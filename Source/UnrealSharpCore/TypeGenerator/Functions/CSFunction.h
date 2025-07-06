#pragma once

#include "CoreMinimal.h"
#include "CSManagedGCHandle.h"
#include "CSFunction.generated.h"

struct FGCHandle;
class UCSClass;

UCLASS()
class UCSFunctionBase : public UFunction
{
	GENERATED_BODY()
public:
	// UFunction interface
	virtual void Bind() override;
	// End of UFunction interface

	// Tries to update the method handle to the function pointer in C#.
	bool TryUpdateMethodHandle();
	
	bool IsOwnedByManagedClass() const;

	bool HasValidMethodHandle() const
	{
		return MethodHandle.IsValid() && !MethodHandle->IsNull();
	}

	static void InvokeManagedMethod(UObject* ObjectToInvokeOn, FFrame& Stack, RESULT_DECL);
private:
	TSharedPtr<FGCHandle> MethodHandle = nullptr;
};
