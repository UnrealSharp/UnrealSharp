#include "Factories/PropertyGenerators/CSArrayPropertyGenerator.h"
#include "MetaData/CSTemplateType.h"
#include "Factories/CSPropertyFactory.h"

FProperty* UCSArrayPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FArrayProperty* ArrayProperty = NewProperty<FArrayProperty>(Outer, PropertyMetaData);
	TSharedPtr<FCSTemplateType> ArrayPropertyMetaData = PropertyMetaData.GetTypeMetaData<FCSTemplateType>();
	ArrayProperty->Inner = FCSPropertyFactory::CreateProperty(Outer, *ArrayPropertyMetaData->GetTemplateArgument(0));
	ArrayProperty->Inner->Owner = ArrayProperty;

	// Replicate behavior from KismetCompiler.cpp:1454 to always pass arrays as reference parameters
	if (ArrayProperty->HasAnyPropertyFlags(CPF_Parm) && !ArrayProperty->HasAnyPropertyFlags(CPF_OutParm))
	{
		ArrayProperty->SetPropertyFlags(CPF_OutParm | CPF_ReferenceParm);
	}
	
	return ArrayProperty;
}

TSharedPtr<FCSUnrealType> UCSArrayPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSTemplateType>();
}