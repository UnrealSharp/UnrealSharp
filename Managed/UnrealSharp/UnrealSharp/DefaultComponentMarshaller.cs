using UnrealSharp.Core.Marshallers;
using UnrealSharp.Engine;
using UnrealSharp.UnrealSharpCore;

namespace UnrealSharp;

public static class DefaultComponentMarshaller<T> where T : UActorComponent
{
    public static T FromNative(AActor owner, string componentName, IntPtr buffer, int offset)
    {
        T? defaultComponent = ObjectMarshaller<T>.FromNative(buffer, offset);
        
        // If the default component is null, we're in construction phase and the component is not spawned yet.
        // So we access the template instead which BP uses to spawn the component later.
        // This is basically the same what the engine does with BP components.
        if (defaultComponent == null)
        { 
            defaultComponent = (T) UCSActorExtensions.GetComponentTemplate(owner, componentName);
        }
        
        return defaultComponent;
    }

    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, T obj)
    {
        ObjectMarshaller<T>.ToNative(nativeBuffer, arrayIndex, obj);
    }
}