using System.Reflection;
using System.Runtime.Loader;

namespace UnrealSharp.Plugins
{
    public class PluginLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver Resolver;
        private readonly ICollection<string> SharedAssemblies;
        private readonly AssemblyLoadContext MainLoadContext;

        public string? AssemblyLoadedPath { get; private set; }

        public PluginLoadContext(string pluginPath, ICollection<string> sharedAssemblies, AssemblyLoadContext mainLoadContext, bool isCollectible) : base(isCollectible)
        {
            Resolver = new AssemblyDependencyResolver(pluginPath);
            SharedAssemblies = sharedAssemblies;
            MainLoadContext = mainLoadContext;

            if (string.IsNullOrEmpty(AppContext.BaseDirectory))
            {
                string? baseDirectory = Path.GetDirectoryName(pluginPath);
                if (baseDirectory != null)
                {
                    if (!Path.EndsInDirectorySeparator(baseDirectory))
                    {
                        baseDirectory += Path.DirectorySeparatorChar;
                    }

                    AppDomain.CurrentDomain.SetData("APP_CONTEXT_BASE_DIRECTORY", baseDirectory);
                }
                else
                {
                    Console.Error.WriteLine("Failed to set AppContext.BaseDirectory. Dynamic loading of libraries may fail.");
                }
            }
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (assemblyName.Name == null)
            {
                return null; 
            }
            
            if (SharedAssemblies.Contains(assemblyName.Name))
            {
                return MainLoadContext.LoadFromAssemblyName(assemblyName);
            }

            string assemblyPath = Resolver.ResolveAssemblyToPath(assemblyName);

            if (assemblyPath == null)
            {
                return default;
            }

            AssemblyLoadedPath = assemblyPath;
            
            // Load in memory to prevent locking the file
            using FileStream assemblyFile = File.Open(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            string pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");

            if (File.Exists(pdbPath))
            {
                using var pdbFile = File.Open(pdbPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return LoadFromStream(assemblyFile, pdbFile);
            }
            
            return LoadFromStream(assemblyFile);
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string? libraryPath = Resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }
            
            return IntPtr.Zero;
        }
    }
}
