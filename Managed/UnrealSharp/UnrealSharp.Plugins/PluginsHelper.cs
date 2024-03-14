using System.Reflection;
using UnrealSharp.Plugins.Attributes;

namespace UnrealSharp.Plugins;

public static class PluginsHelper
{
    public static Type? FindEntryPoint(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (type.GetCustomAttribute(typeof(EntryPointAttribute)) != null)
            {
                return type;
            }
        }

        return null;
    }
    
    public static MethodInfo? FindEntryPointMethod(Assembly assembly)
    {
        Type? entryPointClass = FindEntryPoint(assembly);
        
        if (entryPointClass == null)
        {
            return null;
        }
        
        foreach (var method in entryPointClass.GetMethods())
        {
            if (method.GetCustomAttribute(typeof(EntryPointAttribute)) != null)
            {
                return method;
            }
        }
        
        throw new EntryPointNotFoundException("Couldn't find entry point method");
    }
}