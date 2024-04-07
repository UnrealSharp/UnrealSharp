#pragma once

#include "CSManagedGCHandle.h"

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

		// Replace forward slashes with backslashes
		AssemblyPath.ReplaceInline(TEXT("/"), TEXT("\\"));
		
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
