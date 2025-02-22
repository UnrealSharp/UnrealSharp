#pragma once

#include "CSNamespace.h"

struct UNREALSHARPCORE_API FCSFieldName
{
	FCSFieldName(FName Name, FName Namespace);
	FCSFieldName(const UClass* Class);

	FName GetName() const { return Name; }
	FString GetNameString() const { return Name.ToString(); }
	FCSNamespace GetNamespace() const { return Namespace; }
	UPackage* GetPackage() const { return Namespace.GetPackage(); }
	
	FName GetFullName() const
	{
		return *FString::Printf(TEXT("%s.%s"), *Namespace.GetFullNamespaceString(), *Name.ToString());
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
