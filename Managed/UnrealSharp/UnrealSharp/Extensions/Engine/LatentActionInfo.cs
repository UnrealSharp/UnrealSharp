using UnrealSharp.CoreUObject;

namespace UnrealSharp.Engine;

public partial struct FLatentActionInfo
{
    public FLatentActionInfo(Action action)
    {
        if (action.Target is not UObject target)
        {
            throw new InvalidOperationException("Action target must be a UObject.");
        }
        
        CallbackTarget = target;
        ExecutionFunction = action.Method.Name;
    }
    
    public static implicit operator FLatentActionInfo(Action action)
    {
        return new FLatentActionInfo(action);
    }
}