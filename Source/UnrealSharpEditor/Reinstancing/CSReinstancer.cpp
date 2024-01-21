#include "CSReinstancer.h"
#include "CSReload.h"
#include "CSharpForUE/TypeGenerator/Register/CSTypeRegistry.h"
#include "Kismet2/ReloadUtilities.h"

FCSReinstancer& FCSReinstancer::Get()
{
	static FCSReinstancer Instance;
	return Instance;
}

void FCSReinstancer::Initialize()
{
	FCSTypeRegistry::Get().GetOnNewClassEvent().AddRaw(this, &FCSReinstancer::AddPendingClass);
	FCSTypeRegistry::Get().GetOnNewStructEvent().AddRaw(this, &FCSReinstancer::AddPendingStruct);
	FCSTypeRegistry::Get().GetOnNewEnumEvent().AddRaw(this, &FCSReinstancer::AddPendingEnum);
}

void FCSReinstancer::AddPendingClass(UClass* OldClass, UClass* NewClass)
{
	ClassesToReinstance.Add(MakeTuple(OldClass, NewClass));
}

void FCSReinstancer::AddPendingStruct(UScriptStruct* OldStruct, UScriptStruct* NewStruct)
{
	StructsToReinstance.Add(MakeTuple(OldStruct, NewStruct));
}

void FCSReinstancer::AddPendingEnum(UEnum* OldEnum, UEnum* NewEnum)
{
	EnumsToReinstance.Add(MakeTuple(OldEnum, NewEnum));
}

void FCSReinstancer::AddPendingInterface(UClass* OldInterface, UClass* NewInterface)
{
	InterfacesToReinstance.Add(MakeTuple(OldInterface, NewInterface));
}

void FCSReinstancer::Reinstance()
{
	TUniquePtr<FCSReload> Reload = MakeUnique<FCSReload>(EActiveReloadType::HotReload, TEXT(""), *GLog);

	auto NotifyChanges = [&Reload](const auto& Container)
	{
		for (const auto& [Old, New] : Container)
		{
			if (!Old || !New)
			{
				continue;
			}

			Reload->NotifyChange(New, Old);
		}
	};

	NotifyChanges(InterfacesToReinstance);
	NotifyChanges(StructsToReinstance);
	NotifyChanges(EnumsToReinstance);
	NotifyChanges(ClassesToReinstance);

	Reload->StartReinstancing(*this);

	ClassesToReinstance.Reset();
	StructsToReinstance.Reset();
	EnumsToReinstance.Reset();
	InterfacesToReinstance.Reset();
	
	CollectGarbage(GARBAGE_COLLECTION_KEEPFLAGS);
}
