namespace UnrealSharpWeaver.MetaData;

public class ApiMetaData
{
    public ClassMetaData[] ClassMetaData { get; set; }
    public StructMetaData[] StructMetaData { get; set; }
    public EnumMetaData[] EnumMetaData { get; set; }
    public InterfaceMetaData[] InterfacesMetaData { get; set; }
    
    public string AssemblyName { get; set; }
}