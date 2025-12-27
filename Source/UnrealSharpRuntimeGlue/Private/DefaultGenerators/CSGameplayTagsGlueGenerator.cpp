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
	
	FGameplayTagContainer AllTags;
	GameplayTagsManager.RequestAllGameplayTags(AllTags, false);
	
	TArray<FGameplayTag> GameplayTagArray = AllTags.GetGameplayTagArray();
	GameplayTagArray.Sort([](const FGameplayTag& A, const FGameplayTag& B)
	{
		return A.ToString() < B.ToString();
	});
	
	for (const FGameplayTag& GameplayTag : GameplayTagArray)
	{
		TSharedPtr<FGameplayTagNode> TagSource = GameplayTagsManager.FindTagNode(GameplayTag);
		
		if (!TagSource.IsValid())
		{
			continue;
		}
		
		FName FirstSourceName = TagSource->GetFirstSourceName();
		if (FirstSourceName == TEXT("UnrealSharpCore"))
		{
			continue;
		}
		
		const FString TagName = GameplayTag.ToString();
		const FString TagNameVariable = TagName.Replace(TEXT("."), TEXT("_"));
		ScriptBuilder.AppendLine(FString::Printf(TEXT("public static readonly FGameplayTag %s = new(\"%s\");"), *TagNameVariable, *TagName));
	}

	ScriptBuilder.CloseBrace();
	SaveRuntimeGlue(ScriptBuilder, TEXT("GameplayTags"));
}
