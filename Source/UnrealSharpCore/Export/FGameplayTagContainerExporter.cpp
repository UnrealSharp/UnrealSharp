#include "FGameplayTagContainerExporter.h"

void UFGameplayTagContainerExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(HasTag);
	EXPORT_FUNCTION(HasTagExact);
	EXPORT_FUNCTION(HasAny);
	EXPORT_FUNCTION(HasAnyExact);
	EXPORT_FUNCTION(HasAll);
	EXPORT_FUNCTION(HasAllExact);
	EXPORT_FUNCTION(Filter);
	EXPORT_FUNCTION(FilterExact);
	EXPORT_FUNCTION(AppendTags);
	EXPORT_FUNCTION(AddTag);
	EXPORT_FUNCTION(AddTagFast);
	EXPORT_FUNCTION(AddLeafTag);
	EXPORT_FUNCTION(RemoveTag);
	EXPORT_FUNCTION(RemoveTags);
	EXPORT_FUNCTION(Reset);
	EXPORT_FUNCTION(ToString);
}

bool UFGameplayTagContainerExporter::HasTag(const FGameplayTagContainer* Container, const FGameplayTag* Tag)
{
	check(Container && Tag);
	return Container->HasTag(*Tag);
}

bool UFGameplayTagContainerExporter::HasTagExact(const FGameplayTagContainer* Container, const FGameplayTag* Tag)
{
	check(Container && Tag);
	return Container->HasTagExact(*Tag);
}

bool UFGameplayTagContainerExporter::HasAny(const FGameplayTagContainer* Container, const FGameplayTagContainer* OtherContainer)
{
	check(Container && OtherContainer);
	return Container->HasAny(*OtherContainer);
}

bool UFGameplayTagContainerExporter::HasAnyExact(const FGameplayTagContainer* Container, const FGameplayTagContainer* OtherContainer)
{
	check(Container && OtherContainer);
	return Container->HasAnyExact(*OtherContainer);
}

bool UFGameplayTagContainerExporter::HasAll(const FGameplayTagContainer* Container, const FGameplayTagContainer* OtherContainer)
{
	check(Container && OtherContainer);
	return Container->HasAll(*OtherContainer);
}

bool UFGameplayTagContainerExporter::HasAllExact(const FGameplayTagContainer* Container, const FGameplayTagContainer* OtherContainer)
{
	check(Container && OtherContainer);
	return Container->HasAllExact(*OtherContainer);
}

FGameplayTagContainer UFGameplayTagContainerExporter::Filter(const FGameplayTagContainer* Container, const FGameplayTagContainer* OtherContainer)
{
	check(Container && OtherContainer);
	return Container->Filter(*OtherContainer);
}

FGameplayTagContainer UFGameplayTagContainerExporter::FilterExact(const FGameplayTagContainer* Container, const FGameplayTagContainer* OtherContainer)
{
	check(Container && OtherContainer);
	return Container->FilterExact(*OtherContainer);
}

void UFGameplayTagContainerExporter::AppendTags(FGameplayTagContainer* Container, const FGameplayTagContainer* OtherContainer)
{
	check(Container && OtherContainer);
	Container->AppendTags(*OtherContainer);
}

void UFGameplayTagContainerExporter::AddTag(FGameplayTagContainer* Container, const FGameplayTag* Tag)
{
	check(Container && Tag);
	Container->AddTag(*Tag);
}

void UFGameplayTagContainerExporter::AddTagFast(FGameplayTagContainer* Container, const FGameplayTag* Tag)
{
	check(Container && Tag);
	Container->AddTagFast(*Tag);
}

bool UFGameplayTagContainerExporter::AddLeafTag(FGameplayTagContainer* Container, const FGameplayTag* Tag)
{
	check(Container && Tag);
	return Container->AddLeafTag(*Tag);
}

void UFGameplayTagContainerExporter::RemoveTag(FGameplayTagContainer* Container, const FGameplayTag* Tag, bool bDeferParentTags)
{
	check(Container && Tag);
	Container->RemoveTag(*Tag);
}

void UFGameplayTagContainerExporter::RemoveTags(FGameplayTagContainer* Container, const FGameplayTagContainer* OtherContainer)
{
	check(Container && OtherContainer);
	Container->RemoveTags(*OtherContainer);
}

void UFGameplayTagContainerExporter::Reset(FGameplayTagContainer* Container)
{
	check(Container);
	Container->Reset();
}

void UFGameplayTagContainerExporter::ToString(const FGameplayTagContainer* Container, FString& String)
{
	check(Container);
	String = Container->ToString();
}
