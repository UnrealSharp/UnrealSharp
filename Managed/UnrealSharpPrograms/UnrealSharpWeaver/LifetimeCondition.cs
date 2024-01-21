namespace UnrealSharpWeaver;

public enum LifetimeCondition
{
	None = 0,
	InitialOnly = 1,
	OwnerOnly = 2,	
	SkipOwner = 3,	
	SimulatedOnly = 4,	
	AutonomousOnly = 5,
	SimulatedOrPhysics = 6,
	InitialOrOwner = 7,
	Custom = 8,		
	ReplayOrOwner = 9,
	ReplayOnly = 10,		
	SimulatedOnlyNoReplay = 11,	
	SimulatedOrPhysicsNoReplay = 12,
	SkipReplay = 13,
	Dynamic = 14,				
	Never = 15,
};