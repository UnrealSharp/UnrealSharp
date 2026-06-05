#include "CSPathsUtilities.h"

#include "CSCommonGlobalSettings.h"
#include "CSDotnetUtilties.h"
#include "CSInstallationUtilities.h"
#include "CSProjectUtilities.h"
#include "Interfaces/IPluginManager.h"
#include "Logging/StructuredLog.h"

FString UnrealSharp::Paths::GetPluginDirectory()
{
    TSharedPtr<IPlugin> Plugin = IPluginManager::Get().FindPlugin(UE_PLUGIN_NAME);
    return Plugin->GetBaseDir();
}

FString UnrealSharp::Paths::GetUnrealSharpDirectory()
{
    return FPaths::Combine(GetPluginDirectory(), TEXT("Managed"), TEXT("UnrealSharp"));
}

FString UnrealSharp::Paths::GetPluginAssembliesPath()
{
    if (InstallationUtilities::IsUnrealSharpInstalled())
    {
        return GetUserAssemblyDirectory();
    }
    
    return FPaths::Combine(GetPluginDirectory(), DotNetUtilities::GetManagedBinaries());
}

FString UnrealSharp::Paths::GetUnrealSharpPluginsPath()
{
    return GetPluginAssembliesPath() / TEXT("UnrealSharp.Plugins.dll");
}

FString UnrealSharp::Paths::GetUnrealSharpBuildToolPath()
{
#if PLATFORM_WINDOWS || PLATFORM_MAC
    return FPaths::ConvertRelativePathToFull(GetPluginAssembliesPath() / TEXT("UnrealSharpBuildTool.dll"));
#else
    return FPaths::ConvertRelativePathToFull(GetPluginAssembliesPath() / TEXT("UnrealSharpBuildTool"));
#endif
}

FString UnrealSharp::Paths::GetUserAssemblyDirectory()
{
    return FPaths::ConvertRelativePathToFull(FPaths::Combine(FPaths::ProjectDir(), DotNetUtilities::GetManagedBinaries()));
}

FString UnrealSharp::Paths::GetUnrealSharpMetadataPath()
{
    return FPaths::Combine(GetUserAssemblyDirectory(), TEXT("Assembly.LoadOrder.json"));
}

FString UnrealSharp::Paths::GetGeneratedClassesDirectory()
{
    return FPaths::Combine(GetUnrealSharpDirectory(), TEXT("UnrealSharp"), TEXT("Generated"));
}

const FString& UnrealSharp::Paths::GetScriptFolderDirectory()
{
    static FString ScriptFolderDirectory = FPaths::Combine(FPaths::ProjectDir(), GlobalSettings::Common::GetScriptDirectoryName());
    return ScriptFolderDirectory;
}

const FString& UnrealSharp::Paths::GetPluginsDirectory()
{
    static FString PluginsDirectory = FPaths::Combine(FPaths::ProjectDir(), TEXT("Plugins"));
    return PluginsDirectory;
}

FString UnrealSharp::Paths::GetPathToManagedSolution()
{
    static FString SolutionPath = GetScriptFolderDirectory() / Project::GetUserManagedProjectName() + TEXT(".sln");
    return SolutionPath;
}

FString UnrealSharp::Paths::MakeQuotedPath(const FString& Path)
{
    if (Path.IsEmpty())
    {
        return TEXT("");
    }

    if (Path.StartsWith(TEXT("\"")) && Path.EndsWith(TEXT("\"")))
    {
        return Path;
    }

    return FString::Printf(TEXT("\"%s\""), *Path);
}
