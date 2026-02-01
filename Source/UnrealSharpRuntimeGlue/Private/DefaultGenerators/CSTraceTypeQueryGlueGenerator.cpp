// Fill out your copyright notice in the Description page of Project Settings.


#include "DefaultGenerators/CSTraceTypeQueryGlueGenerator.h"

FString SanitizeToCSharpIdentifier(const FString& InName)
{
	if (InName.IsEmpty())
	{
		return InName;
	}

	FString Result;
	Result.Reserve(InName.Len());

	for (int32 i = 0; i < InName.Len(); ++i)
	{
		TCHAR Char = InName[i];

		if (FChar::IsAlpha(Char) || Char == TEXT('_'))
		{
			Result.AppendChar(Char);
		}
		else if (FChar::IsDigit(Char))
		{
			if (Result.IsEmpty())
			{
				Result.AppendChar(TEXT('_'));
			}
			Result.AppendChar(Char);
		}
		else
		{
			Result.AppendChar(TEXT('_'));
		}
	}

	if (Result.IsEmpty())
	{
		return InName;
	}

	return Result;
}

void UCSTraceTypeQueryGlueGenerator::Initialize()
{
	UCollisionProfile* CollisionProfile = UCollisionProfile::Get();
	CollisionProfile->LoadProfileConfig(true);

	CollisionProfile->OnLoadProfileConfig.AddUObject(this, &ThisClass::OnCollisionProfileChanged);

	ProcessCollisionProfile();
}

void UCSTraceTypeQueryGlueGenerator::OnCollisionProfileChanged(UCollisionProfile* CollisionProfile)
{
	GEditor->GetTimerManager()->SetTimerForNextTick(FTimerDelegate::CreateUObject(this, &ThisClass::ProcessCollisionProfile));
}

void UCSTraceTypeQueryGlueGenerator::ProcessCollisionProfile()
{
	UCollisionProfile* CollisionProfile = UCollisionProfile::Get();

	FCSScriptBuilder ScriptBuilder(FCSScriptBuilder::IndentType::Tabs);
	ScriptBuilder.AppendLine();
	ScriptBuilder.AppendLine(TEXT("using UnrealSharp.Engine;"));
	ScriptBuilder.AppendLine(TEXT("using UnrealSharp.Core;"));
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
		ChannelName = SanitizeToCSharpIdentifier(ChannelName);

		ScriptBuilder.AppendLine(FString::Printf(TEXT("%s = %d,"), *ChannelName, i));
	}

	ScriptBuilder.CloseBrace();

	UEnum* ObjectTypeEnum = StaticEnum<EObjectTypeQuery>();

	ScriptBuilder.AppendLine();
	ScriptBuilder.AppendLine(TEXT("public enum EObjectChannel"));
	ScriptBuilder.OpenBrace();

	for (int32 i = 0; i < ObjectTypeQuery_MAX; i++)
	{
		FString ObjectTypeName = ObjectTypeEnum->GetMetaData(TEXT("ScriptName"), i);

		if (ObjectTypeName.IsEmpty())
		{
			continue;
		}
		
		ObjectTypeName.RemoveFromStart(TEXT("ECC_"));
		ObjectTypeName = SanitizeToCSharpIdentifier(ObjectTypeName);

		ScriptBuilder.AppendLine(FString::Printf(TEXT("%s = %d,"), *ObjectTypeName, i));
	}

	ScriptBuilder.CloseBrace();
	ScriptBuilder.AppendLine();
	
	ScriptBuilder.AppendLine(TEXT("public static class CollisionProfiles"));
	ScriptBuilder.OpenBrace();

	for (int i = 0; i < CollisionProfile->GetNumOfProfiles(); ++i)
	{
		const FCollisionResponseTemplate* Template = CollisionProfile->GetProfileByIndex(i);
		FString ProfileName = Template->Name.ToString();
		FString Code = FString::Printf(TEXT("public static readonly FName %s = new(\"%s\");"), *ProfileName, *ProfileName);
		ScriptBuilder.AppendLine(Code);
	}
	
	ScriptBuilder.CloseBrace();
	
	ScriptBuilder.AppendLine();
	
	ScriptBuilder.AppendLine(TEXT("public static class QueryChannelConverter"));
	ScriptBuilder.OpenBrace();
	
	ScriptBuilder.AppendLine(TEXT("public static ETraceTypeQuery ToTraceQuery(this ETraceChannel traceChannel) => (ETraceTypeQuery)traceChannel;"));
	ScriptBuilder.AppendLine(TEXT("public static ETraceChannel ToTraceChannel(this ETraceTypeQuery traceTypeQuery) => (ETraceChannel)traceTypeQuery;"));
	
	ScriptBuilder.AppendLine(TEXT("public static EObjectTypeQuery ToObjectQuery(this EObjectChannel objectChannel) => (EObjectTypeQuery)objectChannel;"));
	ScriptBuilder.AppendLine(TEXT("public static EObjectChannel ToObjectChannel(this EObjectTypeQuery objectTypeQuery) => (EObjectChannel)objectTypeQuery;"));
	
	ScriptBuilder.CloseBrace();

	SaveRuntimeGlue(ScriptBuilder, TEXT("TraceChannel"));
}
