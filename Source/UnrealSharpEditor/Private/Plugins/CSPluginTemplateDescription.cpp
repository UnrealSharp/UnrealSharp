#include "Plugins/CSPluginTemplateDescription.h"

#include "CSCommonGlobalSettings.h"
#include "UnrealSharpEditor.h"
#include "Interfaces/IPluginManager.h"

void FCSPluginTemplateDescription::OnPluginCreated(const TSharedPtr<IPlugin> NewPlugin)
{
    FPluginTemplateDescription::OnPluginCreated(NewPlugin);
    const FString ModuleName = FString::Printf(TEXT("Managed%s"), *NewPlugin->GetName());
    const FString ProjectPath = NewPlugin->GetBaseDir() / UnrealSharp::GlobalSettings::Common::GetScriptDirectoryName();
    FUnrealSharpEditorModule::Get().AddNewProject(ModuleName, ProjectPath, NewPlugin->GetBaseDir());
}

