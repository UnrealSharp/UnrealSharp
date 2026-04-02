#include "Factories/PropertyGenerators/CSEnumPropertyGenerator.h"
#include "CSManager.h"
#include "Types/CSEnum.h"
#include "ReflectionData/CSFieldType.h"

UCSEnumPropertyGenerator::UCSEnumPropertyGenerator()
{
	EnumRedirectors.Add("EObjectChannel", StaticEnum<EObjectTypeQuery>());
	EnumRedirectors.Add("ETraceChannel", StaticEnum<ETraceTypeQuery>());
}

FProperty* UCSEnumPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData)
{
	const TSharedPtr<FCSFieldType> EnumType = PropertyReflectionData.GetInnerTypeData<FCSFieldType>();
	const FCSFieldName& FieldName = EnumType->InnerType.FieldName;
	
	UEnum* Enum;
	if (UEnum** FoundRedirector = EnumRedirectors.Find(FieldName.GetFName()))
	{
		Enum = *FoundRedirector;
	}
	else
	{
		UCSManagedAssembly* Assembly = UCSManager::Get().FindAssembly(EnumType->InnerType.AssemblyName);
		Enum = Assembly->ResolveUField<UEnum>(FieldName);
	}
	
	if (!IsValid(Enum))
	{
		UE_LOGFMT(LogUnrealSharp, Fatal, "Enum is not valid in {0}. PropertyName: {1}", __FUNCTION__, *PropertyReflectionData.GetName().ToString());
	}

#if WITH_EDITOR
	if (UCSEnum* ManagedEnum = Cast<UCSEnum>(Enum))
	{
		if (UStruct* OwningClass = TryFindingOwningClass(Outer))
		{
			ManagedEnum->GetManagedReferencesCollection().AddReference(OwningClass);
		}
	}
#endif
	
	FProperty* NewEnumProperty;
	if (Enum->GetCppForm() == UEnum::ECppForm::EnumClass)
	{
		FEnumProperty* EnumProperty = NewProperty<FEnumProperty>(Outer, PropertyReflectionData, FEnumProperty::StaticClass());
		FByteProperty* UnderlyingProp = new FByteProperty(EnumProperty, "UnderlyingType", RF_Public);
		
		EnumProperty->SetEnum(Enum);
		EnumProperty->AddCppProperty(UnderlyingProp);
		NewEnumProperty = EnumProperty;
	}
	else
	{
		FByteProperty* ByteProperty = NewProperty<FByteProperty>(Outer, PropertyReflectionData, FByteProperty::StaticClass());
		ByteProperty->Enum = Enum;
		NewEnumProperty = ByteProperty;
	}
	
	return NewEnumProperty;
}

TSharedPtr<FCSUnrealType> UCSEnumPropertyGenerator::CreatePropertyInnerTypeData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSFieldType>();
}
