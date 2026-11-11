#pragma once

#include "SourceCodeNavigation.h"

class FCSSourceCodeNavigation : public ISourceCodeNavigationHandler
{
public:
	static bool OpenClass(const UClass* Class);

	virtual bool CanNavigateToClass(const UClass* Class) override;
	virtual bool NavigateToClass(const UClass* Class) override;
};
