#pragma once

#include "ReflectionData/CSReflectionDataBase.h"

struct UNREALSHARPCORE_API FCSNamespace : FCSReflectionDataBase
{
	FCSNamespace(FName InNamespace = NAME_None) : Namespace(InNamespace)
	{
		
	}
	
	FName GetFName() const { return Namespace; }
	FString GetName() const { return Namespace.ToString(); }
	FString GetLastNamespace() const;

	bool GetParentNamespace(FCSNamespace& OutParent) const;
	bool IsValid() const { return Namespace != NAME_None; }

	UPackage* GetPackage() const;

	UPackage* TryGetAsNativePackage() const
	{
		FString NativePackageName = FString::Printf(TEXT("/Script/%s"), *GetLastNamespace());
		return FindPackage(nullptr, *NativePackageName);
	}
	
	FName GetPackageName() const { return *FString::Printf(TEXT("/Script/%s"), *Namespace.ToString()); }

	static FCSNamespace Invalid() { return FCSNamespace(); }

	bool operator == (const FCSNamespace& Other) const
	{
		return Namespace == Other.Namespace;
	}

	friend uint32 GetTypeHash(const FCSNamespace& InNamespace)
	{
		return GetTypeHash(InNamespace.Namespace);
	}

	// FCSReflectionDataBase interface
	virtual bool Serialize(FConstObject JsonObject) override;
	// End of FCSReflectionDataBase interface

private:
	FName Namespace;
};

