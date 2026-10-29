#include "CSDotnetUtilties.h"

#include "CSBuildUtilties.h"
#include "CSDialogUtilities.h"
#include "CSInstallationUtilities.h"
#include "CSPathsUtilities.h"
#include "CSProjectUtilities.h"
#include "UnrealSharpUtils.h"
#include "HAL/FileManager.h"
#include "HAL/PlatformProcess.h"
#include "Misc/FileHelper.h"
#include "Misc/Paths.h"

#if !defined(_WIN32)
#include <limits.h>
#include <stdlib.h>
#endif

namespace
{
	bool IsUnrealBundledDotNet(const FString& Directory)
	{
		return Directory.Replace(TEXT("\\"), TEXT("/")).Contains(TEXT("/Binaries/ThirdParty/DotNet/"));
	}

	bool HasSdkForMajorVersion(const FString& Directory)
	{
		TArray<FString> SdkVersions;
		IFileManager::Get().FindFiles(SdkVersions, *FPaths::Combine(Directory, TEXT("sdk"), TEXT("*")), false, true);

		const FString MajorPrefix = FString::Printf(TEXT("%d."), DOTNET_MAJOR_VERSION_INT);
		return SdkVersions.ContainsByPredicate([&MajorPrefix](const FString& Version) { return Version.StartsWith(MajorPrefix); });
	}

	// Same rules as TryResolveSdkHost in Build/Scripts/Utilities/DotNetUtilities.cs. Version-manager shims (mise, asdf)
	// have no sdk/ folder, and the engine's bundled .NET (set as DOTNET_ROOT by RunUAT) may be older than we need.
	bool IsDotNetSdkRoot(const FString& Directory)
	{
#if defined(_WIN32)
		const TCHAR* HostName = TEXT("dotnet.exe");
#else
		const TCHAR* HostName = TEXT("dotnet");
#endif
		return !IsUnrealBundledDotNet(Directory)
			&& FPaths::FileExists(FPaths::Combine(Directory, HostName))
			&& HasSdkForMajorVersion(Directory)
			&& !UnrealSharp::DotNetUtilities::GetLatestHostFxrPath(Directory).IsEmpty();
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

	FString ReadDotNetInstallLocationFile(const FString& FilePath)
	{
		FString Contents;
		return FFileHelper::LoadFileToString(Contents, *FilePath) ? Contents.TrimStartAndEnd() : FString();
	}

	// Last resort for GUI-launched editors, whose environment lacks DOTNET_ROOT and PATH entries set up by shell
	// profiles. Covers the locations dotnet-install.sh and Linux distro packages use: the global install-location
	// files defined by the .NET host resolution spec, common distro package roots, and the dotnet-install.sh default.
	FString FindDotNetSdkAtWellKnownLocations(TArray<FString>* OutProbedLocations)
	{
#if PLATFORM_CPU_ARM_FAMILY
		const TCHAR* InstallLocationArchFile = TEXT("/etc/dotnet/install_location_arm64");
#else
		const TCHAR* InstallLocationArchFile = TEXT("/etc/dotnet/install_location_x64");
#endif
		const FString HomeDotNet = FPaths::Combine(FPlatformProcess::UserHomeDir(), TEXT(".dotnet"));

		// Label (what to show in diagnostics) paired with the actual root to check (the two install_location
		// entries resolve to file contents, everything else checks the label itself).
		const TPair<FString, FString> Candidates[] =
		{
			{ InstallLocationArchFile, ReadDotNetInstallLocationFile(InstallLocationArchFile) },
			{ TEXT("/etc/dotnet/install_location"), ReadDotNetInstallLocationFile(TEXT("/etc/dotnet/install_location")) },
			{ TEXT("/usr/share/dotnet"), TEXT("/usr/share/dotnet") },
			{ TEXT("/usr/lib/dotnet"), TEXT("/usr/lib/dotnet") },
			{ TEXT("/usr/lib64/dotnet"), TEXT("/usr/lib64/dotnet") },
			{ HomeDotNet, HomeDotNet }
		};

		for (const TPair<FString, FString>& Candidate : Candidates)
		{
			if (OutProbedLocations)
			{
				OutProbedLocations->Add(Candidate.Key);
			}

			if (!Candidate.Value.IsEmpty() && IsDotNetSdkRoot(Candidate.Value))
			{
				return WithTrailingSlash(Candidate.Value);
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

FString UnrealSharp::DotNetUtilities::GetDotNetDirectory(TArray<FString>* OutProbedLocations)
{
#if WITH_EDITOR
	if (CVarSimulateNoDotNetSDK.GetValueOnAnyThread() == 1)
	{
		return FString();
	}
#endif

	const FString DotNetRootVariable = FPlatformMisc::GetEnvironmentVariable(TEXT("DOTNET_ROOT"));
	if (OutProbedLocations)
	{
		OutProbedLocations->Add(DotNetRootVariable.IsEmpty() ? TEXT("DOTNET_ROOT (not set)") : FString::Printf(TEXT("DOTNET_ROOT=%s"), *DotNetRootVariable));
	}
	if (!DotNetRootVariable.IsEmpty() && IsDotNetSdkRoot(DotNetRootVariable))
	{
		return WithTrailingSlash(DotNetRootVariable);
	}

#if defined(__APPLE__)
	constexpr const TCHAR* DefaultDotNetPath = TEXT("/usr/local/share/dotnet/");
	if (OutProbedLocations)
	{
		OutProbedLocations->Add(DefaultDotNetPath);
	}
	if (IsDotNetSdkRoot(DefaultDotNetPath))
	{
		return DefaultDotNetPath;
	}
#endif

	const FString PathVariable = FPlatformMisc::GetEnvironmentVariable(TEXT("PATH"));
	if (OutProbedLocations)
	{
		OutProbedLocations->Add(FString::Printf(TEXT("PATH=%s"), *PathVariable));
	}

	TArray<FString> Paths;
	PathVariable.ParseIntoArray(Paths, FPlatformMisc::GetPathVarDelimiter());

#if !defined(_WIN32)
	// Follows symlinks such as /usr/bin/dotnet.
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

		if (!IsDotNetSdkRoot(Path))
		{
			continue;
		}

		DotNetPathFromEnv = WithTrailingSlash(Path);
		break;
	}

#if !defined(_WIN32)
	if (DotNetPathFromEnv.IsEmpty())
	{
		DotNetPathFromEnv = FindDotNetSdkAtWellKnownLocations(OutProbedLocations);
	}
#endif

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
namespace
{
	// Built from the same probes GetDotNetDirectory() ran, so the list can't drift out of sync with them.
	FString BuildDotNetNotFoundMessage(const TArray<FString>& ProbedLocations)
	{
		FString Message = FString::Printf(TEXT("UnrealSharp can't be initialized. An installation of .NET %s SDK can't be found on your system.\n\nLooked in:\n"), TEXT(DOTNET_MAJOR_VERSION));

		for (const FString& Location : ProbedLocations)
		{
			Message += FString::Printf(TEXT("  - %s\n"), *Location);
		}

		Message += FString::Printf(TEXT("\nTo fix this:\n  - Install the .NET %d SDK: https://dotnet.microsoft.com/download\n  - Or "), DOTNET_MAJOR_VERSION_INT);

#if defined(_WIN32)
		Message += TEXT("set the DOTNET_ROOT environment variable to the SDK root");
#elif defined(__APPLE__)
		Message += TEXT("set DOTNET_ROOT to the SDK root (e.g. ~/.dotnet). GUI apps need it set with `launchctl setenv DOTNET_ROOT <path>`, a shell profile isn't enough");
#else
		Message += TEXT("set DOTNET_ROOT to the SDK root (e.g. ~/.dotnet). GUI apps need it in ~/.config/environment.d/*.conf, a shell profile isn't enough");
#endif

		Message += TEXT("\n  - Then restart the editor.");
		return Message;
	}
}

bool UnrealSharp::DotNetUtilities::VerifyCSharpEnvironment()
{
	TArray<FString> ProbedLocations;
	FString DotNetInstallationPath = GetDotNetDirectory(&ProbedLocations);
	if (DotNetInstallationPath.IsEmpty() && !InstallationUtilities::IsUnrealSharpInstalled())
	{
		Dialogs::ShowError(FText::FromString(BuildDotNetNotFoundMessage(ProbedLocations)));
		return false;
	}

	// RunUAT overrides DOTNET_ROOT and the managed editor runs `dotnet`, so child processes need this SDK first on PATH.
	if (!DotNetInstallationPath.IsEmpty())
	{
		const FString SdkDirectory = DotNetInstallationPath.LeftChop(1);

		TArray<FString> PathEntries;
		FPlatformMisc::GetEnvironmentVariable(TEXT("PATH")).ParseIntoArray(PathEntries, FPlatformMisc::GetPathVarDelimiter());
		PathEntries.RemoveAll([&](const FString& Entry) { return Entry == SdkDirectory || Entry == DotNetInstallationPath; });
		PathEntries.Insert(SdkDirectory, 0);

		FPlatformMisc::SetEnvironmentVar(TEXT("PATH"), *FString::Join(PathEntries, FPlatformMisc::GetPathVarDelimiter()));
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
