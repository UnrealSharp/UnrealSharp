#include "CSTestActor.h"

bool ACSTestActor::MyScriptMethod(int32 MyInteger)
{
	return true;
}

bool ACSTestActor::MyNonScriptMethod(int32 MyInteger)
{
	return true;
}

void ACSTestActor::MyTestFunction(TMap<FName, int> TestMap)
{
	for (auto& Elem : TestMap)
	{
		UE_LOG(LogTemp, Warning, TEXT("Key: %s, Value: %d"), *Elem.Key.ToString(), Elem.Value);
	}
}
