// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "Features/IPluginsEditorFeature.h"

/**
 *
 */
class UNREALSHARPEDITOR_API FCSPluginTemplateDescription final : public FPluginTemplateDescription
{
public:
    FCSPluginTemplateDescription(FText InName, FText InDescription, FString InOnDiskPath, const bool InCanContainContent,
        const EHostType::Type InModuleDescriptorType, const ELoadingPhase::Type InLoadingPhase = ELoadingPhase::Default,
        const bool InSupportsContentOnlyProjects = true, const bool InRequiresPluginGlue = true) : FPluginTemplateDescription(MoveTemp(InName), MoveTemp(InDescription),
            MoveTemp(InOnDiskPath), InCanContainContent, InModuleDescriptorType, InLoadingPhase, InSupportsContentOnlyProjects), bRequiresPluginGlue(InRequiresPluginGlue)
    {
    }

    virtual void OnPluginCreated(TSharedPtr<IPlugin> NewPlugin) override;

private:
    static void CreateCodeModule(const FString& ModuleName, const FString& ProjectPath, const FString& GlueProjectName, const FString& PluginPath, bool bIncludeGlueProject);

    bool bRequiresPluginGlue;
};
