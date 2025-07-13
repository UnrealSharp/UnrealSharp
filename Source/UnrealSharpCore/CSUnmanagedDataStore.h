// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSUnmanagedDataStore.generated.h"

USTRUCT()
struct FUnmanagedDataStore
{
    GENERATED_BODY()

private:
    static constexpr size_t SmallStorageSize = 56;
    using FSmallStorage = std::array<std::byte, SmallStorageSize>;

    struct FLargeStorageDeleter 
    {
        void operator()(void* Ptr) const
        {
            FMemory::Free(Ptr);
        }
    };

public:
    FUnmanagedDataStore() = default;

    void CopyDataIn(const void* InData, const size_t Size)
    {
        if (Size <= SmallStorageSize)
        {
            Data.Emplace<FSmallStorage>();
            FMemory::Memcpy(Data.Get<FSmallStorage>().data(), InData, Size);
        }
        else
        {
            Data.Emplace<TSharedPtr<void>>(FMemory::Malloc(Size), FLargeStorageDeleter());
            FMemory::Memcpy(Data.Get<TSharedPtr<void>>().Get(), InData, Size);
        }
    }

    void CopyDataOut(void* OutData, const size_t Size) const
    {
        if (Size <= SmallStorageSize)
        {
            FMemory::Memcpy(OutData, Data.Get<FSmallStorage>().data(), Size);
        }
        else
        {
            FMemory::Memcpy(OutData, Data.Get<TSharedPtr<void>>().Get(), Size);
        }
    }

private:
    TVariant<FSmallStorage, TSharedPtr<void>> Data;
    
};
