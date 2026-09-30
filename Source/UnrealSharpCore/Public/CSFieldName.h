#pragma once

#include "CSFieldType.h"
#include "CSNamespace.h"

class UCSManagedAssembly;

struct UNREALSHARPCORE_API FCSFieldName : FCSReflectionDataBase
{
	FCSFieldName() = default;
	FCSFieldName(FName InSourceName, const FCSNamespace& InNamespace, FName InAssemblyName, ECSFieldType InFieldType)
		: SourceName(InSourceName)
		, Namespace(InNamespace)
		, AssemblyName(InAssemblyName)
	{
		if (InFieldType == ECSFieldType::Unknown || InFieldType == ECSFieldType::Enum)
		{
			EngineName = SourceName;
		}
		else
		{
			FString SourceNameString = *SourceName.ToString();
			SourceNameString.RemoveAt(0);
			EngineName = *SourceNameString;
		}
	}
	
	static FCSFieldName FromNativeBase(const UField* NativeField);

	FName GetSourceFName() const { return SourceName; }
	FString GetSourceName() const { return SourceName.ToString(); }

	FName GetEngineFName() const { return EngineName; }
	FString GetEngineName() const { return EngineName.ToString(); }

	FName GetAssemblyName() const { return AssemblyName; }

	bool IsValid() const { return !SourceName.IsNone(); }

	FCSNamespace GetNamespace() const { return Namespace; }
	UPackage* ResolvePackage() const { return Namespace.GetPackage(); }
	FName GetPackageName() const { return Namespace.GetPackageName(); }
	
	FString GetFullName() const
	{
		TStringBuilder<256> Builder;
		AppendFullName(Builder);
		return Builder.ToString();
	}
	
	UCSManagedAssembly* ResolveAssembly() const;
	
	template<typename T>
	T* ResolveField() const
	{
		return CastChecked<T>(ResolveField());
	}
	
	UField* ResolveField() const;
	
	void AppendFullName(FStringBuilderBase& Builder) const;

	bool operator==(const FCSFieldName& Other) const
	{
		return SourceName == Other.SourceName && Namespace == Other.Namespace && AssemblyName == Other.AssemblyName;
	}
	
	bool operator!=(const FCSFieldName& Other) const { return !(*this == Other); }

	friend uint32 GetTypeHash(const FCSFieldName& Field)
	{
		return HashCombineFast(GetTypeHash(Field.SourceName), GetTypeHash(Field.Namespace), GetTypeHash(Field.AssemblyName));
	}

	// FCSReflectionDataBase interface
	virtual bool Serialize(FConstObject JsonObject) override;
	// End of FCSReflectionDataBase interface

private:
	FName SourceName = NAME_None;
	FName EngineName = NAME_None;
	FCSNamespace Namespace;
	FName AssemblyName = NAME_None;
};
