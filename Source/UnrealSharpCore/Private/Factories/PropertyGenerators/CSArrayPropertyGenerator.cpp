#include "Factories/PropertyGenerators/CSArrayPropertyGenerator.h"
#include "ReflectionData/CSTemplateType.h"
#include "Factories/CSPropertyFactory.h"

FProperty* UCSArrayPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData)
{
	FArrayProperty* ArrayProperty = NewProperty<FArrayProperty>(Outer, PropertyReflectionData);
	TSharedPtr<FCSTemplateType> TemplateType = PropertyReflectionData.GetInnerTypeData<FCSTemplateType>();
	ArrayProperty->Inner = FCSPropertyFactory::CreateProperty(Outer, *TemplateType->GetTemplateArgument(0));
	ArrayProperty->Inner->Owner = ArrayProperty;

	// Replicate behavior from KismetCompiler.cpp:1454 to always pass arrays as reference parameters
	if (ArrayProperty->HasAnyPropertyFlags(CPF_Parm) && !ArrayProperty->HasAnyPropertyFlags(CPF_OutParm))
	{
		ArrayProperty->SetPropertyFlags(CPF_OutParm | CPF_ReferenceParm);
	}
	
	return ArrayProperty;
}

TSharedPtr<FCSUnrealType> UCSArrayPropertyGenerator::CreatePropertyInnerTypeData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSTemplateType>();
}