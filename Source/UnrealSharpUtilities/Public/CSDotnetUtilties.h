#pragma once

#include "CoreMinimal.h"
#include "Containers/StringConv.h"

#define HOSTFXR_WINDOWS "hostfxr.dll"
#define HOSTFXR_MAC "libhostfxr.dylib"
#define HOSTFXR_LINUX "libhostfxr.so"

#define CORECLR_WINDOWS "coreclr.dll"
#define CORECLR_MAC "libcoreclr.dylib"
#define CORECLR_LINUX "libcoreclr.so"

#define STRINGIFY_IMPL(x) #x
#define STRINGIFY(x) STRINGIFY_IMPL(x)

#define DOTNET_MAJOR_VERSION_INT 10
#define DOTNET_MAJOR_VERSION STRINGIFY(DOTNET_MAJOR_VERSION_INT) ".0.0"
#define DOTNET_DISPLAY_NAME "net" STRINGIFY(DOTNET_MAJOR_VERSION_INT) ".0"

#define DOTNET_SHARED_FRAMEWORK_NAME "Microsoft.NETCore.App"
#define DOTNET_BUNDLED_FOLDER_NAME "DotNet"
#define DOTNET_RUNTIME_CONFIG_SUFFIX ".runtimeconfig.json"

namespace UnrealSharp::DotNetUtilities
{
#if PLATFORM_WINDOWS
	using FHostChar = WIDECHAR;
#else
	using FHostChar = UTF8CHAR;
#endif

	using FHostStringConversion = decltype(StringCast<FHostChar>(static_cast<const TCHAR*>(nullptr)));

#if WITH_EDITOR
	UNREALSHARPUTILITIES_API bool VerifyCSharpEnvironment();
	UNREALSHARPUTILITIES_API bool BuildUserSolution();
#endif

	UNREALSHARPUTILITIES_API FString& GetManagedBinaries();
	UNREALSHARPUTILITIES_API bool ParseDotNetVersion(const FString& VersionString, int32& OutMajor, int32& OutMinor, int32& OutPatch);
	UNREALSHARPUTILITIES_API bool IsVersionGreaterOrEqual(const FString& Version, const FString& MinVersion);
	UNREALSHARPUTILITIES_API bool IsVersionHigher(const FString& A, const FString& B);

	UNREALSHARPUTILITIES_API const TCHAR* GetHostFxrLibraryName();
	UNREALSHARPUTILITIES_API const TCHAR* GetCoreClrLibraryName();

	UNREALSHARPUTILITIES_API FString GetDotNetDirectory();
	UNREALSHARPUTILITIES_API FString GetDotNetExecutablePath();
	UNREALSHARPUTILITIES_API FString GetLatestHostFxrPath(const FString& DotNetRoot);

	UNREALSHARPUTILITIES_API FString GetRuntimeConfigPath(const FString& AssemblyPath);
	UNREALSHARPUTILITIES_API bool IsSelfContainedDirectory(const FString& Directory);
	UNREALSHARPUTILITIES_API bool IsSharedFrameworkRoot(const FString& DotNetRoot);
	UNREALSHARPUTILITIES_API bool IsSelfContainedRuntimeConfig(const FString& RuntimeConfigPath);
};
