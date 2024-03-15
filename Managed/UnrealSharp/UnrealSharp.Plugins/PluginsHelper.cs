using System.Reflection;

namespace UnrealSharp.Plugins;

public static class PluginsHelper
{
    public static Module? FindModule(Assembly assembly, object[]? optionalArguments = null)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (type.BaseType is not { Name: nameof(Module) })
            {
                continue;
            }
            
            return (Module) Activator.CreateInstance(type, 
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.CreateInstance, 
                null, optionalArguments, 
                null, 
                null)!;
        }

        return null;
    }
}