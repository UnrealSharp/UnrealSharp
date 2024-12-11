using System.Diagnostics;

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
}