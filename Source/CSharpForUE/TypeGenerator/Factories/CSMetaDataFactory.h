#pragma once

#include "TypeGenerator/Register/MetaData/CSUnrealType.h"

#define REGISTER_METADATA_WITH_NAME(CustomName, MetaDataName) \
	MetaDataFactoryMap.Add(CustomName, \
		[]() \
		{ \
			return MakeShared<MetaDataName>(); \
		});

#define REGISTER_METADATA(PropertyName, MetaDataName) \
	REGISTER_METADATA_WITH_NAME(PropertyName, MetaDataName)

class CSMetaDataFactory
{
public:
	
	static TSharedPtr<FCSUnrealType> Create(const TSharedPtr<FJsonObject>& PropertyMetaData);
	
private:
	static void Initialize();
};
