#pragma once

#include "CoreMinimal.h"
#include "CSManagedMethod.h"
#include "CSFunction.generated.h"

class UCSClass;

UCLASS()
class UCSFunctionBase : public UFunction
{
	GENERATED_BODY()
public:

	UCSFunctionBase() : MethodHandle(nullptr) {}

	// UFunction interface
	virtual void Bind() override;
	// End of UFunction interface

	// Tries to update the method handle to the function pointer in C#.
	bool TryUpdateMethodHandle();

	// Gets the owning managed class of this function.
	UCSClass* GetOwningManagedClass() const;

	// Checks if this function is owned by a generated class.
	bool IsOwnedByGeneratedClass() const;

protected:
	static bool InvokeManagedEvent(UObject* ObjectToInvokeOn, FFrame& Stack, UCSFunctionBase* Function, uint8* ArgumentBuffer, RESULT_DECL);
private:
	FCSManagedMethod MethodHandle;
};
