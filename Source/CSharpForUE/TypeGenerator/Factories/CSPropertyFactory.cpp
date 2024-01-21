#include "CSPropertyFactory.h"
#include "CSFunctionFactory.h"
#include "CSharpForUE/CSharpForUE.h"
#include "UObject/UnrealType.h"
#include "UObject/Class.h"
#include "CSharpForUE/TypeGenerator/Register/CSGeneratedEnumBuilder.h"
#include "CSharpForUE/TypeGenerator/Register/CSMetaData.h"

TMap<FName, FMakeNewPropertyDelegate> FCSPropertyFactory::MakeNewPropertyFunctionMap = {};

void FCSPropertyFactory::InitializePropertyFactory()
{
	AddSimpleProperty<FFloatProperty>();
	AddSimpleProperty<FDoubleProperty>();
	
	AddSimpleProperty<FByteProperty>();
	
	AddSimpleProperty<FInt16Property>();
	AddSimpleProperty<FIntProperty>();
	AddSimpleProperty<FInt64Property>();
	
	AddSimpleProperty<FUInt16Property>();
	AddSimpleProperty<FUInt32Property>();
	AddSimpleProperty<FUInt64Property>();
	
	AddSimpleProperty<FBoolProperty>();
	
	AddSimpleProperty<FNameProperty>();
	AddSimpleProperty<FStrProperty>();
	AddSimpleProperty<FTextProperty>();

	AddProperty<FObjectProperty>(&CreateObjectProperty);
	AddProperty<FWeakObjectProperty>(&CreateWeakObjectProperty);
	AddProperty<FSoftObjectProperty>(&CreateSoftObjectProperty);
	AddProperty<FSoftClassProperty>(&CreateSoftClassProperty);
	
	AddProperty<FClassProperty>(&CreateClassProperty);
	AddProperty<FStructProperty>(&CreateStructProperty);
	AddProperty<FArrayProperty>(&CreateArrayProperty);
	AddProperty<FEnumProperty>(&CreateEnumProperty);
	AddProperty<FMulticastInlineDelegateProperty>(&CreateDelegateProperty);
}

template <typename Property>
void FCSPropertyFactory::AddProperty(FMakeNewPropertyDelegate Function)
{
	const FName Name = Property::StaticClass()->GetFName();
	MakeNewPropertyFunctionMap.Add(Name, Function);
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
	TSharedPtr<FMulticastDelegateMetaData> MulticastDelegateMetaData = PropertyMetaData.GetTypeMetaData<FMulticastDelegateMetaData>();

	UClass* Class = CastChecked<UClass>(Outer);
	FMulticastInlineDelegateProperty* DelegateProperty = CreateSimpleProperty<FMulticastInlineDelegateProperty>(Outer, PropertyMetaData, PropertyFlags);
	DelegateProperty->SignatureFunction = FCSFunctionFactory::CreateFunctionFromMetaData(Class, MulticastDelegateMetaData->SignatureFunction);
	
	return DelegateProperty;
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

template <typename PrimitiveProperty>
void FCSPropertyFactory::AddSimpleProperty(const FName& Name)
{
	const FMakeNewPropertyDelegate Delegate = &FPrimitivePropertyWrapper<PrimitiveProperty>::CallCreateSimpleProperty;
	MakeNewPropertyFunctionMap.Add(Name, Delegate);
}

template<typename PrimitiveProperty>
void FCSPropertyFactory::AddSimpleProperty()
{
	AddSimpleProperty<PrimitiveProperty>(PrimitiveProperty::StaticClass()->GetFName());
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
	if (const FMakeNewPropertyDelegate* DelegatePtr = MakeNewPropertyFunctionMap.Find(PropertyMetaData.Type->UnrealPropertyClass))
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

	UE_LOG(LogUnrealSharp, Warning, TEXT("%hs: Property type with name %s doesn't exist. Can't create new property."), __FUNCTION__, *PropertyMetaData.Type->UnrealPropertyClass.ToString());
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

