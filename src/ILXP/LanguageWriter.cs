using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Mono.Cecil;

namespace ILXP
{
    /// <summary>
    /// Base class for the three output languages supported by the application.
    /// Each writer turns Cecil metadata into text annotated with lightweight markup
    /// (<c>&lt;&lt;b&gt;&gt;</c>, <c>&lt;&lt;r&gt;&gt;</c>, ...) that the editor renders
    /// as colored tokens.
    /// </summary>
    public abstract class LanguageWriter
    {
        public abstract string WriteAssembly(AssemblyItem assembly);

        public abstract string WriteNamespace(ModuleItem module);

        public abstract string WriteType(TypeDefinition type);

        public abstract string WriteModule(ModuleDefinition module);

        public abstract string WriteMethod(MethodDefinition method);

        public abstract string WriteProperty(PropertyDefinition property);

        public abstract string WriteField(FieldDefinition field);

        public abstract string WriteEvent(EventDefinition @event);

        /// <summary>
        /// Builds the navigation key ("&lt;&lt;#assembly//Nnamespace//Type&gt;&gt;")
        /// that the editor uses to jump to the declaration of a metadata reference.
        /// </summary>
        public static string GetReferenceKey(object value)
        {
            if (value is AssemblyItem)
            {
                return "<<#" + ((AssemblyItem)value).Assembly.Name.FullName + ">>";
            }
            if (value is AssemblyNameReference)
            {
                return "<<#" + ((AssemblyNameReference)value).FullName + ">>";
            }
            if (value is AssemblyDefinition)
            {
                return "<<#" + ((AssemblyDefinition)value).Name.FullName + ">>";
            }
            if (value is ModuleItem)
            {
                ModuleItem module = (ModuleItem)value;
                return "<<#" + module.Module.Assembly.Name.FullName + "//N" + module.Namespace + ">>";
            }
            if (value is TypeReference)
            {
                TypeReference type = (TypeReference)value;
                return "<<#" + GetAssemblyName(type.Scope).FullName + "//N" + type.Namespace + "//" +
                       type.GetElementType().Name.Replace("<", string.Empty).Replace(">", string.Empty) + ">>";
            }
            if (value is MethodReference)
            {
                MethodReference method = (MethodReference)value;
                return "<<#" + GetAssemblyName(method.DeclaringType.Scope).FullName + "//N" +
                       method.DeclaringType.Namespace + "//" + method.DeclaringType.Name + "//M" +
                       GetMethodSignature(method.GetElementMethod()) + ">>";
            }
            if (value is FieldReference)
            {
                FieldReference field = (FieldReference)value;
                return "<<#" + GetAssemblyName(field.DeclaringType.Scope).FullName + "//N" +
                       field.DeclaringType.Namespace + "//" + field.DeclaringType.Name + "//F" + field.Name + ">>";
            }
            throw new ArgumentException("Unknown object type: " + (value == null ? "null" : value.GetType().FullName));
        }

        public static AssemblyNameReference GetAssemblyName(IMetadataScope scope)
        {
            AssemblyNameReference reference = scope as AssemblyNameReference;
            if (reference != null)
            {
                return reference;
            }

            ModuleDefinition module = scope as ModuleDefinition;
            if (module != null)
            {
                return module.Assembly.Name;
            }

            object rawScope = scope;
            AssemblyDefinition assembly = rawScope as AssemblyDefinition;
            if (assembly != null)
            {
                return assembly.Name;
            }

            throw new ArgumentException("Cannot determine scope type: " + (scope == null ? "null" : scope.GetType().FullName));
        }

        /// <summary>Removes the markup tags from a generated fragment.</summary>
        public string StripMarkup(string text)
        {
            text = Regex.Replace(text, "\\<\\<[r|g|b]?\\>\\>", string.Empty);
            text = Regex.Replace(text, "\\<\\<\\#[^\\>|\\<]+\\>\\>", string.Empty);
            return text;
        }

        public static string GetFullTypeName(TypeReference type)
        {
            return type.Namespace + "." + GetShortTypeName(type);
        }

        public static string GetShortTypeName(TypeReference type)
        {
            string name = type.GetElementType().Name;
            GenericInstanceType generic = type as GenericInstanceType;
            if (generic != null)
            {
                if (type.Name.Length > 2 && type.Name[type.Name.Length - 2] == '`')
                {
                    name = type.Name.Substring(0, type.Name.Length - 2);
                }

                name += "<";
                foreach (TypeReference argument in generic.GenericArguments)
                {
                    name += (string.IsNullOrEmpty(argument.Name) ? "?" : argument.Name) + ", ";
                }
                name = name.TrimEnd(' ', ',') + ">";
            }
            return name;
        }

        public static string GetMethodSignature(MethodReference method)
        {
            string text = "(";
            foreach (ParameterDefinition parameter in method.Parameters)
            {
                text += parameter.ParameterType.Name + ",";
            }
            return method.Name + text.TrimEnd(',') + ")";
        }

        public static string GetMethodSignatureWithReturn(MethodReference method)
        {
            string text = method.Name + "(";
            foreach (ParameterDefinition parameter in method.Parameters)
            {
                text += GetShortTypeName(parameter.ParameterType) + ",";
            }
            text = text.TrimEnd(',') + ")";
            if (!text.StartsWith(".", StringComparison.Ordinal) && method.ReturnType != null)
            {
                text += " : " + GetShortTypeName(method.ReturnType);
            }
            return text;
        }

        public static string FormatIlLabel(int offset)
        {
            return "<<r>>L_" + offset.ToString("x4") + "<<>>";
        }

        /// <summary>Chooses a VB-style prefix for a generated local variable name.</summary>
        internal string GetLocalNamePrefix(TypeReference type)
        {
            string name = type.GetElementType().Name.ToLowerInvariant();
            if (type.IsArray)
            {
                return "array";
            }
            if (name == "string")
            {
                return "str";
            }
            if (name == "boolean")
            {
                return "flag";
            }
            switch (name)
            {
                case "long":
                case "double":
                case "int16":
                case "int32":
                case "int64":
                case "uint16":
                case "uint32":
                case "uint64":
                    return "num";
                default:
                    return name;
            }
        }

        /// <summary>Creates a unique local name such as <c>num1</c>, <c>num2</c>, ...</summary>
        internal string CreateUniqueLocalName(TypeReference type, List<string> usedNames)
        {
            string prefix = GetLocalNamePrefix(type);
            int index = 1;
            while (usedNames.Contains(prefix + index.ToString()))
            {
                index++;
            }
            usedNames.Add(prefix + index.ToString());
            return prefix + index.ToString();
        }
    }
}
