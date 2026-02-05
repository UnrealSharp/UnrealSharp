using System.Collections.ObjectModel;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json.Nodes;

namespace UnrealSharpBuildTool.Actions;

public class BuildEmitLoadOrder : BuildToolAction
{
    public override bool RunAction()
    {
        string output = Program.TryGetArgument("OutputPath");
        string consoleLoggerParameters = Program.TryGetArgument("consoleLoggerParameters");

        Collection<string>? extraArguments = null;
        if (!string.IsNullOrEmpty(output))
        {
            extraArguments =
            [
                $"-p:OutputPath=\"{Program.GetOutputPath()}\""
            ];
        }

        if (!string.IsNullOrEmpty(consoleLoggerParameters))
        {
            extraArguments ??= [];
            extraArguments.Add($"-clp:{consoleLoggerParameters}");
        }

        BuildSolution buildSolution = new BuildSolution(Program.GetScriptFolder(), extraArguments);
        if (!buildSolution.RunAction())
        {
            return false;
        }

        string outputPath = Program.GetOutputPath();
        EmitLoadOrder(outputPath, outputPath);
        return AddLaunchSettings();
    }
    
    public static void EmitLoadOrder(string assemblyFolder, string publishPath)
    {
        DirectoryInfo scriptDirectory = new DirectoryInfo(Program.GetProjectDirectory());
        Dictionary<string, List<FileInfo>> projectFiles = Program.GetProjectFilesByDirectory(scriptDirectory);
        List<FileInfo> allProjectFiles = projectFiles.Values.SelectMany(x => x).ToList();
        
        if (allProjectFiles.Count == 0)
        {
            Console.WriteLine("No project files found to emit load order for.");
            return;
        }

        List<string> assemblyPaths = new List<string>(allProjectFiles.Count);
        foreach (FileInfo projectFile in allProjectFiles)
        {
            string csProjName = Path.GetFileNameWithoutExtension(projectFile.Name);
            string assemblyPath = Path.Combine(assemblyFolder, csProjName + ".dll");
            
            if (!File.Exists(assemblyPath))
            {
                Console.WriteLine($"Could not find assembly for project {csProjName} at expected path {assemblyPath}. Skipping.");
                continue;
            }
            
            assemblyPaths.Add(assemblyPath);
        }
        
        AssemblyLoadOrder.EmitLoadOrder(assemblyPaths, publishPath);
    }
    
    bool AddLaunchSettings()
    {
        List<FileInfo> allProjectFiles = Program.GetAllProjectFiles(new DirectoryInfo(Program.GetProjectDirectory()));

        foreach (FileInfo projectFile in allProjectFiles)
        {
            if (projectFile.Directory!.Name.EndsWith(".Glue"))
            {
                continue;
            }
            
            string csProjectPath = Path.Combine(Program.GetScriptFolder(), projectFile.Directory.Name);
            string propertiesDirectoryPath = Path.Combine(csProjectPath, "Properties");
            string launchSettingsPath = Path.Combine(propertiesDirectoryPath, "launchSettings.json");
            if (!Directory.Exists(propertiesDirectoryPath))
            {
                Directory.CreateDirectory(propertiesDirectoryPath);
            }
            if (File.Exists(launchSettingsPath))
            {
                return true;
            }
            Program.CreateOrUpdateLaunchSettings(launchSettingsPath);
        }
        
        return true;
    }
}

public static class AssemblyLoadOrder
{
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

    public static void EmitLoadOrder(List<string> pathsList, string publishPath)
    {
        List<AssemblyInfo> assemblyInfos = new List<AssemblyInfo>();
        for (int i = 0; i < pathsList.Count; i++)
        {
            AssemblyInfo assemblyInfo = ReadInfo(pathsList[i]);
            assemblyInfos.Add(assemblyInfo);
        }

        Dictionary<string, List<AssemblyInfo>> assembliesByName = new Dictionary<string, List<AssemblyInfo>>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < assemblyInfos.Count; i++)
        {
            AssemblyInfo assemblyInfo = assemblyInfos[i];
            
            if (!assembliesByName.TryGetValue(assemblyInfo.Name, out List<AssemblyInfo>? list))
            {
                list = new List<AssemblyInfo>();
                assembliesByName[assemblyInfo.Name] = list;
            }
            
            list.Add(assemblyInfo);
        }

        Dictionary<string, HashSet<string>> edges = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> inDegree = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < assemblyInfos.Count; i++)
        {
            AssemblyInfo assemblyInfo = assemblyInfos[i];

            if (!edges.ContainsKey(assemblyInfo.Name))
            {
                edges[assemblyInfo.Name] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            inDegree.TryAdd(assemblyInfo.Name, 0);

            for (int r = 0; r < assemblyInfo.References.Count; r++)
            {
                string reference = assemblyInfo.References[r];
                if (!assembliesByName.TryGetValue(reference, out List<AssemblyInfo>? candidates))
                {
                    continue;
                }
                
                string target = candidates[0].Name;
                
                if (target.Equals(assemblyInfo.Name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!edges.ContainsKey(target))
                {
                    edges[target] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                HashSet<string> set = edges[target];

                if (!set.Add(assemblyInfo.Name))
                {
                    continue;
                }

                if (!inDegree.TryAdd(assemblyInfo.Name, 1))
                {
                    inDegree[assemblyInfo.Name] += 1;
                }

                inDegree.TryAdd(target, 0);
            }
        }

        Queue<string> queue = new Queue<string>();
        foreach (KeyValuePair<string, int> entry in inDegree)
        {
            if (entry.Value == 0)
            {
                queue.Enqueue(entry.Key);
            }
        }

        List<string> orderedAssemblies = new List<string>(pathsList.Count);
        while (queue.Count > 0)
        {
            string assembly = queue.Dequeue();
            orderedAssemblies.Add(assembly);

            if (!edges.TryGetValue(assembly, out HashSet<string>? dependencies))
            {
                continue;
            }

            foreach (string dependency in dependencies)
            {
                int newValue = inDegree[dependency] - 1;
                inDegree[dependency] = newValue;

                if (newValue == 0)
                {
                    queue.Enqueue(dependency);
                }
            }
        }

        if (orderedAssemblies.Count != inDegree.Count)
        {
            List<string> cycleNodes = new List<string>();
            foreach (KeyValuePair<string, int> kv in inDegree)
            {
                if (kv.Value > 0)
                {
                    cycleNodes.Add(kv.Key);
                }
            }

            StringBuilder builder = new StringBuilder();
            builder.Append("Cyclic assembly references detected among:\n");
            
            for (int i = 0; i < cycleNodes.Count; i++)
            {
                string fn = Path.GetFileName(cycleNodes[i]);
                builder.Append(" - ");
                builder.Append(fn);
                if (i + 1 < cycleNodes.Count) builder.Append('\n');
            }
            
            throw new InvalidOperationException(builder.ToString());
        }

        JsonObject root = new JsonObject();
        JsonArray array = new JsonArray();
        
        for (int i = 0; i < orderedAssemblies.Count; i++)
        {
            string assemblyName = orderedAssemblies[i];
            array.Add(assemblyName);
        }
        
        root["LoadOrder"] = array;
        
        string jsonString = root.ToJsonString();
        string fullPath = Path.Combine(publishPath, "AssemblyLoadOrder.json");
        File.WriteAllText(fullPath, jsonString);
    }

    private static AssemblyInfo ReadInfo(string path)
    {
        using FileStream assemblyFileStream = File.OpenRead(path);
        using PEReader peReader = new PEReader(assemblyFileStream);
        
        MetadataReader metadataReader = peReader.GetMetadataReader();
        AssemblyDefinition assemblyDefinition = metadataReader.GetAssemblyDefinition();
        string assemblyName = metadataReader.GetString(assemblyDefinition.Name);

        List<string> references = new List<string>();
        foreach (AssemblyReferenceHandle handle in metadataReader.AssemblyReferences)
        {
            AssemblyReference assemblyReference = metadataReader.GetAssemblyReference(handle);
            references.Add(metadataReader.GetString(assemblyReference.Name));
        }
        
        return new AssemblyInfo(assemblyName, references);
    }
}

