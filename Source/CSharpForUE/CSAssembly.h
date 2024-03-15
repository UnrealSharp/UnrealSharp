#pragma once

#include "CSManagedGCHandle.h"

struct FCSManagedPluginCallbacks
{
	using LoadPluginCallback = GCHandleIntPtr(__stdcall*)(const TCHAR*, void*, void*);
	using UnloadPluginCallback = bool(__stdcall*)(GCHandleIntPtr);
	
	LoadPluginCallback LoadPlugin = nullptr;
	UnloadPluginCallback UnloadPlugin = nullptr;
};

struct CSHARPFORUE_API FCSAssembly
{
	explicit FCSAssembly(const FString& InAssemblyPath)
	{
		AssemblyPath = FPaths::ConvertRelativePathToFull(InAssemblyPath);
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
