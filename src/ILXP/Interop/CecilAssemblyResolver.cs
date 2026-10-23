using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;

namespace ILXP.Interop
{
    /// <summary>
    /// Assembly resolver used while assembling IL text. The metadata writer needs to know
    /// whether a referenced type is a value type (enums and structs are encoded differently
    /// in signatures), and that requires resolving the reference. The default Cecil resolver
    /// only probes the application directory, so this one also probes the CLR directory,
    /// the GAC through <see cref="AssemblyResolver"/> and the directories of loaded
    /// assemblies.
    /// </summary>
    internal sealed class CecilAssemblyResolver : IAssemblyResolver
    {
        private readonly Dictionary<string, AssemblyDefinition> cache =
            new Dictionary<string, AssemblyDefinition>(StringComparer.OrdinalIgnoreCase);

        private readonly List<string> directories = new List<string>();

        public CecilAssemblyResolver()
        {
            AddDirectory(Path.GetDirectoryName(typeof(object).Assembly.Location));
            AddDirectory(AppDomain.CurrentDomain.BaseDirectory);
            AddDirectory(Path.GetDirectoryName(typeof(CecilAssemblyResolver).Assembly.Location));
        }

        public void AddDirectory(string directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            if (!directories.Contains(directory))
            {
                directories.Add(directory);
            }
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            return Resolve(name, null);
        }

        public AssemblyDefinition Resolve(string fullName)
        {
            return Resolve(fullName, null);
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            return Resolve(name.FullName, parameters);
        }

        public AssemblyDefinition Resolve(string fullName, ReaderParameters parameters)
        {
            if (string.IsNullOrEmpty(fullName))
            {
                throw new ArgumentNullException("fullName");
            }

            AssemblyDefinition cached;
            if (cache.TryGetValue(fullName, out cached))
            {
                return cached;
            }

            string path = Locate(fullName);
            if (path == null)
            {
                throw new AssemblyResolutionException(new AssemblyNameReference(fullName, new Version(0, 0, 0, 0)));
            }

            ReaderParameters readerParameters = parameters ?? new ReaderParameters();
            readerParameters.AssemblyResolver = this;
            readerParameters.ReadSymbols = false;

            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(path, readerParameters);
            cache[fullName] = assembly;
            return assembly;
        }

        /// <summary>
        /// Returns true when the referenced type is a value type (enum or struct). The
        /// metadata writer must know this to pick the right signature encoding.
        /// </summary>
        public bool TryIsValueType(TypeReference reference)
        {
            if (reference == null || reference.IsValueType)
            {
                return reference != null && reference.IsValueType;
            }

            AssemblyNameReference scope = reference.Scope as AssemblyNameReference;
            if (scope == null)
            {
                return false;
            }

            try
            {
                AssemblyDefinition assembly = Resolve(scope.FullName, null);
                TypeDefinition definition = FindType(assembly.MainModule, reference.FullName);
                return definition != null && definition.IsValueType;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Resolves a type reference to its definition, or null when unavailable.</summary>
        public TypeDefinition TryResolveType(TypeReference reference)
        {
            if (reference == null)
            {
                return null;
            }

            // Generic instances and arrays resolve through their element type.
            TypeReference element = reference;
            while (element is TypeSpecification)
            {
                element = ((TypeSpecification)element).ElementType;
            }

            if (element is TypeDefinition)
            {
                return (TypeDefinition)element;
            }

            AssemblyNameReference scope = element.Scope as AssemblyNameReference;
            if (scope == null)
            {
                // A type of the module being built (or of any module already loaded).
                ModuleDefinition module = element.Module;
                if (module != null)
                {
                    return FindType(module, element.FullName);
                }
                return null;
            }

            try
            {
                AssemblyDefinition assembly = Resolve(scope.FullName, null);
                return FindType(assembly.MainModule, element.FullName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static TypeDefinition FindType(ModuleDefinition module, string fullName)
        {
            foreach (TypeDefinition type in module.Types)
            {
                TypeDefinition found = FindType(type, fullName);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private static TypeDefinition FindType(TypeDefinition type, string fullName)
        {
            if (string.Equals(type.FullName, fullName, StringComparison.Ordinal))
            {
                return type;
            }

            foreach (TypeDefinition nested in type.NestedTypes)
            {
                TypeDefinition found = FindType(nested, fullName);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private string Locate(string fullName)        {
            string simpleName = fullName;
            int comma = simpleName.IndexOf(',');
            if (comma > 0)
            {
                simpleName = simpleName.Substring(0, comma);
            }
            simpleName = simpleName.Trim();

            try
            {
                string resolved = AssemblyResolver.Resolve(fullName, directories);
                if (!string.IsNullOrEmpty(resolved) && File.Exists(resolved))
                {
                    return resolved;
                }
            }
            catch (Exception)
            {
                // Fall through to the simple-name probing below.
            }

            foreach (string directory in directories)
            {
                foreach (string extension in new[] { ".dll", ".exe" })
                {
                    string candidate = Path.Combine(directory, simpleName + extension);
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }
            return null;
        }
    }
}
