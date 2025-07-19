// Fill out your copyright notice in the Description page of Project Settings.


#include "FSubsystemCollectionBaseRefExporter.h"

USubsystem* UFSubsystemCollectionBaseRefExporter::InitializeDependency(FSubsystemCollectionBase* Collection, UClass* SubsystemClass)
{
    return Collection->InitializeDependency(SubsystemClass);
}
