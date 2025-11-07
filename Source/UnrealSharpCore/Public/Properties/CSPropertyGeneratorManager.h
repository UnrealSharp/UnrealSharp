#pragma once

#include "CoreMinimal.h"

struct FCSPropertyMetaData;

class ICSPropertyInitializer
{
public:
	virtual ~ICSPropertyInitializer() = default;
	virtual FProperty* ConstructProperty(UField* Outer, FName PropertyName, const FCSPropertyMetaData& PropertyMetaData) const = 0;
};

class UNREALSHARPCORE_API FCSPropertyGeneratorManager
{
	FCSPropertyGeneratorManager();
	~FCSPropertyGeneratorManager() = default;

	struct FPropertyManagerDeleter
	{
		void operator()(FCSPropertyGeneratorManager* Manager) const
		{
			delete Manager;
		}
	};

	using FPtr = TUniquePtr<FCSPropertyGeneratorManager, FPropertyManagerDeleter>;

public:
	static const FCSPropertyGeneratorManager& Get()
	{
		checkf(Instance != nullptr, TEXT("Property generator manager not initialized"))
		return *Instance;
	}

	static void Initialize();
	static void Shutdown();

	FProperty* ConstructProperty(const FFieldClass* FieldClass, UField* Owner, FName PropertyName, const FCSPropertyMetaData& PropertyMetaData) const;

private:
	static FPtr Instance;
	TMap<FName, TSharedRef<ICSPropertyInitializer>> PropertyInitializers;
};
