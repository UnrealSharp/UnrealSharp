// Fill out your copyright notice in the Description page of Project Settings.


#include "DefaultGenerators/CSGameplayTagsGlueGenerator.h"

#include "CSScriptBuilder.h"
#include "GameplayTagsModule.h"
#include "GameplayTagsSettings.h"

void UCSGameplayTagsGlueGenerator::Initialize()
{
	IGameplayTagsModule::OnTagSettingsChanged.AddUObject(this, &UCSGameplayTagsGlueGenerator::ProcessGameplayTags);
	IGameplayTagsModule::OnGameplayTagTreeChanged.AddUObject(this, &UCSGameplayTagsGlueGenerator::ProcessGameplayTags);
	ProcessGameplayTags();
}

void UCSGameplayTagsGlueGenerator::ProcessGameplayTags()
{
	TArray<const FGameplayTagSource*> Sources;
	UGameplayTagsManager& GameplayTagsManager = UGameplayTagsManager::Get();

	const int32 NumValues = StaticEnum<EGameplayTagSourceType>()->NumEnums();
	for (int32 Index = 0; Index < NumValues; Index++)
	{
		EGameplayTagSourceType SourceType = static_cast<EGameplayTagSourceType>(Index);
		GameplayTagsManager.FindTagSourcesWithType(SourceType, Sources);
	}

	FCSScriptBuilder ScriptBuilder(FCSScriptBuilder::IndentType::Tabs);
	ScriptBuilder.AppendLine(TEXT("using UnrealSharp.GameplayTags;"));
	ScriptBuilder.AppendLine();
	ScriptBuilder.AppendLine(TEXT("public static class GameplayTags"));
	ScriptBuilder.OpenBrace();

	TArray<FName> TagNames;
	auto GenerateGameplayTag = [&ScriptBuilder, &TagNames](const FGameplayTagTableRow& RowTag)
	{
		if (TagNames.Contains(RowTag.Tag))
		{
			return;
		}

		const FString TagName = RowTag.Tag.ToString();
		const FString TagNameVariable = TagName.Replace(TEXT("."), TEXT("_"));
		ScriptBuilder.AppendLine(
			FString::Printf(TEXT("public static readonly FGameplayTag %s = new(\"%s\");"), *TagNameVariable, *TagName));
		TagNames.Add(RowTag.Tag);
	};

	for (const FGameplayTagSource* Source : Sources)
	{
		if (Source->SourceTagList)
		{
			for (const FGameplayTagTableRow& RowTag : Source->SourceTagList->GameplayTagList)
			{
				GenerateGameplayTag(RowTag);
			}
		}

		if (Source->SourceRestrictedTagList)
		{
			for (const FGameplayTagTableRow& RowTag : Source->SourceRestrictedTagList->RestrictedGameplayTagList)
			{
				GenerateGameplayTag(RowTag);
			}
		}
	}

	ScriptBuilder.CloseBrace();
	SaveRuntimeGlue(ScriptBuilder, TEXT("GameplayTags"));
}
