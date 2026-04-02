#include "Factories/PropertyGenerators/CSScriptInterfacePropertyGenerator.h"
#include "ReflectionData/CSFieldType.h"

FProperty* UCSScriptInterfacePropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData)
{
	FInterfaceProperty* InterfaceProperty = NewProperty<FInterfaceProperty>(Outer, PropertyReflectionData);
	
	TSharedPtr<FCSFieldType> InterfaceData = PropertyReflectionData.GetInnerTypeData<FCSFieldType>();
	InterfaceProperty->SetInterfaceClass(InterfaceData->InnerType.ResolveUField<UClass>());
	
	return InterfaceProperty;
}

TSharedPtr<FCSUnrealType> UCSScriptInterfacePropertyGenerator::CreatePropertyInnerTypeData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSFieldType>();
}
