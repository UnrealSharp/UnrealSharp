// Fill out your copyright notice in the Description page of Project Settings.


#include "PropertyGeneratorManager.h"

#include <array>

#include "CSGetterSetterProperty.h"
#include "UnrealSharpCore.h"
#include "TypeGenerator/Register/MetaData/CSPropertyMetaData.h"
#include "TypeGenerator/Register/TypeInfo/CSClassInfo.h"
#include <UObject/PropertyOptional.h>

static TSharedPtr<FGCHandle> GetMethodHandle(FCSAssembly& OwningAssembly, const TSharedPtr<FGCHandle> &TypeHandle, FStringView MethodName) {
	if (MethodName.IsEmpty()) {
		return nullptr;
	}

	const FString InvokeMethodName = FString::Printf(TEXT("Invoke_%s"), MethodName.GetData());
	return OwningAssembly.GetManagedMethod(TypeHandle, InvokeMethodName);;
}


static TTuple<UFunction*, UFunction*> GetGetterAndSetterMethods(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) {
	UClass* Class = Cast<UClass>(Outer);
	if (Class == nullptr) {
		return { nullptr, nullptr};
	}

	return {
		Class->FindFunctionByName(*PropertyMetaData.BlueprintGetter),
		Class->FindFunctionByName(*PropertyMetaData.BlueprintSetter)
	};
}

template <ValidProperty T>
class TCSPropertyInitializer : public ICSPropertyInitializer {
public:
	FProperty* ConstructProperty(UField* Outer, FName PropertyName, const FCSPropertyMetaData& PropertyMetaData) const {
		auto [BlueprintGetterFunction, BlueprintSetterFunction] = GetGetterAndSetterMethods(Outer, PropertyMetaData);
		
		FProperty* NewProperty;
		if (BlueprintGetterFunction != nullptr || BlueprintSetterFunction != nullptr)
		{
			NewProperty = new TCSGetterSetterProperty<T>(Outer, PropertyName, RF_Public, BlueprintSetterFunction, BlueprintGetterFunction);
		}
		else
		{
			NewProperty = new T(Outer, PropertyName, RF_Public);
		}
		NewProperty->PropertyFlags = PropertyMetaData.PropertyFlags;
		return NewProperty;
	}
};

template <ValidProperty T>
static void AddPropertyInitializer(TMap<FName, TSharedRef<ICSPropertyInitializer>>& PropertyInitializers) {
	FFieldClass* FieldClass = T::StaticClass();
	PropertyInitializers.Emplace(FieldClass->GetFName(), MakeShared<TCSPropertyInitializer<T>>());
}

FPropertyGeneratorManager::FPtr FPropertyGeneratorManager::Instance;


FPropertyGeneratorManager::FPropertyGeneratorManager() {
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
	
	AddPropertyInitializer<FArrayProperty>(PropertyInitializers);
	AddPropertyInitializer<FSetProperty>(PropertyInitializers);
	AddPropertyInitializer<FMapProperty>(PropertyInitializers);
	AddPropertyInitializer<FOptionalProperty>(PropertyInitializers);
	
	AddPropertyInitializer<FNameProperty>(PropertyInitializers);
	AddPropertyInitializer<FStrProperty>(PropertyInitializers);
	AddPropertyInitializer<FTextProperty>(PropertyInitializers);
}

void FPropertyGeneratorManager::Init() {
	if (Instance != nullptr) {
		UE_LOG(LogUnrealSharp, Error, TEXT("Property generator already initialized"));
		return;
	}

	Instance.Reset(new FPropertyGeneratorManager());
}

void FPropertyGeneratorManager::Shutdown() {
	if (Instance == nullptr) {
		UE_LOG(LogUnrealSharp, Error, TEXT("Property generator is not initialized"));
		return;
	}

	Instance.Reset();
}

FProperty* FPropertyGeneratorManager::ConstructProperty(const FFieldClass* FieldClass, UField* Owner,
                                                        FName PropertyName, const FCSPropertyMetaData& PropertyMetaData) const {
	auto PropertyInitializer = PropertyInitializers.Find(FieldClass->GetFName());
	if (PropertyInitializer == nullptr) {
		UE_LOG(LogUnrealSharp, Error, TEXT("Property generator for %s is not implemented"), *FieldClass->GetName());
		return static_cast<FProperty*>(FieldClass->Construct(Owner, PropertyName, RF_Public));
	}

	return PropertyInitializer->Get().ConstructProperty(Owner, PropertyName, PropertyMetaData);
}
