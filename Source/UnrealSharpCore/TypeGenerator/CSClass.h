#pragma once

#include "CoreMinimal.h"
#include "Engine/BlueprintGeneratedClass.h"
#include "CSClass.generated.h"

struct FGCHandle;
struct FCSAssembly;
struct FCSClassInfo;

UCLASS()
class UNREALSHARPCORE_API UCSClass : public UBlueprintGeneratedClass
{
	GENERATED_BODY()
public:
	
	TSharedPtr<FCSClassInfo> GetClassInfo() const;
	TSharedPtr<const FGCHandle> GetClassHandle() const;
	TSharedPtr<FCSAssembly> GetOwningAssembly() const;

	void SetClassInfo(const TSharedPtr<FCSClassInfo>& InClassMetaData);

private:
	TSharedPtr<FCSClassInfo> ClassInfo;
};