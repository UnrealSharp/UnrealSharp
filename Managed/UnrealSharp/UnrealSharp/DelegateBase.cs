namespace UnrealSharp;

public abstract class FDelegateBase<TDelegate> where TDelegate : class
{
    public TDelegate Invoke;
}