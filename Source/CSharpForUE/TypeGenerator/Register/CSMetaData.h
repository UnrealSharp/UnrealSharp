#pragma once

#include "CoreMinimal.h"
#include "Misc/Guid.h"
#include "UObject/Script.h"
#include "Dom/JsonObject.h"
#include "UObject/ObjectMacros.h"

struct FDelegateMetaData;
struct FFunctionMetaData;
struct FTypeReferenceMetaData;
struct FClassMetaData;
struct FPropertyMetaData;
struct FStructMetaData;
struct FEnumMetaData;
struct FDefaultComponentMetaData;
struct FArrayPropertyMetaData;
struct FFunctionMetaData;
struct FObjectMetaData;

// Update this enum in PropertyType.cs in the UnrealSharpWeaver if you change this enum
UENUM()
enum class ECSPropertyType : uint8
{
	Unknown,

	Bool,

	Int8,
	Int16,
	Int,
	Int64,

	Byte,
	UInt16,
	UInt32,
	UInt64,

	Double,
	Float,

	Enum,

	Interface,
	Struct,
	Class,

	Object,
	ObjectPtr,
	DefaultComponent,
	LazyObject,
	WeakObject,

	SoftClass,
	SoftObject,

	Delegate,
	MulticastInlineDelegate,
	MulticastSparseDelegate,

	Array,
	Map,
	Set,
        
	String,
	Name,
	Text,

	GameplayTag,
	GameplayTagContainer,

	InternalNativeFixedSizeArray,
	InternalManagedFixedSizeArray
};

struct FMetaDataHelper
{
	static void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject, TMap<FString, FString>& MetaDataMap);
	static void ApplyMetaData(const TMap<FString, FString>& MetaDataMap, UField* Field);
	static void ApplyMetaData(const TMap<FString, FString>& MetaDataMap, FField* Field);
};

struct FTypeReferenceMetaData
{
	virtual ~FTypeReferenceMetaData() = default;

	FName Name;
	FName Namespace;
	FName AssemblyName;

	TMap<FString, FString> MetaData;
	
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject);
};

struct FMemberMetaData
{
	virtual ~FMemberMetaData() = default;

	FName Name;
	TMap<FString, FString> MetaData;
	
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject);;
};

struct FUnrealType
{
	virtual ~FUnrealType() = default;

	FName UnrealPropertyClass;
	ECSPropertyType PropertyType = ECSPropertyType::Unknown;
	int32 ArrayDim;

	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject);
	virtual void OnPropertyCreated(FProperty* Property) {};
};

struct FClassMetaData : FTypeReferenceMetaData
{
	virtual ~FClassMetaData() = default;

	FTypeReferenceMetaData ParentClass;
	
	TArray<FPropertyMetaData> Properties;
	
	TArray<FFunctionMetaData> Functions;
	TArray<FName> VirtualFunctions;
	
	TArray<FName> Interfaces;

	bool bCanTick = false;
	bool bOverrideInput = false;

	EClassFlags ClassFlags;

	FName ClassConfigName;

	// FTypeReferenceMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	// End of implementation
};

struct FClassPropertyMetaData : FUnrealType
{
	virtual ~FClassPropertyMetaData() = default;

	FTypeReferenceMetaData TypeRef;

	//FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	//End of implementation
};

struct FStructMetaData : FTypeReferenceMetaData
{
	virtual ~FStructMetaData() = default;

	TArray<FPropertyMetaData> Properties;

	//FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	//End of implementation
};

struct FStructPropertyMetaData : FUnrealType
{
	virtual ~FStructPropertyMetaData() = default;

	FTypeReferenceMetaData TypeRef;

	// FUnrealType interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	// End of implementation
};

struct FEnumPropertyMetaData : FUnrealType
{
	virtual ~FEnumPropertyMetaData() = default;

	FTypeReferenceMetaData InnerProperty;

	// FUnrealType interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	// End of implementation
};

struct FEnumMetaData : FTypeReferenceMetaData
{
	virtual ~FEnumMetaData() = default;

	TArray<FName> Items;

	//FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	//End of implementation
};

struct FPropertyMetaData : FMemberMetaData
{
	virtual ~FPropertyMetaData() = default;

	TSharedPtr<FUnrealType> Type;
	FName RepNotifyFunctionName;
	int32 ArrayDim = 0;
	EPropertyFlags PropertyFlags;
	ELifetimeCondition LifetimeCondition;

	FString BlueprintSetter;
	FString BlueprintGetter;

	//FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	//End of implementation

	template<typename T>
	TSharedPtr<T> GetTypeMetaData() const
	{
		return StaticCastSharedPtr<T>(Type);
	}
};

struct FArrayPropertyMetaData : FUnrealType
{
	virtual ~FArrayPropertyMetaData() = default;

	FPropertyMetaData InnerProperty;

	//FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	//End of implementation
};

struct FObjectMetaData : FUnrealType
{
	virtual ~FObjectMetaData() = default;

	FTypeReferenceMetaData InnerType;

	//FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	//End of implementation
};

struct FDefaultComponentMetaData : FObjectMetaData
{
	virtual ~FDefaultComponentMetaData() = default;

	bool IsRootComponent = false;
	FName AttachmentComponent;
	FName AttachmentSocket;

	//FUnrealType interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	//End of implementation
};

struct FFunctionMetaData : FMemberMetaData
{
	virtual ~FFunctionMetaData() = default;

	TArray<FPropertyMetaData> Parameters;
	FPropertyMetaData ReturnValue;
	bool IsVirtual = false;
	EFunctionFlags FunctionFlags;

	//FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	//End of implementation
};

struct FDelegateMetaData : FUnrealType
{
	virtual ~FDelegateMetaData() = default;

	FFunctionMetaData SignatureFunction;

	//FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	//End of implementation
};

struct FInterfaceMetaData : FTypeReferenceMetaData
{
	virtual ~FInterfaceMetaData() = default;

	TArray<FFunctionMetaData> Functions;
	
	//FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	//End of implementation
};

namespace CSharpMetaDataUtils
{
	void SerializeFunctions(const TArray<TSharedPtr<FJsonValue>>& FunctionsInfo, TArray<FFunctionMetaData>& FunctionMetaData);
	void SerializeProperties(const TArray<TSharedPtr<FJsonValue>>& PropertiesInfo, TArray<FPropertyMetaData>& PropertiesMetaData, EPropertyFlags DefaultFlags = CPF_None);
	void SerializeProperty(const TSharedPtr<FJsonObject>& PropertyMetaData, FPropertyMetaData& PropertiesMetaData, EPropertyFlags DefaultFlags = CPF_None);

	template<typename FlagType>
	FlagType GetFlags(const TSharedPtr<FJsonObject>& PropertyInfo, const FString& StringField);
}