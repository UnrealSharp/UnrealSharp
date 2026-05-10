#include "CSPathsUtilities.h"

#include "CSCommonGlobalSettings.h"
#include "CSDotnetUtilties.h"
#include "CSProjectUtilities.h"
#include "UnrealSharpUtilities.h"
#include "Interfaces/IPluginManager.h"
#include "Logging/StructuredLog.h"

FString UnrealSharp::Paths::GetDotNetDirectory()
{
#if defined(__APPLE__)
    constexpr const TCHAR* DefaultDotNetPath = TEXT("/usr/local/share/dotnet/");
    if (FPaths::DirectoryExists(DefaultDotNetPath))
    {
       return DefaultDotNetPath;
    }
#endif

    const FString PathVariable = FPlatformMisc::GetEnvironmentVariable(TEXT("PATH"));
    TArray<FString> Paths;
    PathVariable.ParseIntoArray(Paths, FPlatformMisc::GetPathVarDelimiter());

#if defined(_WIN32)
    const FString PathMarker = TEXT("Program Files\\dotnet\\");
#else
    const FString PathMarker = TEXT("dotnet");
#endif

    for (const FString& Path : Paths)
    {
       if (!Path.Contains(PathMarker))
       {
          continue;
       }

       if (!FPaths::DirectoryExists(Path))
       {
          UE_LOGFMT(LogUnrealSharpUtilities, Warning, "Found path to DotNet, but the directory doesn't exist: {0}", Path);
          break;
       }

       return Path;
    }

    return {};
}

FString UnrealSharp::Paths::GetDotNetExecutablePath()
{
#if defined(_WIN32)
    return GetDotNetDirectory() + TEXT("dotnet.exe");
#else
    return GetDotNetDirectory() + TEXT("dotnet");
#endif
}

FString UnrealSharp::Paths::GetLatestHostFxrPath()
{
    const FString DotNetRoot = GetDotNetDirectory();
    const FString HostFxrRoot = FPaths::Combine(DotNetRoot, TEXT("host"), TEXT("fxr"));

    TArray<FString> Folders;
    IFileManager::Get().FindFiles(Folders, *(HostFxrRoot / TEXT("*")), true, true);

    FString HighestVersion;
    for (const FString& Folder : Folders)
    {
       if (HighestVersion.IsEmpty() || DotNetUtilities::IsVersionHigher(Folder, HighestVersion))
       {
          HighestVersion = Folder;
       }
    }

    if (HighestVersion.IsEmpty())
    {
       UE_LOGFMT(LogUnrealSharpUtilities, Fatal, "Failed to find hostfxr version in {0}", HostFxrRoot);
    }

    if (!DotNetUtilities::IsVersionGreaterOrEqual(HighestVersion, TEXT(DOTNET_MAJOR_VERSION)))
    {
       UE_LOGFMT(LogUnrealSharpUtilities, Fatal, "Hostfxr version {0} is less than the required version " DOTNET_MAJOR_VERSION, HighestVersion);
    }

#if defined(_WIN32)
    return FPaths::Combine(HostFxrRoot, HighestVersion, HOSTFXR_WINDOWS);
#elif defined(__APPLE__)
    return FPaths::Combine(HostFxrRoot, HighestVersion, HOSTFXR_MAC);
#else
    return FPaths::Combine(HostFxrRoot, HighestVersion, HOSTFXR_LINUX);
#endif
}

FString UnrealSharp::Paths::GetRuntimeHostPath()
{
#if WITH_EDITOR
    return GetLatestHostFxrPath();
#elif defined(_WIN32)
    return FPaths::Combine(GetPluginAssembliesPath(), HOSTFXR_WINDOWS);
#elif defined(__APPLE__)
    return FPaths::Combine(GetPluginAssembliesPath(), HOSTFXR_MAC);
#else
    return FPaths::Combine(GetPluginAssembliesPath(), HOSTFXR_LINUX);
#endif
}

FString UnrealSharp::Paths::GetRuntimeConfigPath()
{
    return GetPluginAssembliesPath() / TEXT("UnrealSharp.runtimeconfig.json");
}

FString& UnrealSharp::Paths::GetPluginDirectory()
{
    static FString PluginDirectory;

    if (PluginDirectory.IsEmpty())
    {
       TSharedPtr<IPlugin> Plugin = IPluginManager::Get().FindPlugin(UE_PLUGIN_NAME);
       check(Plugin);
       PluginDirectory = Plugin->GetBaseDir();
    }

    return PluginDirectory;
}

FString UnrealSharp::Paths::GetUnrealSharpDirectory()
{
    return FPaths::Combine(GetPluginDirectory(), TEXT("Managed"), TEXT("UnrealSharp"));
}

FString UnrealSharp::Paths::GetPluginAssembliesPath()
{
#if WITH_EDITOR
    return FPaths::Combine(GetPluginDirectory(), DotNetUtilities::GetManagedBinaries());
#else
    return GetUserAssemblyDirectory();
#endif
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
    return FPaths::Combine(GetUserAssemblyDirectory(), TEXT("AssemblyLoadOrder.json"));
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

const FString& UnrealSharp::Paths::GetProjectGlueFolderPath()
{
    static FString ProjectGlueFolderPath = GetScriptFolderDirectory() / Project::AppendGlueSuffix(FApp::GetProjectName());
    return ProjectGlueFolderPath;
}

FString UnrealSharp::Paths::GetPluginGlueFolderPath(const FString& PluginName)
{
    TSharedPtr<IPlugin> Plugin = IPluginManager::Get().FindPlugin(PluginName);
    
    if (!Plugin.IsValid())
    {
       UE_LOGFMT(LogUnrealSharpUtilities, Warning, "Plugin {0} not found. Can't get glue folder path.", PluginName);
       return {};
    }
    
    return FPaths::Combine(Plugin->GetBaseDir(), GlobalSettings::Common::GetScriptDirectoryName(), Project::AppendGlueSuffix(PluginName));
}

FString UnrealSharp::Paths::GetPathToManagedSolution()
{
    static FString SolutionPath = GetScriptFolderDirectory() / Project::GetUserManagedProjectName() + TEXT(".sln");
    return SolutionPath;
}