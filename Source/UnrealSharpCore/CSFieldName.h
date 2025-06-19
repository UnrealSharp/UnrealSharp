#pragma once

#include "CSNamespace.h"

struct UNREALSHARPCORE_API FCSFieldName
{
	FCSFieldName() = default;
	FCSFieldName(FName Name, FName Namespace) : Name(Name), Namespace(Namespace) {}
	FCSFieldName(UClass* Class);

	FName GetFName() const { return Name; }
	FString GetName() const { return Name.ToString(); }
	
	bool IsValid() const { return Name != NAME_None; }
	
	FCSNamespace GetNamespace() const { return Namespace; }
	UPackage* GetPackage() const { return Namespace.GetPackage(); }
	FName GetPackageName() const { return Namespace.GetPackageName(); }
	
	FName GetFullName() const
	{
		return *FString::Printf(TEXT("%s.%s"), *Namespace.GetName(), *Name.ToString());
	}

	bool operator == (const FCSFieldName& Other) const
	{
		return Name == Other.Name && Namespace == Other.Namespace;
	}

	friend uint32 GetTypeHash(const FCSFieldName& Field)
	{
		return GetTypeHash(Field.Name) ^ GetTypeHash(Field.Namespace);
	}
private:
	FName Name;
	FCSNamespace Namespace;
};
