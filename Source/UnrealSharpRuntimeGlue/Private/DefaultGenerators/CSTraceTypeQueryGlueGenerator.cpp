// Fill out your copyright notice in the Description page of Project Settings.


#include "DefaultGenerators/CSTraceTypeQueryGlueGenerator.h"

void UCSTraceTypeQueryGlueGenerator::Initialize()
{
	UCollisionProfile* CollisionProfile = UCollisionProfile::Get();
	CollisionProfile->LoadProfileConfig(true);

	CollisionProfile->OnLoadProfileConfig.AddUObject(this, &ThisClass::OnCollisionProfileChanged);

	ProcessTraceTypeQuery();
}

void UCSTraceTypeQueryGlueGenerator::OnCollisionProfileChanged(UCollisionProfile* CollisionProfile)
{
	GEditor->GetTimerManager()->SetTimerForNextTick(FTimerDelegate::CreateUObject(this, &ThisClass::ProcessTraceTypeQuery));
}

void UCSTraceTypeQueryGlueGenerator::ProcessTraceTypeQuery()
{
	// Initialize CollisionProfile in-case it's not loaded yet
	UCollisionProfile::Get();

	FCSScriptBuilder ScriptBuilder(FCSScriptBuilder::IndentType::Tabs);
	ScriptBuilder.AppendLine();
	ScriptBuilder.AppendLine(TEXT("using UnrealSharp.Engine;"));
	ScriptBuilder.AppendLine();
	ScriptBuilder.AppendLine(TEXT("public enum ETraceChannel"));
	ScriptBuilder.OpenBrace();

	// Hardcoded values for Visibility and Camera. See CollisionProfile.cpp:356
	{
		ScriptBuilder.AppendLine(TEXT("Visibility = 0,"));
		ScriptBuilder.AppendLine(TEXT("Camera = 1,"));
	}

	UEnum* TraceTypeQueryEnum = StaticEnum<ETraceTypeQuery>();
	constexpr int32 NumChannels = TraceTypeQuery_MAX;
	constexpr int32 StartIndex = 2;

	for (int i = StartIndex; i < NumChannels; i++)
	{
		if (TraceTypeQueryEnum->HasMetaData(TEXT("Hidden"), i) || i == NumChannels - 1)
		{
			continue;
		}

		FString ChannelName = TraceTypeQueryEnum->GetMetaData(TEXT("ScriptName"), i);
		ChannelName.RemoveFromStart(TEXT("ECC_"));
		ScriptBuilder.AppendLine(FString::Printf(TEXT("%s = %d,"), *ChannelName, i));
	}

	ScriptBuilder.CloseBrace();

	ScriptBuilder.AppendLine();
	ScriptBuilder.AppendLine(TEXT("public static class TraceChannelStatics"));
	ScriptBuilder.OpenBrace();

	ScriptBuilder.AppendLine(TEXT("public static ETraceTypeQuery ToQuery(this ETraceChannel traceTypeQueryHelper)"));
	ScriptBuilder.OpenBrace();
	ScriptBuilder.AppendLine(TEXT("return (ETraceTypeQuery)traceTypeQueryHelper;"));
	ScriptBuilder.CloseBrace();

	ScriptBuilder.CloseBrace();

	UEnum* ObjectTypeEnum = StaticEnum<EObjectTypeQuery>();

	ScriptBuilder.AppendLine();
	ScriptBuilder.AppendLine(TEXT("public enum ECollisionChannel"));
	ScriptBuilder.OpenBrace();

	for (int32 i = 0; i < ObjectTypeQuery_MAX; i++)
	{
		FString ObjectTypeName = ObjectTypeEnum->GetMetaData(TEXT("ScriptName"), i);

		if (ObjectTypeName.IsEmpty())
		{
			continue;
		}
		
		ObjectTypeName.RemoveFromStart(TEXT("ECC_"));
		ScriptBuilder.AppendLine(FString::Printf(TEXT("%s = %d,"), *ObjectTypeName, i));
	}

	ScriptBuilder.CloseBrace();
	ScriptBuilder.AppendLine();

	ScriptBuilder.AppendLine(TEXT("public static class CollisionChannelStatics"));
	ScriptBuilder.OpenBrace();
	ScriptBuilder.AppendLine(TEXT("public static EObjectTypeQuery ToObjectTypeQuery(this ECollisionChannel traceTypeQueryHelper)"));
	ScriptBuilder.OpenBrace();
	ScriptBuilder.AppendLine(TEXT("return (EObjectTypeQuery)traceTypeQueryHelper;"));
	ScriptBuilder.CloseBrace();
	ScriptBuilder.CloseBrace();

	SaveRuntimeGlue(ScriptBuilder, TEXT("TraceChannel"));
}
