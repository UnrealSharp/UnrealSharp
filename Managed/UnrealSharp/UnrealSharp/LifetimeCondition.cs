namespace UnrealSharp;

public enum LifetimeCondition
{
	// This property has no condition, and will send anytime it changes
	None = 0,
	
	// This property will only attempt to send on the initial bunch
	InitialOnly = 1,
	
	// This property will only send to the actor's owner
	OwnerOnly = 2,	
	
	// This property send to every connection EXCEPT the owner
	SkipOwner = 3,	
	
	// This property will only send to simulated actors
	SimulatedOnly = 4,	
	
	// This property will only send to autonomous actors
	AutonomousOnly = 5,
	
	// This property will send to simulated OR bRepPhysics actors
	SimulatedOrPhysics = 6,
	
	// This property will send on the initial packet, or to the actors owner
	InitialOrOwner = 7,
	
	// This property has no particular condition, but wants the ability to toggle on/off via SetCustomIsActiveOverride
	Custom = 8,		
	
	// This property will only send to the replay connection, or to the actors owner
	ReplayOrOwner = 9,
	
	// This property will only send to the replay connection
	ReplayOnly = 10,		
	
	// This property will send to actors only, but not to replay connections
	SimulatedOnlyNoReplay = 11,	
	
	// This property will send to simulated Or bRepPhysics actors, but not to replay connections
	SimulatedOrPhysicsNoReplay = 12,
	
	// This property will not send to the replay connection
	SkipReplay = 13,
	
	// This property wants to override the condition at runtime. Defaults to always replicate until you override it to a new condition.
	Dynamic = 14,				
	
	// This property will never be replicated
	Never = 15,
};