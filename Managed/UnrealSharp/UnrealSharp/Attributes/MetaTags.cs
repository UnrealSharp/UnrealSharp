#pragma warning disable CS9113 // Suppress 'Parameter is unread' on this file as these parameters are used via reflection
namespace UnrealSharp.Attributes.MetaTags;

//====================================================
// MetaData Tags
// See: https://dev.epicgames.com/documentation/en-us/unreal-engine/metadata-specifiers-in-unreal-engine
//
// Note: If a MetaData Tag is not listed, you can use [UMetaData("key","value")] to specify the key/value directly
//====================================================


//----------------------------------------------------
// Shared Metadata Specifiers
//----------------------------------------------------
#region Shared
/// <summary>
/// [DisplayName]
/// The name to display for this property, instead of the code-generated name.
/// </summary>
/// <param name="DisplayName">Property Name</param>
[AttributeUsage(AttributeTargets.All)]
public sealed class DisplayNameAttribute(string DisplayName) : Attribute { }

/// <summary>
/// [ToolTip]
/// Overrides the automatically generated tooltip from code comments.
/// </summary>
/// <param name="tooltip">Hand-written tooltip</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum  | AttributeTargets.Field | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property)]
public class ToolTipAttribute(string tooltip) : Attribute { }

/// <summary>
/// [ShortToolTip]
/// A short tooltip that is used in some contexts where the full tooltip might be overwhelming, such as the Parent Class Picker dialog.
/// </summary>
/// <param name="ShortToolTip">Short tooltip</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
public sealed class ShortToolTipAttribute(string ShortToolTip) : Attribute { }

/// <summary>
/// [ScriptName]
/// The name to use for this clas, property, or function when exporting it to a scripting language. You may include deprecated names as additional semi-colon-separated entries.
/// </summary>
/// <param name="ScriptName">DisplayName</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method)]
public sealed class ScriptNameAttribute(string ScriptName) : Attribute { }

#endregion

//----------------------------------------------------
// Class Metadata Specifiers
// Classes can use the following Metatag Specifiers:	
//----------------------------------------------------
#region Class
/// <summary>
/// [BlueprintSpawnableComponent]
/// If present, the component Class can be spawned by a Blueprint.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class BlueprintSpawnableComponentAttribute : Attribute { }

/// <summary>
/// [BlueprintThreadSafe]
/// Only valid on Blueprint function libraries. This specifier marks the functions in this class as callable on non-game threads in animation Blueprints.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class BlueprintThreadSafeAttribute : Attribute { }

/// <summary>
/// [ChildCannotTick]
/// Used for Actor and Component classes. If the native class cannot tick, Blueprint-generated classes based on this Actor or Component can never tick, even if bCanBlueprintsTickByDefault is true.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ChildCannotTickAttribute : Attribute { }

/// <summary>
/// [ChildCanTick]
/// Used for Actor and Component classes. If the native class cannot tick, Blueprint-generated classes based on this Actor or Component can have the bCanEverTick flag overridden, even if bCanBlueprintsTickByDefault is false.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ChildCanTickAttribute : Attribute { }

/// <summary>
/// [DeprecatedNode]
/// For behavior tree nodes, indicates that the class is deprecated and will display a warning when compiled.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class DeprecatedNodeAttribute : Attribute { }

/// <summary>
/// [DeprecationMessage]
/// Deprecated classes with this metadata will include this text with the standard deprecation warning that Blueprint Scripts generate during compilation.
/// </summary>
/// <param name="DeprecationMessage">Message Text</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class DeprecationMessageAttribute(string DeprecationMessage) : Attribute { }


/// <summary>
/// [DontUseGenericSpawnObject]
/// Do not spawn an Object of the class using Generic Create Object node in Blueprint Scripts; this specifier applies only to Blueprint-type classes that are neither Actors nor Actor Components.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class DontUseGenericSpawnObjectAttribute : Attribute { }

/// <summary>
/// [ExposedAsyncProxy]
/// Expose a proxy Object of this class in Async Task nodes.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ExposedAsyncProxyAttribute : Attribute { }

/// <summary>
/// [IgnoreCategoryKeywordsInSubclasses]
/// Used to make the first subclass of a class ignore all inherited ShowCategories and HideCategories Specifiers.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class IgnoreCategoryKeywordsInSubclassesAttribute : Attribute { }

/// <summary>
/// [IsBlueprintBase]
/// States that this class is (or is not) an acceptable base class for creating Blueprints, similar to the Blueprintable or NotBlueprintable Specifiers.
/// </summary>
/// <param name="IsBlueprintBase">true/false</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class IsBlueprintBaseAttribute(string IsBlueprintBase) : Attribute { }

/// <summary>
/// [KismetHideOverrides]
/// List of Blueprint events that are not allowed to be overridden.
/// </summary>
/// <param name="KismetHideOverrides">Event1, Event2, ..</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class KismetHideOverridesAttribute(string KismetHideOverrides) : Attribute { }

/// <summary>
/// [ProhibitedInterfaces]
/// Lists Interfaces that are not compatible with the class.
/// </summary>
/// <param name="ProhibitedInterfaces">Interface1, Interface2, ..</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ProhibitedInterfacesAttribute(string ProhibitedInterfaces) : Attribute { }

/// <summary>
/// [RestrictedToClasses]
/// Blueprint function library classes can use this to restrict usage to the classes named in the list.
/// </summary>
/// <param name="RestrictedToClasses">Class1, Class2, ..</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class RestrictedToClassesAttribute(string RestrictedToClasses) : Attribute { }

/// <summary>
/// [ShowWorldContextPin]
/// Indicates that Blueprint nodes placed in graphs owned by this class must show their World context pins, even if they are normally hidden, because Objects of this class cannot be used as World context.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ShowWorldContextPinAttribute : Attribute { }

/// <summary>
/// [UsesHierarchy]
/// Indicates the class uses hierarchical data. Used to instantiate hierarchical editing features in Details panels.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class UsesHierarchyAttribute : Attribute { }

#endregion

//----------------------------------------------------
// Enum Metadata Specifiers
// Enumerated types can use the following Metadata Specifiers:	
//----------------------------------------------------
#region Enum
/// <summary>
/// [Bitflags]
/// Indicates that this enumerated type can be used as flags by integer UPROPERTY variables that are set up with the Bitmask Metadata Specifier.
/// </summary>
[AttributeUsage(AttributeTargets.Enum)]
public sealed class BitflagsAttribute : Attribute { }

/// <summary>
/// [Experimental]
/// Labels this type as experimental and unsupported.
/// </summary>
[AttributeUsage(AttributeTargets.Enum)]
public sealed class ExperimentalAttribute : Attribute { }
#endregion

//----------------------------------------------------
// Interface Metadata Specifiers
//----------------------------------------------------
#region Interface
/// <summary>
/// [CannotImplementInterfaceInBlueprint]
/// This interface may not contain BlueprintImplementableEvent or BlueprintNativeEvent functions, other than internal-only functions. If it contains Blueprint-callable functions that are not blueprint-defined, those functions must be implemented in native code.
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class CannotImplementInterfaceInBlueprintAttribute : Attribute { }
#endregion


//----------------------------------------------------
// Struct Metadata Specifiers
//----------------------------------------------------
#region Struct
/// <summary>
/// [HasNativeBreak]
/// Indicates that this struct has a custom Break Struct node. The module, class, and function name must be provided.
/// </summary>
/// <param name="HasNativeBreak">Module.Class.Function</param>
[AttributeUsage(AttributeTargets.Struct)]
public sealed class HasNativeBreakAttribute(string HasNativeBreak) : Attribute { }

/// <summary>
/// [HasNativeMake]
/// Indicates that this struct has a custom Make Struct node. The module, class, and function name must be provided.
/// </summary>
/// <param name="HasNativeMake">Module.Class.Function</param>
[AttributeUsage(AttributeTargets.Struct)]
public sealed class HasNativeMakeAttribute(string HasNativeMake) : Attribute { }

/// <summary>
/// [HiddenByDefault]
/// Pins in Make Struct and Break Struct nodes are hidden by default.
/// </summary>
[AttributeUsage(AttributeTargets.Struct)]
public sealed class HiddenByDefaultAttribute : Attribute { }

#endregion


//----------------------------------------------------
// Function Metadata Specifiers
//----------------------------------------------------
#region Function (Method)
/// <summary>
/// [AdvancedDisplay]
/// The comma-separated list of parameters will show up as advanced pins (requiring UI expansion).
/// or
/// Replace N with a number, and all parameters after the Nth will show up as advanced pins (requiring UI expansion). For example, 'AdvancedDisplay=2' will mark all but the first two parameters as advanced).
/// </summary>
/// <param name="AdvancedDisplay">Parameter1, Parameter2, .. or N</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AdvancedDisplayAttribute(string AdvancedDisplay) : Attribute {}

/// <summary>
/// [ArrayParm]
/// Indicates that a BlueprintCallable function should use a Call Array Function node and that the listed parameters should be treated as wild card array properties.
/// </summary>
/// <param name="ArrayParm">Parameter1, Parameter2, ..</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ArrayParmAttribute(string ArrayParm) : Attribute { }

/// <summary>
/// [ArrayTypeDependentParams]
/// When ArrayParm is used, this specifier indicates one parameter which will determine the types of all parameters in the ArrayParm list.
/// </summary>
/// <param name="ArrayTypeDependentParams">Parameter</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ArrayTypeDependentParamsAttribute(string ArrayTypeDependentParams) : Attribute { }

/// <summary>
/// [AutoCreateRefTerm]
/// The listed parameters, although passed by reference, will have an automatically created default if their pins are left disconnected. This is a convenience feature for Blueprints, often used on array pins.
/// </summary>
/// <param name="AutoCreateRefTerm">Parameter1, Parameter2, ..</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AutoCreateRefTermAttribute(string AutoCreateRefTerm) : Attribute { }

/// <summary>
/// [BlueprintAutocast]
/// Used only by static BlueprintPure functions from a Blueprint function library. A cast node will be automatically added for the return type and the type of the first parameter of the function.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class BlueprintAutocastAttribute : Attribute { }

/// <summary>
/// [BlueprintInternalUseOnly]
/// This function is an internal implementation detail, used to implement another function or node. It is never directly exposed in a Blueprint graph.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class BlueprintInternalUseOnlyAttribute : Attribute { }

/// <summary>
/// [BlueprintProtected]
/// This function can only be called on the owning Object in a Blueprint. It cannot be called on another instance.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class BlueprintProtectedAttribute : Attribute { }

/// <summary>
/// [CallableWithoutWorldContext]
/// Used for BlueprintCallable functions that have a WorldContext pin to indicate that the function can be called even if its Class does not implement the GetWorld function.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class CallableWithoutWorldContextAttribute : Attribute { }

/// <summary>
/// [CommutativeAssociativeBinaryOperator]
/// Indicates that a BlueprintCallable function should use the Commutative Associative Binary node. This node lacks pin names, but features an Add Pin button that creates additional input pins.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class CommutativeAssociativeBinaryOperatorAttribute : Attribute { }

/// <summary>
/// [CompactNodeTitle]
/// Indicates that a BlueprintCallable function should display in the compact display mode, and provides the name to display in that mode.
/// </summary>
/// <param name="CompactNodeTitle">Name</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class CompactNodeTitleAttribute(string CompactNodeTitle) : Attribute { }

/// <summary>
/// [CustomStructureParam]
/// The listed parameters are all treated as wildcards. This specifier requires the UFUNCTION-level specifier, CustomThunk, which will require the user to provide a custom exec function. In this function, the parameter types can be checked and the appropriate function calls can be made based on those parameter types. The base UFUNCTION should never be called, and should assert or log an error if it is. To declare a custom exec function, use the syntax DECLARE_FUNCTION(execMyFunctionName) where MyFunctionName is the name of the original function.
/// </summary>
/// <param name="CustomStructureParam">Parameter1, Parameter2, ..</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class CustomStructureParamAttribute(string CustomStructureParam) : Attribute { }

/// <summary>
/// [DefaultToSelf]
/// For BlueprintCallable functions, this indicates that the Object property's named default value should be the self context of the node.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class DefaultToSelfAttribute : Attribute { }

/// <summary>
/// [DeprecatedFunction]
/// Any Blueprint references to this function will cause compilation warnings telling the user that the function is deprecated. You can add to the deprecation warning message (for example, to provide instructions on replacing the deprecated function) using the DeprecationMessage metadata specifier.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class DeprecatedFunctionAttribute : Attribute { }


/// <summary>
/// [DeterminesOutputType]
/// The return type of the function will dynamically change to match the input that is connected to the named parameter pin. The parameter should be a templated type like TSubClassOf<X> or TSoftObjectPtr<X>, where the function's original return type is X* or a container with X* as the value type, such as TArray<X*>.
/// </summary>
/// <param name="DeterminesOutputType">Parameter</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class DeterminesOutputTypeAttribute(string DeterminesOutputType) : Attribute { }

/// <summary>
/// [DevelopmentOnly]
/// Functions marked as DevelopmentOnly will only run in Development mode. This is useful for functionality like debug output, which is expected not to exist in shipped products.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class DevelopmentOnlyAttribute : Attribute { }

/// <summary>
/// [ExpandEnumAsExecs("enum")]
/// For BlueprintCallable functions, this indicates that one input execution pin should be created for each entry in the enum used by the parameter.
/// The parameter must be of an enumerated type that has the UENUM tag and be set as an out parameter on the function
/// </summary>
/// <param name="ExpandEnumAsExecs">name of out enum parameter to use</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ExpandEnumAsExecsAttribute(string ExpandEnumAsExecs) : Attribute { }

/// <summary>
/// [ForceAsFunction]
/// Change a BlueprintImplementableEvent with no return value from an event into a function.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ForceAsFunctionAttribute : Attribute { }

/// <summary>
/// [HidePin]
/// For BlueprintCallable functions, this indicates that the parameter pin should be hidden from the user's view. Only one pin per function can be hidden in this manner.
/// </summary>
/// <param name="HidePin">Parameter</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class HidePinAttribute(string HidePin) : Attribute { }

/// <summary>
/// [HideSelfPin]
/// Hides the "self" pin, which indicates the object on which the function is being called. The "self" pin is automatically hidden on BlueprintPure functions that are compatible with the calling Blueprint's Class. Functions that use the HideSelfPin Meta Tag frequently also use the DefaultToSelf Specifier.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class HideSelfPinAttribute : Attribute { }

/// <summary>
/// [InternalUseParam]
/// Similar to HidePin, this hides the named parameter's pin from the user's view, and can only be used for one parameter per function.
/// </summary>
/// <param name="InternalUseParam">Parameter</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class InternalUseParamAttribute(string InternalUseParam) : Attribute { }

/// <summary>
/// [KeyWords]
/// Specifies a set of keywords that can be used when searching for this function, such as when placing a node to call the function in a Blueprint Graph.
/// </summary>
/// <param name="KeyWords">Set Of Keywords</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class KeyWordsAttribute(string KeyWords) : Attribute { }

/// <summary>
/// [Latent]
/// Indicates a latent action. Latent actions have one parameter of type FLatentActionInfo, and this parameter is named by the LatentInfo specifier.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class LatentAttribute : Attribute { }

/// <summary>
/// [LatentInfo]
/// For Latent BlueprintCallable functions, indicates which parameter is the LatentInfo parameter.
/// </summary>
/// <param name="LatentInfo">Parameter</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class LatentInfoAttribute(string LatentInfo) : Attribute { }

/// <summary>
/// [MaterialParameterCollectionFunction]
/// For BlueprintCallable functions, indicates that the material override node should be used.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class MaterialParameterCollectionFunctionAttribute : Attribute { }

/// <summary>
/// [NativeBreakFunc]
/// For BlueprintCallable functions, indicates that the function should be displayed the same way as a standard Break Struct node.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class NativeBreakFuncAttribute : Attribute { }

/// <summary>
/// [NotBlueprintThreadSafe]
/// Only valid in Blueprint function libraries. This function will be treated as an exception to the owning Class's general BlueprintThreadSafe metadata.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class NotBlueprintThreadSafeAttribute : Attribute { }




/// <summary>
/// [UnsafeDuringActorConstruction]
/// This function is not safe to call during Actor construction.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class UnsafeDuringActorConstructionAttribute : Attribute { }

/// <summary>
/// [WorldContext]
/// Used by BlueprintCallable functions to indicate which parameter determines the World in which the operation takes place.
/// </summary>
/// <param name="WorldContext">Parameter</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class WorldContextAttribute(string WorldContext) : Attribute { }
#endregion


//----------------------------------------------------
// Property Metadata Specifiers
//----------------------------------------------------
#region Property
/// <summary>
/// [AllowAbstract]
/// Used for Subclass and SoftClass properties. Indicates whether abstract Class types should be shown in the Class picker.
/// </summary>
/// <param name="AllowAbstract">true/false</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class AllowAbstractAttribute(string AllowAbstract) : Attribute { }

/// <summary>
/// [AllowedClasses]
/// Used for FSoftObjectPath properties. Comma delimited list that indicates the Class type(s) of assets to be displayed in the Asset picker.
/// </summary>
/// <param name="AllowedClasses">Class1, Class2, ..</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class AllowedClassesAttribute(string AllowedClasses) : Attribute { }

/// <summary>
/// [MustImplement]
/// Limits the classes selectable in blueprint from TSoftClassPtr or TSubclassOf to only those that implement the named
/// interfaces
/// </summary>
/// <param name="RequiredInterface">Interface1, Interface2, ..</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class MustImplementAttribute(string RequiredInterface) : Attribute { }

/// <summary>
/// [AllowPreserveRatio]
/// Used for FVector properties. It causes a ratio lock to be added when displaying this property in details panels.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class AllowPreserveRatioAttribute : Attribute { }

/// <summary>
/// [ArrayClamp]
/// Used for integer properties. Clamps the valid values that can be entered in the UI to be between 0 and the length of the array property named.
/// </summary>
/// <param name="ArrayClamp">ArrayProperty</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class ArrayClampAttribute(string ArrayClamp) : Attribute { }

/// <summary>
/// [AssetBundles]
/// Used for SoftObjectPtr or SoftObjectPath properties. List of Bundle names used inside Primary Data Assets to specify which Bundles this reference is part of.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class AssetBundlesAttribute : Attribute { }

/// <summary>
/// [BlueprintBaseOnly]
/// Used for Subclass and SoftClass properties. Indicates whether only Blueprint Classes should be shown in the Class picker.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class BlueprintBaseOnlyAttribute : Attribute { }

/// <summary>
/// [BlueprintCompilerGeneratedDefaults]
/// Property defaults are generated by the Blueprint compiler and will not be copied when the CopyPropertiesForUnrelatedObjects function is called post-compile.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class BlueprintCompilerGeneratedDefaultsAttribute : Attribute { }

/// <summary>
/// [ClampMin]
/// Used for float and integer properties. Specifies the minimum value N that may be entered for the property.
/// </summary>
/// <param name="ClampMin">N</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class ClampMinAttribute(string ClampMin) : Attribute { }

/// <summary>
/// [ClampMax]
/// Used for float and integer properties. Specifies the maximum value N that may be entered for the property.
/// </summary>
/// <param name="ClampMax">N</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class ClampMaxAttribute(string ClampMax) : Attribute { }

/// <summary>
/// [UIMin]
/// Used for float and integer properties. Specifies the lowest that the value slider should represent.
/// </summary>
/// <param name="ClampMin">N</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class UIMinAttribute(string UIMin) : Attribute { }

/// <summary>
/// [UIMax]
/// Used for float and integer properties. Specifies the highest that the value slider should represent.
/// </summary>
/// <param name="ClampMax">N</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class UIMaxAttribute(string UIMax) : Attribute { }

/// <summary>
/// [ConfigHierarchyEditable]
/// This property is serialized to a config (.ini) file, and can be set anywhere in the config hierarchy.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class ConfigHierarchyEditableAttribute : Attribute { }

/// <summary>
/// [ContentDir]
/// Used by FDirectoryPath properties. Indicates that the path will be picked using the Slate-style directory picker inside the Content folder.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class ContentDirAttribute : Attribute { }

/// <summary>
/// [DisplayAfter]
/// This property will show up in the Blueprint Editor immediately after the property named PropertyName, regardless of its order in source code, as long as both properties are in the same category. If multiple properties have the same DisplayAfter value and the same DisplayPriority value, they will appear after the named property in the order in which they are declared in the header file.
/// </summary>
/// <param name="DisplayAfter">PropertyName</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class DisplayAfterAttribute(string DisplayAfter) : Attribute { }

/// <summary>
/// [DisplayPriority]
/// If two properties feature the same DisplayAfter value, or are in the same category and do not have the DisplayAfter Meta Tag, this property will determine their sorting order. The highest-priority value is 1, meaning that a property with a DisplayPriority value of 1 will appear above a property with a DisplayPriority value of 2. If multiple properties have the same DisplayAfter value, they will appear in the order in which they are declared in the header file.
/// </summary>
/// <param name="DisplayPriority">N</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class DisplayPriorityAttribute(string DisplayPriority) : Attribute { }

/// <summary>
/// [DisplayThumbnail]
/// Indicates that the property is an Asset type and it should display the thumbnail of the selected Asset.
/// </summary>
/// <param name="DisplayThumbnail">true</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class DisplayThumbnailAttribute(string DisplayThumbnail) : Attribute { }

/// <summary>
/// [EditCondition]
/// Names a boolean property that is used to indicate whether editing of this property is disabled. Putting "!" before the property name inverts the test. The EditCondition meta tag is no longer limited to a single boolean property. It is now evaluated using a full-fledged expression parser, meaning you can include a full C++ expression.
/// </summary>
/// <param name="EditCondition">BooleanPropertyName</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class EditConditionAttribute(string EditCondition) : Attribute { }

/// <summary>
/// [EditConditionHides]
/// Paired with EditCondition to hide a property from the view when the condition is false.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class EditConditionHidesAttribute : Attribute { }

/// <summary>
/// [EditFixedOrder]
/// Keeps the elements of an array from being reordered by dragging.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class EditFixedOrderAttribute : Attribute { }

/// <summary>
/// [ExactClass]
/// Used for FSoftObjectPath properties in conjunction with AllowedClasses. Indicates whether only the exact Classes specified in AllowedClasses can be used, or if subclasses are also valid.
/// </summary>
/// <param name="ExactClass">true</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class ExactClassAttribute(string ExactClass) : Attribute { }

/// <summary>
/// [ExposeFunctionCategories]
/// Specifies a list of categories whose functions should be exposed when building a function list in the Blueprint Editor.
/// </summary>
/// <param name="ExposeFunctionCategories">Category1, Category2, ..</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class ExposeFunctionCategoriesAttribute(string ExposeFunctionCategories) : Attribute { }

/// <summary>
/// [ExposeOnSpawn]
/// Specifies whether the property should be exposed on a Spawn Actor node for this Class type.
/// </summary>
/// <param name="ExposeOnSpawn">true</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class ExposeOnSpawnAttribute(string ExposeOnSpawn) : Attribute { }

/// <summary>
/// [FilePathFilter]
/// Used by FFilePath properties. Indicates the path filter to display in the file picker. Common values include "uasset" and "umap", but these are not the only possible values.
/// </summary>
/// <param name="FilePathFilter">FileType</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class FilePathFilterAttribute(string FilePathFilter) : Attribute { }

/// <summary>
/// [GetByRef]
/// Makes the "Get" Blueprint Node for this property return a const reference to the property instead of a copy of its value. Only usable with Sparse Class Data, and only when NoGetter is not present.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class GetByRefAttribute : Attribute { }

/// <summary>
/// [HideAlphaChannel]
/// Used for FColor and FLinearColor properties. Indicates that the Alpha property should be hidden when displaying the property widget in the details.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class HideAlphaChannelAttribute : Attribute { }

/// <summary>
/// [HideViewOptions]
/// Used for Subclass and SoftClass properties. Hides the ability to change view options in the Class picker.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class HideViewOptionsAttribute : Attribute { }

/// <summary>
/// [InlineEditConditionToggle]
/// Signifies that the boolean property is only displayed inline as an edit condition toggle in other properties, and should not be shown on its own row.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class InlineEditConditionToggleAttribute : Attribute { }

/// <summary>
/// [LongPackageName]
/// Used by FDirectoryPath properties. Converts the path to a long package name.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class LongPackageNameAttribute : Attribute { }

/// <summary>
/// [MakeEditWidget]
/// Used for Transform or Rotator properties, or Arrays of Transforms or Rotators. Indicates that the property should be exposed in the viewport as a movable widget.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class MakeEditWidgetAttribute : Attribute { }

/// <summary>
/// [NoGetter]
/// Causes Blueprint generation not to generate a "get" Node for this property. Only usable with Sparse Class Data.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class NoGetterAttribute : Attribute { }

/// <summary>
/// [BindWidget]
/// Used for UWidget properties. Indicates that the property should be bound to a widget in the Blueprint Editor.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class BindWidgetAttribute : Attribute { }

/// <summary>
/// [BindWidgetOptional]
/// Used for UWidget properties. Indicates that the property should be bound to a widget in the Blueprint Editor.
//  Will not cause an error if not found.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class BindWidgetOptionalAttribute : Attribute { }

/// <summary>
/// [BindWidgetAnim]
/// Used for binding widget animations to a property
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class BindWidgetAnimAttribute : Attribute { }

/// <summary>
/// [CategoriesPermalink]
/// This allows you to limit which gameplay tags are allowed to be chosen for a FGameplayTag property. Multiple tags
/// can be specified with commas separating them.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class CategoriesAttribute(string Categories) : Attribute { }

#endregion
