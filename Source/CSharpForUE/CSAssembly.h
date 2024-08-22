#pragma once

#include "CSManagedGCHandle.h"

#if !defined(_WIN32)
#define __stdcall
#endif

struct FCSManagedPluginCallbacks
{
	using LoadPluginCallback = GCHandleIntPtr(__stdcall*)(const TCHAR*);
	using UnloadPluginCallback = bool(__stdcall*)(const TCHAR*);
	
	LoadPluginCallback LoadPlugin = nullptr;
	UnloadPluginCallback UnloadPlugin = nullptr;
};

struct CSHARPFORUE_API FCSAssembly
{
	explicit FCSAssembly(const FString& InAssemblyPath)
	{
		AssemblyPath = FPaths::ConvertRelativePathToFull(InAssemblyPath);

#if defined(_WIN32)
		// Replace forward slashes with backslashes
		AssemblyPath.ReplaceInline(TEXT("/"), TEXT("\\"));
#endif
		
		AssemblyName = FPaths::GetBaseFilename(AssemblyPath);
	}

	bool Load();
	bool Unload() const;

	bool IsAssemblyValid() const;
	
	const GCHandleIntPtr& GetAssemblyHandle() const { return Assembly.Handle; }
	const FString& GetAssemblyName() const { return AssemblyName; }
	const FString& GetAssemblyPath() const { return AssemblyPath; }

private:
	
	FGCHandle Assembly;
	
	FString AssemblyPath;
	FString AssemblyName;
	
};
