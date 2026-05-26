using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json.Nodes;

namespace UnrealSharp.Shared;

public sealed class LoadOrderOptions
{
    public bool Collectible { get; init; } = true;
    public int Priority { get; init; } = 0;
}

public static class AssemblyUtilities
{
    private const string ManifestSuffix = ".LoadOrder.json";
    private const string DefaultManifestName = "Assembly";

    private sealed class AssemblyInfo
    {
        public readonly string Name;
        public readonly List<string> References;

        public AssemblyInfo(string name, List<string> references)
        {
            Name = name;
            References = references;
        }
    }
    
    public static string MakeLoadOrderFileName(string baseName)
    {
        return baseName + ManifestSuffix;
    }

    public static void EmitLoadOrder(List<string> pathsList, string publishPath, LoadOrderOptions loadOrderOptions, string? optionalName = null)
    {
        string OutputFileName = (string.IsNullOrEmpty(optionalName) ? DefaultManifestName : optionalName) + ManifestSuffix;

        List<AssemblyInfo> AssemblyInfos = new List<AssemblyInfo>();
        for (int I = 0; I < pathsList.Count; I++)
        {
            AssemblyInfo Info = ReadInfo(pathsList[I]);
            AssemblyInfos.Add(Info);
        }

        Dictionary<string, List<AssemblyInfo>> AssembliesByName = new Dictionary<string, List<AssemblyInfo>>(StringComparer.OrdinalIgnoreCase);
        for (int I = 0; I < AssemblyInfos.Count; I++)
        {
            AssemblyInfo Info = AssemblyInfos[I];

            if (!AssembliesByName.TryGetValue(Info.Name, out List<AssemblyInfo>? List))
            {
                List = new List<AssemblyInfo>();
                AssembliesByName[Info.Name] = List;
            }

            List.Add(Info);
        }

        Dictionary<string, HashSet<string>> Edges = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> InDegree = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int I = 0; I < AssemblyInfos.Count; I++)
        {
            AssemblyInfo Info = AssemblyInfos[I];

            if (!Edges.ContainsKey(Info.Name))
            {
                Edges[Info.Name] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            InDegree.TryAdd(Info.Name, 0);

            for (int R = 0; R < Info.References.Count; R++)
            {
                string Reference = Info.References[R];
                if (!AssembliesByName.TryGetValue(Reference, out List<AssemblyInfo>? Candidates))
                {
                    continue;
                }

                string Target = Candidates[0].Name;

                if (Target.Equals(Info.Name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!Edges.ContainsKey(Target))
                {
                    Edges[Target] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                HashSet<string> Set = Edges[Target];

                if (!Set.Add(Info.Name))
                {
                    continue;
                }

                if (!InDegree.TryAdd(Info.Name, 1))
                {
                    InDegree[Info.Name] += 1;
                }

                InDegree.TryAdd(Target, 0);
            }
        }

        Queue<string> Queue = new Queue<string>();
        foreach (KeyValuePair<string, int> Entry in InDegree)
        {
            if (Entry.Value == 0)
            {
                Queue.Enqueue(Entry.Key);
            }
        }

        List<string> OrderedAssemblies = new List<string>(pathsList.Count);
        while (Queue.Count > 0)
        {
            string Assembly = Queue.Dequeue();
            OrderedAssemblies.Add(Assembly);

            if (!Edges.TryGetValue(Assembly, out HashSet<string>? Dependencies))
            {
                continue;
            }

            foreach (string Dependency in Dependencies)
            {
                int NewValue = InDegree[Dependency] - 1;
                InDegree[Dependency] = NewValue;

                if (NewValue == 0)
                {
                    Queue.Enqueue(Dependency);
                }
            }
        }

        if (OrderedAssemblies.Count != InDegree.Count)
        {
            List<string> CycleNodes = new List<string>();
            foreach (KeyValuePair<string, int> Kv in InDegree)
            {
                if (Kv.Value > 0)
                {
                    CycleNodes.Add(Kv.Key);
                }
            }

            StringBuilder Builder = new StringBuilder();
            Builder.Append("Cyclic assembly references detected among:\n");

            for (int I = 0; I < CycleNodes.Count; I++)
            {
                string Fn = Path.GetFileName(CycleNodes[I]);
                Builder.Append(" - ");
                Builder.Append(Fn);
                if (I + 1 < CycleNodes.Count) Builder.Append('\n');
            }

            throw new InvalidOperationException(Builder.ToString());
        }

        JsonObject Root = new JsonObject();
        Root["Priority"] = loadOrderOptions.Priority;
        Root["Collectible"] = loadOrderOptions.Collectible;

        JsonArray Array = new JsonArray();
        for (int I = 0; I < OrderedAssemblies.Count; I++)
        {
            Array.Add(OrderedAssemblies[I]);
        }
        Root["LoadOrder"] = Array;

        string JsonString = Root.ToJsonString();
        string FullPath = Path.Combine(publishPath, OutputFileName);
        File.WriteAllText(FullPath, JsonString);
    }

    private static AssemblyInfo ReadInfo(string filePath)
    {
        using FileStream AssemblyFileStream = File.OpenRead(filePath);
        using PEReader PeReader = new PEReader(AssemblyFileStream);

        MetadataReader MetadataReader = PeReader.GetMetadataReader();
        AssemblyDefinition AssemblyDefinition = MetadataReader.GetAssemblyDefinition();
        string AssemblyName = MetadataReader.GetString(AssemblyDefinition.Name);

        List<string> References = new List<string>();
        foreach (AssemblyReferenceHandle Handle in MetadataReader.AssemblyReferences)
        {
            AssemblyReference AssemblyReference = MetadataReader.GetAssemblyReference(Handle);
            References.Add(MetadataReader.GetString(AssemblyReference.Name));
        }

        return new AssemblyInfo(AssemblyName, References);
    }
    
    public static List<string> ReadLoadOrder(string manifestPath)
    {
        List<string> Names = new();

        JsonNode? Root = JsonNode.Parse(File.ReadAllText(manifestPath));
        
        if (Root is not JsonObject Obj || Obj["LoadOrder"] is not JsonArray Array)
        {
            return Names;
        }
        
        foreach (JsonNode? Item in Array)
        {
            string? Name = Item?.GetValue<string?>();
            
            if (string.IsNullOrEmpty(Name))
            {
                continue;
            }
            
            Names.Add(Name);
        }

        return Names;
    }
    
    public static List<string> GetLoadOrdersInFolder(string folderPath)
    {
        List<string> LoadOrders = new();

        if (!Directory.Exists(folderPath))
        {
            return LoadOrders;
        }

        IEnumerable<string> ManifestFiles = Directory.EnumerateFiles(folderPath, "*.LoadOrder.json", SearchOption.AllDirectories);

        foreach (string ManifestFile in ManifestFiles)
        {
            LoadOrders.AddRange(ReadLoadOrder(ManifestFile));
        }

        return LoadOrders;
    }
}