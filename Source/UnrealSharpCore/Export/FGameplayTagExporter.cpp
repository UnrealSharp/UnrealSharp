#include "FGameplayTagExporter.h"

void UFGameplayTagExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(MatchesTag);
	EXPORT_FUNCTION(MatchesTagDepth);
	EXPORT_FUNCTION(MatchesAny);
	EXPORT_FUNCTION(MatchesAnyExact);
}

bool UFGameplayTagExporter::MatchesTag(const FGameplayTag* Tag, const FGameplayTag* TagToMatch)
{
	check(Tag && TagToMatch);
	return Tag->MatchesTag(*TagToMatch);
}

int32 UFGameplayTagExporter::MatchesTagDepth(const FGameplayTag* Tag, const FGameplayTag* TagToMatch)
{
	check(Tag && TagToMatch);
	return Tag->MatchesTagDepth(*TagToMatch);
}

bool UFGameplayTagExporter::MatchesAny(const FGameplayTag* Tag, const FGameplayTagContainer* TagsToMatch)
{
	check(Tag && TagsToMatch);
	return Tag->MatchesAny(*TagsToMatch);
}

bool UFGameplayTagExporter::MatchesAnyExact(const FGameplayTag* Tag, const FGameplayTagContainer* TagsToMatch)
{
	check(Tag && TagsToMatch);
	return Tag->MatchesAnyExact(*TagsToMatch);
}
