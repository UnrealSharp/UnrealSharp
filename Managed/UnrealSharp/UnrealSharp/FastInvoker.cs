using System.Linq.Expressions;
using System.Reflection;

namespace UnrealSharp;

public class FastInvoker
{
    static readonly ParameterExpression InstanceArgument = Expression.Parameter(typeof(object), "instance");
    static readonly ParameterExpression BufferArgument = Expression.Parameter(typeof(IntPtr), "buffer");
    static readonly ParameterExpression ReturnValueBufferArgument = Expression.Parameter(typeof(IntPtr), "returnBuffer");
    public readonly Action<object, IntPtr, IntPtr> Invoke;
    
    public FastInvoker(MethodInfo methodInfo)
    {
        Expression instanceConvertExpression = Expression.Convert(InstanceArgument, methodInfo.DeclaringType); 
        MethodCallExpression callExpression = Expression.Call(instanceConvertExpression, methodInfo, BufferArgument, ReturnValueBufferArgument);
        Invoke = Expression.Lambda<Action<object, IntPtr, IntPtr>>(
            callExpression, InstanceArgument, BufferArgument, ReturnValueBufferArgument).Compile();
    }
}