#include "CSSupportedPropertyTranslators.h"
#include "ArrayPropertyTranslator.h"
#include "BitfieldPropertyTranslator.h"
#include "BlittableCustomStructTypePropertyTranslator.h"
#include "BlittableStructPropertyTranslator.h"
#include "BoolPropertyTranslator.h"
#include "ClassPropertyTranslator.h"
#include "PropertyTranslator.h"
#include "CustomStructTypePropertyTranslator.h"
#include "EnumPropertyTranslator.h"
#include "FloatPropertyTranslator.h"
#include "InterfacePropertyTranslator.h"
#include "MulticastDelegatePropertyTranslator.h"
#include "SoftObjectPtrPropertyTranslator.h"
#include "NamePropertyTranslator.h"
#include "NullPropertyTranslator.h"
#include "ObjectPropertyTranslator.h"
#include "SoftClassPropertyTranslator.h"
#include "StringPropertyTranslator.h"
#include "StructPropertyTranslator.h"
#include "TextPropertyTranslator.h"
#include "WeakObjectPropertyTranslator.h"
#include "GlueGenerator/CSInclusionLists.h"
#include "GlueGenerator/CSScriptBuilder.h"
#include "UObject/UObjectIterator.h"
#include "UObject/TextProperty.h"
#include "UObject/EnumProperty.h"
#include "HAL/PlatformFilemanager.h"

using namespace ScriptGeneratorUtilities;

FCSSupportedPropertyTranslators::FCSSupportedPropertyTranslators(const FCSNameMapper& InNameMapper, FCSInclusionLists& Blacklist) : NameMapper(InNameMapper)
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

	AddBlittableCustomStructPropertyTranslator("Vector2D", "System.DoubleNumerics.Vector2", Blacklist);
	AddBlittableCustomStructPropertyTranslator("Vector2f", "System.Numerics.Vector2", Blacklist);
	AddBlittableCustomStructPropertyTranslator("Vector", "System.DoubleNumerics.Vector3", Blacklist);
	AddBlittableCustomStructPropertyTranslator("Vector3f", "System.Numerics.Vector3", Blacklist);
	AddBlittableCustomStructPropertyTranslator("Vector_NetQuantize", "System.DoubleNumerics.Vector3", Blacklist);
	AddBlittableCustomStructPropertyTranslator("Vector_NetQuantize10", "System.DoubleNumerics.Vector3", Blacklist);
	AddBlittableCustomStructPropertyTranslator("Vector_NetQuantize100", "System.DoubleNumerics.Vector3", Blacklist);
	AddBlittableCustomStructPropertyTranslator("Vector_NetQuantizeNormal", "System.DoubleNumerics.Vector3", Blacklist);
	AddBlittableCustomStructPropertyTranslator("Vector4", "System.DoubleNumerics.Vector4", Blacklist);
	AddBlittableCustomStructPropertyTranslator("Vector4f", "System.Numerics.Vector4", Blacklist);
	AddBlittableCustomStructPropertyTranslator("Quat", "System.DoubleNumerics.Quaternion", Blacklist);
	AddBlittableCustomStructPropertyTranslator("Quat4f", "System.Numerics.Quaternion", Blacklist);
	AddBlittableCustomStructPropertyTranslator("Matrix", "System.DoubleNumerics.Matrix4x4", Blacklist);
	AddBlittableCustomStructPropertyTranslator("Matrix44f", "System.Numerics.Matrix4x4", Blacklist);
	AddBlittableCustomStructPropertyTranslator("Rotator", UNREAL_SHARP_NAMESPACE ".Rotator", Blacklist);
	AddBlittableCustomStructPropertyTranslator("Transform", UNREAL_SHARP_NAMESPACE ".Transform", Blacklist);
	AddBlittableCustomStructPropertyTranslator("RandomStream", UNREAL_SHARP_NAMESPACE ".RandomStream", Blacklist);
	AddBlittableCustomStructPropertyTranslator("TimerHandle", UNREAL_SHARP_NAMESPACE ".TimerHandle", Blacklist);
	AddPropertyTranslator(FStructProperty::StaticClass(), new FBlittableStructPropertyTranslator(*this));
	AddPropertyTranslator(FStructProperty::StaticClass(), new FStructPropertyTranslator(*this));

	AddPropertyTranslator(FMulticastSparseDelegateProperty::StaticClass(), new FMulticastDelegatePropertyTranslator(*this));
	AddPropertyTranslator(FMulticastInlineDelegateProperty::StaticClass(), new FMulticastDelegatePropertyTranslator(*this));

	AddPropertyTranslator(FInterfaceProperty::StaticClass(), new FCSInterfacePropertyTranslator(*this));
}

const FPropertyTranslator& FCSSupportedPropertyTranslators::Find(const FProperty* Property) const
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

const FPropertyTranslator& FCSSupportedPropertyTranslators::Find(UFunction* Function) const
{
	FProperty* ReturnProperty = Function->GetReturnProperty();
	
	if (ReturnProperty)
	{
		return Find(ReturnProperty);
	}
	
	return *NullHandler;
}

bool FCSSupportedPropertyTranslators::IsStructBlittable(const UScriptStruct& ScriptStruct) const
{
	return FBlittableStructPropertyTranslator::IsStructBlittable(*this, ScriptStruct);
}

void FCSSupportedPropertyTranslators::AddPropertyTranslator(FFieldClass* PropertyClass, FPropertyTranslator* Handler)
{
	TArray<FPropertyTranslator*>& Handlers = TranslatorMap.FindOrAdd(PropertyClass->GetFName());
	Handlers.Add(Handler);
}

void FCSSupportedPropertyTranslators::AddBlittablePropertyTranslator(FFieldClass* PropertyClass, const FString& CSharpType)
{
	AddPropertyTranslator(PropertyClass, new FBlittableTypePropertyTranslator(*this, PropertyClass, CSharpType));
}

void FCSSupportedPropertyTranslators::AddBlittableCustomStructPropertyTranslator(const FString& UnrealName, const FString& CSharpName, FCSInclusionLists& Blacklist)
{
	AddPropertyTranslator(FStructProperty::StaticClass(), new FBlittableCustomStructTypePropertyTranslator(*this, UnrealName, CSharpName));
	Blacklist.AddStruct(FName(UnrealName));
}

void FCSSupportedPropertyTranslators::AddCustomStructPropertyTranslator(const FString& UnrealName, const FString& CSharpName, FCSInclusionLists& Blacklist)
{
	AddPropertyTranslator(FStructProperty::StaticClass(), new FCustomStructTypePropertyTranslator(*this, UnrealName, CSharpName));
	Blacklist.AddStruct(FName(UnrealName));
}

