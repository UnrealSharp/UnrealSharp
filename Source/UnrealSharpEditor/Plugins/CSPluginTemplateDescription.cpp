#include "CSPluginTemplateDescription.h"
#include "Interfaces/IPluginManager.h"
#include "UnrealSharpEditor/UnrealSharpEditor.h"


void FCSPluginTemplateDescription::OnPluginCreated(const TSharedPtr<IPlugin> NewPlugin)
{
    FPluginTemplateDescription::OnPluginCreated(NewPlugin);

    const FString ModuleName = FString::Printf(TEXT("Managed%s"), *NewPlugin->GetName());
    const FString ProjectPath = NewPlugin->GetBaseDir() / "Script";
    const FString GlueProjectName = FString::Printf(TEXT("%s.Glue"), *NewPlugin->GetName());

    if (bRequiresPluginGlue)
    {
        CreateCodeModule(GlueProjectName, ProjectPath, GlueProjectName, NewPlugin->GetBaseDir(), false);
    }
    
    CreateCodeModule(ModuleName, ProjectPath, GlueProjectName, NewPlugin->GetBaseDir(), bRequiresPluginGlue);
}

void FCSPluginTemplateDescription::CreateCodeModule(const FString& ModuleName, const FString& ProjectPath,
    const FString& GlueProjectName, const FString& PluginPath, const bool bIncludeGlueProject)
{
    TMap<FString, FString> Arguments;
    Arguments.Add(TEXT("GlueProjectName"), GlueProjectName);

    if (!bIncludeGlueProject)
    {
        Arguments.Add(TEXT("SkipIncludeProjectGlue"), TEXT("true"));
    }

    FUnrealSharpEditorModule::Get().AddNewProject(ModuleName, ProjectPath, PluginPath, Arguments);
}

