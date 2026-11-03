#include "CSDotnetUtilties.h"

#include "CSBuildUtilties.h"
#include "CSDialogUtilities.h"
#include "CSInstallationUtilities.h"
#include "CSPathsUtilities.h"
#include "CSProjectUtilities.h"
#include "UnrealSharpUtils.h"
#include "HAL/FileManager.h"
#include "Misc/FileHelper.h"
#if WITH_DEV_AUTOMATION_TESTS && PLATFORM_WINDOWS
#include "Misc/AutomationTest.h"
#include "Misc/Guid.h"
#endif

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

namespace
{
// A PATH entry can end at the dotnet directory without a trailing separator.
// Check for the executable itself instead of matching a path substring.
FString TryGetDotNetDirectory(const FString& Candidate)
{
	if (Candidate.IsEmpty())
	{
		return FString();
	}

#if defined(_WIN32)
	const FString DotNetExecutable = FPaths::Combine(Candidate, TEXT("dotnet.exe"));
#else
	const FString DotNetExecutable = FPaths::Combine(Candidate, TEXT("dotnet"));
#endif

	return FPaths::FileExists(DotNetExecutable) ? Candidate : FString();
}

FString FindDotNetDirectory(
	const FString& DotNetRoot,
	const FString& PathVariable,
	const FString& ProgramFiles)
{
	if (!DotNetRoot.IsEmpty())
	{
		const FString Directory = TryGetDotNetDirectory(DotNetRoot);
		if (!Directory.IsEmpty())
		{
			return Directory;
		}
	}

	TArray<FString> Paths;
	PathVariable.ParseIntoArray(Paths, FPlatformMisc::GetPathVarDelimiter());
	for (const FString& Path : Paths)
	{
		const FString Directory = TryGetDotNetDirectory(Path);
		if (!Directory.IsEmpty())
		{
			return Directory;
		}
	}

#if defined(_WIN32)
	if (!ProgramFiles.IsEmpty())
	{
		return TryGetDotNetDirectory(FPaths::Combine(ProgramFiles, TEXT("dotnet")));
	}
#else
	(void)ProgramFiles;
#endif

	return FString();
}
}

FString UnrealSharp::DotNetUtilities::GetDotNetDirectory()
{
#if WITH_EDITOR
	if (CVarSimulateNoDotNetSDK.GetValueOnAnyThread() == 1)
	{
		return FString();
	}
#endif

	const FString DotNetRoot = FPlatformMisc::GetEnvironmentVariable(TEXT("DOTNET_ROOT"));
	const FString DotNetRootDirectory = TryGetDotNetDirectory(DotNetRoot);
	if (!DotNetRootDirectory.IsEmpty())
	{
		return DotNetRootDirectory;
	}

#if defined(__APPLE__)
	const FString DefaultDotNetDirectory = TryGetDotNetDirectory(TEXT("/usr/local/share/dotnet/"));
	if (!DefaultDotNetDirectory.IsEmpty())
	{
		return DefaultDotNetDirectory;
	}
#endif

	const FString PathVariable = FPlatformMisc::GetEnvironmentVariable(TEXT("PATH"));
#if defined(_WIN32)
	const FString ProgramFiles = FPlatformMisc::GetEnvironmentVariable(TEXT("ProgramFiles"));
#else
	const FString ProgramFiles;
#endif

	return FindDotNetDirectory(FString(), PathVariable, ProgramFiles);
}

FString UnrealSharp::DotNetUtilities::GetDotNetExecutablePath()
{
#if defined(_WIN32)
	return FPaths::Combine(GetDotNetDirectory(), TEXT("dotnet.exe"));
#else
	return FPaths::Combine(GetDotNetDirectory(), TEXT("dotnet"));
#endif
}

#if WITH_DEV_AUTOMATION_TESTS && PLATFORM_WINDOWS
IMPLEMENT_SIMPLE_AUTOMATION_TEST(FDotNetDirectoryResolutionTest, "UnrealSharp.Utilities.DotNetDirectoryResolution", EAutomationTestFlags::EditorContext | EAutomationTestFlags::EngineFilter)

bool FDotNetDirectoryResolutionTest::RunTest(const FString& Parameters)
{
	(void)Parameters;

	const FString TestRoot = FPaths::Combine(
		FPaths::ProjectIntermediateDir(),
		TEXT("Automation"),
		TEXT("DotNetDirectoryTest"),
		FGuid::NewGuid().ToString(EGuidFormats::Digits));
	const FString DotNetRoot = FPaths::Combine(TestRoot, TEXT("custom-root"));
	const FString PathInstall = FPaths::Combine(TestRoot, TEXT("path-install"));
	const FString ProgramFiles = FPaths::Combine(TestRoot, TEXT("Program Files"));
	const FString FallbackInstall = FPaths::Combine(ProgramFiles, TEXT("dotnet"));

	const auto CreateFakeInstall = [](const FString& Directory)
	{
		IFileManager::Get().MakeDirectory(*Directory, true);
		return FFileHelper::SaveStringToFile(TEXT("test executable"), *FPaths::Combine(Directory, TEXT("dotnet.exe")));
	};

	TestTrue(TEXT("Create fake DOTNET_ROOT installation"), CreateFakeInstall(DotNetRoot));
	TestTrue(TEXT("Create fake PATH installation"), CreateFakeInstall(PathInstall));
	TestTrue(TEXT("Create fake Program Files installation"), CreateFakeInstall(FallbackInstall));

	TestEqual(
		TEXT("DOTNET_ROOT is preferred"),
		FindDotNetDirectory(DotNetRoot, PathInstall, ProgramFiles),
		DotNetRoot);
	TestEqual(
		TEXT("PATH entries work without a trailing separator or Program Files path"),
		FindDotNetDirectory(FString(), PathInstall, FString()),
		PathInstall);
	TestEqual(
		TEXT("Program Files is used as a fallback"),
		FindDotNetDirectory(FString(), FString(), ProgramFiles),
		FallbackInstall);
	TestEqual(
		TEXT("An invalid DOTNET_ROOT falls through to PATH"),
		FindDotNetDirectory(FPaths::Combine(TestRoot, TEXT("missing-root")), PathInstall, ProgramFiles),
		PathInstall);
	TestTrue(
		TEXT("Directories without dotnet.exe are rejected"),
		FindDotNetDirectory(FString(), TestRoot, FString()).IsEmpty());

	IFileManager::Get().DeleteDirectory(*TestRoot, false, true);
	return true;
}
#endif

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
