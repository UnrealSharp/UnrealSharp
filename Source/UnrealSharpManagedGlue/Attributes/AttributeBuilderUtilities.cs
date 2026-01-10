using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue.Attributes;

public static class AttributeBuilderUtilities
{
	public static void AddGeneratedTypeAttribute(this AttributeBuilder attributeBuilder, UhtType type)
	{
		attributeBuilder.AddAttribute("GeneratedType");
		attributeBuilder.AddArgument($"\"{type.EngineName}\"");
        
		string fullName = type.GetNamespace() + "." + type.EngineName;
		attributeBuilder.AddArgument($"\"{fullName}\"");
	}
	
	public static void AddGeneratedDelegateTypeAttribute(this AttributeBuilder attributeBuilder, UhtFunction delegateFunction, string csharpDelegateName)
	{
		attributeBuilder.AddAttribute("GeneratedType");
		
		// First parameter: Original UE reflection name (used for runtime UDelegateFunction lookup)
		attributeBuilder.AddArgument($"\"{delegateFunction.EngineName}\"");
		
		// Second parameter: Modified full C# type name (with Outer prefix to avoid conflicts)
		string fullName = delegateFunction.GetNamespace() + "." + csharpDelegateName;
		attributeBuilder.AddArgument($"\"{fullName}\"");
	}
}