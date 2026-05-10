#include "CSDotnetUtilties.h"

#include "CSBuildUtilties.h"
#include "CSDialogUtilities.h"
#include "CSPathsUtilities.h"
#include "CSProjectUtilities.h"
#include "UnrealSharpUtils.h"

bool UnrealSharp::DotNetUtilities::VerifyCSharpEnvironment()
{
#if WITH_EDITOR
	FString DotNetInstallationPath = Paths::GetDotNetDirectory();
	if (DotNetInstallationPath.IsEmpty())
	{
		FString DialogText = FString::Printf(TEXT("UnrealSharp can't be initialized. An installation of .NET %s SDK can't be found on your system."), TEXT(DOTNET_MAJOR_VERSION));
		FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(DialogText));
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

		FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(DialogText));
		return false;
	}

	TArray<FString> ProjectPaths;
	Project::GetAllProjectPaths(ProjectPaths, true);
	
	if (ProjectPaths.IsEmpty())
	{
		return true;
	}
	
	bool IsStandalonePIE = FCSUnrealSharpUtils::IsStandalonePIE();
	bool IsUnattended = FApp::IsUnattended();
	if (!IsStandalonePIE && !IsUnattended && !Build::BuildUserSolution(Dialogs::MakeOkCancelDialogOnError()))
	{
		return false;
	}
	
#endif
	return true;
}

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
