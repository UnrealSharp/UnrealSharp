#include "CSPropertyGenerator.h"

#include "TypeGenerator/CSClass.h"
#include "TypeGenerator/CSSkeletonClass.h"
#include "TypeGenerator/Functions/CSFunction.h"
#include "TypeGenerator/Properties/PropertyGeneratorManager.h"

#if WITH_EDITOR
#include "Kismet2/BlueprintEditorUtils.h"
#endif

ECSPropertyType UCSPropertyGenerator::GetPropertyType() const
{
	return ECSPropertyType::Unknown;
}

FFieldClass* UCSPropertyGenerator::GetPropertyClass()
{
	PURE_VIRTUAL();
	return nullptr;
}

FProperty* UCSPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	return NewProperty(Outer, PropertyMetaData);
}

bool UCSPropertyGenerator::SupportsPropertyType(ECSPropertyType InPropertyType) const
{
	ECSPropertyType PropertyType = GetPropertyType();
	check(PropertyType != ECSPropertyType::Unknown);
	return PropertyType == InPropertyType;
}

TSharedPtr<FCSUnrealType> UCSPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	PURE_VIRTUAL();
	return nullptr;
}

FGuid UCSPropertyGenerator::ConstructGUIDFromName(const FName& Name)
{
	return ConstructGUIDFromString(Name.ToString());
}

FGuid UCSPropertyGenerator::ConstructGUIDFromString(const FString& Name)
{
	const uint32 BufferLength = Name.Len() * sizeof(Name[0]);
	uint32 HashBuffer[5];
	FSHA1::HashBuffer(*Name, BufferLength, reinterpret_cast<uint8*>(HashBuffer));
	return FGuid(HashBuffer[1], HashBuffer[2], HashBuffer[3], HashBuffer[4]); 
}

bool UCSPropertyGenerator::CanBeHashed(const FProperty* InParam)
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

FProperty* UCSPropertyGenerator::NewProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData, const FFieldClass* FieldClass)
{
	FName PropertyName = PropertyMetaData.Name;
	
	if (EnumHasAnyFlags(PropertyMetaData.PropertyFlags, CPF_ReturnParm))
	{
		PropertyName = "ReturnValue";
	}

	if (FieldClass == nullptr)
	{
		FieldClass = GetPropertyClass();
	}

	return FPropertyGeneratorManager::Get().ConstructProperty(FieldClass, Outer, PropertyName, PropertyMetaData);
}

UClass* UCSPropertyGenerator::TryFindingOwningClass(UField* Outer)
{
	if (UCSFunctionBase* Function = Cast<UCSFunctionBase>(Outer))
	{
		Outer = Function->GetOwnerClass();
	}

	if (UCSSkeletonClass* SkeletonClass = Cast<UCSSkeletonClass>(Outer))
	{
		return SkeletonClass->GetGeneratedClass();
	}

	return Cast<UClass>(Outer);
}


