// Fill out your copyright notice in the Description page of Project Settings.


#include "TStrongObjectPtrExporter.h"

void UTStrongObjectPtrExporter::ConstructStrongObjectPtr(TStrongObjectPtr<UObject>* Ptr, UObject* Object)
{
    static_assert(sizeof(TStrongObjectPtr<UObject>) == sizeof(UObject*), "TStrongObjectPtr<UObject> must be the same size as UObject*");
    check(Ptr != nullptr);
    std::construct_at(Ptr, Object);
}

void UTStrongObjectPtrExporter::DestroyStrongObjectPtr(TStrongObjectPtr<UObject>* Ptr)
{
    static_assert(sizeof(TStrongObjectPtr<UObject>) == sizeof(UObject*), "TStrongObjectPtr<UObject> must be the same size as UObject*");
    check(Ptr != nullptr);
    std::destroy_at(Ptr);
}
