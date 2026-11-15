#include "CSBindsRegistry.h"
#include "ProfilingDebugging/CpuProfilerTrace.h"

DECLARE_UNREALSHARP_BINDER(Bind_Profiler)
{
#if CPUPROFILERTRACE_ENABLED
	struct FScope
	{
		int64 Id;
		bool bEmitted;
	};
	TArray<FScope> Scopes;
	int64 NextId = 0;
#endif

	// Direct binding: a reflected call would end its ProcessEvent scope before
	// this child event is closed by managed code.
	int64 BeginCpuTraceScope(const uint8* Utf8Name)
	{
		if (!IsInGameThread() || !Utf8Name)
		{
			return -1;
		}
		const FString Name = FString(UTF8_TO_TCHAR(reinterpret_cast<const char*>(Utf8Name))).TrimStartAndEnd();
		if (Name.IsEmpty() || Name.Len() > 1024)
		{
			return -1;
		}
#if CPUPROFILERTRACE_ENABLED
		if (NextId == MAX_int64)
		{
			return -1;
		}
		const FScope Scope { ++NextId, TRACE_CPUPROFILER_EVENT_MANUAL_IS_ENABLED() };
		if (Scope.bEmitted)
		{
			FCpuProfilerTrace::OutputBeginDynamicEvent(*Name, __FILE__, __LINE__);
		}
		Scopes.Add(Scope);
		return Scope.Id;
#else
		return 0;
#endif
	}

	uint8 EndCpuTraceScope(int64 Id)
	{
		if (!IsInGameThread())
		{
			return 0;
		}
		if (Id == 0)
		{
			return 1;
		}
#if CPUPROFILERTRACE_ENABLED
		if (Scopes.IsEmpty() || Scopes.Last().Id != Id)
		{
			return 0;
		}
		if (Scopes.Last().bEmitted)
		{
			FCpuProfilerTrace::OutputEndEvent();
		}
		Scopes.Pop(EAllowShrinking::No);
		return 1;
#else
		return 0;
#endif
	}

	BIND_UNREALSHARP_FUNCTION(BeginCpuTraceScope)
	BIND_UNREALSHARP_FUNCTION(EndCpuTraceScope)
}
