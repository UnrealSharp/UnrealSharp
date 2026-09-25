#include "CSDotnetUtilties.h"

#include "CSBuildUtilties.h"
#include "CSDialogUtilities.h"
#include "CSInstallationUtilities.h"
#include "CSPathsUtilities.h"
#include "CSProjectUtilities.h"
#include "UnrealSharpUtils.h"
#include "HAL/FileManager.h"
#include "Misc/FileHelper.h"
#include "Misc/Paths.h"

#if !defined(_WIN32)
#include <limits.h>
#include <stdlib.h>
#endif

namespace
{
	// An SDK root contains the dotnet host next to an sdk/ folder. Version-manager shims (mise, asdf) have no sdk/.
	bool IsDotNetSdkRoot(const FString& Directory)
	{
#if defined(_WIN32)
		const TCHAR* HostName = TEXT("dotnet.exe");
#else
		const TCHAR* HostName = TEXT("dotnet");
#endif
		return FPaths::FileExists(FPaths::Combine(Directory, HostName)) && FPaths::DirectoryExists(FPaths::Combine(Directory, TEXT("sdk")));
	}

	FString WithTrailingSlash(FString Directory)
	{
		if (!Directory.EndsWith(TEXT("/")) && !Directory.EndsWith(TEXT("\\")))
		{
			Directory += TEXT("/");
		}
		return Directory;
	}

#if !defined(_WIN32)
	// Finds the SDK root behind a dotnet executable on PATH, e.g. /usr/bin/dotnet -> /usr/lib/dotnet/dotnet.
	FString FindDotNetSdkRootOnPath(const TArray<FString>& PathEntries)
	{
		for (const FString& Entry : PathEntries)
		{
			const FString Candidate = FPaths::Combine(Entry, TEXT("dotnet"));
			if (!FPaths::FileExists(Candidate))
			{
				continue;
			}

			char ResolvedPath[PATH_MAX];
			if (realpath(TCHAR_TO_UTF8(*Candidate), ResolvedPath) == nullptr)
			{
				continue;
			}

			const FString SdkRoot = FPaths::GetPath(UTF8_TO_TCHAR(ResolvedPath));
			if (IsDotNetSdkRoot(SdkRoot))
			{
				return WithTrailingSlash(SdkRoot);
			}
		}

		return FString();
	}
#endif
}

static TAutoConsoleVariable<int32> CVarSimulateNoDotNetSDK(
	TEXT("UnrealSharp.SimulateNoDotNetSDK"),
	0,
	TEXT("Simulate an environment where no .NET SDK is installed. This is useful for testing the UnrealSharp installation experience. Note that this will not affect UnrealSharp's ability to find a bundled .NET runtime, so it can be used to test both installed and non-installed scenarios."),
	ECVF_Default);

const TCHAR* UnrealSharp::DotNetUtilities::GetHostFxrLibraryName()
{
#if defined(_WIN32)
	return TEXT(HOSTFXR_WINDOWS);
#elif defined(__APPLE__)
	return TEXT(HOSTFXR_MAC);
#else
	return TEXT(HOSTFXR_LINUX);
#endif
}

const TCHAR* UnrealSharp::DotNetUtilities::GetCoreClrLibraryName()
{
#if defined(_WIN32)
	return TEXT(CORECLR_WINDOWS);
#elif defined(__APPLE__)
	return TEXT(CORECLR_MAC);
#else
	return TEXT(CORECLR_LINUX);
#endif
}

FString UnrealSharp::DotNetUtilities::GetDotNetDirectory()
{
#if WITH_EDITOR
	if (CVarSimulateNoDotNetSDK.GetValueOnAnyThread() == 1)
	{
		return FString();
	}
#endif

	// DOTNET_ROOT is the standard way to point hosts at a user-local SDK (e.g. ~/.dotnet from dotnet-install.sh).
	const FString DotNetRootVariable = FPlatformMisc::GetEnvironmentVariable(TEXT("DOTNET_ROOT"));
	if (!DotNetRootVariable.IsEmpty() && IsDotNetSdkRoot(DotNetRootVariable))
	{
		return WithTrailingSlash(DotNetRootVariable);
	}

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

#if !defined(_WIN32)
	// Prefer the real SDK behind a dotnet executable on PATH (follows /usr/bin/dotnet style symlinks).
	const FString SdkRootOnPath = FindDotNetSdkRootOnPath(Paths);
	if (!SdkRootOnPath.IsEmpty())
	{
		return SdkRootOnPath;
	}
#endif

#if defined(_WIN32)
	const FString PathMarker = TEXT("Program Files\\dotnet\\");
#else
	const FString PathMarker = TEXT("dotnet");
#endif

	FString DotNetPathFromEnv;
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

		DotNetPathFromEnv = WithTrailingSlash(Path);
		break;
	}

	return DotNetPathFromEnv;
}

FString UnrealSharp::DotNetUtilities::GetDotNetExecutablePath()
{
#if defined(_WIN32)
	return GetDotNetDirectory() + TEXT("dotnet.exe");
#else
	return GetDotNetDirectory() + TEXT("dotnet");
#endif
}

FString UnrealSharp::DotNetUtilities::GetLatestHostFxrPath(const FString& DotNetRoot)
{
	if (DotNetRoot.IsEmpty())
	{
		return FString();
	}

	const FString FxrRoot = FPaths::Combine(DotNetRoot, TEXT("host"), TEXT("fxr"));

	TArray<FString> VersionFolders;
	IFileManager::Get().FindFiles(VersionFolders, *FPaths::Combine(FxrRoot, TEXT("*")), false, true);

	FString HighestVersion;
	for (const FString& Folder : VersionFolders)
	{
		if (HighestVersion.IsEmpty() || IsVersionHigher(Folder, HighestVersion))
		{
			HighestVersion = Folder;
		}
	}

	if (HighestVersion.IsEmpty() || !IsVersionGreaterOrEqual(HighestVersion, TEXT(DOTNET_MAJOR_VERSION)))
	{
		return FString();
	}

	const FString HostFxrPath = FPaths::Combine(FxrRoot, HighestVersion, GetHostFxrLibraryName());
	return FPaths::FileExists(HostFxrPath) ? HostFxrPath : FString();
}

FString UnrealSharp::DotNetUtilities::GetRuntimeConfigPath(const FString& AssemblyPath)
{
	FString RuntimeConfigPath = AssemblyPath;
	RuntimeConfigPath.RemoveFromEnd(TEXT(".dll"));
	return RuntimeConfigPath + TEXT(DOTNET_RUNTIME_CONFIG_SUFFIX);
}

bool UnrealSharp::DotNetUtilities::IsSelfContainedDirectory(const FString& Directory)
{
	if (Directory.IsEmpty())
	{
		return false;
	}

	return FPaths::FileExists(FPaths::Combine(Directory, GetHostFxrLibraryName()))
		&& FPaths::FileExists(FPaths::Combine(Directory, GetCoreClrLibraryName()));
}

bool UnrealSharp::DotNetUtilities::IsSharedFrameworkRoot(const FString& DotNetRoot)
{
	if (DotNetRoot.IsEmpty())
	{
		return false;
	}

	return FPaths::DirectoryExists(FPaths::Combine(DotNetRoot, TEXT("shared"), TEXT(DOTNET_SHARED_FRAMEWORK_NAME)))
		&& FPaths::DirectoryExists(FPaths::Combine(DotNetRoot, TEXT("host"), TEXT("fxr")));
}

bool UnrealSharp::DotNetUtilities::IsSelfContainedRuntimeConfig(const FString& RuntimeConfigPath)
{
	FString Contents;
	if (!FFileHelper::LoadFileToString(Contents, *RuntimeConfigPath))
	{
		UE_LOGFMT(LogUnrealSharpUtilities, Warning, "Could not read runtime config at: {0}", RuntimeConfigPath);
		return false;
	}

	return Contents.Contains(TEXT("includedFrameworks"));
}

#if WITH_EDITOR
bool UnrealSharp::DotNetUtilities::VerifyCSharpEnvironment()
{
	FString DotNetInstallationPath = GetDotNetDirectory();
	if (DotNetInstallationPath.IsEmpty() && !InstallationUtilities::IsUnrealSharpInstalled())
	{
		FString DialogText = FString::Printf(TEXT("UnrealSharp can't be initialized. An installation of .NET %s SDK can't be found on your system."), TEXT(DOTNET_MAJOR_VERSION));
		Dialogs::ShowError(FText::FromString(DialogText));
		return false;
	}

	// Child processes (RunUAT, MSBuild started by the managed editor code) look for `dotnet` on PATH, and RunUAT
	// replaces DOTNET_ROOT with the engine's bundled runtime, which has no SDK. If the SDK was found through
	// DOTNET_ROOT or a symlink, put its directory on this process's PATH so they use the same SDK.
	if (!DotNetInstallationPath.IsEmpty())
	{
		const FString SdkDirectory = DotNetInstallationPath.LeftChop(1);
		const FString PathVariable = FPlatformMisc::GetEnvironmentVariable(TEXT("PATH"));

		TArray<FString> PathEntries;
		PathVariable.ParseIntoArray(PathEntries, FPlatformMisc::GetPathVarDelimiter());

		if (!PathEntries.Contains(SdkDirectory) && !PathEntries.Contains(DotNetInstallationPath))
		{
			FPlatformMisc::SetEnvironmentVar(TEXT("PATH"), *(SdkDirectory + FPlatformMisc::GetPathVarDelimiter() + PathVariable));
		}
	}

	FString UnrealSharpLibraryPath = Paths::GetUnrealSharpPluginsPath();
	if (!FPaths::FileExists(UnrealSharpLibraryPath))
	{
		FString FullPath = FPaths::ConvertRelativePathToFull(UnrealSharpLibraryPath);
		FString DialogText = FString::Printf(TEXT(
			"The bindings library could not be found at the following location:\n%s\n\n"
			"Most likely, the bindings library failed to build due to invalid generated glue."
		), *FullPath);

		Dialogs::ShowError(FText::FromString(DialogText));
		return false;
	}

	return true;
}

bool UnrealSharp::DotNetUtilities::BuildUserSolution()
{
	TArray<FString> ProjectPaths;
	Project::GetAllProjectPaths(ProjectPaths);

	if (ProjectPaths.IsEmpty())
	{
		return true;
	}

	if (FCSUnrealSharpUtils::IsStandalonePIE() || FApp::IsUnattended())
	{
		return true;
	}

	return Build::BuildUserSolution(Dialogs::MakeOkCancelDialogOnError());
}
#endif

FString& UnrealSharp::DotNetUtilities::GetManagedBinaries()
{
	static FString ManagedBinaries = FPaths::Combine(TEXT("Binaries"), TEXT("Managed"), TEXT(DOTNET_DISPLAY_NAME));
	return ManagedBinaries;
}

bool UnrealSharp::DotNetUtilities::ParseDotNetVersion(const FString& VersionString, int32& OutMajor, int32& OutMinor, int32& OutPatch)
{
	TArray<FString> Parts;
	VersionString.ParseIntoArray(Parts, TEXT("."));

	if (Parts.Num() < 3)
	{
		return false;
	}

	OutMajor = FCString::Atoi(*Parts[0]);
	OutMinor = FCString::Atoi(*Parts[1]);
	OutPatch = FCString::Atoi(*Parts[2]);
	return true;
}

bool UnrealSharp::DotNetUtilities::IsVersionGreaterOrEqual(const FString& Version, const FString& MinVersion)
{
	int32 Major, Minor, Patch;
	int32 MinMajor, MinMinor, MinPatch;

	if (!ParseDotNetVersion(Version, Major, Minor, Patch) || !ParseDotNetVersion(MinVersion, MinMajor, MinMinor, MinPatch))
	{
		return false;
	}

	if (Major != MinMajor)
	{
		return Major > MinMajor;
	}

	if (Minor != MinMinor)
	{
		return Minor > MinMinor;
	}

	return Patch >= MinPatch;
}

bool UnrealSharp::DotNetUtilities::IsVersionHigher(const FString& A, const FString& B)
{
	int32 MajorA, MinorA, PatchA;
	int32 MajorB, MinorB, PatchB;

	if (!ParseDotNetVersion(A, MajorA, MinorA, PatchA) || !ParseDotNetVersion(B, MajorB, MinorB, PatchB))
	{
		return false;
	}

	if (MajorA != MajorB)
	{
		return MajorA > MajorB;
	}

	if (MinorA != MinorB)
	{
		return MinorA > MinorB;
	}

	return PatchA > PatchB;
}
