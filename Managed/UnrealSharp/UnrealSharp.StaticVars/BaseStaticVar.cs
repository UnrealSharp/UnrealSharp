using System.Diagnostics;
using System.Runtime.Loader;

namespace UnrealSharp.StaticVars;

public class FBaseStaticVar<T>
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private T? _value;
    
    public virtual T? Value
    {
        get => _value;
        set => _value = value;
    }
    
#if WITH_EDITOR
    public FBaseStaticVar()
    {
        AssemblyLoadContext alc = AssemblyLoadContext.GetLoadContext(GetType().Assembly)!;
        alc.Unloading += OnAlcUnloading;
    }

    protected virtual void OnAlcUnloading(AssemblyLoadContext alc)
    {
        alc.Unloading -= OnAlcUnloading;
    }
#endif
    
    public override string ToString()
    {
        var value = Value;
        if (value == null)
        {
            return "null value";
        }
        
        return value.ToString()!;
    }
}