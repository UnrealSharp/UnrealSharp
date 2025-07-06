#include "CSArrayPropertyGenerator.h"
#include "TypeGenerator/Factories/CSPropertyFactory.h"
#include "TypeGenerator/Register/MetaData/CSContainerBaseMetaData.h"

FProperty* UCSArrayPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FArrayProperty* NewProperty = static_cast<FArrayProperty*>(Super::CreateProperty(Outer, PropertyMetaData));
	TSharedPtr<FCSContainerBaseMetaData> ArrayPropertyMetaData = PropertyMetaData.GetTypeMetaData<FCSContainerBaseMetaData>();
	NewProperty->Inner = FCSPropertyFactory::CreateProperty(Outer, ArrayPropertyMetaData->InnerProperty);
	NewProperty->Inner->Owner = NewProperty;

	// Replicate behavior from KismetCompiler.cpp:1454 to always pass arrays as reference parameters
	if (NewProperty->HasAnyPropertyFlags(CPF_Parm) && !NewProperty->HasAnyPropertyFlags(CPF_OutParm))
	{
		NewProperty->SetPropertyFlags(CPF_OutParm | CPF_ReferenceParm);
	}
	
	return NewProperty;
}

TSharedPtr<FCSUnrealType> UCSArrayPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSContainerBaseMetaData>();
}