#pragma once

#include "CoreMinimal.h"

struct FCSPropertyMetaData;

class ICSPropertyInitializer
{
public:
	virtual ~ICSPropertyInitializer() = default;
	virtual FProperty* ConstructProperty(UField* Outer, FName PropertyName, const FCSPropertyMetaData& PropertyMetaData) const = 0;
};

class UNREALSHARPCORE_API FPropertyGeneratorManager
{
	FPropertyGeneratorManager();
	~FPropertyGeneratorManager() = default;

	struct FPropertyManagerDeleter
	{
		void operator()(FPropertyGeneratorManager* Manager) const
		{
			delete Manager;
		}
	};

	using FPtr = TUniquePtr<FPropertyGeneratorManager, FPropertyManagerDeleter>;

public:
	static const FPropertyGeneratorManager& Get()
	{
		checkf(Instance != nullptr, TEXT("Property generator manager not initialized"))
		return *Instance;
	}

	static void Init();
	static void Shutdown();

	FProperty* ConstructProperty(const FFieldClass* FieldClass, UField* Owner, FName PropertyName, const FCSPropertyMetaData& PropertyMetaData) const;

private:
	static FPtr Instance;
	TMap<FName, TSharedRef<ICSPropertyInitializer>> PropertyInitializers;
};
