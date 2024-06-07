#pragma once

#include "CSVTableHacks.generated.h"

USTRUCT()
struct FCSActorTickFunction : public FActorTickFunction
{
	GENERATED_BODY()
	
	virtual void ExecuteTick(float DeltaTime, ELevelTick TickType, ENamedThreads::Type CurrentThread, const FGraphEventRef& MyCompletionGraphEvent) override
	{
		if (Target && IsValidChecked(Target) && !Target->IsUnreachable())
		{
			if (TickType != LEVELTICK_ViewportsOnly || Target->ShouldTickIfViewportsOnly())
			{
				FScopeCycleCounterUObject ActorScope(Target);
				Target->TickActor(DeltaTime * Target->CustomTimeDilation, TickType, *this);
				Target->ReceiveTick(DeltaTime * Target->CustomTimeDilation);
			}
		}
	}
};

template<>
struct TStructOpsTypeTraits<FCSActorTickFunction> : public TStructOpsTypeTraitsBase2<FCSActorTickFunction>
{
	enum
	{
		WithCopy = false
	};
};

USTRUCT()
struct FCSActorComponentTickFunction : public FActorComponentTickFunction
{
	GENERATED_BODY()
	
	virtual void ExecuteTick(float DeltaTime, ELevelTick TickType, ENamedThreads::Type CurrentThread, const FGraphEventRef& MyCompletionGraphEvent) override
	{
		TRACE_CPUPROFILER_EVENT_SCOPE(FActorComponentTickFunction::ExecuteTick);
		ExecuteTickHelper(Target, Target->bTickInEditor, DeltaTime, TickType, [this, TickType](float DilatedTime)
		{
			Target->TickComponent(DilatedTime, TickType, this);
			Target->ReceiveTick(DilatedTime);
		});
	}
};

template<>
struct TStructOpsTypeTraits<FCSActorComponentTickFunction> : public TStructOpsTypeTraitsBase2<FCSActorComponentTickFunction>
{
	enum
	{
		WithCopy = false
	};
};

static_assert(sizeof(FActorComponentTickFunction) == sizeof(FCSActorComponentTickFunction), "Tick function size mismatch");
static_assert(sizeof(FActorComponentTickFunction) == sizeof(FCSActorTickFunction), "Tick function size mismatch");