#include "CSWorldContextScope.h"

#include "CSManager.h"
#include "UnrealSharpCore.h"

#include "Engine/Engine.h"
#include "Engine/World.h"

namespace
{
	struct FCSWorldContextThreadState
	{
		TArray<TWeakObjectPtr<UWorld>> Stack;
	};

	thread_local FCSWorldContextThreadState GWorldContextThreadState;
}

UWorld* UCSManager::ResolveWorldContext(UObject* WorldContextObject) const
{
	if (!IsValid(WorldContextObject))
	{
		return nullptr;
	}

	if (UWorld* World = Cast<UWorld>(WorldContextObject))
	{
		return World;
	}

	if (UWorld* World = WorldContextObject->GetWorld(); IsValid(World))
	{
		return World;
	}

	return GEngine
		? GEngine->GetWorldFromContextObject(WorldContextObject, EGetWorldErrorMode::ReturnNull)
		: nullptr;
}

void UCSManager::PushWorldContext(UObject* WorldContextObject, bool bInheritIfUnresolved)
{
	UWorld* World = ResolveWorldContext(WorldContextObject);
	if (!World && bInheritIfUnresolved && !GWorldContextThreadState.Stack.IsEmpty())
	{
		World = GWorldContextThreadState.Stack.Last().Get();
	}

	GWorldContextThreadState.Stack.Add(World);
}

void UCSManager::PopWorldContext()
{
	if (GWorldContextThreadState.Stack.IsEmpty())
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("Attempted to pop an empty world context stack"));
		return;
	}

	GWorldContextThreadState.Stack.Pop(EAllowShrinking::No);
}

UWorld* UCSManager::GetCurrentWorldContext() const
{
	return GWorldContextThreadState.Stack.IsEmpty()
		? nullptr
		: GWorldContextThreadState.Stack.Last().Get();
}

FCSWorldContextScope::FCSWorldContextScope(UObject* WorldContextObject, bool bInheritIfUnresolved)
{
	UCSManager::Get().PushWorldContext(WorldContextObject, bInheritIfUnresolved);
}

FCSWorldContextScope::~FCSWorldContextScope()
{
	UCSManager::Get().PopWorldContext();
}
