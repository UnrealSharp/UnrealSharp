#pragma once

#include "CoreMinimal.h"
#include "Engine/BlueprintGeneratedClass.h"
#include "CSClass.generated.h"

struct FGCHandle;
struct FCSAssembly;
struct FCSharpClassInfo;

UCLASS()
class UNREALSHARPCORE_API UCSClass : public UBlueprintGeneratedClass
{
	GENERATED_BODY()
public:
	
	TSharedRef<const FCSharpClassInfo> GetClassInfo() const;
	TSharedRef<const FGCHandle> GetClassHandle() const;
	TSharedPtr<FCSAssembly> GetOwningAssembly() const;

	void SetClassInfo(const TSharedPtr<FCSharpClassInfo>& InClassMetaData);

private:
	TSharedPtr<FCSharpClassInfo> ClassInfo;
};