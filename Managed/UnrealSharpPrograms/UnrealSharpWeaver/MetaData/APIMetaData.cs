namespace UnrealSharpWeaver.MetaData;

public class ApiMetaData
{
    public List<ClassMetaData> ClassMetaData { get; set; }
    public StructMetaData[] StructMetaData { get; set; }
    public EnumMetaData[] EnumMetaData { get; set; }
    public InterfaceMetaData[] InterfacesMetaData { get; set; }
    
    public string AssemblyName { get; set; }
}