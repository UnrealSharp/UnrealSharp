using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace UnrealSharpWeaver.MetaData;

public class FunctionMetaData : BaseMetaData
{ 
    public string Name { get; set; }
    public PropertyMetaData[] Parameters { get; }
    public PropertyMetaData? ReturnValue { get; }
    public FunctionFlags FunctionFlags { get; set; }
    public bool IsBlueprintEvent { get; private set; }
    public bool IsRpc { get; private set; }
    public AccessProtection AccessProtection { get; set; }
    
    public FunctionMetaData(MethodDefinition method)
    {
        Name = method.Name;

        if (method.IsPublic)
        {
            AccessProtection = AccessProtection.Public;

        }
        else if (method.IsPrivate)
        {
            AccessProtection = AccessProtection.Private;
        }
        else
        {
            AccessProtection = AccessProtection.Protected;
        }

        bool hasOutParams = false;
        if (method.ReturnType.FullName != "System.Void")
        {
            hasOutParams = true;
            try
            {
                ReturnValue = PropertyMetaData.FromTypeReference(method.ReturnType, "ReturnValue", ParameterType.ReturnValue);
            }
            catch (InvalidPropertyException e)
            {
                throw new InvalidUnrealFunctionException(method, $"'{method.ReturnType.FullName}' is invalid for unreal function return value.", e);
            }
        }

        Parameters = new PropertyMetaData[method.Parameters.Count];
        for (int i = 0; i < method.Parameters.Count; ++i)
        {
            ParameterDefinition param = method.Parameters[i];
            if (param.IsOut)
            {
                hasOutParams = true;
            }

            try
            {
                bool byReference = false;
                TypeReference paramType = param.ParameterType;
                if (paramType.IsByReference)
                {
                    byReference = true;
                    paramType = ((ByReferenceType)paramType).ElementType;
                }

                ParameterType modifier = ParameterType.Value;
                if (param.IsOut)
                {
                    modifier = ParameterType.Out;
                }
                else if (byReference)
                {
                    modifier = ParameterType.Ref;
                }
                
                Parameters[i] = PropertyMetaData.FromTypeReference(paramType, param.Name, modifier);
                
            }
            catch (InvalidPropertyException e)
            {
                throw new InvalidUnrealFunctionException(method, $"'{param.ParameterType.FullName}' is invalid for unreal function parameter '{param.Name}'.", e);
            }
        }

        FunctionFlags flags = (FunctionFlags) GetFlags(method, "FunctionFlagsMapAttribute");

        if (hasOutParams)
        {
            flags ^= FunctionFlags.HasOutParms;
        }
        
        AddMetadataAttributes(method.CustomAttributes);

        // Do some extra verification.  Matches functionality in FHeaderParser.
        if (flags.HasFlag(FunctionFlags.BlueprintNativeEvent))
        {
            IsBlueprintEvent = true;
            
            if (!method.IsVirtual && method.Body.Instructions.Count == 1 && method.Body.Instructions[0].OpCode == OpCodes.Ret)
            {
                flags ^= FunctionFlags.Native;
            }

            if (flags.HasFlag(FunctionFlags.Net))
            {
                throw new InvalidUnrealFunctionException(method, "BlueprintImplementable methods cannot be replicated!");
            }
        }

        switch (AccessProtection)
        {
            case AccessProtection.Public:
                flags |= FunctionFlags.Public;
                break;
            case AccessProtection.Protected:
                flags |= FunctionFlags.Protected;
                break;
            case AccessProtection.Private:
                flags |= FunctionFlags.Private;
                break;
            default:
                throw new InvalidUnrealFunctionException(method, "Unknown access level");
        }

        CustomAttribute? ufunctionAttribute = FindAttribute(method.CustomAttributes, "UFunctionAttribute");

        if (ufunctionAttribute != null)
        {
            AddBaseAttributes(ufunctionAttribute);
        }

        if (method.IsStatic)
        {
            flags |= FunctionFlags.Static;
        }
        
        FunctionFlags relevantFlags = FunctionFlags.NetServer | FunctionFlags.NetMulticast | FunctionFlags.NetClient;
        bool isRPC = (flags & relevantFlags) != 0;
        
        if (isRPC)
        {
            flags |= FunctionFlags.Net;
            if (method.ReturnType.FullName != "System.Void")
            {
                throw new InvalidUnrealFunctionException(method, "RPCs can't have return values.");
            }
        }
        
        FunctionFlags = flags;
    }

    public static bool IsUFunction(MethodDefinition method)
    {
        if (!method.HasCustomAttributes)
        {
            return false;
        }
        
        CustomAttribute? functionAttribute = FindAttribute(method.CustomAttributes, "UFunctionAttribute");
        return functionAttribute != null;
    }

    public static bool IsBlueprintEventOverride(MethodDefinition method)
    {
        MethodDefinition basemostMethod = method.GetOriginalBaseMethod();
        if (basemostMethod != method && basemostMethod.HasCustomAttributes)
        {
            CustomAttribute? isUnrealFunction = FindAttribute(basemostMethod.CustomAttributes, "UFunctionAttribute");
            if (isUnrealFunction != null)
            {
                return true;
            }
        }

        return false;
    }

    public static FunctionMetaData[] PopulateFunctionArray(TypeDefinition type)
    {
        bool success = true;
        var functions = type.Methods.SelectWhereErrorEmit(IsUFunction,x => new FunctionMetaData(x), out success).ToArray();
        return functions;
    }

    public static bool IsInterfaceFunction(TypeDefinition type, string methodName)
    {
        foreach (var typeInterface in type.Interfaces)
        {
            var interfaceType = typeInterface.InterfaceType.Resolve();
            foreach (var interfaceMethod in interfaceType.Methods)
            {
                if (interfaceMethod.Name == methodName)
                {
                    return true;
                }
            }
        }
        return false;
    }
}