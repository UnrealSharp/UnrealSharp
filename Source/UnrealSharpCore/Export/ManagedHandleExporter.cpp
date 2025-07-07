// Fill out your copyright notice in the Description page of Project Settings.


#include "ManagedHandleExporter.h"

void UManagedHandleExporter::StoreManagedHandle(const FGCHandleIntPtr Handle, FSharedGCHandle& Destination)
{
    Destination = FSharedGCHandle(Handle);
}

FGCHandleIntPtr UManagedHandleExporter::LoadManagedHandle(const FSharedGCHandle& Source)
{
    return Source.GetHandle();
}

void UManagedHandleExporter::StoreUnmanagedMemory(const void* Source, FUnmanagedDataStore& Destination, const int32 Size)
{
    check(Size > 0)
    Destination.CopyDataIn(Source, Size);
}

void UManagedHandleExporter::LoadUnmanagedMemory(const FUnmanagedDataStore& Source, void* Destination, const int32 Size)
{
    check(Size > 0)
    Source.CopyDataOut(Destination, Size);
}
