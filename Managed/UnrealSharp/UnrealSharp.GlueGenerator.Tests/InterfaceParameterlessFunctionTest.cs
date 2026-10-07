using UnrealSharp.Attributes;

namespace TestSourceGen;

[UInterface]
public partial interface IParameterlessInterfaceTest
{
    [UFunction(FunctionFlags.BlueprintCallable)]
    public void ParameterlessVoidCall();

    [UFunction(FunctionFlags.BlueprintCallable)]
    public int ParameterlessReturnCall();

    [UFunction(FunctionFlags.BlueprintCallable)]
    public void ParamCall(int value);

    [UFunction(FunctionFlags.BlueprintEvent)]
    public void ParameterlessNativeEventCall();
}
