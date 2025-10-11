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
	if (FieldClass == nullptr)
	{
		FieldClass = GetPropertyClass();
	}

	return FPropertyGeneratorManager::Get().ConstructProperty(FieldClass, Outer, PropertyMetaData.GetName(), PropertyMetaData);
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


