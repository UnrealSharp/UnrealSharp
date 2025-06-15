namespace UnrealSharpWeaver.MetaData;

public class ApiMetaData
{
    public ApiMetaData(string assemblyName)
    {
        AssemblyName = assemblyName;
        ClassMetaData = new List<ClassMetaData>();
        StructMetaData = new List<StructMetaData>();
        EnumMetaData = new List<EnumMetaData>();
        InterfacesMetaData = new List<InterfaceMetaData>();
        DelegateMetaData = new List<DelegateMetaData>();
    }

    public List<ClassMetaData> ClassMetaData { get; set; }  
    public List<StructMetaData> StructMetaData { get; set; }
    public List<EnumMetaData> EnumMetaData { get; set; }
    public List<InterfaceMetaData> InterfacesMetaData { get; set; }
    public List<DelegateMetaData> DelegateMetaData { get; set; }
    
    public string AssemblyName { get; set; }
}