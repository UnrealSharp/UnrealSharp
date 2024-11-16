using System.Reflection;
using System.Runtime.Loader;

namespace UnrealSharp.Plugins
{
    internal class PluginLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;
        private readonly ICollection<string> _sharedAssemblies;

        public string? AssemblyLoadedPath { get; private set; }

        public PluginLoadContext(string pluginPath, ICollection<string> sharedAssemblies, bool isCollectible) : base(isCollectible)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
            _sharedAssemblies = sharedAssemblies;
            AssemblyLoadedPath = pluginPath;

            if (!string.IsNullOrEmpty(AppContext.BaseDirectory))
            {
                return;
            }
            
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

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (assemblyName.Name == null)
            {
                return null; 
            }
            
            if (_sharedAssemblies.Contains(assemblyName.Name))
            {
                return PluginsInfo.MainLoadContext.LoadFromAssemblyName(assemblyName);
            }
            
            //check if assembly already loaded in another plugin
            var loadedPlugin = PluginsInfo.LoadedPlugins.FirstOrDefault(x => x.AssemblyFullName == assemblyName.FullName);
            if (loadedPlugin is not null)
            {
                return loadedPlugin.Assembly.TryGetTarget(out var pluginAssembly) ? pluginAssembly : null;
            }

            string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);

            if (assemblyPath == null)
            {
                return default;
            }
            
            using FileStream assemblyFile = File.Open(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            string pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");

            if (!File.Exists(pdbPath))
            {
                return LoadFromStream(assemblyFile);
            }
            
            using var pdbFile = File.Open(pdbPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return LoadFromStream(assemblyFile, pdbFile);
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            
            return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : IntPtr.Zero;
        }
    }
}
