using System.IO;
using System.Text.Json;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue;

public static class JsonUtilities
{
    public static void SerializeObjectToJson(object objectToSerialize, string fileName)
    {
        string outputPath = GetJsonOutputPath(fileName);
        
        using FileStream filestream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        JsonSerializer.Serialize(filestream, objectToSerialize);
        filestream.Close();
    }
    
    public static T? DeserializeObjectFromJson<T>(string fileName)
    {
        string inputPath = GetJsonOutputPath(fileName);
        
        if (!File.Exists(inputPath))
        {
            return default;
        }

        using FileStream filestream = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
        
        T? deserializedObject = JsonSerializer.Deserialize<T>(filestream);
        filestream.Close();
        
        return deserializedObject;
    }
    
    static string GetJsonOutputPath(string fileName)
    {
        return Path.Combine(GeneratorStatics.PluginModule.OutputDirectory, fileName + ".json");
    }
}