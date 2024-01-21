#include "CSInclusionLists.h"
#include "CSScriptBuilder.h"
#include "Kismet/KismetMathLibrary.h"
#include "UObject/UnrealType.h"

void FCSInclusionLists::AddEnum(FName EnumName)
{
	Enumerations.Add(EnumName);
}

bool FCSInclusionLists::HasEnum(const UEnum* Enum) const
{
	return Enumerations.Contains(Enum->GetFName());
}

void FCSInclusionLists::AddClass(FName ClassName)
{
	Classes.Add(ClassName);
}

bool FCSInclusionLists::HasClass(const UClass* Class) const
{
	return Classes.Contains(Class->GetFName());
}

void FCSInclusionLists::AddStruct(FName StructName)
{
	Structs.Add(StructName);
}

bool FCSInclusionLists::HasStruct(const UField* Struct) const
{
	return Structs.Contains(Struct->GetFName());
}

void FCSInclusionLists::AddAllFunctions(FName StructName)
{
	AllFunctions.Add(StructName);
}

void FCSInclusionLists::AddFunction(FName StructName, FName FunctionName)
{
	Functions.FindOrAdd(StructName).Add(FunctionName);
}

void FCSInclusionLists::AddFunctionCategory(FName StructName, const FString& Category)
{
	FunctionCategories.FindOrAdd(StructName).Add(Category);
}

bool FCSInclusionLists::HasFunction(const UStruct* Struct, const UFunction* Function) const
{
	if (Struct == UKismetMathLibrary::StaticClass())
	{
		check(true);
	}
	
	if (AllFunctions.Contains(Struct->GetFName()))
	{
		return true;
	}
	const TSet<FName>* List = Functions.Find(Struct->GetFName());
	if (List && List->Contains(Function->GetFName()))
	{
		return true;
	}
	const TSet<FString>* CategoryList = FunctionCategories.Find(Struct->GetFName());
	if (CategoryList && Function->HasMetaData(MD_FunctionCategory))
	{
		const FString& Category = Function->GetMetaData(MD_FunctionCategory);

		return CategoryList->Contains(Category);
	}
	return false;
}

void FCSInclusionLists::AddOverridableFunction(FName StructName, FName OverridableFunctionName)
{
	OverridableFunctions.FindOrAdd(StructName).Add(OverridableFunctionName);
}

bool FCSInclusionLists::HasOverridableFunction(const UStruct* Struct, const UFunction* OverridableFunction) const
{
	const TSet<FName>* List = OverridableFunctions.Find(Struct->GetFName());
	return List && List->Contains(OverridableFunction->GetFName());
}

void FCSInclusionLists::AddProperty(FName StructName, FName PropertyName)
{
	Properties.FindOrAdd(StructName).Add(PropertyName);
}

bool FCSInclusionLists::HasProperty(const UStruct* Struct, const FProperty* Property) const
{
	const TSet<FName>* List = Properties.Find(Struct->GetFName());
	return List && List->Contains(Property->GetFName());
}
