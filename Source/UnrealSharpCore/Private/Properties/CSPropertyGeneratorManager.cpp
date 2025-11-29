#include "Properties/CSPropertyGeneratorManager.h"

#include <array>
#include "UnrealSharpCore.h"
#include "ReflectionData/CSPropertyReflectionData.h"
#include <UObject/PropertyOptional.h>

#include "Factories/CSFunctionFactory.h"
#include "Functions/CSFunction.h"
#include "Properties/CSGetterSetterProperty.h"

UCSFunctionBase* CreateGetterSetterFunction(const UField* Outer, const TSharedPtr<FCSFunctionReflectionData>& GetterSetterReflectionData)
{
	if (!GetterSetterReflectionData.IsValid())
	{
		return nullptr;
	}
	
	return FCSFunctionFactory::CreateFunctionFromReflectionData(Outer->GetOwnerClass(), *GetterSetterReflectionData);
}

template <ValidProperty T>
class TCSPropertyInitializer : public ICSPropertyInitializer
{
public:
	virtual FProperty* ConstructProperty(UField* Outer, FName PropertyName, const FCSPropertyReflectionData& PropertyReflectionData) const override
	{
		FProperty* NewProperty;
		if (PropertyReflectionData.HasGetterOrSetter())
		{
			UCSFunctionBase* Getter = CreateGetterSetterFunction(Outer, PropertyReflectionData.CustomGetter);
			UCSFunctionBase* Setter = CreateGetterSetterFunction(Outer, PropertyReflectionData.CustomSetter);
			NewProperty = new TCSGetterSetterProperty<T>(Outer, PropertyName, RF_Public, Setter, Getter);
		}
		else
		{
			NewProperty = new T(Outer, PropertyName, RF_Public);
		}
		
		NewProperty->PropertyFlags = PropertyReflectionData.PropertyFlags;
		return NewProperty;
	}
};

template <ValidProperty T>
static void AddPropertyInitializer(TMap<FName, TSharedRef<ICSPropertyInitializer>>& PropertyInitializers)
{
	FFieldClass* FieldClass = T::StaticClass();
	PropertyInitializers.Emplace(FieldClass->GetFName(), MakeShared<TCSPropertyInitializer<T>>());
}

FCSPropertyGeneratorManager::FPtr FCSPropertyGeneratorManager::Instance;


FCSPropertyGeneratorManager::FCSPropertyGeneratorManager()
{
	AddPropertyInitializer<FBoolProperty>(PropertyInitializers);

	AddPropertyInitializer<FInt8Property>(PropertyInitializers);
	AddPropertyInitializer<FInt16Property>(PropertyInitializers);
	AddPropertyInitializer<FIntProperty>(PropertyInitializers);
	AddPropertyInitializer<FInt64Property>(PropertyInitializers);

	AddPropertyInitializer<FByteProperty>(PropertyInitializers);
	AddPropertyInitializer<FUInt16Property>(PropertyInitializers);
	AddPropertyInitializer<FUInt32Property>(PropertyInitializers);
	AddPropertyInitializer<FUInt64Property>(PropertyInitializers);

	AddPropertyInitializer<FFloatProperty>(PropertyInitializers);
	AddPropertyInitializer<FDoubleProperty>(PropertyInitializers);

	AddPropertyInitializer<FEnumProperty>(PropertyInitializers);

	AddPropertyInitializer<FInterfaceProperty>(PropertyInitializers);
	AddPropertyInitializer<FStructProperty>(PropertyInitializers);
	AddPropertyInitializer<FClassProperty>(PropertyInitializers);
	AddPropertyInitializer<FFieldPathProperty>(PropertyInitializers);

	AddPropertyInitializer<FObjectProperty>(PropertyInitializers);
	AddPropertyInitializer<FLazyObjectProperty>(PropertyInitializers);
	AddPropertyInitializer<FWeakObjectProperty>(PropertyInitializers);

	AddPropertyInitializer<FSoftClassProperty>(PropertyInitializers);
	AddPropertyInitializer<FSoftObjectProperty>(PropertyInitializers);

	AddPropertyInitializer<FDelegateProperty>(PropertyInitializers);
	AddPropertyInitializer<FMulticastDelegateProperty>(PropertyInitializers);
	AddPropertyInitializer<FMulticastSparseDelegateProperty>(PropertyInitializers);
	AddPropertyInitializer<FMulticastInlineDelegateProperty>(PropertyInitializers);

	AddPropertyInitializer<FArrayProperty>(PropertyInitializers);
	AddPropertyInitializer<FSetProperty>(PropertyInitializers);
	AddPropertyInitializer<FMapProperty>(PropertyInitializers);
	AddPropertyInitializer<FOptionalProperty>(PropertyInitializers);

	AddPropertyInitializer<FNameProperty>(PropertyInitializers);
	AddPropertyInitializer<FStrProperty>(PropertyInitializers);
	AddPropertyInitializer<FTextProperty>(PropertyInitializers);
}

void FCSPropertyGeneratorManager::Initialize()
{
	if (Instance != nullptr)
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("Property generator already initialized"));
		return;
	}

	Instance.Reset(new FCSPropertyGeneratorManager());
}

void FCSPropertyGeneratorManager::Shutdown()
{
	if (Instance == nullptr)
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("Property generator is not initialized"));
		return;
	}

	Instance.Reset();
}

FProperty* FCSPropertyGeneratorManager::ConstructProperty(const FFieldClass* FieldClass, UField* Owner, FName PropertyName, const FCSPropertyReflectionData& PropertyReflectionData) const
{
	const TSharedRef<ICSPropertyInitializer>* PropertyInitializer = PropertyInitializers.Find(FieldClass->GetFName());
	
	if (PropertyInitializer == nullptr)
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("Property generator for %s is not implemented"), *FieldClass->GetName());
		return static_cast<FProperty*>(FieldClass->Construct(Owner, PropertyName, RF_Public));
	}

	return PropertyInitializer->Get().ConstructProperty(Owner, PropertyName, PropertyReflectionData);
}
