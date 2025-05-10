// Fill out your copyright notice in the Description page of Project Settings.


#include "CSSoftObjectPathExtensions.h"

UObject* UCSSoftObjectPathExtensions::ResolveObject(const FSoftObjectPath& SoftObjectPath)
{
	return SoftObjectPath.ResolveObject();
}
