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
	UGameplayTagsManager& GameplayTagsManager = UGameplayTagsManager::Get();

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
		return A.GetTagName().LexicalLess(B.GetTagName());
	});

	for (const FGameplayTag& GameplayTag : GameplayTagArray)
	{
		TSharedPtr<FGameplayTagNode> TagNode = GameplayTagsManager.FindTagNode(GameplayTag);
		if (!TagNode.IsValid())
		{
			continue;
		}
		
		const TArray<FName>& SourceNames = TagNode->GetAllSourceNames();
		if (SourceNames.Contains(TEXT("UnrealSharpCore")))
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
