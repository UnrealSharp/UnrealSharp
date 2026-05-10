#pragma once

#define HOSTFXR_WINDOWS "hostfxr.dll"
#define HOSTFXR_MAC "libhostfxr.dylib"
#define HOSTFXR_LINUX "libhostfxr.so"

#define STRINGIFY_IMPL(x) #x
#define STRINGIFY(x) STRINGIFY_IMPL(x)

#define DOTNET_MAJOR_VERSION_INT 10
#define DOTNET_MAJOR_VERSION STRINGIFY(DOTNET_MAJOR_VERSION_INT) ".0.0"
#define DOTNET_DISPLAY_NAME "net" STRINGIFY(DOTNET_MAJOR_VERSION_INT) ".0"

namespace UnrealSharp::DotNetUtilities
{
	UNREALSHARPUTILITIES_API bool VerifyCSharpEnvironment();
	
	UNREALSHARPUTILITIES_API FString& GetManagedBinaries();
	UNREALSHARPUTILITIES_API bool ParseDotNetVersion(const FString& VersionString, int32& OutMajor, int32& OutMinor, int32& OutPatch);
	UNREALSHARPUTILITIES_API bool IsVersionGreaterOrEqual(const FString& Version, const FString& MinVersion);
	UNREALSHARPUTILITIES_API bool IsVersionHigher(const FString& A, const FString& B);
};
