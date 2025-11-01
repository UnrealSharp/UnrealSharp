using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnrealSharp.Attributes;

[AttributeUsage(AttributeTargets.Delegate)]
public abstract class UDelegateAttribute : Attribute
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    public Type? WrapperType { get; set; }
}
