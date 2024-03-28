#include "CSPropertyFactory.h"
#include "CSFunctionFactory.h"
#include "CSharpForUE/CSharpForUE.h"
#include "UObject/UnrealType.h"
#include "UObject/Class.h"
#include "CSharpForUE/TypeGenerator/Register/CSGeneratedEnumBuilder.h"
#include "CSharpForUE/TypeGenerator/Register/CSMetaData.h"

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
	AddSimpleProperty<FStrProperty>(ECSPropertyType::Str);
	AddSimpleProperty<FTextProperty>(ECSPropertyType::Text);

	AddProperty(ECSPropertyType::DefaultComponent, &CreateObjectPtrProperty);

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
}

void FCSPropertyFactory::AddProperty(ECSPropertyType PropertyType, FMakeNewPropertyDelegate Function)
{
	MakeNewPropertyFunctionMap.Add(PropertyType, Function);
}

FProperty* FCSPropertyFactory::CreateObjectProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags)
{
	return CreateObjectProperty<FObjectProperty>(Outer, PropertyMetaData, PropertyFlags);
}

FProperty* FCSPropertyFactory::CreateWeakObjectProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags)
{
	return CreateObjectProperty<FWeakObjectProperty>(Outer, PropertyMetaData, PropertyFlags);
}

FProperty* FCSPropertyFactory::CreateSoftObjectProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags)
{
	return CreateObjectProperty<FSoftObjectProperty>(Outer, PropertyMetaData, PropertyFlags);
}

FProperty* FCSPropertyFactory::CreateObjectPtrProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags)
{
	return CreateObjectProperty<FObjectPtrProperty>(Outer, PropertyMetaData, PropertyFlags);
}

FProperty* FCSPropertyFactory::CreateSoftClassProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags)
{
	TSharedPtr<FObjectMetaData> ObjectMetaData = PropertyMetaData.GetTypeMetaData<FObjectMetaData>();
	UClass* Class = FCSTypeRegistry::GetClassFromName(*ObjectMetaData->InnerType.Name);

	FSoftClassProperty* SoftObjectProperty = CreateObjectProperty<FSoftClassProperty>(Outer, PropertyMetaData, PropertyFlags);
	SoftObjectProperty->SetMetaClass(Class);
	return SoftObjectProperty;
}

FProperty* FCSPropertyFactory::CreateClassProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags)
{
	auto ClassMetaData = PropertyMetaData.GetTypeMetaData<FObjectMetaData>();
	UClass* Class = FCSTypeRegistry::GetClassFromName(*ClassMetaData->InnerType.Name);
	
	FClassProperty* NewClassProperty = CreateSimpleProperty<FClassProperty>(Outer, PropertyMetaData, PropertyFlags);
	NewClassProperty->SetPropertyClass(UClass::StaticClass());
	NewClassProperty->SetMetaClass(Class);
	
	return NewClassProperty;
}

template <typename ObjectProperty>
ObjectProperty* FCSPropertyFactory::CreateObjectProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags)
{
	auto ObjectMetaData = PropertyMetaData.GetTypeMetaData<FObjectMetaData>();
	UClass* Class = FCSTypeRegistry::GetClassFromName(*ObjectMetaData->InnerType.Name);
	ObjectProperty* NewObjectProperty = CreateSimpleProperty<ObjectProperty>(Outer, PropertyMetaData, PropertyFlags);
	NewObjectProperty->SetPropertyClass(Class);
	return NewObjectProperty;
}

FProperty* FCSPropertyFactory::CreateStructProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags)
{
	auto StructPropertyMetaData = PropertyMetaData.GetTypeMetaData<FStructPropertyMetaData>();
	UScriptStruct* Struct = FCSTypeRegistry::GetStructFromName(*StructPropertyMetaData->TypeRef.Name);
	
	FStructProperty* StructProperty = CreateSimpleProperty<FStructProperty>(Outer, PropertyMetaData, PropertyFlags);
	StructProperty->Struct = Struct;
	
	return StructProperty;
}

FProperty* FCSPropertyFactory::CreateArrayProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags)
{
	auto ArrayPropertyMetaData = PropertyMetaData.GetTypeMetaData<FArrayPropertyMetaData>();
	FArrayProperty* ArrayProperty = CreateSimpleProperty<FArrayProperty>(Outer, PropertyMetaData, PropertyFlags);
	ArrayProperty->Inner = CreateProperty(Outer, ArrayPropertyMetaData->InnerProperty, PropertyFlags);
	return ArrayProperty;
}

FProperty* FCSPropertyFactory::CreateEnumProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags)
{
	const auto ArrayPropertyMetaData = PropertyMetaData.GetTypeMetaData<FEnumPropertyMetaData>();
	
	UEnum* Enum = nullptr;
	FEnumProperty* EnumProperty = CreateSimpleProperty<FEnumProperty>(Outer, PropertyMetaData, PropertyFlags);
	FByteProperty* UnderlyingProp = new FByteProperty(EnumProperty, "UnderlyingType", RF_Public);
	
	EnumProperty->SetEnum(Enum);
	EnumProperty->AddCppProperty(UnderlyingProp);
	
	return EnumProperty;
}

FProperty* FCSPropertyFactory::CreateDelegateProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags)
{
	TSharedPtr<FDelegateMetaData> DelegateMetaData = PropertyMetaData.GetTypeMetaData<FDelegateMetaData>();
	FDelegateProperty* DelegateProperty = CreateSimpleProperty<FDelegateProperty>(Outer, PropertyMetaData, PropertyFlags);
	DelegateProperty->SignatureFunction = FCSFunctionFactory::CreateFunctionFromMetaData(Outer->GetOwnerClass(), DelegateMetaData->SignatureFunction);
	return DelegateProperty;
}

FProperty* FCSPropertyFactory::CreateMulticastDelegateProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags)
{
	TSharedPtr<FDelegateMetaData> MulticastDelegateMetaData = PropertyMetaData.GetTypeMetaData<FDelegateMetaData>();

	UClass* Class = CastChecked<UClass>(Outer);
	UFunction* SignatureFunction = FCSFunctionFactory::CreateFunctionFromMetaData(Class, MulticastDelegateMetaData->SignatureFunction);

	FMulticastDelegateProperty* MulticastDelegateProperty = CreateSimpleProperty<FMulticastInlineDelegateProperty>(Outer, PropertyMetaData, PropertyFlags);
	MulticastDelegateProperty->SignatureFunction = SignatureFunction;
	return MulticastDelegateProperty;
}

template<typename PrimitiveProperty>
class FPrimitivePropertyWrapper
{
public:
	
	static FProperty* CallCreateSimpleProperty(UField* Object, const FPropertyMetaData& MetaData, const EPropertyFlags PropertyFlags)
	{
		FProperty* NewProperty = FCSPropertyFactory::CreateSimpleProperty<PrimitiveProperty>(Object, MetaData, PropertyFlags);
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

FProperty* FCSPropertyFactory::CreateAndAssignProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags)
{
	FProperty* Property = CreateProperty(Outer, PropertyMetaData, PropertyFlags);

	if (!Property)
	{
		return nullptr;
	}

	//Register the property to the owner (struct, function, class)
	Outer->AddCppProperty(Property);
	PropertyMetaData.Type->OnPropertyCreated(Property);
	
	return Property;
}

FProperty* FCSPropertyFactory::CreateProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags)
{
	if (const FMakeNewPropertyDelegate* DelegatePtr = MakeNewPropertyFunctionMap.Find(PropertyMetaData.Type->PropertyType))
	{
		const FMakeNewPropertyDelegate MakeNewProperty = *DelegatePtr;
		FProperty* NewProperty = MakeNewProperty(Outer, PropertyMetaData, PropertyFlags);
		
		NewProperty->SetPropertyFlags(PropertyMetaData.PropertyFlags | PropertyFlags);
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
			
			if (!PropertyMetaData.RepNotifyFunctionName.IsEmpty())
			{
				NewProperty->RepNotifyFunc = *PropertyMetaData.RepNotifyFunctionName;
				NewProperty->SetPropertyFlags(CPF_Net | CPF_RepNotify);
			}
		}
		
		FMetaDataHelper::ApplyMetaData(PropertyMetaData.MetaData, NewProperty);
		
		return NewProperty;
	}

	FText DisplayName = StaticEnum<ECSPropertyType>()->GetDisplayValueAsText(PropertyMetaData.Type->PropertyType);
	UE_LOG(LogUnrealSharp, Warning, TEXT("%hs: Property type with name %s doesn't exist. Can't create new property."), __FUNCTION__, *DisplayName.ToString());
	return nullptr;
}

void FCSPropertyFactory::GeneratePropertiesForType(UField* Outer, const TArray<FPropertyMetaData>& PropertiesMetaData, const EPropertyFlags PropertyFlags)
{
	for (int32 Index = PropertiesMetaData.Num() - 1; Index >= 0; --Index)
	{
		const FPropertyMetaData& PropertyMetaData = PropertiesMetaData[Index];
		CreateAndAssignProperty(Outer, PropertyMetaData, PropertyFlags);
	}
}

template <class FieldClass>
FieldClass* FCSPropertyFactory::CreateSimpleProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags)
{
	FName PropertyName = PropertyMetaData.Name;
	
	if (EnumHasAnyFlags(PropertyFlags, CPF_ReturnParm))
	{
		PropertyName = "ReturnValue";
	}

	FieldClass* NewProperty = new FieldClass(Outer, PropertyName, RF_Public);
	NewProperty->PropertyFlags = PropertyMetaData.PropertyFlags | PropertyFlags;
	
	return NewProperty;
}

bool FCSPropertyFactory::IsOutParameter(const FProperty* InParam)
{
	const bool bIsParam = InParam->HasAnyPropertyFlags(CPF_Parm);
	const bool bIsReturnParam = InParam->HasAnyPropertyFlags(CPF_ReturnParm);
	const bool bIsOutParam = InParam->HasAnyPropertyFlags(CPF_OutParm) && !InParam->HasAnyPropertyFlags(CPF_ConstParm);
	return bIsParam && !bIsReturnParam && bIsOutParam;
}

void FCSPropertyFactory::InitializeVariable(UFunction* Getter, UObject* Outer, FProperty* Property)
{
	void* ValuePtr = Property->ContainerPtrToValuePtr<int32>(Outer);
	Outer->ProcessEvent(Getter, ValuePtr);
}

