#include "CSPropertyFactory.h"
#include "CSFunctionFactory.h"
#include "CSharpForUE/CSharpForUE.h"
#include "UObject/UnrealType.h"
#include "UObject/Class.h"
#include "CSharpForUE/TypeGenerator/Register/CSGeneratedEnumBuilder.h"
#include "CSharpForUE/TypeGenerator/Register/CSMetaDataUtils.h"
#include "TypeGenerator/Register/CSTypeRegistry.h"
#include "TypeGenerator/Register/MetaData/CSArrayPropertyMetaData.h"
#include "TypeGenerator/Register/MetaData/CSDelegateMetaData.h"
#include "TypeGenerator/Register/MetaData/CSEnumPropertyMetaData.h"
#include "TypeGenerator/Register/MetaData/CSMapPropertyMetaData.h"
#include "TypeGenerator/Register/MetaData/CSObjectMetaData.h"
#include "TypeGenerator/Register/MetaData/CSStructPropertyMetaData.h"

#if WITH_EDITOR
#include "Kismet2/BlueprintEditorUtils.h"
#endif

static TMap<ECSPropertyType, FMakeNewPropertyDelegate> MakeNewPropertyFunctionMap;

void FCSPropertyFactory ::InitializePropertyFactory()
{
	AddSimpleProperty<FFloatProperty>(ECSPropertyType::Float);
	AddSimpleProperty<FDoubleProperty>(ECSPropertyType::Double);
	
	AddSimpleProperty<FByteProperty>(ECSPropertyType::Byte);
	
	AddSimpleProperty<FInt16Property>(ECSPropertyType::Int16);
	AddSimpleProperty<FIntProperty>(ECSPropertyType::Int);
	AddSimpleProperty<FInt64Property>(ECSPropertyType::Int64);
	
	AddSimpleProperty<FUInt16Property>(ECSPropertyType::UInt16);
	AddSimpleProperty<FUInt32Property>(ECSPropertyType::UInt32);
	AddSimpleProperty<FUInt64Property>(ECSPropertyType::UInt64);
	
	AddSimpleProperty<FBoolProperty>(ECSPropertyType::Bool);
	
	AddSimpleProperty<FNameProperty>(ECSPropertyType::Name);
	AddSimpleProperty<FStrProperty>(ECSPropertyType::String);
	AddSimpleProperty<FTextProperty>(ECSPropertyType::Text);

	AddProperty(ECSPropertyType::DefaultComponent, &CreateObjectProperty);

	AddProperty(ECSPropertyType::Object, &CreateObjectProperty);
	AddProperty(ECSPropertyType::WeakObject, &CreateWeakObjectProperty);
	AddProperty(ECSPropertyType::SoftObject, &CreateSoftObjectProperty);
	AddProperty(ECSPropertyType::ObjectPtr, &CreateObjectPtrProperty);
	AddProperty(ECSPropertyType::SoftClass, &CreateSoftClassProperty);

	AddProperty(ECSPropertyType::Class, &CreateClassProperty);
	AddProperty(ECSPropertyType::Struct, &CreateStructProperty);
	AddProperty(ECSPropertyType::Array, &CreateArrayProperty);
	AddProperty(ECSPropertyType::Enum, &CreateEnumProperty);
	AddProperty(ECSPropertyType::MulticastInlineDelegate, &CreateMulticastDelegateProperty);
	AddProperty(ECSPropertyType::Delegate, &CreateDelegateProperty);

	AddProperty(ECSPropertyType::Map, &CreateMapProperty);
}

void FCSPropertyFactory::AddProperty(ECSPropertyType PropertyType, FMakeNewPropertyDelegate Function)
{
	MakeNewPropertyFunctionMap.Add(PropertyType, Function);
}

FProperty* FCSPropertyFactory::CreateObjectProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	return CreateObjectProperty<FObjectProperty>(Outer, PropertyMetaData);
}

FProperty* FCSPropertyFactory::CreateWeakObjectProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	return CreateObjectProperty<FWeakObjectProperty>(Outer, PropertyMetaData);
}

FProperty* FCSPropertyFactory::CreateSoftObjectProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	return CreateObjectProperty<FSoftObjectProperty>(Outer, PropertyMetaData);
}

FProperty* FCSPropertyFactory::CreateObjectPtrProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	return CreateObjectProperty<FObjectPtrProperty>(Outer, PropertyMetaData);
}

FProperty* FCSPropertyFactory::CreateSoftClassProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	TSharedPtr<FCSObjectMetaData> ObjectMetaData = PropertyMetaData.GetTypeMetaData<FCSObjectMetaData>();
	UClass* Class = FCSTypeRegistry::GetClassFromName(ObjectMetaData->InnerType.Name);

	FSoftClassProperty* SoftObjectProperty = CreateObjectProperty<FSoftClassProperty>(Outer, PropertyMetaData);
	SoftObjectProperty->SetMetaClass(Class);
	return SoftObjectProperty;
}

FProperty* FCSPropertyFactory::CreateClassProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	auto ClassMetaData = PropertyMetaData.GetTypeMetaData<FCSObjectMetaData>();
	UClass* Class = FCSTypeRegistry::GetClassFromName(ClassMetaData->InnerType.Name);
	
	FClassProperty* NewClassProperty = CreateSimpleProperty<FClassProperty>(Outer, PropertyMetaData);
	NewClassProperty->SetPropertyClass(UClass::StaticClass());
	NewClassProperty->SetMetaClass(Class);
	
	return NewClassProperty;
}

template <typename ObjectProperty>
ObjectProperty* FCSPropertyFactory::CreateObjectProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	auto ObjectMetaData = PropertyMetaData.GetTypeMetaData<FCSObjectMetaData>();
	UClass* Class = FCSTypeRegistry::GetClassFromName(ObjectMetaData->InnerType.Name);
	ObjectProperty* NewObjectProperty = CreateSimpleProperty<ObjectProperty>(Outer, PropertyMetaData);
	NewObjectProperty->SetPropertyClass(Class);
	
	if (FLinkerLoad::IsImportLazyLoadEnabled())
	{
#if ENGINE_MINOR_VERSION >= 4
		NewObjectProperty->SetPropertyFlags(CPF_TObjectPtrWrapper);
#else
		NewObjectProperty->SetPropertyFlags(CPF_UObjectWrapper);
#endif
	}
	
	return NewObjectProperty;
}

FProperty* FCSPropertyFactory::CreateStructProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	auto StructPropertyMetaData = PropertyMetaData.GetTypeMetaData<FCSStructPropertyMetaData>();
	UScriptStruct* Struct = FCSTypeRegistry::GetStructFromName(StructPropertyMetaData->TypeRef.Name);
	
	FStructProperty* StructProperty = CreateSimpleProperty<FStructProperty>(Outer, PropertyMetaData);
	StructProperty->Struct = Struct;
	
	return StructProperty;
}

FProperty* FCSPropertyFactory::CreateArrayProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	auto ArrayPropertyMetaData = PropertyMetaData.GetTypeMetaData<FCSArrayPropertyMetaData>();
	FArrayProperty* ArrayProperty = CreateSimpleProperty<FArrayProperty>(Outer, PropertyMetaData);
	ArrayProperty->Inner = CreateProperty(Outer, ArrayPropertyMetaData->InnerProperty);
	ArrayProperty->Inner->Owner = ArrayProperty;
	return ArrayProperty;
}

FProperty* FCSPropertyFactory::CreateEnumProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	const auto EnumPropertyMetaData = PropertyMetaData.GetTypeMetaData<FCSEnumPropertyMetaData>();
	
	UEnum* Enum = FCSTypeRegistry::GetEnumFromName(EnumPropertyMetaData->InnerProperty.Name);
	FEnumProperty* EnumProperty = CreateSimpleProperty<FEnumProperty>(Outer, PropertyMetaData);
	FByteProperty* UnderlyingProp = new FByteProperty(EnumProperty, "UnderlyingType", RF_Public);
	
	EnumProperty->SetEnum(Enum);
	EnumProperty->AddCppProperty(UnderlyingProp);
	
	return EnumProperty;
}

FProperty* FCSPropertyFactory::CreateDelegateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	TSharedPtr<FCSDelegateMetaData> DelegateMetaData = PropertyMetaData.GetTypeMetaData<FCSDelegateMetaData>();
	FDelegateProperty* DelegateProperty = CreateSimpleProperty<FDelegateProperty>(Outer, PropertyMetaData);
	DelegateProperty->SignatureFunction = FCSFunctionFactory::CreateFunctionFromMetaData(Outer->GetOwnerClass(), DelegateMetaData->SignatureFunction);
	return DelegateProperty;
}

FProperty* FCSPropertyFactory::CreateMulticastDelegateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	TSharedPtr<FCSDelegateMetaData> MulticastDelegateMetaData = PropertyMetaData.GetTypeMetaData<FCSDelegateMetaData>();

	UClass* Class = CastChecked<UClass>(Outer);
	UFunction* SignatureFunction = FCSFunctionFactory::CreateFunctionFromMetaData(Class, MulticastDelegateMetaData->SignatureFunction);

	FMulticastDelegateProperty* MulticastDelegateProperty = CreateSimpleProperty<FMulticastInlineDelegateProperty>(Outer, PropertyMetaData);
	MulticastDelegateProperty->SignatureFunction = SignatureFunction;
	return MulticastDelegateProperty;
}

FProperty* FCSPropertyFactory::CreateMapProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	TSharedPtr<FCSMapPropertyMetaData> MapPropertyMetaData = PropertyMetaData.GetTypeMetaData<FCSMapPropertyMetaData>();
	FMapProperty* MapProperty = CreateSimpleProperty<FMapProperty>(Outer, PropertyMetaData);
	MapProperty->KeyProp = CreateProperty(Outer, MapPropertyMetaData->KeyType);

	if (!CanBeHashed(MapProperty->KeyProp))
	{
		FText DialogText = FText::FromString(FString::Printf(TEXT("Data type cannot be used as a Key in %s.%s. Unsafe to use until fixed. Needs to be able to handle GetTypeHash."),
			*Outer->GetName(), *PropertyMetaData.Name.ToString()));
		FMessageDialog::Open(EAppMsgType::Ok, DialogText);
	}
	
	MapProperty->KeyProp->Owner = MapProperty;
	MapProperty->ValueProp = CreateProperty(Outer, MapPropertyMetaData->ValueType);
	MapProperty->ValueProp->Owner = MapProperty;
	return MapProperty;
}

template<typename PrimitiveProperty>
class FPrimitivePropertyWrapper
{
public:
	
	static FProperty* CallCreateSimpleProperty(UField* Object, const FCSPropertyMetaData& MetaData)
	{
		FProperty* NewProperty = FCSPropertyFactory::CreateSimpleProperty<PrimitiveProperty>(Object, MetaData);
		NewProperty->SetPropertyFlags(CPF_HasGetValueTypeHash);
		return NewProperty;
	}
	
};

template<typename PrimitiveProperty>
void FCSPropertyFactory::AddSimpleProperty(ECSPropertyType PropertyType)
{
	const FMakeNewPropertyDelegate Delegate = &FPrimitivePropertyWrapper<PrimitiveProperty>::CallCreateSimpleProperty;
	MakeNewPropertyFunctionMap.Add(PropertyType, Delegate);
}

FProperty* FCSPropertyFactory::CreateAndAssignProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FProperty* Property = CreateProperty(Outer, PropertyMetaData);

	if (!Property)
	{
		return nullptr;
	}

	//Register the property to the owner (struct, function, class)
	Outer->AddCppProperty(Property);
	PropertyMetaData.Type->OnPropertyCreated(Property);
	
	return Property;
}

FProperty* FCSPropertyFactory::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	if (const FMakeNewPropertyDelegate* DelegatePtr = MakeNewPropertyFunctionMap.Find(PropertyMetaData.Type->PropertyType))
	{
		const FMakeNewPropertyDelegate MakeNewProperty = *DelegatePtr;
		FProperty* NewProperty = MakeNewProperty(Outer, PropertyMetaData);

		if (!NewProperty)
		{
			return nullptr;
		}
		
		NewProperty->SetPropertyFlags(PropertyMetaData.PropertyFlags);
		NewProperty->SetBlueprintReplicationCondition(PropertyMetaData.LifetimeCondition);

#if WITH_EDITOR
		if (!PropertyMetaData.BlueprintSetter.IsEmpty())
		{
			NewProperty->SetMetaData("BlueprintSetter", *PropertyMetaData.BlueprintSetter);

			if (UFunction* BlueprintSetterFunction = CastChecked<UClass>(Outer)->FindFunctionByName(*PropertyMetaData.BlueprintSetter))
			{
				BlueprintSetterFunction->SetMetaData("BlueprintInternalUseOnly", TEXT("true"));
			}
		}

		if (!PropertyMetaData.BlueprintGetter.IsEmpty())
		{
			NewProperty->SetMetaData("BlueprintGetter", *PropertyMetaData.BlueprintGetter);
			
			if (UFunction* BlueprintGetterFunction = CastChecked<UClass>(Outer)->FindFunctionByName(*PropertyMetaData.BlueprintGetter))
			{
				BlueprintGetterFunction->SetMetaData("BlueprintInternalUseOnly", TEXT("true"));
			}
		}
#endif

		if (NewProperty->HasAnyPropertyFlags(CPF_Net))
		{
			UBlueprintGeneratedClass* OwnerClass = CastChecked<UBlueprintGeneratedClass>(Outer);
			++OwnerClass->NumReplicatedProperties;
			
			if (!PropertyMetaData.RepNotifyFunctionName.IsNone())
			{
				NewProperty->RepNotifyFunc = PropertyMetaData.RepNotifyFunctionName;
				NewProperty->SetPropertyFlags(CPF_Net | CPF_RepNotify);
			}
		}

		if (PropertyMetaData.IsArray)
		{
			NewProperty->SetPropertyFlags(NewProperty->PropertyFlags | CPF_ReferenceParm | CPF_OutParm);
		}
		
		FCSMetaDataUtils::ApplyMetaData(PropertyMetaData.MetaData, NewProperty);
		
		return NewProperty;
	}

	FText DisplayName = StaticEnum<ECSPropertyType>()->GetDisplayValueAsText(PropertyMetaData.Type->PropertyType);
	UE_LOG(LogUnrealSharp, Warning, TEXT("%hs: Property type with name %s doesn't exist. Can't create new property."), __FUNCTION__, *DisplayName.ToString());
	return nullptr;
}

void FCSPropertyFactory::GeneratePropertiesForType(UField* Outer, const TArray<FCSPropertyMetaData>& PropertiesMetaData)
{
	for (int32 Index = PropertiesMetaData.Num() - 1; Index >= 0; --Index)
	{
		CreateAndAssignProperty(Outer, PropertiesMetaData[Index]);
	}
}

template <class FieldClass>
FieldClass* FCSPropertyFactory::CreateSimpleProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FName PropertyName = PropertyMetaData.Name;
	
	if (EnumHasAnyFlags(PropertyMetaData.PropertyFlags, CPF_ReturnParm))
	{
		PropertyName = "ReturnValue";
	}

	FieldClass* NewProperty = new FieldClass(Outer, PropertyName, RF_Public);
	NewProperty->PropertyFlags = PropertyMetaData.PropertyFlags;
	
	return NewProperty;
}

bool FCSPropertyFactory::IsOutParameter(const FProperty* InParam)
{
	const bool bIsParam = InParam->HasAnyPropertyFlags(CPF_Parm);
	const bool bIsReturnParam = InParam->HasAnyPropertyFlags(CPF_ReturnParm);
	const bool bIsOutParam = InParam->HasAnyPropertyFlags(CPF_OutParm) && !InParam->HasAnyPropertyFlags(CPF_ConstParm);
	return bIsParam && !bIsReturnParam && bIsOutParam;
}

bool FCSPropertyFactory::CanBeHashed(const FProperty* InParam)
{
#if WITH_EDITOR
	if(InParam->IsA<FBoolProperty>())
	{
		return false;
	}

	if (InParam->IsA<FTextProperty>())
	{
		return false;
	}
	
	if (const FStructProperty* StructProperty = CastField<FStructProperty>(InParam))
	{
		return FBlueprintEditorUtils::StructHasGetTypeHash(StructProperty->Struct);
	}
#endif
	return true;
}



