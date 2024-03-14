#pragma once

#include "CSManagedGCHandle.h"

struct FCSManagedPluginCallbacks
{
	using LoadPluginCallback = GCHandleIntPtr(__stdcall*)(const TCHAR*);
	using UnloadPluginCallback = bool(__stdcall*)(FGCHandle);
	
	LoadPluginCallback LoadPlugin = nullptr;
	UnloadPluginCallback UnloadPlugin = nullptr;
};

struct CSHARPFORUE_API FCSAssembly
{
	explicit FCSAssembly(const FString& InAssemblyPath) : AssemblyPath(InAssemblyPath)
	{
		AssemblyName = FPaths::GetBaseFilename(AssemblyPath);
	}

	bool Load();
	bool Unload() const;

	bool IsAssemblyValid() const;
	GCHandleIntPtr GetAssemblyHandle() const;

	void GetMetaDataPath(FString& OutPath) const;
	
	const FString& GetAssemblyPath() const { return AssemblyPath; }
	const FString& GetAssemblyName() const { return AssemblyName; }

private:
	
	FGCHandle AssemblyHandle;
	
	FString AssemblyPath;
	FString AssemblyName;
	
};
