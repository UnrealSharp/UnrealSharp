using System.Text.Json;
using Mono.Cecil;
using Mono.Cecil.Pdb;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.TypeProcessors;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver;

public record struct AssemblyInfo(AssemblyDefinition Assembly, string OutputPath);

public static class Program
{
    public static WeaverOptions WeaverOptions { get; private set; } = null!;

    public static void Weave(WeaverOptions weaverOptions)
    {
        try
        {
            WeaverOptions = weaverOptions;
            LoadBindingsAssembly();
            ProcessUserAssemblies();
        }
        finally
        {
            WeaverImporter.Shutdown();
        }
    }

    public static int Main(string[] args)
    {
        try
        {
            WeaverOptions = WeaverOptions.ParseArguments(args);
            LoadBindingsAssembly();
            ProcessUserAssemblies();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void LoadBindingsAssembly()
    {
        DefaultAssemblyResolver resolver = new DefaultAssemblyResolver();

        List<string> searchPaths = new();
        foreach (var (assemblyPath, _) in WeaverOptions.AssemblyPaths.Select(StripQuotes).Select(GetAssemblyPaths))
        {
            string? directory = Path.GetDirectoryName(assemblyPath);

            if (string.IsNullOrEmpty(directory) || searchPaths.Contains(directory))
            {
                continue;
            }

            if (!Directory.Exists(directory))
            {
                throw new InvalidOperationException("Could not determine directory for assembly path.");
            }

            resolver.AddSearchDirectory(directory);
            searchPaths.Add(directory);
        }

        WeaverImporter.Instance.AssemblyResolver = resolver;
    }

    private static void ProcessUserAssemblies()
    {
        DirectoryInfo outputDirInfo = new DirectoryInfo(StripQuotes(WeaverOptions.OutputDirectory));

        if (!outputDirInfo.Exists)
        {
            outputDirInfo.Create();
        }

        DefaultAssemblyResolver resolver = GetAssemblyResolver();
        List<AssemblyInfo> userAssemblies = LoadUserAssemblies(resolver);
        ICollection<AssemblyInfo> orderedUserAssemblies = OrderUserAssembliesByReferences(userAssemblies);

        WriteUnrealSharpMetadataFile(orderedUserAssemblies, outputDirInfo);
        ProcessOrderedUserAssemblies(orderedUserAssemblies);
    }

    private static void WriteUnrealSharpMetadataFile(ICollection<AssemblyInfo> orderedAssemblies, DirectoryInfo outputDirectory)
    {
        UnrealSharpMetadata unrealSharpMetadata = new UnrealSharpMetadata
        {
            AssemblyLoadingOrder = orderedAssemblies
                .Select(x => x.Assembly)
                .Select(x => Path.GetFileNameWithoutExtension(x.MainModule.FileName)).ToList(),
        };

        string metaDataContent = JsonSerializer.Serialize(unrealSharpMetadata, new JsonSerializerOptions
        {
            WriteIndented = false,
        });

        string fileName = Path.Combine(outputDirectory.FullName, "UnrealSharp.assemblyloadorder.json");
        File.WriteAllText(fileName, metaDataContent);
    }

    private static void ProcessOrderedUserAssemblies(ICollection<AssemblyInfo> assemblies)
    {
        Exception? exception = null;

        foreach (var (assembly, output) in assemblies)
        {
            if (assembly.Name.FullName == WeaverImporter.Instance.ProjectGlueAssembly.FullName)
            {
                continue;
            }

            try
            {
                string outputPath = Path.Combine(output, Path.GetFileName(assembly.MainModule.FileName));
                StartWeavingAssembly(assembly, outputPath);
                WeaverImporter.Instance.WeavedAssemblies.Add(assembly);
            }
            catch (Exception ex)
            {
                exception = ex;
                break;
            }
        }

        foreach (var (assembly, _) in assemblies)
        {
            assembly.Dispose();
        }

        if (exception != null)
        {
            throw new AggregateException("Assembly processing failed", exception);
        }
    }

    private static ICollection<AssemblyInfo> OrderUserAssembliesByReferences(ICollection<AssemblyInfo> assemblies)
    {
        HashSet<string> assemblyNames = new HashSet<string>();

        foreach (var (assembly, _) in assemblies)
        {
            assemblyNames.Add(assembly.FullName);
        }

        List<AssemblyInfo> result = new List<AssemblyInfo>(assemblies.Count);
        HashSet<AssemblyInfo> remaining = new HashSet<AssemblyInfo>(assemblies);

        // Add assemblies with no references first between the user assemblies.
        foreach (var (assembly, output) in assemblies)
        {
            bool hasReferenceToUserAssembly = false;
            foreach (AssemblyNameReference? reference in assembly.MainModule.AssemblyReferences)
            {
                if (!assemblyNames.Contains(reference.FullName))
                {
                    continue;
                }

                hasReferenceToUserAssembly = true;
                break;
            }

            if (hasReferenceToUserAssembly)
            {
                continue;
            }

            result.Add(new AssemblyInfo(assembly, output));
            remaining.Remove(new AssemblyInfo(assembly, output));
        }

        do
        {
            bool added = false;

            foreach (var (assembly, output) in assemblies)
            {
                if (!remaining.Contains(new AssemblyInfo(assembly, output)))
                {
                    continue;
                }

                bool allResolved = true;
                foreach (AssemblyNameReference? reference in assembly.MainModule.AssemblyReferences)
                {
                    if (assemblyNames.Contains(reference.FullName))
                    {
                        bool found = false;
                        foreach (var (addedAssembly, _) in result)
                        {
                            if (addedAssembly.FullName != reference.FullName)
                            {
                                continue;
                            }

                            found = true;
                            break;
                        }

                        if (found)
                        {
                            continue;
                        }

                        allResolved = false;
                        break;
                    }
                }

                if (!allResolved)
                {
                    continue;
                }

                result.Add(new AssemblyInfo(assembly, output));
                remaining.Remove(new AssemblyInfo(assembly, output));
                added = true;
            }

            if (added || remaining.Count <= 0)
            {
                continue;
            }

            foreach (var (asm, output) in remaining)
            {
                result.Add(new AssemblyInfo(asm, output));
            }

            break;

        } while (remaining.Count > 0);

        return result;
    }

    private static DefaultAssemblyResolver GetAssemblyResolver()
    {
        return WeaverImporter.Instance.AssemblyResolver;
    }

    private static List<AssemblyInfo> LoadUserAssemblies(IAssemblyResolver resolver)
    {
        ReaderParameters readerParams = new ReaderParameters
        {
            AssemblyResolver = resolver,
            ReadSymbols = true,
            SymbolReaderProvider = new PdbReaderProvider(),
        };

        var result = new List<AssemblyInfo>();

        foreach (var (assemblyPath, outputPath) in WeaverOptions.AssemblyPaths.Select(StripQuotes).Select(GetAssemblyPaths))
        {
            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException($"Could not find assembly at: {assemblyPath}");
            }

            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(assemblyPath, readerParams);
            result.Add(new AssemblyInfo(assembly, outputPath));
        }

        return result;
    }

    private static string StripQuotes(string value)
    {
        if (value.StartsWith('\"') && value.EndsWith('\"'))
        {
            return value.Substring(1, value.Length - 2);
        }

        return value;
    }

    private static (string AssemblyPath, string OutputPath) GetAssemblyPaths(string assemblyPath)
    {
        var components = assemblyPath.Split(";", StringSplitOptions.RemoveEmptyEntries);
        if (components.Length != 2)
        {
            throw new InvalidOperationException("Invalid assembly path. Expected format: AssemblyPath;OutputPath");
        }

        return (components[0], components[1]);
    }

    static void StartWeavingAssembly(AssemblyDefinition assembly, string assemblyOutputPath)
    {
        void CleanOldFilesAndMoveExistingFiles()
        {
            var pdbOutputFile = new FileInfo(Path.ChangeExtension(assemblyOutputPath, ".pdb"));

            if (!pdbOutputFile.Exists)
            {
                return;
            }

            var tmpDirectory = Path.Join(Path.GetTempPath(), assembly.Name.Name);
            if (Path.GetPathRoot(tmpDirectory) != Path.GetPathRoot(pdbOutputFile.FullName)) //if the temp directory is on a different drive, move will not work as desired if file is locked since it does a copy for drive boundaries
            {
                tmpDirectory = Path.Join(Path.GetDirectoryName(assemblyOutputPath), "..", "_Temporary", assembly.Name.Name);
            }

            try
            {
                if (Directory.Exists(tmpDirectory))
                {
                    foreach (var file in Directory.GetFiles(tmpDirectory))
                    {
                        File.Delete(file);
                    }
                }
                else
                {
                    Directory.CreateDirectory(tmpDirectory);
                }
            }
            catch
            {
                //no action needed
            }

            //move the file to an temp folder to prevent write locks in case a debugger is attached to UE which locks the pdb for writes (common strategy).
            var tmpDestFileName = Path.Join(tmpDirectory, Path.GetFileName(Path.ChangeExtension(Path.GetTempFileName(), ".pdb")));
            File.Move(pdbOutputFile.FullName, tmpDestFileName);
        }

        Task cleanupTask = Task.Run(CleanOldFilesAndMoveExistingFiles);
        WeaverImporter.Instance.ImportCommonTypes(assembly);

        ApiMetaData assemblyMetaData = new ApiMetaData(assembly.Name.Name);
        StartProcessingAssembly(assembly, assemblyMetaData);

        string sourcePath = Path.GetDirectoryName(assembly.MainModule.FileName)!;
        CopyAssemblyDependencies(assemblyOutputPath, sourcePath);

        Task.WaitAll(cleanupTask);
        assembly.Write(assemblyOutputPath, new WriterParameters
        {
            SymbolWriterProvider = new PdbWriterProvider(),
        });

        WriteAssemblyMetaDataFile(assemblyMetaData, assemblyOutputPath);
    }

    private static void WriteAssemblyMetaDataFile(ApiMetaData metadata, string outputPath)
    {
        string metaDataContent = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
        {
            WriteIndented = false,
        });

        string metadataFilePath = Path.ChangeExtension(outputPath, "metadata.json");
        File.WriteAllText(metadataFilePath, metaDataContent);
    }

    private static void StartProcessingAssembly(AssemblyDefinition userAssembly, ApiMetaData metadata)
    {
        try
        {
            List<TypeDefinition> classes = [];
            List<TypeDefinition> structs = [];
            List<TypeDefinition> enums = [];
            List<TypeDefinition> interfaces = [];
            List<TypeDefinition> multicastDelegates = [];
            List<TypeDefinition> delegates = [];

            try
            {
                void RegisterType(List<TypeDefinition> typeDefinitions, TypeDefinition typeDefinition)
                {
                    typeDefinitions.Add(typeDefinition);
                    typeDefinition.AddGeneratedTypeAttribute();
                }

                foreach (ModuleDefinition? module in userAssembly.Modules)
                {
                    foreach (TypeDefinition? type in module.Types)
                    {
                        if (type.IsUClass())
                        {
                            RegisterType(classes, type);
                        }
                        else if (type.IsUEnum())
                        {
                            RegisterType(enums, type);
                        }
                        else if (type.IsUStruct())
                        {
                            RegisterType(structs, type);
                        }
                        else if (type.IsUInterface())
                        {
                            RegisterType(interfaces, type);
                        }
                        else if (type.BaseType != null && type.BaseType.FullName.Contains("UnrealSharp.MulticastDelegate"))
                        {
                            RegisterType(multicastDelegates, type);
                        }
                        else if (type.BaseType != null && type.BaseType.FullName.Contains("UnrealSharp.Delegate"))
                        {
                            RegisterType(delegates, type);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error enumerating types: {ex.Message}");
                throw;
            }

            UnrealEnumProcessor.ProcessEnums(enums, metadata);
            UnrealInterfaceProcessor.ProcessInterfaces(interfaces, metadata);
            UnrealStructProcessor.ProcessStructs(structs, metadata, userAssembly);
            UnrealClassProcessor.ProcessClasses(classes, metadata);
            UnrealDelegateProcessor.ProcessDelegates(delegates, multicastDelegates, userAssembly, metadata.DelegateMetaData);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during assembly processing: {ex.Message}");
            throw;
        }
    }

    private static void RecursiveFileCopy(DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory)
    {
        // Early out of our search if the last updated timestamps match
        if (sourceDirectory.LastWriteTimeUtc == destinationDirectory.LastWriteTimeUtc) return;

        if (!destinationDirectory.Exists)
        {
            destinationDirectory.Create();
        }

        foreach (FileInfo sourceFile in sourceDirectory.GetFiles())
        {
            string destinationFilePath = Path.Combine(destinationDirectory.FullName, sourceFile.Name);
            FileInfo destinationFile = new FileInfo(destinationFilePath);

            if (!destinationFile.Exists || sourceFile.LastWriteTimeUtc > destinationFile.LastWriteTimeUtc)
            {
                sourceFile.CopyTo(destinationFilePath, true);
            }
        }

        // Update our write time to match source for faster copying
        destinationDirectory.LastWriteTimeUtc = sourceDirectory.LastWriteTimeUtc;

        foreach (DirectoryInfo subSourceDirectory in sourceDirectory.GetDirectories())
        {
            string subDestinationDirectoryPath = Path.Combine(destinationDirectory.FullName, subSourceDirectory.Name);
            DirectoryInfo subDestinationDirectory = new DirectoryInfo(subDestinationDirectoryPath);

            RecursiveFileCopy(subSourceDirectory, subDestinationDirectory);
        }
    }

    private static void CopyAssemblyDependencies(string destinationPath, string sourcePath)
    {
        var directoryName = Path.GetDirectoryName(destinationPath) ?? throw new InvalidOperationException("Assembly path does not have a valid directory.");

        try
        {
            var destinationDirectory = new DirectoryInfo(directoryName);
            var sourceDirectory = new DirectoryInfo(sourcePath);

            RecursiveFileCopy(sourceDirectory, destinationDirectory);
        }
        catch (Exception ex)
        {
            ErrorEmitter.Error("WeaverError", sourcePath, 0, "Failed to copy dependencies: " + ex.Message);
        }
    }
}
