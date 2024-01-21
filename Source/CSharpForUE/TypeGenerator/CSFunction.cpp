// Fill out your copyright notice in the Description page of Project Settings.

#include "CSFunction.h"

void UCSFunction::SetManagedMethod(void* InManagedMethod)
{
	ManagedMethod = InManagedMethod;
}

void* UCSFunction::GetManagedMethod() const
{
	return ManagedMethod;
}
