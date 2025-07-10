// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"

struct FNativeStructType {
    FString Name;
    bool HasDestructor = false;

    FNativeStructType() = default;
    FNativeStructType(FString Name, const bool HasDestructor) : Name(MoveTemp(Name)), HasDestructor(HasDestructor) {}
};

struct FStructLayoutInfo
{
    TArray<FString> BlittableTypes;
    TArray<FNativeStructType> NativeTypes;

    bool IsEmpty() const
    {
        return BlittableTypes.IsEmpty() && NativeTypes.IsEmpty();
    }
};
