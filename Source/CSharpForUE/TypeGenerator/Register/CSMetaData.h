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
        
	Str,
	Name,
	Text,

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
	FString Name;
	FString Namespace;
	FString AssemblyName;

	TMap<FString, FString> MetaData;
	
	virtual ~FTypeReferenceMetaData() = default;
	
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject);
};

struct FMemberMetaData
{
	FName Name;
	TMap<FString, FString> MetaData;
	
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject);;
};

struct FUnrealType
{
	FName UnrealPropertyClass;
	ECSPropertyType PropertyType = ECSPropertyType::Unknown;
	int32 ArrayDim;

	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject);
	virtual void OnPropertyCreated(FProperty* Property) {};
};

struct FClassMetaData : FTypeReferenceMetaData
{
	FTypeReferenceMetaData ParentClass;
	
	TArray<FPropertyMetaData> Properties;
	
	TArray<FFunctionMetaData> Functions;
	TArray<FString> VirtualFunctions;
	
	TArray<FString> Interfaces;

	bool bCanTick = false;
	bool bOverrideInput = false;

	EClassFlags ClassFlags;

	FString ClassConfigName;

	// FTypeReferenceMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	// End of implementation
};

struct FClassPropertyMetaData : FUnrealType
{
	FTypeReferenceMetaData TypeRef;

	//FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	//End of implementation
};

struct FStructMetaData : FTypeReferenceMetaData
{
	TArray<FPropertyMetaData> Properties;
	bool bIsDataTableStruct = false;

	//FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	//End of implementation
};

struct FStructPropertyMetaData : FUnrealType
{
	FTypeReferenceMetaData TypeRef;

	// FUnrealType interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	// End of implementation
};

struct FEnumPropertyMetaData : FUnrealType
{
	FTypeReferenceMetaData InnerProperty;

	// FUnrealType interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	// End of implementation
};

struct FEnumMetaData : FTypeReferenceMetaData
{
	TArray<FString> Items;

	//FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	//End of implementation
};

struct FPropertyMetaData : FMemberMetaData
{
	TSharedPtr<FUnrealType> Type;
	FString RepNotifyFunctionName;
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
	FPropertyMetaData InnerProperty;

	//FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	//End of implementation
};

struct FObjectMetaData : FUnrealType
{
	FTypeReferenceMetaData InnerType;

	//FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	//End of implementation
};

struct FDefaultComponentMetaData : FObjectMetaData
{
	bool IsRootComponent = false;
	FString AttachmentComponent;
	FString AttachmentSocket;

	//FUnrealType interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	virtual void OnPropertyCreated(FProperty* Property) override;
	//End of implementation
};

struct FFunctionMetaData : FMemberMetaData
{
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
	FFunctionMetaData SignatureFunction;

	//FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	//End of implementation
};

struct FInterfaceMetaData : FTypeReferenceMetaData
{
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