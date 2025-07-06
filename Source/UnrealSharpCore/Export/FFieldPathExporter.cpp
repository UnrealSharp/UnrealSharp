// Fill out your copyright notice in the Description page of Project Settings.


#include "FFieldPathExporter.h"

bool UFFieldPathExporter::IsValid(const TFieldPath<FField>& FieldPath)
{
	return FieldPath != nullptr;
}

bool UFFieldPathExporter::IsStale(const FFieldPath& FieldPath)
{
	return FieldPath.IsStale();
}

void UFFieldPathExporter::FieldPathToString(const FFieldPath& FieldPath, FString* OutString)
{
	*OutString = FieldPath.ToString();
}

bool UFFieldPathExporter::FieldPathsEqual(const FFieldPath& A, const FFieldPath& B)
{
	return A == B;
}

int32 UFFieldPathExporter::GetFieldPathHashCode(const FFieldPath& FieldPath)
{
	// GetHashCode returns a signed integer in C#, but GetTypeHash returns an unsigned integer, thus
	// the cast is necessary
	return static_cast<int32>(GetTypeHash(FieldPath));
}
