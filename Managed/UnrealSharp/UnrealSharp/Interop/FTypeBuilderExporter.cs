using System.Runtime.InteropServices;
using UnrealSharp.Binds;
using UnrealSharp.Core;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FTypeBuilderExporter
{
    public static delegate* unmanaged<char*, char*, char*, long, byte, IntPtr, out NativeBool, IntPtr> NewType_Internal;

    public static delegate* unmanaged<IntPtr, int, void> InitMetaData_Internal;
    public static delegate* unmanaged<IntPtr, char*, char*, void> AddMetaData_Internal;

    public static delegate* unmanaged<IntPtr, char*, char*, char*, char*, uint, void> ModifyClass_Internal;

    public static delegate* unmanaged<IntPtr, int, IntPtr> InitializePropertiesFromTemplate_Internal;
    public static delegate* unmanaged<IntPtr, int, IntPtr> InitializePropertiesFromStruct_Internal;
    
    public static delegate* unmanaged<IntPtr, byte, char*, ulong, char*, int, int, char*, char*, IntPtr> MakeProperty_Internal;
    
    public static delegate* unmanaged<IntPtr, char*, char*, char*, void> ModifyFieldProperty_Internal;
    public static delegate* unmanaged<IntPtr, NativeBool, char*, char*, void> ModifyDefaultComponent_Internal;
    
    public static delegate* unmanaged<IntPtr, int, void> ReserveFunctions_Internal;
    public static delegate* unmanaged<IntPtr, char*, uint, int, IntPtr> MakeFunction_Internal;
    
    public static delegate* unmanaged<IntPtr, int, void> ReserveOverrides_Internal;
    public static delegate* unmanaged<IntPtr, char*, void> MakeOverride_Internal;
    
    public static delegate* unmanaged<IntPtr, int, void> ReserveEnumValues_Internal;
    public static delegate* unmanaged<IntPtr, char*, void> AddEnumValue_Internal;

    public static delegate* unmanaged<IntPtr, int, void> ReserveInterfaces_Internal;
    public static delegate* unmanaged<IntPtr, char*, char*, char*, void> AddInterface_Internal;
    
    public static IntPtr NewType(string typeName, long typeVersion, byte fieldType, Type type, out bool needsRebuild)
    {
        IntPtr handlePtr = GCHandle.ToIntPtr(GCHandleUtilities.AllocateStrongPointer(type, type.Assembly));
        
        fixed (char* assemblyNamePtr = type.Assembly.GetName().Name)
        fixed (char* namespaceNamePtr = type.Namespace)
        fixed (char* typeNamePtr = typeName)
        {
            IntPtr nativeType = NewType_Internal(typeNamePtr, namespaceNamePtr, assemblyNamePtr, typeVersion, fieldType, handlePtr, out var nativeNeedsRebuild);
            needsRebuild = nativeNeedsRebuild.ToManagedBool();
            return nativeType;
        }
    }
    
    public static void InitMetaData(IntPtr owner, int count)
    {
        InitMetaData_Internal(owner, count);
    }
    
    public static void AddMetaData(IntPtr owner, string key, string value)
    {
        fixed (char* keyPtr = key)
        fixed (char* valuePtr = value)
        {
            AddMetaData_Internal(owner, keyPtr, valuePtr);
        }
    }

    public static void ModifyClass(IntPtr parentClass, string name, Type type, string configName, uint flags)
    {
        fixed (char* assemblyNamePtr = type.Assembly.GetName().Name)
        fixed (char* namespaceNamePtr = type.Namespace)
        fixed (char* namePtr = name)
        fixed (char* configNamePtr = configName)
        {
            ModifyClass_Internal(parentClass, namePtr, namespaceNamePtr, assemblyNamePtr, configNamePtr, flags);
        }
    }
    
    public static IntPtr InitTemplateProps(IntPtr owner, int fieldType)
    {
        return InitializePropertiesFromTemplate_Internal(owner, fieldType);
    }
    
    public static IntPtr InitStructProps(IntPtr owner, int count)
    {
        return InitializePropertiesFromStruct_Internal(owner, count);
    }
    
    public static IntPtr NewProperty(IntPtr owner, byte propertyType, string name, ulong flags, string repNotifyFunction, int arrayDim, int lifetimeCondition, string blueprintSetter, string blueprintGetter)
    {
        fixed (char* namePtr = name)
        fixed (char* repNotifyFunctionPtr = repNotifyFunction)
        fixed (char* blueprintSetterPtr = blueprintSetter)
        fixed (char* blueprintGetterPtr = blueprintGetter)
        {
            return MakeProperty_Internal(owner, propertyType, namePtr, flags, repNotifyFunctionPtr, arrayDim, lifetimeCondition, blueprintSetterPtr, blueprintGetterPtr);
        }
    }
    
    public static void ModifyFieldProperty(IntPtr property, string name, Type type)
    {
        fixed (char* namePtr = name)
        fixed (char* typeNamespacePtr = type.Namespace)
        fixed (char* assemblyNamrPtr = type.Assembly.GetName().Name)
        {
            ModifyFieldProperty_Internal(property, namePtr, typeNamespacePtr, assemblyNamrPtr);
        }
    }
    
    public static void ModifyDefaultComponent(IntPtr property, bool isRootComponent, string attachmentComponent, string attachmentSocket)
    {
        fixed (char* classNamePtr = attachmentComponent)
        fixed (char* classNamespacePtr = attachmentSocket)
        {
            ModifyDefaultComponent_Internal(property, isRootComponent.ToNativeBool(), classNamePtr, classNamespacePtr);
        }
    }
    
    public static void InitFunctions(IntPtr owner, int count)
    {
        ReserveFunctions_Internal(owner, count);
    }
    
    public static IntPtr NewFunction(IntPtr owner, string name, uint flags, int paramCount)
    {
        fixed (char* namePtr = name)
        {
            return MakeFunction_Internal(owner, namePtr, flags, paramCount);
        }
    }
    
    public static void InitOverrides(IntPtr owner, int count)
    {
        ReserveOverrides_Internal(owner, count);
    }
    
    public static void NewOverride(IntPtr owner, string nativeName)
    {
        fixed (char* nativeNamePtr = nativeName)
        {
            MakeOverride_Internal(owner, nativeNamePtr);
        }
    }
    
    public static void InitEnumValues(IntPtr owner, int count)
    {
        ReserveEnumValues_Internal(owner, count);
    }
    
    public static void NewEnumValue(IntPtr owner, string name)
    {
        fixed (char* namePtr = name)
        {
            AddEnumValue_Internal(owner, namePtr);
        }
    }
    
    public static void InitInterfaces(IntPtr owner, int count)
    {
        ReserveInterfaces_Internal(owner, count);
    }

    public static void NewInterface(IntPtr owner, string name, Type type)
    {
        fixed (char* namePtr = name)
        fixed (char* typeNamespacePtr = type.Namespace)
        fixed (char* assemblyNamrPtr = type.Assembly.GetName().Name)
        {
            AddInterface_Internal(owner, namePtr, typeNamespacePtr, assemblyNamrPtr);
        }
    }
}
