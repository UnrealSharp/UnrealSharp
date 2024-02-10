namespace UnrealSharp;

internal struct DelegateData
{
    public WeakObjectData Object;
    public Name FunctionName;
}

public abstract class Delegate<TDelegate> : DelegateBase<TDelegate> where TDelegate : class
{
    private DelegateData _data;
    
    public Name FunctionName => _data.FunctionName;
    public WeakObject<UnrealSharpObject> Object => new(_data.Object);
    
    internal Delegate(DelegateData data)
    {
        _data = data;
    }
}

public class SimpleDelegateMarshaller<TDelegate> where TDelegate : IDelegateBase, new()
{
    public static TDelegate FromNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner)
    {
        return BlittableMarshaller<TDelegate>.FromNative(nativeBuffer, arrayIndex, owner);
    }

    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner, TDelegate obj)
    {
        BlittableMarshaller<TDelegate>.ToNative(nativeBuffer, arrayIndex, owner, obj);
    }
}