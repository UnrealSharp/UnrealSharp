#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "CSGameplayTagQueryExtensions.generated.h"

struct FGameplayTag;
struct FGameplayTagContainer;
struct FGameplayTagQuery;

UCLASS(meta = (InternalType))
class UCSGameplayTagQueryExtensions : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()

public:

	UFUNCTION(meta=(ScriptMethod))
	static bool Matches(const FGameplayTagQuery& Query, const FGameplayTagContainer& Tags);

	UFUNCTION(meta=(ScriptMethod))
	static bool IsEmpty(const FGameplayTagQuery& Query);

	UFUNCTION(meta=(ScriptMethod))
	static void Clear(UPARAM(ref) FGameplayTagQuery& Query);

	UFUNCTION(meta=(ScriptMethod))
	static void ReplaceTagsFast(UPARAM(ref) FGameplayTagQuery& Query, const FGameplayTagContainer& Tags);

	UFUNCTION(meta=(ScriptMethod))
	static void ReplaceTagFast(UPARAM(ref) FGameplayTagQuery& Query, const FGameplayTag& Tag);

	UFUNCTION(meta=(ScriptMethod))
	static void SetUserDescription(UPARAM(ref) FGameplayTagQuery& Query, const FString& UserDescription);

	UFUNCTION(meta=(ScriptMethod))
	static FString GetDescription(const FGameplayTagQuery& Query);

	UFUNCTION(meta=(ScriptMethod))
	static TArray<FGameplayTag> GetGameplayTagArray(const FGameplayTagQuery& Query);

	UFUNCTION(meta=(ScriptMethod))
	static bool Identical(const FGameplayTagQuery& Query, const FGameplayTagQuery& Other);

	UFUNCTION()
	static FGameplayTagQuery MakeQuery_MatchAnyTags(const FGameplayTagContainer& Tags);

	UFUNCTION()
	static FGameplayTagQuery MakeQuery_MatchAllTags(const FGameplayTagContainer& Tags);

	UFUNCTION()
	static FGameplayTagQuery MakeQuery_MatchNoTags(const FGameplayTagContainer& Tags);

	UFUNCTION()
	static FGameplayTagQuery MakeQuery_ExactMatchAnyTags(const FGameplayTagContainer& Tags);

	UFUNCTION()
	static FGameplayTagQuery MakeQuery_ExactMatchAllTags(const FGameplayTagContainer& Tags);

	UFUNCTION()
	static FGameplayTagQuery MakeQuery_MatchTag(const FGameplayTag& Tag);
};
