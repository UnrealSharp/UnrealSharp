#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "GameplayTagContainer.h"
#include "FGameplayTagExporter.generated.h"

UCLASS(meta=(NotGeneratorValid))
class CSHARPFORUE_API UFGameplayTagExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:

	static bool MatchesTag(const FGameplayTag* Tag, const FGameplayTag* TagToMatch);
	static int32 MatchesTagDepth(const FGameplayTag* Tag, const FGameplayTag* TagToMatch);
	static bool MatchesAny(const FGameplayTag* Tag, const FGameplayTagContainer* TagsToMatch);
	static bool MatchesAnyExact(const FGameplayTag* Tag, const FGameplayTagContainer* TagsToMatch);
	
};
