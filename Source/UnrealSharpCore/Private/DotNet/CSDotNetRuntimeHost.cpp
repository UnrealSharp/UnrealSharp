#include "DotNet/CSDotNetRuntimeHost.h"

#include <vector>

#include "CSBindsRegistry.h"
#include "UnrealSharpCore.h"
#include "CSDotnetUtilties.h"
#include "CSInstallationUtilities.h"
#include "CSManagedPluginCallbacks.h"
#include "CSPathsUtilities.h"
#include "Misc/Paths.h"
#include "Logging/StructuredLog.h"

#ifdef _WIN32
#define PLATFORM_STRING(string) string
#else
#define PLATFORM_STRING(string) TCHAR_TO_ANSI(string)
#endif

#ifdef __clang__
#pragma clang diagnostic ignored "-Wdangling-assignment"
#endif

FCSDotNetRuntimeHost::~FCSDotNetRuntimeHost()
{
	ShutdownManagedRuntime();
}

bool FCSDotNetRuntimeHost::InitializeManagedRuntime()
{
	load_assembly_and_get_function_pointer_fn LoadAssemblyAndGetFunctionPointer = InitializeHost();
	if (!LoadAssemblyAndGetFunctionPointer)
	{
		UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to initialize Runtime Host. Check logs for more details.");
	}

	const FString EntryPointClassName = TEXT("UnrealSharp.Plugins.Main, UnrealSharp.Plugins");
	const FString EntryPointFunctionName = TEXT("InitializeUnrealSharp");
	const FString UnrealSharpLibraryAssembly = FPaths::ConvertRelativePathToFull(UnrealSharp::Paths::GetUnrealSharpPluginsPath());
	const FString UserWorkingDirectory = FPaths::ConvertRelativePathToFull(UnrealSharp::Paths::GetUserAssemblyDirectory());

	FInitializeRuntimeHost InitializeUnrealSharp = nullptr;
	const int32 ErrorCode = LoadAssemblyAndGetFunctionPointer(PLATFORM_STRING(*UnrealSharpLibraryAssembly),
		PLATFORM_STRING(*EntryPointClassName),
		PLATFORM_STRING(*EntryPointFunctionName),
		UNMANAGEDCALLERSONLY_METHOD,
		nullptr,
		reinterpret_cast<void**>(&InitializeUnrealSharp));

	if (ErrorCode != 0)
	{
		UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to load assembly: {0}", ErrorCode);
	}

	if (!InitializeUnrealSharp(*UserWorkingDirectory,
		*UnrealSharpLibraryAssembly,
		&GetManagedPluginCallbacks(),
		(const void*)&FCSBindsRegistry::GetBoundFunction,
		&GetManagedCallbacks()))
	{
		UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to initialize UnrealSharp!");
	}
	
#if !(UE_BUILD_SHIPPING)
	if (FParse::Param(FCommandLine::Get(), TEXT("-waitformanageddebugger")))
	{
		while (!FPlatformMisc::IsDebuggerPresent());
	}
#endif

	return true;
}

void FCSDotNetRuntimeHost::ShutdownManagedRuntime()
{
	if (RuntimeHost)
	{
		FPlatformProcess::FreeDllHandle(RuntimeHost);
	}
	
	Hostfxr_InitForCommandLine = nullptr;
	Hostfxr_InitForRuntimeConfig = nullptr;
	Hostfxr_GetRuntimeDelegate = nullptr;
	Hostfxr_Close = nullptr;
}

load_assembly_and_get_function_pointer_fn FCSDotNetRuntimeHost::InitializeHost()
{
	const FString RuntimeHostPath = UnrealSharp::DotNetUtilities::GetRuntimeHostPath();
	if (!FPaths::FileExists(RuntimeHostPath))
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Couldn't find Hostfxr at: {0}", RuntimeHostPath);
		return nullptr;
	}

	RuntimeHost = FPlatformProcess::GetDllHandle(*RuntimeHostPath);
	if (!RuntimeHost)
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Failed to get the RuntimeHost DLL handle at: {0}", RuntimeHostPath);
		return nullptr;
	}

	const bool BoundAllExports = BindExport(Hostfxr_InitForCommandLine, TEXT("hostfxr_initialize_for_dotnet_command_line"))
		& BindExport(Hostfxr_InitForRuntimeConfig, TEXT("hostfxr_initialize_for_runtime_config"))
		& BindExport(Hostfxr_GetRuntimeDelegate, TEXT("hostfxr_get_runtime_delegate"))
		& BindExport(Hostfxr_Close, TEXT("hostfxr_close"));

	if (!BoundAllExports)
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Failed to resolve all required exports from the Runtime Host.");
		FPlatformProcess::FreeDllHandle(RuntimeHost);
		RuntimeHost = nullptr;
		return nullptr;
	}

	return ConfigureRuntime();
}

load_assembly_and_get_function_pointer_fn FCSDotNetRuntimeHost::ConfigureRuntime() const
{
	const bool bIsInstalled = UnrealSharp::InstallationUtilities::IsUnrealSharpInstalled();
	const FString DotNetPath = bIsInstalled ? UnrealSharp::Paths::GetPluginAssembliesPath() : UnrealSharp::DotNetUtilities::GetDotNetDirectory();

	if (!FPaths::DirectoryExists(DotNetPath))
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Dotnet directory does not exist at: {0}", DotNetPath);
		return nullptr;
	}

	const FString RuntimeHostPath = UnrealSharp::DotNetUtilities::GetRuntimeHostPath();
	if (!FPaths::FileExists(RuntimeHostPath))
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Runtime host path does not exist at: {0}", RuntimeHostPath);
		return nullptr;
	}

	UE_LOGFMT(LogUnrealSharp, Log, "DotNetPath: {0}", DotNetPath);
	UE_LOGFMT(LogUnrealSharp, Log, "RuntimeHostPath: {0}", RuntimeHostPath);

	hostfxr_initialize_parameters InitializeParameters;
	InitializeParameters.dotnet_root = PLATFORM_STRING(*DotNetPath);
	InitializeParameters.host_path = PLATFORM_STRING(*RuntimeHostPath);
	InitializeParameters.size = sizeof(hostfxr_initialize_parameters);

	hostfxr_handle HostFXR_Handle = nullptr;
	int32 ErrorCode;

	if (bIsInstalled)
	{
		const FString PluginAssemblyPath = UnrealSharp::Paths::GetUnrealSharpPluginsPath();
		if (!FPaths::FileExists(PluginAssemblyPath))
		{
			UE_LOGFMT(LogUnrealSharp, Error, "UnrealSharp.Plugins.dll does not exist at: {0}", PluginAssemblyPath);
			return nullptr;
		}

		std::vector Args{PLATFORM_STRING(*PluginAssemblyPath)};

#if defined(_WIN32)
		ErrorCode = Hostfxr_InitForCommandLine(Args.size(), Args.data(), &InitializeParameters, &HostFXR_Handle);
#else
		ErrorCode = Hostfxr_InitForCommandLine(Args.size(), const_cast<const char**>(Args.data()),
		                                       &InitializeParameters, &HostFXR_Handle);
#endif
	}
	else
	{
		const FString RuntimeConfigPath = UnrealSharp::DotNetUtilities::GetRuntimeConfigPath();
		if (!FPaths::FileExists(RuntimeConfigPath))
		{
			UE_LOGFMT(LogUnrealSharp, Error, "No runtime config found at: {0}", RuntimeConfigPath);
			return nullptr;
		}

#if defined(_WIN32)
		ErrorCode = Hostfxr_InitForRuntimeConfig(PLATFORM_STRING(*RuntimeConfigPath), &InitializeParameters, &HostFXR_Handle);
#else
		ErrorCode = Hostfxr_InitForRuntimeConfig(PLATFORM_STRING(*RuntimeConfigPath), nullptr, &HostFXR_Handle);
#endif
	}

	if (ErrorCode != 0)
	{
		UE_LOGFMT(LogUnrealSharp, Error, "hostfxr_initialize failed with code: {0}", ErrorCode);
		return nullptr;
	}

	void* LoadAssemblyAndGetFunctionPointer = nullptr;
	ErrorCode = Hostfxr_GetRuntimeDelegate(HostFXR_Handle, hdt_load_assembly_and_get_function_pointer, &LoadAssemblyAndGetFunctionPointer);
	Hostfxr_Close(HostFXR_Handle);

	if (ErrorCode != 0 || !LoadAssemblyAndGetFunctionPointer)
	{
		UE_LOGFMT(LogUnrealSharp, Error, "hostfxr_get_runtime_delegate failed with code: {0}", ErrorCode);
		return nullptr;
	}

	return reinterpret_cast<load_assembly_and_get_function_pointer_fn>(LoadAssemblyAndGetFunctionPointer);
}
