#include "CSPropertyTranslatorManager.h"
#include "GlueGenerator/CSInclusionLists.h"
#include "UObject/UObjectIterator.h"
#include "UObject/TextProperty.h"
#include "UObject/EnumProperty.h"
#include "HAL/PlatformFilemanager.h"
#include "PropertyTranslators/ArrayPropertyTranslator.h"
#include "PropertyTranslators/BitfieldPropertyTranslator.h"
#include "PropertyTranslators/BlittableCustomStructTypePropertyTranslator.h"
#include "PropertyTranslators/BlittableStructPropertyTranslator.h"
#include "PropertyTranslators/BoolPropertyTranslator.h"
#include "PropertyTranslators/ClassPropertyTranslator.h"
#include "PropertyTranslators/CustomStructTypePropertyTranslator.h"
#include "PropertyTranslators/EnumPropertyTranslator.h"
#include "PropertyTranslators/FloatPropertyTranslator.h"
#include "PropertyTranslators/InterfacePropertyTranslator.h"
#include "PropertyTranslators/MulticastDelegatePropertyTranslator.h"
#include "PropertyTranslators/NamePropertyTranslator.h"
#include "PropertyTranslators/NullPropertyTranslator.h"
#include "PropertyTranslators/ObjectPropertyTranslator.h"
#include "PropertyTranslators/SinglecastDelegatePropertyTranslator.h"
#include "PropertyTranslators/SoftClassPropertyTranslator.h"
#include "PropertyTranslators/SoftObjectPtrPropertyTranslator.h"
#include "PropertyTranslators/StringPropertyTranslator.h"
#include "PropertyTranslators/StructPropertyTranslator.h"
#include "PropertyTranslators/TextPropertyTranslator.h"
#include "PropertyTranslators/WeakObjectPropertyTranslator.h"
#include "UnrealSharpUtilities/UnrealSharpStatics.h"

using namespace ScriptGeneratorUtilities;

FCSPropertyTranslatorManager::FCSPropertyTranslatorManager(const FCSNameMapper& InNameMapper, FCSInclusionLists& DenyList) : NameMapper(InNameMapper)
{
	NullHandler.Reset(new FNullPropertyTranslator(*this));

	AddBlittablePropertyTranslator(FInt8Property::StaticClass(), "sbyte");
	AddBlittablePropertyTranslator(FInt16Property::StaticClass(), "short");
	AddBlittablePropertyTranslator(FIntProperty::StaticClass(), "int");
	AddBlittablePropertyTranslator(FInt64Property::StaticClass(), "long");
	AddBlittablePropertyTranslator(FUInt16Property::StaticClass(), "ushort");
	AddBlittablePropertyTranslator(FUInt32Property::StaticClass(), "uint");
	AddBlittablePropertyTranslator(FUInt64Property::StaticClass(), "ulong");
	AddBlittablePropertyTranslator(FDoubleProperty::StaticClass(), "double");
	AddPropertyTranslator(FFloatProperty::StaticClass(), new FFloatPropertyTranslator(*this));

	const auto EnumPropertyHandler = new FEnumPropertyTranslator(*this);
	AddPropertyTranslator(FEnumProperty::StaticClass(), EnumPropertyHandler);
	AddPropertyTranslator(FByteProperty::StaticClass(), EnumPropertyHandler);

	AddBlittablePropertyTranslator(FByteProperty::StaticClass(), "byte");

	AddPropertyTranslator(FBoolProperty::StaticClass(), new FBitfieldPropertyTranslator(*this));
	AddPropertyTranslator(FBoolProperty::StaticClass(), new FBoolPropertyTranslator(*this));

	AddPropertyTranslator(FStrProperty::StaticClass(), new FStringPropertyTranslator(*this));
	AddPropertyTranslator(FNameProperty::StaticClass(), new FNamePropertyTranslator(*this));
	AddPropertyTranslator(FTextProperty::StaticClass(), new FTextPropertyTranslator(*this));

	AddPropertyTranslator(FWeakObjectProperty::StaticClass(), new FWeakObjectPropertyTranslator(*this));
	AddPropertyTranslator(FObjectProperty::StaticClass(), new FObjectPropertyTranslator(*this));
	AddPropertyTranslator(FClassProperty::StaticClass(), new FClassPropertyTranslator(*this));
	AddPropertyTranslator(FSoftObjectProperty::StaticClass(), new FSoftObjectPtrPropertyTranslator(*this));
	AddPropertyTranslator(FSoftClassProperty::StaticClass(), new FSoftClassPropertyTranslator(*this));

	AddPropertyTranslator(FArrayProperty::StaticClass(), new FArrayPropertyTranslator(*this));

	AddBlittableCustomStructPropertyTranslator("Vector2D", "System.DoubleNumerics.Vector2", DenyList);
	AddBlittableCustomStructPropertyTranslator("Vector2f", "System.Numerics.Vector2", DenyList);
	AddBlittableCustomStructPropertyTranslator("Vector", "System.DoubleNumerics.Vector3", DenyList);
	AddBlittableCustomStructPropertyTranslator("Vector3f", "System.Numerics.Vector3", DenyList);
	AddBlittableCustomStructPropertyTranslator("Vector_NetQuantize", "System.DoubleNumerics.Vector3", DenyList);
	AddBlittableCustomStructPropertyTranslator("Vector_NetQuantize10", "System.DoubleNumerics.Vector3", DenyList);
	AddBlittableCustomStructPropertyTranslator("Vector_NetQuantize100", "System.DoubleNumerics.Vector3", DenyList);
	AddBlittableCustomStructPropertyTranslator("Vector_NetQuantizeNormal", "System.DoubleNumerics.Vector3", DenyList);
	AddBlittableCustomStructPropertyTranslator("Vector4", "System.DoubleNumerics.Vector4", DenyList);
	AddBlittableCustomStructPropertyTranslator("Vector4f", "System.Numerics.Vector4", DenyList);
	AddBlittableCustomStructPropertyTranslator("Quat", "System.DoubleNumerics.Quaternion", DenyList);
	AddBlittableCustomStructPropertyTranslator("Quat4f", "System.Numerics.Quaternion", DenyList);
	AddBlittableCustomStructPropertyTranslator("Matrix", "System.DoubleNumerics.Matrix4x4", DenyList);
	AddBlittableCustomStructPropertyTranslator("Matrix44f", "System.Numerics.Matrix4x4", DenyList);
	AddBlittableCustomStructPropertyTranslator("Rotator", UNREAL_SHARP_NAMESPACE ".Rotator", DenyList);
	AddBlittableCustomStructPropertyTranslator("Transform", UNREAL_SHARP_NAMESPACE ".Transform", DenyList);
	AddBlittableCustomStructPropertyTranslator("RandomStream", UNREAL_SHARP_NAMESPACE ".RandomStream", DenyList);
	AddBlittableCustomStructPropertyTranslator("TimerHandle", UNREAL_SHARP_NAMESPACE ".TimerHandle", DenyList);
	AddBlittableCustomStructPropertyTranslator("ActorInstanceHandle", UNREAL_SHARP_NAMESPACE ".ActorInstanceHandle", DenyList);
	
	AddPropertyTranslator(FStructProperty::StaticClass(), new FBlittableStructPropertyTranslator(*this));
	AddPropertyTranslator(FStructProperty::StaticClass(), new FStructPropertyTranslator(*this));

	const auto MulticastDelegatePropertyHandler = new FMulticastDelegatePropertyTranslator(*this);
	AddPropertyTranslator(FMulticastSparseDelegateProperty::StaticClass(), MulticastDelegatePropertyHandler);
	AddPropertyTranslator(FMulticastInlineDelegateProperty::StaticClass(), MulticastDelegatePropertyHandler);

	AddPropertyTranslator(FDelegateProperty::StaticClass(), new FSinglecastDelegatePropertyTranslator(*this));

	AddPropertyTranslator(FInterfaceProperty::StaticClass(), new FCSInterfacePropertyTranslator(*this));
}

const FPropertyTranslator& FCSPropertyTranslatorManager::Find(const FProperty* Property) const
{
	const TArray<FPropertyTranslator*>* Translators = TranslatorMap.Find(Property->GetClass()->GetFName());
	
	if (Translators)
	{
		for (FPropertyTranslator* Handler : *Translators)
		{
			check(Handler);
			if (Handler->CanHandleProperty(Property))
			{
				return *Handler;
			}
		}
	}

	return *NullHandler;
}

const FPropertyTranslator& FCSPropertyTranslatorManager::Find(UFunction* Function) const
{
	FProperty* ReturnProperty = Function->GetReturnProperty();
	
	if (ReturnProperty)
	{
		return Find(ReturnProperty);
	}
	
	return *NullHandler;
}

bool FCSPropertyTranslatorManager::IsStructBlittable(const UScriptStruct& ScriptStruct) const
{
	return FBlittableStructPropertyTranslator::IsStructBlittable(*this, ScriptStruct);
}

void FCSPropertyTranslatorManager::AddPropertyTranslator(FFieldClass* PropertyClass, FPropertyTranslator* Handler)
{
	TArray<FPropertyTranslator*>& Handlers = TranslatorMap.FindOrAdd(PropertyClass->GetFName());
	Handlers.Add(Handler);
}

void FCSPropertyTranslatorManager::AddBlittablePropertyTranslator(FFieldClass* PropertyClass, const FString& CSharpType)
{
	AddPropertyTranslator(PropertyClass, new FBlittableTypePropertyTranslator(*this, PropertyClass, CSharpType));
}

void FCSPropertyTranslatorManager::AddBlittableCustomStructPropertyTranslator(const FString& UnrealName, const FString& CSharpName, FCSInclusionLists& Blacklist)
{
	AddPropertyTranslator(FStructProperty::StaticClass(), new FBlittableCustomStructTypePropertyTranslator(*this, UnrealName, CSharpName));
	Blacklist.AddStruct(FName(UnrealName));
}

void FCSPropertyTranslatorManager::AddCustomStructPropertyTranslator(const FString& UnrealName, const FString& CSharpName, FCSInclusionLists& Blacklist)
{
	AddPropertyTranslator(FStructProperty::StaticClass(), new FCustomStructTypePropertyTranslator(*this, UnrealName, CSharpName));
	Blacklist.AddStruct(FName(UnrealName));
}

