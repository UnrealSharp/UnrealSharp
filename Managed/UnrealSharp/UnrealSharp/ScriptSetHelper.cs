namespace UnrealSharp;

internal unsafe class ScriptSetHelper
{
    IntPtr* ElementProp;
    ScriptSet* Set;
    FScriptSetLayout SetLayout;
    
    internal ScriptSetHelper(IntPtr* elementProp, IntPtr address)
    {
        ElementProp = elementProp;
        Set = (ScriptSet*) address;
        SetLayout = new FScriptSetLayout();
    }
}