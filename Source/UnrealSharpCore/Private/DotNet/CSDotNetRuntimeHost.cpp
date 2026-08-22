#include "DotNet/CSDotNetRuntimeHost.h"

#include "CSBindsRegistry.h"
#include "UnrealSharpCore.h"
#include "CSDotnetUtilties.h"
#include "CSManagedPluginCallbacks.h"
#include "CSPathsUtilities.h"
#include "HAL/PlatformProcess.h"
#include "Misc/Paths.h"
#include "Logging/StructuredLog.h"

using namespace UnrealSharp;

static_assert(sizeof(DotNetUtilities::FHostChar) == sizeof(char_t), "FHostChar does not match hostfxr's char_t.");

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
	const FString UnrealSharpLibraryAssembly = FPaths::ConvertRelativePathToFull(Paths::GetUnrealSharpPluginsPath());
	const FString UserWorkingDirectory = FPaths::ConvertRelativePathToFull(Paths::GetUserAssemblyDirectory());

	DotNetUtilities::FHostStringConversion AssemblyPathConv = StringCast<DotNetUtilities::FHostChar>(*UnrealSharpLibraryAssembly);
	DotNetUtilities::FHostStringConversion EntryPointClassConv = StringCast<DotNetUtilities::FHostChar>(*EntryPointClassName);
	DotNetUtilities::FHostStringConversion EntryPointFunctionConv = StringCast<DotNetUtilities::FHostChar>(*EntryPointFunctionName);

	FInitializeUnrealSharp InitializeUnrealSharp = nullptr;
	const int32 ErrorCode = LoadAssemblyAndGetFunctionPointer(
		reinterpret_cast<const char_t*>(AssemblyPathConv.Get()),
		reinterpret_cast<const char_t*>(EntryPointClassConv.Get()),
		reinterpret_cast<const char_t*>(EntryPointFunctionConv.Get()),
		UNMANAGEDCALLERSONLY_METHOD,
		nullptr,
		reinterpret_cast<void**>(&InitializeUnrealSharp));

	if (ErrorCode != 0 || !InitializeUnrealSharp)
	{
		UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to load assembly '{0}'. hostfxr error code: {1}", UnrealSharpLibraryAssembly, ErrorCode);
	}

	const FTCHARToUTF8 WorkingDirectoryUtf8(*UserWorkingDirectory);

	FCSInitializationResult InitializationResult;

	InitializeUnrealSharp(
		(const UTF8CHAR*)WorkingDirectoryUtf8.Get(),
		&GetManagedPluginCallbacks(),
		(const void*)&FCSBindsRegistry::GetBoundFunction,
		&GetManagedCallbacks(),
		&InitializationResult);

	if (!InitializationResult.bSuccess)
	{
		UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to initialize UnrealSharp! Exception:\n{0}", InitializationResult.Message);
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
		RuntimeHost = nullptr;
	}

	Hostfxr_InitForCommandLine = nullptr;
	Hostfxr_InitForRuntimeConfig = nullptr;
	Hostfxr_GetRuntimeDelegate = nullptr;
	Hostfxr_Close = nullptr;
}

FCSDotNetLayout FCSDotNetRuntimeHost::ResolveDotNetLayout(const FString& PluginAssemblyPath)
{
	const FString RuntimeDirectory = FPaths::GetPath(PluginAssemblyPath);

	FCSDotNetLayout Layout;
	Layout.AppAssemblyPath = PluginAssemblyPath;
	Layout.RuntimeConfigPath = DotNetUtilities::GetRuntimeConfigPath(PluginAssemblyPath);

	if (DotNetUtilities::IsSelfContainedDirectory(RuntimeDirectory))
	{
		Layout.DotNetRoot = RuntimeDirectory;
		Layout.HostFxrPath = FPaths::Combine(RuntimeDirectory, DotNetUtilities::GetHostFxrLibraryName());
		Layout.bSelfContained = true;
		return Layout;
	}

	Layout.bSelfContained = false;

	TArray<FString> CandidateRoots;
	CandidateRoots.Add(FPaths::Combine(RuntimeDirectory, TEXT(DOTNET_BUNDLED_FOLDER_NAME)));
	CandidateRoots.Add(FPaths::Combine(RuntimeDirectory, TEXT(".."), TEXT(DOTNET_BUNDLED_FOLDER_NAME)));
	CandidateRoots.Add(FPaths::Combine(FPaths::GetPath(FPlatformProcess::ExecutablePath()), TEXT(DOTNET_BUNDLED_FOLDER_NAME)));
	CandidateRoots.Add(DotNetUtilities::GetDotNetDirectory());

	for (FString& CandidateRoot : CandidateRoots)
	{
		FPaths::CollapseRelativeDirectories(CandidateRoot);

		if (!DotNetUtilities::IsSharedFrameworkRoot(CandidateRoot))
		{
			continue;
		}

		const FString HostFxrPath = DotNetUtilities::GetLatestHostFxrPath(CandidateRoot);
		if (HostFxrPath.IsEmpty())
		{
			continue;
		}

		Layout.DotNetRoot = CandidateRoot;
		Layout.HostFxrPath = HostFxrPath;
		break;
	}

	return Layout;
}

load_assembly_and_get_function_pointer_fn FCSDotNetRuntimeHost::InitializeHost()
{
	const FString PluginAssemblyPath = FPaths::ConvertRelativePathToFull(Paths::GetUnrealSharpPluginsPath());

	const FCSDotNetLayout Layout = ResolveDotNetLayout(PluginAssemblyPath);
	if (!Layout.IsValid())
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Could not resolve a .NET runtime layout for: {0}", PluginAssemblyPath);
		return nullptr;
	}

	UE_LOGFMT(LogUnrealSharp, Log, "AppAssemblyPath: {0}", Layout.AppAssemblyPath);
	UE_LOGFMT(LogUnrealSharp, Log, "RuntimeConfigPath: {0}", Layout.RuntimeConfigPath);
	UE_LOGFMT(LogUnrealSharp, Log, "DotNetRoot: {0}", Layout.DotNetRoot);
	UE_LOGFMT(LogUnrealSharp, Log, "HostFxrPath: {0}", Layout.HostFxrPath);

	RuntimeHost = FPlatformProcess::GetDllHandle(*Layout.HostFxrPath);
	if (!RuntimeHost)
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Failed to get the RuntimeHost DLL handle at: {0}", Layout.HostFxrPath);
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

	return ConfigureRuntime(Layout);
}

load_assembly_and_get_function_pointer_fn FCSDotNetRuntimeHost::ConfigureRuntime(const FCSDotNetLayout& Layout) const
{
	if (!FPaths::DirectoryExists(Layout.DotNetRoot))
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Dotnet directory does not exist at: {0}", Layout.DotNetRoot);
		return nullptr;
	}

	if (!FPaths::FileExists(Layout.RuntimeConfigPath))
	{
		UE_LOGFMT(LogUnrealSharp, Error, "No runtime config found at: {0}", Layout.RuntimeConfigPath);
		return nullptr;
	}

	UE_LOGFMT(LogUnrealSharp, Log, "Runtime config is self-contained: {0}",
	          DotNetUtilities::IsSelfContainedRuntimeConfig(Layout.RuntimeConfigPath));

	const FString ExecutablePath = FPlatformProcess::ExecutablePath();

	DotNetUtilities::FHostStringConversion DotNetRootConv = StringCast<DotNetUtilities::FHostChar>(*Layout.DotNetRoot);
	DotNetUtilities::FHostStringConversion HostPathConv = StringCast<DotNetUtilities::FHostChar>(*ExecutablePath);

	hostfxr_initialize_parameters InitializeParameters;
	InitializeParameters.size = sizeof(hostfxr_initialize_parameters);
	InitializeParameters.host_path = HostPathConv.Get();
	InitializeParameters.dotnet_root = DotNetRootConv.Get();

	hostfxr_handle HostFXR_Handle = nullptr;
	int32 ErrorCode;

	if (Layout.bSelfContained)
	{
		DotNetUtilities::FHostStringConversion AppAssemblyConv = StringCast<DotNetUtilities::FHostChar>(*Layout.AppAssemblyPath);
		const char_t* Args[] = { (AppAssemblyConv.Get()) };
		ErrorCode = Hostfxr_InitForCommandLine(UE_ARRAY_COUNT(Args), Args, &InitializeParameters, &HostFXR_Handle);
	}
	else
	{
		DotNetUtilities::FHostStringConversion RuntimeConfigConv = StringCast<DotNetUtilities::FHostChar>(*Layout.RuntimeConfigPath);
		ErrorCode = Hostfxr_InitForRuntimeConfig(reinterpret_cast<const char_t*>(RuntimeConfigConv.Get()), &InitializeParameters, &HostFXR_Handle);
	}

	if (ErrorCode != 0 || !HostFXR_Handle)
	{
		UE_LOGFMT(LogUnrealSharp, Error, "hostfxr_initialize failed with code: {0}. Set COREHOST_TRACE=1 and COREHOST_TRACEFILE=<path> for the full logging", ErrorCode);
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
