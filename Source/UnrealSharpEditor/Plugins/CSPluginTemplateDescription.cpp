// Fill out your copyright notice in the Description page of Project Settings.


#include "CSPluginTemplateDescription.h"

#include "Interfaces/IPluginManager.h"
#include "UnrealSharpEditor/UnrealSharpEditor.h"
#include "UnrealSharpProcHelper/CSProcHelper.h"


void FCSPluginTemplateDescription::OnPluginCreated(const TSharedPtr<IPlugin> NewPlugin)
{
    FPluginTemplateDescription::OnPluginCreated(NewPlugin);

    const FString ModuleName = FString::Printf(TEXT("Managed%s"), *NewPlugin->GetName());
    const FString ProjectPath = NewPlugin->GetBaseDir() / "Script";
    const FString GlueProjectName = FString::Printf(TEXT("%s.PluginGlue"), *NewPlugin->GetName());

    if (bRequiresPluginGlue)
    {
        CreateCodeModule(GlueProjectName, ProjectPath, GlueProjectName, NewPlugin->GetBaseDir(), false);
    }


    TMap<FString, FString> SolutionArguments;
    SolutionArguments.Add(TEXT("MODULENAME"), ModuleName);
    const FString ModuleFilePath = ProjectPath / ModuleName / ModuleName + ".cs";
    FUnrealSharpEditorModule::FillTemplateFile(TEXT("Module"), SolutionArguments, ModuleFilePath);
    CreateCodeModule(ModuleName, ProjectPath, GlueProjectName, NewPlugin->GetBaseDir(), bRequiresPluginGlue);
}

void FCSPluginTemplateDescription::CreateCodeModule(const FString& ModuleName, const FString& ProjectPath,
    const FString& GlueProjectName, const FString& PluginPath, const bool bIncludeGlueProject)
{
    TMap<FString, FString> Arguments;

    Arguments.Add(TEXT("NewProjectName"), ModuleName);
    Arguments.Add(TEXT("NewProjectFolder"), ProjectPath);
    Arguments.Add(TEXT("PluginPath"), PluginPath);
    Arguments.Add(TEXT("GlueProjectName"), GlueProjectName);

    if (!bIncludeGlueProject)
    {
        Arguments.Add(TEXT("SkipIncludeProjectGlue"), TEXT("true"));
    }

    FCSProcHelper::InvokeUnrealSharpBuildTool(BUILD_ACTION_GENERATE_PROJECT, Arguments);
}

