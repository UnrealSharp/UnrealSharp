#include "FTypeBuilderExporter.h"
#include "CSManager.h"
#include "MetaData/CSClassBaseMetaData.h"
#include "MetaData/CSClassMetaData.h"
#include "MetaData/CSDefaultComponentMetaData.h"
#include "MetaData/CSEnumMetaData.h"
#include "MetaData/CSFunctionMetaData.h"
#include "MetaData/CSStructMetaData.h"
#include "MetaData/CSTemplateType.h"
#include "MetaData/FCSFieldTypePropertyMetaData.h"
#include "TypeGenerator/Factories/CSPropertyFactory.h"
#include "TypeGenerator/Factories/PropertyGenerators/CSPropertyGenerator.h"

FCSTypeReferenceMetaData* UFTypeBuilderExporter::NewType_Internal(TCHAR* InFieldName,
                                                                  TCHAR* InNamespace,
                                                                  TCHAR* InAssemblyName,
                                                                  ECSFieldType FieldType,
                                                                  uint8* TypeHandle,
                                                                  bool& NeedsRebuild)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UFTypeBuilderExporter::NewType_Internal);
	
	UCSAssembly* Assembly = UCSManager::Get().FindOrLoadAssembly(InAssemblyName);

	if (!IsValid(Assembly))
	{
		UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to find or load assembly: {0}", InAssemblyName);
		return nullptr;
	}
	
	TSharedPtr<FCSManagedTypeInfo> TypeInfo = Assembly->TryRegisterType(InFieldName, InNamespace, FieldType, TypeHandle, NeedsRebuild);
	TSharedPtr<FCSTypeReferenceMetaData> TypeMetaData = TypeInfo->GetTypeMetaData();
	return TypeMetaData.Get();
}

void UFTypeBuilderExporter::InitMetaData_Internal(FCSTypeReferenceMetaData* Owner, int32 Count)
{
	Owner->MetaData.Reserve(Count);
}

void UFTypeBuilderExporter::AddMetaData_Internal(FCSTypeReferenceMetaData* Owner, TCHAR* Key, TCHAR* Value)
{
	Owner->MetaData.Add(Key, Value);
}

void UFTypeBuilderExporter::ModifyClass_Internal(FCSClassBaseMetaData* Owner, TCHAR* Name, TCHAR* Namespace, TCHAR* AssemblyName, TCHAR* ConfigName, EClassFlags Flags)
{
	Owner->ParentClass.FieldName = FCSFieldName(Name, Namespace);
	Owner->ParentClass.AssemblyName = AssemblyName;
	Owner->ConfigName = ConfigName;
	Owner->ClassFlags = Flags;
}

TArray<FCSPropertyMetaData>* UFTypeBuilderExporter::InitializePropertiesFromTemplate_Internal(FCSPropertyMetaData* Owner, int32 NumProperties)
{
	TSharedPtr<FCSTemplateType> TemplateType = Owner->GetTypeMetaData<FCSTemplateType>();
	TemplateType->TemplateParameters.Reserve(NumProperties);
	return &TemplateType->TemplateParameters;
}

TArray<FCSPropertyMetaData>* UFTypeBuilderExporter::InitializePropertiesFromStruct_Internal(FCSStructMetaData* Owner,
	int32 NumProperties)
{
	Owner->Properties.Reserve(NumProperties);
	return &Owner->Properties;
}

FCSPropertyMetaData* UFTypeBuilderExporter::MakeProperty_Internal(TArray<FCSPropertyMetaData>* Owner, uint8 PropertyTypeInt, TCHAR* Name, uint64 FlagsInt,
                                                   TCHAR* RepNotifyFuncName, int32 InArrayDim, int32 LifetimeConditionInt, TCHAR* InBlueprintSetter,
                                                   TCHAR* InBlueprintGetter)
{
	ECSPropertyType PropertyTypeEnum = static_cast<ECSPropertyType>(PropertyTypeInt);
	UCSPropertyGenerator* PropertyGenerator = FCSPropertyFactory::GetPropertyGenerator(PropertyTypeEnum);
	TSharedPtr<FCSUnrealType> Type = PropertyGenerator->CreateTypeMetaData(PropertyTypeEnum);
	Type->PropertyType = PropertyTypeEnum;
	
	FCSPropertyMetaData& PropertyMetaData = Owner->AddDefaulted_GetRef();
	PropertyMetaData.Type = Type;
	PropertyMetaData.FieldName = FCSFieldName(Name);
	PropertyMetaData.Flags = static_cast<EPropertyFlags>(FlagsInt);
 	PropertyMetaData.RepNotifyFunctionName = RepNotifyFuncName;
	PropertyMetaData.ArrayDim = InArrayDim;
	PropertyMetaData.LifetimeCondition = static_cast<ELifetimeCondition>(LifetimeConditionInt);
	PropertyMetaData.BlueprintSetter = InBlueprintSetter;
	PropertyMetaData.BlueprintGetter = InBlueprintGetter;
	
	return &PropertyMetaData;
}

void UFTypeBuilderExporter::ModifyFieldProperty_Internal(FCSPropertyMetaData* OutMetaData, TCHAR* Name, TCHAR* Namespace, TCHAR* AssemblyName)
{
	TSharedPtr<FCSFieldTypePropertyMetaData> FieldType = OutMetaData->GetTypeMetaData<FCSFieldTypePropertyMetaData>();
	FieldType->InnerType.FieldName = FCSFieldName(Name, Namespace);
	FieldType->InnerType.AssemblyName = AssemblyName;
}

void UFTypeBuilderExporter::ModifyDefaultComponent_Internal(FCSPropertyMetaData* OutMetaData,
	bool IsRootComponent, const TCHAR* AttachmentComponent, const TCHAR* AttachmentSocket)
{
	TSharedPtr<FCSDefaultComponentMetaData> DefaultComponentType = OutMetaData->GetTypeMetaData<FCSDefaultComponentMetaData>();
	DefaultComponentType->IsRootComponent = IsRootComponent;
	DefaultComponentType->AttachmentComponent = AttachmentComponent;
	DefaultComponentType->AttachmentSocket = AttachmentSocket;
}

void UFTypeBuilderExporter::ReserveFunctions_Internal(FCSClassBaseMetaData* Owner, int32 NumFunctions)
{
	Owner->Functions.Reserve(Owner->Functions.Num() + NumFunctions);
}

FCSFunctionMetaData* UFTypeBuilderExporter::MakeFunction_Internal(FCSClassBaseMetaData* Owner, const TCHAR* Name, EFunctionFlags Flags, int32 NumParams)
{
	FCSFunctionMetaData& FunctionMetaData = Owner->Functions.AddDefaulted_GetRef();
	FunctionMetaData.FieldName = FCSFieldName(Name);
	FunctionMetaData.Flags = Flags;
	FunctionMetaData.Properties.Reserve(NumParams);

	FCSFunctionMetaData* OutFunctionMetaData = &FunctionMetaData;
	return OutFunctionMetaData;
}

void UFTypeBuilderExporter::ReserveOverrides_Internal(FCSClassMetaData* Owner, int32 NumOverrides)
{
	Owner->VirtualFunctions.Reserve(NumOverrides);
}

void UFTypeBuilderExporter::MakeOverride_Internal(FCSClassMetaData* Owner, const TCHAR* NativeName)
{
	Owner->VirtualFunctions.Add(NativeName);
}

void UFTypeBuilderExporter::ReserveEnumValues_Internal(FCSEnumMetaData* Owner, int32 NumValues)
{
	Owner->Items.Reserve(NumValues);
}

void UFTypeBuilderExporter::AddEnumValue_Internal(FCSEnumMetaData* Owner, const TCHAR* Name)
{
	Owner->Items.Add(Name);
}

void UFTypeBuilderExporter::ReserveInterfaces_Internal(FCSClassMetaData* Owner, int32 NumInterfaces)
{
	Owner->Interfaces.Reserve(NumInterfaces);
}

void UFTypeBuilderExporter::AddInterface_Internal(FCSClassMetaData* Owner, TCHAR* Name, TCHAR* Namespace, TCHAR* AssemblyName)
{
	FCSTypeReferenceMetaData InterfaceType;
	InterfaceType.FieldName = FCSFieldName(Name, Namespace);
	InterfaceType.AssemblyName = AssemblyName;
	Owner->Interfaces.Add(InterfaceType);
}
