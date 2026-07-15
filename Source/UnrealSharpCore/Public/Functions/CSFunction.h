#pragma once

#include "CoreMinimal.h"
#include "CSManagedGCHandle.h"
#include "CSFunction.generated.h"

struct FGCHandle;
class FObjectPropertyBase;
class UCSClass;

UCLASS()
class UCSFunctionBase : public UFunction
{
	GENERATED_BODY()
public:
	// UFunction interface
	virtual void Bind() override;
	// End of UFunction interface
	
	bool UpdateMethodHandle();
	
	bool IsOwnedByManagedClass() const;

	bool HasValidMethodHandle() const
	{
		return MethodHandle.IsValid() && !MethodHandle->IsNull();
	}
	
	static void InvokeManagedMethod(UObject* ObjectToInvokeOn, FFrame& Stack, RESULT_DECL);
private:
	static FObjectPropertyBase* FindWorldContextProperty(UFunction* Function, bool& bHasWorldContextMetadata);
	void CacheWorldContextProperty();
	bool TryGetExplicitWorldContext(uint8* ParameterBuffer, UObject*& OutWorldContextObject) const;

	TSharedPtr<FGCHandle> MethodHandle = nullptr;
	FObjectPropertyBase* CachedWorldContextProperty = nullptr;
	bool bHasExplicitWorldContext = false;
	bool bWorldContextPropertyCached = false;
};
