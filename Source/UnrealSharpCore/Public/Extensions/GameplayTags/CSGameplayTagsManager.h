#pragma once

#include "CoreMinimal.h"
#include "NativeGameplayTags.h"
#include "UObject/Object.h"
#include "CSGameplayTagsManager.generated.h"

class UCSManagedAssembly;

typedef FName FAssemblyName;

UCLASS(Transient, meta = (InternalType))
class UCSGameplayTagsManager : public UObject
{
	GENERATED_BODY()
	
	static UCSGameplayTagsManager& Get();
	
public:
	
	UFUNCTION(meta=(ScriptMethod))
	static FGameplayTag AddTag_Runtime(const FString& TagName, const FString& DevComment);
	
	UFUNCTION(meta=(ScriptMethod))
	static FGameplayTag AddTag_Editor(const FString& AssemblyName, const FString& TagName, const FString& DevComment);
	
private:
	
	static TSharedPtr<FNativeGameplayTag> CreateTag(const FString& TagName, const FString& DevComment);
	static bool IsTagRegistered(const FString& TagName, FGameplayTag& OutExistingTag);
	
#if WITH_EDITOR
	void OnManagedAssemblyUnloaded(const UCSManagedAssembly* UnloadedAssembly);
	
	TMap<FAssemblyName, TArray<TSharedPtr<FNativeGameplayTag>>> RegisteredTags;
#else
	TArray<TSharedPtr<FNativeGameplayTag>> RegisteredTags;
#endif
	
	static UCSGameplayTagsManager* Instance;
};
