#pragma once

struct FUnrealType;

#define REGISTER_METADATA_WITH_NAME(CustomName, MetaDataName) \
	MetaDataFactoryMap.Add(CustomName, \
		[]() \
		{ \
			return MakeShared<MetaDataName>(); \
		});

#define REGISTER_METADATA(PropertyName, MetaDataName) \
	REGISTER_METADATA_WITH_NAME(PropertyName::StaticClass()->GetFName(), MetaDataName)

class CSMetaDataFactory
{
public:
	
	static TSharedPtr<FUnrealType> Create(const TSharedPtr<FJsonObject>& PropertyMetaData);
	
private:
	static void Initialize();
};
