#pragma once

#include "CoreMinimal.h"
#include "Dom/JsonObject.h"
#include "Json/CSRapidJsonUtilties.h"

using namespace UnrealSharp::RapidJson;

struct FCSReflectionDataBase
{
public:
	virtual ~FCSReflectionDataBase() = default;
protected:
	virtual bool Serialize(FConstObject JsonObject) = 0;
};
