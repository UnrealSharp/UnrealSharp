using System.Text.Json;
using Mono.Cecil;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.TypeProcessors;

namespace UnrealSharpWeaver;

public static class Program
{
    public static WeaverOptions WeaverOptions { get; private set; } = null!;

    public static int Main(string[] args)
    {
        WeaverOptions = WeaverOptions.ParseArguments(args);

        if (!LoadBindingsAssembly())
        {
            return 1;
        }
        
        if (!ProcessUserAssemblies())
        {
            return 2;
        }

        return 0;
    }

    private static bool LoadBindingsAssembly()
    {
        DefaultAssemblyResolver resolver = new DefaultAssemblyResolver();
        
        foreach (var assemblyPath in WeaverOptions.AssemblyPaths)
        {
            string? directory = Path.GetDirectoryName(StripQuotes(assemblyPath));
            
            if (!Directory.Exists(directory))
            {
                throw new InvalidOperationException("Could not determine directory for assembly path.");
            }
            
            resolver.AddSearchDirectory(directory);
        }

        try
        {
            var unrealSharpLibraryAssembly = resolver.Resolve(new AssemblyNameReference(WeaverHelper.UnrealSharpNamespace, new Version(0, 0, 0, 0)));
            WeaverHelper.Initialize(unrealSharpLibraryAssembly);
            return true;
        }
        catch
        {
            Console.Error.WriteLine("Could not resolve the UnrealSharp library assembly.");
        }
        
        return false;
    }

    private static bool ProcessUserAssemblies()
    {
        DirectoryInfo outputDirInfo = new DirectoryInfo(StripQuotes(WeaverOptions.OutputDirectory));
        
        if (!outputDirInfo.Exists)
        {
            outputDirInfo.Create();
        }

        foreach (var quotedAssemblyPath in WeaverOptions.AssemblyPaths)
        {
            string userAssemblyPath = StripQuotes(quotedAssemblyPath);
            
            if (!File.Exists(userAssemblyPath))
            {
                throw new FileNotFoundException($"Could not find assembly at: {userAssemblyPath}");
            }
            
            DefaultAssemblyResolver resolver = new DefaultAssemblyResolver();

            foreach (string assemblyPath in WeaverOptions.AssemblyPaths)
            {
                string assemblyDirectory = Path.GetDirectoryName(assemblyPath)!;
                resolver.AddSearchDirectory(assemblyDirectory);
            }

            ReaderParameters readerParams = new ReaderParameters
            {
                AssemblyResolver = resolver
            };

            AssemblyDefinition userAssembly = AssemblyDefinition.ReadAssembly(userAssemblyPath, readerParams);

            try
            {
                string weaverOutputPath = Path.Combine(outputDirInfo.FullName, Path.GetFileName(userAssemblyPath));
                StartWeavingAssembly(userAssembly, weaverOutputPath);
                continue;
            }
            catch (WeaverProcessError error)
            {
                ErrorEmitter.Error(error.GetType().Name, error.File, error.Line, "Caught fatal error: " + error.Message);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception processing {userAssemblyPath}: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
            }
            return false;
        }
        
        return true;
    }
    
    private static string StripQuotes(string value)
    {
        if (value.StartsWith("\"") && value.EndsWith("\""))
        {
            return value.Substring(1, value.Length - 2);
        }

        return value;
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

        var cleanupTask = Task.Run(CleanOldFilesAndMoveExistingFiles);
        var assemblyMetaData = new ApiMetaData
                               {
                                   AssemblyName = assembly.Name.Name,
                               };
        
        WeaverHelper.ImportCommonTypes(assembly);
        StartProcessingAssembly(assembly, ref assemblyMetaData);
        
        string sourcePath = Path.GetDirectoryName(assembly.MainModule.FileName)!;
        CopyAssemblyDependencies(assemblyOutputPath, sourcePath);

        try
        {
            Task.WaitAll(cleanupTask);
            assembly.Write(assemblyOutputPath);
        }
        catch (Exception ex)
        {
            ErrorEmitter.Error("WeaverError", assembly.MainModule.FileName, 0, "Failed to write assembly: " + ex.Message);
            throw;
        }
        
        WriteAssemblyMetaDataFile(assemblyMetaData, assemblyOutputPath);
    }
    
    private static void WriteAssemblyMetaDataFile(ApiMetaData metadata, string outputPath)
    {
        string metaDataContent = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        string metadataFilePath = Path.ChangeExtension(outputPath, "metadata.json");
        File.WriteAllText(metadataFilePath, metaDataContent);
    }

    static void StartProcessingAssembly(AssemblyDefinition userAssembly, ref ApiMetaData metadata)
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
                    WeaverHelper.AddGeneratedTypeAttribute(typeDefinition);
                }
                
                foreach (var module in userAssembly.Modules)
                {
                    foreach (var type in module.Types)
                    {
                        if (WeaverHelper.IsUClass(type))
                        {
                            RegisterType(classes, type);
                        }
                        else if (WeaverHelper.IsUEnum(type))
                        {
                            RegisterType(enums, type);
                        }
                        else if (WeaverHelper.IsUStruct(type))
                        {
                            RegisterType(structs, type);
                        }
                        else if (WeaverHelper.IsUInterface(type))
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
            UnrealDelegateProcessor.ProcessMulticastDelegates(multicastDelegates);
            UnrealDelegateProcessor.ProcessSingleDelegates(delegates, userAssembly);
            UnrealClassProcessor.ProcessClasses(classes, metadata);
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