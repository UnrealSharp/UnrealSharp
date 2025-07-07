// Fill out your copyright notice in the Description page of Project Settings.


#include "ManagedHandleExporter.h"

void UManagedHandleExporter::StoreManagedHandle(FGCHandleIntPtr Handle, FSharedGCHandle& Destination) {
    Destination = FSharedGCHandle(Handle);
}

FGCHandleIntPtr UManagedHandleExporter::LoadManagedHandle(const FSharedGCHandle& Source) {
    return Source.GetHandle();
}
